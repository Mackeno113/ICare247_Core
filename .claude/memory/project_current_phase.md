# Project Current Phase

> Cập nhật: 2026-07-27. Lịch sử tiến độ per-session (61→5) + bảng phase đã xong → [project_phase_history.md](project_phase_history.md).
> Trạng thái phiên gần nhất → [last_session.md](last_session.md). Việc đang mở đầy đủ → [../../TASKS.md](../../TASKS.md).

## Roadmap chính (đường tới hạn)

**F1 — Đồng bộ config master→tenant** (nền cho engine-hóa màn) → **F2 — engine-hóa màn Công ty** (ORG-CFG) →
danh mục nền tảng (CAT-CFG). F1 code xong (CFGSYNC-0→3, descriptor 14 bảng), `db/050` đã chạy; **còn E2E** +
migration `db/062` (Config). Spec: `docs/spec/16_CONFIG_SYNC_SPEC.md`. Chi tiết + trạng thái từng task → `TASKS.md`.

**Nền tảng mới cho F2 (2026-07-18):** bộ 3 control TreeList/Lookup dùng chung (kéo-thả sắp xếp cây
ADR-027, lọc theo công ty declarative, self-ref parent picker) đã code xong — engine không còn cần
viết SQL tay cho màn có cây + có công ty (Phòng ban, Công ty, Vị trí công việc...). Xem TASKS.md +
last_session.md session 88.

## Control động web = RCL dùng chung (2026-07-09)

- `FieldRenderer` + renderer đã tách sang RCL **`ICare247.UI.DynamicForms`** (host + Portal tương lai dùng chung).
- Project **`ICare247.Blazor.RuntimeCheck` đã XÓA HẲN** (session 79) — không còn bản nhân bản renderer.

## Module xuất tài liệu (2026-07-09)

- **Doc Template** (xuất Word/PDF theo mẫu, mail-merge, ghép-fragment) — spec `docs/spec/28_DOC_TEMPLATE_SPEC.md`.
  Backend GĐ1 + soạn WPF GĐ3 xong; DevExpress cô lập ở `ICare247.Infrastructure.Documents` (backend) +
  `ConfigStudio.WPF.UI.Modules.DocTemplate` (WPF). Còn: chạy `db/077`, GĐ2 soạn Web. Chi tiết → TASKS.md.

## Đang mở / ad-hoc gần đây (đầy đủ ở TASKS.md)

- **Nhật ký lỗi 500** (ADR-037, session 96 2026-07-27, CHƯA commit) — bảng `NK_LoiHeThong` (Audit DB) +
  màn Admin `/m/administration/error-logs` tra "Mã lỗi" trên web thay vì mở file log server. Build backend+Web
  0W/0E, test 145/145. **CHƯA chạy `db/095`/`db/096`, CHƯA smoke runtime.**
- **Sinh mã tự động cột `Ma`** (spec 32 / ADR-036, session 94 commit `e1650b3` + session 95 2026-07-25
  CHƯA commit) — MA-1…MA-8 code xong; **DB sống lệch schema cũ chặn mọi Lưu → đã fix `db/090`, user đã
  chạy**; màn WPF "Quy tắc sinh mã"/"Mẫu Lookup" tách list/popup + cột "Mã dự kiến" + tooltip/hướng dẫn.
  Build ConfigStudio 0W/0E. Chưa bật quy tắc cho bảng nào (cố ý) → còn E2E khi có nghiệp vụ thật cần bật.
- **FK lookup auto-JOIN** (session 72, CHƯA commit) — cột lưới hiện TÊN cha; cần build+restart API + commit.
- **Save hook store** (ADR-029, SVHOOK) · **Bộ lọc cascade + context param** (ADR-030, VFILTER).
- **Hệ đính kèm / Upload file tổng quát** (session 77) — P1–P6 CODE XONG; migration `db/dev/create_tt_attachment_full.sql` ĐÃ CHẠY (user xác nhận 2026-07-20); còn E2E trình duyệt. Spec 26 + hướng dẫn WPF `cau-hinh-attachment.md`.
- **Quản lý thông số hệ thống** — spec `docs/spec/27_SYSTEM_SETTINGS_SPEC.md` viết xong, CHƯA code (schema-driven, hybrid file+DB, Blazor web admin).
- **Bảo mật Tầng 1→5** — SEC1→5 đã code (spec 20 §9); còn E2E Tầng 2/3, MFA, DB least-privilege.

## Định hướng nền tảng (durable — không đổi tùy tiện)

- **ICare247 = SaaS quản lý ĐA NGÀNH (no-code), KHÔNG phải y tế** dù tên có "Care". Định vị "một nền tảng, mọi ngành nghề". [[project-icare247-saas-brand]]
- **Theme = DevExpress Fluent Light + accent xanh `#0F6CBD`** (ADR-012). Đổi màu = thay 1 file `accents/*`. [[project-theme-fluent-light]]
- **Kiến trúc dữ liệu:** Config DB (metadata, có cache) + Data DB per-tenant (HT_/DM_/TC_…, tiếng Việt). DB-per-tenant, không `Tenant_Id` ở Data DB. (ADR-022/025)
- **Backend Phase 1-6 + ConfigStudio 11 màn + Blazor runtime**: đã hoàn thành nền (chi tiết bảng trạng thái → project_phase_history.md).

## Soát chất lượng backend (2026-08-03) — vấn đề cốt lõi

> Báo cáo đầy đủ: [`docs/reviews/2026-08-03-backend-code-audit.md`](../../docs/reviews/2026-08-03-backend-code-audit.md).
> Kiến trúc tốt (Clean Arch/CQRS/DI), nhưng vùng nguy hiểm nhất (dynamic SQL + cô lập tenant) vừa dựa
> kỷ luật thủ công vừa không có test. 6 vấn đề cốt lõi (chưa sửa code):

1. 🔴 **Test gần như bằng 0** — 5 file test / 358 file production; Infrastructure (dynamic SQL) không test.
2. 🔴 **Guard SQL copy-paste, lệch nhau** — `SafeIdentifierRegex` ≥5 bản, 2 pattern khác nhau → quy tắc mới `.claude-rules/sql-safety.md`.
3. 🔴 **Cô lập tenant 1 lớp** — `tenantId` không lọc SQL (ADR-035), sai resolver = rò rỉ chéo im lặng.
4. 🟠 **Engine nuốt exception** — mâu thuẫn architecture.md (đã sửa rule, làm rõ chính sách).
5. 🟠 **God-class + switch dispatch** — ViewRepository 1038 dòng; EventEngine/ImportEngine switch (OCP).
6. 🟡 **Nợ đã đánh dấu** — TODO(SEC1-4) hạ quyền LookupController, CC-3 permission null, RestoreForm workaround, JWT keyring in-memory.

**Ưu tiên sửa:** (1) gom guard SQL → `SqlIdentifier` chung · (2) test repo dynamic-SQL + tenant · (3) chuẩn hóa exception engine · (4) đóng nợ SEC/CC-3.

## Việc nền còn treo

- Integration tests (BE-002) · E2E Master Data với DB thật (BE-003) · WPF-14 LookupBox manual test — xem `TASKS.md`.
