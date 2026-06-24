# Rule — Lưới phẳng `DxGrid` (danh sách bảng)

> Áp cho **dữ liệu phẳng** (1 bảng, không cha-con). Dữ liệu **cây** → xem `grid-dxtreelist.md`.
> Mẫu tham chiếu đang chạy: bản ghi `Ui_View` kiểu Grid **`Grid_TrinhDoVanHoa`** (seed `Grid_{Form_Code}`).
> File này độc lập với `grid-dxtreelist.md` — chỉnh cái này KHÔNG đụng TreeList.

## Nơi set — KHÔNG cấu hình lại từng màn

| Tầng | Ở đâu | Gồm gì | Phạm vi |
|---|---|---|---|
| **Hành vi (baseline)** | component chung `DataView.razor` (view metadata-driven) · `MasterDataGrid.razor` (màn `/master`) | wrap, reorder, resize, hover, focus, cột-chọn | **Toàn cục** — sửa 1 lần áp mọi lưới |
| **Cấu hình theo màn** | DB `Ui_View` / `Ui_View_Column` (ConfigStudio WPF) | cột nào · Width · ghim · căn lề · filter · sort · paging | **Riêng từng View** |

→ Lưới mới = thêm 1 bản ghi `Ui_View` (như `Grid_TrinhDoVanHoa`). **Không viết props hành vi lại.**

## Baseline đã KHÓA (set trong component, đừng lặp ở màn)

| Hành vi | Property | Giá trị |
|---|---|---|
| Cho xuống dòng | `TextWrapEnabled` | `true` |
| Di chuyển cột | `AllowColumnReorder` | `true` |
| Kéo giãn cột + **cuộn ngang, KHÔNG ép vừa màn hình** | `ColumnResizeMode` | **`GridColumnResizeMode.ColumnsContainer`** |
| Hover dòng | `HighlightRowOnHover` | `true` |
| Dòng focus | `FocusedRowEnabled` | `true` |
| **Cột chọn luôn ở đầu** | `DxGridSelectionColumn` `Width="44px"` `FixedPosition="GridColumnFixedPosition.Left"` + **khai báo đầu tiên** | bật khi `SelectionMode="multiple"` |

> **Mấu chốt cuộn ngang:** `ColumnsContainer` = cột giữ bề rộng thật, tổng vượt khung → tự hiện thanh trượt ngang.
> `NextColumn` (cấm dùng làm mặc định) = ép tổng khít khung → bóp cột, không có scrollbar.
> DxGrid không có cờ bật/tắt scrollbar ngang riêng — điều khiển qua mode này + `Width` cột.

## Cấu hình theo màn (per-View, không phải baseline)

- **Cột:** `Width` / `MinWidth`, **ghim** `FixedPosition` (`Left`/`Right`), **căn lề** `TextAlignment` (số → phải), **sort mặc định** `SortIndex`+`SortOrder`.
- **Filter row:** `ShowFilterRow` + `FilterRowEditorVisible` per-cột; operator mặc định suy theo kiểu (text→Contains, số/bool→Equal), user đổi qua `FilterMenuButtonDisplayMode=Always`.
  - ⚠️ **Cấm `FilterRowCellVisible`** — KHÔNG tồn tại ở DX v25.2.3, crash runtime (rớt cột). Dùng **`FilterRowEditorVisible`**.
- **Paging vs VirtualScroll:** theo cấu hình; PageSizeSelector `10/20/50`.
- **Render_Mode ô:** Text / Boolean (`✓`) / Html / Image / Link / **Badge** + conditional format `Style_Rule_Json`.
- **Hàng động:** double-click dòng → Sửa; cột lệnh Sửa/Xóa ẩn/hiện theo quyền (`PermissionState`).

## Định dạng thị giác (theo skill chính)

- Ranh giới = **row-divider 1px nhạt**, KHÔNG vạch dọc từng cột (tránh cảm giác Excel). Header nền chênh 1 bậc, 12–13px/600. Zebra: KHÔNG (hover là tín hiệu chính). Row ~36–40px.
- **Header LUÔN ghim (bắt buộc):** mọi lưới chứa data phải set chiều cao giới hạn (qua `CssClass`, vd `.dv-dxgrid`) để DevExpress sinh vùng cuộn nội bộ + tiêu đề cố định. Tiêu đề **không bao giờ** trôi đi khi cuộn — không ngoại lệ. Xem `.claude-rules/blazor-ui.md` §"BẮT BUỘC — GHIM hàng tiêu đề".
  - ⚠️ **Đặt rule `height` ở CSS GLOBAL (`app.css`), KHÔNG ở `.razor.css` scoped:** CSS isolation không gắn scope `[b-xxx]` lên root `DxGrid` (component con) → `.my-grid { height }` scoped không khớp root, height vô hiệu, header trôi (rule `::deep` vẫn chạy nên dễ tưởng nhầm đã áp).
- Trạng thái = **badge text-màu**, ≤ 2–3 màu.

## Verify khi đụng API DevExpress

Tra `docs/reference/DEVEXPRESS_DXGRID_PROPERTIES.md` (reflect DLL v25.2.3) trước khi set property mới — **không bịa API**.
