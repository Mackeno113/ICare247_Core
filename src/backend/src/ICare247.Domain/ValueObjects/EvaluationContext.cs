// File    : EvaluationContext.cs
// Module  : Engine
// Layer   : Domain
// Purpose : Value object chứa toàn bộ giá trị field hiện tại của form để evaluate expression.

namespace ICare247.Domain.ValueObjects;

/// <summary>
/// Context chứa snapshot giá trị các field tại thời điểm evaluate expression.
/// Được truyền vào <c>IAstEngine.Evaluate()</c>, <c>IValidationEngine</c>, <c>IEventEngine</c>.
/// <para>
/// Lookup luôn OrdinalIgnoreCase vì Field_Code là technical name — không phân biệt hoa thường.
/// </para>
/// </summary>
public sealed class EvaluationContext
{
    private readonly Dictionary<string, object?> _values;

    /// <param name="values">Snapshot giá trị field: key = Field_Code, value = giá trị hiện tại.</param>
    public EvaluationContext(IDictionary<string, object?> values)
    {
        // NOTE: Dùng OrdinalIgnoreCase vì Field_Code là technical name
        _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tạo context rỗng — dùng khi form mới load chưa có giá trị nào.
    /// </summary>
    public static EvaluationContext Empty { get; } =
        new(new Dictionary<string, object?>());

    /// <summary>
    /// Lấy giá trị của field theo tên.
    /// NULL-SAFE: Trả null nếu không tồn tại — không throw.
    /// </summary>
    public object? GetValue(string fieldCode) =>
        _values.TryGetValue(fieldCode, out var value) ? value : null;

    /// <summary>
    /// TryGetValue pattern — dùng khi cần phân biệt "không tồn tại" vs "tồn tại nhưng null".
    /// </summary>
    public bool TryGetValue(string fieldCode, out object? value) =>
        _values.TryGetValue(fieldCode, out value);

    /// <summary>
    /// Tạo context mới với một field được cập nhật giá trị mới.
    /// Không mutate context hiện tại (immutable pattern).
    /// </summary>
    public EvaluationContext WithValue(string fieldCode, object? value)
    {
        var copy = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase)
        {
            [fieldCode] = value
        };
        return new EvaluationContext(copy);
    }

    /// <summary>Toàn bộ values để serialize hoặc logging.</summary>
    public IReadOnlyDictionary<string, object?> Values => _values;
}
