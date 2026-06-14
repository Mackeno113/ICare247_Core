# Hướng dẫn dùng i18n — ICare247 Web UI

> Mục tiêu: **mọi text hiển thị, thông báo, hỏi xóa đều chạy i18n** — không hardcode chữ.
> Tài liệu này là hướng dẫn *thực hành*. Quy ước key chi tiết xem
> [docs/spec/10_RESOURCE_KEY_CONVENTION.md](spec/10_RESOURCE_KEY_CONVENTION.md).

---

## 1. Hai hệ i18n — chọn đúng hệ

| | Hệ 1 — Metadata-driven | Hệ 2 — Hand-coded |
|---|---|---|
| Dành cho | Màn do **ConfigStudio WPF** sinh: label/placeholder/tooltip/caption/title của form, field, section, tab, view | **Chrome viết tay**: nút, dialog, thông báo, cột lệnh, tiêu đề trang bespoke |
| Kho lưu | `Sys_Resource` (DB Config) — backend resolve theo `Lang_Code` | **JSON overlay client**: `_content/ICare247.UI.Shared/i18n/{lang}.json` + host `wwwroot/i18n/{lang}.json` |
| Cấu hình | Trong WPF (nút 🌐 I18nEditorDialog) | Gõ key trong code + dịch trong file json |
| API | DTO metadata (key đã resolve sẵn) | `LocalizationService.L(key, fallback, args)` |

> ⚠️ **KHÔNG** lưu chuỗi hand-coded vào `Sys_Resource`. Mọi chuỗi viết tay đi qua `L()`.

Phần còn lại của tài liệu tập trung vào **Hệ 2 (hand-coded)** — phần dev đụng hằng ngày.

---

## 2. Dùng `L()` trong code

Triết lý: **key thuộc về code, base tiếng Việt nằm ngay tại chỗ gọi (fallback)**.
JSON chỉ là *lớp phủ giá trị* để dịch — không khai báo key trong JSON.

### 2.1 Trong file `.razor`

```razor
@inherits LocalizedComponentBase   @* để tự vẽ lại khi đổi ngôn ngữ *@

<button>@Loc.L("common.action.save", "Lưu")</button>
<span>@Loc.L("masterdata.form.haserrors", "Có {0} lỗi cần sửa.", errorCount)</span>
```

- `@inherits LocalizedComponentBase` cung cấp sẵn `Loc` và tự `StateHasChanged` khi đổi ngôn ngữ.
- Component **không** kế thừa được (vì đã `@inherits` khác) thì `@inject LocalizationService Loc`
  — nhưng sẽ không tự vẽ lại lúc đổi ngôn ngữ giữa chừng (chấp nhận với màn ít đổi).

### 2.2 Trong file `.cs` (service…)

```csharp
private readonly LocalizationService _loc;   // inject qua ctor
...
return new AuthLoginResult(false, _loc.L("auth.error.invalidcredentials",
    "Tên đăng nhập hoặc mật khẩu không đúng."));
```

### 2.3 Tham số `{0}`, `{1}`

Truyền qua `args` của `L()`, format theo `CultureInfo` hiện hành:

```csharp
Loc.L("view.subtitle", "Bảng {0} · {1}", tableCode, viewType)
```

---

## 3. Quy ước đặt key

```
common.{nhóm}.{tên}        -- chuỗi DÙNG CHUNG nhiều màn
{scope}.{...}              -- chuỗi RIÊNG 1 trang/feature (scope = slug trang)
```

| Nhóm `common.*` | Ví dụ |
|---|---|
| Hành động | `common.action.save` / `.cancel` / `.create` / `.edit` / `.delete` / `.refresh` / `.close` |
| Lọc | `common.filter.search` / `.reset` / `.searching` |
| Lookup/combo | `common.combo.loading` / `.loaderror` / `.choose` |
| Khác | `common.validation.required` · `common.label.error` · `common.column.actions` |

Scope riêng theo trang, ví dụ: `masterdata.list.*`, `masterdata.form.*`, `view.*`,
`formrunner.*`, `delete.*`, `auth.error.*`, `dev.i18n.*`, `nav.*`.

> Key viết **thường, phân cấp bằng dấu chấm**, suy từ cấu trúc. Đừng đặt key vô nghĩa (`text1`).

---

## 4. ❓ Làm sao biết một text đang dùng key nào?

Đây là điểm hay vướng. Có **3 cách bù chéo**:

### Cách 1 — Màn tra cứu in-app `/dev/i18n` (khuyên dùng)

Mở **menu ▸ Tra cứu i18n** (hoặc gõ URL `/dev/i18n`):

- **Gõ text vào ô tìm** (vd "Đăng nhập") → ra ngay **key** tương ứng.
- Bảng hiển thị: `key · văn bản gốc vi · bản dịch hiện tại · nơi dùng (file:line) · nút chép key`.
- Nút **"Soi key"**: bật lên thì mọi chữ trên app hiện kèm `⟨key⟩` ngay tại chỗ → dò bằng mắt.
- Mục **"Key chỉ thấy ở runtime"**: các key dựng động (`L($"x.{code}", …)`) mà quét tĩnh bỏ sót.

### Cách 2 — Quét source bằng tool (xem §5)

Tool sinh `catalog.json` (key → text → file:line). Màn `/dev/i18n` đọc chính file này.

### Cách 3 — Grep thủ công

Tìm chuỗi tiếng Việt trong source → đọc tham số 1 của `L()` cùng dòng.

---

## 5. Công cụ quét — `tools/I18nScanner`

Quét toàn bộ frontend, trích mọi `L("key","fallback")` + dò **chuỗi hardcode chưa bọc**.

```bash
# Xem tổng kết (không ghi file)
dotnet run --project tools/I18nScanner

# Ghi thật: catalog.json + merge {lang}.json + báo cáo
dotnet run --project tools/I18nScanner -- --lang en --write
```

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| `--root <dir>` | `src/frontend` | Thư mục quét |
| `--lang <code>` | `en` | Ngôn ngữ merge khung `{lang}.json` |
| `--write` | (tắt) | Bật mới thực sự ghi file |

**Xuất ra** (mỗi i18n-root = project có `wwwroot/i18n`):

- `catalog.json` — key → text vi → nơi dùng → cờ key động (màn `/dev/i18n` fetch).
- `{lang}.json` — thêm key thiếu (value rỗng = chưa dịch), **giữ** bản dịch cũ, **không xoá** key lạ.
- `i18n-report.md` — chuỗi VN hardcode cần bọc + các `L()` key dựng động.

> **Tự động:** build host ở Debug đã tự chạy scanner (target trong `ICare247_UI.csproj`,
> incremental — chỉ chạy khi `.razor/.cs` đổi). Tắt: `dotnet build -p:I18nScanOnBuild=false`.

---

## 6. Thêm / sửa bản dịch (vd tiếng Anh)

1. Đảm bảo catalog mới: chạy scanner `--write` (hoặc build Debug).
2. Mở file overlay tương ứng với project chứa key:
   - Key trong host → `src/frontend/ICare247_UI/wwwroot/i18n/en.json`
   - Key trong Shared → `src/frontend/ICare247.UI.Shared/wwwroot/i18n/en.json`
3. Điền value cho key (key rỗng `""` = chưa dịch → app tự fallback về vi).

```json
{
  "common.action.save": "Save",
  "auth.error.invalidcredentials": "Invalid username or password."
}
```

> Tiếng Việt (base) **không cần file json** — đã nằm ở fallback trong code.

---

## 7. Thêm một ngôn ngữ mới

1. Thêm dòng vào `src/frontend/ICare247.UI.Shared/wwwroot/i18n/languages.json`:
   ```json
   [ { "code": "vi", "name": "Tiếng Việt" },
     { "code": "en", "name": "English" },
     { "code": "ja", "name": "日本語" } ]
   ```
2. Sinh khung file mới: `dotnet run --project tools/I18nScanner -- --lang ja --write`
3. Dịch các value trong `ja.json` của host + Shared.

Bộ chuyển ngôn ngữ (`LanguageSwitcher`) tự đọc `languages.json` → ngôn ngữ mới xuất hiện.
Có sẵn mục **⟦Pseudo⟧** (dev) để soát chuỗi chưa i18n / tràn layout.

---

## 8. Khi nào KHÔNG bọc `L()`

Không phải mọi literal đều cần i18n. **Cố ý để nguyên**:

- **Log của dev** — `_logger.LogError("…")`. Là chữ kỹ thuật, không hiển thị cho người dùng.
- **Message kỹ thuật của exception** — `throw new HttpRequestException("…")`.
- **Data-fallback đã render qua `L`** — ví dụ tiêu đề trong `AppNav.cs` / cột `PermissionMatrix`
  là tham số `title`/`def` được `Loc.L(key, title)` dùng ở nơi render → bản thân literal là *fallback đúng chỗ*.
- **Tên ngôn ngữ** ("Tiếng Việt", "日本語") — không dịch theo locale.

> Scanner đã tự loại log/throw/comment. File dữ-liệu-fallback đánh dấu `i18n:skip-hardcode`
> trong comment để scanner bỏ qua (xem `AppNav.cs`).

---

## 9. Checklist khi thêm màn / chuỗi mới

- [ ] Mọi chữ hiển thị bọc `Loc.L("scope.key", "Tiếng Việt")`.
- [ ] Component dịch → `@inherits LocalizedComponentBase`.
- [ ] Key theo quy ước `common.*` (dùng chung) hoặc `{scope}.*` (riêng trang).
- [ ] Chạy scanner / build Debug → kiểm `i18n-report.md` không còn hardcode mới.
- [ ] Cần đa ngôn ngữ → điền value trong `{lang}.json`.

---

## 10. Tham chiếu

- Quy ước key đầy đủ (gồm hệ metadata WPF): [docs/spec/10_RESOURCE_KEY_CONVENTION.md](spec/10_RESOURCE_KEY_CONVENTION.md)
- Tool: [tools/I18nScanner/README.md](../tools/I18nScanner/README.md)
- Dịch vụ: `src/frontend/ICare247.UI.Shared/Services/I18n/LocalizationService.cs`
- Màn tra cứu: `src/frontend/ICare247_UI/Pages/Dev/I18nToolsPage.razor`
