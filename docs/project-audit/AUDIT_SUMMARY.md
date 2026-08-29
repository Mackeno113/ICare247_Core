# ICare247 Technical Audit — 2026-08-29

> Audit read-only, thực hiện bằng 9 agent song song (kiến trúc backend/Blazor/WPF, bảo mật, hiệu năng, database, test, tài liệu, công việc dở dang). Không có source code nào bị sửa. Dữ liệu chi tiết + dashboard: xem [`index.html`](index.html) và `data/*.json`. Cách đọc: [`README.md`](README.md).

## 1. Current State

ICare247 là một nền tảng no-code/metadata-driven: form nghiệp vụ được định nghĩa hoàn toàn qua database config (Sys_Table/Ui_Form/Val_Rule/Evt_Definition) và render bằng 4 "engine" runtime (Metadata, AST/Grammar, Validation, Event), chạy trên Clean Architecture 4 lớp (.NET 9) + Blazor WASM (DevExpress) cho end-user + WPF ConfigStudio (Prism 9 + DevExpress WPF) cho admin cấu hình. Đa tenant theo mô hình DB-per-tenant (Catalog DB resolver → Config DB + Data DB riêng mỗi tenant).

Dự án đã qua **113 migration SQL**, **1253 file source được git-track**, và một hệ thống quản trị tài liệu AI (BRAIN.md làm SSOT, TASKS.md làm nhật ký công việc 176KB) **đáng tin cậy bất thường** — TASKS.md đã tự phát hiện và sửa 1 đợt sai lệch trạng thái trước đó (8/18 dòng ADR Status sai vào 2026-07-10) và từ đó tập trung hoá trạng thái vào 1 bảng duy nhất, có trích dẫn commit.

**Đánh giá tổng thể: Trung bình-khá — kỷ luật kiến trúc tốt, nhưng có 3 điểm rủi ro tập trung cần xử lý.**

## 2. Architecture

Kiến trúc thực tế **khớp với kiến trúc khai báo trong BRAIN.md** ở mức đáng kể:
- 4 lớp Domain→Application→Infrastructure→Api đúng chiều phụ thuộc, xác nhận **0 EF Core**, **0 eval/dynamic-compile**, **0 exception bị nuốt trong engine**, **0 `.Result`/`.Wait()`** trong toàn bộ backend.
- 4 engine (Metadata/AST/Validation/Event) tách lớp interface (Domain/Application) vs implementation (Infrastructure) đúng quy ước, Event Engine dùng Strategy pattern sạch (9 handler nhỏ).
- Cô lập tenant qua kết nối DB-per-tenant + middleware xác thực chéo (TenantClaimGuardMiddleware) — 2 lớp phòng thủ độc lập.

**Điểm lệch (drift) đã xác nhận:** 1 cache key hardcode ngoài `CacheKeys.cs`, 2 chỗ `SELECT *` được chấp nhận có chủ đích, một số identifier SQL nguồn-từ-config chưa qua guard whitelist đầy đủ (ARCH-004/SEC-005), 1 chính sách override `--dxbl-*` trong Blazor CSS mà **chính codebase đã tự đánh dấu là đang chờ quyết định** (chưa đóng), và ở WPF: 2 view định nghĩa palette màu cục bộ không khớp `DesignTokens.xaml` (đúng kiểu lỗi ADR-031 muốn tránh), cùng code chết của tính năng "tab Permissions" đã gỡ khỏi UI nhưng vẫn còn ở tầng data (rủi ro tái tạo FK-conflict nếu vô tình nối lại).

## 3. Critical Findings

Không có finding mức **Critical**. Có **7 finding mức High**:

| ID | Vấn đề | Mảng |
|---|---|---|
| SEC-001 | Stored XSS trong dialog xoá node Menu Builder — có thể leo quyền qua chiếm session SUPERADMIN | Bảo mật |
| PERF-001 | Validation/Event Engine hoàn toàn bỏ qua cache — hạ tầng cache đã có nhưng chưa nối | Hiệu năng |
| PERF-002 | ViewRepository tải lại metadata FK-join/schema mỗi lần load lưới | Hiệu năng |
| TEST-001 | Không có project test nào cho Blazor và WPF ConfigStudio | Test |
| TEST-002 | 0% coverage cho CQRS handler, Dapper repository, RBAC, Config Sync, tree-integrity guard | Test |
| DOC-003 | `docs/spec/06_SOLUTION_STRUCTURE.md` không còn khớp cây thư mục thật | Tài liệu |
| DOC-004 | `docs/spec/11_DATA_DB_SCHEMA.md` ghi "chưa migrate" nhưng 43 bảng đã chạy thật | Tài liệu |

## 4. Security

0 Critical / **1 High** / 4 Medium / 2 Low / 3 Info. **8 mảng được review và xác nhận KHÔNG có vấn đề** (auth, RBAC, cô lập tenant, SQL injection guard chính, AST engine an toàn, secrets, CORS/header, logging).

- **SEC-001 (High):** Stored XSS ở `MenuBuilderPage.razor:365` — tên node menu (do admin nhập) được đưa thẳng qua `LocalizationService.L()` rồi ép kiểu `MarkupString` mà không encode. Kịch bản tấn công: 1 người có quyền sửa menu (không cần SUPERADMIN) đặt tên node chứa script → bất kỳ ai (kể cả SUPERADMIN) mở dialog xoá node đó sẽ chạy script trong phiên đăng nhập của họ.
- **SEC-002 (Medium):** IDOR trên endpoint attachment — bất kỳ user đã đăng nhập trong tenant có thể tải/xoá file đính kèm của record khác chỉ bằng cách đoán ID, không có kiểm tra chủ sở hữu.
- **SEC-003 (Medium):** `Catalog:EncryptionKey` không có guard fail-fast khi production như `Jwt:SecretKey` đã có — nếu quên set, connection string tenant sẽ lưu dạng plaintext.
- **SEC-004 (Medium):** Audit-log JSON diff generic không có cơ chế che field nhạy cảm theo cột.

## 5. Performance

2 finding High, 4 Medium (confirmed), 1 Low, 2 Medium (potential, chưa verify bằng execution plan thật).

- **PERF-001 (High):** `CacheKeys.FieldList`/`CacheKeys.RuleList` đã được định nghĩa sẵn nhưng **không được dùng ở đâu cả** — Validation/Event Engine gọi thẳng DB (join nhiều bảng) mỗi lần người dùng rời khỏi 1 field (blur) hoặc trigger event. Đây là fix giá trị cao nhất/effort thấp nhất tìm được trong audit này vì hạ tầng đã sẵn sàng.
- **PERF-002 (High):** `ViewRepository` build lại toàn bộ ngữ cảnh FK-join + kiểm tra schema mỗi lần load lưới, dù `ViewMetadata` xung quanh nó đã được cache.
- Không có `SELECT *` nào trong toàn bộ `db/**`. Không phát hiện N+1 nghiêm trọng ở tầng import (đã batch tốt).

## 6. Documentation

185 file tài liệu, quản trị (BRAIN.md/TASKS.md/ADR) **tốt hơn kích thước 176KB của TASKS.md gợi ý** — mọi claim "Đã xong" được spot-check trong audit này đều đúng với code thật. Vấn đề tập trung ở **tài liệu cũ chưa được dọn** (AI_PROJECT_BRIEF.md mâu thuẫn với chính quyết định đã thay thế nó, AI_TASKS.yaml là tracker song song bị bỏ quên, README.md/ROADMAP.md đông cứng từ giai đoạn đầu) và **2 banner trạng thái spec bị lệch** (DOC-003, DOC-004 — xem mục 3).

Đề xuất tổ chức lại (không thực hiện, chỉ đề xuất — xem `data/documents.json` mục KEEP/MOVE/MERGE/ARCHIVE):
- **ARCHIVE:** `AI_PROJECT_BRIEF.md` → `docs/human/archive/`
- **DEPRECATE hoặc REVIVE:** `AI_TASKS.yaml`
- **REWRITE:** `docs/spec/06_SOLUTION_STRUCTURE.md`, `README.md`
- **UPDATE header only:** `docs/spec/11_DATA_DB_SCHEMA.md`, `docs/design-system/WEB_UX_IMPROVEMENT_TASKS.md`
- **KEEP as-is:** phần lớn còn lại — `.claude-rules/`, `.claude/memory/`, `docs/spec/00-05,07-10,12-33`, `docs/nha-chung-cu/`, `TASKS.md`

## 7. Completed Work

- Multi-tenant DB-per-tenant + ADR-035 (bỏ Tenant_Id khỏi Config DB) — **hoàn tất, xác nhận qua code**.
- Sinh mã tự động (ADR-036), Nhật ký lỗi 500 (ADR-037), phân quyền theo phòng ban (AUTHZ-PB-4/5) — **hoàn tất, xác nhận qua code**.
- Form-engine rail workspace (master-detail), Config Sync engine F1 — **hoàn tất**.
- Web form flat styling (bỏ card, spacing/hairline) — CSS xác nhận khớp mô tả commit.

## 8. Partially Completed Work

- **Module NS_ (Nhân sự):** DB migration + form-engine wiring code-complete (Phase 1-3), nhưng **user chưa chạy migration/build-verify** trên môi trường thật — TASKS.md tự ghi nhận điều này.
- **Config Sync:** hoàn tất phạm vi F1, nhưng hook tự động gắn khi tạo tenant mới đang **bị chặn** (chưa có luồng tạo tenant để gắn vào).
- **RBAC/Menu (ADR-023):** phần tenant hoàn tất; đồng bộ master→tenant (`Sys_MenuCatalog` → `HT_ChucNang`) **cố ý hoãn** cho pha nâng cấp menu sau — đã ghi nhận đúng trong TASKS.md, không phải thiếu sót.
- **ADR-020 (audit-log bật/tắt theo bảng+màn):** TASKS.md tự nhận cờ `Audit_Enabled` chưa có.

## 9. Outstanding Work

- **Nhà chung cư (chung cư mới):** hoàn toàn ở dạng tài liệu (ADR, ERD, wireframe) — 0 dòng code, đúng như trạng thái "Draft — chờ duyệt" tự ghi.
- **Forgot/Reset Password:** UI + API nối đầy đủ nhưng backend là **STUB tự nhận** (không sinh token, không gửi email) — **và không hề được track ở TASKS.md/ADR nào**, đây là khoảng trống tài liệu thật sự mà audit này phát hiện ra (xem TASK-005).
- 4 bảng scaffolding (`Sys_Version`, `Sys_Cache_Invalidation`, `Sys_Perf_Log`, `Sys_Tenant`) không có tham chiếu C# nào và không có ghi chú trong TASKS.md — **cần hỏi trực tiếp chủ dự án** đây là dự trữ cho tương lai hay schema chết.

## 10. Technical Debt

12 mục nợ kỹ thuật (`DEBT-001..012`), nổi bật nhất:
- `FormEditorViewModel.cs` (WPF) — **3470 dòng**, gánh quá nhiều trách nhiệm (structure editing, drag/move, auto-save, điều hướng 5+ màn hình).
- Code chết tầng data cho "tab Permissions" đã gỡ khỏi WPF UI nhưng còn tồn tại ở data layer.
- 2 palette màu cục bộ ở WPF không khớp `DesignTokens.xaml` lẫn nhau.
- 39 chuỗi tiếng Việt hardcode chưa bọc i18n `L()` (đã có báo cáo tự động liệt kê chính xác vị trí).

## 11. Recommended Actions

**P0 (làm ngay):** Fix SEC-001 (XSS Menu Builder), SEC-002 (IDOR attachment).
**P1 (trước khi làm feature lớn tiếp theo):** Nối cache Validation/Event Engine (PERF-001), cache ViewRepository FK-join (PERF-002), fail-fast guard cho `Catalog:EncryptionKey` (SEC-003), field masking cho audit log (SEC-004), track rõ Forgot/Reset Password (TASK-005), test coverage cho HierarchyGuard + PermissionService (TEST-002).
**P2:** Cập nhật 2 banner spec lệch (DOC-003/004), dọn code chết WPF permission-tab, chốt chính sách `--dxbl-*`, hợp nhất palette màu WPF, thêm response compression, integration test cho auth/tenant.
**P3:** Refactor `FormEditorViewModel.cs`, đưa nốt control WPF sang DevExpress, dọn i18n còn thiếu, audit dependency riêng, thêm test project cho Blazor/WPF.

Chi tiết đầy đủ với evidence file:line: xem `data/recommendations.json` và dashboard.

## 12. Proposed Roadmap

| Phase | Nội dung | Module ảnh hưởng |
|---|---|---|
| Phase 0 — Immediate risk | Fix SEC-001, SEC-002 | Blazor Admin, Attachments API |
| Phase 1 — Stabilize | Nối cache PERF-001/002, guard SEC-003/004, track TASK-005, test HierarchyGuard/PermissionService | Validation/Event Engine, View Engine, Auth |
| Phase 2 — Architecture cleanup | Cập nhật spec lệch, dọn code chết WPF, chốt policy theme, dọn tài liệu AI cũ | Documentation, ConfigStudio WPF |
| Phase 3 — Security & performance hardening | SQL guard nhất quán, response compression, cache Context_Param | Backend Infrastructure |
| Phase 4 — Test coverage | Integration test backend, bUnit cho Blazor, unit test WPF ViewModel lớn | Backend, Blazor, WPF |
| Phase 5 — Documentation normalization | Áp dụng đề xuất tổ chức lại tài liệu ở mục 6 | docs/ |

---

*Audit này KHÔNG kết luận dự án "tốt hay xấu" — mục tiêu là dựng lại trạng thái kỹ thuật thật dựa trên bằng chứng (code/migration/config), có thể audit lại và so sánh tiến độ ở lần sau nhờ ID ổn định trong `data/*.json`.*
