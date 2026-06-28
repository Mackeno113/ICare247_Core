# /generate-tests — Sinh unit/integration test xUnit

**Mục đích:** Sinh test xUnit cho code mới/sửa, bám pattern test hiện có.
**Input:** `$ARGUMENTS` = class/handler/file cần test. Trống → soát `git diff` lấy code mới.
**Output:** file test trong `src/backend/tests/...` + danh sách kịch bản (ưu tiên Cao/Vừa/Thấp).
**Agent gọi:** `test-generator`.

Thực hiện:
1. Xác định code under test; đọc 1–2 test hiện có để khớp pattern.
2. Gọi `Agent` với `subagent_type: test-generator`, truyền code + tiêu chí đúng.
3. Nếu cần dữ liệu seed/cấu hình chưa rõ → **hỏi user** (không bịa mock). Nhắc chạy `dotnet test`. KHÔNG tự commit.
