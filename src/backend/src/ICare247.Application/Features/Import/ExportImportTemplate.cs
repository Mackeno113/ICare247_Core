// File    : ExportImportTemplate.cs
// Module  : Import
// Layer   : Application
// Purpose : Query xuất template import (.xlsx) cho 1 View — sheet chính + sheet phụ FK + dropdown.
//           Spec 25 §7/§11, ADR-034.

using ICare247.Application.Interfaces;
using MediatR;

namespace ICare247.Application.Features.Import;

/// <summary>Yêu cầu sinh workbook template import cho View. Trả null nếu View/Edit_Form không tồn tại.</summary>
public sealed record ExportImportTemplateQuery(string ViewCode, int TenantId, string LangCode)
    : IRequest<ImportTemplateFile?>;

/// <summary>Dựng template: cột nhập từ metadata + mỗi FK 1 sheet phụ {Mã,Tên} (lọc quyền) + dropdown.</summary>
public sealed class ExportImportTemplateQueryHandler
    : IRequestHandler<ExportImportTemplateQuery, ImportTemplateFile?>
{
    private readonly IImportMetadataProvider _meta;
    private readonly IFkLookupResolver _fk;
    private readonly IImportTemplateBuilder _builder;

    public ExportImportTemplateQueryHandler(
        IImportMetadataProvider meta, IFkLookupResolver fk, IImportTemplateBuilder builder)
    {
        _meta = meta;
        _fk = fk;
        _builder = builder;
    }

    /// <summary>Sinh template → trả bytes .xlsx. Sự kiện theo sau: controller trả File cho client tải.</summary>
    public async Task<ImportTemplateFile?> Handle(ExportImportTemplateQuery r, CancellationToken ct)
    {
        var ctx = await _meta.BuildAsync(r.ViewCode, r.TenantId, r.LangCode, ct);
        if (ctx is null)
            return null;

        // Định nghĩa FK theo cột (để cột nào là FK → sinh sheet phụ + dropdown).
        var fkDefs = (await _fk.GetFkColumnsAsync(ctx.View, ct))
            .ToDictionary(d => d.FieldName, StringComparer.OrdinalIgnoreCase);

        var columns = new List<ImportTemplateColumn>(ctx.Fields.Count);
        foreach (var f in ctx.Fields)
        {
            FkTemplateSource? fk = null;
            if (fkDefs.TryGetValue(f.FieldName, out var def))
            {
                var map = await _fk.BuildCodeMapAsync(def, ct);
                if (map.HasCodeField && map.Items.Count > 0)
                    fk = new FkTemplateSource(
                        map.Items.Select(i => new FkTemplateItem(i.Code, i.Display)).ToList());
            }
            columns.Add(new ImportTemplateColumn(
                f.FieldName, f.Caption, f.Required, FriendlyType(f.NetType, fk is not null), fk));
        }

        var bytes = _builder.Build(new ImportTemplateSpec(ctx.SheetName, columns));
        var fileName = $"{ctx.View.ViewCode}_import_template.xlsx";
        return new ImportTemplateFile(bytes, fileName);
    }

    /// <summary>Gợi ý kiểu dữ liệu (tiếng Việt) cho ô ghi chú tiêu đề; cột FK ưu tiên nhãn "Mã".</summary>
    private static string FriendlyType(string netType, bool isFk) => isFk
        ? "Mã"
        : netType.ToLowerInvariant() switch
        {
            "int" or "int32" or "long" or "int64" or "short" or "int16" or "byte" => "số nguyên",
            "decimal" or "double" or "float" or "single" or "money" => "số",
            "bool" or "boolean" or "bit" => "đúng/sai (1/0)",
            "datetime" or "date" => "ngày (dd/MM/yyyy)",
            _ => "văn bản"
        };
}
