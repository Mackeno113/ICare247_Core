# 02 — Database Schema

> **Nguồn gốc:** Đồng bộ từ `ICare247_Config_202603181213.sql` — ngày 2026-03-18.
> Đây là nguồn chính xác. Mọi thay đổi schema phải cập nhật file này trước.

---

## Quy ước chung

| Quy tắc | Chi tiết |
|---|---|
| Naming | Prefix theo module: `Sys_`, `Ui_`, `Val_`, `Gram_`, `Evt_` |
| Soft delete | `Is_Active = 0` — không xóa vật lý |
| Timestamps | `Created_At`, `Updated_At` — kiểu `datetime DEFAULT getdate()` |
| String | `nvarchar` cho mọi text field |
| JSON | `nvarchar(max)` — suffix `_Json` hoặc `_Schema` |
| Localisation | Text hiển thị lưu dưới dạng **resource key** (`_Key`) → tra `Sys_Resource` |

### Chiến lược Tenant Isolation

`Tenant_Id` **không** có mặt ở mọi bảng. Thay vào đó:

- `Sys_Table` là trung tâm — có `Tenant_Id` (nullable: `NULL` = global, có giá trị = tenant-specific)
- `Ui_Form` → `Table_Id` → `Sys_Table.Tenant_Id`
- `Ui_Section`, `Ui_Field` → qua `Form_Id` → `Ui_Form` → `Sys_Table`
- `Sys_Column` → qua `Table_Id` → `Sys_Table`
- Các bảng có `Tenant_Id` trực tiếp: `Sys_Tenant` (master), `Sys_Role`, `Sys_Config`

---

## Module: System (`Sys_*`)

### Sys_Tenant
> Master table cho multi-tenant. Mỗi tenant = 1 organisation/khách hàng.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Tenant_Id | int | PK IDENTITY | |
| Tenant_Code | nvarchar(100) | UNIQUE NOT NULL | Mã định danh duy nhất |
| Tenant_Name | nvarchar(255) | NOT NULL | Tên hiển thị |
| Is_Active | bit | DEFAULT 1 | Soft delete |
| Created_At | datetime | DEFAULT getdate() | |
| Updated_At | datetime | DEFAULT getdate() | |

---

### Sys_Table
> Registry metadata của tất cả business table trong hệ thống. Là trung tâm của tenant isolation.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Table_Id | int | PK IDENTITY | |
| Table_Code | nvarchar(100) | NOT NULL | Mã bảng |
| Table_Name | nvarchar(255) | NOT NULL DEFAULT '' | Tên hiển thị |
| Schema_Name | nvarchar(50) | NOT NULL DEFAULT 'dbo' | SQL schema |
| Is_Tenant | bit | NOT NULL DEFAULT 0 | Bảng có data theo tenant không |
| Tenant_Id | int | NULL FK→Sys_Tenant | NULL = global, có giá trị = tenant-specific |
| Version | int | NOT NULL DEFAULT 1 | |
| Checksum | nvarchar(64) | NULL | Hash để detect thay đổi |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Created_At | datetime | DEFAULT getdate() | |
| Updated_At | datetime | DEFAULT getdate() | |
| Description | nvarchar(500) | NULL | |

**Indexes:**
- `UQ_Sys_Table_Code_Global`: UNIQUE `(Table_Code)` WHERE `Tenant_Id IS NULL`
- `UQ_Sys_Table_Code_Tenant`: UNIQUE `(Table_Code, Tenant_Id)` WHERE `Tenant_Id IS NOT NULL`

---

### Sys_Column
> Metadata của tất cả cột trong các business table. Là nền tảng để `Ui_Field` bind vào data.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Column_Id | int | PK IDENTITY | |
| Table_Id | int | NOT NULL FK→Sys_Table | |
| Column_Code | nvarchar(100) | NOT NULL | Tên cột thực tế trong DB |
| Data_Type | nvarchar(50) | NOT NULL | SQL type: 'nvarchar', 'int', 'bit',... |
| Net_Type | nvarchar(50) | NOT NULL | .NET type: 'string', 'int', 'bool',... |
| Max_Length | int | NULL | Độ dài tối đa (nvarchar) |
| Precision | int | NULL | Độ chính xác (decimal) |
| Scale | int | NULL | Số chữ số thập phân |
| Is_Nullable | bit | NOT NULL DEFAULT 1 | |
| Is_PK | bit | NOT NULL DEFAULT 0 | Là primary key không |
| Is_Identity | bit | NOT NULL DEFAULT 0 | Auto-increment không |
| Default_Value | nvarchar(255) | NULL | Giá trị mặc định |
| Version | int | NOT NULL DEFAULT 1 | |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |

**Constraints:** UNIQUE `(Table_Id, Column_Code)`

**Indexes:** `IX_Sys_Column_Table (Table_Id, Is_Active)`

---

### Sys_Relation
> Định nghĩa quan hệ master-detail giữa các bảng, dùng để render lookup/combobox.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Relation_Id | int | PK IDENTITY | |
| Master_Table_Id | int | NOT NULL FK→Sys_Table | Bảng cha |
| Detail_Table_Id | int | NOT NULL FK→Sys_Table | Bảng con |
| Relation_Type | nvarchar(50) | NOT NULL | VD: 'OneToMany', 'Lookup' |
| Display_Column | nvarchar(100) | NULL | Cột hiển thị trong dropdown |
| Value_Column | nvarchar(100) | NULL | Cột lưu giá trị khi chọn |
| Is_Active | bit | NOT NULL DEFAULT 1 | |

---

### Sys_Dependency
> Đồ thị dependency giữa các object (field, section, rule,...) trong một form.
> Dùng để tính toán thứ tự evaluate và invalidate cache có chọn lọc.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Dependency_Id | int | PK IDENTITY | |
| Source_Type | nvarchar(50) | NOT NULL | Loại object nguồn: 'Field', 'Section',... |
| Source_Id | int | NOT NULL | ID của object nguồn |
| Target_Type | nvarchar(50) | NOT NULL | Loại object đích |
| Target_Id | int | NOT NULL | ID của object đích |
| Form_Id | int | NOT NULL FK→Ui_Form | Phạm vi của dependency |
| Is_Active | bit | NOT NULL DEFAULT 1 | |

**Constraints:** UNIQUE `(Source_Type, Source_Id, Target_Type, Target_Id, Form_Id)`

**Indexes:**
- `IX_Sys_Dependency_Source (Source_Type, Source_Id, Is_Active)`
- `IX_Sys_Dependency_Target (Target_Type, Target_Id)`

---

### Sys_Version
> Snapshot version của các object để detect thay đổi và invalidate cache.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Object_Type | nvarchar(50) | PK | 'Form', 'Field', 'Rule',... |
| Object_Id | int | PK | ID của object |
| Version | int | NOT NULL | Version hiện tại |
| Updated_At | datetime | DEFAULT getdate() | |

---

### Sys_Role
> Quản lý roles. Role có thể là global (Tenant_Id = NULL) hoặc per-tenant.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Role_Id | int | PK IDENTITY | |
| Role_Code | nvarchar(100) | NOT NULL | |
| Role_Name | nvarchar(255) | NOT NULL | |
| Tenant_Id | int | NULL FK→Sys_Tenant | NULL = global role |
| Is_Active | bit | NOT NULL DEFAULT 1 | |

**Indexes:**
- `UQ_Sys_Role_Code_Global`: UNIQUE `(Role_Code)` WHERE `Tenant_Id IS NULL`
- `UQ_Sys_Role_Code_Tenant`: UNIQUE `(Role_Code, Tenant_Id)` WHERE `Tenant_Id IS NOT NULL`

---

### Sys_Permission
> Phân quyền theo role trên từng object (Form, Section, Field,...).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Permission_Id | int | PK IDENTITY | |
| Role_Id | int | NOT NULL FK→Sys_Role | |
| Object_Type | nvarchar(50) | NOT NULL | 'Form', 'Section', 'Field' |
| Object_Id | int | NOT NULL | ID của object |
| Can_Read | bit | NOT NULL DEFAULT 0 | |
| Can_Write | bit | NOT NULL DEFAULT 0 | |
| Can_Submit | bit | NOT NULL DEFAULT 0 | |

**Constraints:** UNIQUE `(Role_Id, Object_Type, Object_Id)`

**Indexes:** `IX_Sys_Permission_Object (Object_Type, Object_Id)`

---

### Sys_Config
> Cấu hình hệ thống. Hỗ trợ global và per-tenant override.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Config_Id | int | PK IDENTITY | |
| Config_Key | nvarchar(150) | NOT NULL | |
| Config_Value | nvarchar(max) | NOT NULL | |
| Scope | nvarchar(50) | NOT NULL DEFAULT 'Global' | Nhóm cấu hình |
| Tenant_Id | int | NULL FK→Sys_Tenant | NULL = global |
| Version | int | NOT NULL DEFAULT 1 | |

**Indexes:**
- `UQ_Sys_Config_Global`: UNIQUE `(Config_Key, Scope)` WHERE `Tenant_Id IS NULL`
- `UQ_Sys_Config_Tenant`: UNIQUE `(Config_Key, Scope, Tenant_Id)` WHERE `Tenant_Id IS NOT NULL`

---

### Sys_Language
> Danh sách ngôn ngữ hỗ trợ.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Lang_Code | nvarchar(10) | PK | VD: 'vi', 'en' |
| Lang_Name | nvarchar(100) | NOT NULL DEFAULT '' | |
| Is_Default | bit | NOT NULL DEFAULT 0 | |

**Indexes:** `UQ_Sys_Language_Default`: UNIQUE `(Is_Default)` WHERE `Is_Default = 1`
*(Đảm bảo chỉ có 1 ngôn ngữ mặc định)*

---

### Sys_Resource
> Bảng i18n — lưu toàn bộ text hiển thị theo ngôn ngữ.
> Mọi label, placeholder, tooltip trong UI đều tra qua bảng này bằng `_Key`.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Resource_Key | nvarchar(150) | PK | VD: 'field.patient_name.label' |
| Lang_Code | nvarchar(10) | PK FK→Sys_Language | |
| Resource_Value | nvarchar(max) | NOT NULL | Text thực tế |
| Version | int | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |

---

### Sys_Cache_Invalidation
> Queue để invalidate cache phân tán (Redis). Background worker đọc và publish.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Invalidation_Id | bigint | PK IDENTITY | |
| Object_Type | nvarchar(50) | NOT NULL | Loại object bị thay đổi |
| Object_Id | int | NOT NULL | ID của object |
| Invalidated_At | datetime | DEFAULT getdate() | |
| Is_Published | bit | NOT NULL DEFAULT 0 | Đã publish lên Redis chưa |
| Published_At | datetime | NULL | |

**Indexes:** `IX_Sys_Cache_Invalidation_Pending (Is_Published, Invalidated_At)` WHERE `Is_Published = 0`

---

### Sys_Audit_Log
> Lịch sử thay đổi tất cả object quan trọng. Lưu before/after JSON.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Audit_Id | bigint | PK IDENTITY | |
| Object_Type | nvarchar(50) | NOT NULL | 'Form', 'Field', 'Rule',... |
| Object_Id | int | NOT NULL | |
| Action | nvarchar(20) | NOT NULL | 'INSERT', 'UPDATE', 'DELETE' |
| Changed_By | nvarchar(150) | NOT NULL | Username |
| Changed_At | datetime | DEFAULT getdate() | |
| Old_Value_Json | nvarchar(max) | NULL | Snapshot trước khi thay đổi |
| New_Value_Json | nvarchar(max) | NULL | Snapshot sau khi thay đổi |
| Correlation_Id | nvarchar(64) | NULL | Trace ID để correlate với request |

**Indexes:**
- `IX_Sys_Audit_Log_Created (Changed_At)`
- `IX_Sys_Audit_Log_Object (Object_Type, Object_Id)`

---

### Sys_Perf_Log
> Ghi nhận performance metrics: cache hit/miss, query duration.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Perf_Id | bigint | PK IDENTITY | |
| Metric_Type | nvarchar(50) | NOT NULL | VD: 'CacheHit', 'DbQuery' |
| Reference_Code | nvarchar(150) | NULL | VD: form code, query name |
| Duration_MS | bigint | NOT NULL | Thời gian xử lý (ms) |
| Is_Cache_Hit | bit | NULL | NULL = không áp dụng |
| Correlation_Id | nvarchar(64) | NULL | |
| Created_At | datetime | DEFAULT getdate() | |

**Indexes:**
- `IX_Sys_Perf_Log_Created (Created_At)`
- `IX_Sys_Perf_Log_Type (Metric_Type, Created_At)`

---

### Sys_Error_Log
> Lưu lỗi runtime chưa được xử lý (unhandled exceptions).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Error_Id | bigint | PK IDENTITY | |
| Source | nvarchar(150) | NULL | Class/method gây lỗi |
| Message | nvarchar(max) | NULL | |
| Stack | nvarchar(max) | NULL | Stack trace |
| Correlation_Id | nvarchar(64) | NULL | |
| Created_At | datetime | DEFAULT getdate() | |

**Indexes:** `IX_Sys_Error_Log_Created (Created_At)`

---

## Module: UI Form Engine (`Ui_*`)

### Ui_Form
> Định nghĩa một form. Gắn với một business table và một platform.
> Tenant được resolve qua `Table_Id → Sys_Table.Tenant_Id`.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Form_Id | int | PK IDENTITY | |
| Form_Code | nvarchar(100) | UNIQUE NOT NULL | Mã định danh form |
| Table_Id | int | NOT NULL FK→Sys_Table | Business table form này thao tác |
| Platform | nvarchar(50) | NOT NULL | 'web', 'mobile', 'wpf' |
| Layout_Engine | nvarchar(50) | NOT NULL DEFAULT 'Grid' | Engine render layout |
| Version | int | NOT NULL DEFAULT 1 | |
| Checksum | nvarchar(64) | NULL | |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |
| Description | nvarchar(500) | NULL | |

**Indexes:** `IX_Ui_Form_Table (Table_Id, Is_Active)`

---

### Ui_Section
> Nhóm các field trong form thành từng section (tab, panel, group).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Section_Id | int | PK IDENTITY | |
| Form_Id | int | NOT NULL FK→Ui_Form | |
| Section_Code | nvarchar(100) | NOT NULL | Unique trong form |
| Title_Key | nvarchar(150) | NULL | Resource key → Sys_Resource |
| Order_No | int | NOT NULL DEFAULT 0 | Thứ tự hiển thị |
| Layout_Json | nvarchar(max) | NULL | Cấu hình layout chi tiết (JSON) |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Description | nvarchar(500) | NULL | |

**Indexes:**
- `IX_Ui_Section_Form (Form_Id, Is_Active, Order_No)`
- `UQ_Ui_Section_Code`: UNIQUE `(Form_Id, Section_Code)` WHERE `Is_Active = 1`

---

### Ui_Field
> Định nghĩa một field trên form. Bind vào một cột DB qua `Column_Id`.
> Mỗi field có editor type, label, và config props riêng.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Field_Id | int | PK IDENTITY | |
| Form_Id | int | NOT NULL FK→Ui_Form | |
| Section_Id | int | NULL FK→Ui_Section | NULL = field không thuộc section nào |
| Column_Id | int | NOT NULL FK→Sys_Column | Cột DB field này bind vào |
| Editor_Type | nvarchar(50) | NOT NULL | 'TextBox', 'DateEdit', 'ComboBox',... |
| Label_Key | nvarchar(150) | NOT NULL | Resource key → Sys_Resource |
| Placeholder_Key | nvarchar(150) | NULL | Resource key |
| Tooltip_Key | nvarchar(150) | NULL | Resource key |
| Is_Visible | bit | NOT NULL DEFAULT 1 | |
| Is_ReadOnly | bit | NOT NULL DEFAULT 0 | |
| Order_No | int | NOT NULL DEFAULT 0 | Thứ tự trong section |
| Control_Props_Json | nvarchar(max) | NULL | Props riêng của component (JSON) |
| Version | int | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |
| Description | nvarchar(500) | NULL | |

**Indexes:** `IX_Ui_Field_Form (Form_Id, Is_Visible, Order_No)`

> **Lưu ý:** `Is_Required` không có trong bảng này — logic required được định nghĩa qua `Val_Rule` với `Rule_Type_Code = 'Required'` và liên kết qua `Val_Rule_Field`.

---

### Ui_Control_Map
> Ánh xạ từ `Editor_Type` sang component name thực tế theo platform.
> Cho phép cùng một `Editor_Type = 'DateEdit'` render thành component khác nhau trên web/mobile/wpf.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Editor_Type | nvarchar(50) | PK | VD: 'TextBox', 'DateEdit' |
| Platform | nvarchar(50) | PK | 'web', 'mobile', 'wpf' |
| Control_Name | nvarchar(100) | NOT NULL | VD: 'DxTextBox', 'MauiEntry' |
| Default_Props_Json | nvarchar(max) | NULL | Props mặc định cho component |

---

## Module: Validation (`Val_*`)

### Val_Rule_Type
> Danh mục loại validation rule. Mỗi loại có schema params riêng.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Rule_Type_Code | nvarchar(50) | PK | VD: 'Required', 'Regex', 'Range', 'Custom' |
| Param_Schema | nvarchar(max) | NULL | JSON Schema mô tả params hợp lệ |

---

### Val_Rule
> Định nghĩa một validation rule thuộc về một field cụ thể.
> `Error_Key` theo pattern `{table}.val.{column}.{ruletype}` — unique toàn bảng, phản ánh rule luôn gắn với đúng 1 field.
> *(Migration: 003_remove_val_rule_field.sql — bỏ bảng junction Val_Rule_Field, gộp Field_Id vào đây)*

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Rule_Id | int | PK IDENTITY | |
| Field_Id | int | NOT NULL FK→Ui_Field | Field sở hữu rule này |
| Rule_Type_Code | nvarchar(50) | NOT NULL FK→Val_Rule_Type | VD: 'Required', 'Range', 'Regex', 'Custom' |
| Error_Key | nvarchar(150) | NOT NULL UNIQUE | Pattern: `{table}.val.{column}.{type}` → Sys_Resource |
| Severity | nvarchar(20) | NOT NULL DEFAULT 'Error' | 'Error' / 'Warning' / 'Info' |
| Expression_Json | nvarchar(max) | NULL | AST expression (bắt buộc trừ Required) |
| Condition_Expr | nvarchar(max) | NULL | Điều kiện áp dụng rule (AST) |
| Order_No | int | NOT NULL DEFAULT 0 | Thứ tự evaluate trong field |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |

**Constraints:**
- `CHK_Val_Rule_HasExpression`: `Expression_Json IS NOT NULL OR Rule_Type_Code = 'Required'`
  *(Rule Required không cần Expression — engine tự hiểu là kiểm tra not null/empty)*
- `UX_Val_Rule_ErrorKey`: UNIQUE `(Error_Key)`

**Indexes:** `IX_Val_Rule_Field_Id (Field_Id, Order_No)`

> **Thiết kế:** Quan hệ là **1 field → nhiều rules** (1-N). Mỗi rule có `Error_Key` riêng nên không thể dùng chung giữa các field. Bảng junction `Val_Rule_Field` đã bị loại bỏ trong Migration 003.

---

## Module: Grammar / AST Engine (`Gram_*`)

### Gram_Operator
> Danh mục operators trong Grammar V1. `Operator_Symbol` là natural key (PK).

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Operator_Symbol | nvarchar(20) | PK | VD: `+`, `-`, `==`, `&&`, `!=` |
| Operator_Type | nvarchar(50) | NOT NULL | 'Arithmetic', 'Comparison', 'Logical' |
| Precedence | int | NOT NULL DEFAULT 0 | Độ ưu tiên — cao hơn = evaluate trước |
| Description | nvarchar(255) | NULL | |
| Is_Active | bit | NOT NULL DEFAULT 1 | |

---

### Gram_Function
> Danh mục built-in functions trong Grammar V1.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Function_Id | int | PK IDENTITY | |
| Function_Code | nvarchar(100) | UNIQUE NOT NULL | VD: 'len', 'trim', 'iif', 'today' |
| Description | nvarchar(500) | NULL | |
| Return_Net_Type | nvarchar(50) | NOT NULL | .NET return type |
| Param_Count_Min | int | NOT NULL DEFAULT 0 | Số param tối thiểu |
| Param_Count_Max | int | NOT NULL DEFAULT 0 | Số param tối đa (0 = không giới hạn) |
| Is_System | bit | NOT NULL DEFAULT 1 | System function hay user-defined |
| Is_Active | bit | NOT NULL DEFAULT 1 | |

> **Thay đổi so với thiết kế cũ:** Không còn dùng `Param_Count = -1` cho variadic.
> Thay bằng `Param_Count_Max = 0` nghĩa là không giới hạn số params.

---

### Gram_Function_Param
> Định nghĩa từng parameter của function, bao gồm type và optional/default.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Param_Id | int | PK IDENTITY | |
| Function_Id | int | NOT NULL FK→Gram_Function | |
| Param_Index | int | NOT NULL | Vị trí (0-based) |
| Param_Name | nvarchar(100) | NOT NULL | Tên param |
| Net_Type | nvarchar(50) | NOT NULL | .NET type |
| Is_Optional | bit | NOT NULL DEFAULT 0 | |
| Default_Value | nvarchar(255) | NULL | Giá trị mặc định nếu optional |

**Constraints:** UNIQUE `(Function_Id, Param_Index)`

**Indexes:** `IX_Gram_Function_Param (Function_Id)`

---

## Module: Event Engine (`Evt_*`)

### Evt_Trigger_Type
> Danh mục các trigger event. Lookup table đơn giản.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Trigger_Code | nvarchar(50) | PK | VD: 'OnChange', 'OnBlur', 'OnLoad', 'OnSubmit' |

---

### Evt_Definition
> Định nghĩa một event handler: khi nào trigger (Trigger_Code), trên form/field nào, với điều kiện gì.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Event_Id | int | PK IDENTITY | |
| Form_Id | int | NOT NULL FK→Ui_Form | |
| Field_Id | int | NULL FK→Ui_Field | NULL = event ở form level |
| Trigger_Code | nvarchar(50) | NOT NULL FK→Evt_Trigger_Type | |
| Condition_Expr | nvarchar(max) | NULL | AST expression — điều kiện để execute actions |
| Order_No | int | NOT NULL DEFAULT 0 | Thứ tự khi nhiều event cùng trigger |
| Is_Active | bit | NOT NULL DEFAULT 1 | |
| Updated_At | datetime | DEFAULT getdate() | |

**Indexes:**
- `IX_Evt_Definition_Field (Field_Id, Trigger_Code, Is_Active)` WHERE `Field_Id IS NOT NULL`
- `IX_Evt_Definition_Form (Form_Id, Is_Active)`

---

### Evt_Action_Type
> Danh mục loại action. Mỗi loại có schema params riêng.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Action_Code | nvarchar(50) | PK | VD: 'SetValue', 'ShowHide', 'CallApi', 'Navigate' |
| Param_Schema | nvarchar(max) | NULL | JSON Schema mô tả params hợp lệ |

---

### Evt_Action
> Danh sách actions sẽ thực thi khi event trigger. Một event có thể có nhiều actions theo thứ tự.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Action_Id | int | PK IDENTITY | |
| Event_Id | int | NOT NULL FK→Evt_Definition | |
| Action_Code | nvarchar(50) | NOT NULL FK→Evt_Action_Type | |
| Action_Param_Json | nvarchar(max) | NULL | Params cụ thể của action (JSON) |
| Order_No | int | NOT NULL DEFAULT 0 | Thứ tự execute |

**Indexes:** `IX_Evt_Action_Event (Event_Id, Order_No)`

---

### Evt_Execution_Log
> Lịch sử thực thi event: kết quả, duration, error nếu có. Dùng để debug và audit.

| Column | Type | Constraint | Mô tả |
|---|---|---|---|
| Exec_Id | bigint | PK IDENTITY | |
| Event_Id | int | NOT NULL | FK không enforce để giữ log khi event bị xóa |
| Form_Code | nvarchar(100) | NOT NULL | Snapshot tại thời điểm execute |
| Trigger_Code | nvarchar(50) | NOT NULL | |
| Condition_Result | nvarchar(10) | NULL | 'true', 'false', 'skip' |
| Actions_Json | nvarchar(max) | NULL | Snapshot actions đã execute |
| Result_Json | nvarchar(max) | NULL | Kết quả trả về |
| Is_Success | bit | NOT NULL | |
| Error_Message | nvarchar(max) | NULL | |
| Duration_MS | bigint | NOT NULL DEFAULT 0 | |
| Correlation_Id | nvarchar(64) | NULL | |
| Created_At | datetime | DEFAULT getdate() | |

**Indexes:**
- `IX_Evt_Exec_Log_Corr (Correlation_Id)`
- `IX_Evt_Exec_Log_Created (Created_At)`
- `IX_Evt_Exec_Log_Event (Event_Id, Created_At)`

---

## Sơ đồ quan hệ tổng thể

```
Sys_Tenant
    └── Sys_Table (Tenant_Id nullable)
            ├── Sys_Column
            │       └── Ui_Field ──────────────────── Val_Rule (Field_Id FK) ── Val_Rule_Type
            └── Ui_Form (Table_Id)
                    ├── Ui_Section
                    │       └── Ui_Field (Section_Id nullable)
                    ├── Evt_Definition (Form_Id / Field_Id)
                    │       └── Evt_Action ── Evt_Action_Type
                    └── Sys_Dependency

Sys_Relation  : Sys_Table ←→ Sys_Table
Ui_Control_Map: Editor_Type + Platform → Control_Name

Gram_Operator        (lookup, độc lập)
Gram_Function        (lookup, độc lập)
    └── Gram_Function_Param

Sys_Language
    └── Sys_Resource (Resource_Key + Lang_Code → text)

Sys_Role (Tenant_Id nullable)
    └── Sys_Permission

Sys_Config (Tenant_Id nullable)
Sys_Version
Sys_Cache_Invalidation
Sys_Audit_Log
Sys_Perf_Log
Sys_Error_Log
Evt_Execution_Log
```

---

## Thống kê

| Module | Số bảng |
|---|---|
| System (`Sys_*`) | 15 |
| UI Form Engine (`Ui_*`) | 4 |
| Validation (`Val_*`) | 3 |
| Grammar / AST (`Gram_*`) | 3 |
| Event Engine (`Evt_*`) | 5 |
| **Tổng** | **30** |
