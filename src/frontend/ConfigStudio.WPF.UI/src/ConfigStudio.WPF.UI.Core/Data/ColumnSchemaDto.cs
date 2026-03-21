// File    : ColumnSchemaDto.cs
// Module  : Data
// Layer   : Core
// Purpose : DTO cột lấy từ INFORMATION_SCHEMA của Target DB (DB thực sự của ứng dụng).
//           Khác ColumnInfoRecord (lấy từ Sys_Column trong Config DB).

namespace ConfigStudio.WPF.UI.Core.Data;

/// <summary>
/// Thông tin một cột đọc trực tiếp từ INFORMATION_SCHEMA.COLUMNS của Target DB.
/// Dùng để auto-generate Ui_Field khi user chọn table.
/// </summary>
public sealed record ColumnSchemaDto
{
    /// <summary>Tên cột (COLUMN_NAME).</summary>
    public string ColumnName { get; init; } = "";

    /// <summary>Kiểu dữ liệu SQL (DATA_TYPE): nvarchar, int, bit, datetime2, ...</summary>
    public string DataType { get; init; } = "";

    /// <summary>Kiểu .NET tương ứng: string, int, bool, DateTime, ... (computed bởi DataTypeMapper).</summary>
    public string NetType { get; init; } = "";

    /// <summary>EditorType mặc định gợi ý: TextBox, NumericBox, CheckBox, DatePicker, ... (computed).</summary>
    public string DefaultEditorType { get; init; } = "";

    /// <summary>Có cho phép NULL không (IS_NULLABLE = 'YES').</summary>
    public bool IsNullable { get; init; }

    /// <summary>Là cột Identity (auto-increment). Exclude khi auto-generate.</summary>
    public bool IsIdentity { get; init; }

    /// <summary>Là Primary Key. Exclude khi auto-generate.</summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>Thứ tự cột trong bảng (ORDINAL_POSITION) — dùng làm Order_No mặc định.</summary>
    public int OrdinalPosition { get; init; }

    /// <summary>Độ dài tối đa (CHARACTER_MAXIMUM_LENGTH). Null nếu không áp dụng.</summary>
    public int? MaxLength { get; init; }

    /// <summary>Precision (NUMERIC_PRECISION). Null nếu không áp dụng.</summary>
    public int? NumericPrecision { get; init; }

    /// <summary>Scale (NUMERIC_SCALE). Null nếu không áp dụng.</summary>
    public int? NumericScale { get; init; }

    /// <summary>
    /// True nếu cột này nên được bỏ qua khi auto-generate field
    /// (Identity hoặc PrimaryKey).
    /// </summary>
    public bool ShouldSkip => IsIdentity || IsPrimaryKey;

    /// <summary>
    /// Tên hiển thị trong dialog auto-generate:
    /// "OrderDate  (datetime2, NULL)".
    /// </summary>
    public string DisplayLabel =>
        $"{ColumnName}  ({DataType}{(IsNullable ? ", NULL" : ", NOT NULL")})";
}
