// File    : AttachmentRepository.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Dapper impl IAttachmentRepository — TT_TepDinhKem (Data DB tenant) theo mô hình Blob⟂Attachment.
//           Mỗi dòng trỏ Blob_Id (+ThumbBlob_Id), gắn Owner_Table/Owner_Id/Field_Ma. Bytes nằm ở TT_TepBlob.

using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Truy cập bản ghi đính kèm TT_TepDinhKem. CreatedBy tường minh (ADR — không dựa DEFAULT).</summary>
public sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly IDataDbConnectionFactory _db;

    public AttachmentRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<long> InsertAsync(AttachmentInsert d, CancellationToken ct = default)
    {
        // Storage_Kind cột này để mặc định N'Db' (nội dung thật ở TT_TepBlob) — không cần khai.
        const string sql = """
            INSERT INTO dbo.TT_TepDinhKem
                (TenFile, ContentType, KichThuoc, Blob_Id, ThumbBlob_Id,
                 Owner_Table, Owner_Id, Field_Ma, Loai, Checksum, CreatedBy, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@TenFile, @ContentType, @KichThuoc, @BlobId, @ThumbBlobId,
                 @OwnerTable, @OwnerId, @FieldMa, @Loai, @Checksum, @UserId, SYSUTCDATETIME());
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            d.TenFile, d.ContentType, d.KichThuoc, d.BlobId, d.ThumbBlobId,
            d.OwnerTable, d.OwnerId, d.FieldMa, d.Loai, d.Checksum, UserId = d.UserId,
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AttachmentInfo>> ListByOwnerAsync(
        string ownerTable, long ownerId, string? fieldMa, CancellationToken ct = default)
    {
        const string sql = """
            SELECT a.Id, a.TenFile, a.ContentType, a.KichThuoc,
                   CAST(CASE WHEN a.ThumbBlob_Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasThumbnail,
                   a.CreatedAt
            FROM   dbo.TT_TepDinhKem a
            WHERE  a.Owner_Table = @OwnerTable AND a.Owner_Id = @OwnerId AND a.IsDeleted = 0
              AND  (@FieldMa IS NULL OR a.Field_Ma = @FieldMa)
            ORDER  BY a.CreatedAt DESC, a.Id DESC;
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<AttachmentInfo>(new CommandDefinition(
            sql, new { OwnerTable = ownerTable, OwnerId = ownerId, FieldMa = fieldMa }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <inheritdoc />
    public async Task<AttachmentInfo?> GetInfoAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT a.Id, a.TenFile, a.ContentType, a.KichThuoc,
                   CAST(CASE WHEN a.ThumbBlob_Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasThumbnail,
                   a.CreatedAt
            FROM   dbo.TT_TepDinhKem a
            WHERE  a.Id = @Id AND a.IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttachmentInfo>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<AttachmentRow?> GetAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Blob_Id AS BlobId, ThumbBlob_Id AS ThumbBlobId, TenFile, ContentType
            FROM   dbo.TT_TepDinhKem
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttachmentRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> LinkToOwnerAsync(
        IReadOnlyList<long> ids, string ownerTable, long ownerId, string? fieldMa, long userId,
        CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0) return 0;

        // Chỉ gắn dòng CÒN TREO (Owner_Id IS NULL) + đúng người upload → không cướp tệp đã gắn/của người khác.
        const string sql = """
            UPDATE dbo.TT_TepDinhKem
            SET    Owner_Table = @OwnerTable, Owner_Id = @OwnerId, Field_Ma = @FieldMa,
                   UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE  Id IN @Ids AND IsDeleted = 0 AND Owner_Id IS NULL AND CreatedBy = @UserId;
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Ids = ids, OwnerTable = ownerTable, OwnerId = ownerId, FieldMa = fieldMa, UserId = userId,
        }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(long id, long userId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.TT_TepDinhKem
            SET    IsDeleted = 1, UpdatedBy = @UserId, UpdatedAt = SYSUTCDATETIME(), Ver = Ver + 1
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }
}
