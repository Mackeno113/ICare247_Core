# Rule — Lưới cây `DxTreeList` (dữ liệu cha-con)

> Áp cho **dữ liệu cây / tự tham chiếu** (parent-child): `TC_CongTy`, `TC_PhongBan`, `HT_ChucNang`…
> Dữ liệu **phẳng** → xem `grid-dxgrid.md`. File này độc lập — chỉnh cái này KHÔNG đụng DxGrid.
> Render qua component chung `DataView.razor`: tự chọn `DxTreeList` khi `View_Type="TreeList"` + có `KeyField`+`ParentField`.

## Khi nào dùng TreeList thay Grid

Dữ liệu có quan hệ cha-con cần **thấy phân cấp** (tập đoàn → công ty con; khối → phòng → tổ).
Cấu hình View: `View_Type="TreeList"`, `Key_Field` = khóa, `Parent_Field` = khóa cha (NULL = gốc).

## Baseline đã KHÓA (set trong `DataView.razor`)

| Hành vi | Property | Giá trị |
|---|---|---|
| Cho xuống dòng | `TextWrapEnabled` | `true` |
| Di chuyển cột | `AllowColumnReorder` | `true` |
| Kéo giãn cột + **cuộn ngang, KHÔNG ép vừa màn hình** | `ColumnResizeMode` | **`TreeListColumnResizeMode.ColumnsContainer`** |
| Hover dòng | `HighlightRowOnHover` | `true` |
| Dòng focus | `FocusedRowEnabled` | `true` |
| **Cột chọn luôn ở đầu** | `DxTreeListSelectionColumn` `Width="44px"` `FixedPosition="TreeListColumnFixedPosition.Left"` + **khai báo đầu tiên** | bật khi `SelectionMode="multiple"` |

> Khác DxGrid ở **tên enum**: `TreeListColumnResizeMode` / `TreeListColumnFixedPosition` (không phải `GridColumn*`).
> Cuộn ngang cùng cơ chế: `ColumnsContainer` + `Width` cột → tổng vượt khung thì có scrollbar.

## Riêng của TreeList

- **Cột FixedPosition:** `DxTreeListDataColumn.FixedPosition` kiểu `TreeListColumnFixedPosition`.
- **Cuộn dọc giữ header:** `VirtualScrollingEnabled="true"` — **cần đặt chiều cao** cho TreeList qua `CssClass`.
- **Bung/thu node:** `AutoExpandAllNodes` (cây nông → bung sẵn). Cấu hình theo nghiệp vụ.
- **Filter/Search:** `ShowFilterRow`, `ShowSearchBox` lọc theo node (giữ tổ tiên).
- Cột Data: `Width`/`MinWidth`, `TextAlignment`, `AllowSort` — như Grid.

## Định dạng thị giác

Giống `grid-dxgrid.md` (row-divider nhạt, không vạch dọc, header sticky 600, không zebra, badge trạng thái).
Khác biệt duy nhất: **cột đầu hiển thị bậc cây** (indent + nút bung/thu) — để cột định danh (Tên) làm cột đầu.

## Verify khi đụng API DevExpress

Tra `docs/reference/DEVEXPRESS_DXTREELIST_PROPERTIES.md` (reflect DLL v25.2.3) trước khi set property mới.
