# Báo cáo i18n — chuỗi cần xử lý

_Sinh tự động: 2026-06-20 15:36_

## 1. Chuỗi tiếng Việt hardcode (20) — cần bọc L()

- `src/frontend/ICare247_UI/Components/MasterData/ConfirmDeleteDialog.razor:41` — "vĩnh viễn"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:146` — "Tổng quan"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:161` — "Công cụ (Dev)"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:162` — "Tra cứu i18n"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:113` — "Thêm"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:114` — "Sửa"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:115` — "Xóa"
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:62` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:79` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/FormApiService.cs:133` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:98` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:133` — "Nhóm"
- `src/frontend/ICare247_UI/Services/RuntimeApiService.cs:147` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:73` — "Lỗi tải JSON: {ex.Message}"
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:192` — "Phiên đăng nhập đã hết hạn hoặc bạn chưa đăng nhập. Vui lòng đăng nhập lại."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:194` — "Bạn không có quyền xem màn hình{0}. Vui lòng liên hệ quản trị viên để được cấ..."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:196` — "Không tìm thấy màn hình{0} hoặc màn đã bị ẩn."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:198` — "Máy chủ phản hồi quá lâu. Vui lòng kiểm tra kết nối rồi thử lại."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:200` — "Máy chủ đang gặp sự cố. Vui lòng thử lại sau hoặc liên hệ quản trị viên."
- `src/frontend/ICare247.UI.Shared/Services/I18n/LocalizationService.cs:67` — "Tiếng Việt"

## 2. L() key dựng động (12) — chỉ runtime (phương án A) lấy được

- `src/frontend/ICare247_UI/Layout/NavMenu.razor:10`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:19`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:26`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:34`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:45`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:62`
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:78`
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:109`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:5`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:23`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:37`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:45`
