---
name: icare247-admin-ui
description: >-
  Quy chuẩn thiết kế UI quản trị nội bộ / HRM cho ICare247 (Blazor WASM +
  DevExpress, theme Fluent Light token đã khóa). Dùng MỖI KHI dựng hoặc review
  màn hình nghiệp vụ: form nhập liệu, bảng dữ liệu (DxGrid), layout module,
  nút/CTA, spacing, typography, responsive. Mục tiêu: giao diện công cụ làm việc
  mật độ cao — KHÔNG card-hóa mọi thứ, KHÔNG landing-page màu mè, KHÔNG UI kiểu
  AI-generated. Trigger với "dựng màn", "thiết kế form/bảng", "layout HRM",
  "review UI", hoặc bất kỳ việc tạo giao diện admin nào.
---

# ICare247 — Admin/HRM UI Standards

> Giao diện nội bộ là **công cụ làm việc**, không phải trang trình diễn.
> Khuôn mẫu tham chiếu: **Linear, Vercel, Supabase, Sentry** (data dày, phẳng,
> precise) — KHÔNG phải Apple/landing-page.

## ⛔ Ràng buộc bất biến (không được vi phạm)

- **Theme = Fluent Light, token đã KHÓA.** Đổi màu = thay file accent, **KHÔNG**
  override `--dxbl-*`. Không tự thêm màu/font/token mới.
- **Tối đa 3 màu**: Neutral (90% UI) · Accent xanh (CTA/link/selection) ·
  Semantic (đỏ/cam/lá — CHỈ cho trạng thái nghiệp vụ). Không có màu thứ 4.
- **Tối đa 4 cấp chữ.** Hierarchy bằng weight + màu, KHÔNG bằng phóng to size.
  Không có chữ > 24px trong app nội bộ.
- **1 CTA chính / màn hình.** Không bao giờ 2 nút primary cạnh nhau.
- **Bảng chiếm 70–80% diện tích** ở màn danh sách. Toolbar mỏng, không hero/banner.
- **KHÔNG tạo dashboard nếu nghiệp vụ không cần** — vào thẳng bảng/việc.
- **Mỗi module 1 layout riêng** — không dùng chung template generic.

## Checklist nhanh (chạy qua trước khi xuất UI)

- [ ] Có bọc form/bảng trong card + shadow không? → Bỏ. Surface phẳng.
- [ ] Có dùng 1 radius duy nhất khắp nơi không? → Dùng thang radius theo cấp.
- [ ] Có shadow mặc định Bootstrap/DevExpress không? → Bỏ. Shadow chỉ cho lớp nổi tạm thời.
- [ ] Form có section nghiệp vụ + hierarchy chưa? → Bắt buộc.
- [ ] Có > 1 nút primary không? → Chỉ giữ 1.
- [ ] Có > 3 màu không? → Cắt về ≤ 3.
- [ ] Mọi field full-width xếp dọc đều nhau? → Dùng lưới 12 cột, field co theo data.
- [ ] Phân nhóm bằng border hay spacing? → Ưu tiên spacing (24–32px) hơn border.

## 1. Thang Spacing (4px grid — DUY NHẤT một thang)

`4 / 8 / 12 / 16 / 24 / 32`

| Khoảng cách | Giá trị |
|---|---|
| Label → input (trong 1 field) | 4px |
| Giữa các field | 12px |
| Giữa các **nhóm nghiệp vụ** | 24–32px (← thay cho border) |
| Padding vùng nội dung | 16–24px |

## 2. Typography

Giữ **Segoe UI**. Không nạp font "premium". `tabular-nums` cho cột số.

| Vai trò | Size / Weight | Dùng cho |
|---|---|---|
| Page title | 20px / 600 | Tên màn hình (1/màn) |
| Section | 14px / 600 | Tên nhóm field nghiệp vụ |
| Label | 13px / 500, màu phụ | Nhãn field |
| Body / data | 13–14px / 400 | Giá trị, cell bảng |
| Caption | 12px / 400, mờ | Hint, đơn vị, helper |

## 3. Border radius — KHÔNG đồng nhất 1 giá trị

| Thành phần | Radius |
|---|---|
| Input / nút / badge | 4px |
| Panel chọn / popover / modal | 8px |
| Avatar / chip | full |
| Bảng & section | 0 (vuông) |

## 4. Shadow

- Bỏ shadow mặc định. Surface phẳng (nền app vs surface chỉ chênh **1 bậc**).
- Shadow CHỈ cho lớp nổi tạm thời (dropdown/popover/modal/toast):
  `0 2px 8px rgba(0,0,0,.08)` — **một lớp**, khuếch tán thấp.
- Tách trên cùng mặt phẳng = spacing + 1px divider, KHÔNG phải shadow.

## 5. Form rules

1. **Hierarchy bắt buộc:** `Page title → Section nghiệp vụ (14/600) → field`.
   Section cách nhau 24–32px. VD HRM nhân sự:
   *Thông tin cá nhân / Hợp đồng / Lương & phụ cấp / Liên hệ khẩn cấp*.
2. **Label top-aligned** cho form dày (quét nhanh + responsive mượt).
   Label-cạnh-input chỉ cho form rất ngắn.
3. **Lưới 12 cột, field co theo bản chất dữ liệu:** ngày/mã/số → cùng hàng;
   địa chỉ/ghi chú → full-width. KHÔNG kéo mọi input dài bằng nhau.
4. **Footer:** 1 primary (Lưu) + secondary/ghost (Hủy, Lưu & thêm).
5. **KHÔNG bọc form trong card-shadow.** Form ngồi thẳng trên surface.
6. **DevExpress:** `DxFormLayout` + `DxFormLayoutGroup` cho mỗi nhóm nghiệp vụ;
   `ColSpanMd` để chia cột. Đừng để mỗi field 1 dòng full-width máy móc.
7. **Payload Lưu** = mọi field `IsVisible` (chỉ loại field ẩn; giữ read-only/LockOnEdit).

## 6. Grid / Table rules

1. Bảng là nhân vật chính → **70–80% chiều cao**. Toolbar/filter mỏng phía trên.
2. Ranh giới = **row-divider 1px màu rất nhạt**. KHÔNG vạch dọc từng cột (tránh
   cảm giác Excel cũ) → "ưu tiên khoảng trắng hơn border".
3. Header: nền chênh 1 bậc, chữ 12–13px/600, **sticky**. Không tô đậm.
4. **Zebra striping: KHÔNG** (hoặc cực nhạt). Row-hover là tín hiệu chính.
5. Canh phải số/tiền, canh trái text, ngày theo locale. Cột thao tác cố định phải.
6. Row-height ~36–40px (mật độ vừa). Không row 56px kiểu material.
7. Trạng thái = **badge text-màu**, ≤ 2–3 màu. Không chấm tròn to.
8. **DevExpress `DxGrid`:** virtual scroll, `ShowFilterRow`, ẩn GridLines dọc.
   Filter nâng cao theo quy ước Ui_View + Ui_View_Filter.

## 7. Button rules

| Cấp | Style | Khi dùng |
|---|---|---|
| Primary | Nền accent xanh (token khóa) | **1/màn** — Lưu, Tạo |
| Secondary | Viền + nền trong suốt | Hủy, Đóng |
| Ghost/text | Chỉ chữ | Thao tác nhỏ trong toolbar bảng |
| Danger | Đỏ **outline/ghost** | Xóa (tô đỏ đặc chỉ lúc xác nhận) |

- Nút & input cùng cấp radius (4px). **Không gradient, không shadow** trên nút.
- Icon-only chỉ cho toolbar dày quen thuộc; hành động quan trọng phải kèm label.

## 8. Responsive

- Breakpoint: ≥1280 (làm việc chính) · 768–1280 (tablet) · <768 (tra cứu/duyệt).
- Form: 12 cột → 2 cột → 1 cột. Label top-aligned nên xuống cột mượt.
- Bảng hẹp: KHÔNG nhồi cột — đẩy cột phụ vào row-detail/expand, giữ 3–4 cột định danh.
- Nav dọc → icon-rail → off-canvas. CTA chính dính bottom trên mobile.

## 9. Motion

- CHỈ animate `transform` + `opacity` (~150ms) cho hover/expand.
- Tôn trọng `@media (prefers-reduced-motion)`.
- KHÔNG: parallax, scroll-narrative, magnetic button, custom cursor, animate
  `width/height/top/margin`.

## 10. Anti-AI UI — đối chiếu NÊN / KHÔNG

| ❌ KHÔNG | ✅ NÊN |
|---|---|
| Card trắng + shadow bọc mọi thứ | Surface phẳng + section + spacing |
| 1 radius 8px khắp nơi | Thang radius theo thành phần |
| Mọi field full-width xếp dọc đều | Lưới 12 cột, field co theo data |
| Dashboard KPI mặc định mọi module | Không dashboard nếu không cần |
| Hero / banner gradient / illustration | Toolbar mỏng + bảng |
| Nhiều nút primary | 1 CTA/màn |
| Animation kể chuyện | Transition transform/opacity ngắn |

## 11. Layout tham chiếu

### ✅ Màn danh sách (master)
```
┌ Nav dọc ┬─────────────────────────────────────────┐
│ module  │ Tiêu đề màn          [⌕ tìm] [+ Tạo mới] │ ← toolbar mỏng, 1 CTA
│ riêng   ├─────────────────────────────────────────┤
│         │ Filter row (inline)                      │
│         │ ┌─────────────────────────────────────┐ │
│         │ │ BẢNG DỮ LIỆU  ~75% chiều cao        │ │
│         │ │ row-divider mảnh, không vạch dọc     │ │
│         │ └─────────────────────────────────────┘ │
└─────────┴─────────────────────────────────────────┘
```

### ✅ Màn chi tiết / nhập liệu (1:1)
```
Tiêu đề bản ghi                              [Hủy] [Lưu] ← 1 primary
─ Thông tin cá nhân ────────────────  (section 14/600)
  [Họ tên........] [Ngày sinh] [Giới tính]   ← field co theo data
─ Hợp đồng & Lương ─────────────────  (cách trên 32px)
  [Loại HĐ] [Bậc] [Lương cơ bản......]
```

### ❌ KHÔNG làm
```
┌ Card shadow ┐ ┌ Card shadow ┐ ┌ Card shadow ┐   ← card hóa mọi thứ
│ KPI 1       │ │ KPI 2       │ │ KPI 3       │   ← dashboard không ai cần
└─────────────┘ └─────────────┘ └─────────────┘
┌──── Card bọc cả form, bo 8px, shadow ───────┐
│ Label:  [input full-width                ]  │   ← field đều tăm tắp
│ Label:  [input full-width                ]  │   ← không nhóm nghiệp vụ
│              [Lưu]  [In]  [Gửi]  [Duyệt]    │   ← 4 nút primary
└─────────────────────────────────────────────┘
```
