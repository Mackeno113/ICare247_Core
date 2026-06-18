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
    /// Cache key cho RuntimeFormContext (FormMetadata + ResourceMap) — dùng bởi MetadataEngine.
    /// Gắn slot <c>:v{version}</c> (version-stamp theo tenant) để "cưỡng chế làm mới" vô hiệu hàng loạt.
    /// Prefix: icare:meta:rt:{tenantId}:{formCode}:v{version}:lang:{langCode}
    /// </summary>
    public static string RuntimeForm(string formCode, string langCode, int tenantId, int version)
        => $"icare:meta:rt:{tenantId}:{formCode.ToLowerInvariant()}:v{version}:lang:{langCode}";

    /// <summary>
    /// Cache key cho ResourceMap của form theo ngôn ngữ.
    /// Prefix: icare:resource:{tenantId}:{formCode}:lang:{langCode}
    /// </summary>
    public static string ResourceMap(string formCode, string langCode, int tenantId)
        => $"icare:resource:{tenantId}:{formCode.ToLowerInvariant()}:lang:{langCode}";

    // ── ConfigCache facade keys (ADR-014) — gắn sẵn slot :v{version} (version-stamp ready) ──
    // version hiện = 0 (1 instance, dùng event-remove). Khi scale-out (CC-4a): version đọc từ
    // Redis cfgver:* → INCR khi sửa config → key đổi → mọi instance miss, KHÔNG cần pub/sub.

    /// <summary>
    /// ConfigCache — ResourceMap theo scope (form code / 'sys') + ngôn ngữ.
    /// Prefix: icare:cfg:resource:{tenantId}:{scope}:v{version}:lang:{langCode}
    /// </summary>
    public static string ConfigResourceMap(string scope, string langCode, int tenantId, int version)
        => $"icare:cfg:resource:{tenantId}:{scope.ToLowerInvariant()}:v{version}:lang:{langCode}";

    /// <summary>
    /// ConfigCache — Sys_Lookup options theo code + ngôn ngữ.
    /// Prefix: icare:cfg:lookup:{tenantId}:{lookupCode}:v{version}:lang:{langCode}
    /// </summary>
    public static string ConfigLookup(string lookupCode, string langCode, int tenantId, int version)
        => $"icare:cfg:lookup:{tenantId}:{lookupCode.ToLowerInvariant()}:v{version}:lang:{langCode}";

    /// <summary>
    /// ConfigCache — quyền form theo tenant.
    /// Prefix: icare:cfg:perm:{tenantId}:{formId}:v{version}
    /// </summary>
    public static string ConfigPermission(int formId, int tenantId, int version)
        => $"icare:cfg:perm:{tenantId}:{formId}:v{version}";

    /// <summary>
    /// Cache key cho ViewMetadata (header + cột + action), đã localize theo ngôn ngữ.
    /// Prefix: icare:view:{tenantId}:{viewCode}:v{version}:lang:{langCode}
    /// </summary>
    public static string View(string viewCode, int version, string langCode, int tenantId)
        => $"icare:view:{tenantId}:{viewCode.ToLowerInvariant()}:v{version}:lang:{langCode}";
}
