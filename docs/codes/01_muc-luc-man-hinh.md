# 01 · Mục lục màn hình (route → màn → tài liệu)

> Liệt kê mọi route web hiện có (`@page` trong `src/frontend/ICare247_UI`). Cột **Tài liệu** trỏ tới file
> luồng chi tiết; ⬜ = chưa viết, ✅ = đã có. Dùng [`templates/_TEMPLATE-man-hinh.md`](templates/_TEMPLATE-man-hinh.md) để bổ sung.

## Nhóm: Engine-driven (no-code) — chiếm đa số màn nghiệp vụ

| Route | Màn / Component | Loại | Code | Tài liệu |
|---|---|---|---|---|
| `/view/{ViewCode}` | ViewPage → DataView | **Lưới engine** (Grid/TreeList) từ `Ui_View` | [ViewPage.razor](../../src/frontend/ICare247_UI/Pages/View/ViewPage.razor) | ✅ [view-grid-engine](man-hinh/view-grid-engine.md) |
| `/form/{FormCode}` | FormRunner | **Form engine** chạy thử (validation runtime) | [FormRunner.razor](../../src/frontend/ICare247_UI/Pages/FormRunner.razor) | ⬜ |
| `/master/{FormCode}` | MasterDataListPage | Danh mục: list + popup Thêm/Sửa (engine) | [MasterDataListPage.razor](../../src/frontend/ICare247_UI/Pages/MasterData/MasterDataListPage.razor) | ⬜ |
| `/master/{FormCode}/edit[/{Id}]` | MasterDataTabPage | Danh mục: Thêm/Sửa dạng trang | [MasterDataTabPage.razor](../../src/frontend/ICare247_UI/Pages/MasterData/MasterDataTabPage.razor) | ⬜ |
| *(component)* | MasterDataForm | **Luồng GHI**: Thêm/Sửa 1 bản ghi (validate + INSERT/UPDATE + audit) | [MasterDataForm.razor](../../src/frontend/ICare247_UI/Components/MasterData/MasterDataForm.razor) | ✅ [masterdata-form](man-hinh/masterdata-form.md) |

> Component lõi dùng lại: [DataView.razor](../../src/frontend/ICare247_UI/Components/View/DataView.razor),
> [MasterDataForm.razor](../../src/frontend/ICare247_UI/Components/MasterData/MasterDataForm.razor),
> [FieldRenderer.razor](../../src/frontend/ICare247_UI/Components/FieldRenderer.razor) (+ `FieldRenderers/*`).

## Nhóm: Shell & điều hướng

| Route | Màn | Loại | Code | Tài liệu |
|---|---|---|---|---|
| `/` | Dashboard + Shell | Sau đăng nhập: shell + menu server-driven (Dashboard KPI placeholder) | [Dashboard.razor](../../src/frontend/ICare247_UI/Pages/Dashboard.razor) | ✅ [shell-navigation](man-hinh/shell-navigation.md) |
| `/m/{module}[/{screen}]` | ScreenView | Bộ khởi chạy module/màn (lưới thẻ + redirect) | [ScreenView.razor](../../src/frontend/ICare247_UI/Pages/ScreenView.razor) | ⬜ |
| *(layout)* | MainLayout + NavMenu | Sidebar + topbar + menu server-driven | [NavMenu.razor](../../src/frontend/ICare247_UI/Layout/NavMenu.razor) | ✅ [shell-navigation](man-hinh/shell-navigation.md) |

## Nhóm: Xác thực (Auth)

| Route | Màn | Loại | Code | Tài liệu |
|---|---|---|---|---|
| `/login` | Login | Đăng nhập JWT thật | [Login.razor](../../src/frontend/ICare247_UI/Pages/Auth/Login.razor) | ✅ [login](man-hinh/login.md) |
| `/forgot-password` | ForgotPassword | UI + stub | [ForgotPassword.razor](../../src/frontend/ICare247_UI/Pages/Auth/ForgotPassword.razor) | ⬜ |
| `/reset-password` | ResetPassword | UI + stub | [ResetPassword.razor](../../src/frontend/ICare247_UI/Pages/Auth/ResetPassword.razor) | ⬜ |

## Nhóm: Quản trị (Administration)

| Route | Màn | Loại | Code | Tài liệu |
|---|---|---|---|---|
| `/m/administration/menu` | MenuBuilderPage | Cấu hình cây `HT_ChucNang` | [MenuBuilderPage.razor](../../src/frontend/ICare247_UI/Pages/Admin/MenuBuilderPage.razor) | ⬜ |
| `/m/administration/permissions` | PermissionMatrixPage | Ma trận quyền (TreeList × 5 cờ) | [PermissionMatrixPage.razor](../../src/frontend/ICare247_UI/Pages/Admin/PermissionMatrixPage.razor) | ⬜ |
| `/m/administration/config-sync` | ConfigSyncPage | Đồng bộ config master→tenant | [ConfigSyncPage.razor](../../src/frontend/ICare247_UI/Pages/Admin/ConfigSyncPage.razor) | ⬜ |
| `/m/administration/cache` | CacheToolsPage | Xóa/flush cache cấu hình | [CacheToolsPage.razor](../../src/frontend/ICare247_UI/Pages/Admin/CacheToolsPage.razor) | ⬜ |

## Nhóm: Công cụ Dev

| Route | Màn | Loại | Code | Tài liệu |
|---|---|---|---|---|
| `/dev/forms` | Home | Liệt kê Form để chạy thử | [Home.razor](../../src/frontend/ICare247_UI/Pages/Home.razor) | ⬜ |
| `/dev/i18n` | I18nToolsPage | Tra cứu độ phủ i18n runtime | [I18nToolsPage.razor](../../src/frontend/ICare247_UI/Pages/Dev/I18nToolsPage.razor) | ⬜ |

---

### Thứ tự ưu tiên tài liệu hóa (đề xuất)
1. ✅ **view-grid-engine** (mẫu) — vì đa số màn nghiệp vụ đi qua đây.
2. **MasterDataForm save** (luồng ghi + validation + audit) — bổ trợ cho lưới.
3. **Login/Auth** (vào hệ thống) → **NavMenu** (menu server-driven) → các màn admin.

*Cập nhật: 2026-06-20 — bản khung đầu tiên.*
