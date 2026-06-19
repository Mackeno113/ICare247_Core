# Báo cáo i18n — chuỗi cần xử lý

_Sinh tự động: 2026-06-19 14:45_

## 1. Chuỗi tiếng Việt hardcode (22) — cần bọc L()

- `src/frontend/ICare247_UI/Components/Auth/AuthButton.razor:39` — "Đang xử lý..."
- `src/frontend/ICare247_UI/Components/MasterData/ConfirmDeleteDialog.razor:41` — "vĩnh viễn"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:146` — "Tổng quan"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:161` — "Công cụ (Dev)"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:162` — "Tra cứu i18n"
- `src/frontend/ICare247_UI/Pages/Admin/MenuBuilderPage.razor:228` — "Tên trên menu được dịch theo khóa này; \"Tên hiển thị\" ở trên là bản gốc (ti..."
- `src/frontend/ICare247_UI/Pages/Admin/MenuBuilderPage.razor:267` — "Đường dẫn là URL điều hướng khi bấm node trên menu. Mặc định hệ thống tự suy ..."
- `src/frontend/ICare247_UI/Pages/Admin/MenuBuilderPage.razor:318` — "Danh sách icon đọc từ bộ icon dùng chung (Icon.razor). Thiếu icon? Kỹ thuật t..."
- `src/frontend/ICare247_UI/Pages/Admin/MenuBuilderPage.razor:363` — "Bạn chắc chắn xóa node <strong>{0}</strong>? Hành động không thể hoàn tác."
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:113` — "Thêm"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:114` — "Sửa"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:115` — "Xóa"
- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor:99` — "Xóa <strong>{0}</strong> bản ghi đã chọn? Bản ghi đang được tham chiếu sẽ đượ..."
- `src/frontend/ICare247_UI/Pages/View/ViewPage.razor:306` — "Đã xóa {0} bản ghi. Bị tham chiếu (giữ lại): {1}. Lỗi: {2}."
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:62` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/CacheAdminApiService.cs:79` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/FormApiService.cs:133` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:98` — "Lỗi máy chủ ({(int)resp.StatusCode})."
- `src/frontend/ICare247_UI/Services/MenuAdminApiService.cs:133` — "Nhóm"
- `src/frontend/ICare247_UI/Services/RuntimeApiService.cs:147` — "HTTP {(int)response.StatusCode} — body rỗng"
- `src/frontend/ICare247_UI/Services/ViewApiService.cs:70` — "Lỗi tải JSON: {ex.Message}"
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
