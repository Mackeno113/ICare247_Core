// File    : DataTypeMapper.cs
// Module  : Helpers
// Layer   : Core
// Purpose : Map kiểu dữ liệu SQL Server → NetType (.NET) và EditorType mặc định.
//           Dùng trong SchemaInspectorService khi auto-generate fields từ Target DB.

namespace ConfigStudio.WPF.UI.Core.Helpers;

/// <summary>
/// Bảng ánh xạ SQL DataType → NetType + EditorType mặc định.
/// Tất cả so sánh là case-insensitive.
/// </summary>
public static class DataTypeMapper
{
    // ── NetType mapping ──────────────────────────────────────
    // Nguồn: SQL Server system types → .NET CLR types
    private static readonly Dictionary<string, string> NetTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Chuỗi
            ["nvarchar"]         = "string",
            ["varchar"]          = "string",
            ["char"]             = "string",
            ["nchar"]            = "string",
            ["text"]             = "string",
            ["ntext"]            = "string",
            ["xml"]              = "string",

            // Số nguyên
            ["int"]              = "int",
            ["bigint"]           = "long",
            ["smallint"]         = "short",
            ["tinyint"]          = "byte",

            // Số thực
            ["decimal"]          = "decimal",
            ["numeric"]          = "decimal",
            ["money"]            = "decimal",
            ["smallmoney"]       = "decimal",
            ["float"]            = "double",
            ["real"]             = "float",

            // Boolean
            ["bit"]              = "bool",

            // Ngày giờ
            ["date"]             = "DateTime",
            ["datetime"]         = "DateTime",
            ["datetime2"]        = "DateTime",
            ["smalldatetime"]    = "DateTime",
            ["datetimeoffset"]   = "DateTimeOffset",
            ["time"]             = "TimeSpan",

            // Khác
            ["uniqueidentifier"] = "Guid",
            ["varbinary"]        = "byte[]",
            ["binary"]           = "byte[]",
            ["image"]            = "byte[]",
            ["rowversion"]       = "byte[]",
            ["timestamp"]        = "byte[]",
        };

    // ── EditorType mapping ───────────────────────────────────
    // Nguồn: NetType → EditorType dùng trong Ui_Field.Editor_Type
    private static readonly Dictionary<string, string> EditorTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["string"]       = "TextBox",
            ["int"]          = "NumericBox",
            ["long"]         = "NumericBox",
            ["short"]        = "NumericBox",
            ["byte"]         = "NumericBox",
            ["decimal"]      = "NumericBox",
            ["double"]       = "NumericBox",
            ["float"]        = "NumericBox",
            ["bool"]         = "CheckBox",
            ["DateTime"]     = "DatePicker",
            ["DateTimeOffset"] = "DatePicker",
            ["TimeSpan"]     = "TextBox",   // chưa có TimePicker — dùng TextBox
            ["Guid"]         = "TextBox",   // thường là PK/FK, sẽ bị ShouldSkip
            ["byte[]"]       = "TextBox",   // binary — hiếm dùng trong form
        };

    /// <summary>
    /// Trả về NetType (.NET) tương ứng với SQL DataType.
    /// Trả "string" nếu không nhận dạng được.
    /// </summary>
    public static string ToNetType(string sqlDataType)
        => NetTypeMap.TryGetValue(sqlDataType, out var net) ? net : "string";

    /// <summary>
    /// Trả về EditorType mặc định tương ứng với SQL DataType.
    /// Trả "TextBox" nếu không nhận dạng được.
    /// </summary>
    public static string ToEditorType(string sqlDataType)
    {
        var netType = ToNetType(sqlDataType);
        return EditorTypeMap.TryGetValue(netType, out var editor) ? editor : "TextBox";
    }
}
