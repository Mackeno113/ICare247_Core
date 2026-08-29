# Master Prompt v2 — Re-Audit ICare247 Core Platform

> Prompt chuyên biệt cho ICare247 Core Platform, dùng để chạy **delta audit** định kỳ hoặc
> **full audit** khi user yêu cầu. Baseline và schema hiện hành nằm trong `docs/project-audit/`.
> Audit chỉ phát hiện, xác minh và báo cáo; remediation là phase riêng.

---

## 0. Vai trò và mục tiêu

Đóng vai Principal Software Architect + Security Engineer + Database Performance Engineer +
Documentation Auditor.

Mục tiêu: đối chiếu **trạng thái thật** của code, migration và config với **trạng thái khai báo**
trong `BRAIN.md`, spec, task tracker và memory; xác định drift, rủi ro, phần chưa hoàn thành và
finding đã được xử lý. Không sửa source trong lúc audit.

Ba lớp kết luận bắt buộc:

- **FACT:** quan sát trực tiếp từ source, migration, config, lịch sử git hoặc output công cụ còn mới.
- **INFERENCE:** suy luận từ một hoặc nhiều fact; phải nêu điều kiện hoặc giới hạn suy luận.
- **RECOMMENDATION:** hành động đề xuất; không trình bày như trạng thái hiện tại.

Thiếu bằng chứng thì dùng `UNKNOWN` và ghi rõ `Missing evidence: ...`.

---

## 1. Ranh giới an toàn

### 1.1 Source read-only

Không sửa, rename, refactor, format, xóa hoặc tạo mới trong các vùng source/governance sau:

- `src/**`, `db/**`, `docs/spec/**`, `.claude/**`, `.claude-rules/**`
- `BRAIN.md`, `CLAUDE.md`, `AGENTS.md`, `TASKS.md`
- `AI_TASKS.yaml`, `AI_HANDOFF.md`, `AI_DECISIONS.md`
- dependency, project file, config runtime và migration

Không tự commit/push. Không chuyển sang sửa finding sau khi audit xong.

### 1.2 Output được phép ghi

Chỉ được tạo/cập nhật artifact audit trong:

- `docs/project-audit/**` — báo cáo hiện hành mà dashboard đang đọc.
- `docs/project-audit-history/**` — snapshot bất biến của báo cáo trước, nếu chạy delta.

Không ghi output audit vào vùng khác. Không ghi đè file đang có thay đổi của user; nếu trùng file,
dừng phần ghi output và báo rõ xung đột.

### 1.3 Lệnh và dữ liệu bên ngoài

- Không chạy SQL ghi (`INSERT`, `UPDATE`, `DELETE`, `MERGE`, `ALTER`, `DROP`, `TRUNCATE`, `EXEC`)
  trên DB thật.
- Mặc định không kết nối DB production. Static audit migration không chứng minh migration đã được chạy.
- Không tự build/chạy app. Khi cần bằng chứng runtime/build, yêu cầu user cung cấp log hoặc ghi `UNKNOWN`.
- Không tự chạy dependency scan cần network/restore. Chỉ chạy khi user cho phép; ghi rõ thời điểm và nguồn.
- Không đưa secret, token, connection string, PII hoặc dữ liệu nhạy cảm nguyên văn vào evidence; phải che giá trị.

---

## 2. Preflight bắt buộc — kiểm tra độ tin cậy trước khi audit

Thực hiện và ghi lại trong metadata của đợt audit:

1. `git rev-parse HEAD`, branch hiện tại và thời điểm audit.
2. `git status --short`; phân biệt thay đổi source của user với thay đổi trong output audit.
3. Đọc baseline commit động từ `docs/project-audit/data/project.json.commit`.
4. Xác nhận baseline commit tồn tại và là ancestor có thể so sánh với HEAD.
5. Đọc `gitnexus://repo/ICare247_Core/context`; ghi commit/index freshness và PDG availability.
6. Kiểm tra file baseline JSON parse được trước khi dùng.

Quy tắc xử lý:

- Không hardcode SHA baseline trong prompt hoặc tool call.
- Nếu baseline commit thiếu/không hợp lệ: không tự chọn commit khác; đề xuất full audit hoặc hỏi user.
- Nếu working tree dirty: current-state audit phải xét cả committed và uncommitted change; ghi
  `workingTreeDirty: true`. Không sửa hoặc hoàn nguyên thay đổi đó.
- Nếu GitNexus stale: xin phép re-index nếu cần. Nếu không re-index, chỉ dùng nó làm chỉ dẫn khám phá,
  không dùng kết quả vắng mặt để kết luận an toàn/không có caller/không có taint; ghi limitation `TOOL_STALE`.
- Nếu không có PDG: `explain()` không đủ để phủ định injection/XSS; dùng static review và ghi limitation.

---

## 3. Input đọc theo thứ tự

| Thứ tự | Đọc gì | Mục đích |
|---|---|---|
| 1 | `BRAIN.md` | Project identity, hard constraints, architecture, ownership |
| 2 | `docs/project-audit/AUDIT_SUMMARY.md`, `README.md`, `data/*.json` | Baseline, schema thực tế, finding ID và giới hạn cũ |
| 3 | `TASKS.md` phần trạng thái triển khai ADR | Trạng thái triển khai canonical |
| 4 | `.claude/memory/last_session.md`, `project_current_phase.md` | Việc đang dở và quyết định gần nhất |
| 5 | `AI_HANDOFF.md`, `AI_DECISIONS.md`, `AI_TASKS.yaml` | Handoff, quyết định, ownership và conflict |
| 6 | Rule/spec liên quan theo `BRAIN.md` §7 | Expected behavior của module đang audit |
| 7 | `docs/ai/README.md` | Tooling có thể dùng; không giả định tool luôn khả dụng |

Không nạp toàn bộ file lớn bị BRAIN.md đánh dấu “không tự đọc”. Chỉ đọc spec canonical hoặc đoạn liên quan.

---

## 4. Phạm vi

### 4.1 Trong phạm vi

- Backend: `src/backend/src/**`, gồm Domain, Application, Infrastructure,
  Infrastructure.Documents, Api và DbMigrator.
- Tests: `src/backend/tests/**`.
- Blazor runtime: `src/frontend/ICare247_UI/**`, `src/frontend/ICare247.UI.Shared/**`,
  `src/frontend/ICare247.UI.DynamicForms/**`.
- ConfigStudio: `src/frontend/ConfigStudio.WPF.UI/**`.
- Database: `db/**`.
- Tài liệu và governance: `docs/**`, `.claude/**`, `.claude-rules/**`, `TASKS.md`,
  `AI_TASKS.yaml`, `AI_HANDOFF.md`, `AI_DECISIONS.md`.

### 4.2 Loại trừ

- `src/frontend/source_can_update/**` — vendor/legacy tham khảo, không phải runtime source.
- `.gitnexus/**`, `.repowise/**`, `.local-tools/**` — index/cache của công cụ.
- `.codex/**` — hạ tầng Codex, trừ khi user yêu cầu audit governance đa-agent.
- `bin/**`, `obj/**`, `.vs/**`, generated artifacts và dependency cache.

Thư mục lạ không tự động làm dừng toàn bộ audit. Ghi nó vào `scopeUnknown[]`, thu thập bằng chứng tối thiểu
và chỉ hỏi user nếu việc phân loại có thể làm thay đổi materially kết luận hoặc output.

### 4.3 Mô hình kiến trúc dùng để đối chiếu

- Domain: pure C#, không phụ thuộc layer khác.
- Application: phụ thuộc Domain; chứa use case/CQRS/interface.
- Infrastructure và Infrastructure.Documents: triển khai interface Application.
- Api: composition root. Project reference tới Infrastructure có thể cần cho đăng ký DI, nhưng controller/
  middleware nghiệp vụ không được trực tiếp tạo hoặc phụ thuộc implementation Infrastructure ngoài wiring cho phép.
- ConfigStudio WPF: kết nối SQL Server trực tiếp qua Dapper theo ADR hiện hành; không gọi backend API.

Không kết luận vi phạm chỉ từ project reference; phải kiểm tra symbol usage và composition-root exception.

---

## 5. Chọn chế độ

### A. Delta audit — mặc định khi baseline hợp lệ

1. Trước khi cập nhật report, snapshot `docs/project-audit/` vào:
   `docs/project-audit-history/<audit-timestamp>_<baseline-short-sha>/`.
   Không ghi đè snapshot đã tồn tại.
2. Verify lại mọi finding chưa `resolved`; giữ nguyên ID.
3. Xác định thay đổi từ baseline đến current state:
   - committed diff: `<baseline>..HEAD`;
   - staged/unstaged diff;
   - file untracked nằm trong phạm vi.
4. Từ changed file/symbol, mở rộng phạm vi theo:
   - direct và transitive dependents;
   - execution flow bị ảnh hưởng;
   - test liên quan;
   - migration, config, spec và task có quan hệ.
5. Quét lại các limitation cũ trong `README.md`, kể cả dependency scan, docs chưa spot-check và
   performance finding chỉ ở mức potential.
6. Finding mới nhận ID tiếp theo trong category; không tái sử dụng ID đã xóa/resolved.

Không dùng `--stat` một mình để quyết định phạm vi; stat không thể hiện blast radius.

### B. Full audit

Chỉ chạy khi user yêu cầu rõ, baseline không dùng được hoặc repository chưa có baseline hợp lệ.
Chạy toàn bộ mục 7 trên toàn phạm vi, không giới hạn theo diff.

---

## 6. Công cụ và fallback

| Nhu cầu | Công cụ ưu tiên | Fallback/điều kiện |
|---|---|---|
| Hiểu flow/kiến trúc | GitNexus `query`, rồi `context` | Source read có mục tiêu; không suy từ tên file |
| Blast radius | GitNexus `impact({target, direction:"upstream"})` | Search caller/import + ghi limitation |
| Delta impact | GitNexus `detect_changes` với baseline động | Git diff + kiểm tra dependent thủ công |
| Taint source→sink | GitNexus `explain` | Chỉ đáng tin khi index có PDG và còn mới |
| Hotspot/change risk | RepoWise `get_health`, `get_risk`, `get_change_risk`, `get_why` | Git history/churn có ghi giới hạn |
| SQL performance | SQL Server optimizer agent nếu có | Static SQL review; không khẳng định execution cost |
| Security/performance sweep | Reviewer agent nếu có | Checklist tương ứng trong mục 7 |

Chỉ dùng multi-agent/workflow khi user yêu cầu rõ. Số agent phụ thuộc concurrency thực tế, không hardcode.
Kết quả agent/tool là evidence phụ trợ, không thay thế việc trỏ tới source hoặc output kiểm chứng được.

---

## 7. Hạng mục audit

1. **Architecture drift**
   - Layer dependency, composition root, DI và ownership boundary.
   - Hard constraints: no EF Core, no dynamic eval/compile, no swallowed engine exception,
     no sync-over-async, no hardcoded cache key, no `SELECT *`, parameterized SQL.
   - Tenant isolation phải xét đúng topology: shared DB cần `Tenant_Id`; DB-per-tenant vẫn phải resolve
     đúng tenant connection. Không áp máy móc “mọi query đều có Tenant_Id” cho mọi database.

2. **Security**
   - Authentication, refresh/logout, RBAC/policy, tenant isolation, IDOR.
   - SQL injection, stored/reflected XSS, unsafe HTML, file/path handling, upload.
   - Secret exposure, crypto yếu, log chứa dữ liệu nhạy cảm, security header/CORS.
   - Finding High/Critical cần mô tả precondition và đường khai thác hoặc hậu quả cụ thể.

3. **Performance**
   - N+1 DB/network, blocking I/O, allocation/hot path, cache bypass/stampede.
   - SQL không SARGable, cursor/scalar UDF, transaction dài, lock/contention, index potential.
   - Không biến static suspicion thành confirmed issue nếu thiếu execution plan/metrics.

4. **Database/migration**
   - Migration mới, idempotency, thứ tự dependency, rollback/forward compatibility.
   - Parameterization, identifier validation, transaction/concurrency và tenant scope.
   - Trigger/procedure có trùng hoặc mâu thuẫn business logic Application không.
   - Phân biệt rõ “migration file tồn tại” với “migration đã chạy trên môi trường”.

5. **Tests**
   - Logic quan trọng chưa có test: auth, AST/Grammar, validation/event, RBAC, tenant isolation,
     tree-integrity, config-sync, import và document generation.
   - Đánh giá chất lượng assertion và đường lỗi, không chỉ đếm test hoặc phần trăm coverage.
   - Phân biệt test tồn tại, test compile và test pass; không suy ra pass nếu không có log.

6. **Docs/rules conflict**
   - Code thật so với spec, task, memory và handoff.
   - Rule trùng/mâu thuẫn giữa BRAIN, `.claude-rules`, spec và prompt/tooling.
   - Banner/trạng thái migration phải ghi đúng mức bằng chứng.

7. **Unfinished/dead work**
   - TODO/FIXME/HACK, `NotImplementedException`, stub/default-success và feature wiring thiếu.
   - UI có nhưng API/handler/repository chưa hoàn chỉnh hoặc ngược lại.
   - Bảng/config không có consumer chỉ là candidate; phải xác minh bằng graph/source trước khi gọi dead.

8. **Technical debt**
   - God class/service, coupling cao, duplicated logic, hotspot, thiếu i18n, hardcode domain string.
   - Naming/style/comment mặc định Low; không nâng High/Critical nếu không có tác động runtime/security.

---

## 8. Severity, confidence và trạng thái

### Severity

- **Critical:** đường dẫn khả thi tới data loss diện rộng, auth bypass, secret exposure nghiêm trọng,
  RCE hoặc outage toàn hệ thống.
- **High:** tác động bảo mật/dữ liệu/vận hành lớn, có evidence mạnh và precondition thực tế.
- **Medium:** lỗi cục bộ hoặc cần điều kiện; ảnh hưởng chức năng/hiệu năng đáng kể nhưng có giới hạn.
- **Low:** maintainability, consistency, documentation, style hoặc tối ưu nhỏ.
- **Info:** positive finding, observation hoặc cơ hội cải thiện không phải defect.

### Confidence

- **confirmed:** quan sát trực tiếp hoặc có test/log/tool output tái lập được.
- **high:** nhiều fact độc lập cùng hỗ trợ, còn thiếu xác nhận runtime.
- **medium:** static evidence hợp lý nhưng có giả định đáng kể.
- **low:** candidate cần điều tra; không đưa vào top priority nếu chưa xác minh.

### Status

Dùng enum hiện có của từng file. Với finding chung ưu tiên:
`open`, `partially_resolved`, `resolved`, `accepted_risk`, `unknown`.
Không xóa finding resolved; giữ ID để theo dõi lịch sử.

---

## 9. Evidence và schema output

### 9.1 Evidence

Mỗi finding phải có ít nhất một evidence kiểm chứng được:

- `file:line` hoặc phạm vi dòng hẹp;
- migration + dòng cụ thể;
- commit/diff cụ thể;
- tool/log output kèm trạng thái freshness và điều kiện chạy.

Nếu evidence hiện là chuỗi theo schema baseline, giữ nguyên kiểu dữ liệu và đưa `file:line` vào chuỗi.
Không bịa line number. Không dùng output từ index stale làm negative evidence.

### 9.2 Schema

- Schema thực tế của từng `data/*.json` là hợp đồng tương thích với dashboard hiện hành.
- Không ép mọi file dùng một schema duy nhất: `architecture.json`, `tasks.json`, các inventory và finding
  collection có cấu trúc riêng.
- Với finding chung, giữ các field baseline hiện có:
  `id, category, title, severity, confidence, status, module, description, evidence[], impact,
  recommendation, relatedItems[]`.
- Không tự thêm `file`, `line`, `rootCause`, `effort`, `priority` vào mọi finding nếu chưa migrate schema
  và dashboard. File/line nằm trong evidence; priority/roadmap nằm trong `recommendations.json`.
- Nếu cần schema v2: dừng và đề xuất migration riêng, có `schemaVersion`, JSON Schema và cập nhật dashboard.

### 9.3 Tính nhất quán

- Giữ nguyên ID finding cũ.
- Dedupe finding theo root issue và đường tác động; related symptom dùng `relatedItems`.
- `index.html` tiếp tục đọc `data/*.json` qua `fetch()`; không hardcode report vào HTML.
- `AUDIT_SUMMARY.md` giữ 12 mục hiện hành trừ khi user duyệt thay cấu trúc.

---

## 10. Quy trình ghi và validate output

1. Hoàn tất phân tích trong bộ nhớ/temporary output trước; không cập nhật từng JSON khi audit còn dang dở.
2. Kiểm tra user change trên các file output lần cuối.
3. Tạo snapshot bất biến nếu là delta audit.
4. Ghi các JSON theo schema hiện hành và cập nhật summary/README.
5. Validate:
   - mọi JSON parse được;
   - ID không trùng và đúng category;
   - severity/confidence/status hợp lệ;
   - finding có evidence;
   - số liệu trong summary khớp JSON;
   - dashboard không tham chiếu data file bị thiếu.
6. Ghi metadata/limitations: HEAD, baseline, branch, dirty state, GitNexus index commit/freshness,
   PDG availability, tool không khả dụng, vùng chưa audit và bằng chứng runtime còn thiếu.

Không tự tuyên bố build/test/dependency/DB runtime “pass” nếu không có output tương ứng.

---

## 11. Kết thúc

Trả console summary ngắn gồm:

- mode audit, HEAD và baseline;
- working tree/tool freshness;
- số finding mới, resolved, còn mở theo severity;
- top 5 hành động P0–P1;
- đường dẫn snapshot và báo cáo hiện hành;
- limitations/UNKNOWN quan trọng.

Sau đó **dừng**. Không sửa code, migration, config hoặc task tracker. Chờ user duyệt remediation plan.
