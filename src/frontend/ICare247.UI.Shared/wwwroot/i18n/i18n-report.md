# Báo cáo i18n — chuỗi cần xử lý

_Sinh tự động: 2026-06-25 11:47_

## 1. Chuỗi tiếng Việt hardcode (23) — cần bọc L()

- `src/frontend/ICare247_UI/Components/MasterData/ConfirmDeleteDialog.razor:41` — "vĩnh viễn"
- `src/frontend/ICare247_UI/Components/TreeSelectBox.razor:66` — "— Chọn —"
- `src/frontend/ICare247_UI/Components/TreeSelectBox.razor:69` — "Không có dữ liệu."
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:173` — "Tổng quan"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:190` — "Công cụ (Dev)"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:191` — "Tra cứu i18n"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:115` — "Thêm"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:116` — "Sửa"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:117` — "Xóa"
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:62` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:79` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/FormApiService.cs:134` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:98` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:133` — "Nhóm"
- `src/frontend/ICare247_UI/Services/RuntimeApiService.cs:148` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:74` — "Lỗi tải JSON: {ex.Message}"
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:222` — "Phiên đăng nhập đã hết hạn hoặc bạn chưa đăng nhập. Vui lòng đăng nhập lại."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:224` — "Bạn không có quyền xem màn hình{0}. Vui lòng liên hệ quản trị viên để được cấ..."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:226` — "Không tìm thấy màn hình{0} hoặc màn đã bị ẩn."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:228` — "Máy chủ phản hồi quá lâu. Vui lòng kiểm tra kết nối rồi thử lại."
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:230` — "Máy chủ đang gặp sự cố. Vui lòng thử lại sau hoặc liên hệ quản trị viên."
- `src/frontend/ICare247.UI.Shared/Services/Http/ApiErrorHelper.cs:54` — "{message} (Mã lỗi: {code})"
- `src/frontend/ICare247.UI.Shared/Services/I18n/LocalizationService.cs:67` — "Tiếng Việt"

## 2. L() key dựng động (18) — chỉ runtime (phương án A) lấy được

- `src/frontend/ICare247_UI/Layout/NavMenu.razor:12`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:39`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:47`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:62`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:64`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:203`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:205`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:224`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:226`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:243`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:256`
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:259`
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:80`
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:111`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:5`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:23`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:37`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:45`
