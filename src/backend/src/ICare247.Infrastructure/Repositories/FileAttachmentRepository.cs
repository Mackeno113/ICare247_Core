// File    : FileAttachmentRepository.cs
// Module  : Files
// Layer   : Infrastructure
// Purpose : Dapper impl IFileAttachmentRepository — TT_TepDinhKem (Data DB tenant). Bytes trong DB
//           (Storage_Kind='Db'). VARBINARY(MAX) bind qua DbType.Binary size=-1 (tránh cắt 8000 byte).

using System.Data;
using Dapper;
using ICare247.Application.Features.Files.GetFile;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Repositories;

/// <summary>Lưu/đọc tệp đính kèm ở Data DB. CreatedBy tường minh (ADR — không dựa DEFAULT).</summary>
public sealed class FileAttachmentRepository : IFileAttachmentRepository
{
    private readonly IDataDbConnectionFactory _db;

    public FileAttachmentRepository(IDataDbConnectionFactory db) => _db = db;

    /// <inheritdoc />
    public async Task<long> InsertAsync(
        string tenFile, string contentType, long kichThuoc, byte[] noiDung,
        string? loai, string? checksum, long userId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.TT_TepDinhKem
                (TenFile, ContentType, KichThuoc, Storage_Kind, NoiDung, Loai, Checksum, CreatedBy, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@TenFile, @ContentType, @KichThuoc, N'Db', @NoiDung, @Loai, @Checksum, @UserId, SYSUTCDATETIME());
            """;

        var p = new DynamicParameters();
        p.Add("@TenFile", tenFile);
        p.Add("@ContentType", contentType);
        p.Add("@KichThuoc", kichThuoc);
        p.Add("@NoiDung", noiDung, DbType.Binary, size: -1); // -1 = VARBINARY(MAX), không cắt 8000 byte
        p.Add("@Loai", loai);
        p.Add("@Checksum", checksum);
        p.Add("@UserId", userId);

        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, p, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<FileContentDto?> GetAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TenFile, ContentType, KichThuoc, NoiDung, Checksum
            FROM   dbo.TT_TepDinhKem
            WHERE  Id = @Id AND IsDeleted = 0;
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FileContentDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
