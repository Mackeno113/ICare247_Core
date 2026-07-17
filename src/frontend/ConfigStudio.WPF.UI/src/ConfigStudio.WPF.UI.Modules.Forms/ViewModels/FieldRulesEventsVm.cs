// File    : FieldRulesEventsVm.cs
// Module  : Forms
// Layer   : Presentation
// Purpose : REFACTOR-B3 — VM con 2 tab "Validation Rules" + "Events" của màn Cấu hình Field,
//           tách nguyên trạng từ FieldConfigViewModel: danh sách rule/event liên kết, mở editor,
//           xóa (confirm + reindex Order_No). Sửa kèm smell async void: ExecuteDeleteEvent giờ là
//           async Task bọc trong DelegateCommand (cùng pattern DeleteRule). Hành vi giữ NGUYÊN.

using System.Collections.ObjectModel;
using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Core.Interfaces;
using ConfigStudio.WPF.UI.Modules.Forms.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Modules.Forms.ViewModels;

/// <summary>VM con 2 tab Rules/Events — root expose qua property <c>RulesEvents</c>.</summary>
public sealed class FieldRulesEventsVm : BindableBase
{
    /// <summary>Snapshot ngữ cảnh từ root VM tại thời điểm thao tác (đổi theo navigation).</summary>
    public sealed record Context(int FieldId, int FormId, string ColumnCode, string TableCode, string SectionName);

    private readonly IRuleDataService? _ruleService;
    private readonly IEventDataService? _eventService;
    private readonly IAppLogger? _logger;
    private readonly IRegionManager _regionManager;
    private readonly Func<Context> _context;
    private readonly Func<CancellationToken> _token;
    private readonly Action _markDirty;

    public ObservableCollection<RuleSummaryDto> LinkedRules { get; } = [];
    public ObservableCollection<EventSummaryDto> LinkedEvents { get; } = [];

    public DelegateCommand AddRuleCommand { get; }
    public DelegateCommand<RuleSummaryDto> OpenRuleCommand { get; }
    public DelegateCommand<RuleSummaryDto> DeleteRuleCommand { get; }
    public DelegateCommand AddEventCommand { get; }
    public DelegateCommand<EventSummaryDto> OpenEventCommand { get; }
    public DelegateCommand<EventSummaryDto> DeleteEventCommand { get; }

    public FieldRulesEventsVm(
        IRuleDataService? ruleService,
        IEventDataService? eventService,
        IAppLogger? logger,
        IRegionManager regionManager,
        Func<Context> context,
        Func<CancellationToken> token,
        Action markDirty)
    {
        _ruleService = ruleService;
        _eventService = eventService;
        _logger = logger;
        _regionManager = regionManager;
        _context = context;
        _token = token;
        _markDirty = markDirty;

        AddRuleCommand = new DelegateCommand(ExecuteAddRule);
        OpenRuleCommand = new DelegateCommand<RuleSummaryDto>(ExecuteOpenRule);
        DeleteRuleCommand = new DelegateCommand<RuleSummaryDto>(async r => await ExecuteDeleteRuleAsync(r));
        AddEventCommand = new DelegateCommand(ExecuteAddEvent);
        OpenEventCommand = new DelegateCommand<EventSummaryDto>(ExecuteOpenEvent);
        // Trước B3 là async void — giờ async Task bọc trong delegate (exception không rơi tự do).
        DeleteEventCommand = new DelegateCommand<EventSummaryDto>(async e => await ExecuteDeleteEventAsync(e));
    }

    /// <summary>Xóa cả 2 danh sách (root gọi khi reset field / trước khi load field khác).</summary>
    public void Clear()
    {
        LinkedRules.Clear();
        LinkedEvents.Clear();
    }

    /// <summary>
    /// Load rules liên kết field (phụ — lỗi trả message warning, không crash). OperationCanceled
    /// ném tiếp để root abort chuỗi load (giữ trình tự cũ). Trả null khi thành công.
    /// </summary>
    public async Task<string?> LoadRulesAsync(CancellationToken ct)
    {
        var ctx = _context();
        if (ctx.FieldId <= 0 || _ruleService is null) return null;

        try
        {
            var rules = await _ruleService.GetRulesByFieldAsync(ctx.FieldId, ct);
            LinkedRules.Clear();
            foreach (var r in rules)
            {
                LinkedRules.Add(new RuleSummaryDto
                {
                    RuleId            = r.RuleId,
                    OrderNo           = r.OrderNo,
                    RuleTypeCode      = r.RuleTypeCode,
                    ExpressionPreview = r.ExpressionJson ?? "",
                    ErrorKey          = r.ErrorKey,
                    IsActive          = r.IsActive
                });
            }
            // IsRequired là cột DB (Ui_Field.Is_Required) — root đã load từ GetFieldDetailAsync
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Rules load thất bại (VD: chưa chạy migration 003) → warning nhỏ
            _logger?.Capture(ex, $"FieldConfig.LoadRules field #{ctx.FieldId}");
            return $"Không tải được validation rules: {ex.Message}";
        }
    }

    /// <summary>Load events liên kết field (phụ — lỗi chỉ log). OperationCanceled ném tiếp cho root.</summary>
    public async Task LoadEventsAsync(CancellationToken ct)
    {
        var ctx = _context();
        if (ctx.FieldId <= 0 || _eventService is null) return;

        try
        {
            var events = await _eventService.GetEventsByFieldAsync(ctx.FieldId, ct);
            LinkedEvents.Clear();
            foreach (var e in events)
            {
                LinkedEvents.Add(new EventSummaryDto
                {
                    EventId          = e.EventId,
                    OrderNo          = e.OrderNo,
                    TriggerCode      = e.TriggerCode,
                    ConditionPreview = e.ConditionExpr ?? "",
                    ActionsCount     = e.ActionsCount,
                    IsActive         = e.IsActive
                });
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { _logger?.Capture(ex, $"FieldConfig.LoadEvents field #{_context().FieldId}"); }
    }

    /// <summary>Đánh lại OrderNo 1..n sau khi xóa rule.</summary>
    private void ReindexRuleOrders()
    {
        for (int i = 0; i < LinkedRules.Count; i++)
            LinkedRules[i].OrderNo = i + 1;
    }

    /// <summary>Đánh lại OrderNo 1..n sau khi xóa event.</summary>
    private void ReindexEventOrders()
    {
        for (int i = 0; i < LinkedEvents.Count; i++)
            LinkedEvents[i].OrderNo = i + 1;
    }

    private void ExecuteAddRule()
    {
        var ctx = _context();
        var p = new NavigationParameters
        {
            { "fieldId",     ctx.FieldId     },
            { "formId",      ctx.FormId      },
            { "fieldCode",   ctx.ColumnCode  },
            { "tableCode",   ctx.TableCode   },
            { "sectionName", ctx.SectionName },
            { "mode",        "new"           }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private void ExecuteOpenRule(RuleSummaryDto? rule)
    {
        if (rule is null) return;
        var ctx = _context();
        var p = new NavigationParameters
        {
            { "ruleId",      rule.RuleId     },
            { "fieldId",     ctx.FieldId     },
            { "formId",      ctx.FormId      },
            { "fieldCode",   ctx.ColumnCode  },
            { "tableCode",   ctx.TableCode   },
            { "sectionName", ctx.SectionName },
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.ValidationRuleEditor, p);
    }

    private async Task ExecuteDeleteRuleAsync(RuleSummaryDto? rule)
    {
        if (rule is null) return;

        // Xác nhận trước khi xóa
        var confirm = System.Windows.MessageBox.Show(
            $"Xóa rule [{rule.RuleTypeCode}] — {rule.ErrorKey}?\nThao tác này không thể hoàn tác.",
            "Xác nhận xóa rule",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        // Xóa DB nếu rule đã được lưu (RuleId > 0)
        if (rule.RuleId > 0 && _ruleService is not null)
            await _ruleService.DeleteRuleAsync(rule.RuleId, _token());

        LinkedRules.Remove(rule);
        ReindexRuleOrders();
        _markDirty();
    }

    private void ExecuteAddEvent()
    {
        var ctx = _context();
        var p = new NavigationParameters
        {
            { "fieldId",     ctx.FieldId     },
            { "formId",      ctx.FormId      },
            { "fieldCode",   ctx.ColumnCode  },
            { "tableCode",   ctx.TableCode   },
            { "sectionName", ctx.SectionName },
            { "mode",        "new"           }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }

    private async Task ExecuteDeleteEventAsync(EventSummaryDto? evt)
    {
        if (evt is null) return;

        // Xác nhận trước khi xóa — default No, không thể hoàn tác
        var confirm = System.Windows.MessageBox.Show(
            $"Xóa event [{evt.TriggerCode}] → '{evt.FieldTarget}'?\nThao tác này không thể hoàn tác.",
            "Xác nhận xóa event",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        // Xóa DB nếu event đã được lưu (EventId > 0)
        if (evt.EventId > 0 && _eventService is not null)
            await _eventService.DeleteEventAsync(evt.EventId, _token());

        LinkedEvents.Remove(evt);
        ReindexEventOrders();
        _markDirty();
    }

    private void ExecuteOpenEvent(EventSummaryDto? evt)
    {
        if (evt is null) return;
        var ctx = _context();
        var p = new NavigationParameters
        {
            { "eventId",     evt.EventId     },
            { "fieldId",     ctx.FieldId     },
            { "formId",      ctx.FormId      },
            { "fieldCode",   ctx.ColumnCode  },
            { "tableCode",   ctx.TableCode   },
            { "sectionName", ctx.SectionName }
        };
        _regionManager.RequestNavigate(RegionNames.Content, ViewNames.EventEditor, p);
    }
}
