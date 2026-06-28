# /review-architecture — Soát kiến trúc 1 thay đổi

**Mục đích:** Kiểm Clean Architecture 4 lớp + DI + CQRS cho 1 feature/diff.
**Input:** `$ARGUMENTS` = feature/file. Trống → soát `git diff`.
**Output:** ✅/❌ từng tiêu chí + đề xuất nắn lại.
**Agent gọi:** _(chưa có Solution Architect agent — xử lý inline; có thể nhập `backend-architect` sau)._

Thực hiện (đọc `.claude-rules/architecture.md` + `BRAIN.md §4`):
1. Layer đúng chiều: Domain ← Application ← Infrastructure; **Api KHÔNG import Infrastructure** (trừ Program.cs).
2. Không `new` Infrastructure trong Api; chỉ qua DI.
3. CQRS/MediatR + naming chuẩn; mỗi file 1 class/interface/record.
4. Engine không nuốt exception; async xuyên suốt.
5. Báo cáo ✅/❌ + fix cụ thể (file, dòng).
