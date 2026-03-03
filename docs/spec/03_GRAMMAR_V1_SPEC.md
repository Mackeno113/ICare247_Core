# 03 — Grammar V1 Specification

## Tổng quan
Expression lưu dạng JSON trong DB (`Expression_Json`). Parser chuyển JSON → AST (`IExpressionNode`), Compiler chuyển AST → `Func<EvaluationContext, object?>`.

## AST Node Types

| Node Type       | Ví dụ JSON                                      | Mô tả                         |
| --------------- | ----------------------------------------------- | ----------------------------- |
| `literal`       | `{"type":"literal","value":42}`                 | Giá trị literal (số, string, bool, null) |
| `identifier`    | `{"type":"identifier","name":"Field_A"}`        | Tham chiếu đến field trong context |
| `binary`        | `{"type":"binary","op":"+","left":...,"right":...}` | Phép toán 2 vế           |
| `unary`         | `{"type":"unary","op":"!","operand":...}`        | Phép toán 1 vế               |
| `function_call` | `{"type":"function_call","name":"iif","args":[...]}` | Gọi hàm whitelist       |
| `member_access` | `{"type":"member","object":...,"property":"..."}`| Truy cập property (dot notation) |

## Operators Whitelist (từ Gram_Operator)

| Ký hiệu | Loại     | Ưu tiên |
| ------- | -------- | ------- |
| `*`     | Số học   | 5       |
| `/`     | Số học   | 5       |
| `+`     | Số học   | 4       |
| `-`     | Số học   | 4       |
| `==`    | So sánh  | 3       |
| `!=`    | So sánh  | 3       |
| `>`     | So sánh  | 3       |
| `>=`    | So sánh  | 3       |
| `<`     | So sánh  | 3       |
| `<=`    | So sánh  | 3       |
| `&&`    | Logic    | 2       |
| `\|\|`  | Logic    | 1       |

## Functions Whitelist (từ Gram_Function)

| Hàm       | Params  | Mô tả                              |
| --------- | ------- | ---------------------------------- |
| `len`     | 1       | Độ dài string                      |
| `trim`    | 1       | Bỏ khoảng trắng đầu/cuối           |
| `upper`   | 1       | Chuyển hoa                         |
| `lower`   | 1       | Chuyển thường                      |
| `round`   | 1-2     | Làm tròn số                        |
| `floor`   | 1       | Làm tròn xuống                     |
| `ceil`    | 1       | Làm tròn lên                       |
| `iif`     | 3       | Conditional: iif(cond, true, false)|
| `toDate`  | 1-2     | Parse string → DateTime            |
| `today`   | 0       | Ngày hiện tại                      |
| `now`     | 0       | DateTime hiện tại                  |
| `coalesce`| variadic| Giá trị đầu tiên không null        |

## Null Propagation Rules
- Mọi phép toán có operand là `null` → kết quả là `null` (không throw)
- Chia cho `0` → trả `null` (không throw)
- `identifier` không tồn tại trong context → trả `null` (không throw)
- `function_call` với null argument → tuỳ hàm (hầu hết trả null)

## Giới hạn
- Max depth = 20 (configurable qua `appsettings.Grammar.MaxAstDepth`)
- Max expression size = 64KB JSON
- Không hỗ trợ: loop, assignment, side-effects trong expression

## Ví dụ Expression_Json

```json
{
  "type": "function_call",
  "name": "iif",
  "args": [
    {
      "type": "binary",
      "op": ">",
      "left": { "type": "identifier", "name": "Age" },
      "right": { "type": "literal", "value": 18 }
    },
    { "type": "literal", "value": "Đủ tuổi" },
    { "type": "literal", "value": "Chưa đủ tuổi" }
  ]
}
```
