# Coding Style Feedback

> Agent ghi lại mỗi khi user sửa lỗi coding style để không lặp lại.

## [2026-03-20] Không dùng MaterialDesign trong ConfigStudio WPF

Khi viết XAML mới cho Forms module, dùng MaterialDesign (`md:Card`, `md:PackIcon`, `MaterialDesignRaisedButton`) mà không đọc code hiện có trước.

**Why:** Project ConfigStudio.WPF.UI dùng **DevExpress + pure WPF** (không có MaterialDesign). Forms module không có `MaterialDesignThemes` trong .csproj. TASKS.md có chữ "MaterialDesign Dialog" nhưng đó chỉ là mô tả ý tưởng, không phải chỉ định thư viện.

**How to apply:** Trước khi viết XAML mới trong bất kỳ module nào → **đọc ít nhất 1 view hiện có trong cùng module** để xác định:
- Thư viện UI: DevExpress (`dx:`, `dxe:`, `dxg:`) + pure WPF — không có MaterialDesign
- Button pattern: custom `ControlTemplate` với `Border` + hover `Trigger`
- Icon: Unicode text (⚙ ✎ 👁 ⧉ 🔒 🔓) — không dùng PackIcon
- Colors: Tailwind palette (#3B82F6, #EF4444, #E2E8F0...)
- `DynamicResource` không fail lúc compile — chỉ crash runtime → phải kiểm tra kỹ sau mỗi lần refactor

## [2026-03-21] KHÔNG tự commit/push — chỉ làm khi user yêu cầu rõ ràng

**Why:** Tốn token, nhiễu git history, user muốn kiểm soát hoàn toàn khi nào push.

**How to apply:**
- SAU KHI sửa xong + build OK → **DỪNG LẠI**, chỉ báo "sửa xong, build OK"
- KHÔNG tự chạy `git add`, `git commit`, `git push` dù build thành công
- CHỈ commit/push khi user gõ rõ: "commit", "push", "/finish-task", hoặc tương đương
- Rule này áp dụng cho MỌI fix dù nhỏ hay lớn

## [2026-03-22] Xóa dữ liệu DB phải xác nhận — default No

Mọi thao tác xóa dữ liệu thực trong DB (DELETE SQL) phải hiển thị hộp thoại xác nhận trước khi thực thi.

**Why:** Xóa DB là không thể hoàn tác. User cần cơ hội từ chối khi nhấn nhầm.

**How to apply:**
- Trước khi gọi bất kỳ service method nào có DELETE → show confirm dialog (DevExpress `DXMessageBox` hoặc `IDialogService`)
- **Default button = No / Cancel** (không phải Yes)
- Nội dung dialog phải nêu rõ đối tượng bị xóa: tên, mã, hoặc ID
- Rule áp dụng cho cả: xóa record đơn lẻ, xóa hàng loạt, xóa cascade
- KHÔNG áp dụng cho: xóa item chỉ trong bộ nhớ (ObservableCollection chưa save DB)

## [2026-03-23] DebugLogger — thay thế Console.WriteLine

Mọi `Console.WriteLine($"[X] ...")` trong backend API **bắt buộc** dùng `DebugLogger`.

**Why:** Console.WriteLine không thể bật/tắt, không ghi ra file, không thống nhất format.

**How to apply:**
```csharp
// ❌ SAI
Console.WriteLine($"[LocalConfig] Nạp cấu hình từ: {path}");

// ✅ ĐÚNG
DebugLogger.Log("LocalConfig",  $"Nạp cấu hình từ: {path}");
DebugLogger.Warn("LocalConfig", $"File chưa tồn tại — tạo template tại: {path}");
DebugLogger.Error("LocalConfig",$"Lỗi đọc file: {ex.Message}");
```

**Chi tiết:**
- Class static: `ICare247.Api.DebugLogger` tại `src/backend/src/ICare247.Api/DebugLogger.cs`
- Cấu hình qua `appsettings.local.json` section `"DebugLog"`: `Enabled`, `WriteToFile`, `FilePath`
- Gọi `DebugLogger.Configure(builder.Configuration)` trong `Program.cs` ngay sau `builder.AddLocalConfig()`
- Khi `WriteToFile=true` → ghi ra `%APPDATA%\ICare247\Api\debug.log`
- Rule doc đầy đủ: `.claude-rules/debug-logger.md`
- Áp dụng cho: backend API, LocalConfigLoader, bất kỳ code nào chạy trước Serilog
- KHÔNG áp dụng cho: Serilog `ILogger<T>` trong services/repositories (giữ nguyên)

## [2026-04-20] Memory files luôn commit lên master — không phải feature branch

**Why:** Memory files (`.claude/memory/`) là global/cross-branch — dùng chung cho mọi session, mọi máy. Commit lên feature branch làm memory bị lock trong branch đó.

**How to apply:**
- Sau khi cập nhật bất kỳ file nào trong `.claude/memory/` → checkout master, commit ở đó, push master
- Nếu đang trên feature branch → cherry-pick commit memory sang master
- KHÔNG commit memory files trực tiếp lên feature branch

## [2026-03-20] Run.Text binding trong XAML phải dùng Mode=OneWay

WPF `<Run Text="{Binding Prop}" />` mặc định là **TwoWay**. Nếu property có `private set` → throw `InvalidOperationException` runtime.

**Why:** Khác với `TextBlock.Text` (mặc định OneWay), `Run.Text` mặc định TwoWay vì Run nằm trong FlowDocument có thể editable.

**How to apply:** Mọi `<Run Text="{Binding ...}" />` phải thêm `Mode=OneWay` — nhất là khi ViewModel property có `private set` hoặc chỉ có getter.
