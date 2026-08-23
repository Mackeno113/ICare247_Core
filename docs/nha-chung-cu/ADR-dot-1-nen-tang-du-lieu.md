# QUYẾT ĐỊNH THIẾT KẾ — ĐỢT 1 · NỀN TẢNG DỮ LIỆU

| | |
|---|---|
| **Tài liệu** | Architecture Decision Record (ADR) |
| **Dự án** | Phần mềm Quản lý Nhà chung cư |
| **Phạm vi** | Nhóm A — Nền tảng dữ liệu (căn hộ, chủ sở hữu, diện tích sở hữu riêng) |
| **Ngày lập** | 22/08/2026 |
| **Trạng thái** | Đề xuất — chờ chốt với team kỹ thuật và BQL |
| **Liên quan** | Đặc tả nghiệp vụ §5 (M1-01), Wireframe đợt 1 (A-01, A-03) |

---

## MỤC LỤC

- [ADR-001 · Gộp hay tách "phần diện tích khác"](#adr-001--gộp-hay-tách-phần-diện-tích-khác)
- [ADR-002 · Chính sách import all-or-nothing](#adr-002--chính-sách-import-all-or-nothing)
- [Tóm tắt hành động](#tóm-tắt-hành-động)

---

# ADR-001 · Gộp hay tách "phần diện tích khác"

## 1. Bối cảnh

Màn hình A-01 (Danh mục căn hộ) hiển thị đồng thời 486 căn hộ và 6 phần diện tích khác (sàn thương mại, văn phòng, căn hộ lưu trú du lịch) trong **cùng một bảng**, chỉ phân biệt bằng nền màu nhạt.

Câu hỏi đặt ra: nên gộp chung hay tách thành hai module riêng biệt?

## 2. Vì sao đây là quyết định kiến trúc, không phải quyết định UI

Chọn gộp hay tách ở tầng giao diện thực chất là chọn **mô hình dữ liệu**. Nếu tách UI, sớm muộn team sẽ tách luôn bảng `Unit` thành `Apartment` và `CommercialSpace`. Từ đó mọi truy vấn tính phiếu phải `UNION` hai bảng — và mỗi chỗ quên `UNION` là một lần tính sai kết quả hội nghị.

## 3. Lập luận cho việc TÁCH

| Lý do | Sức nặng | Ghi chú |
|---|---|---|
| Nghiệp vụ khác nhau thật: sàn TM có hợp đồng thuê, kỳ hạn, đơn giá/m²; căn hộ có cư dân, phương tiện, phí quản lý | **Mạnh** | Đây là lập luận duy nhất thực sự có trọng lượng |
| Người dùng khác nhau: bộ phận cho thuê ≠ bộ phận vận hành cư dân | Trung bình | Giải được bằng phân quyền và bộ lọc |
| Bảng gộp có nhiều cột `NULL` cho nhóm này hoặc nhóm kia | Yếu | Giải bằng bảng mở rộng |
| Số lượng chênh lệch lớn: 486 căn vs 6 phần diện tích | Yếu | Lọc là đủ |

## 4. Lập luận cho việc GỘP

Điểm quyết định nằm ở **định nghĩa pháp lý**: chủ sở hữu nhà chung cư bao gồm cả chủ sở hữu phần diện tích khác không phải căn hộ (Luật Nhà ở 2023, Điều 2 khoản 19; Quy chế TT 05, Điều 41).

Với engine tính phiếu, sàn thương mại 1.840 m² và căn hộ 78 m² là **cùng một loại thực thể** — đều là "một khối diện tích sở hữu riêng có người đại diện". Chúng chỉ khác nhau ở nghiệp vụ ngoại vi.

### Con số minh họa (tòa S2.03)

| Chỉ tiêu | Giá trị |
|---|---|
| Tổng diện tích sở hữu riêng | 41.280,50 m² |
| Phần diện tích khác (6 đơn vị) | 3.610,00 m² |
| **Tỷ trọng phiếu** | **8,7%** |
| Quy đổi tương đương | ~46 căn hộ trung bình |

Bỏ sót nhóm này là bỏ sót gần một tầng rưỡi trong kết quả biểu quyết.

## 5. Quyết định

> **Một bảng `Unit` duy nhất làm nguồn chân lý cho trọng số phiếu, kèm bảng mở rộng theo loại. UI được phép tách tab, nhưng mọi màn hình liên quan hội nghị luôn hiển thị đủ toàn bộ đơn vị.**

### Mô hình dữ liệu

```
Unit  (NGUỒN CHÂN LÝ cho trọng số phiếu)
 ├─ unit_code
 ├─ unit_type          [APARTMENT | COMMERCIAL | OFFICE | TOURIST]
 ├─ private_area_sqm   DECIMAL(10,2)
 ├─ handover_status    [DELIVERED | NOT_DELIVERED | DEVELOPER_RETAINED]
 ├─ handover_date
 └─ representative_id

UnitResidential   →  1-1 với Unit khi unit_type = APARTMENT
   cư dân, phương tiện, phí quản lý, tạm trú

UnitCommercial    →  1-1 với Unit khi unit_type in (COMMERCIAL, OFFICE)
   hợp đồng thuê, đơn giá, kỳ hạn, bên thuê
```

### Nguyên tắc UI

- Bộ phận cho thuê có màn hình riêng với bộ lọc mặc định — không ép họ nhìn 486 căn hộ.
- **Màn hình A-01 và mọi màn hình liên quan hội nghị luôn hiển thị đủ 492 đơn vị.** Không cho phép bộ lọc mặc định ẩn nhóm nào.
- Phần diện tích khác phân biệt bằng nền màu nhạt, không tách bảng.

## 6. Ràng buộc kỹ thuật bắt buộc

| # | Ràng buộc |
|---|---|
| K1 | Truy vấn tính phiếu **chỉ được đọc từ `Unit`**, cấm join sang bảng mở rộng |
| K2 | Ô thống kê "Tổng diện tích sở hữu riêng" ở A-01 tính trên toàn bộ `Unit`, **không phụ thuộc bộ lọc đang bật** |
| K3 | Test case bắt buộc: hội nghị có 1 sàn TM chiếm >10% tổng m² → kết quả phải khác rõ rệt so với khi bỏ sót nó |
| K4 | Ô thống kê "CĐT giữ lại" hiển thị tổng m² kèm ghi chú "có quyền phiếu" — minh bạch tỷ trọng chủ đầu tư ngay từ danh mục |

## 7. Hệ quả

**Tích cực**
- Engine tính phiếu chỉ đọc một bảng, không có nguy cơ quên `UNION`
- Tổng diện tích sở hữu riêng là hằng số kiểm tra được (ràng buộc `DB1`)
- Minh bạch tỷ trọng phiếu của chủ đầu tư

**Tiêu cực cần chấp nhận**
- Bảng `Unit` có một số trường không áp dụng cho mọi loại
- Bộ phận cho thuê phải dùng bộ lọc thay vì có module hoàn toàn riêng

---

# ADR-002 · Chính sách import all-or-nothing

## 1. Bối cảnh

Màn hình A-03 (Nhập danh sách từ Excel) áp dụng chính sách: **phát hiện lỗi ở bất kỳ dòng nào thì không dòng nào được nhập**. Ví dụ minh họa: file 1.000 dòng có 7 lỗi → nút "Nhập dữ liệu" bị khóa.

Đây là ràng buộc gây tranh cãi. Phản đối của dev và của BQL sẽ giống nhau: *"tại sao 993 dòng đúng lại không được nhập?"* — câu hỏi hợp lý, cần trả lời bằng lập luận chứ không bằng thẩm quyền.

## 2. So sánh hai phương án

| Tiêu chí | All-or-nothing | Nhập một phần |
|---|---|---|
| Toàn vẹn tổng m² | Đảm bảo | Không đảm bảo |
| Trải nghiệm file 1.000 dòng có 7 lỗi | Khó chịu — sửa file, tải lại | Dễ chịu — nhập 993 dòng, sửa 7 dòng sau |
| Khả năng phát hiện lỗi sót | Cao — buộc soát trước | Thấp — dễ quên 7 dòng còn lại |
| Rollback khi phát hiện sai | Đơn giản | Phức tạp |
| Truy vết nguồn gốc dữ liệu | Rõ ràng theo lô | Phân mảnh |

## 3. Ba lập luận quyết định

### 3.1 Lỗi tổng diện tích là loại lỗi không tự lộ ra

Nếu nhập thiếu 7 căn, hệ thống vẫn chạy bình thường suốt nhiều tháng. Phí vẫn tính, thông báo vẫn gửi, không có báo lỗi nào.

Nó chỉ lộ ra vào **đúng ngày hội nghị**, khi có người cầm giấy chứng nhận đến hỏi tại sao căn của họ không có trong danh sách cử tri — thời điểm tệ nhất có thể, trước mặt toàn thể cư dân và đại diện UBND.

### 3.2 Import không phải thao tác hằng ngày

Import danh mục xảy ra khi:
- Onboarding tòa nhà mới
- Chủ đầu tư bàn giao đợt lớn
- Chuyển đổi từ hệ thống cũ

Tức là **vài lần trong vòng đời một dự án**. Tối ưu trải nghiệm cho thao tác hiếm bằng cách hy sinh toàn vẹn dữ liệu là đánh đổi sai chiều.

### 3.3 Chi phí sửa file thấp hơn chi phí đối soát sau

| Việc | Chi phí |
|---|---|
| Sửa 7 dòng lỗi có số dòng cụ thể trong Excel | ~5 phút |
| Phát hiện "hệ thống đang thiếu mấy căn nào" sau 3 tháng | Đối chiếu thủ công 1.000 dòng |

## 4. Quyết định

> **Giữ all-or-nothing cho import danh mục. Tách riêng luồng import bổ sung cho các trường không ảnh hưởng trọng số phiếu, luồng này được phép nhập một phần.**

### Phân loại hai luồng import

| Luồng | Trường được phép | Chính sách | Lý do |
|---|---|---|---|
| **Import danh mục** | mã căn, diện tích sở hữu riêng, loại đơn vị, trạng thái bàn giao, chủ sở hữu, người đại diện | **All-or-nothing** | Ảnh hưởng trực tiếp trọng số phiếu |
| **Import bổ sung** | số điện thoại, email, biển số xe, ghi chú | Nhập một phần, báo cáo dòng lỗi | Không đụng `private_area_sqm` |

Cách phân loại này giữ nguyên tắc ở chỗ cần, và trả lại sự linh hoạt ở chỗ không nguy hiểm — đủ để thuyết phục cả dev lẫn BQL.

## 5. Năm biện pháp giảm đau bắt buộc kèm theo

| # | Biện pháp | Mô tả |
|---|---|---|
| G1 | **Validate sớm** | Kiểm tra ngay khi chọn file, không bắt chờ upload hết rồi mới báo lỗi |
| G2 | **File lỗi tải về được** | Có cột ghi chú lỗi ngay cạnh dòng sai, mở Excel là sửa được luôn |
| G3 | **Chế độ thử nghiệm (dry run)** | Chạy kiểm tra không ghi dữ liệu, cho phép soát nhiều vòng |
| G4 | **Đối chiếu tổng trước khi nhập** | Chênh lệch tổng m² phải được xác nhận có chủ đích, kèm chứng từ |
| G5 | **Lưu file gốc** | Đính kèm bản ghi import để truy vết về sau |

> Không được triển khai all-or-nothing mà thiếu G1–G3. Nếu thiếu, chính sách này chỉ còn là rào cản, không phải cơ chế bảo vệ.

## 6. Danh mục lỗi phải bắt được

| Loại lỗi | Ví dụ | Mức |
|---|---|---|
| Diện tích ≤ 0 | `0,00` | Chặn |
| Trùng mã đơn vị | Dòng 47 trùng dòng 46 | Chặn |
| Đồng sở hữu thiếu người đại diện | 3 người, không chỉ định | Chặn |
| Thiếu mã đơn vị | ô trống | Chặn |
| Sai định dạng thập phân | `95,5` thay vì `95,50` | Chặn |
| Thiếu loại đơn vị | ô trống | Chặn |
| Ngày bàn giao trong tương lai | sau ngày hôm nay | Chặn |
| Chênh lệch tổng m² so với hệ thống | `+72,30` | Cảnh báo, cần xác nhận |

> Lỗi định dạng thập phân được bắt riêng vì đây là nguồn **sai số tích lũy** khi tính phiếu trên 500 căn.

## 7. Hệ quả

**Tích cực**
- Tổng diện tích sở hữu riêng luôn khớp chứng từ
- Rollback đơn giản: hủy nguyên lô
- Buộc người dùng soát dữ liệu trước khi đưa vào hệ thống

**Tiêu cực cần chấp nhận**
- Người dùng có thể phải tải lên nhiều lần
- Cần đầu tư thêm cho dry run và báo cáo lỗi chi tiết

---

# Tóm tắt hành động

| # | Việc | Người chịu trách nhiệm | Thời hạn |
|---|---|---|---|
| 1 | Chốt mô hình `Unit` + bảng mở rộng với team kỹ thuật | Kiến trúc sư hệ thống | Trước khi code Phase 1 |
| 2 | Viết test case K3 (sàn TM >10% tổng m²) | QA | Cùng sprint với engine tính phiếu |
| 3 | Thống nhất hai luồng import với BQL pilot | Product owner | Trước khi onboarding tòa đầu tiên |
| 4 | Triển khai G1–G3 cùng lúc với chính sách all-or-nothing | Dev team | Không tách sprint |
| 5 | Rà soát danh mục lỗi §6 với chuyên viên nhập liệu thực tế | BA | Trước khi chốt đặc tả A-03 |

---

*Tài liệu ghi nhận quyết định thiết kế phục vụ thảo luận nội bộ. Mọi quyết định trong tài liệu này có thể được xem xét lại khi có căn cứ mới, nhưng phải cập nhật kèm lý do thay đổi.*
