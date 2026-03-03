# ICare247 Core Platform

> **Enterprise metadata-driven low-code form engine**  
> Tạo và vận hành form nghiệp vụ hoàn toàn từ cấu hình database — không cần code, không cần deploy lại khi thay đổi logic.

---

## Tech Stack

| Thành phần | Công nghệ |
|-----------|-----------|
| Backend | .NET 9 / ASP.NET Core 9 |
| Frontend | Blazor WebAssembly + DevExpress Blazor |
| Database | MS SQL Server |
| Data Access | Dapper (**EF Core bị cấm**) |
| Cache | MemoryCache (L1) + Redis (L2) |
| Logging | Serilog + OpenTelemetry |
| Auth | JWT + Policy-based Authorization |
| CQRS | MediatR |

---

## Quick Start

```bash
# 1. Clone repo
git clone https://github.com/your-org/icare247.git
cd icare247

# 2. Tạo database
sqlcmd -S localhost -i db/ICare247_Config.sql
sqlcmd -S localhost -i db/ICare247_SeedData.sql

# 3. Cấu hình connection string
# Tạo file src/backend/ICare247.Api/appsettings.Development.json
# (file này bị .gitignore — không commit)

# 4. Run backend
cd src/backend
dotnet run --project ICare247.Api

# 5. Run frontend (riêng terminal)
cd src/frontend
dotnet run --project ICare247.Client
```

---

## Cấu trúc Repository

```
ICare247/
├── src/
│   ├── backend/                    ← .NET 9 solution
│   │   ├── ICare247.Domain/        ← Entities, AST, Engine interfaces
│   │   ├── ICare247.Application/   ← CQRS Handlers, Use Cases
│   │   ├── ICare247.Infrastructure/← Dapper, Redis, AST Engine impl
│   │   └── ICare247.Api/           ← Controllers, Middleware
│   └── frontend/
│       └── ICare247.Client/        ← Blazor WASM
├── db/
│   ├── ICare247_Config.sql         ← DDL schema
│   └── ICare247_SeedData.sql       ← Seed data
└── docs/                           ← Specs & architecture docs
```

---

## Documentation

| File | Mô tả |
|------|-------|
| [00_PROJECT_OVERVIEW.md](docs/00_PROJECT_OVERVIEW.md) | Tổng quan, mục tiêu, tech stack, phases |
| [01_ARCHITECTURE.md](docs/01_ARCHITECTURE.md) | Clean Architecture, caching strategy, security |
| [02_DATABASE_SCHEMA.md](docs/02_DATABASE_SCHEMA.md) | Toàn bộ bảng DB, columns, relationships |
| [03_GRAMMAR_V1_SPEC.md](docs/03_GRAMMAR_V1_SPEC.md) | Grammar V1, AST node types, null propagation |
| [04_ENGINE_SPEC.md](docs/04_ENGINE_SPEC.md) | Engine specs: Metadata, AST, Validation, Event |
| [05_ACTION_RULE_PARAM_SCHEMA.md](docs/05_ACTION_RULE_PARAM_SCHEMA.md) | Action/Rule parameter JSON schema |
| [06_SOLUTION_STRUCTURE.md](docs/06_SOLUTION_STRUCTURE.md) | Folder structure, naming conventions |
| [07_API_CONTRACT.md](docs/07_API_CONTRACT.md) | API endpoints, request/response schemas |
| [08_CONVENTIONS.md](docs/08_CONVENTIONS.md) | Cache keys, Dapper patterns, comment rules |

---

## AI Agent Configuration Files

Bộ cấu hình này giúp các AI coding assistant hiểu project context và tuân theo conventions.

| File | Công cụ | Mô tả |
|------|---------|-------|
| `CLAUDE.md` | Claude Code | Main config cho Claude Code CLI |
| `AGENTS.md` | OpenAI Codex | Config cho Codex CLI / Responses API |
| `.cursorrules` | Cursor IDE | Rules cho Cursor AI assistant |
| `.github/copilot-instructions.md` | GitHub Copilot | Instructions cho GitHub Copilot |
| `.editorconfig` | Tất cả IDE | Code style enforcement |

---

## Nguyên Tắc Bắt Buộc (AI & Developer)

```
✅ Metadata over hardcode — Không hardcode form, rule, event, string
✅ Dapper only — EF Core bị cấm tuyệt đối
✅ Parameterized SQL — Không string interpolation vào SQL
✅ Multi-tenant — Mọi query/cache key phải có Tenant_Id
✅ No eval/dynamic compile — Chỉ AST-based execution
✅ Dependency graph — Không scan toàn bộ rule, dùng Sys_Dependency
✅ Thread-safe — Tất cả engine phải thread-safe
✅ Async all the way — Không .Result, .Wait()
✅ Comment tiếng Việt — Toàn bộ code comment bằng tiếng Việt
✅ Exception bubble up — Không swallow exception trong engine
```
