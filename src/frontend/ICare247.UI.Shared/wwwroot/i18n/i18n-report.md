# Báo cáo i18n — chuỗi cần xử lý

_Sinh tự động: 2026-06-14 23:06_

## 1. Chuỗi tiếng Việt hardcode (12) — cần bọc L()

- `src/frontend/ICare247_UI/Components/Auth/AuthButton.razor:39` — "Đang xử lý..."
- `src/frontend/ICare247_UI/Components/MasterData/ConfirmDeleteDialog.razor:41` — "vĩnh viễn"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:146` — "Tổng quan"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:160` — "Công cụ (Dev)"
- `src/frontend/ICare247_UI/Layout/NavMenu.razor:161` — "Tra cứu i18n"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:113` — "Thêm"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:114` — "Sửa"
- `src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor:115` — "Xóa"
- `src/frontend/ICare247_UI/Services/FormApiService.cs:133` — "HTTP {(int)response.StatusCode} — body rỗng"
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
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:22`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:36`
- `src/frontend/ICare247_UI/Pages/ScreenView.razor:44`
