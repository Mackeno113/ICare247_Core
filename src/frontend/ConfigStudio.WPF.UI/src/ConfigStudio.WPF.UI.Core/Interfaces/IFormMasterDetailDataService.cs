// File    : IFormMasterDetailDataService.cs
// Module  : Data
// Layer   : Core
// Purpose : Interface truy vấn / ghi Ui_Form_Detail (pane master-detail / rail) + cột
//           Ui_Form.Detail_Layout, qua Dapper trên Config DB (db/106).

using ConfigStudio.WPF.UI.Core.Data;

namespace ConfigStudio.WPF.UI.Core.Interfaces;

/// <summary>
/// Cấu hình master-detail (rail workspace) cho form: bố cục form (<c>Ui_Form.Detail_Layout</c>)
/// + các pane chi tiết (<c>Ui_Form_Detail</c>). Mọi query parameterized; guard schema db/106.
/// </summary>
public interface IFormMasterDetailDataService
{
    /// <summary>Lấy danh sách form (Form_Id + Form_Code) để chọn form master / form con.</summary>
    Task<IReadOnlyList<FormLookupItem>> GetFormsAsync(CancellationToken ct = default);

    /// <summary>Đọc kiểu bố cục chi tiết của form master (Inline | Rail). Mặc định 'Inline'.</summary>
    Task<string> GetDetailLayoutAsync(int formId, CancellationToken ct = default);

    /// <summary>Ghi kiểu bố cục chi tiết của form master.</summary>
    /// <remarks>Side-effect: cập nhật <c>Ui_Form.Detail_Layout</c> + Version++.</remarks>
    Task SaveDetailLayoutAsync(int formId, string layout, CancellationToken ct = default);

    /// <summary>Lấy danh sách pane chi tiết của 1 form master (kèm Form_Code form con đã join).</summary>
    /// <param name="formId">Form master.</param>
    /// <param name="includeInactive">true = lấy cả pane đã ẩn.</param>
    Task<IReadOnlyList<FormMasterDetailRecord>> GetPanesAsync(
        int formId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Tạo mới (DetailId=0) hoặc cập nhật một pane. Trả Detail_Id sau khi ghi.</summary>
    /// <remarks>Side-effect: ghi 1 dòng Ui_Form_Detail; ném lỗi nếu trùng (Form_Id + Detail_Code).</remarks>
    Task<int> SavePaneAsync(FormMasterDetailRecord r, CancellationToken ct = default);

    /// <summary>Ẩn (soft-delete) một pane — set Is_Active = 0.</summary>
    Task DeactivatePaneAsync(int detailId, CancellationToken ct = default);
}
