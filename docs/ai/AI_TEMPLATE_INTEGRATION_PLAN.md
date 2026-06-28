# AI_TEMPLATE_INTEGRATION_PLAN — Tích hợp aitmpl.com vào ICare247

> Nguồn template: **aitmpl.com** (davila7/claude-code-templates, MIT, mã nguồn mở).
> Rào chắn bắt buộc: `BRAIN.md §11` + checklist `docs/ai/TEMPLATE_INTAKE.md`.
> Nguyên tắc: **không copy nguyên bản**; mâu thuẫn → điều chỉnh trước, không ghi đè rule.

---

## Tổng quan đề xuất

ICare247 đã có hệ AI-governance trưởng thành (BRAIN.md là SSOT cho Claude Code + Codex + Copilot;
10 file `.claude-rules/`; 2 agent; 6 command; skill `icare247-admin-ui`). Vì vậy **không nhập cả bộ
aitmpl** — chỉ rút **vài agent backend/SQL/security/test** đáng giá, **việt hóa + ép convention**, và
**tự viết** các agent đặc thù engine (Metadata/Form/Validation/Event) vì aitmpl không có.

Ba quyết định cốt lõi:
1. **UI:** KHÔNG dùng UI agent của aitmpl — đã có skill `icare247-admin-ui` mạnh hơn và khóa theme.
2. **Backend/SQL:** dùng template làm khung, **bắt buộc ép Dapper + MS SQL + Tenant_Id** trước khi nhập.
3. **Engine ICare247:** tự viết agent riêng (không có template tương đương).

---

## 1. Đánh giá nhóm template — phân mức

| Mức | Nhóm | Nguồn aitmpl | Ghi chú |
|---|---|---|---|
| **Bắt buộc** | Code Reviewer | `development-team/code-architect`, `code-explorer` | Gộp vào `/review-changes` sẵn có |
| **Bắt buộc** | Security Reviewer | `security/security-auditor`, `read-only-auditor` | Customize: bỏ K8s/cloud, tập trung JWT/SQLi/tenant |
| **Bắt buộc** | Performance Engineer | `performance-testing/performance-engineer` | Tập trung Dapper N+1, cache, SQL plan |
| **Bắt buộc** | Senior C# Developer | `programming-languages/dotnet-core-expert`, `csharp-developer` | Ép Dapper, cấm EF |
| **Bắt buộc** | SQL Server Expert | `database/database-optimizer` | Ép MS SQL, CTE/CROSS APPLY, tránh scalar UDF |
| **Nên có** | Solution Architect | `development-team/backend-architect`, `code-architect` | Bám Clean Architecture 4 lớp |
| **Nên có** | Database Architect | `database/database-architect` | Bám metadata schema `Sys_*/Ui_*/Val_*/Evt_*` |
| **Nên có** | Test Engineer | `development-team/test-generator`, `performance-testing/test-automator` | xUnit + `src/backend/tests` |
| **Nên có** | Technical Writer | `documentation/*` | Việt hóa, bám `docs/spec` |
| **Nên có** | API Designer | `development-team/backend-architect` (phần API) | RFC 7807, `07_API_CONTRACT.md` |
| **Có thể sau** | DevOps Engineer | `development-team/devops-engineer`, `security/github-actions-expert` | Khi tới Phase 5 CI/CD |
| **Có thể sau** | Prompt Engineer | (skill-creator nội bộ) | Tối ưu agent sau khi ổn |
| **Không phù hợp** | DevExpress Expert | _(không có template)_ | **Tự viết** + skill nội bộ |
| **Không phù hợp** | UI/UX Designer | `development-team/ui-ux-designer` | **Loại** — dùng `icare247-admin-ui` + `design-agent` |
| **Bỏ qua** | marketing/SEO/content/social/image/blockchain/game… | nhiều category | Không liên quan |

---

## Bộ agent nên dùng

Ký hiệu nguồn: 🟢 **Template** (nhập + customize) · 🔵 **Tự viết** (không có template) · 🟡 **Đã có nội bộ**.

| Agent | Vai trò | Khi nào dùng | Input | Output | Nguồn |
|---|---|---|---|---|---|
| Solution Architect | Đề xuất kiến trúc/luồng theo Clean Arch | Trước feature lớn | Yêu cầu, spec | ADR ngắn, sơ đồ lớp, file đụng tới | 🟢 backend-architect |
| Business Analyst | Map nghiệp vụ → engine/data | Khi yêu cầu mơ hồ | Yêu cầu nghiệp vụ | Phân tích + wireframe + flow | 🟡 `product-analyst` |
| Database Architect | Thiết kế bảng/quan hệ metadata | Thêm bảng/quan hệ | Nghiệp vụ, schema hiện tại | DDL nháp + tác động FK/cache | 🟢 database-architect |
| SQL Optimization | Tối ưu query | Query chậm/review | SQL + schema + plan | SQL viết lại (CTE/APPLY) + lý do | 🟢 database-optimizer |
| ASP.NET Core Backend | Sinh handler/endpoint CQRS | Thêm API | Spec API, DTO | Query/Command + Handler + endpoint | 🟢 dotnet-core-expert |
| GenericRepository/Dapper | Sinh repo Dapper | Thêm truy vấn dữ liệu | Bảng, cột, filter | Repo + SQL parameterized + ct | 🟢 csharp-developer (ép Dapper) |
| DevExpress UI | Dựng form/grid Blazor | Màn nghiệp vụ | Field config, layout | Razor + tuân theme khóa | 🔵 + skill `icare247-admin-ui` |
| Web Admin UI | Layout module admin | Màn danh sách/chi tiết | Module, entity | Layout + DxGrid/Form | 🟡 skill `icare247-admin-ui` |
| Metadata Engine | Logic `Sys_Table/Column/Relation` | Sửa metadata engine | Bảng meta, version | Code + cache-invalidation | 🔵 Tự viết |
| Form Engine | Render `Ui_Form/Section/Field` | Sửa form runtime | Ui_* config | Renderer + control-map | 🔵 Tự viết |
| Validation Rule Engine | Logic `Val_Rule*` + AST | Thêm rule type | Val_Rule schema | Rule handler AST | 🔵 Tự viết |
| Event Engine | Logic `Evt_*` + action | Thêm trigger/action | Evt_Definition | Action handler + exec-log | 🔵 Tự viết |
| Permission/RBAC | `Sys_Role/Permission` + policy | Sửa phân quyền | Role, permission | Policy + check phía API | 🔵 Tự viết |
| Cache/Redis | L1/L2 + invalidation | Thêm cache | CacheKeys, TTL | Code cache đúng `caching.md` | 🔵 Tự viết |
| Observability | `Sys_Perf/Error/Audit_Log` | Thêm log/correlation | Điểm log | Serilog + correlationId | 🔵 Tự viết |
| Security Review | Soát JWT/SQLi/tenant/secret | Trước merge | Diff | Danh sách lỗ hổng + fix | 🟢 security-auditor |
| Performance Review | Soát N+1/cache/SQL | Trước merge | Diff + query | Nút thắt + đề xuất | 🟢 performance-engineer |
| Test Case | Sinh xUnit | Sau khi có handler | Code + tiêu chí | Unit test + edge cases | 🟢 test-generator |
| Documentation | Cập nhật `docs/spec` | Sau thay đổi lớn | Diff, quyết định | Doc tiếng Việt | 🟢 documentation |
| Code Review | Soát convention ICare247 | Mọi diff | Diff | Checklist ✅/❌ + fix | 🟡 `/review-changes` |

---

## Bộ command nên tạo

| Command | Mục đích | Input | Output | Agent gọi |
|---|---|---|---|---|
| `/review-architecture` | Soát kiến trúc 1 thay đổi | đường dẫn/feature | nhận xét layer/DI/CQRS | Solution Architect |
| `/review-db-schema` | Soát bảng/quan hệ + index | bảng/migration | đề xuất schema/cache | Database Architect |
| `/generate-crud` | Sinh CRUD Dapper | tên bảng + cột | Repo+Query+Command+Handler | Backend + Dapper |
| `/generate-api` | Sinh endpoint + DTO | spec API | endpoint + DTO + RFC7807 | API Designer |
| `/generate-devexpress-form` | Sinh form Blazor | field config | Razor tuân theme | DevExpress UI + skill |
| `/optimize-sql` | Tối ưu query | SQL + schema | SQL viết lại + lý do | SQL Optimization |
| `/review-security` | Soát bảo mật diff | diff | lỗ hổng + fix | Security Review |
| `/review-performance` | Soát hiệu năng diff | diff/query | nút thắt + fix | Performance Review |
| `/generate-tests` | Sinh xUnit | handler/class | test + edge | Test Case |
| `/generate-docs` | Cập nhật spec/doc | diff | doc tiếng Việt | Documentation |
| `/analyze-metadata` | Phân tích `Sys_*` cho 1 form | form code | bản đồ metadata | Metadata Engine |
| `/build-dependency-graph` | Dựng `Sys_Dependency` | bảng/field | graph + thứ tự tính | Metadata Engine |
| `/generate-validation-rule` | Sinh `Val_Rule` + AST | field + điều kiện | rule + handler | Validation Engine |
| `/generate-event-action` | Sinh `Evt_*` | trigger + action | definition + handler | Event Engine |

> Lưu ý: `/review-changes`, `/start-session`, `/pick-task`, `/finish-task`, `/save-memory`, `/design` **đã có** — không tạo trùng.

---

## Cấu trúc thư mục đề xuất

```
.claude/
  agents/        # Agent (đã customize từ aitmpl HOẶC tự viết) — mỗi file 1 vai trò
  commands/      # Slash command — orchestration, gọi agent + đọc rule/spec
  skills/        # Skill chuyên sâu (icare247-admin-ui…) — quy chuẩn dày, có references/
  hooks/         # Hook (flag/remind memory) — KHÔNG thêm auto-commit
.claude-rules/   # Rule coding chi tiết (SSOT cấp 2) — agent đọc khi cần
docs/
  ai/            # ⭐ MỚI: governance template
    AI_TEMPLATE_INTEGRATION_PLAN.md   # file này — kế hoạch + danh sách được phép
    TEMPLATE_INTAKE.md                # checklist lọc bắt buộc
    prompts/                          # prompt system mẫu từng agent (tham chiếu)
  spec/          # Spec nghiệp vụ/kỹ thuật (00–24) — nguồn tra cứu
BRAIN.md         # SSOT — §11 governance template
CLAUDE.md / AGENTS.md / .github/copilot-instructions.md  # config per-agent (pointer)
```

Giải thích: `docs/ai/` là **nơi duy nhất** chứa quản trị template; agent/command nhập về để trong `.claude/`;
rule coding ở `.claude-rules/`; nghiệp vụ ở `docs/spec/`. KHÔNG tạo `prompts/`, `rules/`, `scripts/` ở gốc
(đã có chỗ tương ứng) để tránh phân mảnh.

---

## Thứ tự triển khai (5 phase)

### Phase 1 — Nền tảng AI Agent tối thiểu
- **Việc:** tạo `docs/ai/` (✅ đã có plan + intake); nhập 3 agent review: Code/Security/Performance (customize); thêm `/review-security`, `/review-performance`.
- **Kết quả:** mọi diff review được theo convention ICare247.
- **Rủi ro:** agent review aitmpl mặc định cloud/K8s, gây nhiễu. → **Giảm:** cắt scope còn .NET/Dapper/SQL/JWT/tenant.

### Phase 2 — Tích hợp metadata engine
- **Việc:** tự viết Metadata/Validation/Event Engine agent; thêm `/analyze-metadata`, `/build-dependency-graph`.
- **Kết quả:** agent hiểu `Sys_*/Ui_*/Val_*/Evt_*` và AST V1.
- **Rủi ro:** agent suy diễn sai schema. → **Giảm:** bắt buộc đọc `docs/spec/02,03,04` + đọc live DB trước (`feedback-verify-live-db-schema`).

### Phase 3 — Tự động sinh CRUD/API/UI
- **Việc:** nhập backend + Dapper + SQL agent (ép Dapper/MS SQL); thêm `/generate-crud`, `/generate-api`, `/optimize-sql`, `/generate-devexpress-form`.
- **Kết quả:** sinh nhanh tầng data/API/form đúng chuẩn.
- **Rủi ro:** **cao nhất** — template sinh EF Core/`SELECT *`/string-SQL/card-UI. → **Giảm:** cổng A của `TEMPLATE_INTAKE.md` + review bắt buộc trước commit.

### Phase 4 — Review / Test / Performance / Security
- **Việc:** nhập test agent; thêm `/generate-tests`, `/generate-docs`; chuẩn hóa luồng review trước merge.
- **Kết quả:** coverage + spec luôn cập nhật.
- **Rủi ro:** test giả lập (mock) sai dữ liệu thật. → **Giảm:** dữ liệu thật theo cấu hình (`feedback-always-ask-first`), không mock vô căn cứ.

### Phase 5 — CI/CD & GitHub workflow
- **Việc:** nhập devops/github-actions agent; dựng workflow build+test; tận dụng `/code-review ultra`.
- **Kết quả:** kiểm tra tự động trên PR.
- **Rủi ro:** workflow lộ secret / tự deploy. → **Giảm:** không secret trong repo; không auto-deploy; chỉ build/test.

---

## Prompt mẫu cho từng agent

> Lưu bản đầy đủ tại `docs/ai/prompts/`. Mọi prompt KẾ THỪA BRAIN.md §3 (Hard Constraints).

**Metadata Engine Expert**
> Bạn là chuyên gia Metadata Engine ICare247. Làm việc trên `Sys_Table/Sys_Column/Sys_Relation/Sys_Version/Sys_Cache_Invalidation`. Luôn đọc `docs/spec/02,04` + live DB trước khi sửa. Mọi thay đổi metadata phải kèm cache-invalidation theo version. Dapper + parameterized + Tenant_Id. Không EF, không SQL chuỗi. Comment tiếng Việt. Tái dùng shared, sửa logic 1 chỗ.

**Form Engine Expert**
> Bạn là chuyên gia Form Engine ICare247 (`Ui_Form/Ui_Section/Ui_Field/Ui_Control_Map`). Form render từ config, KHÔNG hardcode. UI tuân skill `icare247-admin-ui` (theme khóa). Đọc view hiện có trước khi thêm. Không tạo control trùng — mở rộng control-map. Comment tiếng Việt.

**Validation Engine Expert**
> Chuyên gia Validation Engine (`Val_Rule_Type/Val_Rule/Val_Rule_Field`) + AST Grammar V1. Chỉ AST-based, CẤM eval/dynamic-compile. Tuân null-rules `docs/spec/03`. Rule tái dùng qua type, không nhân bản. SQL tối ưu, parameterized. Comment tiếng Việt.

**Event Engine Expert**
> Chuyên gia Event Engine (`Evt_Trigger_Type/Evt_Definition/Evt_Action_Type/Evt_Action/Evt_Execution_Log`). Mọi action ghi `Evt_Execution_Log`. Không nuốt exception trong engine — bubble lên middleware. Action tái dùng qua type. Dapper + ct. Comment tiếng Việt.

**SQL Server Expert**
> Chuyên gia MS SQL Server cho ICare247. Tối ưu tốc độ: ưu tiên CTE/CROSS APPLY, **tránh scalar UDF**, dùng index hợp lý, đọc execution plan. CẤM `SELECT *`, CẤM SQL chuỗi — parameterized. Mọi query có `Tenant_Id` + `Is_Active`. Không đổi schema khi chưa confirm.

**ASP.NET Core + Dapper Expert**
> Senior .NET 9 backend ICare247. Clean Architecture 4 lớp (Api←App←Infra, Api không import Infra). CQRS/MediatR, naming `csharp-naming.md`. Data access **chỉ Dapper** + `IDbConnectionFactory`, `CommandDefinition` + `CancellationToken`. CẤM EF/`.Result`/`.Wait()`. String mặc định `string.Empty`. Tái dùng shared/common, không copy-paste. Comment tiếng Việt + file header.

**DevExpress UI Expert**
> Chuyên gia Blazor WASM + DevExpress cho ICare247. BẮT BUỘC tuân skill `icare247-admin-ui`: Fluent Light token KHÓA, KHÔNG override `--dxbl-*`, ≤3 màu, surface phẳng, 1 CTA/màn, bảng 70–80%, sticky header. Mọi chuỗi qua i18n (`Loc.L`). Đọc view hiện có trước khi viết.

**Code Reviewer**
> Review diff theo checklist ICare247 (`/review-changes`): layer, Dapper parameterized + Tenant_Id + không `SELECT *`, async + ct, naming, comment tiếng Việt, không secret. Báo ✅/❌ từng mục + fix cụ thể (file, dòng). Ưu tiên chỉ ra trùng lặp logic để gộp.

**Security Reviewer**
> Soát bảo mật diff ICare247: SQL injection (string-SQL), thiếu `Tenant_Id` (rò tenant), JWT/policy, secret commit, lỗ hổng phân quyền RBAC. Bỏ qua hạ tầng cloud/K8s. Output: lỗ hổng + mức + cách fix. Read-only, không sửa code.

**Performance Reviewer**
> Soát hiệu năng diff ICare247: N+1 Dapper, thiếu cache (`CacheKeys`), query không index/`SELECT *`, scalar UDF, async sai (`.Result`). Đề xuất CTE/CROSS APPLY khi hợp. Output: nút thắt + tác động + fix ưu tiên.

---

## Rủi ro và cách kiểm soát

| Rủi ro | Template dễ gây | Kiểm soát |
|---|---|---|
| Sinh EF Core / `SELECT *` / SQL chuỗi | backend/SQL generator | Cổng A1–A2 `TEMPLATE_INTAKE`; review bắt buộc |
| Phá theme/UI | ui-ux-designer, Tailwind | Loại UI template; chỉ dùng skill nội bộ (A9) |
| Sinh SSOT thứ 2 (CLAUDE.md riêng) | nhiều bộ template | A7 + BRAIN §11.1 |
| Quá chung chung, lệch domain | code-architect, generic | Ép đọc `docs/spec` + bám engine ICare247 |
| Tự commit/deploy/lộ secret | devops, git, ci | A8 + Phase 5 chỉ build/test |
| Trùng agent/command | review/architect/test | Cổng C — gộp, không nhân bản |
| Mock dữ liệu sai | test/data agent | Dữ liệu thật theo cấu hình (always-ask-first) |

**Rule bắt buộc thêm để AI không phá kiến trúc:** toàn bộ BRAIN.md §3 (11 Hard Constraints) + §11 governance + checklist intake. Đây là điều kiện vào của mọi template.

---

## Kết luận: tích hợp thế nào cho phù hợp ICare247

1. **Giữ BRAIN.md là SSOT duy nhất**; template chỉ là nguyên liệu, không phải luật.
2. **Nhập chọn lọc 5–6 agent** (backend, Dapper, SQL, security, performance, test) — **customize bắt buộc**, không copy.
3. **Tự viết** 6 agent engine đặc thù (Metadata/Form/Validation/Event/RBAC/Observability/Cache) vì aitmpl không có.
4. **Loại hoàn toàn** UI agent ngoài — dùng skill `icare247-admin-ui` + `design-agent`.
5. **Mọi file template qua `TEMPLATE_INTAKE.md`**; mâu thuẫn → điều chỉnh trước, ưu tiên kiến trúc/DB/security/style ICare247 trên template.
6. Triển khai theo 5 phase, **review trước commit**, không tự commit/push.
