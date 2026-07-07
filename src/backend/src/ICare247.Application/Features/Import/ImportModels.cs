// File    : ImportModels.cs
// Module  : Import
// Layer   : Application
// Purpose : DTO kết quả import (preview + commit + template) + helper resolve thông báo lỗi i18n.
//           Spec 25 §11–§14, ADR-034.

using ICare247.Application.Interfaces;

namespace ICare247.Application.Features.Import;

/// <summary>File template import đã sinh (bytes .xlsx + tên tệp gợi ý).</summary>
public sealed record ImportTemplateFile(byte[] Content, string FileName);

/// <summary>Kết quả preview (dry-run) — thống kê + lỗi cấp file + từng dòng.</summary>
public sealed record ImportPreviewResult(
    ImportPreviewSummary Summary,
    IReadOnlyList<string> FileErrors,
    IReadOnlyList<ImportPreviewRow> Rows);

/// <summary>Thống kê mẻ (số dòng thêm/cập nhật/lỗi/bỏ qua).</summary>
public sealed record ImportPreviewSummary(int Total, int New, int Update, int Error, int Skipped);

/// <summary>1 dòng preview: số dòng Excel + trạng thái + Id trùng (nếu Update) + lỗi đã dịch.</summary>
public sealed record ImportPreviewRow(
    int RowNumber, string Operation, long? MatchedId, IReadOnlyList<ImportPreviewError> Errors);

/// <summary>1 lỗi ô (đã resolve i18n) — Field để tô đỏ.</summary>
public sealed record ImportPreviewError(string? Field, string Message);

/// <summary>Kết quả commit: phiên + trạng thái cuối + thống kê thực ghi + các dòng lỗi.</summary>
public sealed record ImportCommitResult(
    Guid SessionId, string Status,
    ImportPreviewSummary Summary,
    IReadOnlyList<string> FileErrors,
    IReadOnlyList<ImportPreviewRow> ErrorRows);

/// <summary>Resolve <see cref="ImportCellError"/> (key + args) → text i18n (server-side, ADR-014/029).</summary>
internal static class ImportMessageResolver
{
    /// <summary>Lấy template theo key rồi thay token {0}/{1}… bằng args; fallback = chính key. Không phát sự kiện.</summary>
    public static async Task<string> ResolveAsync(
        IConfigCache config, ImportCellError e, string lang, int tenantId, CancellationToken ct)
    {
        var template = await config.ResolveKeyAsync(e.ErrorKey, lang, tenantId, ct) ?? e.ErrorKey;
        for (var i = 0; i < e.Args.Count; i++)
            template = template.Replace("{" + i + "}", e.Args[i]);
        return template;
    }

    /// <summary>Dịch danh sách lỗi của 1 dòng → <see cref="ImportPreviewError"/>.</summary>
    public static async Task<List<ImportPreviewError>> ResolveRowAsync(
        IConfigCache config, IReadOnlyList<ImportCellError> errors, string lang, int tenantId, CancellationToken ct)
    {
        var list = new List<ImportPreviewError>(errors.Count);
        foreach (var e in errors)
            list.Add(new ImportPreviewError(e.FieldName, await ResolveAsync(config, e, lang, tenantId, ct)));
        return list;
    }
}
