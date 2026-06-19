# Debug: Views (lưới hiển thị danh sách — Grid/TreeList)

> Cấu hình hiển thị danh sách (Ui_View/Ui_View_Column/Ui_View_Action) tách khỏi form sửa.
> Metadata ở **Config DB**, dữ liệu lấy từ **Live DB**. Bối cảnh: [README.md](README.md).

## 1. API (route gốc `api/v1/views`)

| Method | URL | Mục đích | Command/Query |
|---|---|---|---|
| GET | `/` | Danh sách view (search + paging) | `GetViewsListQuery` |
| GET | `/{code}/info` | Metadata view (cột, action, render) | `GetViewByCodeQuery` |
| GET | `/{code}/data` | Dữ liệu lưới (Source=Table) | `GetViewDataQuery` |
| POST | `/{code}/search` | Lưới nâng cao (Source=Sp/Sql) + bộ lọc | `GetViewFilteredDataQuery` |
| POST | `/{code}/invalidate-cache` | Xóa cache view | `IConfigCache.InvalidateViewAsync` |
| GET | `/{code}/my-layout` | Layout lưới đã lưu của user (per-user) | `IUserGridLayoutStore.GetAsync` |
| PUT | `/{code}/my-layout` | Lưu layout lưới của user (UPSERT, write-through) | `IUserGridLayoutStore.SaveAsync` |
| DELETE | `/{code}/my-layout` | Khôi phục mặc định (xóa layout user) | `IUserGridLayoutStore.ResetAsync` |

Header bắt buộc: `X-Tenant-Id: 1`. 3 endpoint `my-layout` cần `[Authorize]` (NguoiDung_Id lấy từ JWT `sub`).

> **Layout lưới per-user (2026-06-19):** sở thích người dùng (rộng/thứ tự/sort/filter/paging) lưu ở **Data DB**
> `HT_NguoiDung_LuoiLayout` (KHÔNG phải `Ui_View` config). Đường đọc: FE **L0 localStorage** → server (cache-aside
> L1/L2 `CacheKeys.UserGridLayout`, key-space riêng, **không version-stamp** config) → DB. Single-writer → write-through.
> FE nối qua `DxGrid.LayoutAutoLoading/Saving` (`GridLayoutService` + `DataView`). Lazy theo View; mở lại = 0 query DB.
>
> **Runtime ViewPage (2026-06-19):** Thêm/Sửa mở **popup ngay trên màn lưới** (`DraggableModal`, kéo tiêu đề, chỉ đóng
> bằng nút), đóng/Lưu về đúng màn — KHÔNG còn điều hướng sang `/master`. Lưới chuẩn hóa: cột **chọn (checkbox) + STT +
> Thao tác (Sửa/Xóa)** + **xóa hàng loạt**. Nút "Xóa cache" chỉ hiện cho **super-admin** (role `SUPERADMIN`).

## 2. Payload

- **GET {code}/info**: `?lang=vi`
- **GET {code}/data**: `?lang=vi&search=abc&page=1&pageSize=50`
- **POST {code}/search** (lưới nâng cao SP/SQL) — key = **Filter_Code**:
  ```json
  { "filters": { "TuNgay": "2026-01-01", "DenNgay": "2026-06-30", "PhongBan": "3" } }
  ```
  - Thiếu/sai tham số bắt buộc → **400** `{ message }` (từ `ArgumentException`).
  - View không tồn tại/ẩn → **404** `{ message }`.

## 3. Code ở lớp nào

| Lớp | File |
|---|---|
| Api | `Controllers/ViewController.cs` |
| Application | `Features/Views/Queries/{GetViewsList,GetViewByCode,GetViewData,GetViewFilteredData}` |
| Application (cache) | `Engines/ConfigCache.cs` → `GetViewAsync` (cache-aside L1+L2) |
| Infrastructure | `Repositories/ViewRepository.cs` — **Config DB** (metadata view) + **Live DB** (dữ liệu, SP/SQL) |

## 4. Luồng

```
# Metadata
ViewController.GetInfo → GetViewByCodeQuery → handler
  └─ IConfigCache.GetViewAsync(code, lang, tenant)  ← cache-aside → ViewRepository (Config DB)

# Dữ liệu lưới thường (Source=Table)
ViewController.GetData → GetViewDataQuery → handler
  ├─ nạp metadata view (cache) → biết bảng nguồn + cột (Field_Name whitelist)
  └─ ViewRepository.GetDataAsync ──► SELECT cột từ bảng nguồn (Live DB) + search + paging

# Lưới nâng cao (Source=Sp/Sql) — panel lọc trái
ViewController.Search → GetViewFilteredDataQuery → handler
  ├─ đọc cấu hình filter (Ui_View_Filter): tham số bắt buộc, kiểu, default
  ├─ bind WHITELIST tham số từ body.filters (Filter_Code) — chặn SQL injection
  │     • thiếu/sai bắt buộc → ArgumentException → 400
  └─ gọi SP / SQL (Live DB) → trả rows
```

## 5. Breakpoint
1. `ViewController.GetInfo/GetData/Search` — `code`, `GetTenantId()`, `filters`.
2. `GetViewByCodeQueryHandler` / `ConfigCache.GetViewAsync` — cache hit/miss.
3. `ViewRepository.GetDataAsync` / `GetFilteredDataAsync` — SQL/SP + tham số bind, connection (Live DB).
4. Nhánh `catch (ArgumentException)` trong `Search` — tham số nào thiếu.

## 6. Lỗi thường gặp
- **Cột mất khi render** → property DevExpress không tồn tại (vd `FilterRowCellVisible` ở DX 25.2.3 →
  dùng `FilterRowEditorVisible`); hoặc `Field_Name` không khớp cột data.
- **400 ở /search** → thiếu tham số `Filter_Code` bắt buộc; xem `message`.
- **Đổi view ở ConfigStudio không thấy** → gọi `POST /{code}/invalidate-cache`.
- **Source=Sp/Sql trả rỗng** → SP/tham số sai; bật log Dapper, chạy SP thủ công trên Live DB.
- **NotSupported** → `Source_Type` chưa hỗ trợ ở handler (vd export server-side pdf/docx — hoãn).
