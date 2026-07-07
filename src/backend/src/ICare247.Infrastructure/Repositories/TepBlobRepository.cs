// File    : TepBlobRepository.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Dapper impl ITepBlobRepository — TT_TepBlob (Data DB tenant). Upsert dedup race-safe bằng
//           MERGE HOLDLOCK; đọc nội dung để stream; giảm/đánh dấu xóa cho cơ chế dọn rác.

using System.Data;
using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Truy cập TT_TepBlob — đơn vị dedup theo Checksum + đếm tham chiếu (RefCount).</summary>
public sealed class TepBlobRepository : ITepBlobRepository
{
    private readonly IDataDbConnectionFactory _db;

    public TepBlobRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<long> UpsertAsync(
        string checksum, string contentType, long kichThuoc, string storageKind,
        byte[]? noiDung, string? storageKey, long userId, CancellationToken ct = default)
    {
        // MERGE HOLDLOCK: khóa khoảng theo Checksum → 2 upload cùng nội dung đồng thời không tạo 2 blob.
        // MATCHED (đã có) → RefCount++; NOT MATCHED → chèn mới RefCount=1. OUTPUT trả Id cho cả 2 nhánh.
        const string sql = """
            MERGE dbo.TT_TepBlob WITH (HOLDLOCK) AS t
            USING (SELECT @Checksum AS Checksum) AS s
               ON t.Checksum = s.Checksum AND t.IsDeleted = 0
            WHEN MATCHED THEN
                UPDATE SET RefCount = t.RefCount + 1, UpdatedBy = @UserId,
                           UpdatedAt = SYSUTCDATETIME(), Ver = t.Ver + 1
            WHEN NOT MATCHED THEN
                INSERT (Checksum, ContentType, KichThuoc, Storage_Kind, NoiDung, Storage_Key,
                        RefCount, CreatedBy, CreatedAt)
                VALUES (@Checksum, @ContentType, @KichThuoc, @StorageKind, @NoiDung, @StorageKey,
                        1, @UserId, SYSUTCDATETIME())
            OUTPUT INSERTED.Id;
            """;

        var p = new DynamicParameters();
        p.Add("@Checksum", checksum);
        p.Add("@ContentType", contentType);
        p.Add("@KichThuoc", kichThuoc);
        p.Add("@StorageKind", storageKind);
        p.Add("@NoiDung", noiDung, DbType.Binary, size: -1); // -1 = VARBINARY(MAX), không cắt 8000 byte
        p.Add("@StorageKey", storageKey);
        p.Add("@UserId", userId);

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<TepBlobContent?> GetContentAsync(long blobId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Storage_Kind AS StorageKind, NoiDung, Storage_Key AS StorageKey,
                   ContentType, KichThuoc, Checksum
            FROM   dbo.TT_TepBlob
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TepBlobContent>(
            new CommandDefinition(sql, new { Id = blobId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> DecrementRefAsync(long blobId, long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.TT_TepBlob
            SET    RefCount = CASE WHEN RefCount > 0 THEN RefCount - 1 ELSE 0 END,
                   UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            OUTPUT INSERTED.RefCount
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        var newRef = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { Id = blobId, UserId = userId }, cancellationToken: ct));
        return newRef ?? -1; // không có dòng (đã xóa) → -1
    }

    /// <inheritdoc />
    public async Task MarkDeletedAsync(long blobId, long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.TT_TepBlob
            SET    IsDeleted = 1, UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE  Id = @Id;
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = blobId, UserId = userId }, cancellationToken: ct));
    }
}
