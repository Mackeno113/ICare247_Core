# /review-db-schema — Soát bảng/quan hệ/index

**Mục đích:** Kiểm thiết kế bảng mới/migration: cột audit, soft-delete, FK, index, tenant.
**Input:** `$ARGUMENTS` = bảng/file migration. Trống → soát file `.sql` trong `git diff`.
**Output:** ✅/❌ + đề xuất schema/index/cache.
**Agent gọi:** `sql-server-optimizer` (phần index/query) — _(Database Architect agent có thể nhập sau)._

Thực hiện (đọc `docs/spec/02_DATABASE_SCHEMA.md` + memory verify-live-db-schema):
1. **Đọc DDL thật/live DB** trước (DB thật hay lệch file `.sql` cũ).
2. Bảng phải có audit (`CreatedBy/CreatedAt/...`) + `Is_Active`/`IsDeleted` (ADR-022); set audit tường minh, không dựa DEFAULT.
3. FK đúng; cột nghiệp vụ tiếng Việt, cột auto tiếng Anh (Id/CreatedBy/.../Ver).
4. Index cho cột WHERE/JOIN; filtered index soft-delete; gọi `sql-server-optimizer` nếu cần tối ưu truy vấn.
5. Tác động cache/version (`Sys_Cache_Invalidation`). Báo ✅/❌ + fix.
