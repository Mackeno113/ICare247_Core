# Cache Rules — ICare247

## Caching Strategy

```
L1: MemoryCache (trong process, tốc độ cao)
    ↓ miss
L2: Redis (distributed, cross-instance)
    ↓ miss
L3: SQL Server qua Dapper
```

## Cache Key Rules

- Tất cả key lấy từ `CacheKeys.cs` — KHÔNG hardcode string rải rác
- Mọi key có data của tenant → phải có `{tenantId}` trong key
- TTL mặc định: L1 Memory = 5 phút, L2 Redis = 30 phút

```csharp
// ✅ ĐÚNG
var key = CacheKeys.Form(formCode, version, langCode, platform, tenantId);
// → "icare:form:{tenantId}:{formCode}:v{version}:lang:{langCode}:plat:{platform}"

// ❌ SAI — hardcode string
var key = $"form_{formCode}_{version}";
```

## CacheKeys.cs Pattern

```csharp
public static class CacheKeys
{
    public static string Form(string formCode, int version, string langCode, string platform, int tenantId)
        => $"icare:form:{tenantId}:{formCode}:v{version}:lang:{langCode}:plat:{platform}";

    public static string FieldList(int formId, int tenantId)
        => $"icare:fields:{tenantId}:{formId}";

    public static string RuleList(int formId, string fieldCode, int tenantId)
        => $"icare:rules:{tenantId}:{formId}:{fieldCode}";

    public static string CompiledAst(string expressionHash)
        => $"icare:ast:compiled:{expressionHash}";

    public static string GramFunctions() => "icare:gram:functions";
    public static string GramOperators() => "icare:gram:operators";
}
```
