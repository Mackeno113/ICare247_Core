// File    : ExportViewDataQuery.cs
// Module  : Views
// Layer   : Application
// Purpose : Query xuất TOÀN BỘ dữ liệu khớp lọc của View (Source_Type='Table'/'View') ra .xlsx/.csv —
//           WEB-UX-01 export: người dùng lọc ra bao nhiêu dòng thì xuất đúng bấy nhiêu, không chỉ 1 trang.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Views.Queries.ExportViewData;

/// <summary>Yêu cầu xuất dữ liệu View. <paramref name="Format"/>: "xlsx" (mặc định) hoặc "csv".</summary>
public sealed record ExportViewDataQuery(
    string ViewCode, int TenantId, string LangCode, string? Search, string Format = "xlsx"
) : IRequest<ViewExportFile?>;

/// <summary>File xuất + số dòng thực xuất/tổng số dòng khớp lọc (khác nhau nếu bị cắt bởi trần export).</summary>
public sealed record ViewExportFile(
    byte[] Bytes, string FileName, string ContentType, int ExportedCount, int TotalCount);

/// <summary>
/// Nạp metadata + toàn bộ dữ liệu khớp <c>Search</c> qua <see cref="IViewRepository.GetAllDataAsync"/>,
/// rồi dựng file theo <see cref="ExportViewDataQuery.Format"/>. Trả <c>null</c> nếu View không tồn tại
/// hoặc <see cref="ICare247.Domain.Entities.View.ViewMetadata.AllowExport"/>=false.
/// </summary>
public sealed class ExportViewDataQueryHandler : IRequestHandler<ExportViewDataQuery, ViewExportFile?>
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IConfigCache _configCache;
    private readonly IViewRepository _viewRepository;
    private readonly IViewExportBuilder _exportBuilder;

    public ExportViewDataQueryHandler(
        IConfigCache configCache, IViewRepository viewRepository, IViewExportBuilder exportBuilder)
    {
        _configCache = configCache;
        _viewRepository = viewRepository;
        _exportBuilder = exportBuilder;
    }

    public async Task<ViewExportFile?> Handle(ExportViewDataQuery request, CancellationToken ct)
    {
        var view = await _configCache.GetViewAsync(request.ViewCode, request.LangCode, request.TenantId, ct);
        if (view is null || !view.AllowExport)
            return null;

        var data = await _viewRepository.GetAllDataAsync(view, request.Search, ct);

        // Cột export = cột Data đang hiển thị + AllowExport=1, theo đúng thứ tự Order_No của lưới.
        var columns = view.Columns
            .Where(c => c.IsVisible && c.AllowExport
                     && string.Equals(c.ColumnKind, "Data", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(c.FieldName))
            .OrderBy(c => c.OrderNo)
            .Select(c => new ViewExportColumn(
                c.FieldName, c.ExportCaption ?? c.Caption ?? c.FieldName))
            .ToList();

        var baseName = string.IsNullOrWhiteSpace(view.ExportFileName) ? view.ViewCode : view.ExportFileName!;

        if (string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = BuildCsv(columns, data.Items);
            return new ViewExportFile(csvBytes, $"{baseName}.csv", "text/csv", data.Items.Count, data.TotalCount);
        }

        var xlsxBytes = _exportBuilder.Build(new ViewExportSpec(view.ViewCode, columns, data.Items));
        return new ViewExportFile(xlsxBytes, $"{baseName}.xlsx", XlsxContentType, data.Items.Count, data.TotalCount);
    }

    /// <summary>CSV UTF-8 (kèm BOM để Excel mở đúng tiếng Việt có dấu) — thuần string, không cần DevExpress.</summary>
    private static byte[] BuildCsv(
        IReadOnlyList<ViewExportColumn> columns, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Caption))));
        foreach (var row in rows)
        {
            var cells = columns.Select(c =>
                EscapeCsv(row.TryGetValue(c.FieldName, out var v) ? FormatCsvValue(v) : ""));
            sb.AppendLine(string.Join(",", cells));
        }
        return [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes(sb.ToString())];
    }

    private static string FormatCsvValue(object? v) => v switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
        bool b => b ? "1" : "0",
        _ => v.ToString() ?? ""
    };

    private static string EscapeCsv(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
}
