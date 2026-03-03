# Hướng Dẫn Chi Tiết — Bộ Cấu Hình AI Agent cho ICare247 Core

---

## Tổng Quan Bộ Cấu Hình

Bộ cấu hình này gồm **6 file** giúp các AI coding assistant (Claude Code, OpenAI Codex, GitHub Copilot, Cursor) hiểu context của project ICare247 và tự động tuân theo conventions mà không cần nhắc lại mỗi lần.

```
ICare247/                           ← Git root
├── CLAUDE.md                       ← Config cho Claude Code
├── AGENTS.md                       ← Config cho OpenAI Codex
├── .cursorrules                    ← Config cho Cursor IDE
├── .github/
│   └── copilot-instructions.md    ← Config cho GitHub Copilot
├── .editorconfig                   ← Code style (tất cả IDE/tool)
├── .gitignore                      ← Git exclusion rules
└── README.md                       ← Project overview
```

---

## FILE 1: `CLAUDE.md` — Claude Code Configuration

### Cơ chế hoạt động
Claude Code (Anthropic's CLI tool) **tự động đọc** `CLAUDE.md` khi khởi động trong thư mục project. Nội dung file này được inject vào system prompt của Claude.

```bash
# Claude Code tìm file theo thứ tự:
# 1. CLAUDE.md trong thư mục hiện tại (--cwd)
# 2. CLAUDE.md trong thư mục cha (leo dần lên đến git root)
# 3. ~/.claude/CLAUDE.md (global config)
```

### Cấu trúc file và giải thích từng phần

**Phần 1: Project Identity**
```markdown
## Project Identity
- Tên dự án: ICare247 Core Platform
- Loại: Enterprise metadata-driven low-code form engine
- Ngôn ngữ code: C# (.NET 9)
- Ngôn ngữ comment: Tiếng Việt (bắt buộc)
```
→ Giúp Claude hiểu đây là project .NET enterprise, không phải web app thông thường. Quan trọng vì Claude sẽ chọn library và pattern phù hợp ngay từ đầu.

**Phần 2: Tech Stack Table**
```markdown
| Data Access | Dapper | EF Core (cấm tuyệt đối) |
```
→ Cột "KHÔNG dùng" quan trọng nhất — ngăn Claude suggest EF Core vì mặc định Claude thường suggest EF Core cho .NET project.

**Phần 3: Architecture Rules**
```markdown
Domain ← Application ← Infrastructure
                    ← Api (only via Application interfaces)
```
→ Claude cần biết dependency flow để không import sai layer khi tạo code. Ví dụ, Claude biết không được `new FormRepository()` trong Controller.

**Phần 4: Coding Checklist**
```
✅ Namespace phải match folder path
✅ Repository method suffix = Async
✅ Mọi Dapper query = parameterized
...
```
→ Đây là "pre-commit checklist" của Claude. Trước khi output code, Claude tự kiểm tra danh sách này. Rất hiệu quả để ngăn lỗi phổ biến.

**Phần 5: Naming Conventions Table**
→ Giúp Claude đặt tên nhất quán với codebase hiện có. Đặc biệt quan trọng cho CQRS pattern vì nó có nhiều class với naming rule phức tạp.

**Phần 6: File Header Template**
```csharp
// File    : {FileName}.cs
// Module  : {ModuleName}
// Layer   : {Domain|Application|Infrastructure|Api}
// Purpose : {Mô tả bằng tiếng Việt}
```
→ Mỗi file .cs phải có header này. Giúp developer sau đọc code nhanh hiểu mục đích file mà không cần đọc nội dung.

**Phần 7: Comment Rules**
→ Bắt buộc comment tiếng Việt + XML doc cho public API. Đây là quy tắc đặc thù của team này.

**Phần 8: Docs Reference Table**
→ Chỉ dẫn Claude đọc file spec nào trước khi tạo code trong từng tình huống. Ngăn Claude "tự suy luận" sai spec.

### Cách dùng Claude Code với file này

```bash
# Khởi động Claude Code trong thư mục project
cd /path/to/ICare247
claude

# Claude tự động đọc CLAUDE.md và áp dụng rules
# Bạn có thể kiểm tra bằng cách hỏi:
> Bạn đang làm việc trong project nào?
> Quy tắc data access trong project này là gì?
```

---

## FILE 2: `AGENTS.md` — OpenAI Codex Configuration

### Cơ chế hoạt động
OpenAI Codex CLI (`codex` command) **tự động đọc** `AGENTS.md` khi chạy trong thư mục project. Đây là chuẩn mới của OpenAI (2025) thay thế cho việc nhúng instructions vào prompt thủ công.

```bash
# Cài đặt Codex CLI
npm install -g @openai/codex

# Chạy trong thư mục project — tự động đọc AGENTS.md
cd /path/to/ICare247
codex "Tạo FormRepository implement IFormRepository"
```

### Phần Critical Constraints — Tại sao đặt đầu tiên

```markdown
## Critical Constraints (KHÔNG ĐƯỢC VI PHẠM)
1. KHÔNG dùng EF Core — Data access chỉ bằng Dapper
2. KHÔNG string interpolation vào SQL
...
```
→ Codex đọc file theo thứ tự từ trên xuống. Đặt constraints đầu tiên đảm bảo chúng được "ghi nhớ" tốt hơn trước khi đọc các hướng dẫn chi tiết.

### Phần Standard Templates

```csharp
public async Task<FormMetadata?> GetByCodeAsync(
    string formCode, CancellationToken ct = default)
{
    const string sql = """
        SELECT Form_Id, Form_Code, Version
        FROM   dbo.Ui_Form
        WHERE  Form_Code = @FormCode
          AND  Is_Active  = 1
        """;
    // ...
}
```
→ Codex học theo pattern từ template. Cung cấp template cụ thể tốt hơn là chỉ mô tả rules bằng text.

### Khác biệt CLAUDE.md vs AGENTS.md

| Khía cạnh | CLAUDE.md | AGENTS.md |
|-----------|-----------|-----------|
| Tool | Claude Code CLI | OpenAI Codex CLI |
| Định dạng | Markdown tự do | Markdown tự do |
| Đặt ở | Git root | Git root |
| Auto-read | ✅ | ✅ |
| Tên file | Cố định `CLAUDE.md` | Cố định `AGENTS.md` |

---

## FILE 3: `.cursorrules` — Cursor IDE Configuration

### Cơ chế hoạt động
Cursor IDE (AI-powered code editor) đọc `.cursorrules` từ thư mục gốc của project và inject vào context của mọi AI request (Chat, Autocomplete, Cmd+K).

```
Vị trí: .cursorrules (root của project, cạnh .gitignore)
```

### Cấu trúc file

**Dòng đầu — Plain text description**
```
You are an expert .NET 9 / C# developer working on ICare247 Core Platform — 
a metadata-driven low-code form engine.
```
→ Cursor dùng dòng này để "role-play" — AI sẽ trả lời như một expert .NET developer, không phải như một generalist.

**Section ABSOLUTE RULES**
```
1. **Data Access = Dapper ONLY** — EF Core is FORBIDDEN in this project
```
→ Dùng bold và ALL CAPS để nhấn mạnh. Cursor đọc markdown formatting, nên bold có tác động mạnh hơn text thường.

**Section Standard Patterns**
→ Code examples ngắn gọn (không quá dài) vì Cursor giới hạn context window của `.cursorrules`. Tối ưu: ~100 lines.

**Section Documentation files**
```markdown
- `docs/02_DATABASE_SCHEMA.md` — DB tables and columns
- `docs/03_GRAMMAR_V1_SPEC.md` — Grammar V1, AST nodes
```
→ Cursor có thể `@docs/02_DATABASE_SCHEMA.md` để include file trong context. Danh sách này giúp developer biết cần include file nào khi hỏi AI về topic cụ thể.

### Cách dùng Cursor với file này

```
# Trong Cursor Chat:
@codebase Tạo class FormRepository implements IFormRepository

# Cursor tự động áp dụng .cursorrules
# → Sẽ dùng Dapper, không EF Core
# → Sẽ có file header tiếng Việt
# → Sẽ có XML doc comments
```

---

## FILE 4: `.github/copilot-instructions.md` — GitHub Copilot

### Cơ chế hoạt động
GitHub Copilot đọc `.github/copilot-instructions.md` và dùng nó như custom instructions cho toàn bộ repository. Áp dụng cho cả Copilot Chat và inline suggestions.

```
Vị trí bắt buộc: .github/copilot-instructions.md
(Copilot chỉ nhận diện file tại path này)
```

### Tại sao ngắn hơn CLAUDE.md và AGENTS.md

GitHub Copilot có giới hạn context window nhỏ hơn. File này được thiết kế tối ưu:
- Tập trung vào code patterns (không lý thuyết)
- Dùng code examples ngắn thay vì giải thích dài
- Table ngắn gọn cho naming conventions

### Phần Always/Never patterns

```markdown
### Always use Dapper (never EF Core)
```csharp
// ✅ Correct
using var conn = _connectionFactory.CreateConnection();

// ❌ Wrong
_dbContext.Forms.FirstOrDefaultAsync(...)
```
→ "Always/Never" format + contrast ví dụ đúng/sai là cách hiệu quả nhất để train Copilot. Copilot học theo pattern matching, nên ví dụ code quan trọng hơn mô tả text.

---

## FILE 5: `.editorconfig` — Code Style Enforcement

### Cơ chế hoạt động
`.editorconfig` là chuẩn mở (https://editorconfig.org) được hỗ trợ bởi Visual Studio, VS Code, JetBrains Rider, và các AI tool. Không cần plugin — hỗ trợ native.

```
Vị trí: git root (cạnh .gitignore)
Ảnh hưởng: tất cả editor khi mở file trong project
```

### Giải thích từng dòng quan trọng

**`root = true`**
```ini
root = true
```
→ Nói với editor: "Đây là `.editorconfig` cao nhất, không tìm tiếp lên thư mục cha". Bắt buộc có để tránh settings bị override bởi `.editorconfig` ở thư mục ngoài.

**`[*]` — Global rules**
```ini
[*]
indent_style = space       # Dùng space (không tab) — tránh tab/space mixing
indent_size = 4            # 4 spaces per indent level (C# convention)
end_of_line = crlf         # Windows line ending — vì team dùng Windows + SQL Server
charset = utf-8-bom        # UTF-8 with BOM — Visual Studio compatibility
trim_trailing_whitespace = true   # Xóa khoảng trắng cuối dòng (tránh diff noise)
insert_final_newline = true       # Dòng trống cuối file (Unix convention, Git friendly)
max_line_length = 120             # Cảnh báo dòng > 120 ký tự
```

**`csharp_style_namespace_declarations = file_scoped:warning`**
```ini
csharp_style_namespace_declarations = file_scoped:warning
```
→ Bắt dùng file-scoped namespace:
```csharp
// ✅ File-scoped (yêu cầu)
namespace ICare247.Domain.Entities.Form;
public class FormMetadata { }

// ❌ Block-scoped (không dùng)
namespace ICare247.Domain.Entities.Form
{
    public class FormMetadata { }
}
```
Lý do: File-scoped giảm 1 level indent cho toàn bộ file, code trông sạch hơn. C# 10+ feature.

**`csharp_style_var_for_built_in_types = false:suggestion`**
```ini
csharp_style_var_for_built_in_types = false:suggestion
```
→ Với built-in types, dùng type tường minh:
```csharp
int count = 0;        // ✅ rõ ràng
string code = "HDB";  // ✅ rõ ràng
var count = 0;        // ❌ không rõ type
```

**`csharp_style_var_when_type_is_apparent = true:suggestion`**
```ini
csharp_style_var_when_type_is_apparent = true:suggestion
```
→ Khi type rõ ràng từ vế phải, dùng `var`:
```csharp
var form = new FormMetadata();   // ✅ type rõ ràng
var repo = _serviceProvider.GetRequiredService<IFormRepository>(); // ✅ OK
```

**`csharp_new_line_before_open_brace = all`**
```ini
csharp_new_line_before_open_brace = all
```
→ Allman style — dấu `{` luôn xuống dòng mới:
```csharp
// ✅ Allman (yêu cầu)
public class FormRepository
{
    public async Task<FormMetadata?> GetByCodeAsync(...)
    {
    }
}

// ❌ K&R (không dùng)
public class FormRepository {
    public async Task<FormMetadata?> GetByCodeAsync(...) {
    }
}
```

**`dotnet_style_require_accessibility_modifiers = always:warning`**
```ini
dotnet_style_require_accessibility_modifiers = always:warning
```
→ Luôn viết access modifier, không bỏ qua:
```csharp
private readonly IDbConnectionFactory _factory;  // ✅
readonly IDbConnectionFactory _factory;           // ❌ thiếu private
```

### Section files đặc biệt

**`[*.sql]`**
```ini
[*.sql]
indent_size = 4
end_of_line = crlf
```
→ SQL files dùng CRLF (Windows), indent 4 spaces. Consistent với SQL Server Management Studio convention.

**`[*.json]`**
```ini
[*.json]
indent_size = 2
```
→ JSON dùng 2 spaces — đây là standard cho JSON (package.json, appsettings.json, v.v.).

**`[*.md]`**
```ini
[*.md]
trim_trailing_whitespace = false
```
→ Markdown KHÔNG trim trailing whitespace vì `2 spaces + Enter` = line break trong Markdown. Nếu trim thì mất line break.

---

## FILE 6: `.gitignore` — Git Exclusion Rules

### Cơ chế hoạt động
Git đọc `.gitignore` để biết file/thư mục nào không track. Mọi developer trong team dùng chung file này.

### Giải thích các section quan trọng

**`bin/` và `obj/`**
```gitignore
bin/
obj/
```
→ Build output — không commit vì:
1. Kích thước lớn (hàng MB)
2. Được regenerate từ source code
3. Khác nhau giữa Debug/Release build

**`appsettings.Development.json` và `appsettings.Production.json`**
```gitignore
appsettings.Development.json
appsettings.Production.json
```
→ Bắt buộc exclude vì các file này chứa:
- Connection string với username/password
- JWT Secret key
- Redis connection string
- API keys

Chỉ commit `appsettings.json` (template không có giá trị thật).

**`.env` và `.env.*`**
```gitignore
.env
.env.*
```
→ Environment variables files — chứa secrets, không bao giờ commit.

**`*.user` và `.vs/`**
```gitignore
*.user
.vs/
```
→ Cấu hình Visual Studio cá nhân — mỗi developer có startup project, breakpoints, window layout khác nhau → không commit, tránh conflict.

**`wwwroot/_framework/`**
```gitignore
wwwroot/_framework/
```
→ Blazor WASM compiled output (`.dll.br`, `.wasm.br`, v.v.) — được tạo khi `dotnet build/publish`. Không commit vì:
1. Rất lớn (hàng chục MB)
2. Được regenerate từ C# source

---

## Cách Deploy Bộ Cấu Hình Này

### Bước 1: Đặt file vào đúng vị trí

```
ICare247/                            ← Git root
├── CLAUDE.md                        ← ĐÂY
├── AGENTS.md                        ← ĐÂY
├── .cursorrules                     ← ĐÂY
├── .editorconfig                    ← ĐÂY
├── .gitignore                       ← ĐÂY
├── README.md                        ← ĐÂY
└── .github/
    └── copilot-instructions.md      ← ĐÂY (phải đúng path này)
```

### Bước 2: Commit tất cả (trừ file bị .gitignore)

```bash
git add CLAUDE.md AGENTS.md .cursorrules .editorconfig .gitignore README.md
git add .github/copilot-instructions.md
git commit -m "chore: add AI agent configuration files"
```

### Bước 3: Verify từng tool

**Claude Code:**
```bash
claude
> /status           # Kiểm tra CLAUDE.md đã được load chưa
```

**OpenAI Codex:**
```bash
codex "List the coding rules for this project"
# Codex sẽ liệt kê rules từ AGENTS.md
```

**Cursor:**
```
Trong Cursor Chat: "What are the rules for data access in this project?"
→ Cursor trả lời dựa trên .cursorrules
```

**GitHub Copilot:**
```
Trong Copilot Chat: "What database library should I use?"
→ Copilot trả lời "Dapper" dựa trên copilot-instructions.md
```

---

## Bảo Trì Bộ Cấu Hình

### Khi nào cần update

| Tình huống | File cần update |
|-----------|----------------|
| Thêm tech stack mới | CLAUDE.md, AGENTS.md, .cursorrules |
| Thêm naming convention mới | Tất cả 3 file AI config |
| Thêm bảng DB mới | docs/02_DATABASE_SCHEMA.md (không phải AI config) |
| Đổi .NET version | CLAUDE.md, AGENTS.md, .cursorrules, .editorconfig |
| Thêm cấu hình editor | .editorconfig |
| Thêm loại file mới cần ignore | .gitignore |

### Nguyên tắc bảo trì

1. **Consistency**: Quy tắc trong CLAUDE.md phải khớp với AGENTS.md và .cursorrules
2. **DRY**: Không lặp lại chi tiết spec — reference docs/ thay vì copy paste
3. **Concise**: AI config files nên ngắn gọn (< 300 lines) — quá dài → AI bỏ sót phần cuối
4. **Examples > Prose**: Code example cụ thể hiệu quả hơn mô tả text
5. **Negative examples**: Luôn kèm ví dụ SAI (❌) bên cạnh ví dụ đúng (✅)
