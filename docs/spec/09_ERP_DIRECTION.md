# 09 — ERP Direction & Decision Update

> **Mục đích:** Ghi lại định hướng chuyển ICare247 Core Platform thành nền tảng vận hành
> **ERP nội bộ** (nông nghiệp / thương mại), và các quyết định kiến trúc đi kèm.
> **Ngày chốt:** 2026-06-07
> **Trạng thái:** Đã chốt — áp dụng cho toàn bộ công việc frontend & nghiệp vụ tiếp theo.

---

## 1. Tóm tắt định hướng

ICare247 vẫn là **metadata-driven low-code form engine** ở phần lõi. Trên nền đó, dự án
xây một **ERP nội bộ** gồm 8 module: Nhân sự, Công lương, Thu mua cafe nhân, Thu mua hồ
tiêu, Phân bón, Kho, Công nợ, Báo cáo.

Mô hình triển khai: **Engine + Code tay kết hợp.**
- **Engine** (ConfigStudio khai báo metadata → FormRunner render): danh mục & form đơn giản.
- **Code tay** (component Blazor + CQRS handler riêng): nghiệp vụ phức tạp (lương, thu mua
  master-detail, kho, công nợ, dashboard, báo cáo).

Cả hai dùng chung backbone đã xây: schema metadata, 4 engine backend, Dapper repository,
cache hybrid, JWT/multi-tenant, logging.

---

## 2. Decision Records

> Bản đầy đủ xem tại `.claude/memory/architecture_decisions.md`.

### ADR-011: Giữ .NET 9 + Blazor WASM (không đổi sang Server)
- **Context:** Cân nhắc Blazor Server cho ERP nội bộ (gọi Dapper thẳng, ít code).
- **Decision:** **Giữ nguyên repo — .NET 9 + Blazor WASM + API + Dapper + Redis.** Không
  hạ version, không viết lại frontend.
- **Reason:** Backbone WASM/API đã có; lo ngại "nhiều DB call" giải quyết bằng caching +
  gộp query, không cần đổi mô hình hosting. Tránh chi phí viết lại.
- **Hệ quả bắt buộc (kỷ luật WASM):** Mỗi màn hình áp dụng từ đầu:
  1. **Bootstrap endpoint** — gộp metadata + lookup + initial data trong 1 response.
  2. **Dapper `QueryMultiple`** — gộp nhiều query thành 1 DB roundtrip.
  3. **Client cache** trong WASM cho danh mục tĩnh (`Sys_Lookup`) — load 1 lần / phiên.
  4. **Server-side paging / virtual scrolling** cho mọi grid lớn.
  5. **Smart invalidation** qua `Sys_Cache_Invalidation` + `Sys_Version` khi ghi.

### ADR-012: Thay hẳn Design Tokens sang palette ERP
- **Context:** Bộ tokens cũ theo brand "I Care 24/7" — Colorful/Playful (Coral/Violet/Teal),
  không hợp ERP nghiêm túc.
- **Decision:** **Thay hoàn toàn** `docs/design-system/tokens.css` bằng palette ERP:
  Primary `#1E3A5F`, Secondary `#2E7D32`, Accent `#F59E0B`, nền `#F5F7FA`, card `#FFFFFF`,
  border `#E5E7EB`. Font đổi sang Inter.
- **Reason:** Phong cách mục tiêu là Odoo / SAP Fiori / Oracle Fusion — trung tính, đặc
  data. Blazor frontend còn sơ khai nên chi phí thay tokens ~0.
- **Phong cách:** ERP first, Data first, form đơn giản, bo góc nhẹ, đổ bóng tiết chế.
- **Trạng thái (2026-06-09):** `tokens.css` ERP đã áp dụng vào `wwwroot/css/` của
  `ICare247.Blazor.RuntimeCheck`. Chưa wire `--dx-*` override (BE-004 còn pending).

### ADR-013: Domain chuyển sang nông nghiệp / thương mại
- **Context:** Domain gốc là y tế (ví dụ `patient_name`, `BloodType` trong spec/agent).
- **Decision:** Domain chính thức là **ERP nông nghiệp/thương mại** (cafe, hồ tiêu, phân bón).
  Engine vẫn đa-ngành; chỉ metadata nghiệp vụ và ví dụ trong tài liệu cần đổi.
- **Reason:** Không ảnh hưởng schema (metadata trung tính). Chỉ cập nhật ví dụ & agent.

---

## 3. Danh sách file cần sửa trong repo

| File | Việc cần làm | Trạng thái |
|---|---|---|
| `docs/design-system/tokens.css` | Thay hẳn bằng `tokens.css` ERP | ✅ Done (session 14) |
| `docs/design-system/README.md` | Cập nhật mô tả palette/phong cách → ERP, bỏ "Playful" | 🔴 Chưa làm |
| `.claude/agents/design-agent.md` | Đổi brand direction → ERP; bỏ Coral/Violet/Teal | 🔴 Chưa làm |
| `.claude/agents/product-analyst.md` | Đổi mô tả domain "y tế" → "ERP nông nghiệp/thương mại" | 🔴 Chưa làm |
| `.claude/memory/architecture_decisions.md` | Thêm ADR-011, 012, 013 | ✅ Done |
| `docs/spec/00_PROJECT_OVERVIEW.md` | Thêm mục tiêu ERP + mô hình engine/code tay | 🔴 Chưa làm |
| `README.md` | Thêm dòng mô tả: nền tảng dùng để dựng ERP nội bộ | 🔴 Chưa làm |
| `ROADMAP.md` | Lộ trình tổng thể | ✅ Done (2026-06-09) |
| `docs/spec/09_ERP_DIRECTION.md` | Chính là file này | ✅ Done |

> **Lưu ý:** Không sửa schema DB. Các bảng metadata (`Sys_*`, `Ui_*`, `Val_*`, `Evt_*`)
> giữ nguyên — ERP chỉ *thêm dữ liệu* metadata, không đổi cấu trúc.

---

## 4. Trạng thái nền tảng (cập nhật 2026-06-09)

Tất cả điểm xác minh ban đầu đã được giải quyết:

| Điểm xác minh | Trạng thái |
|---|---|
| DevExpress Blazor cài + license | ✅ Xác nhận (session 15) — nâng cấp 24.2→25.2.3, theme `blazing-berry.bs5.min.css` |
| FormRunner render form đa loại field | ✅ 8 renderer đầy đủ: TextBox, Memo, CheckBox, Numeric, DatePicker, Select, LookupBox, TreePicker |
| Tên biến `--dx-*` trong tokens | 🔴 Chưa wire — BE-004 còn pending |
| Master-detail giao dịch ERP | 🔴 Cần template riêng — Phase 0 còn thiếu (ROADMAP Phase 0) |
| Tab phức tạp / tính toán liên field | ✅ Hạ tầng sẵn: Ui_Tab (migration 005), EventEngine (6 action), AST engine 25 hàm |

### Backbone đã xây (tính đến 2026-06-09)

| Thành phần | Trạng thái |
|---|---|
| Backend .NET 9 — Phase 1-6 (entities, repos, 4 engines, API infra) | ✅ Done |
| DB schema 30 migrations (000–030) | ✅ Canonical |
| ConfigCache facade ADR-014 (CC-0→CC-2) | ✅ Done |
| Master Data CRUD generic full-stack | ✅ Done (session 38-41) |
| DxGrid chuẩn `MasterDataGrid` | ✅ Done (session 41) |
| Ui_View ADR-015 — cấu hình danh sách tách khỏi form | 🟡 VIEW-0 chốt; VIEW-1→4c pending |
| ConfigStudio WPF (11 screens, Direct DB) | ✅ Done |
| Shell ERP + Sidebar navigation | 🔴 Chưa làm |
| Module nghiệp vụ ERP (Phase 1-8) | 🔴 Chưa làm |

> Xem `ROADMAP.md` để biết thứ tự triển khai từng phase và lộ trình chi tiết.
