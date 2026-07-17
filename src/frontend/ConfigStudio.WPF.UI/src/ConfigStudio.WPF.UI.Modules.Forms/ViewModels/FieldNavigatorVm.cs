// File    : FieldNavigatorVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : REFACTOR-B2 — VM con vùng "Field Navigator" (panel trái màn Cấu hình Field), tách
//           nguyên trạng từ FieldConfigViewModel: cây field theo section, badge trạng thái,
//           di chuyển ↑↓, bulk multi-select + chuyển section, nhóm "cột chưa tạo field".
//           Giao tiếp với root: Func<Context> (chụp ngữ cảnh FieldId/FormId/mã form/section),
//           Func<CancellationToken> (token theo vòng đời navigation của root), Action onLoaded
//           (root soát cascade sau khi list sẵn sàng). Hành vi giữ NGUYÊN.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Data;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>VM con panel Field Navigator — root expose qua property <c>Navigator</c>.</summary>
public sealed class FieldNavigatorVm : BindableBase
{
    /// <summary>Snapshot ngữ cảnh từ root VM tại thời điểm thao tác (root state đổi theo navigation).</summary>
    public sealed record Context(
        int FieldId, int FormId, string TableCode, string FormCode, string FormName,
        IReadOnlyList<SectionOptionItem> AvailableSections);

    private readonly IFormDetailDataService? _formDetailService;
    private readonly IFieldDataService? _fieldService;
    private readonly II18nDataService? _i18nService;
    private readonly IAppConfigService? _appConfig;
    private readonly IAppLogger? _logger;
    private readonly IRegionManager _regionManager;
    private readonly Func<Context> _context;
    private readonly Func<CancellationToken> _token;
    private readonly Action _onLoaded;

    /// <summary>FormId đã load vào Groups — tránh reload khi chỉ đổi field trong cùng form.</summary>
    public int LoadedFormId { get; private set; } = -1;

    public ObservableCollection<FieldNavGroup> Groups { get; } = [];
    public ObservableCollection<FieldNavItem> BulkSelectedFields { get; } = [];

    /// <summary>Section đích cho menu "Chuyển N field…". Rebuild qua <see cref="RefreshMoveTargets"/> mỗi khi mở menu.</summary>
    public ObservableCollection<FieldMoveTargetItem> MoveTargets { get; } = [];

    public bool CanMoveBulk => BulkSelectedFields.Count >= 1;

    public string BulkMoveHeader => BulkSelectedFields.Count > 0
        ? $"Chuyển {BulkSelectedFields.Count} field đã chọn sang…"
        : "Chưa tick field nào để chuyển";

    public DelegateCommand<FieldNavItem> NavigateToFieldCommand { get; }
    public DelegateCommand RefreshNavigatorCommand { get; }
    public DelegateCommand<FieldNavItem> MoveFieldUpCommand { get; }
    public DelegateCommand<FieldNavItem> MoveFieldDownCommand { get; }
    public DelegateCommand<FieldNavItem?> ToggleBulkSelectionCommand { get; }
    public DelegateCommand<FieldMoveTargetItem?> MoveBulkToSectionCommand { get; }

    public FieldNavigatorVm(
        IFormDetailDataService? formDetailService,
        IFieldDataService? fieldService,
        II18nDataService? i18nService,
        IAppConfigService? appConfig,
        IAppLogger? logger,
        IRegionManager regionManager,
        Func<Context> context,
        Func<CancellationToken> token,
        Action onLoaded)
    {
        _formDetailService = formDetailService;
        _fieldService = fieldService;
        _i18nService = i18nService;
        _appConfig = appConfig;
        _logger = logger;
        _regionManager = regionManager;
        _context = context;
        _token = token;
        _onLoaded = onLoaded;

        NavigateToFieldCommand = new DelegateCommand<FieldNavItem>(NavigateToField);
        RefreshNavigatorCommand = new DelegateCommand(async () => await LoadAsync(_token()));
        MoveFieldUpCommand = new DelegateCommand<FieldNavItem>(async item => await ExecuteMoveFieldAsync(item, -1));
        MoveFieldDownCommand = new DelegateCommand<FieldNavItem>(async item => await ExecuteMoveFieldAsync(item, +1));
        ToggleBulkSelectionCommand = new DelegateCommand<FieldNavItem?>(ExecuteToggleBulkSelection);
        MoveBulkToSectionCommand = new DelegateCommand<FieldMoveTargetItem?>(
            async t => await ExecuteMoveBulkToSectionAsync(t),
            _ => CanMoveBulk);
        BulkSelectedFields.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(CanMoveBulk));
            RaisePropertyChanged(nameof(BulkMoveHeader));
            MoveBulkToSectionCommand.RaiseCanExecuteChanged();
        };
    }

    /// <summary>
    /// Load danh sách field của form (grouped by section) cho Left Panel Navigator.
    /// Lỗi bị bỏ qua — navigator là tính năng phụ trợ, không ảnh hưởng main flow.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct)
    {
        if (_formDetailService is null || _appConfig is not { IsConfigured: true }) return;

        var ctx = _context();
        try
        {
            var tenantId     = _appConfig.TenantId;
            var sectionsTask = _formDetailService.GetSectionsByFormAsync(ctx.FormId, tenantId, ct);
            var fieldsTask   = _formDetailService.GetFieldsByFormAsync(ctx.FormId, tenantId, ct);
            await Task.WhenAll(sectionsTask, fieldsTask);

            var sections = sectionsTask.Result;
            var fields   = fieldsTask.Result;

            // Group fields by SectionCode
            var fieldsBySec = fields
                .GroupBy(f => f.SectionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.OrderNo).ToList(),
                              StringComparer.OrdinalIgnoreCase);

            ClearBulkSelection();   // item cũ sắp bị thay thế → tránh giữ reference stale
            Groups.Clear();
            // Pass 1 (đồng bộ): dựng group/item với mã → list hiện ngay. Ghi TitleKey/LabelKey để
            // pass 2 resolve tên (vi) và cập nhật qua INotifyPropertyChanged (không chặn hiển thị).
            var sectionTitleKeys = new List<(FieldNavGroup Group, string? TitleKey)>();
            foreach (var sec in sections.OrderBy(s => s.OrderNo))
            {
                var group = new FieldNavGroup { SectionId = sec.SectionId, SectionCode = sec.SectionCode };
                if (fieldsBySec.TryGetValue(sec.SectionCode, out var secFields))
                {
                    foreach (var f in secFields)
                        group.Fields.Add(new FieldNavItem
                        {
                            FieldId        = f.FieldId,
                            SortOrder      = f.OrderNo,
                            ColumnCode     = f.ColumnCode,
                            FieldCode      = f.FieldCode,
                            EditorType     = f.EditorType,
                            IsVirtual      = f.IsVirtual,
                            LabelKey       = f.LabelKey,
                            IsCurrentField = f.FieldId == ctx.FieldId,
                            // Đã cấu hình = cờ Ui_Field.Is_Configured (bật khi user bấm Lưu Field).
                            Status         = f.IsConfigured
                                             ? FieldNavStatus.Configured
                                             : FieldNavStatus.Incomplete
                        });
                }
                if (group.Fields.Count > 0)
                {
                    Groups.Add(group);
                    sectionTitleKeys.Add((group, sec.TitleKey));
                }
            }

            // ── Cột chưa tạo field: có trong Sys_Column nhưng chưa có Ui_Field ──
            // Gộp vào 1 nhóm riêng ở cuối để user biết cột nào còn "chỉ mới tạo cột".
            await AppendUnconfiguredColumnsAsync(ctx.FormId, fields, tenantId, ct);

            // Pass 2: resolve tên section + field ra tiếng Việt (fallback mã khi chưa có bản dịch).
            await ResolveNamesAsync(sectionTitleKeys, ct);

            LoadedFormId = ctx.FormId;
            _onLoaded();   // root soát cascade (P2/P3) khi đã có field list
        }
        catch (OperationCanceledException) { /* bỏ qua */ }
        catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.LoadFieldNavigator"); }
    }

    /// <summary>
    /// Đồng bộ item navigator cho field vừa Lưu — navigator chỉ nạp lại khi ĐỔI form nên phải cập
    /// nhật tại chỗ: badge theo cờ <c>Ui_Field.Is_Configured</c> vừa ghi, và tên hiển thị theo nhãn
    /// (vi) vừa lưu vào <c>Sys_Resource</c>. Không tìm thấy item → bỏ qua, lần nạp sau lấy đúng từ DB.
    /// Sự kiện theo sau: badge đổi sang "đã cấu hình" + tên đổi sang nhãn tiếng Việt, không query lại.
    /// </summary>
    public void SyncItemAfterSave(int fieldId, string labelKey, string? labelVi)
    {
        var item = Groups
            .SelectMany(g => g.Fields)
            .FirstOrDefault(f => f.FieldId == fieldId);

        if (item is null) return;

        item.Status   = FieldNavStatus.Configured;
        item.LabelKey = labelKey;

        var label = (labelVi ?? "").Trim();
        if (!string.IsNullOrEmpty(label))
            item.DisplayName = label;   // rỗng → giữ nguyên, Title tự fallback về mã cột
    }

    /// <summary>Cập nhật IsCurrentField cho tất cả item — không reload list.</summary>
    public void UpdateSelection(int currentFieldId)
    {
        foreach (var group in Groups)
            foreach (var item in group.Fields)
                item.IsCurrentField = item.FieldId == currentFieldId;
    }

    /// <summary>
    /// Thêm nhóm "Cột chưa tạo field": các cột trong <c>Sys_Column</c> (bỏ cột khóa chính) chưa được
    /// map vào bất kỳ <c>Ui_Field</c> nào của form. Sự kiện theo sau: click item → mở tạo field mới.
    /// </summary>
    private async Task AppendUnconfiguredColumnsAsync(
        int formId, IReadOnlyList<FieldDetailRecord> fields, int tenantId, CancellationToken ct)
    {
        if (_fieldService is null) return;

        var tableId = await _fieldService.GetTableIdByFormAsync(formId, tenantId, ct);
        if (tableId <= 0) return;

        var columns = await _fieldService.GetColumnsByTableAsync(tableId, ct);
        if (columns.Count == 0) return;

        // Cột đã được map vào field (non-virtual) → loại khỏi danh sách "chưa tạo field".
        var mappedCodes = fields
            .Where(f => !f.IsVirtual && !string.IsNullOrWhiteSpace(f.ColumnCode))
            .Select(f => f.ColumnCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Cột hệ thống English (khóa chính + khối audit chuẩn) — không phải field nghiệp vụ → ẩn.
        var systemCodes = new HashSet<string>(
            Core.Services.AuditColumnTemplate.RequiredColumns, StringComparer.OrdinalIgnoreCase)
            { "Id" };

        var group = new FieldNavGroup { SectionId = 0, SectionCode = "CHƯA TẠO FIELD" };
        foreach (var c in columns)
        {
            // Bỏ cột khóa chính + cột hệ thống + cột đã map — chỉ hiện cột nghiệp vụ chưa tạo field.
            if (c.IsPk || systemCodes.Contains(c.ColumnCode) || mappedCodes.Contains(c.ColumnCode))
                continue;

            group.Fields.Add(new FieldNavItem
            {
                FieldId    = 0,
                ColumnCode = c.ColumnCode,
                EditorType = c.DataType,
                Status     = FieldNavStatus.ColumnOnly
            });
        }

        if (group.Fields.Count > 0)
            Groups.Add(group);
    }

    // Pass 2 của navigator: resolve tên section (TitleKey) + tên field (LabelKey) ra tiếng Việt.
    // Chạy tuần tự (giống FormEditor) sau khi list đã hiện mã → tên "điền dần" qua INotifyPropertyChanged.
    // Cột chưa tạo field (LabelKey rỗng) giữ nguyên mã.
    private async Task ResolveNamesAsync(
        IReadOnlyList<(FieldNavGroup Group, string? TitleKey)> sectionTitleKeys, CancellationToken ct)
    {
        if (_i18nService is null || _appConfig is not { IsConfigured: true }) return;

        foreach (var (group, titleKey) in sectionTitleKeys)
            group.SectionName = await ResolveViAsync(titleKey, ct);

        foreach (var group in Groups)
            foreach (var item in group.Fields)
                if (!string.IsNullOrEmpty(item.LabelKey))
                    item.DisplayName = await ResolveViAsync(item.LabelKey, ct);
    }

    /// <summary>Resolve 1 resource key sang tiếng Việt; rỗng/không có bản dịch → chuỗi rỗng (fallback mã).</summary>
    private async Task<string> ResolveViAsync(string? key, CancellationToken ct)
    {
        if (_i18nService is null || string.IsNullOrEmpty(key) || _appConfig is not { IsConfigured: true })
            return "";
        try { return await _i18nService.ResolveKeyAsync(key, "vi", ct) ?? ""; }
        catch (OperationCanceledException) { return ""; }
        catch (Exception ex) { _logger?.Capture(ex, $"FieldConfig.ResolveVi {key}"); return ""; }
    }

    /// <summary>Click item navigator: cột chưa tạo field → mở mode "new" chọn sẵn cột; field → navigate edit.</summary>
    public void NavigateToField(FieldNavItem? item)
    {
        if (item is null) return;
        var ctx = _context();

        // Cột chưa tạo field → mở chế độ "new" với cột đã chọn sẵn, nối cuối danh sách.
        if (item.Status == FieldNavStatus.ColumnOnly)
        {
            var firstSectionId = Groups.FirstOrDefault(g => g.SectionId > 0)?.SectionId ?? 0;
            var appendOrder = Groups
                .SelectMany(g => g.Fields).Where(f => f.FieldId > 0)
                .Select(f => f.SortOrder).DefaultIfEmpty(0).Max() + 1;

            var pNew = new NavigationParameters
            {
                { "fieldId",    0 },
                { "formId",     ctx.FormId },
                { "sectionId",  firstSectionId },
                { "orderNo",    appendOrder },
                { "columnCode", item.ColumnCode },
                { "tableCode",  ctx.TableCode },
                { "formCode",   ctx.FormCode },
                { "formName",   ctx.FormName },
                { "mode",       "new" }
            };
            _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, pNew);
            return;
        }

        if (item.FieldId == ctx.FieldId) return;

        // Tìm section chứa field này để truyền đúng sectionId → dropdown Section không bị mất khi navigate
        var sectionId = Groups
            .FirstOrDefault(g => g.Fields.Any(f => f.FieldId == item.FieldId))
            ?.SectionId ?? 0;

        var p = new NavigationParameters
        {
            { "fieldId",   item.FieldId },
            { "formId",    ctx.FormId },
            { "sectionId", sectionId },
            { "tableCode", ctx.TableCode },
            { "formCode",  ctx.FormCode },
            { "formName",  ctx.FormName },
            { "mode",      "edit" }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.FieldConfig, p);
    }

    /// <summary>
    /// Di chuyển <paramref name="item"/> lên (direction=-1) hoặc xuống (+1) trong group chứa nó,
    /// rồi persist Order_No (1, 3, 5...).
    /// </summary>
    private async Task ExecuteMoveFieldAsync(FieldNavItem? item, int direction)
    {
        if (item is null) return;

        var group = Groups.FirstOrDefault(g => g.Fields.Contains(item));
        if (group is null) return;

        var idx    = group.Fields.IndexOf(item);
        var newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= group.Fields.Count) return;

        group.Fields.Move(idx, newIdx);

        // Gán lại Order_No: bắt đầu 1, bước +2 → 1, 3, 5, 7...
        var orderItems = new List<(int FieldId, int OrderNo)>();
        for (var i = 0; i < group.Fields.Count; i++)
        {
            group.Fields[i].SortOrder = 1 + i * 2;
            orderItems.Add((group.Fields[i].FieldId, group.Fields[i].SortOrder));
        }

        if (_fieldService is not null)
        {
            try { await _fieldService.UpdateFieldOrderAsync(orderItems, _token()); }
            catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.PersistFieldOrder"); }
        }
    }

    // ── Bulk multi-select + chuyển sang Section khác (context-menu) ──
    // Song song FormEditor: tick checkbox trên field → gom vào BulkSelectedFields; right-click
    // navigator → menu "Chuyển N field đã chọn sang…". Nhóm "CHƯA TẠO FIELD" (FieldId=0) không
    // được tick (không thể chuyển field chưa tồn tại).

    /// <summary>Toggle 1 field vào/khỏi bulk selection theo trạng thái IsMultiChecked của nó.</summary>
    private void ExecuteToggleBulkSelection(FieldNavItem? item)
    {
        if (item is null || item.IsColumnOnly || item.FieldId <= 0) return;

        if (item.IsMultiChecked)
        {
            if (!BulkSelectedFields.Contains(item))
                BulkSelectedFields.Add(item);
        }
        else
        {
            BulkSelectedFields.Remove(item);
        }
    }

    /// <summary>Bỏ tick toàn bộ + xóa khỏi BulkSelectedFields.</summary>
    public void ClearBulkSelection()
    {
        foreach (var f in BulkSelectedFields.ToList())
            f.IsMultiChecked = false;
        BulkSelectedFields.Clear();
    }

    /// <summary>Rebuild <see cref="MoveTargets"/> từ các section đang có (gọi khi mở context-menu).
    /// Header ưu tiên tên đã resolve từ navigator group; section rỗng chưa có group → fallback mã.</summary>
    public void RefreshMoveTargets()
    {
        MoveTargets.Clear();
        foreach (var s in _context().AvailableSections.Where(s => s.Id > 0))
        {
            var resolved = Groups.FirstOrDefault(g => g.SectionId == s.Id)?.SectionName;
            var header   = !string.IsNullOrWhiteSpace(resolved) ? resolved : s.Code;
            MoveTargets.Add(new FieldMoveTargetItem(header, s.Id, s.Code, MoveBulkToSectionCommand));
        }
    }

    // Chuyển toàn bộ field đã tick sang section đích. Persist DB TRƯỚC (MoveFieldToSectionAsync),
    // chỉ khi thành công mới đổi vị trí trong navigator → tránh lệch state khi DB lỗi. Sau đó
    // reindex Order_No (1,3,5...) cho các group bị ảnh hưởng và persist qua UpdateFieldOrderAsync.
    /// <summary>Chuyển các field trong <see cref="BulkSelectedFields"/> sang section của <paramref name="target"/>.</summary>
    private async Task ExecuteMoveBulkToSectionAsync(FieldMoveTargetItem? target)
    {
        if (target is null || target.SectionId <= 0) return;
        if (BulkSelectedFields.Count == 0 || _fieldService is null) return;

        var fields = BulkSelectedFields.ToList();

        // Group đích có thể chưa tồn tại (navigator chỉ hiện group có field) → khởi tạo, chèn
        // trước nhóm "CHƯA TẠO FIELD" (SectionId=0) nếu có.
        var targetGroup = Groups.FirstOrDefault(g => g.SectionId == target.SectionId);
        if (targetGroup is null)
        {
            targetGroup = new FieldNavGroup
            {
                SectionId   = target.SectionId,
                SectionCode = target.SectionCode,
                SectionName = target.Header   // Header = tên đã resolve (hoặc mã fallback)
            };
            var colOnlyIdx = -1;
            for (var i = 0; i < Groups.Count; i++)
                if (Groups[i].SectionId == 0) { colOnlyIdx = i; break; }
            if (colOnlyIdx >= 0) Groups.Insert(colOnlyIdx, targetGroup);
            else                 Groups.Add(targetGroup);
        }

        var affectedGroups = new HashSet<FieldNavGroup>();
        foreach (var field in fields)
        {
            if (field.FieldId <= 0) continue; // cột chưa tạo field → bỏ qua

            var src = Groups.FirstOrDefault(g => g.Fields.Contains(field));
            if (src is null || ReferenceEquals(src, targetGroup)) continue; // đã ở section đích

            try
            {
                await _fieldService.MoveFieldToSectionAsync(field.FieldId, target.SectionId, _token());
            }
            catch (Exception ex)
            {
                _logger?.Capture(ex, $"FieldConfig.BulkMove field #{field.FieldId} → section #{target.SectionId}");
                continue; // DB lỗi → giữ nguyên field ở section nguồn
            }

            src.Fields.Remove(field);
            targetGroup.Fields.Add(field);
            affectedGroups.Add(src);
        }

        if (affectedGroups.Count == 0) { ClearBulkSelection(); return; }
        affectedGroups.Add(targetGroup);

        // Reindex Order_No (1,3,5...) cho mọi group bị ảnh hưởng + persist 1 lần.
        var orderItems = new List<(int FieldId, int OrderNo)>();
        foreach (var g in affectedGroups)
            for (var i = 0; i < g.Fields.Count; i++)
            {
                if (g.Fields[i].FieldId <= 0) continue;
                g.Fields[i].SortOrder = 1 + i * 2;
                orderItems.Add((g.Fields[i].FieldId, g.Fields[i].SortOrder));
            }

        if (orderItems.Count > 0)
        {
            try { await _fieldService.UpdateFieldOrderAsync(orderItems, _token()); }
            catch (Exception ex) { _logger?.Capture(ex, "FieldConfig.BulkMovePersistOrder"); }
        }

        // Xóa group nguồn nếu rỗng (navigator chỉ hiện group có field).
        foreach (var g in affectedGroups.Where(g => g.SectionId != target.SectionId && g.Fields.Count == 0).ToList())
            Groups.Remove(g);

        ClearBulkSelection();
    }
}
