// File    : ScreenScaffolder.cs
// Module  : Infrastructure
// Layer   : Infrastructure
// Purpose : Hiện thực IScreenScaffolder — sinh nhanh (headless) Form / Lưới từ 1 bảng Sys_Table.
//           Đọc cột thật từ Target DB (bỏ PK/Identity), tự tạo Sys_Column nếu thiếu, rồi ghi
//           Ui_Form/Ui_Field hoặc Ui_View/Ui_View_Column vào Config DB. Tái dùng đúng các data
//           service sẵn có (không truy vấn Dapper trực tiếp). Logic map cột→field song song với
//           luồng "Tạo Fields tự động" trong FormEditor để giữ hành vi nhất quán.

using System.Text.RegularExpressions;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Core.Services;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Sinh nhanh màn hình từ metadata bảng. Form và Lưới là 2 hành động độc lập (1-chạm mỗi cái).
/// </summary>
public sealed class ScreenScaffolder : IScreenScaffolder
{
    private readonly IAppConfigService _appConfig;
    private readonly ISchemaInspectorService _schemaInspector;
    private readonly IFormDataService _formData;
    private readonly IFormDetailDataService _formDetail;
    private readonly IFieldDataService _fieldData;
    private readonly IViewDataService _viewData;
    private readonly IRelationDataService _relationData;
    private readonly II18nDataService _i18n;
    private readonly IAppLogger? _logger;

    /// <summary>Khối cột audit hệ thống — KHÔNG sinh field/cột (người dùng không cấu hình). Xem AuditColumnTemplate.</summary>
    private static readonly HashSet<string> AuditColumns =
        new(AuditColumnTemplate.RequiredColumns, StringComparer.OrdinalIgnoreCase);

    /// <summary>Khởi tạo scaffolder với đầy đủ data service phụ thuộc (DI).</summary>
    public ScreenScaffolder(
        IAppConfigService appConfig,
        ISchemaInspectorService schemaInspector,
        IFormDataService formData,
        IFormDetailDataService formDetail,
        IFieldDataService fieldData,
        IViewDataService viewData,
        IRelationDataService relationData,
        II18nDataService i18n,
        IAppLogger? logger = null)
    {
        _appConfig = appConfig;
        _schemaInspector = schemaInspector;
        _formData = formData;
        _formDetail = formDetail;
        _fieldData = fieldData;
        _viewData = viewData;
        _relationData = relationData;
        _i18n = i18n;
        _logger = logger;
    }

    /// <summary>
    /// Sinh 1 Ui_Form (1 section) + toàn bộ Ui_Field từ cột bảng.
    /// Sự kiện theo sau: Ui_Form/Ui_Section/Ui_Field (và Sys_Column, Sys_Resource) được ghi vào Config DB;
    /// form sẵn sàng render ở runtime (cần đẩy ConfigSync sang tenant nếu là môi trường master).
    /// </summary>
    public async Task<ScaffoldResult> GenerateFormAsync(
        int tableId, string schemaName, string tableCode, string tableName,
        int tenantId, CancellationToken ct = default)
    {
        try
        {
            var (ok, columns, err) = await ReadTargetColumnsAsync(schemaName, tableCode, ct);
            if (!ok) return new ScaffoldResult(false, 0, 0, err);

            // Bỏ PK/Identity + khối cột audit — không đưa vào form nhập liệu.
            var usable = BusinessColumns(columns);
            if (usable.Count == 0)
                return new ScaffoldResult(false, 0, 0,
                    $"Bảng [{schemaName}].[{tableCode}] không có cột nghiệp vụ nào (chỉ PK/Identity/cột audit).");

            var lower = tableCode.ToLowerInvariant();

            // 1. Tạo Ui_Form (Form_Code = Table_Code, Platform web, DisplayMode Popup).
            var formId = await _formData.CreateFormAsync(
                tableCode, tableName, "web", tenantId, tableId, "Popup", ct);
            if (formId <= 0)
                return new ScaffoldResult(false, 0, 0, "Không tạo được Ui_Form (Form_Id không hợp lệ).");

            // 2. Tạo 1 section mặc định "Thông tin chung".
            var sectionTitleKey = $"{lower}.section.general.title";
            var sectionId = await _formDetail.UpsertSectionAsync(
                new SectionUpsertRequest(
                    FormId:      formId,
                    SectionId:   0,
                    SectionCode: $"sec_{lower}_1",
                    TitleKey:    sectionTitleKey,
                    OrderNo:     1,
                    IsActive:    true,
                    OldTitleKey: ""),
                ct);
            await SafeSaveResourceAsync(sectionTitleKey, "Thông tin chung", ct);

            // 3. Nạp Sys_Relation của bảng con này để map cột FK → LookupBox dynamic (nguồn quan hệ DUY NHẤT).
            var relsByFkColumn = await LoadFkRelationsAsync(tableId, tenantId, ct);

            // 4. Sinh field cho từng cột.
            var order = 1;
            var created = 0;
            foreach (var col in usable)
            {
                var columnId = await _fieldData.EnsureColumnExistsAsync(tableId, col, ct);
                if (columnId <= 0) continue; // không xác định được Column_Id → bỏ qua, tránh FK violation

                var labelKey = $"{lower}.field.{col.ColumnName.ToLowerInvariant()}";

                FieldLookupConfigRecord? lookupConfig = null;
                if (relsByFkColumn.TryGetValue(col.ColumnName, out var rel))
                {
                    lookupConfig = new FieldLookupConfigRecord
                    {
                        QueryMode     = "table",
                        SourceName    = rel.MasterTableCode,
                        ValueColumn   = FirstNonEmpty(rel.ValueColumn, rel.MasterKeyColumn, "Id"),
                        DisplayColumn = FirstNonEmpty(rel.DisplayColumn, "Ten"),
                        SearchEnabled = true,
                    };
                }
                var isFk = lookupConfig is not null;

                var record = new FieldConfigRecord
                {
                    FieldId      = 0,
                    FormId       = formId,
                    SectionId    = sectionId > 0 ? sectionId : null,
                    ColumnId     = columnId,
                    ColumnCode   = col.ColumnName,
                    EditorType   = isFk ? "LookupBox" : col.DefaultEditorType,
                    LabelKey     = labelKey,
                    IsVisible    = true,
                    IsReadOnly   = false,
                    IsRequired   = !col.IsNullable,
                    OrderNo      = order++,
                    LookupSource = isFk ? "dynamic" : null, // dynamic → Lookup_Code NULL (khớp CHK_Ui_Field_LookupConsistency)
                };

                var fieldId = await _fieldData.SaveFieldAsync(record, tenantId, lookupConfig, ct: ct);
                if (fieldId > 0)
                {
                    await SafeSaveResourceAsync(labelKey, SplitPascalCase(col.ColumnName), ct);
                    created++;
                }
            }

            return new ScaffoldResult(true, formId, created,
                $"Đã sinh Form '{tableCode}' với {created} field (bỏ qua PK/Identity). " +
                "Chỉnh control/nhãn tại màn Cấu hình Field nếu cần.");
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ScreenScaffolder.GenerateForm");
            return new ScaffoldResult(false, 0, 0, $"Lỗi sinh Form: {ex.Message}");
        }
    }

    /// <summary>
    /// Sinh 1 Ui_View (Grid) + toàn bộ Ui_View_Column từ cột bảng.
    /// Sự kiện theo sau: Ui_View/Ui_View_Column (và Sys_Column, Sys_Resource cột) được ghi vào Config DB;
    /// lưới danh sách sẵn sàng render ở runtime.
    /// </summary>
    public async Task<ScaffoldResult> GenerateGridAsync(
        int tableId, string schemaName, string tableCode, string tableName,
        int tenantId, CancellationToken ct = default)
    {
        try
        {
            var (ok, columns, err) = await ReadTargetColumnsAsync(schemaName, tableCode, ct);
            if (!ok) return new ScaffoldResult(false, 0, 0, err);

            var usable = BusinessColumns(columns);
            if (usable.Count == 0)
                return new ScaffoldResult(false, 0, 0,
                    $"Bảng [{schemaName}].[{tableCode}] không có cột nghiệp vụ nào để dựng lưới.");

            var lower = tableCode.ToLowerInvariant();
            var titleKey = $"{lower}.view.title";

            var order = 0;
            var viewColumns = new List<ViewColumnRecord>();
            foreach (var col in usable)
            {
                // EnsureColumnExists để cột lưới gắn Column_Id thật (nguồn Table). Idempotent.
                var columnId = await _fieldData.EnsureColumnExistsAsync(tableId, col, ct);
                var captionKey = $"{lower}.field.{col.ColumnName.ToLowerInvariant()}";
                await SafeSaveResourceAsync(captionKey, SplitPascalCase(col.ColumnName), ct);

                viewColumns.Add(new ViewColumnRecord
                {
                    ColumnId    = columnId > 0 ? columnId : (int?)null,
                    FieldName   = col.ColumnName,
                    CaptionKey  = captionKey,
                    ColumnKind  = "Data",
                    RenderMode  = "Text",
                    IsVisible   = true,
                    OrderNo     = order++,
                    AllowSort   = true,
                    AllowFilter = true,
                    IsActive    = true,
                });
            }

            var request = new ViewUpsertRequest
            {
                ViewId            = 0,
                ViewCode          = tableCode,
                ViewType          = "Grid",
                TableId           = tableId,
                SourceType        = "Table",
                TitleKey          = titleKey,
                ShowFilterRow     = true,
                ShowColumnChooser = true,
                ShowSearchBox     = true,
                AllowAdd          = true,
                AllowEdit         = true,
                AllowDelete       = true,
                AllowExport       = true,
                IsActive          = true,
                Columns           = viewColumns,
            };

            var viewId = await _viewData.SaveViewAsync(request, tenantId, ct);
            if (viewId <= 0)
                return new ScaffoldResult(false, 0, 0, "Không tạo được Ui_View (View_Id không hợp lệ).");

            await SafeSaveResourceAsync(titleKey, tableName, ct);

            return new ScaffoldResult(true, viewId, viewColumns.Count,
                $"Đã sinh Lưới '{tableCode}' với {viewColumns.Count} cột. " +
                "Gắn Form Thêm/Sửa và tinh chỉnh cột tại màn Quản lý View nếu cần.");
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ScreenScaffolder.GenerateGrid");
            return new ScaffoldResult(false, 0, 0, $"Lỗi sinh Lưới: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra bảng đã có Ui_View chưa (Table_Id hoặc View_Code = Table_Code, chỉ tính active).
    /// Lỗi truy vấn → coi như CHƯA có (false) để không chặn nhầm người dùng.
    /// </summary>
    public async Task<bool> ViewExistsForTableAsync(
        int tableId, string tableCode, int tenantId, CancellationToken ct = default)
    {
        try
        {
            var views = await _viewData.GetViewsAsync(tenantId, includeInactive: false, ct);
            return views.Any(v =>
                v.TableId == tableId ||
                string.Equals(v.ViewCode, tableCode, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ScreenScaffolder.ViewExists");
            return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Đọc cột thật của bảng từ Target DB. Trả (false, [], message) nếu chưa cấu hình Target DB
    /// hoặc bảng không tồn tại / không có cột.
    /// </summary>
    private async Task<(bool Ok, IReadOnlyList<ColumnSchemaDto> Columns, string Error)>
        ReadTargetColumnsAsync(string schemaName, string tableCode, CancellationToken ct)
    {
        if (!_appConfig.IsConfigured) await _appConfig.LoadAsync();

        if (!_appConfig.IsTargetConfigured || string.IsNullOrWhiteSpace(_appConfig.TargetConnectionString))
            return (false, [], "Chưa cấu hình Target Database. Vào Settings → Target Database để nhập connection string.");

        var schema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
        var columns = await _schemaInspector.GetColumnsAsync(
            _appConfig.TargetConnectionString, schema, tableCode, ct);

        if (columns.Count == 0)
            return (false, [],
                $"Không đọc được cột của [{schema}].[{tableCode}] — bảng không tồn tại trong Target DB " +
                "hoặc Table_Code là mã logic (không phải bảng vật lý).");

        return (true, columns, "");
    }

    /// <summary>
    /// Nạp Sys_Relation của bảng con → map theo cột FK (Detail_FK_Column) để field FK ra LookupBox.
    /// Lỗi nạp → trả dict rỗng (không có FK là chấp nhận được, field vẫn ra editor theo kiểu SQL).
    /// </summary>
    private async Task<Dictionary<string, RelationRecord>> LoadFkRelationsAsync(
        int tableId, int tenantId, CancellationToken ct)
    {
        var map = new Dictionary<string, RelationRecord>(StringComparer.OrdinalIgnoreCase);
        if (tableId <= 0) return map;
        try
        {
            var rels = await _relationData.GetRelationsAsync(tenantId, includeInactive: false, ct);
            foreach (var r in rels)
                if (r.DetailTableId == tableId && !string.IsNullOrWhiteSpace(r.DetailFkColumn))
                    map[r.DetailFkColumn!] = r;
        }
        catch (Exception ex)
        {
            _logger?.Capture(ex, "ScreenScaffolder.LoadFkRelations");
        }
        return map;
    }

    /// <summary>Ghi Sys_Resource (vi) an toàn — nuốt lỗi để không chặn cả luồng sinh vì 1 key.</summary>
    private async Task SafeSaveResourceAsync(string key, string value, CancellationToken ct)
    {
        try { await _i18n.SaveResourceAsync(key, "vi", value, ct); }
        catch (Exception ex) { _logger?.Capture(ex, "ScreenScaffolder.SaveResource"); }
    }

    /// <summary>
    /// Lọc còn cột nghiệp vụ để sinh field/cột: bỏ PK/Identity và khối cột audit hệ thống
    /// (CreatedBy/CreatedAt/UpdatedBy/UpdatedAt/IsDeleted/Ver), sắp theo thứ tự cột gốc.
    /// </summary>
    private static List<ColumnSchemaDto> BusinessColumns(IReadOnlyList<ColumnSchemaDto> columns)
        => columns.Where(c => !c.ShouldSkip && !AuditColumns.Contains(c.ColumnName))
                  .OrderBy(c => c.OrdinalPosition)
                  .ToList();

    /// <summary>Trả giá trị không rỗng đầu tiên (fallback theo thứ tự cho Value/Display column lookup).</summary>
    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

    /// <summary>Tách PascalCase thành các từ có khoảng cách: "MaNhanVien" → "Ma Nhan Vien".</summary>
    private static string SplitPascalCase(string input)
        => string.IsNullOrEmpty(input)
            ? input
            : Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
}
