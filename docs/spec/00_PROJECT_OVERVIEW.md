# 00 — Project Overview

## Tên dự án
ICare247 Core Platform

## Mô tả
Enterprise metadata-driven low-code form engine. Cho phép định nghĩa form, field, validation rule, event/action thông qua metadata lưu trong database — không cần deploy lại code khi thay đổi nghiệp vụ.

## Mục tiêu
- Render form động từ metadata (Ui_Form, Ui_Field, Ui_Section)
- Evaluate rule/expression qua AST engine (không eval, không Roslyn)
- Multi-tenant: mỗi tenant có metadata riêng biệt
- Platform: Web (Blazor WASM) + Mobile (tương lai)

## Tech Stack

| Thành phần  | Công nghệ                       |
| ----------- | ------------------------------- |
| Backend     | .NET 9 / ASP.NET Core 9         |
| Frontend    | Blazor WebAssembly + DevExpress |
| Database    | MS SQL Server                   |
| Data Access | Dapper (EF Core bị cấm)         |
| Cache       | MemoryCache + Redis (Hybrid)    |
| Logging     | Serilog + OpenTelemetry         |
| Auth        | JWT + Policy-based              |

## Các Engine chính

| Engine             | Trách nhiệm                                        |
| ------------------ | -------------------------------------------------- |
| MetadataEngine     | Load form/field/section metadata, cache hybrid     |
| AstEngine          | Parse + compile Expression_Json thành delegate     |
| ValidationEngine   | Evaluate rule list theo dependency order           |
| EventEngine        | Xử lý event → trigger action → build UI delta      |

## Phạm vi Phase 1
Foundation: DB connection, Repository pattern, Cache, Domain entities, CQRS skeleton.
