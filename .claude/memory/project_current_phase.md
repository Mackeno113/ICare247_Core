# Project Current Phase

> Cập nhật: 2026-07-04. Lịch sử tiến độ per-session (61→5) + bảng phase đã xong → [project_phase_history.md](project_phase_history.md).
> Trạng thái phiên gần nhất → [last_session.md](last_session.md). Việc đang mở đầy đủ → [../../TASKS.md](../../TASKS.md).

## Roadmap chính (đường tới hạn)

**F1 — Đồng bộ config master→tenant** (nền cho engine-hóa màn) → **F2 — engine-hóa màn Công ty** (ORG-CFG) →
danh mục nền tảng (CAT-CFG). F1 code xong (CFGSYNC-0→3, descriptor 14 bảng), `db/050` đã chạy; **còn E2E** +
migration `db/062` (Config). Spec: `docs/spec/16_CONFIG_SYNC_SPEC.md`. Chi tiết + trạng thái từng task → `TASKS.md`.

## Control động web = RCL dùng chung (2026-07-09)

- `FieldRenderer` + renderer đã tách sang RCL **`ICare247.UI.DynamicForms`** (host + Portal tương lai dùng chung).
- Project **`ICare247.Blazor.RuntimeCheck` đã XÓA HẲN** (session 79) — không còn bản nhân bản renderer.

## Module xuất tài liệu (2026-07-09)

- **Doc Template** (xuất Word/PDF theo mẫu, mail-merge, ghép-fragment) — spec `docs/spec/28_DOC_TEMPLATE_SPEC.md`.
  Backend GĐ1 + soạn WPF GĐ3 xong; DevExpress cô lập ở `ICare247.Infrastructure.Documents` (backend) +
  `ConfigStudio.WPF.UI.Modules.DocTemplate` (WPF). Còn: chạy `db/077`, GĐ2 soạn Web. Chi tiết → TASKS.md.

## Đang mở / ad-hoc gần đây (đầy đủ ở TASKS.md)

- **FK lookup auto-JOIN** (session 72, CHƯA commit) — cột lưới hiện TÊN cha; cần build+restart API + commit.
- **Save hook store** (ADR-029, SVHOOK) · **Bộ lọc cascade + context param** (ADR-030, VFILTER).
- **Hệ đính kèm / Upload file tổng quát** (session 77) — P1–P6 CODE XONG (storage hybrid, bảo mật, streaming, tối ưu ảnh SkiaSharp, dedup, control 2 chế độ, đa-tệp-khi-thêm-mới); còn chạy migration (`db/dev/create_tt_attachment_full.sql`) + E2E. Spec 26 + hướng dẫn WPF `cau-hinh-attachment.md`.
- **Quản lý thông số hệ thống** — spec `docs/spec/27_SYSTEM_SETTINGS_SPEC.md` viết xong, CHƯA code (schema-driven, hybrid file+DB, Blazor web admin).
- **Bảo mật Tầng 1→5** — SEC1→5 đã code (spec 20 §9); còn E2E Tầng 2/3, MFA, DB least-privilege.

## Định hướng nền tảng (durable — không đổi tùy tiện)

- **ICare247 = SaaS quản lý ĐA NGÀNH (no-code), KHÔNG phải y tế** dù tên có "Care". Định vị "một nền tảng, mọi ngành nghề". [[project-icare247-saas-brand]]
- **Theme = DevExpress Fluent Light + accent xanh `#0F6CBD`** (ADR-012). Đổi màu = thay 1 file `accents/*`. [[project-theme-fluent-light]]
- **Kiến trúc dữ liệu:** Config DB (metadata, có cache) + Data DB per-tenant (HT_/DM_/TC_…, tiếng Việt). DB-per-tenant, không `Tenant_Id` ở Data DB. (ADR-022/025)
- **Backend Phase 1-6 + ConfigStudio 11 màn + Blazor runtime**: đã hoàn thành nền (chi tiết bảng trạng thái → project_phase_history.md).

## Việc nền còn treo

- Integration tests (BE-002) · E2E Master Data với DB thật (BE-003) · WPF-14 LookupBox manual test — xem `TASKS.md`.
