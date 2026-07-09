// File    : DocxRenderEngine.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents — project DevExpress duy nhất)
// Purpose : Lõi DevExpress Office File API: mail-merge 1 fragment + ghép nhiều fragment (giữ page-setup
//           A4 dọc/ngang theo section) + xuất .docx/.pdf. Thuần định dạng — KHÔNG chạm DB.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §6, §14.

using System.Data;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents;

/// <summary>
/// Lõi render tài liệu bằng DevExpress <see cref="RichEditDocumentServer"/>.
/// Không phụ thuộc DB — nhận sẵn bytes template + dữ liệu (DataTable).
/// </summary>
public sealed class DocxRenderEngine
{
    /// <summary>
    /// Trộn (mail-merge) 1 fragment template với dữ liệu.
    /// Sự kiện theo sau: nạp fragment (.docx OpenXml) → bind <paramref name="data"/> vào MERGEFIELD →
    /// trả bytes .docx đã trộn. Master truyền DataTable 1 dòng; detail truyền N dòng.
    /// </summary>
    /// <param name="fragmentDocx">Bytes fragment .docx (đã soạn, có MERGEFIELD).</param>
    /// <param name="data">Nguồn dữ liệu — cột trùng tên MERGEFIELD.</param>
    public byte[] MailMergeFragment(byte[] fragmentDocx, DataTable data)
    {
        using var server = new RichEditDocumentServer();
        server.LoadDocument(fragmentDocx, DocumentFormat.OpenXml);

        var options = server.CreateMailMergeOptions();
        options.DataSource = data;
        // MergeMode mặc định giữ trong 1 tài liệu; page-setup (dọc/ngang) đã nằm trong template.

        using var outStream = new MemoryStream();
        server.MailMerge(options, outStream, DocumentFormat.OpenXml);
        return outStream.ToArray();
    }

    /// <summary>
    /// Nạp nguyên 1 bảng dữ liệu (nhiều cột × nhiều dòng) vào fragment detail: nạp shell (giữ hướng giấy +
    /// tiêu đề đã soạn) → chèn 1 bảng ở cuối gồm hàng tiêu đề (tên cột) + N hàng dữ liệu.
    /// Sự kiện theo sau: trả bytes .docx detail đã điền — dùng cho <see cref="Assemble"/>.
    /// </summary>
    /// <param name="fragmentShellDocx">Bytes fragment detail (khổ A4 ngang, có thể có tiêu đề).</param>
    /// <param name="data">Kết quả stored proc — mỗi cột 1 cột bảng, mỗi dòng 1 hàng.</param>
    public byte[] BuildDetailTable(byte[] fragmentShellDocx, DataTable data)
    {
        using var server = new RichEditDocumentServer();
        server.LoadDocument(fragmentShellDocx, DocumentFormat.OpenXml);
        var doc = server.Document;

        int cols = data.Columns.Count;
        if (cols > 0)
        {
            var table = doc.Tables.Create(doc.Range.End, data.Rows.Count + 1, cols);
            for (int c = 0; c < cols; c++)
                doc.InsertText(table[0, c].Range.Start, data.Columns[c].ColumnName);
            for (int r = 0; r < data.Rows.Count; r++)
                for (int c = 0; c < cols; c++)
                    doc.InsertText(table[r + 1, c].Range.Start, data.Rows[r][c]?.ToString() ?? "");
        }

        using var outStream = new MemoryStream();
        server.SaveDocument(outStream, DocumentFormat.OpenXml);
        return outStream.ToArray();
    }

    /// <summary>
    /// Ghép master + các detail (đã trộn) thành 1 tài liệu, giữ nguyên section/hướng giấy từng mảnh,
    /// rồi xuất theo định dạng yêu cầu.
    /// Sự kiện theo sau: nạp master → <c>AppendDocumentContent</c> lần lượt các detail → export .docx/.pdf.
    /// </summary>
    /// <param name="masterDocx">Bytes master đã trộn (A4 dọc).</param>
    /// <param name="detailDocxList">Danh sách bytes detail đã trộn (A4 ngang), theo thứ tự ghép.</param>
    /// <param name="format">Định dạng đầu ra.</param>
    public byte[] Assemble(byte[] masterDocx, IReadOnlyList<byte[]> detailDocxList, DocOutputFormat format)
    {
        using var result = new RichEditDocumentServer();
        result.LoadDocument(masterDocx, DocumentFormat.OpenXml);

        foreach (var detail in detailDocxList)
        {
            // Đọc hướng giấy/khổ của fragment (do template quy định — A4 ngang cho detail).
            var (landscape, width, height) = ReadPageSetup(detail);

            // Tạo SECTION mới (sang trang) để KHÔNG gộp vào section trước → giữ page-setup riêng.
            // AppendDocumentContent thuần sẽ gộp section, làm mất hướng giấy detail (đã kiểm chứng).
            var section = result.Document.AppendSection();
            section.StartType = SectionStartType.NextPage;
            section.Page.Landscape = landscape;
            section.Page.Width = width;
            section.Page.Height = height;

            using var ds = new MemoryStream(detail);
            result.Document.AppendDocumentContent(ds, DocumentFormat.OpenXml);
        }

        using var outStream = new MemoryStream();
        if (format == DocOutputFormat.Pdf)
            result.ExportToPdf(outStream);
        else
            result.SaveDocument(outStream, DocumentFormat.OpenXml);
        return outStream.ToArray();
    }

    /// <summary>
    /// Đọc hướng giấy + khổ (Width/Height) của section đầu 1 fragment .docx.
    /// Sự kiện theo sau: dùng để dựng section mới khi ghép, giữ đúng A4 dọc/ngang của template.
    /// </summary>
    private static (bool Landscape, float Width, float Height) ReadPageSetup(byte[] fragmentDocx)
    {
        using var s = new RichEditDocumentServer();
        s.LoadDocument(fragmentDocx, DocumentFormat.OpenXml);
        var page = s.Document.Sections[0].Page;
        return (page.Landscape, page.Width, page.Height);
    }
}
