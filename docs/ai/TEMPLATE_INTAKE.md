# TEMPLATE_INTAKE — Checklist lọc template ngoài cho ICare247

> **Mục đích:** Hàng rào bắt buộc trước khi đưa bất kỳ skill/agent/command từ
> aitmpl.com (Claude Code Templates) hoặc nguồn ngoài vào repo.
> **Nguyên tắc gốc:** xem `BRAIN.md §11`. **KHÔNG copy nguyên bản.**
> Có mâu thuẫn → đề xuất điều chỉnh trước, KHÔNG tự ghi đè rule hiện tại.

---

## 0. Khi nào chạy checklist này
Mỗi lần định nhập **1 file** template (1 agent / 1 command / 1 skill). Làm từng file, không nhập cả bộ.

---

## A. Cổng chặn cứng (FAIL 1 mục = loại hoặc bắt buộc customize)

| # | Kiểm tra | Đạt khi |
|---|---|---|
| A1 | **Data access** | KHÔNG có EF Core / DbContext / LINQ-to-DB. Chỉ Dapper + `IDbConnectionFactory` |
| A2 | **SQL** | Parameterized 100%; KHÔNG string-interp; KHÔNG `SELECT *`; có `Tenant_Id` + `Is_Active`; đúng MS SQL Server |
| A3 | **Layer** | Không để Api `new` Infrastructure; Domain không import gì; Api không import Infrastructure |
| A4 | **Async** | Có `CancellationToken ct`; KHÔNG `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` |
| A5 | **Cache** | Không hardcode cache key; lấy từ `CacheKeys.cs` |
| A6 | **Comment** | Tiếng Việt + file header (File/Module/Layer/Purpose) + ghi sự kiện theo sau |
| A7 | **SSOT** | KHÔNG tạo/ghi đè `BRAIN.md`, `CLAUDE.md`, `AGENTS.md`, `copilot-instructions.md` |
| A8 | **Git** | KHÔNG tự commit/push; KHÔNG hook auto-commit |
| A9 | **UI** (nếu là UI template) | KHÔNG tự sinh token/màu/CSS; tuân skill `icare247-admin-ui` (Fluent Light khóa, ≤3 màu, surface phẳng, 1 CTA/màn) |
| A10 | **DBMS/Stack** | Không gắn Postgres/MySQL/Supabase/Neon/Mongo; không Node/Python backend |

## B. Cổng convention (FAIL = sửa cho khớp trước khi dùng)

- [ ] Naming CQRS/Repository theo `csharp-naming.md` (`Get{X}By{Y}Query`, `I{Entity}Repository`…)
- [ ] Logging: Serilog + `DebugLogger`, KHÔNG `Console.WriteLine` (`debug-logger.md`)
- [ ] API response: RFC 7807 ProblemDetails (`api-response.md`)
- [ ] AST/Grammar: chỉ AST-based, KHÔNG eval/dynamic-compile (`ast-grammar.md`)
- [ ] String mặc định = `string.Empty` (không null)
- [ ] Ưu tiên shared/common — sửa logic 1 chỗ, không copy-paste; SQL ưu tiên CTE/CROSS APPLY, tránh scalar function

## C. Cổng trùng lặp (tránh nhiễu)

- [ ] Có agent/command/skill nội bộ đã làm việc này chưa? (review → `review-changes`; UI → `icae247-admin-ui`; phân tích → `product-analyst`; design → `design-agent`)
- [ ] Nếu trùng → **gộp ý vào file hiện có**, KHÔNG thêm bản song song.

---

## D. Quyết định & cách xử lý mâu thuẫn

Với mỗi mâu thuẫn phát hiện, chọn 1 và ghi lại:

| Hướng | Khi dùng |
|---|---|
| Giữ rule hiện tại | Mặc định khi xung đột kiến trúc/DB/security |
| Customize template | Template tốt nhưng lệch convention → sửa rồi nhập |
| Gộp 2 rule | Template bổ sung ý hay cho rule sẵn có |
| Tách phạm vi | Áp template cho 1 agent/ngữ cảnh hẹp |
| Loại template | Vi phạm cổng A không cứu được |

**Phân loại mức:** Critical (phá kiến trúc/DB) · High (lệch convention) · Medium (trùng/nhiễu) · Low (khác biệt nhỏ).

---

## E. Thủ tục nhập (sau khi PASS A + B + C)

1. Tải file template về `scratchpad`, KHÔNG đặt thẳng vào `.claude/`.
2. Việt hóa mô tả + ép convention ICare247.
3. Thêm header vào đầu file:
   ```
   <!-- Nguồn: aitmpl.com/<đường dẫn> | Đã customize cho ICare247 ngày YYYY-MM-DD
        Đổi: <tóm tắt thay đổi so với bản gốc> -->
   ```
4. Đặt vào `.claude/agents|commands|skills/`.
5. Cập nhật bảng "agent/command được phép" trong `docs/ai/AI_TEMPLATE_INTEGRATION_PLAN.md`.
6. Báo user, KHÔNG commit cho tới khi user duyệt.

---

## F. Log nhập template

| Ngày | Template gốc | Loại | Kết quả (Nhập/Loại) | Mức mâu thuẫn | Ghi chú |
|---|---|---|---|---|---|
| 2026-06-28 | `security/security-auditor` | Agent | ✅ Nhập (customize) | High | Cắt cloud/K8s/compliance; mài SQLi/tenant/JWT/AST; read-only → `.claude/agents/security-reviewer.md` |
| 2026-06-28 | `performance-testing/performance-engineer` | Agent | ✅ Nhập (customize) | High | Bỏ load-test/CDN/web-vitals; mài N+1/cache/SQL; **chuyển Write/Edit/Bash → read-only** → `.claude/agents/performance-reviewer.md` |
| 2026-06-28 | `programming-languages/dotnet-core-expert` | Agent | ✅ Nhập (customize) | **Critical** | **EF Core → Dapper**; bỏ cloud/K8s/AOT/gRPC; .NET10→.NET9 → `.claude/agents/backend-dapper-expert.md` |
| 2026-06-28 | `database/database-optimizer` | Agent | ✅ Nhập (customize) | High | Đa DBMS → **chỉ MS SQL Server**; pattern Postgres → SQL Server; → **read-only advisory** → `.claude/agents/sql-server-optimizer.md` |
| 2026-06-28 | `development-team/test-generator` | Agent | ✅ Nhập (customize) | Medium | Bỏ tools thừa; ép **xUnit + src/backend/tests**; ràng buộc dữ liệu thật → `.claude/agents/test-generator.md` |
