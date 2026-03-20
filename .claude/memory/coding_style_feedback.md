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

## [2026-03-20] Run.Text binding trong XAML phải dùng Mode=OneWay

WPF `<Run Text="{Binding Prop}" />` mặc định là **TwoWay**. Nếu property có `private set` → throw `InvalidOperationException` runtime.

**Why:** Khác với `TextBlock.Text` (mặc định OneWay), `Run.Text` mặc định TwoWay vì Run nằm trong FlowDocument có thể editable.

**How to apply:** Mọi `<Run Text="{Binding ...}" />` phải thêm `Mode=OneWay` — nhất là khi ViewModel property có `private set` hoặc chỉ có getter.
