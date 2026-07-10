Hoàn thành task hiện tại. Thực hiện đúng thứ tự:

1. Build verify **solution bị ảnh hưởng**. `.slnx` KHÔNG nằm ở gốc repo — luôn dùng đường dẫn đầy đủ:
   - Backend: `dotnet build src/backend/ICare247.slnx`
   - Web: `dotnet build src/frontend/ICare247_UI.slnx`
   - ConfigStudio: `dotnet build src/frontend/ConfigStudio.WPF.UI/ConfigStudio.WPF.UI.slnx`
   - Có test → `dotnet test src/backend/tests/ICare247.Application.Tests/ICare247.Application.Tests.csproj`
   - Fail → sửa lỗi trước, KHÔNG tiếp tục. (Fail chỉ do **file-lock** vì app đang chạy = coi như xong.)
2. **Soát ADR — thay đổi này có làm ADR nào "bắt kịp" không?**
   - Vừa code xong thứ mà một ADR mô tả? → cập nhật bảng **`TASKS.md § Trạng thái triển khai ADR`**.
   - ⛔ **KHÔNG sửa `architecture_decisions.md`** để ghi trạng thái — ADR là quyết định bất biến.
     File đó không còn dòng `Status:` (xem luật ở đầu file: 8/18 dòng cũ đã từng SAI vì lý do này).
   - Có quyết định kiến trúc **mới** (không phải trạng thái) → viết ADR mới, đánh số tiếp.
3. Cập nhật `TASKS.md`:
   - Move task từ 🔴 In Progress → ✅ Done
   - Ghi note nếu có quyết định quan trọng vào Decisions Log
4. Cập nhật `.claude/memory/last_session.md`:
   - Ghi task vừa hoàn thành vào "Đã làm"
   - Ghi ngày + task tiếp theo gợi ý
5. Cập nhật `.claude/memory/project_current_phase.md` nếu phase thay đổi
6. Xóa flag reminder: `rm -f "$(git rev-parse --show-toplevel)/.claude/.memory_needs_update"`
7. Stage tất cả file liên quan (code + TASKS.md + memory)
8. Hỏi user: "Commit với message gì?" (đề xuất message phù hợp)
9. Sau khi user confirm → commit
10. Hỏi user: "Push luôn không?" (quan trọng nếu user chuyển máy)
