// File    : CacheKeys.cs
// Module  : Common
// Layer   : Application
// Purpose : Tập trung toàn bộ cache key — KHÔNG hardcode string rải rác trong code.

namespace ICare247.Application.Constants;

/// <summary>
/// Tập trung tất cả cache key patterns.
/// Mọi key chứa data tenant phải có <c>{tenantId}</c>.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Cache key cho FormMetadata aggregate (full load).
    /// Prefix: icare:form:{tenantId}:{formCode}:v{version}:lang:{langCode}:plat:{platform}
    /// </summary>
    public static string Form(string formCode, int version, string langCode, string platform, int tenantId)
        => $"icare:form:{tenantId}:{formCode}:v{version}:lang:{langCode}:plat:{platform}";

    /// <summary>
    /// Cache key cho danh sách fields của một form.
    /// Prefix: icare:fields:{tenantId}:{formId}
    /// </summary>
    public static string FieldList(int formId, int tenantId)
        => $"icare:fields:{tenantId}:{formId}";

    /// <summary>
    /// Cache key cho danh sách rules của một field trong form.
    /// Prefix: icare:rules:{tenantId}:{formId}:{fieldCode}
    /// </summary>
    public static string RuleList(int formId, string fieldCode, int tenantId)
        => $"icare:rules:{tenantId}:{formId}:{fieldCode}";

    /// <summary>
    /// Cache key cho AST đã compile (tránh parse lại expression giống nhau).
    /// Prefix: icare:ast:compiled:{expressionHash}
    /// </summary>
    public static string CompiledAst(string expressionHash)
        => $"icare:ast:compiled:{expressionHash}";

    /// <summary>Cache key cho danh sách Grammar functions (global).</summary>
    public static string GramFunctions() => "icare:gram:functions";

    /// <summary>Cache key cho danh sách Grammar operators (global).</summary>
    public static string GramOperators() => "icare:gram:operators";

    /// <summary>
    /// Cache key cho form list (dùng cho GetListAsync có paging/filter).
    /// Prefix: icare:formlist:{tenantId}:{hash}
    /// </summary>
    public static string FormList(int tenantId, string filterHash)
        => $"icare:formlist:{tenantId}:{filterHash}";

    /// <summary>
    /// Cache key cho Sys_Lookup items theo code + tenant + ngôn ngữ.
    /// Prefix: icare:lookup:{tenantId}:{lookupCode}:lang:{langCode}
    /// </summary>
    public static string Lookup(string lookupCode, int tenantId, string langCode)
        => $"icare:lookup:{tenantId}:{lookupCode.ToLowerInvariant()}:lang:{langCode}";
}
