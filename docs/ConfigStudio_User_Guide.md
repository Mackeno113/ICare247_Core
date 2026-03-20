# ICare247 ConfigStudio — Huong Dan Su Dung

> Phien ban: 1.0 | Cap nhat: 2026-03-20
> Doi tuong: Admin he thong, Business Analyst, IT cau hinh form

---

## Muc luc

1. [Tong quan](#1-tong-quan)
2. [Khoi dong ung dung](#2-khoi-dong-ung-dung)
3. [Giao dien chinh (Shell)](#3-giao-dien-chinh)
4. [Quan ly Form (Form Manager)](#4-quan-ly-form)
5. [Xem chi tiet Form (Form Detail)](#5-xem-chi-tiet-form)
6. [Chinh sua Form (Form Editor)](#6-chinh-sua-form)
7. [Cau hinh Field (Field Config)](#7-cau-hinh-field)
8. [Validation Rule Editor](#8-validation-rule-editor)
9. [Event Editor](#9-event-editor)
10. [Expression Builder](#10-expression-builder)
11. [Dependency Viewer](#11-dependency-viewer)
12. [Grammar Library](#12-grammar-library)
13. [i18n Manager](#13-i18n-manager)
14. [Publish Checklist](#14-publish-checklist)
15. [Cau hinh ung dung (Settings)](#15-cau-hinh-ung-dung)

---

## 1. Tong quan

ConfigStudio la cong cu cau hinh form metadata cho nen tang ICare247. Cho phep:

- **Tao va quan ly form** theo metadata-driven (khong can code)
- **Cau hinh field** voi editor type, validation rules, events
- **Xay dung bieu thuc** (expression) bang giao dien keo-tha
- **Quan ly da ngon ngu** (i18n) cho label, placeholder, error message
- **Kiem tra truoc khi publish** dam bao form hop le

### Luong lam viec chinh

```
Tao Form --> Them Section/Field --> Cau hinh Field --> Dat Rule/Event --> Kiem tra --> Publish
```

---

## 2. Khoi dong ung dung

1. Chay file `ConfigStudio.WPF.UI.exe`
2. Ung dung hien thi giao dien chinh voi sidebar ben trai
3. Thanh trang thai goc tren phai hien thi:
   - **Tenant**: ten doi tuong dang lam viec (vd: DEMO)
   - **User**: tai khoan dang nhap (vd: admin)
   - **Connection**: trang thai ket noi DB
   - **Cache**: trang thai Redis
   - **Version**: phien ban ung dung

---

## 3. Giao dien chinh

### Sidebar (menu trai)

| Menu | Man hinh | Mo ta |
|------|----------|-------|
| Dashboard | Trang chu | Tong quan he thong |
| **Forms** | | Nhom quan ly form |
| - Sys Table | Quan ly bang DB | Xem danh sach business table |
| - Form List | Danh sach form | Xem/tim/loc/them/sua/xoa form |
| - New Form | Tao form moi | Mo Form Editor che do tao moi |
| Validation Rules | Quan ly rule | Xem/sua validation rule |
| Events | Quan ly event | Xem/sua event trigger + action |
| **Grammar** | | Nhom grammar |
| - Functions | Thu vien ham | Danh sach ham cho phep trong expression |
| - Operators | Thu vien toan tu | Danh sach toan tu cho phep |
| i18n Keys | Quan ly da ngon ngu | Quan ly key dich va ban dich |
| Settings | Cau hinh | Cau hinh ket noi, giao dien |

### Thao tac

- **Click menu** de chuyen man hinh
- **Click mui ten** de mo/dong nhom menu con (Forms, Grammar)
- **Click nut thu gon** (goc trai sidebar) de an/hien sidebar
- **Doi giao dien**: chon giua "Light Ocean" va "Slate Professional" trong Settings

---

## 4. Quan ly Form (Form Manager)

> Man hinh chinh de xem va quan ly tat ca form trong he thong.

### Giao dien

- **Thanh tieu de**: "Quan Ly Form" + nut [+ Tao Form Moi] + nut [Lam moi]
- **Thanh loc**:
  - O tim kiem (theo ma hoac ten form)
  - Loc Platform (Tat ca / web / mobile / wpf)
  - Loc Business Table
  - Checkbox "Hien form da an"
- **Bang du lieu**: danh sach form voi cac cot:
  - Ma Form (click de xem chi tiet)
  - Ten Form, Table, Platform (badge mau), Version, Sections, Fields
  - Trang thai (Active / Inactive)
  - Cap nhat (ngay gio)
  - Hanh dong: [Sua thong tin] [Mo Editor] [Xem truoc] [Nhan ban] [Vo hieu hoa/Khoi phuc]
- **Thanh thong ke**: Hien thi / Tong / Active / Inactive

### Thao tac tung buoc

#### Tim kiem va loc form
1. Go tu khoa vao o tim kiem → danh sach loc tu dong
2. Chon Platform tu dropdown de loc theo nen tang
3. Chon Business Table de loc theo bang du lieu
4. Bat "Hien form da an" de thay form Inactive

#### Tao form moi
1. Click **[+ Tao Form Moi]**
2. Chuyen sang man hinh Form Editor che do tao moi

#### Nhan ban form (Clone)
1. Click nut **[Nhan ban]** (bieu tuong ⧉) tren dong form can clone
2. Hop thoai **Nhan ban Form** hien len:
   - Hien ten form nguon
   - Nhap **Form_Code moi** (chi dung chu HOA, so, dau gach duoi)
   - He thong kiem tra trung lap tu dong khi go
   - Thong bao loi do neu Form_Code khong hop le hoac da ton tai
3. Click **[Clone]** de xac nhan → form moi duoc tao voi Version = 1

#### Vo hieu hoa form (Deactivate)
1. Click nut **[Vo hieu hoa]** (bieu tuong khoa) tren dong form
2. Hop thoai **Vo hieu hoa Form** hien len:
   - Hien ten form va ma form
   - Hien so luong anh huong: X section, Y field, Z event
   - Canh bao: form se bi an khoi runtime, du lieu khong bi xoa
3. Click **[Vo hieu hoa]** de xac nhan → form chuyen sang trang thai Inactive

#### Khoi phuc form
1. Bat "Hien form da an" de thay form Inactive
2. Click nut **[Khoi phuc]** (bieu tuong mo khoa) → form chuyen lai Active

---

## 5. Xem chi tiet Form (Form Detail)

> Man hinh xem readonly toan bo thong tin form.

### Cach mo
- Click vao **Ma Form** (link xanh gach chan) trong Form Manager

### Giao dien
- **Header**: Ma Form, Ten, Table, Platform, Layout Engine, Version, Checksum, trang thai
- **5 tab**:
  - **Sections**: danh sach section voi thu tu, ma, tieu de, so field
  - **Fields**: tat ca field voi column, section, editor type, visibility, readonly, rule count
  - **Events**: event voi trigger, field target, dieu kien, so action
  - **Rules**: validation rule voi loai, bieu thuc, error key
  - **Audit Log**: lich su thay doi (thoi gian, hanh dong, nguoi thuc hien)

### Thao tac
- **[← Back]**: quay lai Form Manager
- **[Edit]**: chuyen sang Form Editor de chinh sua
- **[Deactivate/Restore]**: doi trang thai form

---

## 6. Chinh sua Form (Form Editor)

> Man hinh chinh de thiet ke cau truc form.

### Cach mo
- Click **[+ Tao Form Moi]** trong Form Manager (che do tao moi)
- Click **[Sua thong tin]** hoac **[Mo Editor]** tren dong form (che do chinh sua)

### Giao dien — Che do tao moi

- Header: "Tao Form Moi" + nut [← Back]
- Card nhap thong tin:
  - **Form_Code**: chi cho phep A-Z, 0-9, _ (tu dong viet hoa)
  - **Form_Name**: ten hien thi cua form
  - **Business Table**: chon bang du lieu tu danh sach DB
  - **Platform**: web / mobile / wpf
  - **Layout Engine**: Grid / Flex / Tab
  - **Mo ta**: ghi chu tuy chon
- Nut **[Tao Form Moi]** (chi bat khi nhap du va hop le)

### Giao dien — Che do chinh sua

**Cot trai — Cay cau truc:**
- TreeView hien thi cau truc: Section (nut goc) → Field (nut con)
- Thanh cong cu: [+ Section] [+ Field] [Expand All] [Collapse All]
- O tim kiem de loc node

**Cot phai — Tab chi tiet:**
- **Tab Form Info**: metadata form (code, name, platform, table, version...)
- **Tab Sections & Fields**: thuoc tinh node dang chon trong cay
- **Tab Events**: DataGrid event voi [+ Them Event] [Sua] [Xoa]
- **Tab Permissions**: bang quyen theo role (Read / Write / Submit)

**Header actions:**
- [← Back]: quay lai Form Manager
- [Save]: luu thay doi (chi bat khi co thay doi)
- [Publish]: chuyen sang Publish Checklist
- [View Dependencies]: mo Dependency Viewer

### Thao tac tung buoc

#### Them Section
1. Click **[+ Section]** tren thanh cong cu cay
2. Section moi xuat hien cuoi cay voi ten mac dinh
3. Click vao section de doi ten va thuoc tinh o panel ben phai

#### Them Field
1. Chon section trong cay (hoac click [+ Field])
2. Field moi xuat hien duoi section da chon
3. Click vao field → panel ben phai hien thuoc tinh co ban
4. Click **[⚙]** de mo **Field Config** cau hinh chi tiet

#### Di chuyen node
- Chon node → click **[↑ Move Up]** hoac **[↓ Move Down]**
- Hoac keo-tha trong cay (neu ho tro)

#### Them/Sua Event
- Chuyen sang tab **Events**
- Click **[+ Them Event]** hoac click **[⚙]** tren dong event
- Chuyen sang man hinh Event Editor

#### Luu form
- Click **[Save]** (nut chi bat khi co thay doi — dau ● vang tren header bao hieu)
- He thong luu va cap nhat Version++

---

## 7. Cau hinh Field (Field Config)

> Man hinh cau hinh chi tiet 1 field: column, editor type, display, behavior, rules, events.

### Cach mo
- Click nut **[⚙]** tren field trong Form Editor

### Giao dien

**Header**: nut [← Back], tieu de "Cau hinh Field", breadcrumb (Form > Section > Field), dau ● khi chua luu, nut [Luu Field] [Huy]

**4 tab:**

#### Tab 1 — Co ban

3 card section:

**Card "Thong Tin Co Ban":**
- **Column (DB)**: dropdown chon cot tu Sys_Column (hien thi dang "SoLuong (Int32, NOT NULL)")
- **Net Type**: tu dong hien kieu .NET cua column da chon (readonly)
- **Editor Type**: dropdown chon loai component (TextBox, NumericBox, ComboBox, DatePicker, LookupBox, TextArea, CheckBox, ToggleSwitch)
- **Thu tu hien thi**: so thu tu sap xep field trong section

**Card "Display (i18n)":**
- **Label Key**: nhap key (vd: `lbl.soluong`) → preview hien ban dich ben canh
- **Placeholder Key**: nhap key → preview
- **Tooltip Key**: nhap key → preview
- Link **[Manage i18n →]** de chuyen sang I18n Manager

**Card "Behavior":**
- **Visible**: bat/tat hien thi field tren form
- **ReadOnly**: bat/tat chi xem khong sua
- **Required**: bat/tat bat buoc nhap → tu dong tao/xoa Required rule

#### Tab 2 — Control Props

- Hien thi thuoc tinh dong dua tren Editor Type da chon
- Moi Editor Type co bo props rieng, vi du:
  - **NumericBox**: gia tri toi thieu, toi da, so thap phan, buoc nhay, cho phep rong
  - **TextBox**: do dai toi da, nhieu dong, so dong
  - **ComboBox**: API datasource, value field, display field, cho phep rong
  - **DatePicker**: dinh dang ngay, ngay min/max
- **Raw JSON Preview**: xem JSON se luu vao DB (co the mo rong)

#### Tab 3 — Rules

- Hien badge so luong rule
- DataGrid: #, Rule Type, Expression, Error Key, Active
- Nut **[+ Them Rule]** → chuyen sang Validation Rule Editor
- Nut **[⚙]** tren dong → mo rule trong Rule Editor
- Nut **[Xoa]** tren dong → xoa rule (chua luu DB)

#### Tab 4 — Events

- Hien badge so luong event
- DataGrid: ID, Trigger, Condition, Actions count, Active
- Nut **[+ Them Event]** → chuyen sang Event Editor
- Nut **[⚙]** tren dong → mo event trong Event Editor

### Thao tac tung buoc

#### Cau hinh 1 field day du
1. Chon **Column** tu dropdown → Net Type tu dong cap nhat
2. Chon **Editor Type** phu hop (vd: NumericBox cho so)
3. Nhap **Label Key** → kiem tra preview ban dich
4. Bat/tat **Visible**, **ReadOnly**, **Required** theo yeu cau
5. Chuyen sang tab **Control Props** → dieu chinh thuoc tinh rieng cua component
6. Chuyen sang tab **Rules** → them validation rule neu can
7. Chuyen sang tab **Events** → them event neu can
8. Click **[Luu Field]**

---

## 8. Validation Rule Editor

> Man hinh quan ly cac validation rule gan vao 1 field.

### Cach mo
- Click **[+ Them Rule]** hoac **[⚙]** trong tab Rules cua Field Config

### Giao dien
- **Header**: ten field dang cau hinh
- **Loc**: dropdown Rule Type (Tat ca, Required, Numeric, Range, Regex, Custom)
- **DataGrid**: Rule ID, Rule Type, Expression Preview, Error Key, Severity (Error/Warning/Info), Active, Order
- **Thanh cong cu**: [+ Add Rule] [Edit] [Delete] [Move Up] [Move Down] [Open Expression Builder] [Save All] [Back]

### Thao tac tung buoc

#### Them rule moi
1. Click **[+ Add Rule]**
2. Panel chinh sua hien len:
   - Chon **Rule Type** (Required, Numeric, Range, Regex, Custom)
   - Nhap **Error Key** (vd: `err.soluong.range`)
   - Chon **Severity** (Error / Warning / Info)
3. Click **[Open Expression Builder]** de xay dung bieu thuc
4. Luu rule

#### Sua bieu thuc rule
1. Chon rule trong DataGrid
2. Click **[Open Expression Builder]**
3. Xay dung hoac chinh sua bieu thuc (xem muc 10)
4. Click **[Apply]** trong dialog → bieu thuc cap nhat vao rule

#### Sap xep thu tu rule
- Chon rule → click **[Move Up]** hoac **[Move Down]**
- Thu tu anh huong den trinh tu kiem tra validation

---

## 9. Event Editor

> Man hinh quan ly event va action gan vao 1 field.

### Cach mo
- Click **[+ Them Event]** hoac **[⚙]** trong tab Events cua Field Config

### Giao dien
- **Cot trai — Events DataGrid**: Event ID, Trigger, Condition Preview, Action Count, Active
- **Cot phai — Actions DataGrid**: chi tiet action cua event dang chon
  - Action ID, Action Type, Target Field, Param JSON, Order

### Cac loai Trigger

| Trigger | Khi nao kich hoat |
|---------|-------------------|
| OnChange | Khi gia tri field thay doi |
| OnBlur | Khi field mat focus |
| OnFocus | Khi field duoc focus |
| OnLoad | Khi form duoc load |
| OnSubmit | Khi form duoc submit |

### Cac loai Action

| Action Type | Mo ta |
|-------------|-------|
| SetValue | Gan gia tri cho field khac |
| SetVisible | An/hien field khac |
| SetReadOnly | Khoa/mo field khac |
| SetRequired | Bat/tat bat buoc cho field khac |
| Recalculate | Tinh lai gia tri field khac |
| ShowMessage | Hien thong bao cho nguoi dung |
| Navigate | Chuyen trang |

### Thao tac tung buoc

#### Them event moi
1. Click **[+ Add Event]**
2. Event moi xuat hien voi trigger mac dinh OnChange
3. Chon trigger phu hop tu dropdown

#### Dat dieu kien (Condition)
1. Chon event trong DataGrid
2. Click **[Edit Condition]**
3. Expression Builder mo len → xay dung dieu kien
4. Click **[Apply]** → dieu kien cap nhat

#### Them action
1. Chon event (ben trai)
2. Click **[+ Add Action]** (ben phai)
3. Chon Action Type, Target Field, nhap tham so

---

## 10. Expression Builder

> Dialog xay dung bieu thuc logic bang giao dien truc quan (khong can viet code).

### Cach mo
- Click **[Open Expression Builder]** tu Rule Editor hoac Event Editor

### Giao dien 3 cot

**Cot trai — Palette:**
- **Operators**: danh sach toan tu (+, -, ==, !=, &&, ||, ...)
- **Functions**: danh sach ham (len, trim, iif, today, ...)
- **Fields**: danh sach field cua form (SoLuong, DonGia, ...)
- O tim kiem de loc

**Cot giua — AST Tree:**
- Cay bieu thuc hien thi cau truc logic
- Chon node de xem chi tiet
- Xoa node da chon

**Cot phai — Preview & Validation:**
- **Natural Language**: bieu thuc duoc dien giai bang ngon ngu tu nhien
- **JSON Output**: bieu thuc dang JSON (de luu vao DB)
- **Validation Status**: kiem tra bieu thuc hop le (kieu tra ve, loi cu phap...)

### Thao tac

1. Click **Operator/Function/Field** tu Palette → them vao AST Tree
2. Sap xep cac node de tao bieu thuc mong muon
3. Kiem tra **Validation Status** — dam bao khong co loi
4. Xem **Natural Language** de hieu bieu thuc vua tao
5. Click **[Apply]** de xac nhan → bieu thuc tra ve man hinh goi

### Vi du bieu thuc

| Bieu thuc | Dien giai |
|-----------|-----------|
| `SoLuong >= 1 && SoLuong <= 9999` | So luong phai tu 1 den 9999 |
| `TrangThai == "TuChoi"` | Khi trang thai la "Tu Choi" |
| `iif(SoLuong > 0, SoLuong * DonGia, 0)` | Tinh thanh tien, tra ve 0 neu so luong = 0 |
| `len(trim(MaDonHang)) > 0` | Ma don hang khong duoc rong |

---

## 11. Dependency Viewer

> Man hinh hien thi do thi phu thuoc giua Field, Rule va Event.

### Cach mo
- Click **[View Dependencies]** tu Form Editor

### Giao dien

**Cot trai — Bo loc:**
- Checkbox **Show Rules**: an/hien node Rule
- Checkbox **Show Events**: an/hien node Event
- Dropdown **Filter by Field**: loc theo 1 field cu the
- Nut **[Auto Layout]**: tu dong sap xep do thi
- Nut **[Regenerate]**: tai lai toan bo do thi

**Giua — Canvas do thi:**
- **Node xanh duong** (cot trai): Field — ten field va kieu du lieu
- **Node cam** (cot giua): Rule — loai rule va error key
- **Node xanh la** (cot phai): Event — trigger va field target
- **Mui ten**: quan he phu thuoc giua cac node

**Cot phai — Chi tiet node:**
- Khi click 1 node: hien loai, ten, thong tin phu
- Nut **[Open Node Editor]**: nhay den man hinh chinh sua tuong ung

### Thao tac
1. Xem tong quan phu thuoc giua cac thanh phan
2. Loc de tap trung vao 1 field cu the
3. Click node → xem chi tiet → click [Open Node Editor] de chuyen den man hinh chinh sua
4. Kiem tra **Circular Dependency** (canh bao vong lap) → sua neu co

---

## 12. Grammar Library

> Thu vien ham va toan tu duoc phep su dung trong bieu thuc.

### Cach mo
- Click **Grammar > Functions** hoac **Grammar > Operators** trong sidebar

### Tab Functions

| Cot | Mo ta |
|-----|-------|
| Function Name | Ten ham (vd: `len`, `iif`, `today`) |
| Category | Nhom: String, Math, Date, Logic, Conversion |
| Param Count | So tham so |
| Return Type | Kieu tra ve |
| Description | Mo ta chuc nang |
| Example | Vi du su dung |

- **Tim kiem**: go ten ham hoac mo ta de loc
- **Loc danh muc**: chon String, Math, Date, Logic, Conversion
- **[+ Add Function]**: them ham moi vao whitelist
- **[Delete]**: xoa ham khoi whitelist

### Tab Operators

| Cot | Mo ta |
|-----|-------|
| Symbol | Ky hieu (+, -, ==, &&, ...) |
| Name | Ten toan tu |
| Category | Nhom: Arithmetic, Comparison, Logical |
| Precedence | Do uu tien (so nho = uu tien cao) |
| Description | Mo ta |

> **Luu y**: Chi nhung ham/toan tu trong whitelist moi duoc phep dung trong Expression Builder. Publish Checklist se bao loi neu bieu thuc dung ham/toan tu khong co trong whitelist.

---

## 13. i18n Manager

> Quan ly cac resource key va ban dich da ngon ngu.

### Cach mo
- Click **i18n Keys** trong sidebar
- Hoac click **[Manage i18n →]** tu Field Config

### Giao dien
- **Bo loc**: o tim kiem, loc Module (Form/Field/Rule/Event/System), checkbox "Chi hien thieu ban dich"
- **Thong ke**: Tong entry / Hien thi / Thieu ban dich
- **DataGrid**: Resource Key, Module, Vi-Vn, En-Us, Ja-Jp, trang thai thieu

### Thao tac tung buoc

#### Them key moi
1. Click **[+ Add Entry]**
2. Dong moi xuat hien cuoi danh sach
3. Nhap Resource Key (vd: `lbl.newfield`)
4. Chon Module (Field)
5. Nhap ban dich cho tung ngon ngu

#### Tim key thieu ban dich
1. Bat checkbox **"Chi hien thieu ban dich"**
2. Danh sach loc chi con cac key chua dich day du
3. Nhap ban dich truc tiep vao o trong DataGrid

#### Sua ban dich
- Click truc tiep vao o ngon ngu trong DataGrid → nhap/sua ban dich

> **Luu y**: Publish Checklist se kiem tra tat ca Error_Key da duoc dich day du. Neu thieu se bao loi va co link nhay den day.

---

## 14. Publish Checklist

> Man hinh kiem tra truoc khi publish form ra production.

### Cach mo
- Click **[Publish]** tu Form Editor

### 11 muc kiem tra

| # | Kiem tra | Mo ta |
|---|---------|-------|
| 1 | Label_Key hop le | Tat ca field phai co Label_Key |
| 2 | Expression parse OK | Tat ca bieu thuc JSON phai parse thanh cong |
| 3 | Ham trong whitelist | Tat ca ham dung trong bieu thuc phai co trong Grammar Library |
| 4 | Toan tu trong whitelist | Tat ca toan tu phai co trong Grammar Library |
| 5 | Rule tra ve Boolean | Bieu thuc validation rule phai tra ve kieu Boolean |
| 6 | Kieu calculate tuong thich | Bieu thuc tinh toan phai tuong thich kieu du lieu field |
| 7 | Khong vong lap phu thuoc | Khong co circular dependency giua cac field |
| 8 | Do sau AST hop le | Cay bieu thuc khong qua 20 tang |
| 9 | Error_Key da dich | Tat ca error message da dich day du cac ngon ngu |
| 10 | URL API hop le | Tat ca URL trong CallAPI action phai dung dinh dang |
| 11 | Sys_Dependency day du | Bang phu thuoc cheo field phai duoc khai bao |

### Trang thai moi muc

| Trang thai | Y nghia |
|------------|---------|
| Pending | Chua chay |
| Running | Dang kiem tra |
| Passed | Dat |
| Failed | Loi — can sua |
| Warning | Canh bao — nen xem xet |

### Thao tac

1. Click **[Run All Checks]** → he thong chay 11 muc kiem tra
2. Xem ket qua:
   - **Tat ca Passed**: nut [Publish] duoc bat
   - **Co Failed**: xem chi tiet loi, click **[Jump To]** de nhay den man hinh lien quan de sua
3. Sua loi → quay lai → chay lai kiem tra
4. Khi tat ca Passed → click **[Publish]** → form duoc xuat ban

---

## 15. Cau hinh ung dung (Settings)

### Cach mo
- Click **Settings** trong sidebar

### Cac cau hinh

- **Ket noi DB**: connection string den SQL Server
- **Tenant ID**: ma doi tuong lam viec
- **Redis**: cau hinh cache (host, port)
- **Giao dien**: chon theme (Light Ocean / Slate Professional)

---

## Phu luc

### Phim tat

| Phim | Chuc nang |
|------|-----------|
| Ctrl+S | Luu (trong Form Editor, Field Config) |
| Escape | Huy / Dong dialog |
| F5 | Lam moi du lieu |

### Quy uoc ten

| Quy uoc | Vi du | Mo ta |
|---------|-------|-------|
| Form_Code | `PO_ORDER` | Chi chu HOA, so, dau gach duoi |
| Label_Key | `lbl.soluong` | Tien to `lbl.` + ten field viet thuong |
| Placeholder_Key | `ph.soluong` | Tien to `ph.` |
| Error_Key | `err.sl.range` | Tien to `err.` + viet tat |

### Loi thuong gap

| Loi | Nguyen nhan | Cach xu ly |
|-----|------------|------------|
| "Form_Code da ton tai" | Trung ma khi tao/clone | Doi ten Form_Code khac |
| Publish that bai: "Error_Key thieu ban dich" | Chua dich message sang tat ca ngon ngu | Mo i18n Manager → dich cac key con thieu |
| Publish that bai: "Circular dependency" | 2 field phu thuoc vong lap | Mo Dependency Viewer → pha vong lap |
| Expression khong hop le | Dung ham/toan tu khong co trong whitelist | Mo Grammar Library → them vao whitelist |
