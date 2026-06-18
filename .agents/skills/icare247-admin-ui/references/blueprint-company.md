# Blueprint — Màn "Công ty" (`TC_CongTy`)

> Màn mẫu áp dụng đầy đủ chuẩn admin-UI. Dùng làm khuôn cho các màn tổ chức/danh mục sau.
> Route: `/m/organization/company`. Bảng: `TC_CongTy` (cây tự tham chiếu `CongTy_Cha_Id`).

## Pattern tổng thể — master-detail

Dữ liệu **cây** → list dùng **TreeList** (xem `grid-dxtreelist.md`), không phải Grid phẳng.

```
┌ Nav dọc ┬─────────────────────────────────────────────────────┐
│ Tổ chức │ Công ty                          [⌕] [+ Thêm công ty] │ ← toolbar mỏng, 1 CTA
│         ├──────────────────────┬──────────────────────────────┤
│         │ TreeList cây công ty │  Form chi tiết (1:1)          │
│         │ Tập đoàn             │  ─ Định danh ───────────────  │
│         │  ├ Công ty A         │   [Mã][Tên][Tên viết tắt]     │
│         │  └ Công ty B         │   [Cấp][Công ty cha][MST]     │
│         │ (ghim cột chọn đầu)  │  ─ Địa chỉ ─────────────────  │
│         │                      │   [Số nhà/đường......full]    │
│         │                      │   [Phường-Xã ⌄] → Tỉnh suy ra │
│         │                      │  ─ Liên hệ ─────────────────  │
│         │                      │   [ĐT][Email][Website]        │
│         │                      │  ─ Pháp nhân & kế toán ─────  │
│         │                      │   [Người ĐD][GĐ][KTT]         │
│         │                      │   [Ngân hàng ⌄][Số TK]        │
│         │                      │  ─ Nhận diện ───── (TT_ sau)  │
│         │                      │                  [Hủy] [Lưu]  │ ← 1 primary
└─────────┴──────────────────────┴──────────────────────────────┘
```

## List — `Ui_View` kiểu TreeList `Tree_TC_CongTy`

- `View_Type=TreeList`, `Key_Field=Id`, `Parent_Field=CongTy_Cha_Id`.
- Cột đầu = **Tên** (hiển thị bậc cây). Cột tiếp: Mã · Tên viết tắt · Cấp · MST · **Trạng thái (badge)**.
- Baseline TreeList áp tự động (wrap, reorder, resize ColumnsContainer + cuộn ngang, hover, focus, cột chọn ghim-trái-đầu).

## Detail — form 1:1 theo section nghiệp vụ

Hierarchy: `Page title → Section (14/600) → field`, section cách nhau 24–32px. Lưới 12 cột, field co theo data
(mã/cấp/MST cùng hàng; địa chỉ/website full-width). `DxFormLayout` + `DxFormLayoutGroup` mỗi section.

| Section | Field (`TC_CongTy`) |
|---|---|
| **Định danh** | `Ma` · `Ten` · `TenVietTat` · `CapCongTy_Id` (lookup) · `CongTy_Cha_Id` (tree-picker self) · `MaSoThue` |
| **Địa chỉ** | `DiaChi` (full) · `PhuongXa_Id` (cascade → Tỉnh suy qua `DM_PhuongXa.TinhThanhPho_Id`, **không lưu trùng Tỉnh**) |
| **Liên hệ** | `DienThoai` · `Email` · `Website` |
| **Pháp nhân & kế toán** | `NguoiDaiDien` · `GiamDoc` · `KeToanTruong` · `NganHang_Id` (lookup) · `SoTaiKhoan` |
| **Nhận diện** | `Logo_Id` → `TT_TepDinhKem` — **hoãn tới đợt `TT_`** (chừa chỗ, chưa upload) |
| (ẩn/auto) | `TrangThai` (badge HoatDong/NgungHoatDong) · khối audit `CreatedBy/At…` |

- **1 CTA** "Lưu" + secondary "Hủy". Surface phẳng, không card-shadow. Radius theo thang.
- Payload Lưu = mọi field `IsVisible` (giữ read-only/LockOnEdit).

## i18n (bắt buộc — xem `i18n.md`)

- Section title: `organization.company.section.{identity|address|contact|legal|branding}`.
- Field label/placeholder/tooltip: đi qua metadata `Sys_Resource` (cấu hình WPF) — KHÔNG hardcode.
- Nút/toolbar/badge: `common.action.*`, `common.column.actions`, trạng thái qua i18n shell.

## Cách dựng (chốt riêng khi code)

`TC_CongTy` đặc thù (cây cha-con + cascade địa chỉ + lookup ngân hàng + nhiều section + logo) → **bespoke Razor**
trong RCL `ICare247.UI.Organization` (nguyên tắc "no-code mặc định, bespoke khi đặc thù"). List vẫn tái dùng
`DataView` qua `Ui_View` `Tree_TC_CongTy`.
