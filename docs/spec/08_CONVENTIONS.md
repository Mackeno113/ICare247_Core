# 08 — Conventions

## Cache Keys

Tất cả key tập trung trong `CacheKeys.cs`. Không hardcode string rải rác.

```csharp
// Pattern: "icare:{resource}:{key}:..."
public static class CacheKeys
{
    // Form metadata (theo formCode + version + langCode + platform + tenantId)
    public static string Form(string formCode, int version, string langCode, string platform, int tenantId)
        => $"icare:form:{tenantId}:{formCode}:v{version}:lang:{langCode}:plat:{platform}";

    // Field list của form
    public static string FieldList(int formId, int tenantId)
        => $"icare:fields:{tenantId}:{formId}";

    // Rule list của field
    public static string RuleList(int formId, string fieldCode, int tenantId)
        => $"icare:rules:{tenantId}:{formId}:{fieldCode}";

    // Compiled AST delegate (theo hash của expression JSON)
    public static string CompiledAst(string expressionHash)
        => $"icare:ast:compiled:{expressionHash}";

    // Function whitelist
    public static string GramFunctions()
        => "icare:gram:functions";

    // Operator whitelist
    public static string GramOperators()
        => "icare:gram:operators";
}
```

**Lưu ý bắt buộc:**
- Mọi key có data của tenant → phải có `{tenantId}` trong key
- TTL mặc định: L1 Memory = 5 phút, L2 Redis = 30 phút

---

## Dapper Patterns

### Query chuẩn
```csharp
const string sql = """
    SELECT f.Form_Id,
           f.Form_Code,
           f.Version
    FROM   dbo.Ui_Form f
    WHERE  f.Form_Code = @FormCode
      AND  f.Tenant_Id = @TenantId
      AND  f.Is_Active = 1
    """;

var result = await conn.QueryFirstOrDefaultAsync<FormMetadata>(
    new CommandDefinition(sql, new { FormCode = formCode, TenantId = tenantId },
        cancellationToken: ct));
```

### Danh sách
```csharp
var items = await conn.QueryAsync<FieldMetadata>(
    new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
        cancellationToken: ct));
return items.AsList();
```

### Execute (Insert/Update)
```csharp
await conn.ExecuteAsync(
    new CommandDefinition(sql, param, cancellationToken: ct));
```

**Cấm tuyệt đối:**
```csharp
// ❌ String interpolation
$"WHERE Form_Code = '{formCode}'"

// ❌ SELECT *
SELECT * FROM dbo.Ui_Form

// ❌ new SqlConnection() trực tiếp
new SqlConnection(connectionString)  // → dùng IDbConnectionFactory

// ❌ .Result hay .Wait()
repository.GetAsync().Result
```

---

## Async / CancellationToken

```csharp
// ✅ Luôn truyền CancellationToken xuyên suốt
public async Task<FormMetadata?> GetByCodeAsync(
    string formCode, int tenantId, CancellationToken ct = default)
{
    await using var conn = await _factory.CreateAsync(ct);
    return await conn.QueryFirstOrDefaultAsync<FormMetadata>(
        new CommandDefinition(sql, param, cancellationToken: ct));
}

// ❌ Không dùng .Result
var form = _repo.GetByCodeAsync(code, tenantId).Result;
```

---

## File Header

Mọi file `.cs` phải có header:
```csharp
// File    : FormRepository.cs
// Module  : Form
// Layer   : Infrastructure
// Purpose : Repository truy vấn metadata form từ bảng Ui_Form qua Dapper.
```

---

## Comment Block trong Method

```csharp
public async Task<FormMetadata?> GetByCodeAsync(string formCode, int tenantId, CancellationToken ct)
{
    // ── 1. Check cache ────────────────────────────────────────────────────
    var key = CacheKeys.Form(formCode, ...);
    if (_cache.TryGetValue(key, out FormMetadata? cached))
        return cached;

    // ── 2. Load từ DB ─────────────────────────────────────────────────────
    await using var conn = await _factory.CreateAsync(ct);
    var form = await conn.QueryFirstOrDefaultAsync<FormMetadata>(...);

    // ── 3. Set cache + return ────────────────────────────────────────────
    if (form is not null)
        _cache.Set(key, form, TimeSpan.FromMinutes(5));

    return form;
}
```

---

## Exception Policy
- Repository methods: **không catch** SQL exceptions — bubble up
- Handler: catch domain exceptions, convert sang ProblemDetails
- GlobalExceptionMiddleware: catch tất cả unhandled → 500 ProblemDetails + log Error
- **Không swallow** exception trong engine (không `catch {}` rỗng)
