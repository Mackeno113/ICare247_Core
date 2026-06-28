# /build-dependency-graph — Dựng/kiểm Sys_Dependency

**Mục đích:** Dựng đồ thị phụ thuộc field/rule (`Sys_Dependency`) + thứ tự tính (topological).
**Input:** `$ARGUMENTS` = form code / bảng / field. Trống → hỏi user phạm vi.
**Output:** danh sách cạnh phụ thuộc + thứ tự evaluate + cảnh báo chu trình (cycle).
**Agent gọi:** `metadata-engine` (sở hữu `Sys_Dependency`), phối `validation-engine` khi liên quan rule.

Thực hiện:
1. Đọc `docs/spec/04_ENGINE_SPEC.md` (dependency order). Đọc cấu hình rule/field liên quan.
2. Gọi `Agent` với `subagent_type: metadata-engine`, yêu cầu dựng graph + topo-sort cho `$ARGUMENTS`.
3. Cảnh báo nếu có chu trình; nêu thứ tự tính an toàn.
