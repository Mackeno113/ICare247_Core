# PROMPT 02 — Form Manager (Screen 02)

> **Project:** ICare247.ConfigStudio.Modules.Forms
> **Chạy sau khi Prompt 01 (Shell) đã hoàn thành.**

---

## PROMPT

```
Đọc CLAUDE.md trước.

Implement FormManagerView trong module ICare247.ConfigStudio.Modules.Forms.

─── FILES CẦN TẠO ───────────────────────────────────────

1. FormsModule.cs                            ← đăng ký View+ViewModel, navigate mặc định
2. Views/FormManagerView.xaml + .xaml.cs
3. ViewModels/FormManagerViewModel.cs
4. Models/FormSummaryDto.cs

─── LAYOUT: MASTER-DETAIL ───────────────────────────────

Grid(RowDefinitions: *, Auto):
┌── Row 0: Toolbar + DataGrid (Fill) ────────────────────────────────────────────┐
│  [+ New Form] [✎ Edit] [⧉ Clone] [🗑 Delete]          [SearchBox 🔍]          │
│  ─────────────────────────────────────────────────────────────────────────────  │
│  DataGrid (AutoGenerateColumns=False, CanUserResizeRows=False,                  │
│            SelectionMode=Single, IsSynchronizedWithCurrentItem=True):           │
│  Columns:                                                                       │
│    Form_Code      (200px, readonly)                                             │
│    Table_Code     (160px, readonly)                                             │
│    Platform       (100px, readonly) — badge style (Web=blue, Mobile=purple)     │
│    Layout_Engine  (100px, readonly)                                             │
│    FieldCount     (80px,  readonly, header="Fields")                            │
│    RuleCount      (80px,  readonly, header="Rules")                             │
│    EventCount     (80px,  readonly, header="Events")                            │
│    IsActive       (80px,  readonly) — badge (Active=green ●, Draft=gray ○)     │
└────────────────────────────────────────────────────────────────────────────────┘
┌── Row 1: Detail Panel (Height=160, collapsible) ──────────────────────────────┐
│  Khi SelectedForm != null:                                                      │
│  ┌─ Card ─────────────────────────────────────────────────────────────────────┐│
│  │  Form: PURCHASE_ORDER  │  Table: tbl_PurchaseOrder  │  Platform: Web       ││
│  │  Sections: 3  │  Fields: 12  │  Rules: 24  │  Events: 18                  ││
│  │  Last Modified: 28/02/2026 by admin                                         ││
│  │  [Open Editor →]  [Preview Runtime]  [Export Metadata JSON]  [View Deps]   ││
│  └────────────────────────────────────────────────────────────────────────────┘│
│  Khi SelectedForm == null:                                                      │
│    TextBlock "Chọn một form để xem thông tin chi tiết" (centered, muted)       │
└────────────────────────────────────────────────────────────────────────────────┘

─── MODEL: FormSummaryDto ────────────────────────────────

public class FormSummaryDto
{
    public int FormId { get; set; }
    public string FormCode { get; set; } = "";
    public string TableCode { get; set; } = "";
    public string Platform { get; set; } = "";           // "Web" | "Mobile"
    public string LayoutEngine { get; set; } = "";       // "Grid" | "Stack"
    public int FieldCount { get; set; }
    public int RuleCount { get; set; }
    public int EventCount { get; set; }
    public int SectionCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

─── VIEWMODEL PROPERTIES & COMMANDS ─────────────────────

Properties:
  - Forms: ObservableCollection<FormSummaryDto>
  - SelectedForm: FormSummaryDto?
  - FilteredForms: CollectionViewSource (wrap Forms, filter theo SearchText)
  - SearchText: string (OnPropertyChanged → refresh FilteredForms)
  - IsDetailVisible: bool (= SelectedForm != null)
  - IsLoading: bool

Commands:
  - LoadFormsCommand: DelegateCommand        ← gọi trong OnNavigatedTo
  - NewFormCommand: DelegateCommand
      → navigate sang FormEditor: { "mode": "new" }
  - EditFormCommand: DelegateCommand
      → CanExecute: SelectedForm != null
      → navigate: { "formId": SelectedForm.FormId, "mode": "edit" }
  - CloneFormCommand: DelegateCommand
      → CanExecute: SelectedForm != null
      → Confirm dialog "Clone form [Form_Code]?" → gọi service → reload
  - DeleteFormCommand: DelegateCommand
      → CanExecute: SelectedForm != null
      → Confirm dialog "Xóa form [Form_Code]? Thao tác không thể hoàn tác."
      → gọi service → reload
  - OpenEditorCommand: DelegateCommand       ← từ detail panel button
  - ExportJsonCommand: DelegateCommand       ← mở SaveFileDialog, export JSON

─── DATA (tạm thời dùng mock data) ──────────────────────

Trong LoadFormsCommand, tạo ObservableCollection với mock data:
  new FormSummaryDto { FormId=1, FormCode="PURCHASE_ORDER",
    TableCode="tbl_PurchaseOrder", Platform="Web", LayoutEngine="Grid",
    FieldCount=12, RuleCount=24, EventCount=18, SectionCount=3,
    IsActive=true, LastModified=DateTime.Now.AddDays(-3), LastModifiedBy="admin" }
  new FormSummaryDto { FormId=2, FormCode="CUSTOMER_FORM", ... }
  new FormSummaryDto { FormId=3, FormCode="INVOICE_FORM", Platform="Mobile",
    IsActive=false, ... }
(thêm 2-3 rows nữa tùy ý)

─── STYLE YÊU CẦU ───────────────────────────────────────

Platform badge:
  DataTrigger: Platform=="Web"    → Background=#1565C0, Foreground=White
  DataTrigger: Platform=="Mobile" → Background=#6A1B9A, Foreground=White
  Border CornerRadius=4, Padding=3,8

IsActive badge:
  DataTrigger: IsActive==true  → "● Active", Foreground=#2E7D32
  DataTrigger: IsActive==false → "○ Draft",  Foreground=#78909C

DataGrid row double-click → EditFormCommand (dùng EventSetter MouseDoubleClick)

─── FORMSMODULE.CS ──────────────────────────────────────

public class FormsModule : IModule
{
    public void RegisterTypes(IContainerRegistry cr)
    {
        cr.RegisterForNavigation<FormManagerView, FormManagerViewModel>(ViewNames.FormManager);
        // FormEditor sẽ thêm ở prompt tiếp theo
    }

    public void OnInitialized(IContainerProvider cp)
    {
        var rm = cp.Resolve<IRegionManager>();
        rm.RequestNavigate(RegionNames.Content, ViewNames.FormManager);
    }
}
```