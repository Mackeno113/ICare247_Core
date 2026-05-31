# Codex Coding Style Feedback

> Cập nhật lần cuối: 2026-04-25

## Rules từ user (áp dụng cho Codex)

- Comment bằng Tiếng Việt — không mix ngôn ngữ trong cùng file
- Không EF Core — chỉ Dapper
- Không SELECT * — chỉ select cột cần thiết
- Không string interpolation vào SQL
- Async/await xuyên suốt — không .Result, không .Wait()
- CancellationToken truyền xuyên suốt
- Encoding safety: file `.xaml`, `.cs`, `.md`, `.yaml`, `.sql` phải giữ UTF-8; không dùng PowerShell `Get-Content` + `Set-Content` mặc định để rewrite toàn file có tiếng Việt.
- Khi sửa XAML/CS có tiếng Việt, ưu tiên `apply_patch`; nếu phải bulk rewrite thì đọc/ghi UTF-8 tường minh và scan mojibake (`Ã`, `Â`, `â€`, `ðŸ`, `áº`, `á»`, `Ä‘`, `Æ°`) trước khi build.

## WPF-specific (ConfigStudio)

- Dùng Prism 9 cho navigation
- Dùng DevExpress WPF controls (không WPF thuần)
- MVVM: ViewModel không reference View
- Không code-behind logic trong .xaml.cs (trừ khởi tạo)

## Feedback chưa có

_Chưa có feedback cụ thể từ user cho Codex. Cập nhật khi nhận được correction._
