# WEB-UX-04 — Audit Design System (bản đồ việc cần sửa)

> Ngày: 2026-07-19 · Phạm vi: `src/frontend/ICare247_UI` + `src/frontend/ICare247.UI.DynamicForms` (CSS).
> Đây là **audit chẩn đoán** — CHƯA sửa CSS. Việc migrate token (đổi màu/spacing/radius component) là
> bước rủi ro của WEB-UX-04 (đã rollback 1 lần), cần visual regression + duyệt ảnh từng bước — làm sau,
> theo "Cổng kiểm soát trước khi sửa giao diện" trong `WEB_UX_IMPROVEMENT_TASKS.md`.
>
> **Nguồn chuẩn (runtime):** `wwwroot/css/tokens.css`. Bộ token: màu (primary xanh #0F6CBD, secondary
> #2E7D32, accent amber #F59E0B, semantic success/warning/danger/info, gray 50–900), spacing 4/8/12/16/20/24/32,
> radius 4/6/8, font `--fs-xs..2xl`. Chuẩn bố cục/component = skill `.claude/skills/icare247-admin-ui/`.

## Xếp hạng

| # | Mức | Vấn đề | Vị trí |
|---|---|---|---|
| 1 | **P0** | Màu **tím legacy** rò ra runtime (trái brand xanh) | TreeLookupBox |
| 2 | **P1** | Font **Inter** khai trước nhưng KHÔNG bundle → khác máy | tokens.css |
| 3 | **P1** | Tài liệu `README.md` mô tả brand CŨ (Coral/Violet/Teal, Plus Jakarta) — trái hoàn toàn runtime | docs |
| 4 | **P2** | Hex hardcode nhiều cụm (không qua token) | DynamicForms, vài razor.css, app.css |
| 5 | **P3** | Mâu thuẫn chính sách override `--dxbl-*` (2 tài liệu nói ngược nhau) | skill vs tokens.css |
| 6 | **P3** | Comment trong tokens.css tự mâu thuẫn (Fluent vs "berry tím") | tokens.css |

---

## 1 (P0) — Màu tím legacy rò ra runtime → ĐÃ SỬA (2026-07-19)

- Nguồn: `TreeLookupBoxRenderer.razor.css` (node cây chọn/highlight) + `LookupBoxRenderer.razor.css`
  (item dropdown chọn) dùng token legacy KHÔNG tồn tại → rơi về hex **tím**:
  `var(--color-violet-600, #6B44E0)`, `var(--table-row-selected, #F5F0FF/#F0EAFF)`.
- **Đã sửa:** map về token brand — `--color-primary` (viền/chữ) + `--color-primary-soft` (nền chọn). Đồng thời
  dọn các fallback tím CHẾT `#845EF7` ở `var(--input-border-focus, #845EF7)` / `var(--color-primary, #845EF7)`
  (token đã tồn tại → vốn resolve xanh; bỏ fallback tím để không còn tím trong source). **Không còn hex tím**
  trong code (chỉ còn 1 comment lịch sử ở `tokens.css:4`).
- Đây là thay đổi GIAO DIỆN (node/item lookup được chọn đổi từ tím → xanh brand) — cần verify trực quan.

## 2 (P1) — Font Inter không bundle → ĐÃ SỬA (2026-07-19)

- `tokens.css:62` khai `"Inter"` đầu nhưng không có `@font-face` → khác render giữa máy.
- **Đã sửa:** đảo `--font-sans` → `"Segoe UI", system-ui, -apple-system, "Inter", sans-serif` (Segoe UI đầu —
  có sẵn trên Windows admin). Đổi 1 chỗ, mọi nơi đã dùng `var(--font-sans/--font-body)`.
- Là thay đổi GIAO DIỆN toàn cục (font chữ) — cần verify trực quan; máy không cài Inter thì vốn đã hiển thị
  Segoe UI nên khác biệt nhỏ.

## 3 (P1) — Tài liệu README lệch runtime → ĐÃ SYNC

- `docs/design-system/README.md` (cũ 187 dòng) mô tả brand "Colorful · Playful", màu Coral #FF6B6B /
  Violet #845EF7 / Teal #00C9A7, font Plus Jakarta Sans, token `--text-*` / `--radius-md=8px` /
  `--radius-lg=12px` / row 48px / button 40px — **không giá trị nào khớp runtime** và tham chiếu hàng loạt
  token không tồn tại (`--color-violet-500`, `--btn-height-md`, `--font-heading`, `--radius-2xl`…).
- **Đã xử lý (2026-07-19):** thay README bằng bản ngắn, chính xác, TRỎ VỀ `tokens.css` làm nguồn chuẩn
  (không duplicate token — README cũ drift chính vì duplicate). `hrm-layout-principles.md` kiểm lại: **còn chuẩn**
  (đã nói giữ Fluent Light + tokens.css), không đụng.

## 4 (P2) — Hex hardcode (không qua token) — ĐANG DỌN DẦN

Đếm hex literal (không phải trong tokens.css / vendor bootstrap):

| Vùng | Số hex | File (số) | Trạng thái |
|---|---|---|---|
| DynamicForms field renderers | ~29 còn | ~~TreeLookupBox(27)~~ ✅ · ~~LookupBox(27)~~ ✅ · Attachment(19) · LookupAddDialog(10) | **2/4 xong** |
| ICare247_UI razor.css | **53** | ImportWizard(21), I18nToolsPage(14), FilterPanel(13), UserManagement(5) | chưa |
| app.css | ~12 | xem dưới | chưa |

**Đã tokenize (2026-07-19):** `TreeLookupBoxRenderer.razor.css` + `LookupBoxRenderer.razor.css` — canary cụm
control lookup (liên quan WEB-UX-05). Dead-token → token chuẩn: `--text-*`→`--color-text*`, `--color-error`→
`--color-danger`, `--bg-surface`→`--color-card`, `--border-default`→`--color-border`, `--color-neutral-100`→
`--gray-100`, `--table-row-hover`→`--gray-50`, `--shadow-lg`→`--shadow-popup`, `--color-warning-*`→
`--color-accent-soft`/`--color-text`. Thay đổi thị giác nhỏ (xám nhích, đỏ lỗi #EF4444→brand #C62828) + node/item
chọn tím→xanh (P0). **Cần verify control lookup khi build.** Còn lại (Attachment/LookupAddDialog + razor.css +
app.css) — canary tiếp theo, chưa làm.

app.css đáng chú ý:
- `:498` `#0f6cbd` literal thay vì `var(--color-primary)`.
- `:60` `--btn-primary-active: #102538`, `:62` `--btn-danger-hover: #A02020` — sắc hover/active off-token
  (nên thành token dẫn xuất).
- `:258/:394` `#92400e`, `:284` `#854d0e` — chữ amber-nâu (delta-badge/notice) off semantic token.
- `:407/:415` `#64748b`, `:416` `#0f172a` — slate lệch thang gray (`--gray-500`=#6B7280).
- Debug panel (`:252/:253` `#7dd3fc/#0b1220`) — console tối cố ý, ưu tiên thấp.
- **Fix (khi migrate):** map về token gần nhất; cụm DynamicForms là ưu tiên (control nghiệp vụ hay xuất hiện).

## 4b (P1) — Control dropdown KHÔNG đồng nhất (khối địa chỉ) → ĐÃ SỬA (2026-07-19)

- User báo: trên 1 form có tới 3-4 kiểu dropdown. Gốc: input nền của form đều là **DevExpress** (DxTextBox/
  DxDateEdit/DxComboBox…), nhưng `IcAddressBlock` (khối AddressBox) render **Tỉnh = `<select>` native**
  (danh sách mở OS tự vẽ, KHÔNG style được; cao 32px lệch DevExpress ~38px) + **Địa chỉ = `<input>` native**.
- **Đã sửa (chốt sau khi user phản hồi):** control chuẩn form muốn đồng nhất = **dropdown TÙY BIẾN kiểu
  LookupBox** ("Cấp công ty": bo góc, header, ✓ chọn nền xanh nhạt) — KHÔNG phải DxComboBox. Nên:
  - **Tỉnh** → dropdown custom (button + popup `.iab-xa`/`.iab-popup`, lọc client ~34 tỉnh), style bám
    `.lookupbox` (editbox 42px + radius-input, popup radius-lg + shadow-popup, item hover xanh, **✓ + soft-blue
    khi chọn**, viền xanh khi focus). (Đã thử DxComboBox → user báo "khác hoàn toàn" → revert.)
  - **Xã/Phường** → giữ tìm-server (API cap 200/tỉnh, tỉnh sau sáp nhập 2025 có thể >200 xã) + dùng chung
    style `.iab-*` đó → giống Tỉnh + Cấp công ty.
  - **Địa chỉ** → `DxTextBox` (khớp các ô text khác Số điện thoại/Email/Website đều DxTextBox).
- **Bài học API DevExpress:** DxComboBox dùng **`TextFieldName`** (KHÔNG phải `TextField` — property này không
  tồn tại, bị nuốt như attribute lạ → fallback ToString; record thì lòi hết field). `LookupComboBoxRenderer`
  đang "chạy" nhờ hack `LookupOptionDto.ToString()=>Label` — pattern SAI, đừng lặp.
- **Ghi chú:** hệ dropdown hợp lý = 2 loại: **combobox đơn** (danh mục nhỏ) vs **search-picker** (dữ liệu lớn).
  Gom TẤT CẢ renderer (LookupComboBox=DxComboBox vs LookupBox custom) về một chuẩn là quyết định lớn hơn — chưa chốt.

## 4c (P0 nợ kỹ thuật) — Không có control select DÙNG CHUNG → ĐANG TÁCH (2026-07-19)

- User báo (đúng): mỗi dropdown là code riêng (LookupBoxRenderer/DynamicForms · IcAddressBlock `.iab`/Shared ·
  DxComboBox) → cứ phải "chỉnh hoài" cho giống nhau. Gốc: **thiếu 1 control select dùng chung**.
- **Đã làm:** tạo **`ICare247.UI.Shared/Components/Pickers/IcSelectBox.razor`** — control select/lookup chuẩn
  "Cấp công ty" (generic `TItem`): gõ-thẳng-trong-ô lọc, popup teleport ra body, header + ✓ soft-blue +
  highlight bàn phím, "Thêm mới"; 2 chế độ **client** (Items) / **server** (SearchFunc debounce). Không phụ
  thuộc FieldState → mọi nơi dùng chung.
  - **IcAddressBlock đã migrate:** Tỉnh (client ~34) + Xã (server) đều dùng IcSelectBox → giống "Cấp công ty",
    gõ-thẳng-lọc (bỏ ô tìm riêng thừa). Địa chỉ giữ DxTextBox.
  - **LookupBoxRenderer ĐÃ migrate (2026-07-19):** UI editbox+popup thay bằng `<IcSelectBox>`; GIỮ data-logic
    (cascade reload, LoadDataAsync eager, đa-cột GetRowDisplayText, add-dialog). CSS `.lookupbox-*`/`.popup-*`
    cũ đã bỏ. → **"Cấp công ty" và Tỉnh/Xã giờ LÀ CÙNG một control** (IcSelectBox). ⚠️ Control lõi mọi form
    dùng — CẦN test kỹ: chọn/xóa/gõ-lọc, cascade (field cha đổi → reload + clear), "Thêm mới", teleport trong modal.
  - **ComboBoxRenderer + LookupComboBoxRenderer ĐÃ migrate (2026-07-19):** bỏ DxComboBox, dùng `<IcSelectBox>`.
    ComboBox (dynamic) giữ load+cascade; LookupComboBox (static Sys_Lookup) `allowUserInput` → `AllowFreeText`.
    Bổ sung IcSelectBox: `AllowFreeText` (combo tự do), `MaxRender=100` (list lớn — render N dòng + gợi ý gõ,
    thay virtualization), `HasError` (viền đỏ như dx-has-error cũ). **→ MỌI dropdown phẳng giờ = IcSelectBox.**
    Đánh đổi nhỏ: `searchMode="None"` (IcSelectBox luôn gõ-lọc) + `clearButton=false` (luôn cho xóa) → prop
    giữ để tương thích nhưng không còn tác dụng; mất virtualization thật của DxComboBox (thay bằng MaxRender).
  - **Chưa gộp (cố ý):** `TreeLookupBoxRenderer` (cây phân cấp — khác bản chất IcSelectBox phẳng, giữ riêng).
  - ⚠️ ComboBox/LookupComboBox dùng ở NHIỀU form → cần test: chọn/xóa/gõ-lọc, cascade (ComboBox dynamic),
    free-text (LookupComboBox allowUserInput), list lớn (ComboBox — kiểm gợi ý "còn N kết quả").

## 5 (P3) — Mâu thuẫn chính sách override `--dxbl-*`

- `app.css:454-470` override `--dxbl-text-edit-*` TRÊN selector `.dxbl-text-edit` (viền/focus/underline editor).
- `tokens.css:127-136` **cho phép** cách này (re-tint DevExpress qua override `--dxbl-*` trên selector
  component, không ở `:root`). Nhưng skill admin-ui ghi **"KHÔNG override `--dxbl-*`"**.
- → **Không phải bug**, là mâu thuẫn văn bản. Cần chốt 1 chính sách: hoặc cho phép override có kiểm soát
  trên selector component (thực tế đang cần để đồng bộ editor với theme), hoặc cấm tuyệt đối. Đề nghị: **cho
  phép có kiểm soát** + ghi rõ vào skill để hết mâu thuẫn. (`FilterPanel.razor.css` chỉ ĐỌC `var(--dxbl-*, fallback)`
  — không phải override, hợp lệ.)

## 6 (P3) — Comment tokens.css tự mâu thuẫn → ĐÃ SỬA

- `tokens.css` header nói accent = **Fluent Light #0F6CBD**, nhưng khối dưới (`:127-136`) vẫn nói theme là
  **"blazing-berry v25.2.3"** với **"berry tím #5f368d"** — tàn dư từ thời theme cũ, gây hiểu nhầm.
- **Đã sửa (2026-07-19):** cập nhật comment về Fluent Light cho khỏi mâu thuẫn (chỉ comment — không đổi giá trị).

---

## Gợi ý audit/lint tự động (chưa dựng — WEB-UX-04 đề xuất)

Khi dựng lint CSS, bắt các mẫu:
- Hex literal ngoài whitelist (chỉ tokens.css được khai hex gốc).
- `var(--…)` trỏ biến không có trong tokens.css (vd `--color-violet-600`).
- spacing/radius px ngoài thang (4/8/12/16/20/24/32 · radius 4/6/8).
- `font-family` literal (phải qua `var(--font-*)`).
- Khai (không phải đọc) `--dxbl-*` ngoài danh sách selector được duyệt.

## Cái CHƯA làm trong lượt này (bước migrate rủi ro — cần duyệt ảnh)

Sửa thật CSS ở finding 1/2/4 (đổi violet→primary, đảo font, dẹp 148 hex về token) là **thay đổi giao diện** —
làm theo Cổng kiểm soát: chụp baseline (1440/1280/768…), migrate từng nhóm component, duyệt ảnh trước–sau,
rollback nếu giảm hierarchy. KHÔNG gộp nhiều component trong một bước.
