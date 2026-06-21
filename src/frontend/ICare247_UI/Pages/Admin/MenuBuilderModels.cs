// File    : MenuBuilderModels.cs
// Module  : ICare247_UI / Pages.Admin
// Purpose : Kiểu dữ liệu cho combobox (DxComboBox) ở màn Quản lý menu.
//           PHẢI là kiểu public top-level (giống LookupItem): DxComboBox nằm ở assembly DevExpress
//           khác → cần truy cập được kiểu để dựng accessor cho TextField/ValueField. Nếu để private
//           nested trong component, DevExpress fallback ToString() (hiện "ComboOpt {…}") và KHÔNG
//           rút được ValueField → giá trị bind ra null (Module không truyền lên server).

namespace ICare247_UI.Pages.Admin;

/// <summary>Tùy chọn combo khóa chuỗi (View, Phân hệ). ToString = nhãn hiển thị (dự phòng khi reflection lỗi).</summary>
public sealed record ComboOpt(string Code, string Display)
{
    public override string ToString() => Display;
}

// Node cha trước dùng combo (ParentOpt); nay chuyển sang DxDropDownBox + DxTreeList (chọn theo cây)
// nên không cần kiểu riêng — bind thẳng MenuNodeVm.
