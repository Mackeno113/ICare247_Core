# PROMPT 03 — Form Editor (Screen 03)

> **Project:** ICare247.ConfigStudio.Modules.Forms
> **Chạy sau Prompt 02.**

---

## PROMPT

```
Đọc CLAUDE.md trước.

Implement FormEditorView trong ICare247.ConfigStudio.Modules.Forms.
Đây là màn hình cấu trúc form: quản lý Section và Field bên trong mỗi Section.

─── FILES CẦN TẠO ───────────────────────────────────────

1. Views/FormEditorView.xaml + .xaml.cs
2. ViewModels/FormEditorViewModel.cs
3. Models/SectionDto.cs
4. Models/FieldRowDto.cs                ← hiển thị trong DataGrid của section
5. Views/SectionHeaderControl.xaml     ← UserControl cho header của mỗi section

─── LAYOUT ──────────────────────────────────────────────

DockPanel:
┌── Toolbar (Top, Height=52) ─────────────────────────────────────────────────────┐
│  ← Back  │  [Form: PURCHASE_ORDER] [Table: tbl_PurchaseOrder] [Platform: Web]  │
│  [+ Add Section]  [↑↓ Reorder Sections]                   [💾 Save] [▶ Publish]│
└────────────────────────────────────────────────────────────────────────────────┘
┌── Content (Fill) ───────────────────────────────────────────────────────────────┐
│  ScrollViewer                                                                   │
│    ItemsControl (ItemsSource=Sections):                                         │
│      DataTemplate per SectionDto:                                               │
│      ┌─ Expander (IsExpanded=true) ─────────────────────────────── Header ──── ┐│
│      │  Header = SectionHeaderControl:                                          ││
│      │    [≡ drag] "Section: Thông Tin Chung" [Order:1]   [✎ Edit] [🗑 Delete] ││
│      │  Content:                                                                 ││
│      │    Toolbar: [+ Add Field] [↑↓ Reorder]                                  ││
│      │    DataGrid (fields của section này):                                    ││
│      │      #  │ Column_Code │ Editor_Type │ Label_Key │ Vis │ RO │ Order │ ⚙  ││
│      │      ─────────────────────────────────────────────────────────────────  ││
│      │      1  │ MaDonHang   │ TextBox     │ lbl.mdn   │  ✓  │ ✓  │  1   │[⚙] ││
│      │      2  │ TrangThai   │ ComboBox    │ lbl.tt    │  ✓  │    │  2   │[⚙] ││
│      └────────────────────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────────────────────────┘

─── MODELS ──────────────────────────────────────────────

public class SectionDto
{
    public int SectionId { get; set; }
    public string SectionCode { get; set; } = "";
    public string TitleKey { get; set; } = "";
    public string TitleDisplay { get; set; } = "";    // resolved từ i18n
    public int OrderNo { get; set; }
    public bool IsActive { get; set; }
    public ObservableCollection<FieldRowDto> Fields { get; set; } = new();
}

public class FieldRowDto
{
    public int FieldId { get; set; }
    public string ColumnCode { get; set; } = "";      // từ Sys_Column
    public string NetType { get; set; } = "";         // Int32, String, DateTime...
    public string EditorType { get; set; } = "";      // TextBox, ComboBox, NumericBox...
    public string LabelKey { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public bool IsReadOnly { get; set; }
    public int OrderNo { get; set; }
    public int RuleCount { get; set; }                // badge count
    public int EventCount { get; set; }               // badge count
}

─── VIEWMODEL ───────────────────────────────────────────

Properties:
  - FormId: int
  - FormCode: string
  - TableCode: string
  - Platform: string
  - Sections: ObservableCollection<SectionDto>
  - SelectedSection: SectionDto?
  - SelectedField: FieldRowDto?
  - IsDirty: bool                    ← true khi có thay đổi chưa save
  - IsLoading: bool

Commands:
  - LoadFormCommand: DelegateCommand       ← gọi trong OnNavigatedTo với formId
  - AddSectionCommand: DelegateCommand     ← mở dialog nhập Section_Code, Title_Key
  - EditSectionCommand: DelegateCommand<SectionDto>
  - DeleteSectionCommand: DelegateCommand<SectionDto>   ← confirm nếu có fields
  - MoveSectionUpCommand: DelegateCommand<SectionDto>   ← swap Order_No
  - MoveSectionDownCommand: DelegateCommand<SectionDto>

  - AddFieldCommand: DelegateCommand<SectionDto>
      → navigate sang FieldConfig: { "formId", "sectionId", "mode": "new" }
  - ConfigFieldCommand: DelegateCommand<FieldRowDto>
      → navigate sang FieldConfig: { "fieldId", "mode": "edit" }
  - DeleteFieldCommand: DelegateCommand<FieldRowDto>
  - MoveFieldUpCommand: DelegateCommand<FieldRowDto>
  - MoveFieldDownCommand: DelegateCommand<FieldRowDto>

  - SaveCommand: DelegateCommand            ← save tất cả thay đổi
  - PublishCommand: DelegateCommand         ← navigate sang PublishChecklist: { "formId" }
  - BackCommand: DelegateCommand            ← navigate về FormManager

─── MOCK DATA ────────────────────────────────────────────

OnNavigatedTo nhận formId=1, load mock data:
Section 1 "Thông Tin Chung" (OrderNo=1):
  - MaDonHang   │ TextBox    │ lbl.madohang     │ Vis=true  │ RO=true  │ Order=1 │ Rules=1
  - NgayDatHang │ DatePicker │ lbl.ngaydathang  │ Vis=true  │ RO=false │ Order=2 │ Rules=0
  - TrangThai   │ ComboBox   │ lbl.trangthai    │ Vis=true  │ RO=false │ Order=3 │ Events=2
  - NhaCungCap  │ LookupBox  │ lbl.nhacungcap   │ Vis=true  │ RO=false │ Order=4

Section 2 "Chi Tiết" (OrderNo=2):
  - SoLuong     │ NumericBox │ lbl.soluong      │ Vis=true  │ RO=false │ Order=1 │ Rules=2
  - DonGia      │ NumericBox │ lbl.dongia       │ Vis=true  │ RO=false │ Order=2 │ Rules=1
  - ThanhTien   │ NumericBox │ lbl.thanhtien    │ Vis=true  │ RO=true  │ Order=3 │ Events=1
  - LyDoTuChoi  │ TextArea   │ lbl.lydotuchoi   │ Vis=false │ RO=false │ Order=4 │ Rules=1

─── STYLE YÊU CẦU ───────────────────────────────────────

- Is_Visible=false → Row text màu muted (#78909C), icon 👁‍🗨 with strikethrough hint
- Is_ReadOnly=true → icon 🔒 nhỏ bên cạnh Column_Code
- RuleCount/EventCount: hiển thị dưới dạng chip nhỏ "R:2" "E:1" màu xanh/cam
  - RuleCount=0 → ẩn chip
- Column ⚙ (config): IconButton với icon Settings, click → ConfigFieldCommand
- Expander header: Accent color (#1565C0), bold text, icon arrow
- Drag handle (≡): hiển thị nhưng chưa cần implement drag thật (Phase 2)

─── IsDirty TRACKING ────────────────────────────────────

- Khi thêm/xóa/sửa section hoặc field → IsDirty = true
- WindowClosing hoặc BackCommand → nếu IsDirty: confirm "Có thay đổi chưa lưu. Thoát?"
- Save thành công → IsDirty = false
```
