// File    : DuplicateValueException.cs
// Module  : Domain
// Layer   : Domain
// Purpose : Ném khi insert/update vi phạm ràng buộc duy nhất (field Is_Unique).
//           Tầng trên (handler) bắt và resolve message đa ngôn ngữ theo Column.

namespace ICare247.Domain.Exceptions;

/// <summary>
/// Giá trị đã tồn tại ở một cột phải duy nhất.
/// <see cref="Column"/> = tên cột vi phạm → tầng Application resolve thông báo i18n.
/// </summary>
public sealed class DuplicateValueException : Exception
{
    /// <summary>Tên bảng nguồn (Table_Code/Source_Name) — để build resource key.</summary>
    public string Table { get; }

    /// <summary>Tên cột (Column_Code) bị trùng.</summary>
    public string Column { get; }

    public DuplicateValueException(string table, string column)
        : base($"Duplicate value in '{table}.{column}'.")
    {
        Table  = table;
        Column = column;
    }

    /// <summary>Resource key thông báo trùng: {table}.val.{column}.unique (lowercase).</summary>
    public string ResourceKey =>
        $"{Table.ToLowerInvariant()}.val.{Column.ToLowerInvariant()}.unique";
}
