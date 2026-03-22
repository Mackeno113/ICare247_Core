// File    : FunctionParam.cs
// Module  : Forms
// Layer   : Models
// Purpose : Mô tả 1 tham số của Table-Valued Function trong queryMode = "function".
//           Nguồn giá trị có thể là field khác trong form hoặc tham số hệ thống.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Tham số của TVF (Table-Valued Function).
/// <para>
/// VD: <c>fn_GetPhongBanHieuLuc(@NgayHieuLuc, @TenantId)</c><br/>
/// → Param[0]: { Name="NgayHieuLuc", SourceType="field",  FieldRef="NgayVaoLam", Type="DateTime" }<br/>
/// → Param[1]: { Name="TenantId",    SourceType="system", SystemKey="@TenantId" }
/// </para>
/// </summary>
public sealed class FunctionParam : BindableBase
{
    private string _name = "";
    /// <summary>Tên tham số truyền vào hàm. VD: "NgayHieuLuc", "TenantId".</summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _sourceType = "field";
    /// <summary>
    /// Nguồn lấy giá trị:
    /// <list type="bullet">
    /// <item>"field"  → lấy từ FieldRef trong form</item>
    /// <item>"system" → lấy từ SystemKey (@TenantId / @Today / @CurrentUser)</item>
    /// </list>
    /// </summary>
    public string SourceType
    {
        get => _sourceType;
        set
        {
            if (SetProperty(ref _sourceType, value))
            {
                RaisePropertyChanged(nameof(IsFieldSource));
                RaisePropertyChanged(nameof(IsSystemSource));
            }
        }
    }

    private string _fieldRef = "";
    /// <summary>FieldCode của field trong form cung cấp giá trị (khi SourceType = "field").</summary>
    public string FieldRef
    {
        get => _fieldRef;
        set => SetProperty(ref _fieldRef, value);
    }

    private string _systemKey = "@TenantId";
    /// <summary>Tham số hệ thống (khi SourceType = "system"). VD: "@TenantId", "@Today".</summary>
    public string SystemKey
    {
        get => _systemKey;
        set => SetProperty(ref _systemKey, value);
    }

    private string _type = "String";
    /// <summary>Kiểu dữ liệu: String | DateTime | Int | Decimal.</summary>
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    // ── Computed helpers ──────────────────────────────────────────

    public bool IsFieldSource  => SourceType == "field";
    public bool IsSystemSource => SourceType == "system";
}
