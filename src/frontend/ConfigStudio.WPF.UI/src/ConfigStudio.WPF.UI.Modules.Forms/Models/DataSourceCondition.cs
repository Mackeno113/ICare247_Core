// File    : DataSourceCondition.cs
// Module  : Forms
// Layer   : Models
// Purpose : Mô tả 1 điều kiện chuyển đổi bảng nguồn dữ liệu của LookupBox.
//           Khi field "when.field" có giá trị thoả điều kiện "when.op"/"when.value"
//           thì runtime đổi tableName/displayField/filterSql tương ứng.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Một điều kiện trong <c>dataSourceConditions</c> của <c>LookupBox</c>.
/// <para>
/// VD: Nếu <c>LoaiNhanVien = "THUE_NGOAI"</c> → lấy dữ liệu từ <c>DM_DonViThueNgoai</c>
/// thay vì bảng mặc định.
/// </para>
/// </summary>
public sealed class DataSourceCondition : BindableBase
{
    // ── Điều kiện kích hoạt ────────────────────────────────────────

    private string _whenField = "";
    /// <summary>FieldCode của field trong form dùng để so sánh. VD: "LoaiNhanVien"</summary>
    public string WhenField
    {
        get => _whenField;
        set => SetProperty(ref _whenField, value);
    }

    private string _whenOp = "eq";
    /// <summary>Phép so sánh: eq | neq | gt | gte | lt | lte | contains | startsWith</summary>
    public string WhenOp
    {
        get => _whenOp;
        set => SetProperty(ref _whenOp, value);
    }

    private string _whenValue = "";
    /// <summary>Giá trị so sánh. VD: "THUE_NGOAI", "2", "true"</summary>
    public string WhenValue
    {
        get => _whenValue;
        set => SetProperty(ref _whenValue, value);
    }

    // ── Datasource thay thế ────────────────────────────────────────

    private string _tableName = "";
    /// <summary>Tên bảng DB thay thế khi điều kiện thoả. VD: "DM_DonViThueNgoai"</summary>
    public string TableName
    {
        get => _tableName;
        set => SetProperty(ref _tableName, value);
    }

    private string _displayField = "";
    /// <summary>Cột hiển thị trong bảng thay thế. VD: "Ten_Don_Vi"</summary>
    public string DisplayField
    {
        get => _displayField;
        set => SetProperty(ref _displayField, value);
    }

    private string _filterSql = "";
    /// <summary>Điều kiện lọc riêng cho bảng thay thế. VD: "Is_Active = 1"</summary>
    public string FilterSql
    {
        get => _filterSql;
        set => SetProperty(ref _filterSql, value);
    }

    // ── Helper: mô tả điều kiện bằng tiếng Việt ───────────────────

    /// <summary>Diễn giải điều kiện thành câu tiếng Việt ngắn gọn.</summary>
    public string WhenOpLabel => WhenOp switch
    {
        "eq"         => "=",
        "neq"        => "≠",
        "gt"         => ">",
        "gte"        => "≥",
        "lt"         => "<",
        "lte"        => "≤",
        "contains"   => "chứa",
        "startsWith" => "bắt đầu bằng",
        _            => WhenOp
    };
}
