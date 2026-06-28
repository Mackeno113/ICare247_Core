# /analyze-metadata — Phân tích metadata 1 form

**Mục đích:** Bản đồ metadata 1 form: Sys_Table/Column/Relation → Ui_Form/Section/Field, version, cache.
**Input:** `$ARGUMENTS` = form code. Trống → hỏi user form nào.
**Output:** sơ đồ cấu trúc metadata + điểm cache/version + quan hệ FK.
**Agent gọi:** `metadata-engine` (read-only phần phân tích).

Thực hiện:
1. Đọc `docs/spec/02_DATABASE_SCHEMA.md` + `04_ENGINE_SPEC.md`; đọc live DB nếu cần.
2. Gọi `Agent` với `subagent_type: metadata-engine`, yêu cầu phân tích metadata form `$ARGUMENTS`.
3. Trình bày bản đồ; chỉ ra cache key (`CacheKeys.Form/FieldList`) + version liên quan.
