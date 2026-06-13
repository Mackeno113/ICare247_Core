# Debug: Master Data (CRUD danh mục generic — Live DB)

> CRUD dữ liệu danh mục **sinh động từ metadata** (Ui_Form/Ui_Field/Sys_Table). Bảng đích đọc ở
> server theo `formCode` — client KHÔNG gửi tên bảng. Dữ liệu nằm ở **Live DB**. Bối cảnh: [README.md](README.md).

## 1. API (route gốc `api/v1/master-data`)

| Method | URL | Mục đích | Command/Query |
|---|---|---|---|
| GET | `/{formCode}/info` | Metadata bảng + cột (render lưới/form) | `GetMasterDataFormInfoQuery` |
| GET | `/{formCode}` | Danh sách (search + activeOnly + paging) | `GetMasterDataListQuery` |
| GET | `/{formCode}/{id}` | 1 bản ghi theo PK (form Sửa) | `GetMasterDataRecordQuery` |
| GET | `/{formCode}/{id}/usage` | Soft-check đang bị tham chiếu? | `CheckMasterDataUsageQuery` |
| POST | `/{formCode}` | Thêm mới (validate → **422** nếu lỗi) | `SaveMasterDataCommand` (Id=null) |
| PUT | `/{formCode}/{id}` | Cập nhật | `SaveMasterDataCommand` |
| DELETE | `/{formCode}/{id}` | Xóa cứng (**409** nếu bị tham chiếu) | `DeleteMasterDataCommand` |

Header bắt buộc: `X-Tenant-Id: 1`.

## 2. Payload

- **GET list**: `?search=abc&activeOnly=true&page=1&pageSize=50`
- **POST/PUT** body — key = **Field_Code** (= tên cột DB):
  ```json
  { "values": { "Ma": "NV001", "HoTen": "Nguyễn Văn A", "PhongBan_Id": 3 } }
  ```
  - Thành công → 200 `{ success: true, id, ... }`.
  - Validation fail → **422** `{ success: false, errors: { fieldCode: [msg] } }`.
- **DELETE** bị chặn → **409** ProblemDetails, `extensions.blockedBy` liệt kê nơi đang dùng.

## 3. Code ở lớp nào

| Lớp | File |
|---|---|
| Api | `Controllers/MasterDataController.cs` (có `NormalizeValues` unwrap JsonElement → CLR) |
| Application | `Features/MasterData/Queries/*`, `Features/MasterData/Commands/{SaveMasterData,DeleteMasterData}` |
| Application | `Features/MasterData/Models/MasterDataResults.cs` (kết quả Save/Delete) |
| Application (validate) | `Engines/ValidationEngine.cs` (Save chạy validate trước khi insert) |
| Infrastructure | `Repositories/MasterDataRepository.cs` — **Config DB** (metadata) + **Live DB** (dữ liệu) |
| Infrastructure | `Repositories/ReferenceCheckService.cs` — soft-check tham chiếu (usage/delete) |

> `MasterDataRepository` inject **cả** `IDbConnectionFactory` (Config — đọc metadata) **và**
> `IDataDbConnectionFactory` (Live — đọc/ghi dữ liệu). Đây là điểm hay nhầm khi debug "sai DB".

## 4. Luồng (Save — POST/PUT)

```
MasterDataController.Create/Update
  ├─ NormalizeValues(body.values)   ← unwrap JsonElement (Dapper không bind được JsonElement)
  └─ SaveMasterDataCommand → SaveMasterDataCommandHandler
       ├─ đọc metadata form (Config DB): bảng đích, cột, rule, Is_Unique
       ├─ ValidationEngine.ValidateFormAsync  → lỗi → trả { success:false, errors }  (HTTP 422)
       ├─ check trùng (Is_Unique) → DuplicateValueException → message i18n qua ConfigCache
       └─ MasterDataRepository.Insert/Update  ──► INSERT/UPDATE bảng đích (Live DB)
  → 200 { success:true, id }
```

Luồng **Delete**: `DeleteMasterDataCommand` → `ReferenceCheckService` quét FK soft → nếu có nơi dùng
→ `{ success:false, blockedBy }` → controller trả **409**; nếu trống → xóa → 200.

## 5. Breakpoint
1. `MasterDataController.Create/Update` — sau `NormalizeValues`: `values` đã là kiểu CLR chưa?
2. `SaveMasterDataCommandHandler.Handle` — rẽ nhánh validate / trùng / insert.
3. `MasterDataRepository` — SQL INSERT/UPDATE + **connection nào** (phải Live DB).
4. `ReferenceCheckService` (khi debug 409 xóa) — danh sách `blockedBy`.

## 6. Lỗi thường gặp
- **422 dù dữ liệu đúng** → rule/required từ metadata; xem `errors` field nào, kiểm Ui_Field.
- **Insert sai DB / table not found** → `formCode` map sai `Sys_Table`, hoặc đang ghi vào Config thay vì Live.
- **409 không xóa được** → đúng nghiệp vụ (đang bị tham chiếu); xem `extensions.blockedBy`.
- **Cột bị bỏ khi lưu** → field `Is_Virtual=1` hoặc không nằm trong metadata → không vào payload.
