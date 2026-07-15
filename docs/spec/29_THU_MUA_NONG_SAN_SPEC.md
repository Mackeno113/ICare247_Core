# 29 — Thu Mua Nông Sản Ký Gửi (`KD_` / `KT_`) — Business Core Spec

> Ngày lập: 2026-07-15 · Trạng thái: **Draft đã chốt định hướng** (4 quyết định §0.2)
> Nguồn trích xuất: `src/frontend/source_can_update/` — app WPF legacy (.NET Framework 4.8, DevExpress 25.2),
> vertical **Ngọc Chương** (đại lý thu mua – ký gửi cà phê, 63 ViewModel trong `CategoryNgocChuong`).
> Legacy chỉ đóng vai trò **spec nghiệp vụ + checklist tính năng** — KHÔNG port code, KHÔNG map cột 1-1.

---

## 0. Phạm vi & quyết định đã chốt

### 0.1 Phạm vi

Thiết kế lõi nghiệp vụ **ngành thu mua nông sản có ký gửi** cho nền ICare247 (Data DB per-tenant,
form/lưới dựng từ config, định khoản cấu hình). Cà phê Ngọc Chương là **tenant đầu tiên**;
tiêu/điều/lúa… dùng lại cùng schema chỉ bằng cấu hình.

### 0.2 Quyết định đã chốt (2026-07-15)

| # | Quyết định | Chọn |
|---|---|---|
| 1 | Tiền tố bảng | `KD_` (giao dịch kinh doanh) + `KT_` (bút toán kế toán) |
| 2 | Thứ tự làm | **Viết trọn spec trước, code sau** (tài liệu này) |
| 3 | Mức tổng quát | Generic ngành thu mua nông sản ký gửi — cà phê = tenant đầu |
| 4 | Engine định khoản | **Backend C#** — chạy trong cùng transaction lưu phiếu (khớp cơ chế ADR-029/spec 18 §5) |

### 0.3 Nguyên tắc kế thừa từ legacy (giữ ý tưởng, bỏ hiện thực)

- **GIỮ:** mô hình "1 phiếu nghiệp vụ → n bút toán theo loại giao dịch" (OrderMaster→VoucherDetails);
  quy tắc báo cáo đọc thuần từ bút toán (`docs rules accounting-reporting.md` của legacy);
  cây nghiệp vụ + tham số định khoản (note refactor 2026-04-19 legacy đã tự đề ra nhưng chưa làm).
- **BỎ:** nhồi nghĩa cột (§2), hardcode Nợ/Có trong ViewModel, lưu không transaction (đếm `StepSave`),
  SQL string-format, view/proc đặt tên theo ngày (`v_Transaction_29112020_MKN`).

---

## 1. Mô hình nghiệp vụ — 6 cụm

Ngữ cảnh: đại lý thu mua nông sản. Nông dân ("đối tác") mang hàng tới cân; có thể **bán ngay**
hoặc **ký gửi chờ giá**. Đại lý kiêm **sơ chế** (phơi/sấy/xay), **tín dụng nông hộ** (cho vay/ứng tiền)
và **bán ra** theo hợp đồng.

### ① Chu trình ký gửi – chốt giá (lõi đặc trưng)

```
Cân hàng nhập ──► Tách 2 phần ──► [Bán ngay]  → bút toán mua (Nợ kho / Có phải-trả)
 (bì, chất lượng,  │
  quy chuẩn,       └► [Ký gửi]   → quy đổi về HÀNG CHUẨN theo hệ số
  quy đổi)                        → treo TK ngoài bảng (002) — hàng giữ hộ
                                       │
                        Chốt giá ◄─────┘  khách quyết bán ở giá hiện tại
                        (Nợ tiền mặt*/Có phải-trả · xử lý cọc)
                                       │
                        Rút tiền / trả hàng chốt giá
                        (theo dõi: đã chốt, đã trả, cọc, cọc hoàn)
```
\* legacy: khi phiếu đánh dấu "đã nhận cọc trước khi dùng phần mềm" thì bỏ vế Nợ.

Thông số cân hàng (dùng chung mọi phiếu có cân): số bao, kg mang tới, **trừ bì**
(mặc định 200 gr/bao — phải thành **thông số cấu hình**, spec 27), **độ cà** (tỷ lệ hạt),
**độ tạp**, **% thủy phần** → ra **số lượng quy chuẩn**; nếu ký gửi thì nhân **hệ số quy đổi**
ra lượng hàng chuẩn (cà tươi → cà nhân).

### ② Mua hàng — 4 biến thể

| Biến thể | Legacy symbol | Đặc thù |
|---|---|---|
| Mua tươi/quả | `BUY_CF_CHERRIES` / `BUYFRESH` | Cân + quy đổi về nhân; phần không bán → ký gửi |
| Mua khô | `BUY_CF_DRY` / `BUYDRY` | Kèm đối trừ nợ cũ (nợ cũ / thanh toán / còn nợ) |
| Mua nhân | `BUY_CF_BEAN` / `BUYINDIRECT` | Mua trực tiếp hàng chuẩn |
| Mua non | `BUY_CF_YOUNG` / `SHORTSELLING` | Mua trước vụ: **tài sản thế chấp, chủ sở hữu, người chịu trách nhiệm, người ghi sổ**; hàng đã chốt chờ nhập treo TK 151 |

### ③ Sơ chế (mẻ)

Xuất **sấy** (`EXPORTDRYING` — lò sấy, ngày vào/ra lò, chi phí, lợi nhuận tạm tính) ·
xuất **phơi** (`EXPORTDRYING_P` — sân phơi, khu vực, người quản lý, ngày về) ·
nhập lại **nhân từ sấy** (`IMPORTBEAN`) / **khô từ phơi** (`IMPORTDRIED_P`) ·
**xay khô ra nhân** (`GET_CF_DRYING`, hệ số quy đổi). Mỗi công đoạn = phiếu chuyển kho + hao hụt.

### ④ Tín dụng nông hộ

- **Cho khách vay** (`LENDING` — Nợ 1283): lãi suất tháng, tài sản thế chấp, chủ sở hữu,
  người chịu trách nhiệm, người ghi sổ; thu gốc (`REC_LENDING`), thu lãi (`INTEREST_LENDINGMONEY`).
- **Nhận vay của khách** (`LOANMONEY` — Có 341): khách gửi tiền cho đại lý giữ/vay; trả gốc
  (`PAY_LOANMONEY` — Nợ 341), trả lãi (Nợ 635).
- Tiền vay/lãi có thể **đối trừ ngay trên phiếu mua/thanh toán** (legacy làm trong màn thanh toán mua hàng).

### ⑤ Bán ra

Hợp đồng kinh tế (`INFOR_ECONOMI`) → **hợp đồng giao hàng** (`DELIVERY_CONTRACTS`: cân lại tại
nơi bán lần 2; phân biệt **hàng bán** — Nợ 131/Có 511 + Nợ 632/Có kho — với **hàng gửi bán** — TK 157) ·
bán trực tiếp (`SELLDIRECT`) · **chốt giá bán** (`PRICE_CLOSE_SELL`) · ứng tiền (`ADVANCE_MONEY`,
có lãi suất) · nhận tiền hợp đồng (`RECEIVE_MONEY` — Nợ 1111/Có 131; quá hạn tính lãi Nợ 635/Có 131).

### ⑥ Báo cáo (proc-only)

Tồn kho · tồn ký gửi theo đối tác · công nợ NCC theo ngày · sổ vay chi tiết · sổ cà tươi ·
sổ sấy/phơi · người chịu trách nhiệm · công nợ bán hàng tổng hợp. Legacy chạy qua
`usp_Infor_*_NgocChuong_*_MKN`; chuẩn mới: `sp_BC_ThuMua_*` (§6).

---

## 2. Bệnh legacy phải tránh: nhồi nghĩa cột

Bằng chứng (từ comment trong chính legacy) — **cùng cột vật lý, mỗi màn một nghĩa**:

| Cột legacy | Mua tươi | Nhận ký gửi | Đem sấy | Thanh toán mua | Rút chốt giá |
|---|---|---|---|---|---|
| `SalePrice` | Số bao mang tới | % trừ thủy phần | % trừ thủy phần | Tiền khách cần vay chưa đưa | % trừ thủy phần |
| `NetPrice` | Số kg trừ | Tổng ký bị trừ | Tổng ký bị trừ | Thanh toán tiền khách vay | Tổng ký bị trừ |
| `Package` | Hệ số quy nhân | Độ cà | Độ cà | Thanh toán nợ 331 | Độ cà |
| `RateDiscount` | Tiền vay | Tiền vay | Lợi nhuận tạm tính | — | Số khách bán |
| `Specifications` | SL đem tới | SL thu được | SL ra lò | **Số tiền còn nợ** | SL đem tới |
| `ShipFee` | — | Số kg khô xay | Đơn giá mua | — | Tiền cọc |

Và chiều ngược: cùng khái niệm nằm cột khác nhau ("hệ số quy đổi" = `Package` hoặc
`InventoryItemSale`; "% trừ thủy phần" = `SalePrice` hoặc `CodFee`).

**Luật thiết kế rút ra:** (1) mỗi khái niệm = **1 tên cột tiếng Việt duy nhất**, mọi màn dùng chung;
(2) nghiệp vụ nào cần thêm trường thì thêm **bảng vệ tinh**, không mượn cột trống của bảng chung.

---

## 3. Từ điển khái niệm chuẩn (glossary)

Tên cột mới (tiếng Việt, ADR-022). Cột "Legacy" ghi mọi cột cũ từng chứa khái niệm đó.

### 3.1 Nhóm cân hàng & chất lượng

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `BienSoXe` | Biển số xe chở hàng | TransportersName |
| `SoBao` | Số bao mang tới | SalePrice (màn mua tươi) |
| `SoLuongMangToi` | Kg cân tại chỗ | Specifications |
| `TrongLuongTruBi` | Kg trừ bì bao (SoBao × gram-cấu-hình) | — (tính tay) |
| `DoCa` | Độ cà (tỷ lệ hạt) | Package |
| `DoTap` | Độ tạp chất | Container |
| `PhanTramThuyPhan` | % trừ thủy phần | SalePrice, CodFee, ShipFee (giao hàng) |
| `TongKgTru` | Tổng kg bị trừ (bì + tạp + thủy phần) | NetPrice, TransferAmount |
| `SoLuongQuyChuan` | Kg quy chuẩn sau trừ | StandardQuantity, CustomerShipFee (giao hàng) |
| `HeSoQuyDoi` | Hệ số quy về hàng chuẩn | Package (mua tươi), InventoryItemSale |
| `SoLuongQuyDoi` | Kg hàng chuẩn quy đổi | DepositQuantity |

### 3.2 Nhóm mua / bán

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `SoLuong` | SL giao dịch (mua/bán) | OrderQuantity, Quantity, UsedPoints |
| `DonGia` | Đơn giá | OrderPrice, UnitPrice, UnitPriceUSD |
| `ThanhTien` | Thành tiền | OrderAmount, Amount, AmountUSD, SumAmount |
| `TienDaThanhToan` | Đã thanh toán | RateEvent, CashAmount, PrePayment (SL) |
| `TienConLai` | Còn phải trả | InstallmentAmount, DiscountAmount (mua khô) |
| `NoCu` | Nợ cũ đối trừ | SalePrice (mua khô) |
| `ThucNhan` | Tiền thực nhận | VatRate (thanh toán) |

### 3.3 Nhóm ký gửi

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `SoLuongKyGui` | Kg gửi vào | DepositQuantity |
| `SoLuongKyGuiTon` | Kg còn gửi (derive từ sổ) | RateDiscountMaster, DeclaredFee, UsedPointsAmount |
| `SoLuongDaBan` | Kg gửi đã bán/chốt | RateVat, DiscountMaster |

### 3.4 Nhóm chốt giá

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `SoLuongChot` | Kg chốt giá | CodFee, RateDiscountMaster, ShipFee (BuyCoffeeFilling) |
| `GiaChot` | Giá chốt | OrderPrice (màn chốt) |
| `SoLuongDaTraChot` | Kg đã trả sau chốt | DiscountMaster, OrderQuantity (màn rút), DeclaredFee |
| `TienCoc` | Tiền cọc | ShipFee (màn rút) |
| `TienCocDaHoan` | Cọc đã hoàn | DeclaredFee (màn rút) |

### 3.5 Nhóm tín dụng (vay / ứng)

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `TienVay` | Gốc vay / tiền ứng | RateDiscount, UnitPrice (ứng tiền) |
| `LaiSuatThang` | % lãi suất | DiscountAmount, VatRate/VatAmount (ứng) |
| `TienGocDaTra` | Gốc đã trả | VatAmount, CreditAmount |
| `TienLai` | Lãi phát sinh/đã thu | OrderAmount (màn thanh toán vay) |
| `TaiSanTheChap` | Tài sản thế chấp | HBL |
| `ChuSoHuu` | Chủ sở hữu tài sản | MBL |
| `NguoiGhiSo_Id` | Người ghi sổ | SupplierName |
| `NguoiChiuTrachNhiem_Id` | Người chịu trách nhiệm | AgencyName |

### 3.6 Nhóm sơ chế

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `LoaiSoChe` | Phơi / Sấy / Xay | (suy từ symbol) |
| `DiaDiemSoChe` | Lò sấy / sân phơi | ContactPerson, ImportStock |
| `KhuVuc` | Khu vực sân phơi | SupplierName (màn phơi) |
| `NguoiQuanLy_Id` | Người quản lý mẻ | AgencyName (màn phơi) |
| `NgayVao` / `NgayRa` | Ngày vào / ra lò (về sân) | ETD, DeadlinePayment |
| `SoLuongDauVao` | Kg đem sơ chế | RateDiscountMaster |
| `SoLuongDauRa` | Kg thu được | Specifications |
| `ChiPhiSoChe` | Chi phí mẻ | DepositQuantity (màn sấy) |
| `LoiNhuanTamTinh` | Lợi nhuận tạm tính | RateDiscount (màn sấy) |
| `DaHoanTat` | Mẻ xong | IsLock |

### 3.7 Nhóm hợp đồng & giao hàng

| Cột chuẩn | Nghĩa | Legacy |
|---|---|---|
| `SoHopDong` | Số hợp đồng | OrderNumber (ECONOMI) |
| `NgayGiaoHang` | Ngày giao | DeliveryDate |
| `LaHangGuiBan` | Hàng gửi bán (chưa chuyển sở hữu, TK 157) | IsUsing (vs IsLock = hàng bán) |
| `SoLuongTaiNoiBan` | Kg cân lần 2 tại nơi bán | Specifications (bộ cân hàng thứ 2) |

> Cân 2 lần trên 1 phiếu giao hàng (tại kho + tại nơi bán) = **2 dòng `KD_CanHang`**
> phân biệt bằng `VaiTro` (`TaiKho` / `TaiNoiBan`) — không nhân đôi cột như legacy.

---

## 4. Schema `KD_` / `KT_` (Data DB per-tenant)

Mọi bảng theo ADR-022: khối cột auto (`Id`, `CreatedBy/CreatedAt/UpdatedBy/UpdatedAt`,
`IsDeleted`, `Ver`…), cột nghiệp vụ tiếng Việt, audit tường minh. Cây dùng cơ chế ADR-027
(`Cha_Id`, `ThuTu`, `ThuTuCay`, `DuongDanCay`). FK khai báo vật lý + `Sys_Relation` (không đoán theo tên).

### 4.1 Cấu hình nghiệp vụ (cây + định khoản)

**`KD_LoaiGiaoDich`** 🌳 — cây loại giao dịch (thay `TransactionType` + enum `eVoucherType_NCGL`)

| Cột | Ghi chú |
|---|---|
| `Ma` | Symbol ổn định (VD `MUA_TUOI`, `KY_GUI`, `CHOT_GIA`) — dùng trong code engine, KHÔNG dịch |
| `Ten` | i18n theo spec 10 |
| `Cha_Id`, `ThuTu`, `ThuTuCay`, `DuongDanCay` | Cây ADR-027; node nhóm không bắt buộc có định khoản |
| `KyHieuPhieu` | Prefix sinh số phiếu (VD `MT` → `MT-2607-0001`) |
| `CoCanHang`, `CoKyGui`, `CoChotGia`, `CoKhoanVay`, `CoSoChe`, `CoHopDong` | Cờ bật bảng vệ tinh + section form tương ứng |
| `KhoMacDinh_Id`, `LoaiPhieu` | Tuỳ chọn hỗ trợ UI |

**`KD_LoaiGiaoDich_DinhKhoan`** — mẫu bút toán per node (thay hardcode `DebitAccount = "1561"` trong VM)

| Cột | Ghi chú |
|---|---|
| `LoaiGiaoDich_Id` | FK node cây |
| `MaButToan` | Định danh dòng bút toán (thay `DetailType`: `NHAP_KHO_MUA`, `KY_GUI`, `QUY_DOI`…) |
| `TaiKhoanNo_Id`, `TaiKhoanCo_Id` | FK `DM_TaiKhoan`; một vế được phép NULL (TK ngoài bảng) |
| `NguonSoLuong`, `NguonDonGia`, `NguonSoTien` | Biểu thức AST Grammar V1 trên payload phiếu (VD `CanHang.SoLuongQuyDoi`, `ChiTiet.SoLuong * ChiTiet.DonGia`) |
| `DieuKienApDung` | Biểu thức AST bool — bỏ qua dòng nếu false (thay if rải trong VM, VD "đã nhận cọc trước" thì bỏ vế Nợ) |
| `ThuTu` | Thứ tự sinh bút toán |

> Cấu hình cây + định khoản làm trên **ConfigStudio WPF** (quy tắc "config qua WPF, không SQL"),
> sync master→tenant qua ConfigSync (spec 16) nếu là mẫu ngành; tenant được override.

### 4.2 Chứng từ

**`KD_GiaoDich`** — master mọi phiếu (thay `OrderMaster` + `VoucherMaster`)

`LoaiGiaoDich_Id` · `SoPhieu` (sinh theo `KyHieuPhieu`, cấu hình reset theo tháng) · `NgayGiaoDich` ·
`DoiTac_Id` · `CongTy_Id` (chi nhánh) · `KhoNhap_Id` · `KhoXuat_Id` · `NhanVien_Id` ·
`PhieuGoc_Id` (phiếu phát sinh từ phiếu nào — thay `MasterParentKey`/`GroupKey`) ·
`HopDong_Id` (nullable) · `TrangThai` (Nhap → DaGhiSo → DaHuy) · `GhiChu`.

**`KD_GiaoDich_ChiTiet`** — dòng hàng (thay `OrderDetail`)

`GiaoDich_Id` · `HangHoa_Id` · `DonViTinh_Id` · `SoLuong` · `DonGia` · `ThanhTien` · `ThuTu` · `GhiChu`.

### 4.3 Bảng vệ tinh (bật theo cờ trên `KD_LoaiGiaoDich`)

| Bảng | Quan hệ | Cột chính (từ glossary §3) |
|---|---|---|
| **`KD_CanHang`** | n-1 `GiaoDich_ChiTiet` (`VaiTro`: TaiKho/TaiNoiBan) | BienSoXe, SoBao, SoLuongMangToi, TrongLuongTruBi, DoCa, DoTap, PhanTramThuyPhan, TongKgTru, SoLuongQuyChuan, HeSoQuyDoi, SoLuongQuyDoi |
| **`KD_KyGui`** | sổ ledger theo `DoiTac_Id` + `HangHoa_Id` (hàng chuẩn) | GiaoDichChiTiet_Id, `LoaiBienDong` (NhapGui / ChotGia / TraHang / BanGui / DieuChinh), `SoLuong` (dấu +/-), NgayPhatSinh. **Tồn ký gửi = SUM** — không lưu số dư đè lên nhau như legacy |
| **`KD_ChotGia`** | 1-1 `GiaoDich` | SoLuongChot, GiaChot, ThanhTien, TienCoc, TienCocDaHoan, SoLuongDaTraChot, TrangThai |
| **`KD_KhoanVay`** | sổ vay per khoản | DoiTac_Id, `Chieu` (ChoVay/DiVay), TienGoc, LaiSuatThang, NgayVay, TaiSanTheChap, ChuSoHuu, NguoiGhiSo_Id, NguoiChiuTrachNhiem_Id, TrangThai |
| **`KD_KhoanVay_PhatSinh`** | n-1 `KD_KhoanVay` | `LoaiPhatSinh` (VayThem/TraGoc/TraLai), SoTien, NgayPhatSinh, GiaoDich_Id |
| **`KD_SoChe`** | 1-1 `GiaoDich` xuất; link phiếu nhập | `LoaiSoChe` (Phoi/Say/Xay), DiaDiemSoChe, KhuVuc, NguoiQuanLy_Id, NgayVao, NgayRa, SoLuongDauVao, SoLuongDauRa, ChiPhiSoChe, LoiNhuanTamTinh, DaHoanTat, GiaoDichNhap_Id |
| **`KD_HopDong`** | độc lập; `KD_GiaoDich.HopDong_Id` trỏ về | SoHopDong, DoiTac_Id, `LoaiHopDong` (KinhTe/GiaoHang), NgayKy, HangHoa_Id, TongSoLuong, DonGia, TrangThai |

### 4.4 Bút toán & tài khoản

**`KT_ButToan`** — thay `VoucherDetails` (mỗi dòng 1 bút toán, CHỈ engine ghi)

`GiaoDich_Id` · `GiaoDichChiTiet_Id` (nullable) · `MaButToan` (từ mẫu định khoản — thay
`DetailType` + `FilterType`) · `TaiKhoanNo_Id` · `TaiKhoanCo_Id` · `HangHoa_Id` · `SoLuong` ·
`DonGia` · `SoTien` · `Kho_Id` · `DoiTac_Id` · `NgayHachToan` · `GhiChu`.

**`DM_TaiKhoan`** 🌳 — hệ thống tài khoản (cây), có `LoaiTaiKhoan` phân biệt **TK ngoài bảng**
(002 hàng ký gửi) với TK trong bảng; seed theo TT133/TT200 rút gọn, tenant sửa được.

### 4.5 Danh mục bổ trợ

- **`DM_HangHoa`** 🌳 — hàng hóa (cà tươi/khô/nhân là node cùng cây). Legacy suy "hàng chuẩn"
  bằng cách nhảy 2 cấp `ItemsGroupKey` — **bỏ**; thay bằng:
- **`DM_HangHoa_QuyDoi`** — `HangHoaTu_Id`, `HangHoaVe_Id` (hàng chuẩn), `HeSoMacDinh`
  (cho phép override per phiếu tại `KD_CanHang.HeSoQuyDoi`).
- `DM_DoiTac`, `DM_Kho` — dùng/mở rộng danh mục nền tảng hiện có.
- Thông số hệ thống (spec 27): `ThuMua.GramTruMoiBao` (=200), `ThuMua.PhuongPhapGiaXuatKho`
  (FiFo / BinhQuan — legacy có enum `eCalculateOutStock`), `ThuMua.SoThapPhanQuyChuan` (=2).

---

## 5. Engine sinh bút toán (backend C#)

Vị trí: trong pipeline save của phiếu `KD_GiaoDich`, **cùng một DB transaction** với lưu
master + chi tiết + vệ tinh (khớp mô hình transaction ADR-029/spec 18 §5 — validate hook →
lưu phiếu → **posting engine** → after-save hook, commit chung, fail đâu rollback hết —
xóa hẳn kiểu đếm `StepSave` của legacy).

Thuật toán:

1. Load cây + mẫu định khoản của `LoaiGiaoDich_Id` (qua ConfigCache, invalidation theo version).
2. Duyệt `KD_LoaiGiaoDich_DinhKhoan` theo `ThuTu`; dòng nào `DieuKienApDung` = false → bỏ.
3. Evaluate `NguonSoLuong/NguonDonGia/NguonSoTien` bằng AST engine trên payload phiếu
   (master + chi tiết + vệ tinh).
4. Ghi `KT_ButToan` (delete-insert theo `GiaoDich_Id` khi sửa phiếu — bút toán là dẫn xuất,
   không sửa tay).
5. Phiếu có `CoKyGui` → ghi dòng `KD_KyGui` tương ứng trong cùng transaction.

Bất biến kiểm tra được (unit test): tổng Nợ = tổng Có per phiếu (trừ dòng TK ngoài bảng);
tồn ký gửi không âm; sửa/hủy phiếu tái sinh đúng bút toán.

---

## 6. Báo cáo

Giữ nguyên tắc legacy (đúng): **proc-only**, số liệu từ `KT_ButToan`, cột danh mục chỉ để
lọc/nhóm/truy vết. Chuẩn tên `sp_BC_ThuMua_<Ten>`:

| Proc | Thay cho legacy |
|---|---|
| `sp_BC_ThuMua_TonKho` | `usp_Infor_Inventory_NgocChuong_*` |
| `sp_BC_ThuMua_TonKyGui` | báo cáo khách gửi (đọc `KD_KyGui`) |
| `sp_BC_ThuMua_CongNoNCC` | `usp_Infor_DebitSupplier_Date_NgocChuong_*` |
| `sp_BC_ThuMua_SoVay` | `usp_Infor_*LoanMoney*` |
| `sp_BC_ThuMua_SoSoChe` | sổ sấy/phơi |
| `sp_BC_ThuMua_CongNoBanHang` | `GeneralSalesDebt` |
| `sp_BC_ThuMua_NguoiChiuTrachNhiem` | `ResponsiblePerson` |

Hiển thị bằng Ui_View + bộ lọc ADR-030; xuất in phiếu (phiếu gửi cà, phiếu chốt giá — legacy
in Excel template) chuyển sang **Doc Template spec 28** gắn qua `Ui_View_Action`.

## 7. Map màn hình legacy → màn mới

Toàn bộ dựng bằng Form Engine + Ui_View; section vệ tinh (cân hàng, ký gửi, vay…) bật theo cờ
`KD_LoaiGiaoDich`. Không viết màn bespoke trừ khi engine thiếu control.

| Legacy (Exchanges) | Màn mới | Ghi chú |
|---|---|---|
| ucBuyFreshCoffee | Mua hàng tươi | + section Cân hàng + Ký gửi |
| ucBuyDryCoffee | Mua hàng khô | + đối trừ nợ cũ |
| ucBuyCoffeeFilling / GetCoffeeBeans | Nhập ký gửi hàng chuẩn | + Cân hàng |
| ucBuyYoungCoffee | Mua non | + section Thế chấp |
| ucDryingFreshCoffee / P | Xuất sấy / Xuất phơi | + section Sơ chế |
| ucGetCoffeeBeans/Drying (nhập lại) | Nhập từ sơ chế / Xay | link mẻ `KD_SoChe` |
| ucPriceClosing | Chốt giá ký gửi | + section Chốt giá |
| ucWithdrawalClosingPrice (+Payment) | Rút tiền / trả hàng chốt giá | màn phức tạp nhất — làm sau cùng cụm |
| ucLendingMoney / LoanMoney (+Payment) | Cho vay / Nhận vay (+ thu/trả) | sổ `KD_KhoanVay` |
| **(SellExchanges)** | | |
| ucEconomiContracts / SellContracts | Hợp đồng bán | `KD_HopDong` |
| ucDeliveryContracts | Giao hàng theo hợp đồng | 2 dòng Cân hàng (kho/nơi bán) |
| ucDirectSale | Bán trực tiếp | |
| ucPriceCloseSell / AdvanceMoney / ReceiveMoneyContracts | Chốt giá bán / Ứng tiền / Nhận tiền HĐ | |

## 8. Lộ trình triển khai (sau khi spec này được duyệt)

| Đợt | Nội dung | Phụ thuộc |
|---|---|---|
| Đ1 | Migration `KD_`/`KT_`/`DM_TaiKhoan`/`DM_HangHoa_QuyDoi` + màn ConfigStudio cấu hình cây nghiệp vụ & định khoản + seed mẫu cà phê | ADR-027 (cơ chế cây — CHƯA code), spec 27 (thông số) |
| Đ2 | Posting engine C# + unit test bất biến | Đ1, cơ chế transaction ADR-029 |
| Đ3 | Cụm mua + cân hàng + ký gửi (4 màn) | Đ2 |
| Đ4 | Chốt giá + rút/trả + thanh toán | Đ3 |
| Đ5 | Sơ chế + tín dụng | Đ3 |
| Đ6 | Bán/hợp đồng + báo cáo `sp_BC_ThuMua_*` + in phiếu Doc Template | Đ4 |

## 9. Điểm cần chốt thêm (trước Đ1)

1. **Phương pháp giá xuất kho** mặc định: FiFo hay bình quân gia quyền? (legacy hỗ trợ cả 2)
2. **Lãi vay:** lãi đơn theo tháng, tính ngày lẻ ra sao? (legacy chỉ lưu %, tính tay)
3. **Số dư đầu kỳ** khi go-live tenant Ngọc Chương: nhập tay qua màn Opening hay import từ DB cũ?
4. **Đơn vị tính:** cố định kg hay đa DVT + quy đổi DVT? (legacy thuần kg)
5. ADR-027 (cây) chưa code — Đ1 kéo theo việc code cơ chế cây generic trước.

---

## 10. Đánh giá năng lực nền tảng cho 3 kiểu màn (2026-07-15 — đã verify trên code)

Vertical này cần đúng 3 kiểu màn. Đối chiếu với nền tảng hiện tại:

| Kiểu màn | Mức đáp ứng | Chi tiết |
|---|---|---|
| **① Danh mục** | ✅ Đủ | Ui_Form + Ui_View + lookup/cây + validation + i18n — đã chạy thật |
| **② Master list** (nút thêm/sửa/xóa/in, lọc ngày bắt đầu/kết thúc, Xem dữ liệu, 1 hoặc 2 lưới) | ⚠️ ~80% | ĐÃ có: `Ui_View_Action` (toolbar/row + confirm + Require_Selection), panel lọc trái `Ui_View_Filter` (param DATE + nút Tìm, spec 14 §9, `ViewPage.razor` đã render), in Doc Template theo dòng chọn. **THIẾU: 2 lưới master-detail** — `Detail_View_Id` mới đặt chỗ ở schema (dữ liệu NULL, không có runtime Blazor render) → spec 14 §11 |
| **③ Form chứng từ** (1 đơn / 1 khách / n dòng hàng / nhiều giá, sự kiện dày) | ❌ Gap lớn nhất | ĐÃ có nền: FormRunner với vòng đời FIELD_CHANGED → UiDelta (nhưng chỉ field đơn của form 1 bản ghi); validation AST; lookup/cascade. THIẾU 4 mảnh: (1) renderer **lưới chi tiết editable** (bộ renderer hiện chỉ 10 control field đơn); (2) **công thức + sự kiện mức dòng** (ThanhTien=SoLuong×DonGia, tổng footer, lấy giá theo chính sách khi chọn khách+hàng); (3) **save master + n detail 1 transaction** (API hiện save theo 1 bảng); (4) **màn cấu hình ConfigStudio WPF** cho form master-detail |

Khoảng trống ③ **không phải đặc thù vertical** — là năng lực nền tảng "form chứng từ" mà mọi
module ERP cần → thiết kế thành capability chung: **spec `30_FORM_CHUNG_TU_SPEC.md`**.
Phát hiện kiến trúc quan trọng: `AstParser`/`AstCompiler` nằm ở `ICare247.Domain` thuần C#
không phụ thuộc hạ tầng → **Blazor WASM tham chiếu trực tiếp được** ⇒ công thức dòng chạy
client-side bằng chính AST Grammar V1, không JS, không round-trip mỗi phím gõ.
