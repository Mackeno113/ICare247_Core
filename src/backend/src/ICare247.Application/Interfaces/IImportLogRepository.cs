// File    : IImportLogRepository.cs
// Module  : Import
// Layer   : Application
// Purpose : Ghi log import (Sys_Import_Log + Detail) + gọi hook sau-import sp_AfterImport_<Table> (Data DB).
//           Spec 25 §12.2/§13, ADR-034.

namespace ICare247.Application.Interfaces;

/// <summary>Ghi nhật ký import cấp mẻ + chi tiết dòng lỗi; chạy hook sau-import (opt-in <c>OBJECT_ID</c>).</summary>
public interface IImportLogRepository
{
    /// <summary>Tạo bản ghi <c>Sys_Import_Log</c> (cấp mẻ) → trả Id. Sau đó caller ghi chi tiết + hoàn tất.</summary>
    Task<long> CreateAsync(ImportLogHeader header, CancellationToken ct = default);

    /// <summary>Ghi các dòng lỗi vào <c>Sys_Import_Log_Detail</c> (chỉ ERROR/SKIP — §13.2).</summary>
    Task AddErrorDetailsAsync(long importLogId, IReadOnlyList<ImportLogDetail> details, CancellationToken ct = default);

    /// <summary>Cập nhật thống kê + trạng thái cuối cho <c>Sys_Import_Log</c>.</summary>
    Task CompleteAsync(long importLogId, ImportLogCompletion completion, CancellationToken ct = default);

    /// <summary>
    /// Gọi <c>sp_AfterImport_&lt;Table&gt;</c> nếu tồn tại (opt-in <c>OBJECT_ID</c>) — hook sau import (§12.2).
    /// Lỗi hook KHÔNG rollback dữ liệu đã ghi; caller quyết định ghi cảnh báo. Trả true nếu proc đã chạy.
    /// </summary>
    Task<bool> RunAfterImportAsync(ImportAfterHookArgs args, CancellationToken ct = default);
}

/// <summary>Header 1 mẻ import (audit tường minh — CreatedBy/StartedAt do App set, ADR-022).</summary>
public sealed record ImportLogHeader(
    Guid SessionId, string ViewCode, string? TableName, string? FileName, long? FileSize,
    string? FileHash, string Mode, string Status, DateTime StartedAt, long CreatedBy, string? CorrelationId);

/// <summary>1 dòng chi tiết (lỗi): Row_Json đã làm mờ; Error_Args_Json đã làm mờ nếu chạm cột nhạy cảm.</summary>
public sealed record ImportLogDetail(
    int RowNumber, string Operation, long? RecordId, string? ErrorKey, string? ErrorArgsJson,
    string? FieldName, string? RowJson);

/// <summary>Thống kê + trạng thái cuối mẻ.</summary>
public sealed record ImportLogCompletion(
    int Total, int Inserted, int Updated, int ErrorCount, int Skipped, string Status,
    DateTime FinishedAt, int DurationMs);

/// <summary>Tham số hook sau-import (cấp mẻ).</summary>
public sealed record ImportAfterHookArgs(
    string Schema, string TableName, Guid SessionId, long UserId, int TenantId,
    int Inserted, int Updated, int ErrorCount, IReadOnlyList<long> RecordIds, DateTime ImportedAt);
