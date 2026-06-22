// File    : ContextParamResolver.cs
// Module  : Context
// Layer   : Infrastructure
// Purpose : Resolve giá trị token ngữ cảnh được tham chiếu trong SQL admin tự viết (spec 19, ADR-030):
//           Claim (bất biến) · Header · ActiveScope (header + Validate_Sql theo quyền). An toàn injection
//           (giá trị parameterized; chỉ token đăng ký được bind).

using System.Globalization;
using Dapper;
using ICare247.Application.Interfaces;
using ICare247.Domain.Entities.Context;
using Microsoft.Extensions.Logging;

namespace ICare247.Infrastructure.Repositories;

/// <summary>
/// Resolver token ngữ cảnh. Đọc registry (<see cref="IContextParamRepository"/>), lấy giá trị thô từ
/// <see cref="IRequestContextAccessor"/>, validate ActiveScope qua Data DB. Trả map tên→giá trị đã ép kiểu.
/// </summary>
public sealed class ContextParamResolver : IContextParamResolver
{
    private readonly IContextParamRepository _registry;
    private readonly IRequestContextAccessor _request;
    private readonly IDataDbConnectionFactory _dataDb;
    private readonly ILogger<ContextParamResolver> _logger;

    public ContextParamResolver(
        IContextParamRepository registry, IRequestContextAccessor request,
        IDataDbConnectionFactory dataDb, ILogger<ContextParamResolver> logger)
    {
        _registry = registry;
        _request = request;
        _dataDb = dataDb;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>> ResolveAsync(
        IEnumerable<string> referencedNames, CancellationToken ct = default)
    {
        var wanted = new HashSet<string>(
            referencedNames.Select(n => n.TrimStart('@')), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
            return result;

        var all = await _registry.GetActiveAsync(ct);
        foreach (var p in all)
        {
            if (!wanted.Contains(p.ParamName))
                continue;

            result[p.ParamName] = await ResolveOneAsync(p, ct);
        }
        return result;
    }

    /// <summary>Resolve 1 token theo Source_Kind → giá trị đã ép kiểu (null khi rỗng và không có Default).</summary>
    private async Task<object?> ResolveOneAsync(ContextParam p, CancellationToken ct)
    {
        switch (p.SourceKind)
        {
            case "Claim":
                return Convert(p.SourceKey == "sub" && _request.UserId > 0
                        ? _request.UserId.ToString(CultureInfo.InvariantCulture)
                        : _request.GetClaim(p.SourceKey),
                    p);

            case "Header":
                return Convert(_request.GetHeader(p.SourceKey), p);

            case "ActiveScope":
                return await ResolveActiveScopeAsync(p, ct);

            default:
                _logger.LogWarning("Context param '{Name}' có Source_Kind lạ '{Kind}'.", p.ParamName, p.SourceKind);
                return Convert(null, p);
        }
    }

    /// <summary>
    /// ActiveScope: đọc header → nếu rỗng dùng Default; nếu có giá trị, chạy Validate_Sql(@NguoiDungID,@val)
    /// → hợp lệ giữ giá trị, ngược lại ép Default (vd 0 = bỏ thu hẹp). Ranh giới cứng vẫn do @NguoiDungID giữ.
    /// </summary>
    private async Task<object?> ResolveActiveScopeAsync(ContextParam p, CancellationToken ct)
    {
        var raw = _request.GetHeader(p.SourceKey);
        if (string.IsNullOrWhiteSpace(raw))
            return Convert(p.DefaultValue, p);

        var val = Convert(raw, p);
        if (val is null || string.IsNullOrWhiteSpace(p.ValidateSql))
            return Convert(p.DefaultValue, p);

        try
        {
            using var data = _dataDb.CreateConnection();
            var ok = await data.ExecuteScalarAsync<int?>(new CommandDefinition(
                p.ValidateSql, new { NguoiDungID = _request.UserId, val }, cancellationToken: ct));
            return ok.GetValueOrDefault() > 0 ? val : Convert(p.DefaultValue, p);
        }
        catch (Exception ex)
        {
            // Validate lỗi (SQL sai/bảng chưa có) → fail-safe về Default (không lộ phạm vi rộng hơn).
            _logger.LogWarning(ex, "Validate_Sql token '{Name}' lỗi — ép Default.", p.ParamName);
            return Convert(p.DefaultValue, p);
        }
    }

    /// <summary>Ép chuỗi thô sang kiểu Sql_Type; parse hỏng → null (caller dùng Default nếu cần).</summary>
    private static object? Convert(string? raw, ContextParam p)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        raw = raw.Trim();
        return p.SqlType.ToLowerInvariant() switch
        {
            "bigint" => long.TryParse(raw, out var l) ? l : null,
            "int" => int.TryParse(raw, out var i) ? i : null,
            "decimal" => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (object?)null,
            "date" => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null,
            "bool" => raw is "1" or "true" or "True" ? true : raw is "0" or "false" or "False" ? false : (object?)null,
            _ => raw
        };
    }
}
