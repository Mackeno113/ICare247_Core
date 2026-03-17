# Dapper Patterns — ICare247

## Quy tắc cứng

1. LUÔN dùng parameterized query — KHÔNG string interpolation vào SQL
2. LUÔN dùng `IDbConnectionFactory` — KHÔNG `new SqlConnection()` trực tiếp
3. LUÔN dùng async: `QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`
4. LUÔN truyền `CancellationToken` qua `CommandDefinition`
5. LUÔN có `AND Tenant_Id = @TenantId` cho bảng có Tenant_Id
6. LUÔN có `AND Is_Active = 1` cho soft-delete tables
7. KHÔNG `SELECT *` — chỉ SELECT cột cần thiết

## Template: Query single

```csharp
const string sql = """
    SELECT f.Form_Id, f.Form_Code, f.Version
    FROM   dbo.Ui_Form f
    WHERE  f.Form_Code = @FormCode
      AND  f.Tenant_Id = @TenantId
      AND  f.Is_Active = 1
    """;
var result = await conn.QueryFirstOrDefaultAsync<FormMetadata>(
    new CommandDefinition(sql, new { FormCode = formCode, TenantId = tenantId },
        cancellationToken: ct));
```

## Template: Query list

```csharp
var items = await conn.QueryAsync<FieldMetadata>(
    new CommandDefinition(sql, new { FormId = formId, TenantId = tenantId },
        cancellationToken: ct));
return items.AsList();
```

## Template: Execute (Insert/Update)

```csharp
await conn.ExecuteAsync(
    new CommandDefinition(sql, param, cancellationToken: ct));
```

## Cấm tuyệt đối

```csharp
// ❌ String interpolation (SQL injection risk)
$"WHERE Form_Code = '{formCode}'"

// ❌ SELECT *
SELECT * FROM dbo.Ui_Form

// ❌ new SqlConnection() trực tiếp
new SqlConnection(connectionString)  // → dùng IDbConnectionFactory

// ❌ .Result hay .Wait()
repository.GetAsync().Result
```
