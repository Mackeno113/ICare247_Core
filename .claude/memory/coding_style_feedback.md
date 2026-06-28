# Coding Style Feedback

> Agent ghi lại mỗi khi user sửa lỗi coding style để không lặp lại.

## [2026-06-22] Commit message KHÔNG có trailer `Co-Authored-By`

**Why:** Quy tắc của user cho repo này — không nhét `Co-Authored-By: Claude ...` vào commit message.

**How to apply:** Mọi `git commit` → **bỏ hẳn** dòng `Co-Authored-By:` (kể cả khi hướng dẫn mặc định của harness yêu cầu thêm). Message chỉ gồm tiêu đề + thân mô tả.

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

## [2026-06-01] KHÔNG tự ý sửa code — phải trình bày và chờ user chốt

**Why:** User cần kiểm soát mọi thay đổi code. Tự ý sửa gây khó debug, mất context, và tạo ra thay đổi ngoài ý muốn.

**How to apply:**
- Khi phát hiện bug hoặc cần thay đổi code → **DỪNG LẠI**
- Trình bày đầy đủ theo thứ tự:
  1. **Nguyên nhân** — root cause cụ thể (file, dòng, logic sai ở đâu)
  2. **Cách xử lý** — phương án đề xuất, có thể đề xuất nhiều phương án nếu có trade-off
  3. **Các bước thực hiện** — liệt kê rõ từng file sẽ thay đổi, thay đổi gì
- **Chờ user xác nhận** ("ok", "làm đi", "dùng phương án X") rồi mới bắt đầu code
- Rule áp dụng cho: mọi thay đổi code, SQL, config, XAML — dù nhỏ hay lớn
- NGOẠI LỆ: chỉ tự sửa ngay khi user nói rõ "sửa luôn", "fix đi", hoặc tương đương

## [2026-03-20] Run.Text binding trong XAML phải dùng Mode=OneWay

WPF `<Run Text="{Binding Prop}" />` mặc định là **TwoWay**. Nếu property có `private set` → throw `InvalidOperationException` runtime.

**Why:** Khác với `TextBlock.Text` (mặc định OneWay), `Run.Text` mặc định TwoWay vì Run nằm trong FlowDocument có thể editable.

**How to apply:** Mọi `<Run Text="{Binding ...}" />` phải thêm `Mode=OneWay` — nhất là khi ViewModel property có `private set` hoặc chỉ có getter.

<!-- ───────── Gộp từ agent memory (2026-06-12) ───────── -->

## [Nguyên tắc số 1] LUÔN HỎI TRƯỚC — không mock, không tự suy diễn

LUÔN HỎI TRƯỚC khi quyết định làm bất cứ việc gì. Không tự ý chọn cách làm, không tự bịa dữ liệu mẫu (mock), không tự suy diễn yêu cầu khi còn nhiều hướng.

**Why:** User yêu cầu màn test grid; agent tự quyết làm mock data tĩnh thay vì lấy dữ liệu thật theo cấu hình → sai ý định. User nhấn mạnh đây là nguyên tắc số 1 (đã ghi đầu CLAUDE.md).

**How to apply:** Khi yêu cầu chưa rõ hoặc nhiều hướng (dữ liệu thật vs mock, chọn API/bảng/field, phạm vi) → DỪNG và HỎI rồi mới code. Mặc định dữ liệu lấy THẬT theo cấu hình hệ thống.

## [DevExpress] Xác minh thuộc tính/API qua reflection DLL — đừng đoán

Khi dùng thuộc tính/API DevExpress Blazor (DxGrid, editors…), PHẢI xác minh tên tồn tại trước khi dùng — đừng đoán theo trí nhớ.

**Why:** `DataView.razor` từng dùng `FilterRowCellVisible` (không tồn tại ở DX 25.2.3) → DxGrid ném `InvalidOperationException`, rớt toàn bộ cột. Tên đúng: `FilterRowEditorVisible`.

**How to apply:**
1. Đọc lỗi thật ở **F12 Console** trước khi kết luận (đừng đoán nguyên nhân).
2. Reflect DLL đúng version: `C:\Users\Mackeno_01\.nuget\packages\devexpress.blazor\25.2.3\lib\net8.0\DevExpress.Blazor.v25.2.dll` — tạo console net9 (FrameworkReference Microsoft.AspNetCore.App + PackageReference DevExpress.Blazor) rồi `GetType(...).GetProperties()`. PowerShell 5.1 KHÔNG load được DLL net8.0.
3. Tài liệu: `docs/reference/DEVEXPRESS_DXGRID_PROPERTIES.md`.

## [Comment] Mọi hàm C# — XML doc tiếng Việt + sự kiện theo sau

Mọi hàm C# phải có XML doc đầy đủ bằng **tiếng Việt**: `<summary>` (ý nghĩa), `<param>`, `<returns>`, và `<remarks>` (bắt buộc nếu có side-effect: liệt kê event/trigger/state đổi SAU khi hàm chạy).

**Why:** User yêu cầu code self-document tiếng Việt, đặc biệt nêu rõ "sự kiện theo sau" để dễ trace luồng.

**How to apply:** Mọi hàm C# mới hoặc khi sửa hàm cũ. Template: `.claude-rules/comment-rules.md`.

## [Form runtime] Payload Lưu = field IsVisible (KHÔNG lọc theo read-only)

Khi build payload Lưu form (Thêm/Sửa), gửi **mọi field đang hiển thị** (`IsVisible=true`). Field hiển thị nhưng bị khóa (read-only / `LockOnEdit`) **vẫn gửi**. Chỉ field ẩn (`IsVisible=false`) mới không gửi.

**Why:** Lọc theo `EffectiveReadOnly`/`IsReadOnly` làm cột Mã/khóa (`LockOnEdit`) thiếu khỏi payload khi Sửa → server báo "bắt buộc" dù màn hình có giá trị. Tiêu chí loại trừ là **tính hiển thị**, không phải read-only.

**How to apply:** `MasterDataForm.SaveAsync` (và mọi chỗ build values gửi API): `_fieldStates.Where(fs => fs.IsVisible)`. Không dùng `!EffectiveReadOnly`/`!IsReadOnly`.

## [Build] Build fail chỉ do file-lock = PASS, đừng rebuild

Khi `dotnet build` báo lỗi **chỉ do file-lock** (MSB3021/MSB3027 — DLL trong `bin/` bị app đang chạy giữ) thì **coi như build XONG** (biên dịch source đã pass, chỉ kẹt bước copy output). KHÔNG rebuild, không kill process, trừ khi user yêu cầu rõ.

**Why:** App (Api/Blazor) thường đang chạy lúc dev; lock DLL là bình thường, không phản ánh lỗi code.

**How to apply:** Toàn lỗi là MSB3021/MSB3027 → báo "build ok (chỉ kẹt copy do app đang chạy)" và đi tiếp. Cần chắc không có lỗi C# thật → build 1 project KHÁC không khóa bin (vd Infrastructure/Application).

## [Debug] So DB ↔ API ↔ UI TRƯỚC khi đọc code — ground truth trước, lý thuyết sau

Khi 1 cờ/giá trị hiển thị sai (vd `LockOnEdit` web vẫn cho sửa dù cấu hình bật), **đừng đọc code đoán bug**. Lấy SỰ THẬT 3 tầng đặt cạnh nhau: **giá trị trong DB** → **JSON API trả về** (DevTools → Network, hoặc nhờ user copy) → **trạng thái UI**. Tầng nào lệch → khoanh đúng tầng đó, CHỈ KHI ĐÓ mới đọc code tầng đó.

**Why:** Lần fix `LockOnEdit` (form `DM_CHINHANHNGANHANG`, field LookupBox) chậm vì 3 sai lầm quy trình: (1) đọc ~10 file frontend săn bug trong khi web vốn ĐÚNG; (2) đọc nhầm output DB (`Field_Code=[]` tưởng `''` nhưng là `NULL`) rồi dựng cả giả thuyết COALESCE sai, suýt sửa nhầm; (3) tự vật lộn sqlcmd/sandbox + mint JWT gọi API thay vì hỏi user. Khi user ép "lấy JSON thật" + "ko tự check" → so DB(`true`) vs API(`false`) ra ngay lỗi backend trong 1 bước (FormRepository copy tay `FieldMetadata` cho field dynamic làm rớt `LockOnEdit`).

**How to apply:**
- Bug "giá trị hiển thị sai": hỏi/lấy **response API thật** + **giá trị DB thật** NGAY, trước khi đọc code.
- DB(đúng) + API(sai) → lỗi backend đọc/map; UI khác API → lỗi frontend. Bisect rồi mới đọc code.
- Phân biệt rõ `NULL` vs `''` vs `0` khi đọc output — đừng mặc định.
- Cần connection string / 1 dòng query / 1 response Network → **hỏi user**, đừng đốt lượt tự mở khóa môi trường.
- Gotcha kèm theo: copy tay object init (`new FieldMetadata { ... }`) dễ rớt property mới thêm → copy ĐỦ field hoặc cân nhắc `record` + `with`.
