# ICare247 — Bản Nâng Cấp AI-Assisted Development Workflow

> **Mục tiêu:** Xây dựng hệ thống tài liệu và cấu hình để AI agent (Claude Code) có thể
> làm việc tự chủ, nhớ ngữ cảnh xuyên suốt các phiên, tự quản lý tiến độ,
> và duy trì chất lượng code nhất quán — không cần nhắc lại mỗi lần mở terminal.

---

## Mục lục

1. [Chẩn đoán hiện trạng](#1-chẩn-đoán-hiện-trạng)
2. [Kiến trúc tài liệu mới](#2-kiến-trúc-tài-liệu-mới)
3. [CLAUDE.md — Thiết kế lại](#3-claudemd--thiết-kế-lại)
4. [Hệ thống Memory — Bộ nhớ dài hạn](#4-hệ-thống-memory--bộ-nhớ-dài-hạn)
5. [TASKS.md — Nâng cấp Task Tracking](#5-tasksmd--nâng-cấp-task-tracking)
6. [Hooks — Cổng chất lượng tự động](#6-hooks--cổng-chất-lượng-tự-động)
7. [Custom Skills — Lệnh tắt cho workflow lặp lại](#7-custom-skills--lệnh-tắt-cho-workflow-lặp-lại)
8. [Workflow vận hành hàng ngày](#8-workflow-vận-hành-hàng-ngày)
9. [Cấu trúc file đề xuất](#9-cấu-trúc-file-đề-xuất)
10. [Lộ trình triển khai](#10-lộ-trình-triển-khai)

---

## 1. Chẩn đoán hiện trạng

### Điểm tốt đã có

| Thành phần | Đánh giá |
|---|---|
| `CLAUDE.md` | Đầy đủ coding rules, naming, architecture, Dapper patterns |
| `docs/spec/` (9 file) | Spec chi tiết cho DB, Grammar, Engine, API, Conventions |
| `docs/ICare247 Config Studio/CLAUDE.md` | Memory riêng cho WPF module, rất chi tiết |
| `TASKS.md` | Tracking theo phase, có decisions log |
| `.claude/settings.local.json` | Đã whitelist một số lệnh build |

### Vấn đề cần giải quyết

| # | Vấn đề | Hậu quả |
|---|---|---|
| 1 | **CLAUDE.md quá dài (~310 dòng), đơn khối** | Claude Code load toàn bộ mỗi session → tốn context window cho những phần không liên quan đến task hiện tại. Khó bảo trì khi project lớn lên. |
| 2 | **Không có hệ thống Memory** | Mỗi session mới, agent quên sạch: đã làm gì, gặp bug gì, quyết định thiết kế gì. User phải nhắc lại ngữ cảnh mỗi lần. |
| 3 | **Không có Hooks** | Không có cổng chất lượng tự động. Agent có thể commit code không build được, vi phạm coding rules mà không ai chặn. |
| 4 | **Không có Custom Skills** | Workflow lặp lại (tạo CQRS feature, tạo repository, tạo WPF screen) phải mô tả bằng lời mỗi lần. |
| 5 | **TASKS.md thiếu trạng thái session** | Agent không biết "phiên trước dừng ở đâu", "file nào đã sửa nhưng chưa commit". |
| 6 | **Docs reference trong CLAUDE.md trỏ sai path** | CLAUDE.md ghi `docs/00_PROJECT_OVERVIEW.md` nhưng thực tế file nằm ở `docs/spec/00_PROJECT_OVERVIEW.md`. |
| 7 | **Hai sub-project dùng chung TASKS.md** | Backend (.NET) và ConfigStudio (WPF) trộn lẫn tasks → khó phân tách khi agent làm từng phần. |

---

## 2. Kiến trúc tài liệu mới

### Nguyên tắc thiết kế

```
┌─────────────────────────────────────────────────────────────┐
│  CLAUDE.md (root)                                           │
│  = Bản đồ tổng quan + router                                │
│  ≤ 100 dòng — chỉ chứa:                                     │
│    • Project identity                                        │
│    • Tech stack (bảng tóm tắt)                                │
│    • Luật bất biến (≤ 15 dòng)                                │
│    • Pointer đến các file chi tiết                            │
│  Agent chỉ đọc thêm file chi tiết KHI CẦN                    │
└─────────────────────────────────────────────────────────────┘
         │
         ├── .claude/                        ← Cấu hình Claude Code
         │   ├── settings.local.json         ← Permissions (đã có)
         │   ├── hooks/                      ← Pre/post hooks
         │   └── skills/                     ← Custom slash commands
         │
         ├── .claude-rules/                  ← Coding rules tách nhỏ
         │   ├── architecture.md             ← Layer dependency, DI
         │   ├── csharp-naming.md            ← Naming conventions
         │   ├── dapper-patterns.md          ← SQL/Dapper rules
         │   ├── caching.md                  ← Cache key, TTL, hybrid
         │   ├── ast-grammar.md              ← AST/Grammar rules
         │   ├── api-response.md             ← Response format, ProblemDetails
         │   ├── comment-rules.md            ← File header, Vietnamese comments
         │   └── wpf-configstudio.md         ← WPF-specific rules
         │
         ├── docs/spec/                      ← Specification (đã có, giữ nguyên)
         │
         └── ~\.claude\projects\             ← Memory (per-user, ngoài repo)
             └── D--ICare247-Core\memory\
                 ├── MEMORY.md               ← Index
                 └── *.md                    ← Memory files
```

### Tại sao tách nhỏ?

Claude Code có cơ chế **CLAUDE.md cascade**: nó tự động đọc `CLAUDE.md` ở root, nhưng các file trong `.claude-rules/` chỉ được load khi agent đọc chúng. Điều này giúp:

1. **Tiết kiệm context window**: Agent làm task Dapper chỉ cần đọc `dapper-patterns.md`, không cần load WPF rules.
2. **Dễ bảo trì**: Mỗi file < 80 dòng, dễ review và cập nhật.
3. **Tránh xung đột**: Nhiều người/agent sửa song song không conflict cùng 1 file.

---

## 3. CLAUDE.md — Thiết kế lại

### Phiên bản mới (đề xuất)

```markdown
# ICare247 Core Platform — AI Agent Configuration

## Project Identity
- **Dự án:** ICare247 Core Platform — Enterprise metadata-driven low-code form engine
- **Code:** C# (.NET 9) | **Comment:** Tiếng Việt | **Pattern:** Clean Architecture + CQRS

## Tech Stack
| Thành phần | Dùng | KHÔNG dùng |
|---|---|---|
| Backend | .NET 9 / ASP.NET Core 9 | - |
| Frontend | Blazor WASM + DevExpress | - |
| DB | MS SQL Server | MySQL, PostgreSQL |
| Data Access | **Dapper** | **EF Core (cấm)** |
| Cache | MemoryCache + Redis | - |
| Logging | Serilog + OpenTelemetry | Console.WriteLine |
| Auth | JWT + Policy-based | - |

## Luật bất biến (KHÔNG ngoại lệ)
1. Domain layer = pure C#, không import gì
2. Application chỉ import Domain
3. Infrastructure import Application
4. Api KHÔNG import Infrastructure trực tiếp (trừ composition root)
5. KHÔNG dùng EF Core — chỉ Dapper
6. Mọi SQL = parameterized (không string interpolation)
7. Mọi query/cache key phải có Tenant_Id
8. Async/await xuyên suốt — không .Result, không .Wait()
9. CancellationToken truyền xuyên suốt
10. Không eval / dynamic compile — chỉ AST-based

## Quy tắc chi tiết → đọc theo nhu cầu
| File | Nội dung |
|---|---|
| `.claude-rules/architecture.md` | Layer dependency, DI registration, CQRS pattern |
| `.claude-rules/csharp-naming.md` | Naming conventions, file header template |
| `.claude-rules/dapper-patterns.md` | SQL patterns, connection factory, async methods |
| `.claude-rules/caching.md` | CacheKeys.cs, L1/L2 TTL, hybrid strategy |
| `.claude-rules/ast-grammar.md` | AST nodes, operators, functions, null rules |
| `.claude-rules/api-response.md` | Response format, ProblemDetails, RFC 7807 |
| `.claude-rules/comment-rules.md` | File header, class/method/logic block comments |
| `.claude-rules/wpf-configstudio.md` | Prism, MaterialDesign, MVVM, navigation |

## Specification → đọc khi cần tra cứu
| File | Nội dung |
|---|---|
| `docs/spec/00_PROJECT_OVERVIEW.md` | Tổng quan, mục tiêu |
| `docs/spec/02_DATABASE_SCHEMA.md` | Bảng DB, columns |
| `docs/spec/03_GRAMMAR_V1_SPEC.md` | Grammar V1, AST nodes |
| `docs/spec/04_ENGINE_SPEC.md` | 4 engines: Metadata, AST, Validation, Event |
| `docs/spec/07_API_CONTRACT.md` | API endpoints, request/response |

## Task Tracking
- Backend tasks: `TASKS.md` (git root)
- ConfigStudio tasks: `docs/ICare247 Config Studio/TASKS_WPF.md`
- Khi bắt đầu session → đọc TASKS.md, xác nhận task với user
- Khi bắt đầu task → move sang 🔴 In Progress
- Khi hoàn thành → move sang ✅ Done + commit

## Session Protocol
1. Đọc `TASKS.md` để biết việc cần làm
2. Đọc memory (nếu có context liên quan từ phiên trước)
3. Hỏi user: "Hôm nay làm task nào?"
4. Đọc `.claude-rules/` liên quan đến task
5. Đọc `docs/spec/` nếu cần tra cứu schema/API
6. Code → test → commit
7. Cập nhật TASKS.md + lưu memory nếu có quyết định quan trọng
```

### Điểm khác biệt so với bản cũ

| Khía cạnh | Bản cũ | Bản mới |
|---|---|---|
| Độ dài | ~310 dòng | ~80 dòng |
| Coding rules | Inline tất cả | Pointer đến `.claude-rules/` |
| Code examples | Nhiều ví dụ trong CLAUDE.md | Ví dụ nằm trong file rules chi tiết |
| Session protocol | Chỉ có "Tóm tắt Flow" cuối file | Section riêng với 7 bước rõ ràng |
| Multi-project | Chung 1 TASKS.md | Tách riêng Backend/WPF |
| Docs path | Sai (`docs/00_...`) | Đúng (`docs/spec/00_...`) |

---

## 4. Hệ thống Memory — Bộ nhớ dài hạn

### Memory là gì?

Claude Code có built-in memory system lưu ở `~/.claude/projects/<project-id>/memory/`.
Memory **tồn tại giữa các session** — agent mở terminal mới vẫn nhớ.

### Cấu trúc đề xuất

```
~/.claude/projects/D--ICare247-Core/memory/
├── MEMORY.md                          ← Index (≤ 200 dòng)
├── user_profile.md                    ← Thông tin về user/team
├── project_architecture_decisions.md  ← ADR quan trọng
├── project_current_phase.md           ← Phase hiện tại, priorities
├── feedback_coding_style.md           ← User corrections về style
├── feedback_workflow.md               ← User corrections về process
├── reference_external_systems.md      ← Links to Jira, Grafana, etc.
└── project_known_issues.md            ← Bugs/workarounds đang tồn tại
```

### MEMORY.md (Index)

```markdown
# ICare247 Memory Index

## User
- [user_profile.md](user_profile.md) — Vai trò, kinh nghiệm, preferences

## Project
- [project_architecture_decisions.md](project_architecture_decisions.md) — Quyết định kiến trúc
- [project_current_phase.md](project_current_phase.md) — Phase và ưu tiên hiện tại
- [project_known_issues.md](project_known_issues.md) — Bugs/workarounds

## Feedback
- [feedback_coding_style.md](feedback_coding_style.md) — Corrections về coding style
- [feedback_workflow.md](feedback_workflow.md) — Corrections về quy trình

## Reference
- [reference_external_systems.md](reference_external_systems.md) — Links hệ thống ngoài
```

### Khi nào agent tự lưu memory?

| Tình huống | Loại memory | Ví dụ |
|---|---|---|
| User nói "đừng làm X" | `feedback` | "Không tự thêm try-catch ở repository layer" |
| Gặp bug lạ phải workaround | `project` | "Prism 9 breaking change: dùng Prism.Navigation.Regions" |
| User cho context nền | `user` | "Tôi là sole developer, review code một mình" |
| Quyết định thiết kế quan trọng | `project` | "Chọn Scalar thay Swagger UI vì..." |
| Phase/sprint thay đổi | `project` | "Bắt đầu Phase 2 — Grammar V1 từ 2026-03-20" |

### Khi nào agent đọc memory?

- **Luôn luôn** khi bắt đầu session mới (đọc MEMORY.md index)
- Khi task liên quan đến topic đã có memory
- Khi user nói "nhớ lại" hoặc "lần trước mình đã..."

---

## 5. TASKS.md — Nâng cấp Task Tracking

### Vấn đề hiện tại

TASKS.md hiện tại tốt cho tracking tổng thể nhưng thiếu:
- **Session state**: Agent không biết phiên trước dừng ở đâu
- **Micro-tasks**: Không track các bước nhỏ trong 1 task lớn
- **Blockers**: Không ghi lại lý do tại sao task bị stuck

### Cấu trúc đề xuất

```markdown
# ICare247 Core — Task Tracking

## 🔴 Đang làm (In Progress)

### TASK-012: Implement FormRepository (Dapper)
- **Assigned:** Claude Code session 2026-03-17
- **Branch:** feature/form-repository
- **Depends on:** TASK-011 (SqlConnectionFactory) ✅
- **Checklist:**
  - [x] Tạo IFormRepository trong Application
  - [x] Tạo FormRepository trong Infrastructure
  - [ ] Implement GetByCodeAsync
  - [ ] Implement GetByFormIdAsync
  - [ ] Register trong DependencyInjection.cs
  - [ ] Build test
- **Notes:** Cần check schema Ui_Form có cột Platform không → xem docs/spec/02

---

## 🟡 Cần làm (Todo)

### Phase 1 — Foundation
- [ ] TASK-013: Implement FieldRepository (Dapper)
- [ ] TASK-014: Implement HybridCacheService
- [ ] TASK-015: Tạo GetFormByCodeQuery + Handler
...

## ✅ Đã xong (Done)
...

## 📝 Decisions Log
| Ngày | Quyết định | Lý do |
|---|---|---|
...
```

### Khác biệt chính

| Khía cạnh | Bản cũ | Bản mới |
|---|---|---|
| Task ID | Không có | `TASK-XXX` để dễ reference |
| Checklist con | Không có | Có — agent track tiến độ micro |
| Branch | Không ghi | Ghi rõ branch đang dùng |
| Dependencies | Không rõ | Ghi rõ depends on task nào |
| Session info | Không có | Ghi session date, agent notes |
| Separation | Chung 1 file | Backend: `TASKS.md`, WPF: riêng |

### TodoWrite Integration

Claude Code có built-in TodoWrite tool để track micro-tasks trong session. Kết hợp:
- **TASKS.md** = tracking dài hạn, xuyên sessions, commit được
- **TodoWrite** = tracking ngắn hạn trong session hiện tại, tự động

---

## 6. Hooks — Cổng chất lượng tự động

### Hooks là gì?

Claude Code hooks là shell commands chạy tự động khi agent thực hiện hành động.
Ví dụ: mỗi khi agent sửa file `.cs`, hook tự chạy `dotnet build` để kiểm tra.

### Cấu hình đề xuất

File: `~/.claude/projects/D--ICare247-Core/settings.json`

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "description": "Build sau khi edit file C#",
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "bash -c 'if echo \"$CLAUDE_FILE_PATH\" | grep -q \"\\.cs$\"; then dotnet build src/backend/ICare247.slnx --no-restore -v q 2>&1 | tail -5; fi'"
          }
        ]
      }
    ],
    "PreToolUse": [
      {
        "description": "Chặn tạo file không đúng namespace",
        "matcher": "Write",
        "hooks": [
          {
            "type": "command",
            "command": "bash -c 'if echo \"$CLAUDE_FILE_PATH\" | grep -qE \"\\.cs$\" && ! head -5 \"$CLAUDE_FILE_PATH\" 2>/dev/null | grep -q \"// File\"; then echo \"BLOCK: File .cs thiếu header bắt buộc\"; exit 1; fi'"
          }
        ]
      }
    ]
  }
}
```

### Các hook đề xuất

| Hook | Trigger | Mục đích |
|---|---|---|
| **Build check** | Sau khi edit `.cs` | Đảm bảo code compile |
| **File header check** | Trước khi write `.cs` | Đảm bảo có header comment |
| **EF Core guard** | Trước khi write `.cs` | Chặn nếu import `Microsoft.EntityFrameworkCore` |
| **SQL injection guard** | Trước khi write `.cs` | Cảnh báo nếu thấy string interpolation trong SQL |
| **Namespace check** | Sau khi write `.cs` | Verify namespace match folder path |

### Lưu ý quan trọng

- Hooks KHÔNG thay thế code review — chỉ là lớp bảo vệ đầu tiên
- Hook nên chạy nhanh (< 5 giây) để không block workflow
- Hook nên trả message rõ ràng khi fail để agent tự sửa
- Bắt đầu với 1-2 hook cơ bản, thêm dần khi workflow ổn định

---

## 7. Custom Skills — Lệnh tắt cho workflow lặp lại

### Skills là gì?

Custom skills là các prompt template mà user gọi bằng `/tên-skill`.
Thay vì mô tả bằng lời "tạo cho tôi 1 CQRS query gồm Query record, Handler, Validator...",
user chỉ cần gõ `/new-query GetFormByCode`.

### Skills đề xuất

#### `/new-query` — Tạo CQRS Query mới

```markdown
---
name: new-query
description: Tạo CQRS Query + Handler + Validator theo naming convention ICare247
---

Tạo CQRS Query feature với đầy đủ files theo convention:

**Input:** Tên query (VD: GetFormByCode)

**Output:** 3 files trong `Application/Features/{Module}/Queries/{QueryName}/`

1. `{QueryName}Query.cs`
   - Record implement `IRequest<{ResponseDto}>`
   - File header bắt buộc
   - XML comment tiếng Việt

2. `{QueryName}QueryHandler.cs`
   - Class implement `IRequestHandler<{QueryName}Query, {ResponseDto}>`
   - Constructor inject repository interface
   - Async method với CancellationToken
   - Logic blocks với comment sections

3. `{QueryName}QueryValidator.cs`
   - Class extend `AbstractValidator<{QueryName}Query>`
   - Basic validation rules

Tuân thủ tất cả rules trong `.claude-rules/architecture.md` và `.claude-rules/csharp-naming.md`.
```

#### `/new-repo` — Tạo Repository mới

```markdown
---
name: new-repo
description: Tạo Repository interface + implementation (Dapper) theo convention ICare247
---

Tạo Repository với đầy đủ files:

**Input:** Entity name (VD: Form)

**Output:**
1. `Application/Common/Interfaces/I{Entity}Repository.cs` — Interface
2. `Infrastructure/Repositories/{Entity}Repository.cs` — Dapper implementation
3. Update `Infrastructure/DependencyInjection.cs` — Register DI

Tuân thủ `.claude-rules/dapper-patterns.md`. Mọi method phải:
- Suffix Async
- Nhận CancellationToken
- Filter Tenant_Id + Is_Active
- Dùng parameterized query
- Dùng IDbConnectionFactory
```

#### `/new-wpf-screen` — Tạo WPF Screen mới

```markdown
---
name: new-wpf-screen
description: Tạo WPF View + ViewModel + Module registration theo Prism pattern
---

Tạo screen mới cho ConfigStudio:

**Input:** Screen name + Module name (VD: GrammarLibrary, Grammar)

**Output:**
1. `Views/{ScreenName}View.xaml` — MaterialDesign layout, AutoWireViewModel
2. `ViewModels/{ScreenName}ViewModel.cs` — Kế thừa ViewModelBase, INavigationAware
3. Update `{Module}Module.cs` — RegisterForNavigation
4. Update `Core/Constants/ViewNames.cs` nếu cần

Tuân thủ `docs/ICare247 Config Studio/CLAUDE.md` sections 4-8.
```

#### `/review-task` — Review và cập nhật TASKS.md

```markdown
---
name: review-task
description: Đọc TASKS.md, kiểm tra trạng thái, đề xuất task tiếp theo
---

1. Đọc TASKS.md
2. Đọc memory `project_current_phase.md` (nếu có)
3. Kiểm tra git log gần nhất để biết task nào đã commit
4. Tổng hợp:
   - Task nào đang In Progress? Còn bước nào chưa xong?
   - Task nào là Todo tiếp theo hợp lý (theo dependency)?
   - Có blocker nào từ phiên trước không?
5. Trình bày cho user và hỏi "Hôm nay làm task nào?"
```

#### `/session-end` — Kết thúc session gọn gàng

```markdown
---
name: session-end
description: Wrap up session — commit, update TASKS.md, lưu memory
---

1. Kiểm tra có file chưa commit không (git status)
2. Nếu có: hỏi user commit hay stash
3. Cập nhật TASKS.md:
   - Task đã hoàn thành → ✅ Done
   - Task đang dở → ghi note tiến độ
4. Lưu memory nếu có quyết định thiết kế mới
5. Tóm tắt ngắn cho user: "Hôm nay đã làm X, Y. Còn lại Z."
```

### Cách tạo custom skill

Tạo file `.md` trong thư mục skills:
```
~/.claude/projects/D--ICare247-Core/skills/
├── new-query.md
├── new-repo.md
├── new-wpf-screen.md
├── review-task.md
└── session-end.md
```

Hoặc đặt trong repo:
```
.claude/skills/
├── new-query.md
└── ...
```

---

## 8. Workflow vận hành hàng ngày

### Session Flow (chi tiết)

```
┌──────────────────────────────────────────────────────────┐
│  KHỞI ĐỘNG SESSION                                        │
│                                                            │
│  User: cd D:/ICare247_Core && claude                       │
│                                                            │
│  Agent tự động:                                            │
│  ├── Đọc CLAUDE.md (router ~80 dòng)                       │
│  ├── Đọc MEMORY.md index                                    │
│  └── Sẵn sàng nhận task                                    │
│                                                            │
│  User: "Hôm nay làm task gì?"                              │
│  hoặc: /review-task                                        │
│                                                            │
│  Agent:                                                    │
│  ├── Đọc TASKS.md                                          │
│  ├── Đọc memory liên quan                                  │
│  ├── Check git log gần nhất                                │
│  └── Đề xuất: "Task tiếp theo là TASK-013: FieldRepo.     │
│      Phiên trước đã xong SqlConnectionFactory. Làm không?" │
└──────────────────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────┐
│  LÀM VIỆC                                                  │
│                                                            │
│  Agent:                                                    │
│  ├── Move task sang 🔴 In Progress trong TASKS.md           │
│  ├── Đọc .claude-rules/ liên quan                          │
│  ├── Đọc docs/spec/ nếu cần tra cứu                       │
│  ├── Tạo branch (nếu chưa có)                              │
│  ├── Code (tuân thủ rules, hooks tự kiểm)                  │
│  ├── Build verify                                          │
│  └── Tick từng checkbox trong TASKS.md                      │
│                                                            │
│  [Hook tự động chạy sau mỗi file edit]                     │
│  ├── Build check → PASS/FAIL                               │
│  ├── Header check → có/thiếu                               │
│  └── EF Core guard → clean/violation                       │
└──────────────────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────┐
│  KẾT THÚC SESSION                                          │
│                                                            │
│  User: /session-end                                        │
│  hoặc: "Xong rồi, commit và wrap up"                      │
│                                                            │
│  Agent:                                                    │
│  ├── git add + commit (message chuẩn)                      │
│  ├── Cập nhật TASKS.md (done / progress notes)             │
│  ├── Lưu memory nếu có quyết định mới                     │
│  └── Tóm tắt: "Đã xong TASK-013. Tiếp theo: TASK-014."   │
└──────────────────────────────────────────────────────────┘
```

### Multi-agent Scenario

Khi project có nhiều người dùng Claude Code cùng lúc (hoặc 1 người dùng nhiều terminal):

```
Terminal 1 (Backend)              Terminal 2 (ConfigStudio WPF)
─────────────────────             ──────────────────────────────
Đọc CLAUDE.md (root)              Đọc CLAUDE.md (root)
Đọc TASKS.md                      Đọc docs/ICare247 Config Studio/CLAUDE.md
Đọc .claude-rules/dapper-*        Đọc .claude-rules/wpf-*
Làm TASK-013: FieldRepository     Làm WPF-TASK-007: Auto-save
Commit vào branch riêng           Commit vào branch riêng
```

Hai agent không conflict vì:
- Sửa file khác nhau (backend vs WPF)
- TASKS.md có task ID rõ ràng
- Memory lưu per-user nên không đè nhau
- Branch riêng cho từng feature

---

## 9. Cấu trúc file đề xuất

### Tổng quan

```
D:/ICare247_Core/
├── CLAUDE.md                              ← Router (≤100 dòng) [SỬA]
├── TASKS.md                               ← Backend tasks [SỬA format]
│
├── .claude/
│   ├── settings.local.json                ← Permissions [ĐÃ CÓ]
│   └── skills/                            ← Custom skills [MỚI]
│       ├── new-query.md
│       ├── new-repo.md
│       ├── new-wpf-screen.md
│       ├── review-task.md
│       └── session-end.md
│
├── .claude-rules/                         ← Coding rules tách nhỏ [MỚI]
│   ├── architecture.md                    ← ~60 dòng
│   ├── csharp-naming.md                   ← ~50 dòng
│   ├── dapper-patterns.md                 ← ~50 dòng
│   ├── caching.md                         ← ~30 dòng
│   ├── ast-grammar.md                     ← ~40 dòng
│   ├── api-response.md                    ← ~30 dòng
│   ├── comment-rules.md                   ← ~60 dòng
│   └── wpf-configstudio.md               ← ~50 dòng
│
├── docs/
│   ├── spec/                              ← [GIỮ NGUYÊN]
│   │   ├── 00_PROJECT_OVERVIEW.md
│   │   ├── ...
│   │   └── 08_CONVENTIONS.md
│   ├── ICare247 Config Studio/            ← [GIỮ NGUYÊN]
│   │   ├── CLAUDE.md
│   │   ├── TASKS_WPF.md                   ← WPF tasks riêng [MỚI]
│   │   └── ...
│   └── AI_WORKFLOW_UPGRADE_PROPOSAL.md    ← File này
│
└── src/backend/...                         ← Code [GIỮ NGUYÊN]
```

### Ước tính per-user (ngoài repo)

```
~/.claude/projects/D--ICare247-Core/
├── settings.json                          ← Hooks config [MỚI]
└── memory/
    ├── MEMORY.md                          ← Index [MỚI]
    ├── user_profile.md
    ├── project_architecture_decisions.md
    ├── project_current_phase.md
    ├── feedback_coding_style.md
    └── reference_external_systems.md
```

---

## 10. Lộ trình triển khai

### Bước 1: Foundation (làm ngay)

| Việc | Effort | Tác động |
|---|---|---|
| Tách CLAUDE.md → router + `.claude-rules/` | 30 phút | Giảm 70% context load mỗi session |
| Tạo MEMORY.md + initial memories | 15 phút | Agent nhớ context từ session 2 trở đi |
| Sửa docs reference path trong CLAUDE.md | 5 phút | Sửa bug trỏ sai `docs/spec/` |
| Thêm Session Protocol vào CLAUDE.md | 10 phút | Agent biết quy trình làm việc |

### Bước 2: Quality Gates (tuần sau)

| Việc | Effort | Tác động |
|---|---|---|
| Setup hooks: build check + header check | 20 phút | Chặn code không compile |
| Setup hooks: EF Core guard | 10 phút | Chặn vi phạm luật cấm |
| Nâng cấp TASKS.md format | 15 phút | Agent track tiến độ chi tiết hơn |
| Tách TASKS_WPF.md riêng | 10 phút | Không lẫn 2 workstream |

### Bước 3: Automation (tuần tiếp)

| Việc | Effort | Tác động |
|---|---|---|
| Tạo skill `/new-query` | 15 phút | Tạo CQRS feature trong 30 giây |
| Tạo skill `/new-repo` | 15 phút | Tạo repository boilerplate nhanh |
| Tạo skill `/review-task` | 10 phút | Bắt đầu session mượt mà |
| Tạo skill `/session-end` | 10 phút | Kết thúc session gọn gàng |

### Bước 4: Refine (liên tục)

- Bổ sung memory khi có quyết định thiết kế mới
- Thêm hooks khi phát hiện lỗi lặp lại
- Thêm skills khi thấy workflow lặp lại > 3 lần
- Review và prune memory mỗi 2 tuần

---

## Phụ lục A: So sánh Before/After

### Phiên làm việc điển hình

**BEFORE (hiện tại):**
```
User: claude
User: "Đọc TASKS.md, hôm nay làm FormRepository"
Agent: [đọc CLAUDE.md 310 dòng] [đọc TASKS.md] [bắt đầu code]
Agent: [hỏi lại naming convention vì quên]
Agent: [viết xong, commit]
Agent: [quên update TASKS.md]
--- Ngày hôm sau ---
User: claude
User: "Hôm qua mình làm gì rồi nhỉ?"
Agent: "Tôi không có thông tin về phiên trước."
User: [phải nhắc lại context 5 phút]
```

**AFTER (đề xuất):**
```
User: claude
Agent: [đọc CLAUDE.md 80 dòng] [đọc MEMORY.md → biết đang Phase 1]
User: /review-task
Agent: "Phiên trước (2026-03-16) đã xong TASK-012 FormRepository.
        Task tiếp theo: TASK-013 FieldRepository. Dependencies đã đủ.
        Làm task này?"
User: "Ừ, làm đi"
Agent: [đọc .claude-rules/dapper-patterns.md]
       [đọc docs/spec/02_DATABASE_SCHEMA.md → lấy schema Ui_Field]
       [code, hook tự build check]
       [tick checkboxes trong TASKS.md]
User: /session-end
Agent: [commit] [update TASKS.md ✅] [save memory: "TASK-013 done"]
       "Đã xong FieldRepository. Tiếp theo: TASK-014 HybridCacheService."
```

### Tác động ước tính

| Metric | Before | After |
|---|---|---|
| Context window dùng cho rules | ~310 dòng/session | ~80 + on-demand |
| Thời gian warm-up mỗi session | 3-5 phút (user nhắc context) | 30 giây (agent tự đọc memory) |
| Lỗi vi phạm coding rules | Phát hiện khi review | Chặn ngay khi viết (hooks) |
| Boilerplate CQRS/Repository | 5-10 phút mô tả | 30 giây (`/new-query`) |
| Continuity giữa sessions | Không có | Có (memory + TASKS.md) |

---

## Phụ lục B: Nội dung chi tiết `.claude-rules/`

### `.claude-rules/architecture.md`

```markdown
# Architecture Rules — ICare247

## Layer Dependency (Clean Architecture)
- Domain: KHÔNG import gì (pure C#, no ORM)
- Application: chỉ import Domain
- Infrastructure: import Application (implement interfaces)
- Api: import Application (KHÔNG import Infrastructure trực tiếp)

## Exception: Composition Root
- `Api.csproj` reference Infrastructure CHỈ để `Program.cs` gọi `AddInfrastructure()`
- Controllers KHÔNG được `new` bất kỳ class Infrastructure nào

## DI Registration
- Mỗi layer có `DependencyInjection.cs` riêng
- Program.cs chỉ gọi:
  ```csharp
  builder.Services.AddApplication();
  builder.Services.AddInfrastructure();
  ```

## CQRS Pattern (MediatR)
- Query: `IRequest<TResponse>` — đọc dữ liệu
- Command: `IRequest<TResponse>` — ghi/thực thi
- Handler: `IRequestHandler<TRequest, TResponse>`
- Validator: `AbstractValidator<TRequest>` (FluentValidation)
- Flow: Request → IMediator.Send() → Handler → Repository → DB/Cache

## File structure per feature
```
Application/Features/{Module}/Queries/{QueryName}/
├── {QueryName}Query.cs
├── {QueryName}QueryHandler.cs
└── {QueryName}QueryValidator.cs
```
```

### `.claude-rules/dapper-patterns.md`

```markdown
# Dapper Patterns — ICare247

## Quy tắc cứng
1. LUÔN dùng parameterized query — KHÔNG string interpolation vào SQL
2. LUÔN dùng `IDbConnectionFactory` — KHÔNG `new SqlConnection()`
3. LUÔN dùng async: `QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`
4. LUÔN truyền `CancellationToken` qua `CommandDefinition`
5. LUÔN có `AND Tenant_Id = @TenantId` cho bảng có Tenant_Id
6. LUÔN có `AND Is_Active = 1` cho soft-delete tables
7. KHÔNG `SELECT *` — chỉ SELECT cột cần thiết

## Template: Query single
```csharp
const string sql = """
    SELECT f.Form_Id, f.Form_Code, f.Version
    FROM   dbo.Ui_Form f
    WHERE  f.Form_Code = @FormCode
      AND  f.Tenant_Id = @TenantId
      AND  f.Is_Active = 1
    """;
var result = await conn.QueryFirstOrDefaultAsync<FormMetadata>(
    new CommandDefinition(sql, new { FormCode = formCode, TenantId = tenantId },
        cancellationToken: ct));
```

## Template: Query list
```csharp
var items = await conn.QueryAsync<FieldMetadata>(
    new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
        cancellationToken: ct));
return items.AsList();
```

## Template: Execute
```csharp
await conn.ExecuteAsync(
    new CommandDefinition(sql, param, cancellationToken: ct));
```
```

*(Các file `.claude-rules/` khác tương tự — trích xuất từ CLAUDE.md hiện tại + docs/spec/08)*

---

## Phụ lục C: Checklist tự kiểm cho agent

Agent nên chạy checklist này trước khi commit:

```
□ Code builds thành công (dotnet build)
□ Mỗi file .cs có file header (// File, Module, Layer, Purpose)
□ Namespace match folder path
□ Không có `using Microsoft.EntityFrameworkCore`
□ Không có string interpolation trong SQL
□ Mọi async method có suffix Async
□ Mọi async method nhận CancellationToken
□ Mọi SQL query có Tenant_Id filter (nếu bảng có)
□ Mọi cache key dùng CacheKeys.cs
□ Comment bằng tiếng Việt
□ TASKS.md đã cập nhật
```

---

*Tài liệu này là đề xuất nâng cấp. Sau khi user đồng ý, sẽ triển khai từng bước theo lộ trình Mục 10.*
