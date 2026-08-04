# Soát chất lượng Backend — Vấn đề cốt lõi & SOLID

> Ngày: 2026-08-03 · Phạm vi: `src/backend` (Application + Infrastructure + Api) · Loại: read-only audit (chưa sửa code).
> Nguồn quy tắc phái sinh: [.claude-rules/sql-safety.md](../../.claude-rules/sql-safety.md), [.claude-rules/architecture.md](../../.claude-rules/architecture.md).

## Kết luận nhanh

Kiến trúc **thiết kế tốt** (Clean Architecture 4 lớp, CQRS, DI, có ý thức bảo mật). Hai điểm yếu **cùng loại và cộng hưởng**:

> Vùng nguy hiểm nhất — **dynamic SQL** và **cô lập tenant** — vừa dựa vào kỷ luật thủ công (guard copy-paste),
> vừa **không có test**. Kỷ luật + không lưới an toàn = rủi ro tích lũy theo thời gian.

---

## 🔴 #1 — Độ phủ test gần như bằng 0 trên vùng rủi ro nhất

| Chỉ số | Giá trị |
|---|---|
| File production (.cs) | 358 |
| File test | 5 |
| Test project | 1 (`ICare247.Application.Tests`) |

- Toàn bộ `Infrastructure` (repository + dynamic SQL) **KHÔNG có test** — chính nơi dựng SQL bằng nối chuỗi.
- Không có integration test cho API, ConfigSync, Import, Event/Validation engine end-to-end.

**Tác động:** mọi rủi ro bên dưới không có lưới an toàn tự động; mỗi lần sửa repo là "sửa mù".

## 🔴 #2 — Guard chống SQL injection bị copy-paste, KHÔNG nhất quán

`SafeIdentifierRegex()` được định nghĩa lại ≥ 5 lần với **2 pattern khác nhau**:

| File | Pattern |
|---|---|
| `MaCodeGenerator.cs:26`, `CodeRuleCatalog.cs:30`, `HookStoreCatalog.cs:26`, `FkLookupResolver.cs:25` | `^[a-zA-Z_][a-zA-Z0-9_]*$` |
| `DynamicLookupRepository.cs:35` | `^[a-zA-Z_][a-zA-Z0-9_.]*$` ← cho phép thêm dấu `.` |

- `Bracket()` viết lại riêng mỗi file (`FkLookupResolver.cs:129`, `ViewRepository.cs:1030`…).
- `custom_sql` / `FilterSql` từ Config DB chỉ chặn bằng **blocklist keyword** (`DynamicLookupRepository.cs:384,413`) — luôn yếu hơn whitelist/tham số hóa.

**Rủi ro:** guard bảo mật trọng yếu bị phân tán → một file drift là mở lỗ hổng injection mà không ai thấy.
→ Xem quy tắc khắc phục: `.claude-rules/sql-safety.md`.

## 🔴 #3 — Cô lập tenant chỉ có MỘT lớp phòng thủ

`IViewRepository.cs:21,84`, `IDynamicLookupRepository.cs:62` ghi rõ *"tenantId… KHÔNG lọc SQL (ADR-035)"*.

Mô hình 1 Config-DB = 1 tenant (connection-per-tenant) hợp lệ, **nhưng không có lớp phòng thủ thứ hai**.
Nếu `ITenantConnectionResolver` cấu hình sai / cache lệch connection → **rò rỉ dữ liệu chéo tenant im lặng**,
không có `WHERE TenantId = @x` chặn lại. Cần test cô lập resolver + guard ở tầng connection.

## 🟠 #4 — Quy tắc kiến trúc mâu thuẫn code (nuốt exception trong engine)

`.claude-rules/architecture.md` (bản cũ) quy định *"Exception bubble lên — không swallow trong engine"*,
nhưng engine nuốt exception **8 chỗ**:
- `EventEngine.cs:180-186` (`catch → return []`)
- `ValidationEngine.cs:222-226, 248-252` (`catch → return false/skip`)
- `MetadataEngine.cs:168, 207` (`catch { }` rỗng)

**Đã xử lý mâu thuẫn:** cập nhật `architecture.md` để phân biệt *lỗi config* (phải nổi lên/log rõ) vs
*giá trị an toàn có chủ đích*. Việc nuốt lỗi im lặng che giấu config hỏng (AST/JSON lỗi) → field âm thầm
mất validation, bug khó truy.

## 🟠 #5 — God-class + dispatch bằng switch (SOLID)

- `ViewRepository.cs` (**1038 dòng**) ôm 5 trách nhiệm: metadata + i18n + build SQL + cascade lookup + tree reorder.
- `DynamicLookupRepository.cs` (**973**), `MasterDataRepository.cs` (**737**).
- Dispatch switch (vi phạm OCP): `EventEngine.cs:165`, `ImportEngine.cs:188,299`, `ContextParamResolver.cs:61`.
- Pattern dựng-SQL-động lặp ở 4-5 repo, mỗi nơi một kiểu → DRY + liên quan trực tiếp #2.

**Hướng:** action handler = Strategy + registry (thêm action = thêm 1 file, không sửa engine).

## 🟡 #6 — Nợ kỹ thuật đã đánh dấu, chưa xử lý

- `LookupController.cs:141` — `TODO(SEC1-4)`: endpoint tạm hạ quyền xuống "chỉ cần đăng nhập".
- `ConfigCache.cs:156` — `TODO(CC-3)`: permission cache trả `null` (chưa implement).
- `RestoreFormCommandHandler.cs:39-41` — workaround `GetByCode` không lấy record inactive → Restore có thể sai.
- `Program.cs:164` — keyring JWT còn in-memory → scale-out sẽ hỏng verify token.

---

## Thứ tự khắc phục đề xuất

| Ưu tiên | Việc | Giải quyết |
|---|---|---|
| 1 | Gom guard SQL về 1 lớp `SqlIdentifier` (validate + bracket + blocklist), thay mọi bản copy | #2, #5 |
| 2 | Test cho repo dynamic-SQL (đặc biệt injection cases) + tenant resolver | #1, #3 |
| 3 | Thống nhất chính sách exception trong engine | #4 |
| 4 | Đóng nợ SEC1-4, CC-3, RestoreForm, JWT keyring | #6 |

> **Trạng thái:** báo cáo — chưa sửa code. Khi bắt tay mục nào: chạy impact analysis (GitNexus) trước, báo blast radius, rồi mới code + build verify + commit.
