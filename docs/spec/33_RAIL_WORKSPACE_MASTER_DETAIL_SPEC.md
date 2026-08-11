# 33 — Rail Workspace (Master–Detail Hồ Sơ) — Capability + Hướng Dẫn Cấu Hình

> Ngày lập: 2026-08-11 · Trạng thái: **Pha 1+2+3 đã code** (chưa build/chạy nghiệm thu).
> Xuất phát: NS-MASTERDETAIL (hồ sơ nhân viên `NS_NhanVien`) — chốt bố cục **RAIL WORKSPACE** ngày
> 2026-08-10 (không tab/inline). Đây là **năng lực nền tảng Form Engine**: mọi màn "1 bản ghi chủ +
> n bảng con dạng hồ sơ" (khách hàng/nhà cung cấp/tài sản…) đều dùng lại, `NS_NhanVien` là ca đầu.
>
> Quan hệ với Spec 30: dùng CHUNG config `Ui_Form_Detail` + renderer, nhưng KHÁC layout & save:
> - **Spec 30 (chứng từ):** `Detail_Layout='Inline'`, lưới trong thân form, `Save_Mode='WithMaster'`
>   (master + n dòng lưu **1 transaction**).
> - **Spec 33 (hồ sơ, tài liệu này):** `Detail_Layout='Rail'`, rail điều hướng con, `Save_Mode='Immediate'`
>   (mỗi dòng con lưu ngay qua MasterData CRUD, không gộp transaction với master).

---

## 1. Bức tranh tổng thể

```
┌──────────────────────────────────────────────────────────────────────┐
│  ← Danh sách   NGUYỄN VĂN A                         (header ĐỊNH DANH  │  ← dính (sticky)
│                NS_NhanVien · #42                     — cuộn vẫn thấy)   │
├───────────────┬──────────────────────────────────────────────────────┤
│  👥 Thông tin  │                                                       │
│     chung      │   [Pane phải: đổi nội dung theo mục rail đang chọn]   │
│  ─ RELATED ─   │                                                       │
│  🏢 Địa chỉ    │   VD chọn "Học vấn":                                   │
│  ▤ Học vấn ◀── │   ┌───────────────────────────────── + Thêm mới ──┐  │
│  🌐 Ngoại ngữ  │   │  Trình độ   Trường      Năm TN   Xếp loại  ⋯   │  │
│  📦 Chứng chỉ  │   │  Đại học    BK HN       2015     Giỏi    ✎ 🗑 │  │
│  👥 Thân nhân  │   │  ...                                           │  │
│  💳 Giấy tờ NN │   └────────────────────────────────────────────────┘  │
└───────────────┴──────────────────────────────────────────────────────┘
     RAIL (trái)                    PANE (phải)
```

- **"Thông tin chung"** = các field vô hướng của form master (tái dùng `MasterDataForm` chế độ Sửa).
  Là mục **tự động**, KHÔNG cần tạo pane.
- Mỗi **quan hệ 1-N** (bảng con) = **1 pane Grid** khai trong `Ui_Form_Detail` → 1 mục trên rail.
- Chọn 1 dòng con để **Sửa/Xóa**, bấm **+ Thêm mới** để thêm — mọi thao tác lưu **ngay** (per-row),
  cột khóa cha (`NhanVien_Id`) tự gán = Id bản ghi master, không cho sửa.

### Luồng runtime

```
Ui_Form.Detail_Layout='Rail'  +  Ui_Form_Detail (n pane, Config DB)
        │
        │  GET /api/v1/forms/{code}/details?lang=vi   (đọc PHÒNG THỦ, tenant chưa migrate → Inline rỗng)
        ▼
   FormDetailLayoutDto { layout, panes[] }   ── RuntimeApiService.GetDetailLayoutAsync
        │
        ▼
   MasterDataTabPage  (/master/{code}/edit/{id})
        │   IsRail && đang Sửa  →  RailWorkspace  (full-width)
        │   ngược lại           →  MasterDataForm (form phẳng như cũ)
        ▼
   RailWorkspace  →  pane Grid  →  DetailGridPane
        │                              │  GET /api/v1/master-data/{childForm}
        │                              │      ?parentKey=NhanVien_Id&parentValue=42   ← lọc phía SERVER
        │                              ▼
        │                          MasterDataGrid + MasterDataForm (popup) + ConfirmDeleteDialog
        ▼
   pane "Thông tin chung"  →  MasterDataForm (edit-mode, field vô hướng master)
```

---

## 2. Config model (Config DB)

Cấu hình nằm ở **2 nơi**, đều chỉnh qua ConfigStudio WPF (quy tắc "config qua WPF, không SQL"):

### 2.1 `Ui_Form.Detail_Layout` — bố cục chi tiết của form master

| Giá trị | Ý nghĩa |
|---|---|
| `Inline` (mặc định) | Lưới chi tiết chèn trong thân form (chứng từ — Spec 30). Tương thích ngược. |
| `Rail` | **Rail workspace** — form vô hướng + rail điều hướng con (hồ sơ — Spec 33). |

### 2.2 `Ui_Form_Detail` — mỗi dòng = 1 pane trên rail (bảng `db/106`)

| Cột | Bắt buộc | Dùng ở Rail (Pha 2)? | Ghi chú |
|---|---|---|---|
| `Form_Id` | ✅ | ✅ | Form MASTER (chọn qua "Form master" trên màn WPF). |
| `Detail_Code` | ✅ | ✅ | Mã pane, **unique trong 1 form**. Cũng là key rail + nhãn dự phòng. VD `HocVan`. |
| `Pane_Type` | ✅ | ✅ | `Grid` = lưới CRUD bảng con · `Timeline` = dòng thời gian (**hoãn phase sau** — Pha 2 chỉ hiện placeholder). |
| `Detail_Form_Id` | ✅ (khi Grid) | ✅ | Form CON định nghĩa cột lưới + CRUD (vd `NS_NhanVien_HocVan`). |
| `Parent_Key_Column` | ✅ (khi Grid) | ✅ | Cột FK trên bảng con trỏ về master (vd `NhanVien_Id`). Dùng để **lọc + gán** khi CRUD. |
| `Save_Mode` | — | ⚠️ | Với Rail đặt **`Immediate`** (mỗi dòng lưu ngay). Runtime Rail luôn CRUD per-row bất kể giá trị. |
| `Title_Key` | — | ✅ | Key i18n nhãn pane. **Tự sinh** `{formcode}.detail.{detailcode}.title` khi lưu (WPF, không nhập tay); người dùng gõ **nhãn tiếng Việt** thẳng + 🌐 dịch. Backend resolve → `Title`; trống → `Detail_Code`. |
| `Icon` | — | ✅ | **Tên icon Feather đã đăng ký** (xem §6). Tên lạ/emoji → hiện chấm tròn. |
| `Group_Key` | — | ✅ | **Mã** gom pane cùng nhóm (VD `RELATED`). Nhãn hiển thị = ô "Nhãn nhóm (tiếng Việt)" + 🌐, key tự sinh `{form}.railgroup.{key}.title`, resolve backend → `GroupTitle`. |
| `Edit_Mode` | — | ❌ (chưa) | `EntryPanel\|CellInline\|RowPopup`. Pha 2 **luôn dùng popup** (DraggableModal); trường này để dành. |
| `Allow_Add` | — | ✅ | Ẩn/hiện nút **+ Thêm mới** (còn phải có quyền Thêm trên form con). |
| `Allow_Delete` | — | ✅ | Ẩn/hiện nút **🗑 Xóa** (còn phải có quyền Xóa trên form con). |
| `Allow_Reorder` | — | ❌ (chưa) | Kéo sắp thứ tự dòng — để dành. |
| `Min_Rows` | — | ❌ (chưa) | Validate tối thiểu (dành cho chứng từ). |
| `Summary_Json` | — | ❌ (chưa) | Footer tổng (dành cho chứng từ). |
| `Options_Json` | — | ❌ (chưa) | Map cột cho Timeline (phase sau). |
| `Order_No` | — | ✅ | Thứ tự pane trên rail (tăng dần). |
| `Is_Active` | — | ✅ | Bỏ tick = ẩn pane (soft-delete). |

> ✅ = runtime Pha 2 đọc & dùng · ❌ (chưa) = có cột nhưng runtime Rail chưa tiêu thụ (để dành cho
> Inline/Timeline/phase sau) · ⚠️ = có ảnh hưởng nhưng lưu ý đặc thù.

---

## 3. Hướng dẫn cấu hình (dành cho người vận hành)

Thao tác từng bước trên ConfigStudio (menu **Forms › Master-Detail / Rail**), tham chiếu từng ô Editor,
danh sách icon, nhãn i18n và **bảng lỗi thường gặp**: xem tài liệu vận hành riêng
👉 [../huong-dan-wpf/cau-hinh-master-detail-rail.md](../huong-dan-wpf/cau-hinh-master-detail-rail.md).

Tóm tắt: **Bước 1** chọn form master → Bố cục `Rail` → 💾 Lưu bố cục (ghi `Ui_Form.Detail_Layout`).
**Bước 2** với mỗi bảng con: ＋ Tạo pane → điền `Detail_Code`/`Pane_Type=Grid`/`Detail_Form`/
`Parent_Key_Column`/`Save_Mode=Immediate`/`Icon`/`Group_Key` → 💾 Lưu pane.

---

## 4. Ví dụ hoàn chỉnh — `NS_NhanVien` (6 pane)

Cần **màn list** (Ui_View grid + menu) có `Edit_Form = NS_NhanVien` để vào bản ghi (§5). Sáu form con
(`db/105`) đều có khóa cha `NhanVien_Id`:

| Order_No | Detail_Code | Pane_Type | Detail_Form | Parent_Key_Column | Save_Mode | Icon | Group_Key |
|---|---|---|---|---|---|---|---|
| 1 | `DiaChi` | Grid | `NS_NhanVien_DiaChi` | `NhanVien_Id` | Immediate | `building` | RELATED |
| 2 | `HocVan` | Grid | `NS_NhanVien_HocVan` | `NhanVien_Id` | Immediate | `list` | RELATED |
| 3 | `NgoaiNgu` | Grid | `NS_NhanVien_NgoaiNgu` | `NhanVien_Id` | Immediate | `languages` | RELATED |
| 4 | `ChungChi` | Grid | `NS_NhanVien_ChungChi` | `NhanVien_Id` | Immediate | `package` | RELATED |
| 5 | `ThanNhan` | Grid | `NS_NhanVien_ThanNhan` | `NhanVien_Id` | Immediate | `users` | RELATED |
| 6 | `GiayToNuocNgoai` | Grid | `NS_NhanVien_GiayToNuocNgoai` | `NhanVien_Id` | Immediate | `credit-card` | RELATED |

Sau khi lưu 6 pane + có màn list: mở Web → màn list nhân viên → **Sửa 1 nhân viên** → rail hiện 7 mục
("Thông tin chung" + 6 pane).

---

## 5. Điều kiện & giới hạn

### 5.1 Điểm vào (entry point) — reachability

Rail sống ở trang routed `/master/{FormCode}/edit/{id}` (`MasterDataTabPage`, branch khi `IsRail` + có Id).
Vào trang đó qua **1 trong 2** đường:

| Đường | Cách | Ghi chú |
|---|---|---|
| **View grid** (khuyến nghị) | List = `Ui_View` grid (`/view/{code}`) có `Edit_Form` = form master Rail. `ViewPage.OpenEdit` đọc `GetDetailLayoutAsync(EditFormCode)`; `IsRail` → `Nav` sang trang rail (kèm `returnUrl` về đúng View); ngược lại popup như cũ. | `Display_Mode` form master **không cần** = Tab. Áp mọi module. |
| **MasterData list** | `/master/{FormCode}` (`MasterDataListPage`) → OpenForm điều hướng edit khi `Display_Mode='Tab'`. | Cần `Display_Mode='Tab'`. Màn generic, thiếu cột/lọc Ui_View. |

> Popup (DraggableModal) không đủ chỗ cho rail — nên cả 2 đường đều mở **trang** rail, không popup.

### 5.2 Điều kiện & giới hạn

| Điều kiện | Lý do |
|---|---|
| **Có màn list trỏ Edit_Form = form master Rail** | Không có màn list = không vào được bản ghi → không thấy rail (§5.1). |
| **Chỉ ở chế độ SỬA** | Lưới con lọc theo `NhanVien_Id` = Id bản ghi master; Thêm mới chưa có Id → render form phẳng. Lưu master xong, mở Sửa mới thấy rail. |
| **Có ≥1 pane Active** | `Detail_Layout='Rail'` nhưng 0 pane → coi như thường (form phẳng). `IsRail = Layout=='Rail' && Panes.Count>0`. |
| **Tenant đã chạy `db/106`** | Chưa migrate → endpoint `/details` trả `Inline` rỗng (đọc phòng thủ theo `OBJECT_ID`) → form phẳng, KHÔNG lỗi. |
| **Pane Timeline = placeholder** | Pha 2 chỉ hiện "Dòng thời gian sẽ có ở phiên bản sau". Cần view biến động (`vw_NhanVien_HienTai`/TVF) ở phase sau. |

---

## 6. Icon hợp lệ (bộ Feather đã đăng ký)

Ô **Icon** nhận **tên** icon (không phải emoji); component `<Icon>` tra theo tên, tên lạ/emoji → vẽ
**chấm tròn** (an toàn). Danh sách tên cho người vận hành: xem
[../huong-dan-wpf/cau-hinh-master-detail-rail.md](../huong-dan-wpf/cau-hinh-master-detail-rail.md#icon-hợp-lệ).

> Thêm icon mới (dev): dán `<path>` từ lucide.dev thành 1 case trong
> `src/frontend/ICare247.UI.Shared/Components/Icon.razor` + thêm tên vào mảng `RegisteredNames`.

---

## 7. i18n — chọn đúng hệ (QUAN TRỌNG)

Tuân thủ 2 hệ i18n của dự án (xem `docs/HUONG_DAN_I18N.md`):

- **Nhãn cấu-hình-được = Hệ 1 (metadata-driven, `Sys_Resource`), resolve phía BACKEND.**
  - **Nhãn pane:** `GetDetailLayoutAsync` đã `LEFT JOIN Sys_Resource ON Resource_Key = Title_Key
    AND Lang_Code`, trả `COALESCE(Resource_Value, Detail_Code) AS Title`. FE chỉ hiển thị `pane.Title`.
    **Nhập theo khuôn editor field:** `Title_Key` **tự sinh** `{formcode}.detail.{detailcode}.title`
    (`FormMasterDetailManagerViewModel.BuildDetailTitleKey`, không gõ key tay); người dùng gõ nhãn vi
    thẳng vào ô "Tiêu đề pane" + nút 🌐 (`ViewNames.I18nEditorDialog`) cho ngôn ngữ khác; lưu vào
    `Sys_Resource` qua `II18nDataService`. KHÔNG vào `{lang}.json` client.
  - **Nhãn nhóm:** `Group_Key` là **mã** gom nhóm; nhãn hiển thị resolve phía backend qua Sys_Resource với
    key TỰ SINH `{form_code}.railgroup.{group_key}.title` (SQL ghép `LOWER(Form_Code)+'.railgroup.'+
    LOWER(Group_Key)+'.title'`), trả `COALESCE(rg.Resource_Value, d.Group_Key) AS GroupTitle`. FE cụm theo
    `GroupKey`, hiển thị `GroupTitle`. WPF ghi resource dưới cùng key đó (`BuildGroupTitleKey`). ⚠️ TUYỆT ĐỐI
    **KHÔNG** dựng `Loc.L($"rail.group.{Group_Key}")` ở FE (key i18n động từ dữ liệu — đã gỡ).
- **Chuỗi giao diện CỐ ĐỊNH của rail = Hệ 2 (hand-coded `Loc.L(key, base-vi)`, key TĨNH).** Đã dùng đúng:
  `rail.pane.general`, `rail.pane.timelineSoon`, `rail.pane.missing`, `rail.pane.gridUnconfigured`,
  `rail.nav.aria` — scanner tự bắt, không hand-add JSON.

---

## 8. Cơ chế phía sau (tham khảo)

### 8.1 Lọc lưới con phía server — an toàn injection & cô lập cha

Endpoint danh sách master-data nhận thêm `parentKey`/`parentValue`:

```
GET /api/v1/master-data/{childForm}?parentKey=NhanVien_Id&parentValue=42
```

- `parentKey` được **whitelist** theo `Sys_Column` của bảng con (`HasColumn` + `SqlIdentifier.IsSafe`)
  → không thể bơm identifier lạ.
- Cột cha **không tồn tại** trên bảng → `WHERE 1=0` (trả rỗng), **KHÔNG** trả toàn bộ: cấu hình sai
  không được rò dòng của cha khác.
- Tenant cô lập ở tầng connection (Data DB per-tenant, ADR-035).

### 8.2 Thêm/Sửa dòng con

- Popup dùng chung `MasterDataForm`: khi **Thêm mới**, cột `Parent_Key_Column` được **prefill =
  Id master** và **khóa read-only** (ADR-030 `InitialValues` + `LockedFields`).
- Lưu = tái dùng thẳng `POST/PUT /api/v1/master-data/{childForm}` (per-row, Immediate). Dirty-guard
  đóng modal + toast dùng lại hạ tầng sẵn.
- Quyền: kiểm theo **form con** (`Form` target = `childForm`), giao với `Allow_Add`/`Allow_Delete`.

---

## 9. Xử lý sự cố (troubleshooting)

Bảng triệu chứng ↔ nguyên nhân cho người vận hành: xem
[../huong-dan-wpf/cau-hinh-master-detail-rail.md](../huong-dan-wpf/cau-hinh-master-detail-rail.md#bảng-lỗi-thường-gặp-chống-cấu-hình-sai).

Nguyên tắc chẩn (kỹ thuật): rail hiện khi `Detail_Layout='Rail'` **và** `Panes.Count>0` **và** đang Sửa
(có master Id) **và** `/details` trả pane (tenant đã migrate `db/106`). Lưới con rỗng dù có dữ liệu ⇒
`Parent_Key_Column` không phải cột thật của bảng con ⇒ whitelist rớt ⇒ `WHERE 1=0` (§8.1).

---

## 10. Bản đồ mã nguồn (files)

**Config DB**
- `db/106_create_ui_form_detail.sql` — bảng `Ui_Form_Detail` + cột `Ui_Form.Detail_Layout`.
- `db/105_seed_ui_form_ns_nhanvien.sql` — form `NS_NhanVien` + 6 form con.

**Backend**
- `ICare247.Domain/Entities/Form/FormDetailLayout.cs`, `FormDetailPane.cs`.
- `ICare247.Application/Interfaces/IFormRepository.cs` → `GetDetailLayoutAsync`.
- `ICare247.Infrastructure/Repositories/FormRepository.cs` — đọc phòng thủ (guard `OBJECT_ID`).
- `ICare247.Api/Controllers/RuntimeController.cs` → `GET /forms/{code}/details`.
- Lọc lưới con: `Features/MasterData/Queries/GetMasterDataList/*` + `IMasterDataRepository` +
  `MasterDataRepository.GetListAsync` + `MasterDataController.GetList` (`parentKey`/`parentValue`).
- ConfigSync descriptor: `Infrastructure/ConfigSync/ConfigSyncTables.cs`.

**Frontend (Blazor — Pha 2)**
- `ICare247_UI/Models/FormDetailLayoutDto.cs`.
- `ICare247_UI/Services/RuntimeApiService.cs` → `GetDetailLayoutAsync`.
- `ICare247_UI/Services/MasterDataApiService.cs` → `GetListAsync` (+parentKey/parentValue).
- `ICare247_UI/Components/MasterData/RailWorkspace.razor` — header + rail + pane host.
- `ICare247_UI/Components/MasterData/DetailGridPane.razor` — lưới con CRUD per-row.
- `ICare247_UI/Pages/MasterData/MasterDataTabPage.razor` — nhánh Rail/phẳng + `returnUrl` (back về View gốc).
- `ICare247_UI/Pages/View/ViewPage.razor` — `OpenEdit`: Edit_Form là Rail → `Nav` sang trang rail (kèm
  `returnUrl`) thay vì popup (đọc `GetDetailLayoutAsync` khi nạp view → `_editIsRail`).
- `ICare247_UI/wwwroot/css/app.css` — khối `.rail-ws*`, `.rail-nav`, `.rail-item`, `.rail-pane*`.

**ConfigStudio WPF (Pha 3)**
- `ConfigStudio.WPF.UI.Modules.Forms/Views/FormMasterDetailManagerView.xaml` (+ `.cs`, `ViewModel`).
- `ConfigStudio.WPF.UI/Infrastructure/FormMasterDetailDataService.cs` (Dapper Config DB, guard `db/106`).

---

## 11. Chưa làm (roadmap)

- **Pane Timeline** — "Quá trình công tác" đọc từ biến động (`NS_BienDongNhanSu`), cần view/TVF
  `vw_NhanVien_HienTai` + map cột qua `Options_Json`.
- **Thanh % hoàn thiện hồ sơ** + **command palette ⌘K** (đã duyệt ý tưởng, chưa code).
- **Edit_Mode** (CellInline/RowPopup), **Allow_Reorder**, **Min_Rows/Summary_Json** cho Rail.
- Địa chỉ composite ('address') như 1 pane đặc thù.
