// File    : IImportMetadataProvider.cs
// Module  : Import
// Layer   : Application
// Purpose : Dựng bối cảnh import từ cấu hình View + Edit_Form (cột nhập, kiểu, bắt buộc, khoá ghép, masking).
//           Dùng chung cho template + validate + commit. Spec 25 §11–§14, ADR-034.

using ICare247.Domain.Entities.View;

namespace ICare247.Application.Interfaces;

/// <summary>
/// Tổng hợp metadata cần cho import 1 View: bảng đích + danh sách cột nhập (kiểu/bắt buộc/masking) + khoá ghép.
/// Đọc <c>Ui_View</c> (Import_Key_Fields), <c>Ui_Form</c>/<c>Ui_Field</c> (cột nhập) và <c>Sys_Column</c> (masking).
/// </summary>
public interface IImportMetadataProvider
{
    /// <summary>
    /// Dựng <see cref="ImportContext"/> cho View. Trả null nếu View/Edit_Form không tồn tại (không import được).
    /// Không phát sự kiện.
    /// </summary>
    Task<ImportContext?> BuildAsync(
        string viewCode, int tenantId, string langCode, CancellationToken ct = default);
}

/// <summary>Bối cảnh import đã tổng hợp — nguồn cho <see cref="ImportPlanRequest"/> + template + commit.</summary>
public sealed record ImportContext(
    ViewMetadata View,
    string FormCode,
    string Schema,
    string TargetTable,
    string PkColumn,
    string SheetName,
    IReadOnlyList<ImportFieldSpec> Fields,
    IReadOnlyList<string> KeyFields,
    IReadOnlyList<FkLookupDefinition> FkColumns);
