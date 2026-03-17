# Hướng dẫn AI-Assisted Development Workflow — ICare247

> **Mục đích:** Tài liệu này giải thích toàn bộ hệ thống cấu hình để Claude Code
> (AI agent) có thể tự động làm việc, nhớ context xuyên sessions, đồng bộ qua
> nhiều máy tính, và tuân thủ đúng quy tắc dự án.
>
> **Đối tượng:** Developer (con người) cần hiểu hệ thống hoạt động thế nào.

---

## 1. Tổng quan kiến trúc

### 1.1. Vấn đề cần giải quyết

Khi làm việc với AI agent (Claude Code) trong một dự án enterprise:

1. **Agent quên mọi thứ** khi tắt session → cần hệ thống memory
2. **Quy tắc dự án phức tạp** (naming, architecture, SQL patterns) → cần file rules agent đọc được
3. **Làm việc trên nhiều máy** → cần mọi config đồng bộ qua git
4. **Workflow lặp lại** (bắt đầu session, chọn task, review code) → cần slash commands tự động hóa
5. **Spec dự án dài** → cần tổ chức để agent tra cứu đúng file, không đọc hết

### 1.2. Giải pháp: 5 tầng cấu hình

```
┌─────────────────────────────────────────────────────────┐
│  Tầng 1: CLAUDE.md (Router)                             │
│  ─ Agent đọc ĐẦU TIÊN mỗi session                      │
│  ─ Chỉ chứa: identity + tech stack + 10 luật + pointer  │
│  ─ KHÔNG chứa chi tiết → trỏ xuống tầng dưới            │
├─────────────────────────────────────────────────────────┤
│  Tầng 2: .claude-rules/ (Coding Rules)                  │
│  ─ Agent đọc KHI CẦN (theo task đang làm)               │
│  ─ 8 file, mỗi file 1 chủ đề                            │
│  ─ Chứa: template code, checklist, ví dụ đúng/sai       │
├─────────────────────────────────────────────────────────┤
│  Tầng 3: .claude/memory/ (Agent Memory)                 │
│  ─ Agent đọc ĐẦU session, ghi CUỐI session              │
│  ─ 6 file: last_session, phase, ADR, feedback, profile   │
│  ─ Git-tracked → sync qua nhiều máy                      │
├─────────────────────────────────────────────────────────┤
│  Tầng 4: .claude/commands/ (Slash Commands)             │
│  ─ User gọi bằng /command-name trong chat                │
│  ─ 5 commands: start, pick, finish, review, save-memory  │
│  ─ Tự động hóa workflow lặp lại                          │
├─────────────────────────────────────────────────────────┤
│  Tầng 5: docs/spec/ (Project Specification)             │
│  ─ Agent đọc khi cần tra cứu schema/API/engine           │
│  ─ 9 file spec chi tiết                                  │
│  ─ Source of truth cho domain knowledge                   │
└─────────────────────────────────────────────────────────┘
```

### 1.3. Nguyên tắc thiết kế

| Nguyên tắc | Giải thích |
|---|---|
| **Router, không monolith** | CLAUDE.md nhỏ (~100 dòng), chỉ pointer. Agent đọc nhanh, biết cần đọc gì thêm |
| **Đọc khi cần, không đọc hết** | Agent chỉ load `.claude-rules/dapper-patterns.md` khi task liên quan DB — tiết kiệm context window |
| **Git-tracked = đồng bộ** | Mọi config nằm trong repo. Clone repo = có đầy đủ |
| **Memory tách riêng per-concern** | Không 1 file memory khổng lồ. 6 file nhỏ, agent ghi/đọc chính xác |
| **Commands = workflow chuẩn hóa** | Mọi session bắt đầu/kết thúc giống nhau, không quên bước nào |

---

## 2. Cấu trúc thư mục

```
ICare247_Core/
├── CLAUDE.md                          ← Tầng 1: Router (agent đọc đầu tiên)
├── TASKS.md                           ← Task tracking (agent đọc/ghi)
│
├── .claude-rules/                     ← Tầng 2: Coding Rules (8 file)
│   ├── architecture.md                   Layer dependency, CQRS, DI
│   ├── csharp-naming.md                  Naming conventions
│   ├── dapper-patterns.md                SQL patterns, cấm tuyệt đối
│   ├── caching.md                        CacheKeys, L1/L2 TTL
│   ├── ast-grammar.md                    AST nodes, null propagation
│   ├── api-response.md                   RFC 7807, ProblemDetails
│   ├── comment-rules.md                  File header, XML doc, tiếng Việt
│   └── wpf-configstudio.md              Prism 9, MVVM, MaterialDesign
│
├── .claude/                           ← Tầng 3 + 4: Memory + Commands
│   ├── settings.json                     Team settings (git-tracked)
│   ├── settings.local.json               Per-machine (gitignored)
│   │
│   ├── memory/                        ← Tầng 3: Agent Memory (git-tracked)
│   │   ├── MEMORY.md                     Index file
│   │   ├── last_session.md               Session trước làm gì
│   │   ├── project_current_phase.md      Phase hiện tại + priorities
│   │   ├── architecture_decisions.md     ADR log
│   │   ├── coding_style_feedback.md      User corrections
│   │   └── user_profile.md              User preferences
│   │
│   └── commands/                      ← Tầng 4: Slash Commands (git-tracked)
│       ├── start-session.md              /start-session
│       ├── pick-task.md                  /pick-task
│       ├── finish-task.md                /finish-task
│       ├── review-changes.md             /review-changes
│       └── save-memory.md               /save-memory
│
└── docs/spec/                         ← Tầng 5: Project Specification (9 file)
    ├── 00_PROJECT_OVERVIEW.md
    ├── 01_ARCHITECTURE.md
    ├── 02_DATABASE_SCHEMA.md
    ├── 03_GRAMMAR_V1_SPEC.md
    ├── 04_ENGINE_SPEC.md
    ├── 05_ACTION_RULE_PARAM_SCHEMA.md
    ├── 06_SOLUTION_STRUCTURE.md
    ├── 07_API_CONTRACT.md
    └── 08_CONVENTIONS.md
```

### Tổng cộng: 30 file cấu hình AI

| Tầng | Số file | Git-tracked |
|---|---|---|
| Tầng 1: Router | 1 (CLAUDE.md) | ✅ |
| Tầng 2: Rules | 8 | ✅ |
| Tầng 3: Memory | 6 | ✅ |
| Tầng 4: Commands | 5 | ✅ |
| Tầng 5: Spec | 9 | ✅ |
| Settings (team) | 1 | ✅ |
| Settings (local) | 1 | ❌ gitignored |

---

## 3. Chi tiết từng tầng

### 3.1. Tầng 1: CLAUDE.md — Router

**Vị trí:** `ICare247_Core/CLAUDE.md` (git root)

**Claude Code tự động đọc file này** khi mở project. Đây là điểm vào duy nhất.

**Nội dung:**

| Section | Mục đích |
|---|---|
| Project Identity | Tên dự án, ngôn ngữ, pattern |
| Tech Stack | Bảng dùng/không dùng — agent biết ngay constraint |
| 10 Luật bất biến | Luật KHÔNG BAO GIỜ được vi phạm — agent check mỗi dòng code |
| Bảng pointer Rules | Trỏ đến 8 file `.claude-rules/` — agent đọc khi cần |
| Bảng pointer Spec | Trỏ đến 9 file `docs/spec/` — agent tra cứu khi cần |
| Bảng pointer Memory | Trỏ đến 6 file `.claude/memory/` |
| Bảng Slash Commands | 5 commands có sẵn |
| Session Protocol | 9 bước agent phải tuân theo mỗi session |

**Tại sao router mà không monolith?**

- CLAUDE.md monolith (300+ dòng) → agent đọc hết, tốn context window, dễ bỏ sót
- Router (~100 dòng) → agent scan nhanh, biết cần đọc gì thêm cho task cụ thể
- Ví dụ: task về Dapper → agent chỉ cần đọc thêm `dapper-patterns.md` + `02_DATABASE_SCHEMA.md`

---

### 3.2. Tầng 2: `.claude-rules/` — Coding Rules

**Vị trí:** `ICare247_Core/.claude-rules/` (8 file)

**Khi nào agent đọc:** Chỉ khi task liên quan. CLAUDE.md có bảng mapping rõ ràng.

| File | Agent đọc khi task liên quan đến... |
|---|---|
| `architecture.md` | Tạo class mới, tổ chức layer, DI |
| `csharp-naming.md` | Tạo class/method/variable mới |
| `dapper-patterns.md` | Viết SQL query, repository |
| `caching.md` | Thêm cache, sửa CacheKeys |
| `ast-grammar.md` | AST engine, expression parsing |
| `api-response.md` | API endpoint, error handling |
| `comment-rules.md` | Mọi file .cs mới (file header bắt buộc) |
| `wpf-configstudio.md` | ConfigStudio WPF screens |

**Cấu trúc mỗi file rule:**

```
1. Quy tắc cứng (PHẢI tuân theo)
2. Template code (copy-paste ready)
3. Ví dụ ✅ ĐÚNG
4. Ví dụ ❌ SAI (cấm tuyệt đối)
5. Checklist (nếu có)
```

**Cách thêm rule mới:**

1. Tạo file `.claude-rules/ten-rule-moi.md` theo cấu trúc trên
2. Thêm 1 dòng vào bảng pointer trong CLAUDE.md
3. Commit + push → tất cả máy đều có

---

### 3.3. Tầng 3: `.claude/memory/` — Agent Memory

**Vị trí:** `ICare247_Core/.claude/memory/` (6 file)

**Vấn đề:** Claude Code mất toàn bộ context khi tắt session.

**Giải pháp:** Agent ghi thông tin quan trọng vào file trong repo. Session sau đọc lại.

| File | Agent đọc khi | Agent ghi khi |
|---|---|---|
| `last_session.md` | Bắt đầu session mới | Kết thúc session |
| `project_current_phase.md` | Cần biết làm gì tiếp | Phase thay đổi |
| `architecture_decisions.md` | Gặp quyết định tương tự | Có ADR mới |
| `coding_style_feedback.md` | Viết code | User sửa lỗi style |
| `user_profile.md` | Cần biết preferences | User thay đổi preference |
| `MEMORY.md` | Index — biết có file nào | Thêm file memory mới |

**Ví dụ luồng memory:**

```
Session 1 (Máy A):
  → Agent làm task "Tạo IFormRepository"
  → Quyết định: dùng IDbConnectionFactory pattern
  → Ghi vào architecture_decisions.md
  → Ghi vào last_session.md: "Đã tạo IFormRepository"
  → Commit + push

Session 2 (Máy B):
  → git pull
  → Agent đọc last_session.md → biết session trước tạo IFormRepository
  → Agent đọc architecture_decisions.md → biết dùng IDbConnectionFactory
  → Tiếp tục từ đúng chỗ dừng
```

**Tại sao memory nằm trong repo (không phải ~/.claude/)?**

| Vị trí | Ưu | Nhược |
|---|---|---|
| `~/.claude/projects/` | Claude Code tự quản lý | ❌ Không sync qua máy khác |
| `.claude/memory/` (repo) | ✅ Git sync, version history | Cần agent tự ghi |

→ Chọn repo vì yêu cầu multi-machine là bắt buộc.

---

### 3.4. Tầng 4: `.claude/commands/` — Slash Commands

**Vị trí:** `ICare247_Core/.claude/commands/` (5 file)

**Cách dùng:** Gõ `/command-name` trong Claude Code chat.

#### `/start-session` — Bắt đầu ngày làm việc

```
User gõ: /start-session

Agent tự động:
1. Đọc last_session.md → "Session trước tạo xong IFormRepository"
2. Đọc project_current_phase.md → "Phase 1, tiếp theo: CacheKeys.cs"
3. Đọc TASKS.md → liệt kê task In Progress / Todo
4. Trả lời user:
   "Session trước: Tạo xong IFormRepository trên máy A.
    Đang ở Phase 1 Foundation.
    3 task tiếp theo:
    1. Tạo CacheKeys.cs (S)
    2. Tạo GetFormByCodeQuery + Handler (M)
    3. Implement SqlConnectionFactory (M)
    Hôm nay làm task nào?"
```

#### `/pick-task` — Chọn task

```
User gõ: /pick-task

Agent tự động:
1. Đọc TASKS.md + phase priorities
2. Liệt kê top 5 task kèm: tên, size (S/M/L), dependencies, files cần đọc
3. User chọn → agent move task sang 🔴 In Progress
4. Agent đọc .claude-rules/ + docs/spec/ liên quan
5. Bắt đầu code
```

#### `/finish-task` — Hoàn thành task

```
User gõ: /finish-task

Agent tự động:
1. dotnet build → verify thành công
2. Cập nhật TASKS.md: task → ✅ Done
3. Cập nhật last_session.md
4. Cập nhật project_current_phase.md (nếu cần)
5. Đề xuất commit message → user confirm → commit
```

#### `/review-changes` — Review code

```
User gõ: /review-changes

Agent tự động:
1. git diff → xem tất cả thay đổi
2. Check từng file theo checklist:
   ✅/❌ Architecture (layer dependency)
   ✅/❌ Dapper (parameterized, Tenant_Id, no SELECT *)
   ✅/❌ Async (CancellationToken, no .Result)
   ✅/❌ Naming (PascalCase, _camelCase)
   ✅/❌ Comments (file header tiếng Việt)
3. Báo cáo pass/fail + đề xuất fix
```

#### `/save-memory` — Lưu nhớ

```
User gõ: /save-memory

Agent: "Muốn lưu gì?"
User: "Quyết định dùng FluentValidation cho tất cả Commands"
Agent: Ghi vào architecture_decisions.md → confirm
```

**Cách thêm command mới:**

1. Tạo file `.claude/commands/ten-command.md`
2. Nội dung = prompt cho agent (viết bằng tiếng Việt)
3. Thêm 1 dòng vào bảng Commands trong CLAUDE.md
4. Commit + push → mọi máy đều có

---

### 3.5. Tầng 5: `docs/spec/` — Project Specification

**Vị trí:** `ICare247_Core/docs/spec/` (9 file)

**Khi nào agent đọc:** Khi cần tra cứu chi tiết domain.

| Tình huống | Agent đọc file |
|---|---|
| Viết SQL query | `02_DATABASE_SCHEMA.md` (bảng, columns, constraints) |
| Viết AST parser | `03_GRAMMAR_V1_SPEC.md` + `04_ENGINE_SPEC.md` |
| Tạo API endpoint | `07_API_CONTRACT.md` (request/response schema) |
| Tổ chức folder | `06_SOLUTION_STRUCTURE.md` |

**Quy tắc:** Agent tra cứu spec TRƯỚC khi tự suy luận. Spec là source of truth.

---

## 4. Đồng bộ Multi-Machine

### 4.1. Sơ đồ đồng bộ

```
Máy A (Văn phòng)              Git Remote              Máy B (Nhà)
┌─────────────┐               ┌──────────┐            ┌─────────────┐
│ Claude Code │──git push───→ │  GitHub   │ ←─git pull─│ Claude Code │
│             │               │          │            │             │
│ CLAUDE.md   │               │ Toàn bộ  │            │ CLAUDE.md   │
│ .claude-rules/│             │ 30 file  │            │ .claude-rules/│
│ .claude/memory/│            │ AI config│            │ .claude/memory/│
│ .claude/commands/│          │ đồng bộ  │            │ .claude/commands/│
│ TASKS.md    │               │          │            │ TASKS.md    │
│ docs/spec/  │               │          │            │ docs/spec/  │
│             │               │          │            │             │
│ settings.   │               │ KHÔNG có │            │ settings.   │
│ local.json  │               │ file local│           │ local.json  │
│ (riêng máy) │               │          │            │ (riêng máy) │
└─────────────┘               └──────────┘            └─────────────┘
```

### 4.2. Setup máy mới

```bash
# 1. Clone repo
git clone <repo-url> ICare247_Core
cd ICare247_Core

# 2. Tạo settings local (per-machine permissions)
# File này bị gitignore (*.local.json) nên mỗi máy tự tạo
cat > .claude/settings.local.json << 'EOF'
{
  "permissions": {
    "allow": [
      "Bash(dotnet build*)",
      "Bash(dotnet restore*)",
      "Bash(git *)",
      "Bash(ls*)"
    ]
  }
}
EOF

# 3. Mở Claude Code → tự động đọc CLAUDE.md → sẵn sàng
claude
```

**Chỉ bước 2 là manual.** Bước 3 trở đi, agent đã có đầy đủ context.

### 4.3. Luồng làm việc xuyên máy

```
Máy A (sáng):
  /start-session → Agent đọc memory → bắt đầu task
  ... code ...
  /finish-task → Agent cập nhật TASKS.md + memory → commit + push

Máy B (chiều):
  git pull
  /start-session → Agent đọc memory → biết sáng làm gì
  ... tiếp tục code ...
  /finish-task → commit + push
```

### 4.4. Phân biệt file đồng bộ vs file local

| File | Git-tracked? | Tại sao? |
|---|---|---|
| `.claude/settings.json` | ✅ Sync | Team rules, ai cũng cần |
| `.claude/settings.local.json` | ❌ Local | Permissions khác nhau giữa các máy |
| `.claude/memory/*` | ✅ Sync | Agent cần nhớ xuyên máy |
| `.claude/commands/*` | ✅ Sync | Workflow giống nhau mọi nơi |
| `.claude-rules/*` | ✅ Sync | Quy tắc code là duy nhất |
| `docs/spec/*` | ✅ Sync | Spec là source of truth |

---

## 5. Workflow hàng ngày

### 5.1. Luồng chuẩn (Happy Path)

```
┌──────────────────────────────────────────────────────┐
│  1. git pull (nếu đổi máy)                           │
│  2. Mở Claude Code                                   │
│  3. /start-session                                   │
│     → Agent đọc memory + TASKS.md                    │
│     → "Session trước: ... Task tiếp: ..."            │
│     → "Hôm nay làm task nào?"                        │
│  4. User chọn task (hoặc /pick-task)                 │
│     → Agent move task → 🔴 In Progress               │
│     → Agent đọc .claude-rules/ + docs/spec/ liên quan│
│  5. Agent code (có thể nhiều vòng)                   │
│     → Hỏi user khi cần clarify                       │
│     → Tuân thủ 10 luật bất biến                      │
│  6. /review-changes                                  │
│     → Agent review theo checklist                    │
│     → Fix nếu có issue                               │
│  7. /finish-task                                     │
│     → dotnet build verify                            │
│     → Cập nhật TASKS.md + memory                     │
│     → Commit                                         │
│  8. git push (để máy khác tiếp tục)                  │
└──────────────────────────────────────────────────────┘
```

### 5.2. Khi agent đưa ra quyết định kiến trúc

```
Agent: "Tôi đề xuất dùng Strategy Pattern cho..."
User: "OK, đồng ý"
→ /save-memory → ghi vào architecture_decisions.md
→ Session sau, agent nhớ quyết định này
```

### 5.3. Khi user sửa lỗi coding style

```
User: "Đừng dùng var, luôn viết explicit type"
→ Agent ghi vào coding_style_feedback.md
→ Mọi session sau, agent tuân theo
```

---

## 6. Cách mở rộng hệ thống

### 6.1. Thêm Rule mới

```bash
# 1. Tạo file rule
echo "# New Rule" > .claude-rules/ten-rule-moi.md

# 2. Thêm vào CLAUDE.md (bảng pointer)
# | `.claude-rules/ten-rule-moi.md` | Mô tả |

# 3. Commit + push
```

### 6.2. Thêm Command mới

```bash
# 1. Tạo file command
echo "Prompt cho agent..." > .claude/commands/ten-command.md

# 2. Thêm vào CLAUDE.md (bảng Commands)
# | `/ten-command` | Mô tả |

# 3. Commit + push
```

### 6.3. Thêm Memory category mới

```bash
# 1. Tạo file memory
echo "# New Category" > .claude/memory/ten-category.md

# 2. Thêm vào .claude/memory/MEMORY.md (index)

# 3. Thêm vào CLAUDE.md (bảng Memory)

# 4. Commit + push
```

### 6.4. Thêm Spec mới

```bash
# 1. Tạo file spec
echo "# New Spec" > docs/spec/09_TEN_SPEC.md

# 2. Thêm vào CLAUDE.md (bảng Specification)

# 3. Commit + push
```

---

## 7. Danh sách file hoàn chỉnh (30 file)

### Tầng 1: Router (1 file)

| # | File | Dòng | Mô tả |
|---|---|---|---|
| 1 | `CLAUDE.md` | ~120 | Router — agent đọc đầu tiên |

### Tầng 2: Rules (8 file)

| # | File | Dòng | Mô tả |
|---|---|---|---|
| 2 | `.claude-rules/architecture.md` | ~55 | Layer, CQRS, DI |
| 3 | `.claude-rules/csharp-naming.md` | ~50 | Naming conventions |
| 4 | `.claude-rules/dapper-patterns.md` | ~50 | SQL, Dapper templates |
| 5 | `.claude-rules/caching.md` | ~40 | CacheKeys, L1/L2 |
| 6 | `.claude-rules/ast-grammar.md` | ~40 | AST, null propagation |
| 7 | `.claude-rules/api-response.md` | ~35 | RFC 7807, ProblemDetails |
| 8 | `.claude-rules/comment-rules.md` | ~55 | Header, XML doc, Việt |
| 9 | `.claude-rules/wpf-configstudio.md` | ~55 | Prism 9, MVVM |

### Tầng 3: Memory (6 file)

| # | File | Mô tả |
|---|---|---|
| 10 | `.claude/memory/MEMORY.md` | Index |
| 11 | `.claude/memory/last_session.md` | Session continuity |
| 12 | `.claude/memory/project_current_phase.md` | Phase + priorities |
| 13 | `.claude/memory/architecture_decisions.md` | 6 ADR |
| 14 | `.claude/memory/coding_style_feedback.md` | Style corrections |
| 15 | `.claude/memory/user_profile.md` | User preferences |

### Tầng 4: Commands (5 file)

| # | File | Trigger |
|---|---|---|
| 16 | `.claude/commands/start-session.md` | `/start-session` |
| 17 | `.claude/commands/pick-task.md` | `/pick-task` |
| 18 | `.claude/commands/finish-task.md` | `/finish-task` |
| 19 | `.claude/commands/review-changes.md` | `/review-changes` |
| 20 | `.claude/commands/save-memory.md` | `/save-memory` |

### Tầng 5: Spec (9 file)

| # | File | Mô tả |
|---|---|---|
| 21 | `docs/spec/00_PROJECT_OVERVIEW.md` | Tổng quan |
| 22 | `docs/spec/01_ARCHITECTURE.md` | Architecture |
| 23 | `docs/spec/02_DATABASE_SCHEMA.md` | DB schema |
| 24 | `docs/spec/03_GRAMMAR_V1_SPEC.md` | Grammar V1 |
| 25 | `docs/spec/04_ENGINE_SPEC.md` | 4 engines |
| 26 | `docs/spec/05_ACTION_RULE_PARAM_SCHEMA.md` | Action/Rule schema |
| 27 | `docs/spec/06_SOLUTION_STRUCTURE.md` | Folder structure |
| 28 | `docs/spec/07_API_CONTRACT.md` | API contract |
| 29 | `docs/spec/08_CONVENTIONS.md` | Conventions |

### Settings (2 file)

| # | File | Git? | Mô tả |
|---|---|---|---|
| 30 | `.claude/settings.json` | ✅ | Team permissions |
| 31 | `.claude/settings.local.json` | ❌ | Per-machine permissions |

### Task Tracking (1 file)

| # | File | Mô tả |
|---|---|---|
| 32 | `TASKS.md` | Backend + ConfigStudio task list |
