# 27 — Đặc tả: Quản lý thông số hệ thống (System Settings)

> **Trạng thái:** ĐỀ XUẤT (chưa code) — chốt thiết kế trước khi hiện thực.
> **Quyết định đã chốt:** UI = **Blazor web admin** · Lưu trữ = **Hybrid (file + DB)** · Phạm vi = **chỉ thông số vận hành** (không sửa secret qua UI).
> **Liên quan:** [LocalConfigLoader](../../src/backend/src/ICare247.Api/LocalConfigLoader.cs) (file config hiện tại) · ADR-018 (multi-tenant) · ADR-023 (RBAC/menu) · [26_FILE_UPLOAD_SPEC](26_FILE_UPLOAD_SPEC.md) (ví dụ section FileStorage).

---

## 1. Mục tiêu & phạm vi

Cho phép **quản trị viên chỉnh thông số vận hành của hệ thống qua giao diện web**, với **gợi ý sẵn** (nhãn, mô tả, giá trị mặc định, lựa chọn hợp lệ, ràng buộc) — thay vì sửa tay `appsettings.local.json`.

**Trong phạm vi (v1):**
- `FileStorage` (Provider, BaseRoot, DbThresholdBytes, MaxBytes, Validation.AllowedExtensions, Image.*).
- `DebugLog` (Enabled, WriteToFile, FilePath).
- `Cache` (Enabled).
- `Serilog.MinimumLevel` (Default + Override theo namespace).

**Ngoài phạm vi (v1):**
- **Secret** (ConnectionStrings, Jwt.SecretKey, FileStorage.Object.SecretKey): CHỈ hiển thị trạng thái *đã đặt / chưa đặt*, KHÔNG sửa qua UI (giảm rủi ro sập app). Vẫn sửa tay ở file.
- Thông số per-tenant (những thứ này là **cấp deployment**, áp cho cả instance API).

---

## 2. Kiến trúc tổng quan

```
Schema descriptor (C#)  ── nguồn "gợi ý" (nhãn/mô tả/kiểu/mặc định/options/ràng buộc)
        │
        ▼
AdminSettingsController (RBAC admin)
   GET  /schema   → catalog thông số
   GET  /values   → giá trị hiệu lực hiện tại (secret che ●●●)
   PUT  /section  → validate theo schema → ghi
        │
        ├─► LỚP FILE  : appsettings.local.json (bootstrap + secret, giữ nguyên)
        └─► LỚP DB    : Sys_Config (thông số vận hành, dùng chung mọi node)
                          │
             SysConfigConfigurationProvider nạp ĐÈ lên file
                          │
             services.Configure<FileStorageOptions>… tự nhận giá trị DB
                          │
             UI render form từ /schema + /values
```

**Ý tưởng "Form Engine cho cấu hình":** UI không hardcode field — render động từ **schema**, hệt như Form Engine render form từ metadata.

---

## 3. Schema descriptor

Khai bằng **C# descriptor** (versioned theo code, gắn validation compile-safe). Đặt ở `Application/Settings/`.

```csharp
public sealed record SettingDescriptor(
    string Section,          // "FileStorage"
    string Key,              // "Provider"  (hỗ trợ path lồng: "Validation.AllowedExtensions")
    string Label,            // "Backend lưu file lớn"
    string Hint,             // mô tả dài (nguồn "gợi ý")
    SettingKind Kind,        // Bool | Int | Long | Enum | Text | Path | List | Secret
    object? Default,
    string[]? Options,       // cho Enum (Db/FileSystem/Object)
    bool RequiresRestart,    // true nếu options bind qua IOptions (snapshot lúc start)
    bool IsSecret,           // true → không trả plaintext, không sửa (v1)
    string? ValidationRegex, // ràng buộc chuỗi
    (long Min, long Max)? Range);

public enum SettingKind { Bool, Int, Long, Enum, Text, Path, List, Secret }

public sealed class SettingsSection(string Name, string Label, string Hint, IReadOnlyList<SettingDescriptor> Settings);
```

**Ví dụ catalog (trích):**

| Section | Key | Kind | Default | Options | Restart |
|---|---|---|---|---|---|
| FileStorage | Provider | Enum | Db | Db/FileSystem/Object | ✔ |
| FileStorage | BaseRoot | Path | "" | | ✔ |
| FileStorage | MaxBytes | Long | 52428800 | | ✔ |
| FileStorage | Validation.AllowedExtensions | List | (default 16 đuôi) | | ✔ |
| FileStorage | Image.Quality | Int (1–100) | 82 | | ✔ |
| DebugLog | Enabled | Bool | true | | ✖ (reload) |
| Cache | Enabled | Bool | true | | ✖ (reload) |
| Serilog | MinimumLevel.Default | Enum | Information | Verbose/Debug/Information/Warning/Error/Fatal | ✖ |

> `RequiresRestart` = true khi option bind qua `IOptions<T>` (chụp lúc khởi động). Muốn hot-reload → refactor consumer sang `IOptionsMonitor<T>` (ghi chú §8).

---

## 4. Lưu trữ Hybrid

### 4.1. Lớp FILE (giữ nguyên)
`appsettings.local.json` trên server (xem [LocalConfigLoader](../../src/backend/src/ICare247.Api/LocalConfigLoader.cs)) — nạp với `reloadOnChange: true`. Chứa **bootstrap + secret** (connection string, JWT). KHÔNG đụng qua UI.

### 4.2. Lớp DB (mới) — `Sys_Config`
Thông số **vận hành** lưu ở bảng `Sys_Config` tại **DB hệ thống/Catalog** (cấp deployment, KHÔNG per-tenant):

```sql
CREATE TABLE dbo.Sys_Config (
    Config_Key   NVARCHAR(200) NOT NULL,   -- 'FileStorage:Provider' (dấu ':' như IConfiguration)
    Config_Value NVARCHAR(MAX) NULL,       -- giá trị (list = JSON array)
    Updated_By   BIGINT NULL,
    Updated_At   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Sys_Config PRIMARY KEY (Config_Key)
);
```

### 4.3. `SysConfigConfigurationProvider`
Một `IConfigurationSource/Provider` tùy biến, thêm vào pipeline **SAU file** (ưu tiên cao hơn) — đọc `Sys_Config` → nạp vào `IConfiguration`. Nhờ đó `services.Configure<FileStorageOptions>` **không đổi**, giá trị tự đến từ DB.

- **Chicken-egg:** provider cần connection string tới DB hệ thống → lấy từ **file** (đã nạp trước). Không vòng lặp.
- **Fallback:** chưa cấu hình Catalog/DB hệ thống (dev/1-tenant) → provider bỏ qua, dùng file. Tính năng vẫn chạy ở chế độ file-only (P1).
- **Đa-node + hot-reload:** provider refresh định kỳ (VD 30–60s) hoặc khi Save phát tín hiệu (Redis pub/sub nếu có) → mọi node cùng nhận giá trị mới.

---

## 5. API (RBAC admin-only)

`AdminSettingsController` — route `api/v1/admin/settings`, chặn bằng policy admin (§9).

| Method | Route | Mô tả |
|---|---|---|
| GET | `/schema` | Trả catalog (sections + descriptors) — nguồn gợi ý cho UI. |
| GET | `/values` | Giá trị hiệu lực hiện tại (đọc từ `IConfiguration`); secret → `{ isSet: true/false }`, KHÔNG plaintext. |
| PUT | `/{section}` | Ghi 1 section: validate theo schema → ghi DB (`Sys_Config`) hoặc file (chế độ file-only) + backup. Trả field lỗi nếu có. |

**Service:** `ISystemSettingsService` (Application) — `GetSchema()`, `GetValues()`, `SaveSectionAsync(section, values, userId)`.

**File writer (chế độ file-only / P1):** đọc `appsettings.local.json` bằng `JsonNode` → merge chỉ key thay đổi (GIỮ nguyên key lạ + `_readme`) → **backup** `.bak` → ghi lại (indented). Không phá cấu trúc/comment người dùng thêm.

---

## 6. Frontend (Blazor web admin)

- Trang `SettingsAdminPage.razor` tại `/m/administration/settings`.
- `SettingsAdminApiService`: gọi 3 endpoint trên.
- **Render động từ schema** — map Kind → control:

| Kind | Control |
|---|---|
| Bool | DxCheckBox (switch) |
| Int/Long | DxSpinEdit (+ range) |
| Enum | DxComboBox (Options) |
| Text/Path | DxTextBox |
| List | DxMemo (mỗi dòng 1 mục) hoặc tag editor |
| Secret | chỉ badge *"Đã đặt / Chưa đặt"* (read-only) |

- UI: **tab theo section**, mỗi field kèm **Hint** (gợi ý), **badge "Cần khởi động lại"** nếu `RequiresRestart`, nút **Lưu theo section** (validate rồi PUT).
- Sau khi lưu field `RequiresRestart` → hiện thông báo *"Cần khởi động lại API để áp dụng"*.

---

## 7. Bảo mật

- Endpoint + trang **chỉ admin** (§9). 
- Secret **không bao giờ** trả plaintext ra client (chỉ `isSet`), **không** sửa qua UI (v1).
- Ghi file/DB: kiểm giá trị theo schema (kiểu, range, regex, options) trước khi ghi — chống giá trị rác làm hỏng bind.
- Ghi log audit (ai đổi key gì, khi nào) — dùng hạ tầng audit hiện có.

---

## 8. Áp dụng thay đổi (reload vs restart)

- **Hot-reload được:** option consume qua `IOptionsMonitor<T>` + nguồn config reload (file `reloadOnChange` / DB provider refresh). VD `Cache:Enabled`, `DebugLog`.
- **Cần restart:** option consume qua `IOptions<T>` (snapshot lúc start). Hiện `FileStorageOptions` dùng `IOptions` → **đổi FileStorage cần restart**, TRỪ khi refactor sang `IOptionsMonitor`.
- Schema đánh dấu `RequiresRestart` để UI cảnh báo. (Tùy chọn P sau: nút "Khởi động lại API".)

---

## 9. Phụ thuộc RBAC / Menu (cần chốt khi code)

- Cần **1 quyền admin** cho trang + endpoint (theo ADR-023: `Sys_Permission` + policy JWT + mục menu `HT_ChucNang`).
- Vị trí menu: nhóm `administration` (cạnh `/m/administration/permissions`, `/m/administration/menu`).

---

## 10. Kế hoạch phase

| Phase | Nội dung | Kết quả |
|---|---|---|
| **P1 — File + UI** | Schema descriptor + `ISystemSettingsService` (file writer merge/backup) + `AdminSettingsController` + `SettingsAdminPage` + RBAC | Quản lý thông số vận hành qua web (1 node) |
| **P2 — DB overlay** | `Sys_Config` (DB hệ thống) + `SysConfigConfigurationProvider` (đè file + refresh) | Chia sẻ đa-node + hot-reload |
| **P3 — Secret read-only + audit** | Badge trạng thái secret, audit log đổi config, (tùy chọn) nút restart | An toàn, truy vết |

---

## 11. Điểm cần chốt trước khi code

1. **DB hệ thống cho `Sys_Config`**: đặt ở Catalog DB (master) hay 1 DB hệ thống riêng? (ảnh hưởng P2)
2. **Quyền RBAC**: tạo permission mới tên gì; ai được cấp mặc định.
3. **Danh sách section/key chính xác** cho v1 (bản §1 là đề xuất — cần bạn duyệt từng key).
4. **Cơ chế restart**: chỉ hiện cảnh báo, hay làm nút "Khởi động lại API" (cần cơ chế graceful restart per-node).
5. **Refactor `IOptionsMonitor`** cho FileStorage để hot-reload — làm ở P2 hay để cần-restart.
