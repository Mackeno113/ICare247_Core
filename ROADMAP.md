# ROADMAP — ICare247 ERP

> Lộ trình biến nền tảng ICare247 (form engine) thành **ERP nội bộ** vận hành được.
> Tech stack chốt: **.NET 9 + Blazor WASM + API + Dapper + Redis** (xem ADR-011).
> Mô hình: **Engine + Code tay** (xem `docs/spec/09_ERP_DIRECTION.md`).
> Cập nhật lần cuối: 2026-06-09

---

## Nguyên tắc xếp thứ tự

Thứ tự bám theo **luồng phụ thuộc dữ liệu**, không theo số thứ tự module:

```
Master data  →  Đối tượng nghiệp vụ  →  Giao dịch  →  Tồn kho/Công nợ  →  Báo cáo
(danh mục)      (nhân viên, NCC)        (thu mua,      (rollup từ          (tổng hợp
                                          bán hàng)      giao dịch)          mọi nguồn)
```

Lý do: không thể làm "Phiếu thu mua cafe" trước khi có danh mục Kho, Loại cafe, Nhà
cung cấp. Không thể làm Công nợ trước khi có giao dịch mua/bán. Không thể làm Báo cáo/
Dashboard trước khi có dữ liệu để tổng hợp.

**Ký hiệu đường đi:**
🟦 ENGINE (ConfigStudio + FormRunner) · 🟧 CODE TAY (component Blazor + CQRS) · 🟨 LAI

---

## PHASE 0 — Nền tảng UI & kỷ luật WASM (bắt buộc trước mọi module)

Đây là phần "đỡ" cho tất cả module sau. Không làm xong cái này thì mọi màn hình đều dở dang.

| Việc | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Xác minh DevExpress Blazor (cài + license) | — | ✅ Done (session 15) | Nâng cấp 24.2→25.2.3, theme blazing-berry |
| Thay tokens ERP (`tokens.css`) | 🟧 | ✅ Done (session 14) | `docs/design-system/tokens.css` + `app.css` |
| **Shell layout**: Sidebar 280/72px + Header 56px + Content | 🟧 | 🔴 Chưa làm | Cần làm trước khi deploy ERP thật |
| Sidebar menu tree (8 module + Dashboard + Hệ thống) | 🟧 | 🔴 Chưa làm | Theo cây menu ERP đã định |
| **Grid chuẩn** tái sử dụng (DxGrid + search/filter/sort/paging/column chooser) | 🟧 | ✅ Done (session 41) | `MasterDataGrid.razor` + `MasterDataGridConfig` |
| **Bootstrap endpoint pattern** + Dapper `QueryMultiple` | 🟧 | 🔴 Chưa làm | Kỷ luật WASM #1, #2 |
| **Client lookup cache** (`Sys_Lookup` load 1 lần/phiên) | 🟧 | ✅ Done (session 40) | `IConfigCache` facade CC-0→CC-2, ADR-014 |
| **Form card chuẩn** (PageHeader + sections + audit) cho danh mục < 10 trường | 🟦 | ✅ Done (nhiều session) | FormRunner + FieldRenderer đầy đủ renderer |
| Template **màn hình giao dịch master-detail** (4 khối) | 🟧 | 🔴 Chưa làm | Khuôn cho thu mua/bán hàng (Phase 4) |
| **Ui_View** — cấu hình hiển thị danh sách tách khỏi form sửa | 🟧 | 🟡 In Progress | ADR-015 chốt; VIEW-0 ✅; VIEW-1→4c pending |

> **Ui_View** là hạng mục mới bổ sung so với kế hoạch ban đầu — xem `TASKS.md` roadmap VIEW-0→VIEW-4c và `docs/spec/14_VIEW_CONFIG_SPEC.md`.

**Mốc hoàn thành Phase 0:** mở app thấy shell ERP đúng màu, có 1 grid mẫu chạy được với
paging server-side, và 1 form card mẫu render từ metadata.

> ⚠️ Grid chuẩn và form card đã xong; còn thiếu Shell ERP + Sidebar + Bootstrap endpoint pattern.

---

## PHASE 1 — Master data (chứng minh đường ENGINE)

Toàn bộ là danh mục đơn giản → khai báo qua ConfigStudio, FormRunner render. Rủi ro thấp,
học được pattern engine, và là dữ liệu nền cho mọi module sau.

> **Hạ tầng engine đã hoàn chỉnh** (backend CRUD generic, FormRunner, FieldRenderer đủ loại).
> Việc còn lại: khai báo các form nghiệp vụ cụ thể trong ConfigStudio + chạy migrations Ui_View.

| Danh mục | Đường | Trạng thái | Phục vụ module |
|---|---|---|---|
| Phòng ban | 🟦 | 🔴 Chưa khai báo form | Nhân sự, Lương |
| Chức vụ | 🟦 | 🔴 Chưa khai báo form | Nhân sự, Lương |
| Đơn vị tính | 🟦 | 🔴 Chưa khai báo form | Thu mua, Kho, Phân bón |
| Danh mục Kho (kho vật lý) | 🟦 | 🔴 Chưa khai báo form | Cafe, Hồ tiêu, Phân bón, Kho |
| Loại hàng (cafe/tiêu/phân bón) | 🟦 | 🔴 Chưa khai báo form | Thu mua, Bán hàng |
| Nhà cung cấp | 🟦 | 🔴 Chưa khai báo form | Thu mua, Công nợ |
| Khách hàng | 🟦 | 🔴 Chưa khai báo form | Bán hàng, Công nợ |

**Mốc:** tất cả danh mục nhập liệu được qua giao diện ERP, không viết code Razor riêng.

**Phụ thuộc chặn:** Ui_View (VIEW-1 migration) phải xong trước khi màn hình danh sách ERP dùng Ui_View.

---

## PHASE 2 — Nhân sự (ENGINE-leaning)

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Hồ sơ nhân viên | 🟨 | 🔴 Chưa làm | Phần lớn engine; FK lookup → Phòng ban/Chức vụ |
| Hợp đồng lao động | 🟨 | 🔴 Chưa làm | Form + ít logic (thời hạn, loại HĐ) |

Phụ thuộc: Phase 1 (Phòng ban, Chức vụ). Là dữ liệu nền cho Công lương.

---

## PHASE 3 — Công lương (CODE TAY — tận dụng AST engine)

Module tính toán đầu tiên. **Điểm tái sử dụng quan trọng:** công thức lương/phụ cấp/khấu
trừ có thể đẩy vào **AST/Grammar engine có sẵn** (đã có 25 hàm, 125 test) thay vì hardcode.

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Chấm công | 🟧 | 🔴 Chưa làm | Grid theo tháng × nhân viên; nhập/import công |
| Tạm ứng | 🟨 | 🔴 Chưa làm | Form đơn giản; ràng buộc ≤ lương (Val_Rule) |
| Bảng lương | 🟧 | 🔴 Chưa làm | Tính lương thực: công × đơn giá + phụ cấp − tạm ứng − BHXH; công thức qua AST |

Phụ thuộc: Phase 2 (nhân viên), Phase 1 (chức vụ → hệ số lương).

---

## PHASE 4 — Thu mua Cafe & Hồ tiêu (CODE TAY — master-detail)

Hai module gần giống nhau → **làm Cafe trước làm khuôn, Hồ tiêu tái sử dụng** ~80%.
Dùng template giao dịch 4 khối từ Phase 0.

Layout phiếu: `Thông tin phiếu → Chi tiết hàng hóa → Tổng hợp thanh toán → Lịch sử xử lý`

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Phiếu thu mua cafe | 🟧 | 🔴 Chưa làm | Master-detail; quy đổi độ ẩm/tạp chất; tính thành tiền |
| Hợp đồng cafe | 🟨 | 🔴 Chưa làm | Form; liên kết phiếu |
| Phiếu thu mua hồ tiêu | 🟧 | 🔴 Chưa làm | Tái sử dụng khuôn cafe |

Phụ thuộc: Phase 1 (Loại cafe/tiêu, NCC, Kho, ĐVT). **Đầu vào cho Kho và Công nợ phải trả.**

---

## PHASE 5 — Phân bón (CODE TAY)

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Nhập hàng phân bón | 🟧 | 🔴 Chưa làm | Tương tự phiếu thu mua → đầu vào Kho + Công nợ phải trả |
| Bán hàng phân bón | 🟧 | 🔴 Chưa làm | Chiều xuất → giảm Kho + Công nợ phải thu |

Phụ thuộc: Phase 1 (Loại phân bón, Kho, NCC, KH). Mở rộng logic kho sang chiều **xuất**.

---

## PHASE 6 — Kho (CODE TAY — rollup)

Logic nhập/xuất tồn được tích lũy dần qua Phase 4-5. Phase này dựng **màn hình Kho hợp nhất**.

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Tồn kho hiện tại | 🟧 | 🔴 Chưa làm | Tổng hợp nhập − xuất theo kho × mặt hàng |
| Thẻ kho / lịch sử xuất nhập | 🟧 | 🔴 Chưa làm | Truy vết từng mặt hàng |
| Định giá tồn (bình quân / FIFO) | 🟧 | 🔴 Chưa làm | Phục vụ giá vốn cho báo cáo |

Phụ thuộc: Phase 4, 5 (mọi giao dịch nhập/xuất).

---

## PHASE 7 — Công nợ (CODE TAY — rollup)

| Màn hình | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| Công nợ phải trả (NCC) | 🟧 | 🔴 Chưa làm | Từ phiếu thu mua + nhập phân bón |
| Công nợ phải thu (KH) | 🟧 | 🔴 Chưa làm | Từ bán hàng phân bón |
| Cấn trừ / thanh toán | 🟧 | 🔴 Chưa làm | Ghi nhận trả/thu, cập nhật số dư |

Phụ thuộc: Phase 4, 5 (giao dịch mua/bán).

---

## PHASE 8 — Dashboard & Báo cáo (CODE TAY — tổng hợp cuối)

Làm sau cùng vì cần dữ liệu từ mọi module.

| Thành phần | Đường | Trạng thái | Ghi chú |
|---|---|---|---|
| KPI Card: Doanh thu, Tồn kho, Công nợ, Nhân viên | 🟧 | 🔴 Chưa làm | Tổng hợp đa nguồn |
| Biểu đồ giá cafe / hồ tiêu | 🟧 | 🔴 Chưa làm | Theo thời gian |
| Biểu đồ thu mua theo tháng | 🟧 | 🔴 Chưa làm | Từ Phase 4 |
| Báo cáo nghiệp vụ (lương, tồn, công nợ) | 🟧 | 🔴 Chưa làm | Export Excel |

Phụ thuộc: gần như tất cả phase trước.

---

## Hạng mục kỹ thuật ngang (Horizontal concerns)

Các task này không thuộc 1 phase cụ thể nhưng là điều kiện chất lượng.

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| ConfigCache facade (ADR-014) | ✅ Done CC-0→CC-2 | CC-3 (permission) hoãn; CC-4 (scale-out) khi cần |
| Ui_View — cấu hình danh sách metadata-driven (ADR-015) | 🟡 VIEW-0 done | VIEW-1 (Codex migration) → VIEW-2→3 (Claude) → VIEW-4 (Codex WPF) |
| `ICare247.ApiClient` SDK tách client dùng chung | 🔴 Chưa làm | SDK-1→4 trong TASKS.md; làm khi có web app thật thứ 2 |
| BE-002 Integration tests | 🔴 Chưa làm | ValidationEngine + EventEngine + MetadataEngine |
| BE-004 Apply Design System tokens vào Blazor | 🔴 Chưa làm | tokens.css có sẵn, chưa wire vào `--dx-*` |
| NumericBox locale config từ system config | 🔴 Pending | CC-config-number-format; hiện hardcode locale per-field |

---

## Sơ đồ phụ thuộc tổng thể

```
        ┌──────────────────────────────────────────────┐
        │ PHASE 0  Nền tảng UI + kỷ luật WASM            │
        │ (Shell + Sidebar 🔴, Grid ✅, Cache ✅, Form ✅) │
        └───────────────────┬──────────────────────────┘
                            │
        ┌───────────────────▼──────────────────────────┐
        │ PHASE 1  Master data (Phòng ban, Kho, NCC...)  │ 🟦 🔴
        └───┬───────────────┬───────────────────┬───────┘
            │               │                   │
   ┌────────▼──────┐  ┌─────▼─────────┐   ┌─────▼──────────────┐
   │ PHASE 2 Nhân sự│  │ PHASE 4 Thu mua│   │ PHASE 5 Phân bón   │
   │  🟨 🔴        │  │ Cafe + Tiêu 🟧 │   │ Nhập + Bán 🟧      │
   └────────┬──────┘  │ 🔴            │   │ 🔴                 │
            │         └─────┬─────────┘   └─────┬──────────────┘
   ┌────────▼──────┐        └────────┬──────────┘
   │ PHASE 3 Lương  │                │
   │  🟧 🔴 (AST)  │     ┌──────────▼──────────┐
   └───────────────┘     │ PHASE 6 Kho 🟧 🔴   │
                         │ PHASE 7 Công nợ 🟧 🔴│
                         └──────────┬──────────┘
                                    │
                         ┌──────────▼──────────┐
                         │ PHASE 8 Dashboard +  │
                         │ Báo cáo 🟧 🔴        │
                         └─────────────────────┘
```

---

## Phân bổ Engine vs Code tay (tổng kết)

| Đường | Module/màn hình | Ước lượng |
|---|---|---|
| 🟦 ENGINE | Toàn bộ master data, phần lớn hồ sơ NV/hợp đồng | ~30-40% |
| 🟧 CODE TAY | Lương, thu mua, phân bón, kho, công nợ, dashboard, báo cáo | ~60-70% |

**Tái sử dụng then chốt:** AST engine cho công thức (lương, thành tiền, quy đổi); grid
chuẩn & form card từ Phase 0; template master-detail dùng lại cho cafe→tiêu→phân bón;
toàn bộ repo pattern + cache + auth dùng chung cả hai đường.

---

## Trạng thái nền tảng (tính đến 2026-06-09)

| Thành phần | Trạng thái |
|---|---|
| Backend .NET 9 (entities, repos, cache, controller) | ✅ Phase 1-6 done |
| Grammar V1 / AST Engine (parser, compiler, 25 functions, 125 tests) | ✅ Done |
| Validation Engine (rules, dependencies, topological sort) | ✅ Done |
| Event Engine (6 action handlers, UiDelta) | ✅ Done |
| API Infrastructure (middleware, JWT, OpenTelemetry) | ✅ Done |
| Form Management CRUD API | ✅ Done |
| ConfigCache facade (ADR-014) CC-0→CC-2 | ✅ Done |
| Master Data CRUD generic (backend + Blazor + WPF config) | ✅ Done (session 38-41) |
| ConfigStudio WPF (11 screens, 6 UI thật, Direct DB) | ✅ Done |
| Blazor FormRunner + FieldRenderer (8 loại renderer) | ✅ Done |
| DevExpress DxGrid tích hợp MasterDataGrid | ✅ Done (session 41) |
| Ui_View ADR-015 chốt thiết kế | ✅ VIEW-0 done |
| DB Migrations (000–030) | ✅ Canonical schema |
| Shell ERP + Sidebar navigation | 🔴 Chưa làm |
| Bootstrap endpoint pattern (QueryMultiple) | 🔴 Chưa làm |
| Module nghiệp vụ ERP (Phase 1-8) | 🔴 Chưa làm |

---

## Gợi ý nhịp triển khai

- **Phase 0 là điều kiện cần** — phần còn thiếu (Shell ERP + Sidebar) phải xong trước khi demo ERP với user.
- **Ui_View VIEW-1** (Codex migration) là unblock cho Phase 1 — danh sách ERP dùng Ui_View thay hard-code.
- **Phase 1 nên làm trọn** trước Phase 2-5 vì là dữ liệu nền chung.
- Phase 2-3 (HR + Lương) và Phase 4-5 (Thu mua + Phân bón) là **hai nhánh độc lập** — có
  thể làm song song nếu đủ người.
- Phase 6-7-8 **phải sau** vì là rollup/tổng hợp.
