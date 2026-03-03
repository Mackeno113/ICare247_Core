# 02 — Database Schema

## Quy ước chung
- Tên bảng: PascalCase, prefix theo module (`Ui_`, `Sys_`, `Gram_`)
- Mọi bảng có `Tenant_Id` đều phải filter trong mọi query
- Soft delete: `Is_Active = 0` (không xóa vật lý)
- Audit: `Created_At`, `Created_By`, `Updated_At`, `Updated_By`

## Module UI (Form Engine)

### Ui_Form
| Column      | Type         | Mô tả                        |
| ----------- | ------------ | -----------------------------|
| Form_Id     | INT PK       | Auto-increment                |
| Tenant_Id   | INT          | Bắt buộc filter               |
| Form_Code   | NVARCHAR(100)| Unique trong tenant           |
| Form_Name   | NVARCHAR(255)| Tên hiển thị                  |
| Version     | INT          | Phiên bản metadata            |
| Platform    | NVARCHAR(50) | 'web' / 'mobile'              |
| Is_Active   | BIT          | Soft delete                   |

### Ui_Section
| Column      | Type         | Mô tả                        |
| ----------- | ------------ | -----------------------------|
| Section_Id  | INT PK       |                               |
| Form_Id     | INT FK       | → Ui_Form                    |
| Tenant_Id   | INT          |                               |
| Section_Code| NVARCHAR(100)|                               |
| Sort_Order  | INT          |                               |
| Is_Active   | BIT          |                               |

### Ui_Field
| Column           | Type          | Mô tả                        |
| ---------------- | ------------- | -----------------------------|
| Field_Id         | INT PK        |                               |
| Form_Id          | INT FK        | → Ui_Form                    |
| Section_Id       | INT FK NULL   | → Ui_Section                 |
| Tenant_Id        | INT           |                               |
| Field_Code       | NVARCHAR(100) | Unique trong form             |
| Field_Type       | NVARCHAR(50)  | 'text','number','date',...    |
| Default_Value_Json| NVARCHAR(MAX)| JSON default                  |
| Is_Required      | BIT           |                               |
| Is_Active        | BIT           |                               |

## Module Grammar (AST Engine)

### Gram_Function
| Column        | Type          | Mô tả                        |
| ------------- | ------------- | -----------------------------|
| Function_Id   | INT PK        |                               |
| Function_Name | NVARCHAR(100) | VD: 'len', 'trim', 'iif'     |
| Param_Count   | INT           | -1 = variadic                |
| Is_Active     | BIT           |                               |

### Gram_Operator
| Column        | Type          | Mô tả                        |
| ------------- | ------------- | -----------------------------|
| Operator_Id   | INT PK        |                               |
| Operator_Symbol| NVARCHAR(10) | VD: '+', '-', '==', '&&'     |
| Precedence    | INT           | Độ ưu tiên (cao hơn = trước) |
| Is_Active     | BIT           |                               |

## Module System

### Sys_Dependency
Lưu dependency giữa các field (field A phụ thuộc field B để validate).

### Sys_Rule
Lưu rule metadata: Expression_Json, Error_Message, Severity.

## TODO
- Bổ sung schema chi tiết khi có file SQL seed chính thức
- Thêm indexes cho các query path thường dùng
