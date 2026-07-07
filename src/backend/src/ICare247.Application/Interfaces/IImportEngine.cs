// File    : IImportEngine.cs
// Module  : Import
// Layer   : Application
// Purpose : Đọc workbook import + trim + validate (format/required/FK/trùng khoá) + dựng kế hoạch
//           upsert khoá ghép + làm mờ log. Trả preview NEW/UPDATE/ERROR. Spec 25 §11–§13, ADR-034.

using ICare247.Domain.Entities.View;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Engine đọc file import theo cấu hình View + dựng <see cref="ImportPlan"/>: mỗi dòng phân loại
/// <c>New/Update/Error/Skip</c>, giá trị đã trim + FK resolve Mã→Id, và Row_Json đã làm mờ cột nhạy cảm.
/// KHÔNG ghi DB — chỉ lập kế hoạch (commit ở <c>CommitImportCommand</c>).
/// </summary>
public interface IImportEngine
{
    /// <summary>
    /// Phân tích <paramref name="workbook"/> theo <paramref name="request"/> → kế hoạch import (preview).
    /// Sau lời gọi, caller dựng preview hoặc (khi commit) ghi các dòng hợp lệ. Không phát sự kiện.
    /// </summary>
    Task<ImportPlan> BuildPlanAsync(
        ImportPlanRequest request, Stream workbook, CancellationToken ct = default);
}

/// <summary>Bối cảnh phân tích 1 file import: View + bảng đích + cột nhập + khoá ghép upsert + định nghĩa FK.</summary>
public sealed record ImportPlanRequest(
    ViewMetadata View,
    string Schema,
    string TargetTable,
    string PkColumn,
    string SheetName,
    IReadOnlyList<ImportFieldSpec> Fields,
    IReadOnlyList<string> KeyFields,
    IReadOnlyList<FkLookupDefinition> FkColumns);

/// <summary>
/// Mô tả 1 cột nhập của file import. <paramref name="NetType"/> ép kiểu validate; <paramref name="IsMasked"/>
/// + <paramref name="MaskMode"/> làm mờ khi ghi log (Full/Partial/Hash).
/// </summary>
public sealed record ImportFieldSpec(
    string FieldName,
    string Caption,
    bool Required,
    string NetType,
    bool IsMasked,
    string? MaskMode);

/// <summary>Kế hoạch import đã phân tích: các dòng + lỗi cấp file + thống kê.</summary>
public sealed record ImportPlan(
    IReadOnlyList<ImportRow> Rows,
    IReadOnlyList<ImportCellError> FileErrors,
    ImportSummary Summary);

/// <summary>Thống kê mẻ import.</summary>
public sealed record ImportSummary(int Total, int New, int Update, int Error, int Skipped);

/// <summary>Trạng thái xử lý 1 dòng.</summary>
public enum ImportRowOperation { New, Update, Error, Skip }

/// <summary>
/// Một dòng đã phân tích. <paramref name="Values"/> = giá trị đã trim + FK resolve Id (dùng để ghi khi commit);
/// <paramref name="MaskedRowJson"/> = Row_Json đã làm mờ (dùng ghi log dòng lỗi).
/// </summary>
public sealed record ImportRow(
    int RowNumber,
    ImportRowOperation Operation,
    long? MatchedId,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<ImportCellError> Errors,
    string? MaskedRowJson);

/// <summary>1 lỗi ô/dòng: key i18n + tham số (theo ADR-029). FieldName null = lỗi cấp dòng/file.</summary>
public sealed record ImportCellError(string? FieldName, string ErrorKey, IReadOnlyList<string> Args);
