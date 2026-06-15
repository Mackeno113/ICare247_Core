# 16 — Đồng bộ Config master → tenant (F1)

> **Trạng thái:** 📋 thiết kế CHỐT — CHƯA code (chờ duyệt 5 quyết định mở §10).
> **Bối cảnh:** mô hình **1 DB / 1 tenant** (mỗi khách có **Config DB + Data DB riêng**, ADR-018/ADR-025).
> Config (`Sys_Table` + `Ui_*`) thiết kế **1 lần ở master qua ConfigStudio WPF**, phải **đồng bộ xuống Config DB
> từng tenant**. Đây là **nền tảng F1** — tiền đề để engine-hóa mọi màn nghiệp vụ (F2, vd màn Công ty — ADR-024).

---

## 1. Nơi đặt master

- **Master = một Config DB "vàng" (canonical), cùng schema hệt Config DB tenant** — chính là DB ConfigStudio WPF
  trỏ vào để thiết kế. Sync đồng dạng (bảng→bảng theo mã), không fork schema, WPF không đổi.
- `ICare247_Master` (catalog) **giữ vai trò** đăng ký tenant + `Sys_MenuCatalog`; **không** nhồi toàn bộ `Ui_*`.

## 2. Phạm vi + thứ tự đồng bộ (theo phụ thuộc)

```
1. Sys_Table → 2. Sys_Column → 3. Sys_Relation
4. Sys_Lookup (+ items)        5. Sys_Resource (key + vi/en)
6. Ui_Form → 7. Ui_Tab → 8. Ui_Section → 9. Ui_Field → 10. Ui_Field_Lookup
11. Ui_Field rules (validation)
12. Ui_View → 13. Ui_View_Column → 14. Ui_View_Action
```
Sync **đúng thứ tự** để FK cha luôn có trước con.

## 3. Khóa đồng bộ — UPSERT theo MÃ, re-link FK theo MÃ

Bất biến: **KHÔNG bê `*_Id` identity** (mỗi DB khác). Thuật toán mỗi bảng:

1. Khóa tự nhiên = **mã (+ ngữ cảnh cha)**:

   | Bảng | Khóa match trên tenant |
   |---|---|
   | Sys_Table | `Table_Code` |
   | Sys_Column | (`Table_Id`←Table_Code) + `Column_Code` |
   | Ui_Form | `Form_Code` |
   | Ui_Tab | (`Form_Id`←Form_Code) + `Tab_Code` |
   | Ui_Section | (`Form_Id`) + `Section_Code` |
   | Ui_Field | (`Form_Id`/`Section_Id`) + `Field_Code` |
   | Ui_View | `View_Code` |

2. **UPSERT** theo khóa (chưa có → INSERT; có → UPDATE theo chính sách §4).
3. Sau mỗi bảng, dựng **map `Code → Id_tenant`** để bảng con resolve FK từ **chính DB tenant**.

## 4. Hệ thống vs tùy biến — tenant chỉnh KHÔNG mất khi re-sync

Mỗi bảng config thêm 2 cờ (tái dùng ý `LaHeThong` của menu — ADR-023):

| Cờ | Ý nghĩa | Khi sync |
|---|---|---|
| `LaHeThong = 1` | bản gốc từ master | UPSERT (ghi đè) — **trừ khi** `DaTuyBien=1` |
| `LaHeThong = 0` | tenant tự thêm | **không bao giờ đụng** |
| `DaTuyBien = 1` | bản hệ thống tenant đã sửa | sync **bỏ qua** (giữ bản tenant) |

Pha đầu: bảo vệ **mức dòng (row-level)**. Mức **field-level** (khóa từng thuộc tính) → pha sau.

## 5. Xóa / ngừng (tombstone)

Master gỡ bản hệ thống → trên tenant đặt **`Is_Active = 0`** (ngừng), **KHÔNG hard-delete** (tránh vỡ tham
chiếu + giữ lịch sử). Bản tenant tự thêm: không đụng.

## 6. Kích hoạt + phiên bản

- **Provisioning tenant mới:** full-sync 1 lần (toàn §2).
- **Cập nhật về sau:** incremental — master có **version stamp** (hoặc theo `UpdatedAt/Ver` từng dòng); tenant lưu
  `LastSyncedVersion`; chỉ sync dòng đổi.
- **Tích hợp `ConfigCache` (ADR-014/CC-4):** sau sync → bump version cache tenant → tự invalidate.

## 7. Thay đổi schema (khi code)

- Thêm `LaHeThong` + `DaTuyBien` (+ tùy chọn `NguonVer`/`SyncedAt`) vào: `Sys_Table`, `Ui_Form/Tab/Section/Field`,
  `Ui_View*`, rule, `Sys_Resource`, `Sys_Lookup`.
- Bảng **log sync** (tenant · thời điểm · version · số dòng I/U/deactivate) — audit + dry-run.

## 8. An toàn

- **Một chiều** master→tenant; cấm ghi ngược.
- **Dry-run/preview**: trả diff trước khi áp.
- **Transaction theo tenant**, idempotent.
- Ghi **audit** mỗi lần sync.

## 9. Nơi chạy

- Backend: `IConfigSyncService` (Application) + impl (Infrastructure), đọc master → ghi Config DB tenant
  (`IDbConnectionFactory` tenant-aware).
- Trigger: hook provisioning + **action super admin "Cập nhật cấu hình từ master"** (gate
  `[RequirePermission]`/SUPERADMIN) + (tùy chọn) scheduled.
- WPF chỉ tác động **master**; không chạm Config DB tenant.

## 10. Quyết định mở (chốt trước khi code)

1. **Master ở đâu:** Config DB "vàng" canonical (khuyến nghị) / DB template riêng?
2. **Mức bảo vệ tùy biến:** row-level (`DaTuyBien` cả dòng — khuyến nghị) / field-level ngay?
3. **Master sửa dòng tenant đã tùy biến:** giữ bản tenant (khuyến nghị) / báo super admin review / ép ghi đè?
4. **Trigger:** provisioning + nút thủ công super admin (khuyến nghị) — có cần scheduled?
5. **Xóa:** `Is_Active=0` (khuyến nghị) / cho hard-delete bản hệ thống?

## 11. Liên quan

- **ADR-024** — màn nghiệp vụ chuẩn = engine-driven (F2 cưỡi lên F1); màn Công ty pivot khỏi bespoke.
- **ADR-025** — 1 DB/tenant (Config + Data) → cần F1.
- ADR-018 (DB-per-tenant), ADR-023 (master→tenant menu — pattern gốc), ADR-014 (ConfigCache).
- **Hoãn:** phân quyền dữ liệu (RLS đọc qua SQL View + `SESSION_CONTEXT`) — thiết kế sau.
