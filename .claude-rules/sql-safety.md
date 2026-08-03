# SQL Safety Rules — ICare247

> Nguồn: audit backend 2026-08-03 (`docs/reviews/2026-08-03-backend-code-audit.md`, mục #2/#3/#5).
> Áp dụng cho MỌI repository/service dựng SQL động (ViewRepository, DynamicLookupRepository,
> MasterDataRepository, FkLookupResolver, MaCodeGenerator, ConfigSyncService…).

## Nguyên tắc cốt lõi

**Giá trị → LUÔN qua Dapper parameter (`@param`). Identifier (tên bảng/cột) → whitelist + bracket.
KHÔNG BAO GIỜ nối chuỗi giá trị người dùng vào SQL.**

## 1. Một guard duy nhất — KHÔNG copy-paste

> ⛔ KHÔNG tự chế `SafeIdentifierRegex()` / `Bracket()` riêng trong từng file.

- Guard xác thực identifier và bọc `[]` phải nằm ở **một helper dùng chung** (VD `SqlIdentifier`),
  mọi repo gọi vào đó. Lý do: guard bảo mật copy-paste sẽ **drift** — audit 2026-08-03 phát hiện
  ≥ 5 bản `SafeIdentifierRegex` với **2 pattern lệch nhau** (`[a-zA-Z0-9_]` vs `[a-zA-Z0-9_.]`).
- Nếu cần cho phép `schema.table` (có dấu `.`) → tách hàm riêng, tách rõ (VD `IsSafeQualifiedName`),
  KHÔNG nới regex identifier đơn.
- `Bracket(ident) => "[" + ident.Replace("]", "]]") + "]"` — chỉ định nghĩa 1 lần, dùng chung.

## 2. Whitelist trước, blocklist sau (không dựa blocklist đơn độc)

- Identifier: **chỉ nhận** khi khớp regex an toàn (`^[a-zA-Z_][a-zA-Z0-9_]*$`) → deny-by-default.
- Fragment SQL thô từ Config DB (`FilterSql`, `custom_sql`, `OrderBy`): admin-trust NHƯNG vẫn phải
  qua blocklist keyword DDL/DML (`ContainsDangerousKeyword`) **như lớp phụ**, không phải lớp chính.
  Ưu tiên diễn đạt khai báo (filter/param có kiểu) thay vì cho nhập SQL thô khi có thể.
- Tên proc chạy động: bắt buộc nằm trong whitelist registry (VD `Doc_Proc_Registry`) — deny-by-default.

## 3. Cô lập tenant — cần lớp phòng thủ thứ hai

- Mô hình ADR-035 (1 Config-DB = 1 tenant, connection-per-tenant) → `tenantId` KHÔNG lọc trong SQL.
- Vì chỉ có 1 lớp phòng thủ (connection resolver), BẮT BUỘC:
  - Có test cô lập cho `ITenantConnectionResolver` (không lẫn connection giữa tenant).
  - Guard/log tại tầng connection khi resolve — không "tin ngầm" cache.
- KHÔNG viết code ghi vào Config DB từ context đang chạy trong tenant (defense-in-depth, xem debugging.md lớp 3).

## 4. Bắt buộc có test cho SQL động

- Mọi repo dựng SQL động PHẢI có unit test kèm **ca injection** (identifier có `;`, `--`, `]`, khoảng trắng,
  keyword DDL) → assert bị từ chối/không lọt vào câu SQL.
- Đây là điều kiện review: thêm/sửa repo dynamic-SQL mà không kèm test = chưa xong.

## Checklist khi review repo có SQL

```
✅ Giá trị người dùng đi qua @param (Dapper), KHÔNG nối chuỗi
✅ Identifier validate bằng helper CHUNG + Bracket() chung (không copy-paste guard)
✅ Fragment SQL thô: whitelist/khai báo trước, blocklist chỉ là lớp phụ
✅ Proc động nằm trong whitelist registry
✅ Có unit test kèm ca injection cho phần dựng SQL
```
