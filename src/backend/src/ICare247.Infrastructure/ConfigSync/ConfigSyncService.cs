// File    : ConfigSyncService.cs
// Module  : ConfigSync
// Layer   : Infrastructure
// Purpose : Engine đồng bộ config master → Config DB tenant (CFGSYNC-2, spec 16).
//           Generic descriptor-driven: UPSERT theo MÃ + re-link FK theo mã, đúng thứ tự phụ thuộc,
//           một chiều, transaction/tenant, dry-run preview. Vertical slice 5 bảng (ConfigSyncTables).
//
// Thuật toán (mỗi bảng, theo thứ tự ConfigSyncTables.Order):
//   1) Đọc cột thực tế (INFORMATION_SCHEMA) ở master + tenant → tập cột ghi = giao hai bên.
//   2) Đọc dòng master + tenant → dựng "khóa nghiệp vụ" mỗi dòng (mã + ngữ cảnh cha đã re-link).
//   3) Với mỗi dòng master: khớp theo khóa nghiệp vụ →
//        • có ở tenant + Is_Customized=1 → BỎ QUA (giữ bản tenant).
//        • có ở tenant            → UPDATE từ master (re-link FK, Is_System=1, Synced_At/Source_Ver).
//        • chưa có                → INSERT (OUTPUT Id mới → cập nhật map cho bảng con).
//   4) Tombstone: dòng tenant Is_System=1, chưa tùy biến, không còn ở master → Is_Active=0.

using System.Data;
using System.Data.Common;
using Dapper;
using ICare247.Application.ConfigSync;
using ICare247.Application.Interfaces;
using ICare247.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.ConfigSync;

/// <summary>
/// Cài đặt <see cref="IConfigSyncService"/>. Master = <c>ConnectionStrings:Config</c> (DB "vàng" canonical —
/// dev hiện tại trùng tenant nên sync vô hại; khi tách 1 DB/tenant đổi nguồn master tại đây). Tenant đích =
/// <see cref="IDbConnectionFactory"/> (scoped, tenant-aware). Tiền đề: đã chạy <c>db/050</c> hai phía.
/// </summary>
public sealed class ConfigSyncService : IConfigSyncService
{
    // Ký tự ngăn cách trong khóa nghiệp vụ ghép (không xuất hiện trong mã) — phân tách ngữ cảnh cha ↔ mã con.
    private const char KeySeparator = '';

    private readonly IDbConnectionFactory _tenantFactory;
    private readonly string _masterConnString;
    private readonly ILogger<ConfigSyncService> _logger;

    /// <summary>Khởi tạo engine. Đọc connection master từ cấu hình ngay lúc tạo (đời sống scoped).</summary>
    public ConfigSyncService(
        IDbConnectionFactory tenantFactory,
        IConfiguration configuration,
        ILogger<ConfigSyncService> logger)
    {
        _tenantFactory = tenantFactory;
        _logger = logger;
        _masterConnString = configuration.GetConnectionString("Config")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:Config (nguồn master cho đồng bộ config).");
    }

    /// <inheritdoc />
    public async Task<ConfigSyncResult> SyncAsync(ConfigSyncOptions options, CancellationToken ct = default)
    {
        var result = new ConfigSyncResult { DryRun = options.DryRun, StartedAt = DateTime.Now };

        // Master chỉ đọc — dùng SqlConnectionFactory để không new SqlConnection trực tiếp.
        var masterFactory = new SqlConnectionFactory(_masterConnString);
        using var master = masterFactory.CreateConnection();
        using var tenant = _tenantFactory.CreateConnection();
        await OpenAsync(master, ct);
        await OpenAsync(tenant, ct);

        // Ghi tenant nằm trong 1 transaction; dry-run không mở transaction (chỉ đọc).
        using var tx = options.DryRun ? null : tenant.BeginTransaction();

        // State dùng chung giữa các bảng — bảng con tra map Code↔Id của cha tại đây.
        var states = new Dictionary<string, TableSyncState>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var d in ConfigSyncTables.Order)
            {
                ct.ThrowIfCancellationRequested();
                var tableResult = await SyncTableAsync(d, master, tenant, tx, states, options.DryRun, ct);
                result.Tables.Add(tableResult);
            }

            if (!options.DryRun) tx!.Commit();
            result.FinishedAt = DateTime.Now;
            await WriteSyncLogAsync(tenant, transaction: null, options, result, ct);
            return result;
        }
        catch (Exception ex)
        {
            try { tx?.Rollback(); } catch { /* rollback best-effort */ }
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.FinishedAt = DateTime.Now;
            // Log ngoài transaction đã rollback (transaction:null) để vết đồng bộ thất bại vẫn lưu lại.
            try { await WriteSyncLogAsync(tenant, transaction: null, options, result, ct); }
            catch (Exception logEx) { _logger.LogError(logEx, "Ghi log đồng bộ thất bại."); }
            _logger.LogError(ex, "Đồng bộ config thất bại ở bảng đang xử lý.");
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Đồng bộ 1 bảng. Sự kiện theo sau: state[d.TableName] sẵn sàng cho bảng con re-link.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<ConfigSyncTableResult> SyncTableAsync(
        ConfigTableDescriptor d, IDbConnection master, IDbConnection tenant, IDbTransaction? tx,
        Dictionary<string, TableSyncState> states, bool dryRun, CancellationToken ct)
    {
        var res = new ConfigSyncTableResult { TableName = d.TableName };
        var state = new TableSyncState();
        states[d.TableName] = state;

        // (1) Tập cột ghi = giao cột master ∩ tenant, bỏ cột Id. Bắt buộc có 4 cột cờ (db/050).
        //     Master chỉ đọc (không tx); tenant trong nhánh apply có tx → phải truyền tx.
        var masterCols = await GetColumnsAsync(master, d.TableName, null, ct);
        var tenantCols = await GetColumnsAsync(tenant, d.TableName, tx, ct);
        EnsureSyncFlags(d.TableName, masterCols);
        EnsureSyncFlags(d.TableName, tenantCols);
        var writeCols = masterCols
            .Where(c => tenantCols.Contains(c) && !c.Equals(d.IdColumn, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // (2) Đọc dòng master (đủ cột để copy) + dòng tenant (đủ cột để khớp khóa & cờ).
        var masterRows = await ReadRowsAsync(master, d.TableName, masterCols, ct);
        var tenantRows = await ReadTenantRowsAsync(tenant, d, tx, ct);

        // (3) Dựng map khóa nghiệp vụ phía master (cho bảng con re-link tới bảng này).
        var masterKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyedMasterRows = new List<(IDictionary<string, object> Row, string Key)>();
        foreach (var row in masterRows)
        {
            var local = AsString(row[d.LocalKeyColumn]);
            if (string.IsNullOrWhiteSpace(local)) { res.Skipped++; continue; } // thiếu mã → không khớp được
            var key = BuildKey(d, row, states);
            if (key is null) { res.Skipped++; continue; }                      // cha mồ côi → bỏ qua
            state.MasterKeyById[GetInt(row[d.IdColumn])] = key;
            masterKeySet.Add(key);
            keyedMasterRows.Add((row, key));
        }

        // (4) Dựng map khóa nghiệp vụ phía tenant.
        foreach (var tr in tenantRows)
        {
            if (string.IsNullOrWhiteSpace(tr.LocalCode)) continue;
            var key = BuildTenantKey(d, tr, states);
            if (key is null) continue;
            tr.Key = key;
            state.TenantKeyById[tr.Id] = key;
            state.TenantIdByKey[key] = tr.Id;
            state.TenantFlagsByKey[key] = tr;
        }

        // (5) UPSERT theo khóa.
        foreach (var (row, key) in keyedMasterRows)
        {
            if (state.TenantFlagsByKey.TryGetValue(key, out var existing))
            {
                if (existing.Customized) { res.Skipped++; continue; }          // giữ bản tenant (spec §4)
                if (!dryRun) await UpdateRowAsync(d, tenant, tx, writeCols, row, existing.Id, states, ct);
                res.Updated++;
            }
            else
            {
                if (!dryRun)
                {
                    var newId = await InsertRowAsync(d, tenant, tx, writeCols, row, states, ct);
                    state.TenantIdByKey[key] = newId;                          // bảng con dùng được ngay
                    state.TenantKeyById[newId] = key;
                }
                res.Inserted++;
            }
        }

        // (6) Tombstone — chỉ bảng có cột Is_Active.
        if (d.ActiveColumn is not null)
        {
            foreach (var tr in tenantRows)
            {
                if (tr.Key is null || !tr.System || tr.Customized || !tr.Active) continue;
                if (masterKeySet.Contains(tr.Key)) continue;                   // master vẫn còn → không ngừng
                if (!dryRun) await DeactivateRowAsync(d, tenant, tx, tr.Id, ct);
                res.Deactivated++;
            }
        }

        _logger.LogInformation(
            "Sync {Table}: +{Ins} ~{Upd} x{Deact} skip{Skip} (dryRun={Dry}).",
            d.TableName, res.Inserted, res.Updated, res.Deactivated, res.Skipped, dryRun);
        return res;
    }

    // ── Khóa nghiệp vụ ───────────────────────────────────────────────────────

    /// <summary>Khóa nghiệp vụ dòng master = [khóa cha đã re-link] + mã con. Null nếu cha mồ côi.</summary>
    private static string? BuildKey(
        ConfigTableDescriptor d, IDictionary<string, object> row,
        Dictionary<string, TableSyncState> states)
    {
        var local = AsString(row[d.LocalKeyColumn])!;
        if (d.ContextParent is null) return local;

        var fk = row[d.ContextParent.FkColumn];
        if (fk is null or DBNull) return null;
        var parent = states[d.ContextParent.ParentTable];
        return parent.MasterKeyById.TryGetValue(GetInt(fk), out var parentKey)
            ? parentKey + KeySeparator + local
            : null;
    }

    /// <summary>Khóa nghiệp vụ dòng tenant (dùng map tenant của cha).</summary>
    private static string? BuildTenantKey(
        ConfigTableDescriptor d, TenantRow tr, Dictionary<string, TableSyncState> states)
    {
        if (d.ContextParent is null) return tr.LocalCode;
        if (tr.ContextFk is null) return null;
        var parent = states[d.ContextParent.ParentTable];
        return parent.TenantKeyById.TryGetValue(tr.ContextFk.Value, out var parentKey)
            ? parentKey + KeySeparator + tr.LocalCode
            : null;
    }

    // ── INSERT / UPDATE / Tombstone ────────────────────────────────────────────

    private async Task<int> InsertRowAsync(
        ConfigTableDescriptor d, IDbConnection tenant, IDbTransaction? tx, List<string> writeCols,
        IDictionary<string, object> row, Dictionary<string, TableSyncState> states, CancellationToken ct)
    {
        var p = new DynamicParameters();
        var colList = new List<string>();
        var valList = new List<string>();
        var i = 0;
        foreach (var col in writeCols)
        {
            colList.Add($"[{col}]");
            valList.Add($"@p{i}");
            p.Add($"@p{i}", ResolveWriteValue(d, col, row, states, isInsert: true));
            i++;
        }
        var sql = $"INSERT INTO dbo.[{d.TableName}] ({string.Join(", ", colList)}) " +
                  $"OUTPUT INSERTED.[{d.IdColumn}] VALUES ({string.Join(", ", valList)});";
        return await tenant.ExecuteScalarAsync<int>(new CommandDefinition(sql, p, tx, cancellationToken: ct));
    }

    private async Task UpdateRowAsync(
        ConfigTableDescriptor d, IDbConnection tenant, IDbTransaction? tx, List<string> writeCols,
        IDictionary<string, object> row, int tenantId, Dictionary<string, TableSyncState> states, CancellationToken ct)
    {
        var p = new DynamicParameters();
        var setList = new List<string>();
        var i = 0;
        foreach (var col in writeCols)
        {
            // Giữ nguyên Is_Customized của tenant (đã lọc dòng customized ở trên — đây là phòng hờ).
            if (col.Equals("Is_Customized", StringComparison.OrdinalIgnoreCase)) continue;
            setList.Add($"[{col}] = @p{i}");
            p.Add($"@p{i}", ResolveWriteValue(d, col, row, states, isInsert: false));
            i++;
        }
        p.Add("@id", tenantId);
        var sql = $"UPDATE dbo.[{d.TableName}] SET {string.Join(", ", setList)} WHERE [{d.IdColumn}] = @id;";
        await tenant.ExecuteAsync(new CommandDefinition(sql, p, tx, cancellationToken: ct));
    }

    private async Task DeactivateRowAsync(
        ConfigTableDescriptor d, IDbConnection tenant, IDbTransaction? tx, int tenantId, CancellationToken ct)
    {
        var sql = $"UPDATE dbo.[{d.TableName}] SET [{d.ActiveColumn}] = 0, [Synced_At] = @now " +
                  $"WHERE [{d.IdColumn}] = @id;";
        await tenant.ExecuteAsync(new CommandDefinition(
            sql, new { now = DateTime.Now, id = tenantId }, tx, cancellationToken: ct));
    }

    /// <summary>Giá trị ghi cho 1 cột: FK→re-link, cờ→engine set, còn lại→giá trị master.</summary>
    private object? ResolveWriteValue(
        ConfigTableDescriptor d, string col, IDictionary<string, object> row,
        Dictionary<string, TableSyncState> states, bool isInsert)
    {
        // Cột FK cần re-link sang Id tenant theo mã.
        var fkLink = d.RelinkParents.FirstOrDefault(
            r => r.FkColumn.Equals(col, StringComparison.OrdinalIgnoreCase));
        if (fkLink is not null) return ResolveTenantFk(fkLink, row, states);

        if (col.Equals("Is_System", StringComparison.OrdinalIgnoreCase)) return true;
        if (col.Equals("Is_Customized", StringComparison.OrdinalIgnoreCase)) return false; // chỉ chạy khi INSERT
        if (col.Equals("Synced_At", StringComparison.OrdinalIgnoreCase)) return DateTime.Now;
        if (col.Equals("Source_Ver", StringComparison.OrdinalIgnoreCase))
            return d.VersionColumn is not null && row.TryGetValue(d.VersionColumn, out var v) ? Normalize(v) : null;

        return Normalize(row.TryGetValue(col, out var val) ? val : null);
    }

    /// <summary>Dịch giá trị FK master → Id tenant qua khóa nghiệp vụ của cha. Null FK → null.</summary>
    private static int? ResolveTenantFk(
        ParentLink link, IDictionary<string, object> row, Dictionary<string, TableSyncState> states)
    {
        var fk = row.TryGetValue(link.FkColumn, out var v) ? v : null;
        if (fk is null or DBNull) return null;
        var parent = states[link.ParentTable];
        if (!parent.MasterKeyById.TryGetValue(GetInt(fk), out var parentKey))
            throw new InvalidOperationException(
                $"Re-link FK {link.FkColumn}: không tìm thấy mã master cho Id={fk} (bảng cha {link.ParentTable}).");
        if (!parent.TenantIdByKey.TryGetValue(parentKey, out var tenantId))
            throw new InvalidOperationException(
                $"Re-link FK {link.FkColumn}: cha '{parentKey}' chưa có ở tenant (bảng {link.ParentTable}).");
        return tenantId;
    }

    // ── Đọc dữ liệu ────────────────────────────────────────────────────────────

    /// <summary>Tên cột thực tế của bảng (INFORMATION_SCHEMA) — để dựng SELECT/INSERT tường minh, không SELECT *.</summary>
    /// <remarks>Phải truyền <paramref name="tx"/> khi đọc trên connection đang có transaction (nhánh apply trên
    /// tenant); nếu không SqlClient ném "command ... in a pending local transaction".</remarks>
    private static async Task<HashSet<string>> GetColumnsAsync(
        IDbConnection conn, string table, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM   INFORMATION_SCHEMA.COLUMNS
            WHERE  TABLE_NAME = @t AND TABLE_SCHEMA = 'dbo';
            """;
        var cols = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { t = table }, transaction: tx, cancellationToken: ct));
        return new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Đọc toàn bộ dòng master (SELECT cột tường minh) dưới dạng dictionary cột→giá trị.</summary>
    private static async Task<List<IDictionary<string, object>>> ReadRowsAsync(
        IDbConnection conn, string table, HashSet<string> cols, CancellationToken ct)
    {
        var select = string.Join(", ", cols.Select(c => $"[{c}]"));
        var sql = $"SELECT {select} FROM dbo.[{table}];";
        var rows = await conn.QueryAsync(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Cast<IDictionary<string, object>>().ToList();
    }

    /// <summary>Đọc dòng tenant đủ để khớp khóa + đọc cờ (Id, mã, FK ngữ cảnh, Is_System/Customized/Active).</summary>
    private static async Task<List<TenantRow>> ReadTenantRowsAsync(
        IDbConnection conn, ConfigTableDescriptor d, IDbTransaction? tx, CancellationToken ct)
    {
        var cols = new List<string> { d.IdColumn, d.LocalKeyColumn, "Is_System", "Is_Customized" };
        if (d.ContextParent is not null) cols.Add(d.ContextParent.FkColumn);
        if (d.ActiveColumn is not null) cols.Add(d.ActiveColumn);

        var select = string.Join(", ", cols.Distinct().Select(c => $"[{c}]"));
        var sql = $"SELECT {select} FROM dbo.[{d.TableName}];";
        var rows = await conn.QueryAsync(new CommandDefinition(sql, transaction: tx, cancellationToken: ct));

        var list = new List<TenantRow>();
        foreach (IDictionary<string, object> r in rows.Cast<IDictionary<string, object>>())
        {
            list.Add(new TenantRow
            {
                Id = GetInt(r[d.IdColumn]),
                LocalCode = AsString(r[d.LocalKeyColumn]) ?? "",
                System = ToBool(r["Is_System"]),
                Customized = ToBool(r["Is_Customized"]),
                Active = d.ActiveColumn is null || ToBool(r[d.ActiveColumn]),
                ContextFk = d.ContextParent is not null && r[d.ContextParent.FkColumn] is { } fk and not DBNull
                    ? GetInt(fk) : null,
            });
        }
        return list;
    }

    // ── Log + tiện ích ──────────────────────────────────────────────────────────

    /// <summary>Ghi 1 dòng vào Sys_Config_Sync_Log (audit + dry-run, spec §7). Bỏ qua nếu bảng chưa tồn tại.</summary>
    private async Task WriteSyncLogAsync(
        IDbConnection tenant, IDbTransaction? transaction, ConfigSyncOptions options,
        ConfigSyncResult result, CancellationToken ct)
    {
        if (await GetColumnsAsync(tenant, "Sys_Config_Sync_Log", transaction, ct) is { Count: 0 }) return;

        const string sql = """
            INSERT INTO dbo.Sys_Config_Sync_Log
                (Tenant_Code, Started_At, Finished_At, Source_Ver, Is_DryRun,
                 Rows_Inserted, Rows_Updated, Rows_Deactivated, Rows_Skipped,
                 Status, Detail_Json, Error_Message, Triggered_By)
            VALUES
                (@TenantCode, @StartedAt, @FinishedAt, @SourceVer, @IsDryRun,
                 @Ins, @Upd, @Deact, @Skip,
                 @Status, @Detail, @Error, @By);
            """;
        var detail = string.Join("; ", result.Tables.Select(
            t => $"{t.TableName}:+{t.Inserted}/~{t.Updated}/x{t.Deactivated}/s{t.Skipped}"));
        await tenant.ExecuteAsync(new CommandDefinition(sql, new
        {
            TenantCode = (string?)null,
            result.StartedAt,
            result.FinishedAt,
            SourceVer = (int?)null,
            IsDryRun = options.DryRun,
            Ins = result.TotalInserted,
            Upd = result.TotalUpdated,
            Deact = result.TotalDeactivated,
            Skip = result.TotalSkipped,
            result.Status,
            Detail = detail,
            Error = result.ErrorMessage,
            By = options.TriggeredBy,
        }, transaction, cancellationToken: ct));
    }

    /// <summary>Bảo đảm bảng có đủ 4 cột cờ đồng bộ (db/050). Thiếu → lỗi thân thiện.</summary>
    private static void EnsureSyncFlags(string table, HashSet<string> cols)
    {
        foreach (var f in new[] { "Is_System", "Is_Customized", "Synced_At", "Source_Ver" })
            if (!cols.Contains(f))
                throw new InvalidOperationException(
                    $"Bảng {table} thiếu cột '{f}' — chạy db/050_alter_config_sync_flags.sql trước khi đồng bộ.");
    }

    private static async Task OpenAsync(IDbConnection conn, CancellationToken ct)
    {
        if (conn.State == ConnectionState.Open) return;
        if (conn is DbConnection db) await db.OpenAsync(ct); else conn.Open();
    }

    private static string? AsString(object? v) => v is null or DBNull ? null : v.ToString();
    private static int GetInt(object v) => Convert.ToInt32(v);
    private static bool ToBool(object? v) => v is not (null or DBNull) && Convert.ToBoolean(v);
    private static object? Normalize(object? v) => v is DBNull ? null : v;

    // ── State runtime ────────────────────────────────────────────────────────

    /// <summary>Map Code↔Id của một bảng đã đồng bộ — bảng con tra để re-link FK.</summary>
    private sealed class TableSyncState
    {
        /// <summary>Id master → khóa nghiệp vụ (để dịch FK của con).</summary>
        public Dictionary<int, string> MasterKeyById { get; } = new();

        /// <summary>Id tenant → khóa nghiệp vụ.</summary>
        public Dictionary<int, string> TenantKeyById { get; } = new();

        /// <summary>Khóa nghiệp vụ → Id tenant (tra FK đích + khớp upsert).</summary>
        public Dictionary<string, int> TenantIdByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Khóa nghiệp vụ → cờ dòng tenant (để biết customized khi upsert).</summary>
        public Dictionary<string, TenantRow> TenantFlagsByKey { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Dòng tenant rút gọn cho khớp khóa + cờ.</summary>
    private sealed class TenantRow
    {
        public int Id { get; init; }
        public string LocalCode { get; init; } = "";
        public bool System { get; init; }
        public bool Customized { get; init; }
        public bool Active { get; init; }
        public int? ContextFk { get; init; }
        public string? Key { get; set; }
    }
}
