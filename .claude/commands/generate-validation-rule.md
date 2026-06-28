# /generate-validation-rule — Sinh Val_Rule + AST

**Mục đích:** Sinh rule validate (Val_Rule_Type/Val_Rule/Val_Rule_Field) + biểu thức AST.
**Input:** `$ARGUMENTS` = field + điều kiện (vd "Tuoi >= 18", "NgayKetThuc > NgayBatDau").
**Output:** cấu hình rule + AST whitelist + resource key thông báo lỗi.
**Agent gọi:** `validation-engine`.

Thực hiện:
1. Đọc `docs/spec/03_GRAMMAR_V1_SPEC.md` + `23_VALIDATION_RULE_GUIDE.md` + `.claude-rules/ast-grammar.md`.
2. Gọi `Agent` với `subagent_type: validation-engine`, yêu cầu sinh rule cho `$ARGUMENTS` (chỉ AST whitelist, null-safe, tái dùng qua rule type).
3. Đảm bảo resource key `{formCode}.val.{fieldCode}.{RuleType}`. KHÔNG tự commit.
