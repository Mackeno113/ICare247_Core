# ICare247 — Design System (Web)

> **Phong cách hiện tại: ERP Fluent Light** — nghiêm túc, mật độ cao, trung tính, ≤3 màu, accent **xanh
> Fluent `#0F6CBD`**. Công cụ làm việc, KHÔNG phải landing-page.
>
> ⚠️ Bản "Colorful · Playful" (Coral/Violet/Teal, Plus Jakarta Sans) trước đây **đã bị thay thế hoàn toàn**
> (ADR-012). Nếu bạn còn thấy màu tím/coral ở đâu → đó là tàn dư cần dọn (xem `DESIGN_AUDIT.md`).

## Nguồn chuẩn (đừng duplicate)

Tài liệu này **không** liệt kê lại từng token — README cũ drift chính vì chép token ra đây rồi lệch với code.
Nguồn sự thật:

| Nguồn | Là gì |
|---|---|
| **`../../src/frontend/ICare247_UI/wwwroot/css/tokens.css`** | **Nguồn chuẩn token runtime** — màu, spacing, radius, font, shadow, z-index. Đổi token = sửa ở đây. |
| **`.claude/skills/icare247-admin-ui/`** | Chuẩn bố cục + component (form, bảng, nút, spacing, typography, anti-AI). Đọc khi dựng/review UI. |
| `hrm-layout-principles.md` | Nguyên tắc bố cục HRM đa công ty (1:1 form / 1:N grid, nav dọc, TreeList). |
| `auth-screens.md` | Bộ 4 màn Auth (vùng style riêng có chủ đích — `auth.css`). |
| `DESIGN_AUDIT.md` | Audit WEB-UX-04: chỗ còn lệch token + bản đồ migrate. |
| `WEB_UX_IMPROVEMENT_TASKS.md` | Backlog 7 task cải thiện UX Web + trạng thái. |

## Tóm tắt bộ token (chi tiết ở `tokens.css`)

- **Màu (≤3 nhóm):** Neutral (gray 50–900, chiếm ~90% UI) · Accent xanh `--color-primary #0F6CBD` (CTA/link/
  selection/focus) · Semantic chỉ cho trạng thái: `--color-success #2E7D32` · `--color-warning/accent #F59E0B` ·
  `--color-danger #C62828` · `--color-info #1565C0` (mỗi màu có bản `-soft`). **Không màu thứ 4.**
- **Font:** `--font-sans` (Segoe UI — xem finding P1 trong audit), `--font-mono` (JetBrains Mono cho số/mã).
- **Cỡ chữ:** `--fs-xs 12` · `--fs-sm 13` (mặc định grid/form) · `--fs-base 14` · `--fs-md 16` · `--fs-lg 18` ·
  `--fs-xl 22` · `--fs-2xl 28`. Tối đa ~4 cấp thực dụng; hierarchy bằng weight+màu, không phóng to size.
- **Spacing:** thang 4px — `--space-1..8` = 4/8/12/16/20/24/32.
- **Radius:** `--radius-sm 4` (input/nút/badge) · `--radius-md 6` (mặc định) · `--radius-lg 8` (popup/modal).
  Bảng & section = 0 (vuông).
- **Shadow:** phẳng — chỉ lớp nổi tạm thời (popup/modal/toast) dùng `--shadow-popup`; surface không đổ bóng.

## Ràng buộc bất biến

- Theme = **Fluent Light**, token đã KHÓA. Đổi màu accent = qua accent API Fluent / file accent, **KHÔNG**
  override `--dxbl-*` ở `:root`. (Chính sách override `--dxbl-*` trên selector component: xem finding P3 trong
  `DESIGN_AUDIT.md` — đang chờ chốt.)
- **1 CTA chính / màn.** Bảng chiếm 70–80% màn danh sách. Không card hóa mọi vùng.
- **i18n bắt buộc** — mọi chuỗi qua `Loc.L(key, "fallback vi")` hoặc `Sys_Resource`. Không hardcode.
- Mọi màu hiển thị phải qua `var(--color-*)` — không hex literal trong component (xem audit để dọn tàn dư).

## Layout trang (WEB-UX-02)

- `.page-container` mặc định **rộng** (max-width 1680px) cho màn danh sách; trang form/đọc thêm `.page-narrow`
  (1100px). Cuộn ngang chỉ trong vùng lưới (`.app-content { overflow-x: hidden }`). Filter panel stack-above ≤991px.
