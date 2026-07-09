// File    : IDocTemplateRenderer.cs
// Module  : DocTemplate
// Layer   : Application
// Purpose : Hợp đồng sinh văn bản từ mẫu (mail-merge, ghép fragment master+detail).
//           Ẩn hoàn toàn DevExpress khỏi Application — impl ở ICare247.Infrastructure.Documents.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §7.1.

namespace ICare247.Application.Interfaces;

/// <summary>
/// Sinh tài liệu (.docx/.pdf) từ bộ mẫu <c>Doc_Template</c>: bơm dữ liệu thật (qua stored proc)
/// vào các biến, lặp bảng detail, ghép master (A4 dọc) + N detail (A4 ngang) thành 1 file.
/// </summary>
/// <remarks>Impl duy nhất tham chiếu DevExpress Office File API (project cô lập).</remarks>
public interface IDocTemplateRenderer
{
    /// <summary>
    /// Sinh 1 tài liệu từ bộ mẫu + tham số khóa master.
    /// Sự kiện theo sau: gọi stored proc master/detail trên Data DB (đã check whitelist) → mail-merge →
    /// ghép fragment → trả bytes theo <paramref name="format"/>. KHÔNG ghi DB, KHÔNG phát event nghiệp vụ.
    /// </summary>
    /// <param name="templateId">Id bộ mẫu trong <c>Doc_Template</c>.</param>
    /// <param name="keyParams">Tham số khóa (VD { "NhanVien_Id": 42 }) — bind vào proc theo Doc_Template_Param.</param>
    /// <param name="format">Định dạng đầu ra (Docx | Pdf).</param>
    /// <param name="ct"></param>
    Task<DocRenderResult> RenderAsync(
        long templateId,
        IReadOnlyDictionary<string, object?> keyParams,
        DocOutputFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Như <see cref="RenderAsync"/> nhưng định danh bộ mẫu bằng <c>Ma</c> thay vì Id — dùng khi màn lưới
    /// gắn nút xuất qua <c>Ui_View_Action.Target = Ma</c> (mã ổn định/đọc được hơn Id sinh tự động).
    /// Sự kiện theo sau: tra Id theo mã (tenant hiện tại) rồi render; không tồn tại → ném InvalidOperationException.
    /// </summary>
    /// <param name="code">Mã bộ mẫu (<c>Doc_Template.Ma</c>).</param>
    /// <param name="keyParams">Tham số khóa (thường là cả dòng lưới đang chọn) — bind theo Doc_Template_Param.</param>
    /// <param name="format">Định dạng đầu ra (Docx | Pdf).</param>
    /// <param name="ct"></param>
    Task<DocRenderResult> RenderByCodeAsync(
        string code,
        IReadOnlyDictionary<string, object?> keyParams,
        DocOutputFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Khám phá danh sách biến (cột kết quả) của 1 stored proc — phục vụ màn soạn kéo biến.
    /// Sự kiện theo sau: chạy <c>sp_describe_first_result_set</c> (không side-effect) trên Data DB;
    /// proc phải nằm trong whitelist <c>Doc_Proc_Registry</c>.
    /// </summary>
    /// <param name="procName">Tên stored proc (đã đăng ký).</param>
    /// <param name="ct"></param>
    Task<IReadOnlyList<DocVariable>> DescribeVariablesAsync(
        string procName,
        CancellationToken ct = default);
}

/// <summary>Định dạng đầu ra của tài liệu sinh ra.</summary>
public enum DocOutputFormat
{
    /// <summary>Word OpenXML (.docx).</summary>
    Docx = 0,
    /// <summary>PDF (để in / xem cố định).</summary>
    Pdf = 1
}

/// <summary>Kết quả sinh tài liệu: bytes + content-type + tên file gợi ý.</summary>
public sealed record DocRenderResult(byte[] Bytes, string ContentType, string FileName);

/// <summary>Một biến (cột kết quả proc) hiển thị trên màn soạn.</summary>
public sealed record DocVariable(string Name, string DbType);
