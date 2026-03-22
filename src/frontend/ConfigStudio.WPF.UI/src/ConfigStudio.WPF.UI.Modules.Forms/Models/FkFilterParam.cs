// File    : FkFilterParam.cs
// Module  : Forms
// Layer   : Models
// Purpose : Ánh xạ 1 tham số SQL (@Param) trong filterSql của LookupBox
//           tới 1 field khác trong cùng form (fieldRef).
//           Runtime engine resolve giá trị field đó rồi truyền vào SQL parameter.

using Prism.Mvvm;

namespace ConfigStudio.WPF.UI.Modules.Forms.Models;

/// <summary>
/// Tham số động trong <c>filterSql</c> của <c>LookupBox</c>.
/// <para>
/// VD: <c>{ "param": "NgayVaoLam", "fieldRef": "NgayVaoLam", "type": "DateTime" }</c>
/// → khi render form, engine lấy giá trị field <c>NgayVaoLam</c> rồi bind vào <c>@NgayVaoLam</c>.
/// </para>
/// Khi field <c>fieldRef</c> thay đổi giá trị → lookup tự động reload.
/// </summary>
public sealed class FkFilterParam : BindableBase
{
    private string _param = "";
    /// <summary>
    /// Tên tham số SQL (không có @). VD: "NgayVaoLam".
    /// Dùng trong filterSql như: <c>Ngay_Hieu_Luc &lt;= @NgayVaoLam</c>
    /// </summary>
    public string Param
    {
        get => _param;
        set => SetProperty(ref _param, value);
    }

    private string _fieldRef = "";
    /// <summary>
    /// FieldCode của field trong cùng form cung cấp giá trị.
    /// VD: "NgayVaoLam" → engine lấy form.fields["NgayVaoLam"].value tại runtime.
    /// </summary>
    public string FieldRef
    {
        get => _fieldRef;
        set => SetProperty(ref _fieldRef, value);
    }

    private string _type = "String";
    /// <summary>
    /// Kiểu dữ liệu của tham số: String | DateTime | Int | Decimal.
    /// Dùng để cast đúng kiểu khi truyền vào Dapper.
    /// </summary>
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }
}
