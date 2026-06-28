# /generate-devexpress-form — Dựng form Blazor + DevExpress

**Mục đích:** Dựng/sửa form nghiệp vụ Blazor render từ config, đúng theme khóa.
**Input:** `$ARGUMENTS` = form code / mô tả field + layout. Trống → hỏi user form nào.
**Output:** Razor + control-map, tuân skill `icare247-admin-ui` (Fluent Light, ≤3 màu, 1 CTA, i18n).
**Agent gọi:** `form-engine` + skill `icare247-admin-ui`.

Thực hiện:
1. **Bắt buộc** đọc skill `icare247-admin-ui` + `docs/spec/24_BLAZOR_CONTROL_RENDERER_SPEC.md`. Đọc 1 form hiện có.
2. Gọi `Agent` với `subagent_type: form-engine`, yêu cầu lắp form từ config (KHÔNG hardcode field/chuỗi; mọi text qua i18n).
3. Verify theme không vỡ (≤3 màu, surface phẳng, sticky header). KHÔNG tự commit.
