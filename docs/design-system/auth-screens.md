# ICare247 — Phong cách bộ màn Auth (ĐÃ CHỐT)

> Phong cách giao diện cho 4 màn xác thực: **Đăng nhập · Đăng ký · Quên mật khẩu · Đặt lại mật khẩu**.
> Style hi-tech / hiện đại, neo trên accent xanh ICare247. Áp dụng cho `src/frontend/ICare247_UI`.
> Định vị thương hiệu: SaaS quản lý **đa ngành nghề** (no-code) — KHÔNG phải y tế.

---

## Nguyên tắc chung

- **Tách riêng `auth.css`** — KHÔNG đụng `tokens.css`/grid ERP, KHÔNG dùng DevExpress cho màn auth (Blazor thuần + CSS).
- Màn auth mang **brand tập đoàn ICare247** → hardcode accent xanh `#0F6CBD` là đúng chủ đích (không dùng màu theo tenant).
- Khu làm việc bên trong (ngồi 4h+) giữ palette ERP trung tính dịu mắt (ADR-012) — auth không ảnh hưởng vì tách css.
- Mọi text → i18n key (xem `docs/spec/10_RESOURCE_KEY_CONVENTION.md`).
- Bắt buộc khi code: bọc animation trong `@media (prefers-reduced-motion: no-preference)`.

## Khung layout (cả 4 màn)

- Split 2 cột, card bo `16px`, viền mảnh.
- **Cột trái (~50%, nền trắng):** form.
- **Cột phải (~50%):** panel thương hiệu xanh + hiệu ứng.
- Responsive < 768px: ẩn panel phải, form full-width.
- Component tái dùng: `AuthShell` · `AuthInput` · `AuthButton` · `BrandPanel` (panel viết 1 lần, 4 màn slot nội dung).

## Tokens auth (gom trong `auth.css`)

| Token | Giá trị |
|---|---|
| accent | `#0F6CBD` |
| nút chính | `linear-gradient(135deg,#1A7AD4,#0F6CBD)` |
| panel | `linear-gradient(165deg,#1763AE,#0F4C8A 55%,#0C3D70)` (đổi nhẹ tông mỗi màn) |
| input | cao `46px` · radius `10px` · nền `#F8FAFC` · icon trái |
| focus ring | `0 0 0 4px rgba(15,108,189,.12)` + viền xanh |
| neon (badge/node) | cyan `#7DECF8` |
| success (checklist/strength) | `#16A34A` |
| logo tạm | ô gradient + icon `hexagon-letter-i`, chữ "ICare**247**" (247 xanh) |

## Mỗi màn 1 motif riêng (tránh lặp) + texture nền khác nhau

| Màn | Motif panel phải | Texture nền | Form đặc thù |
|---|---|---|---|
| **Đăng nhập** | Luồng dữ liệu / mạng lưới | Lưới kỹ thuật | Checkbox "Ghi nhớ đăng nhập" + social |
| **Đăng ký** | Hành trình onboarding 3 bước | Glow tối giản | Họ tên/Email/MK + thanh đo độ mạnh + đồng ý điều khoản |
| **Quên mật khẩu** | Khiên bảo mật + vòng radar | Vòng lan tỏa | 1 email + info banner xanh lá (chống dò email) |
| **Đặt lại mật khẩu** | Cụm thiết bị được khóa | Chấm bi (dot) | MK mới + xác nhận + checklist 4 điều kiện |

## Accessibility

- Mọi `<input>` có `<label for>`; nút hiện/ẩn mật khẩu có `aria-label`.
- Checklist độ mạnh mật khẩu: `aria-live="polite"`.
- Contrast chữ trên panel xanh đạt AA; focus ring xanh `4px` rõ cho keyboard nav.

## Trạng thái

Đã chốt phong cách (mức mockup). **CHƯA nối API** (chỉ UI). Bước tiếp: viết `auth.css` + các `.razor` vào `src/frontend/ICare247_UI`.
