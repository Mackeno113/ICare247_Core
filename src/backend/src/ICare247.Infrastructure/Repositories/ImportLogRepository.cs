// File    : ImportLogRepository.cs
// Module  : Import
// Layer   : Infrastructure
// Purpose : Ghi Sys_Import_Log + Sys_Import_Log_Detail (Data DB) + gọi hook sp_AfterImport_<Table>.
//           Spec 25 §12.2/§13, ADR-034.

using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Dapper impl ghi log import (Data DB tenant) + chạy hook sau-import opt-in (<c>OBJECT_ID</c>).</summary>
public sealed partial class ImportLogRepository : IImportLogRepository
{
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ImportLogRepository> _logger;

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeIdentifierRegex();

    public ImportLogRepository(IDataDbConnectionFactory dataDb, ILogger<ImportLogRepository> logger)
    {
        _dataDb = dataDb;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(ImportLogHeader h, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Sys_Import_Log
                (ImportSessionId, View_Code, Table_Name, File_Name, File_Size, File_Hash,
                 [Mode], [Status], Started_At, Correlation_Id, CreatedBy, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@SessionId, @ViewCode, @TableName, @FileName, @FileSize, @FileHash,
                 @Mode, @Status, @StartedAt, @CorrelationId, @CreatedBy, @CreatedAt);
            """;
        using var conn = _dataDb.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            h.SessionId, ViewCode = h.ViewCode, h.TableName, h.FileName, h.FileSize, h.FileHash,
            h.Mode, h.Status, h.StartedAt, h.CorrelationId, h.CreatedBy, CreatedAt = h.StartedAt
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task AddErrorDetailsAsync(
        long importLogId, IReadOnlyList<ImportLogDetail> details, CancellationToken ct = default)
    {
        if (details.Count == 0) return;

        const string sql = """
            INSERT INTO dbo.Sys_Import_Log_Detail
                (Import_Log_Id, Row_Number, Operation, Record_Id, Error_Key, Error_Args_Json, Field_Name, Row_Json)
            VALUES
                (@ImportLogId, @RowNumber, @Operation, @RecordId, @ErrorKey, @ErrorArgsJson, @FieldName, @RowJson);
            """;
        using var conn = _dataDb.CreateConnection();
        foreach (var d in details)
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ImportLogId = importLogId,
                d.RowNumber, d.Operation, d.RecordId, d.ErrorKey, d.ErrorArgsJson, d.FieldName, d.RowJson
            }, cancellationToken: ct));
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync(
        long importLogId, ImportLogCompletion c, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Sys_Import_Log
            SET Total_Rows = @Total, Inserted = @Inserted, Updated = @Updated,
                Error_Count = @ErrorCount, Skipped = @Skipped, [Status] = @Status,
                Finished_At = @FinishedAt, Duration_Ms = @DurationMs
            WHERE Id = @Id;
            """;
        using var conn = _dataDb.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = importLogId,
            c.Total, c.Inserted, c.Updated, c.ErrorCount, c.Skipped, c.Status, c.FinishedAt, c.DurationMs
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> RunAfterImportAsync(ImportAfterHookArgs a, CancellationToken ct = default)
    {
        if (!SafeIdentifierRegex().IsMatch(a.TableName))
            return false;

        var procName = $"dbo.sp_AfterImport_{a.TableName}";
        var recordIdsJson = JsonSerializer.Serialize(a.RecordIds);

        // Opt-in: proc chưa tồn tại → bỏ qua (màn chưa bật hook chạy như thường).
        const string existsSql = "SELECT CASE WHEN OBJECT_ID(@Proc, 'P') IS NULL THEN 0 ELSE 1 END";
        using var conn = _dataDb.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { Proc = procName }, cancellationToken: ct));
        if (exists == 0)
            return false;

        var execSql = $"EXEC {procName} " +
            "@ImportSessionId=@SessionId, @NguoiDungID=@UserId, @TenantId=@TenantId, " +
            "@InsertedCount=@Inserted, @UpdatedCount=@Updated, @ErrorCount=@ErrorCount, " +
            "@RecordIdsJson=@RecordIdsJson, @ImportedAt=@ImportedAt";
        await conn.ExecuteAsync(new CommandDefinition(execSql, new
        {
            a.SessionId, a.UserId, a.TenantId, a.Inserted, a.Updated, a.ErrorCount,
            RecordIdsJson = recordIdsJson, a.ImportedAt
        }, cancellationToken: ct));
        return true;
    }
}
