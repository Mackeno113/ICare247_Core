# AST / Grammar Rules — ICare247

## Pipeline

```
Expression_Json (string) → Parse → IExpressionNode (AST)
                         → Compile → Func<EvaluationContext, object?>
                         → Execute → object?
```

## AST Node Types

| Node Type       | Mô tả                                |
| --------------- | ------------------------------------ |
| `literal`       | Giá trị literal (số, string, bool, null) |
| `identifier`    | Tham chiếu đến field trong context   |
| `binary`        | Phép toán 2 vế (+, -, *, /, ==, &&...) |
| `unary`         | Phép toán 1 vế (!)                   |
| `function_call` | Gọi hàm whitelist (len, trim, iif...) |
| `member_access` | Truy cập property (dot notation)     |

## Null Propagation Rules

- Phép toán có operand là `null` → kết quả là `null` (KHÔNG throw)
- Chia cho `0` → trả `null` (KHÔNG throw)
- `identifier` không tồn tại trong context → trả `null` (KHÔNG throw)
- `function_call` với null argument → tuỳ hàm (hầu hết trả null)

## Giới hạn

- **Max depth = 20** (configurable `appsettings.Grammar.MaxAstDepth`)
- Max expression size = 64KB JSON
- Không hỗ trợ: loop, assignment, side-effects

## Cấm tuyệt đối

- Không `eval()`, không `Roslyn.Compile`, không `dynamic` SQL
- Chỉ function/operator whitelist từ `Gram_Function`, `Gram_Operator`

## Compiled Delegate Cache

- Cache theo `hash(expressionJson)` qua `CacheKeys.CompiledAst(hash)`
