# ĐẶC TẢ YÊU CẦU NGHIỆP VỤ
## Phần mềm Quản lý Nhà chung cư — Trọng tâm Module Hội nghị Nhà chung cư

| | |
|---|---|
| **Phiên bản** | 0.1 (Draft) |
| **Ngày lập** | 22/08/2026 |
| **Phạm vi** | Quản lý vận hành nhà chung cư + tổ chức Hội nghị nhà chung cư |
| **Căn cứ pháp lý** | Luật Nhà ở 2023 (số 27/2023/QH15); Thông tư 05/2024/TT-BXD ngày 31/7/2024 |
| **Trạng thái** | Chờ rà soát pháp lý (xem §9) |

---

## MỤC LỤC

1. [Bối cảnh và mục tiêu](#1-bối-cảnh-và-mục-tiêu)
2. [Căn cứ pháp lý](#2-căn-cứ-pháp-lý)
3. [Tác nhân và phân quyền](#3-tác-nhân-và-phân-quyền)
4. [Phân loại mức độ ưu tiên](#4-phân-loại-mức-độ-ưu-tiên)
5. [Nhóm M1 — Bắt buộc pháp lý](#5-nhóm-m1--bắt-buộc-pháp-lý)
6. [Nhóm M2 — Bắt buộc vận hành](#6-nhóm-m2--bắt-buộc-vận-hành)
7. [Nhóm S / C / W](#7-nhóm-s--c--w)
8. [Yêu cầu phi chức năng](#8-yêu-cầu-phi-chức-năng)
9. [Vấn đề cần chốt với pháp chế](#9-vấn-đề-cần-chốt-với-pháp-chế)
10. [Mô hình dữ liệu cốt lõi](#10-mô-hình-dữ-liệu-cốt-lõi)
11. [Lộ trình triển khai](#11-lộ-trình-triển-khai)
12. [Thuật ngữ](#12-thuật-ngữ)

---

## 1. Bối cảnh và mục tiêu

### 1.1 Bối cảnh

Thị trường phần mềm quản lý chung cư tại Việt Nam đã có nhiều sản phẩm (CyHome, Landsoft Building, Resident, Building Care, Luci Building, Pihome…), nhưng phần lớn tập trung vào **thu phí và vận hành**. Module **Hội nghị nhà chung cư** — hoạt động có rủi ro pháp lý cao nhất và thường phát sinh tranh chấp — vẫn còn sơ sài ở hầu hết các sản phẩm.

Thông tư 05/2024/TT-BXD (hiệu lực 01/8/2024) thay thế Quy chế theo Thông tư 02/2016/TT-BXD, thay đổi một số quy tắc cốt lõi — đáng chú ý nhất là **cách tính phiếu biểu quyết**. Đây là cơ hội để xây dựng sản phẩm bám sát quy định mới.

### 1.2 Mục tiêu sản phẩm

| Mục tiêu | Chỉ số đo |
|---|---|
| Hội nghị được tổ chức hợp lệ, không bị khiếu nại về thủ tục | 0 nghị quyết bị tuyên vô hiệu do lỗi thủ tục |
| Rút ngắn thời gian kiểm phiếu | Từ 2–4 giờ (thủ công) → dưới 15 phút |
| Tăng tỷ lệ tham dự | +15% so với kỳ liền trước |
| Hoàn thiện hồ sơ hội nghị ngay trong ngày | Biên bản + phụ lục xuất được trong 30 phút sau bế mạc |

### 1.3 Ngoài phạm vi (Out of scope) — bản 1.0

- Kế toán tài chính doanh nghiệp của đơn vị quản lý vận hành
- Hệ thống BMS/điều khiển thiết bị (chỉ tích hợp, không tự phát triển)
- Sàn giao dịch mua bán / cho thuê căn hộ

---

## 2. Căn cứ pháp lý

### 2.1 Văn bản áp dụng

| Văn bản | Nội dung liên quan |
|---|---|
| Luật Nhà ở 2023 (27/2023/QH15) | Điều 145 (Hội nghị nhà chung cư), Điều 151 (giá dịch vụ QLVH), Điều 155 (kinh phí bảo trì) |
| Thông tư 05/2024/TT-BXD | Quy chế quản lý, sử dụng nhà chung cư — Điều 13–22 (hội nghị, Ban quản trị), Điều 41 (quyền chủ sở hữu) |
| Luật Giao dịch điện tử 2023 | Giá trị pháp lý của chữ ký số, thông điệp dữ liệu |
| Pháp luật về bảo vệ dữ liệu cá nhân | Xử lý CCCD, số điện thoại, biển số xe |

> **Lưu ý cập nhật:** Đặc tả này được lập trên cơ sở văn bản có hiệu lực tại thời điểm 08/2026. Trước khi khởi động Phase 2, cần rà soát lại xem đã có văn bản sửa đổi, bổ sung Thông tư 05/2024/TT-BXD hay chưa. Toàn bộ ngưỡng và tỷ lệ trong tài liệu này phải được cấu hình được, không hard-code.

### 2.2 Các quy tắc pháp lý cốt lõi cần mã hóa

| # | Quy tắc | Căn cứ | Ảnh hưởng hệ thống |
|---|---|---|---|
| R1 | Hội nghị lần đầu: tổ chức trong 12 tháng kể từ ngày bàn giao **và** có ít nhất 50% số căn hộ đã bàn giao | TT05, Điều 15 | Cảnh báo deadline; điều kiện mở kỳ hội nghị |
| R2 | Điều kiện tiến hành Hội nghị lần đầu (tòa nhà): tối thiểu **50%** đại diện chủ sở hữu căn hộ **đã nhận bàn giao** tham dự | TT05, Điều 17 | Khóa nút Khai mạc |
| R3 | Hội nghị thường niên: mỗi năm 1 lần, tối thiểu **30%** đại diện chủ sở hữu căn hộ đã nhận bàn giao tham dự, **hoặc thấp hơn** nếu Hội nghị lần đầu đã thống nhất | TT05 | Ngưỡng là tham số theo tòa |
| R4 | Quyền biểu quyết tính theo **diện tích sở hữu riêng: 1m² = 01 phiếu** | TT05, Điều 18 khoản 3 | Engine tính phiếu; **khác Thông tư 02 cũ** |
| R5 | Hình thức mặc định là **họp trực tiếp**; chỉ được họp trực tuyến hoặc kết hợp khi do **dịch bệnh, thiên tai** không thể họp trực tiếp, và vẫn phải đủ số lượng, thành phần tham dự | Luật Nhà ở Đ.145; TT05 Đ.18 | Bắt buộc chọn lý do + lưu căn cứ |
| R6 | Chủ sở hữu / người sử dụng là **người khuyết tật** không thể tham dự thì lấy phiếu biểu quyết **tại địa chỉ căn hộ** | TT05, Điều 18 | Luồng phiếu ngoài phòng họp |
| R7 | Quyết định thông qua theo **nguyên tắc đa số**, bằng biểu quyết hoặc bỏ phiếu; phải lập **biên bản có chữ ký** của thành viên chủ trì và thư ký | Luật Nhà ở Đ.145 khoản 5 | Sinh biên bản, chỗ ký |
| R8 | Quá 12 tháng kể từ bàn giao, đủ 50% căn hộ bàn giao, chủ đầu tư không tổ chức và có đơn đề nghị → **UBND cấp xã** có trách nhiệm tổ chức | TT05, Điều 15 khoản 5 | Vai trò UBND cấp xã trong hệ thống |
| R9 | Kinh phí bảo trì **chỉ được dùng** để bảo trì, thay thế hạng mục thuộc sở hữu chung **theo kế hoạch bảo trì đã được Hội nghị thông qua** | Luật Nhà ở Đ.155 khoản 1 | Ràng buộc chi quỹ ↔ nghị quyết |
| R10 | Chủ sở hữu phần diện tích khác (không phải căn hộ) cũng là chủ sở hữu nhà chung cư, có quyền dự họp và biểu quyết theo m² | Luật Nhà ở Đ.2 khoản 19; TT05 Đ.41 | Chủ đầu tư giữ diện tích → có phiếu |

---

## 3. Tác nhân và phân quyền

### 3.1 Danh sách tác nhân

| Tác nhân | Mô tả | Kênh chính |
|---|---|---|
| **Cư dân / Chủ sở hữu** | Chủ sở hữu căn hộ hoặc phần diện tích khác | App / Zalo OA |
| **Người sử dụng** | Người thuê, người ở nhờ — dự họp khi chủ sở hữu không dự | App |
| **Ban quản lý (BQL)** | Đơn vị quản lý vận hành | Web admin |
| **Ban quản trị (BQT)** | Đại diện cư dân do Hội nghị bầu | Web (quyền xem + phê duyệt) |
| **Chủ đầu tư (CĐT)** | Tổ chức hội nghị lần đầu; có thể còn sở hữu diện tích | Web |
| **Tổ bầu cử / Ban kiểm phiếu** | Nhóm được chỉ định cho từng kỳ hội nghị | Web + app kiosk |
| **UBND cấp xã** | Tham dự, hoặc tổ chức thay khi CĐT không tổ chức | Cổng tra cứu (chỉ đọc) |
| **Quản trị hệ thống** | Nhà cung cấp phần mềm | Backoffice |

### 3.2 Ma trận phân quyền — Module Hội nghị

| Chức năng | Cư dân | BQL | BQT | CĐT | Kiểm phiếu | UBND xã |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Xem tài liệu hội nghị | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Tạo kỳ hội nghị | — | ✓ | ✓ | ✓ | — | ✓* |
| Duyệt giấy ủy quyền | — | ✓ | ✓ | — | — | — |
| Check-in đại biểu | — | ✓ | ✓ | ✓ | ✓ | — |
| Khai mạc / bế mạc | — | — | ✓ | ✓ | — | ✓* |
| Mở/đóng phiếu biểu quyết | — | — | — | — | ✓ | — |
| Xem kết quả trước công bố | — | — | — | — | ✓ | — |
| Công bố kết quả | — | — | ✓ | ✓ | — | — |
| Xuất biên bản | — | ✓ | ✓ | ✓ | ✓ | ✓ |
| Xem audit log | — | — | ✓ | — | — | ✓ |
| Sửa/xóa phiếu sau công bố | — | — | — | — | — | — |

\* Chỉ trong trường hợp R8.

> **Nguyên tắc:** BQT phải **xem được** toàn bộ tài chính nhưng **không sửa được** dữ liệu vận hành. Đây là điểm nhiều sản phẩm hiện có làm sai, gây mất niềm tin của cư dân.

---

## 4. Phân loại mức độ ưu tiên

| Ký hiệu | Nghĩa | Hệ quả nếu thiếu |
|---|---|---|
| **M1** | Bắt buộc pháp lý | Nghị quyết có nguy cơ vô hiệu; không được release |
| **M2** | Bắt buộc vận hành | BQL không dùng được sản phẩm hằng ngày |
| **S** | Cần có (Should) | Thua đối thủ khi chào hàng |
| **C** | Mở rộng (Could) | Khác biệt hóa, bán gói cao cấp |
| **W** | Chưa làm (Won't — this release) | Rủi ro pháp lý hoặc ROI thấp |

---

## 5. NHÓM M1 — BẮT BUỘC PHÁP LÝ

### M1-01 · Danh bạ sở hữu và trọng số phiếu

**User story**
> Là **BQL**, tôi cần quản lý chính xác diện tích sở hữu riêng và tư cách của từng chủ sở hữu, để hệ thống tính đúng trọng số phiếu theo quy định 1m² = 1 phiếu.

**Yêu cầu chi tiết**

- Lưu diện tích sở hữu riêng theo **m², số thập phân 2 chữ số** cho:
  - từng căn hộ;
  - từng phần diện tích khác (sàn thương mại, văn phòng, căn hộ lưu trú du lịch) — kể cả phần chủ đầu tư giữ lại.
- Ba trạng thái bàn giao độc lập: `ĐÃ_BÀN_GIAO` / `CHƯA_BÀN_GIAO` / `CĐT_GIỮ_LẠI_KHÔNG_BÁN`.
- Phân biệt vai trò `CHỦ_SỞ_HỮU` và `NGƯỜI_SỬ_DỤNG`; người sử dụng chỉ thay quyền dự họp, trọng số phiếu vẫn truy về diện tích của chủ sở hữu.
- Đồng sở hữu: nhiều cá nhân/tổ chức trên một căn → bắt buộc chỉ định **01 người đại diện** (`representative_id`).
- Toàn bộ thay đổi (sang tên, tách/gộp căn, điều chỉnh diện tích) phải ghi lịch sử có hiệu lực theo thời gian (temporal record), không ghi đè.

**Tiêu chí nghiệm thu (AC)**

- [ ] AC1: Tổng diện tích sở hữu riêng toàn tòa là hằng số kiểm tra được; hệ thống cảnh báo khi tổng thay đổi mà không có chứng từ.
- [ ] AC2: Import Excel danh sách 1.000 căn, hệ thống báo lỗi từng dòng (trùng mã căn, diện tích ≤ 0, thiếu người đại diện) và không import một phần.
- [ ] AC3: Truy vấn "danh sách chủ sở hữu tại ngày D" trả về đúng trạng thái lịch sử tại ngày D.
- [ ] AC4: Một căn có 3 đồng sở hữu chỉ sinh ra 01 quyền biểu quyết với trọng số = diện tích căn.

---

### M1-02 · Snapshot cử tri tại thời điểm khai mạc

**User story**
> Là **Ban kiểm phiếu**, tôi cần danh sách cử tri và trọng số được đóng băng tại thời điểm khai mạc, để kết quả không bị thay đổi bởi biến động dữ liệu trong lúc họp.

**Yêu cầu chi tiết**

- Khi bấm **Khai mạc**, hệ thống tạo bản `VoterRoll` bất biến gồm: danh sách chủ thể có quyền biểu quyết, diện tích sở hữu riêng, tư cách (trực tiếp/ủy quyền/phiếu tại nhà), tổng m² làm mẫu số.
- Mọi thay đổi dữ liệu gốc sau thời điểm này **không** ảnh hưởng kỳ hội nghị đang diễn ra.
- Snapshot được hash (SHA-256) và ghi vào audit log.

**AC**

- [ ] AC1: Sang tên một căn sau khi khai mạc → trọng số phiếu trong kỳ họp không đổi.
- [ ] AC2: Hash của snapshot in được trên biên bản và kiểm chứng lại được.
- [ ] AC3: Không có API nào cho phép sửa `VoterRoll` sau khi tạo (kể cả quyền admin).

---

### M1-03 · Quản lý giấy ủy quyền

**User story**
> Là **BQT**, tôi cần tiếp nhận và thẩm định giấy ủy quyền trước hội nghị, để tránh tình trạng phiếu trùng hoặc gom phiếu trái nội quy.

**Yêu cầu chi tiết**

- Mẫu giấy ủy quyền tải sẵn; nhập: người ủy quyền, người nhận, phạm vi (toàn bộ / từng nội dung cụ thể), thời hạn; upload bản scan (ảnh/PDF).
- **Chặn ủy quyền chồng**: một căn tại một thời điểm chỉ có 01 quyền biểu quyết hiệu lực.
- Cấu hình **trần số ủy quyền** mà một người được nhận (theo nội quy từng tòa; luật không quy định trần nhưng thực tế cần chặn gom phiếu).
- Trạng thái: `CHỜ_DUYỆT` → `HỢP_LỆ` / `TỪ_CHỐI` (bắt buộc nhập lý do).
- Thu hồi ủy quyền trước giờ khai mạc: chủ sở hữu tự thao tác trên app, ghi log.

**AC**

- [ ] AC1: Nộp 2 giấy ủy quyền cho cùng 1 căn → giấy sau bị chặn, hiển thị giấy đang hiệu lực.
- [ ] AC2: Người nhận vượt trần cấu hình → hệ thống từ chối, nêu rõ trần hiện hành.
- [ ] AC3: Chủ sở hữu đã ủy quyền nhưng tự đến check-in trực tiếp → hệ thống tự vô hiệu ủy quyền, ghi log, cảnh báo cho người nhận.
- [ ] AC4: Ủy quyền phạm vi hẹp (chỉ nội dung số 3) → người nhận chỉ mở được phiếu của nội dung số 3.

---

### M1-04 · Check-in và kiểm đếm quorum thời gian thực

**User story**
> Là **Ban tổ chức**, tôi cần biết tức thời hội nghị đã đủ điều kiện tiến hành hay chưa, để quyết định khai mạc hoặc tiếp tục vận động.

**Yêu cầu chi tiết**

- Check-in bằng QR / số căn hộ / CCCD; ghi rõ hình thức: `TRỰC_TIẾP` / `QUA_ỦY_QUYỀN` / `PHIẾU_TẠI_CĂN_HỘ`.
- Màn hình quorum hiển thị **đồng thời hai tỷ lệ khác nhau**:
  - **Tỷ lệ điều kiện tiến hành** = % đại diện chủ sở hữu căn hộ đã nhận bàn giao đang tham dự (theo R2/R3);
  - **Tỷ lệ trọng số** = % m² diện tích sở hữu riêng đang hiện diện (dùng cho kiểm phiếu).
- Ngưỡng cấu hình theo **từng tòa × từng loại hội nghị**. Mặc định: lần đầu 50%, thường niên 30%, bất thường theo quy chế.
- **Khóa nút Khai mạc** khi chưa đạt ngưỡng; ghi nhận mốc thời gian đạt ngưỡng.
- Màn hình chiếu (projector view) hiển thị công khai tỷ lệ cho cư dân.

**AC**

- [ ] AC1: Hai tỷ lệ tính độc lập và hiển thị đúng khi có căn diện tích lớn/nhỏ chênh lệch.
- [ ] AC2: Chưa đủ ngưỡng → nút Khai mạc disable, tooltip nêu còn thiếu bao nhiêu đại biểu / bao nhiêu m².
- [ ] AC3: Check-in 500 lượt trong 30 phút, độ trễ cập nhật quorum < 3 giây.
- [ ] AC4: Mất mạng → app check-in hoạt động offline, đồng bộ khi có mạng, không sinh bản ghi trùng.

---

### M1-05 · Biểu quyết theo trọng số m²

**User story**
> Là **Ban kiểm phiếu**, tôi cần hệ thống tính kết quả theo đúng nguyên tắc 1m² = 1 phiếu, để nghị quyết có giá trị pháp lý.

**Yêu cầu chi tiết**

- Mỗi nội dung (`AgendaItem`) có: loại (`THÔNG_QUA` / `BẦU_CỬ` / `BÃI_MIỄN`), các phương án, ngưỡng thông qua riêng, phương pháp tính.
- Engine tính phiếu theo **R4: 1m² diện tích sở hữu riêng = 01 phiếu**.
  > ⚠️ **Cảnh báo migrate:** Thông tư 02/2016 cũ áp dụng nguyên tắc khác. Nếu chuyển dữ liệu từ hệ thống cũ, đây là lỗi pháp lý phổ biến nhất — phải kiểm thử riêng.
- Ghi nhận đủ 4 trạng thái: `TÁN_THÀNH` / `KHÔNG_TÁN_THÀNH` / `KHÔNG_Ý_KIẾN` / `KHÔNG_BIỂU_QUYẾT (vắng)`.
- Bầu BQT: chọn **N trong M** ứng viên; xử lý trường hợp bằng phiếu (bầu lại vòng 2 hoặc theo quy chế bầu cử đã thông qua).
- Hiển thị mẫu số dùng để tính (tổng m² hiện diện hay tổng m² toàn tòa) — nêu rõ trên biên bản.

**AC**

- [ ] AC1: Bộ test 20 tình huống trọng số (căn 45m², 120m², sàn thương mại 800m²) cho kết quả khớp bảng tính đối chiếu thủ công đến 2 chữ số thập phân.
- [ ] AC2: Tổng 4 trạng thái luôn = 100% tổng m² của snapshot cử tri.
- [ ] AC3: Bầu 5/9 ứng viên, có 2 người bằng phiếu ở vị trí thứ 5 → hệ thống cảnh báo và đề xuất quy trình xử lý, không tự chọn.
- [ ] AC4: Ứng viên bị rút tên giữa chừng → phiếu đã bỏ cho người đó xử lý theo quy chế, có log.

---

### M1-06 · Luồng phiếu ngoài phòng họp

**User story**
> Là **Tổ bầu cử**, tôi cần lấy phiếu tại căn hộ cho chủ sở hữu/người sử dụng là người khuyết tật không thể tham dự, theo đúng R6.

**Yêu cầu chi tiết**

- Danh sách chỉ định trước hội nghị, có căn cứ (ghi nhận tình trạng, không lưu dữ liệu y tế chi tiết).
- Sinh **phiếu in có mã hóa** (QR/barcode) gắn với căn hộ và kỳ hội nghị.
- Nhập kết quả yêu cầu **xác nhận hai người** thuộc Tổ bầu cử (two-man rule), có ảnh chụp phiếu.
- Gộp vào tổng kiểm phiếu, đánh dấu rõ nguồn phiếu.

**AC**

- [ ] AC1: Một phiếu in chỉ nhập được 01 lần; quét lại báo trùng.
- [ ] AC2: Chỉ 1 người xác nhận → không lưu được kết quả.
- [ ] AC3: Bảng kiểm phiếu tách riêng dòng "phiếu lấy tại căn hộ" với số lượng và m².

---

### M1-07 · Hình thức tổ chức hội nghị

**User story**
> Là **Ban tổ chức**, tôi cần hệ thống nhắc đúng ràng buộc pháp lý về hình thức họp, để không tổ chức trực tuyến trái quy định.

**Yêu cầu chi tiết**

- Mặc định: `TRỰC_TIẾP`.
- Chọn `TRỰC_TUYẾN` hoặc `KẾT_HỢP` → **bắt buộc** chọn lý do trong danh mục đóng: `DỊCH_BỆNH` / `THIÊN_TAI`, và upload văn bản căn cứ (công bố dịch, công điện phòng chống thiên tai…).
- Dù trực tuyến, vẫn phải đáp ứng **đủ số lượng và thành phần tham dự** như họp trực tiếp (R5) → engine quorum không đổi.
- Hiển thị cảnh báo pháp lý ngay trên UI khi chọn hình thức ngoại lệ.

> **Định vị sản phẩm:** đây là công cụ **hỗ trợ hội nghị trực tiếp** (check-in, kiểm đếm, kiểm phiếu), **không phải** nền tảng thay thế hội nghị bằng biểu quyết qua app. Marketing và UI phải nhất quán với định vị này.

**AC**

- [ ] AC1: Không chọn lý do → không lưu được cấu hình hình thức ngoại lệ.
- [ ] AC2: Văn bản căn cứ được đính kèm vào hồ sơ hội nghị và in ra trong phụ lục.

---

### M1-08 · Thông báo, mời họp và công khai

**User story**
> Là **BQT**, tôi cần bằng chứng đã gửi giấy mời và tài liệu đến từng chủ sở hữu, để phản bác khiếu nại "không được mời".

**Yêu cầu chi tiết**

- Gửi giấy mời + tài liệu (dự thảo báo cáo, kế hoạch bảo trì, danh sách ứng viên BQT, dự thảo quy chế bầu cử…) trước hội nghị theo thời hạn cấu hình.
- Đa kênh: App, Zalo OA, Email, SMS, và **bản in có ký nhận** (nhập tay kết quả phát tay).
- Lưu **bằng chứng gửi**: thời điểm, kênh, trạng thái `ĐÃ_GỬI` / `ĐÃ_NHẬN` / `ĐÃ_ĐỌC` / `THẤT_BẠI`.
- Sau hội nghị: công khai nội quy, quy chế BQT, nghị quyết trên app và bản in tại nhà sinh hoạt cộng đồng / sảnh thang / khu lễ tân.

**AC**

- [ ] AC1: Xuất được báo cáo "danh sách chủ sở hữu chưa nhận được thông báo" trước ngày họp.
- [ ] AC2: Báo cáo bằng chứng gửi xuất PDF, có mốc thời gian, dùng làm phụ lục hồ sơ.

---

### M1-09 · Hồ sơ đầu ra và biên bản

**User story**
> Là **Thư ký hội nghị**, tôi cần xuất biên bản đầy đủ ngay sau bế mạc để lấy chữ ký các bên khi mọi người còn có mặt.

**Yêu cầu chi tiết**

- **Biên bản hội nghị** theo mẫu đính kèm Thông tư 05/2024/TT-BXD, có ô ký của **thành viên chủ trì** và **thư ký** (R7).
- Phụ lục bắt buộc:
  1. Danh sách đại biểu tham dự (kèm hình thức tham dự)
  2. Danh sách giấy ủy quyền hợp lệ
  3. Bảng kiểm phiếu từng nội dung (số phiếu, m², tỷ lệ, mẫu số)
  4. Nghị quyết hội nghị
  5. Bằng chứng gửi thông báo
  6. Văn bản căn cứ hình thức họp (nếu có)
- Xuất PDF và DOCX; đánh số trang dạng "Trang x/y"; watermark `BẢN NHÁP` cho đến khi công bố chính thức.
- Bộ hồ sơ đóng gói ZIP, kèm file hash để đối chiếu.

**AC**

- [ ] AC1: Xuất trọn bộ hồ sơ cho hội nghị 500 căn trong dưới 60 giây.
- [ ] AC2: Số liệu trên biên bản khớp 100% với dữ liệu hệ thống (kiểm thử đối chiếu tự động).
- [ ] AC3: File DOCX mở được trên MS Word và Google Docs không vỡ layout.

---

### M1-10 · Audit log bất biến

**User story**
> Là **BQT / UBND cấp xã**, tôi cần truy vết mọi thao tác liên quan đến phiếu bầu, để xử lý khi có tố cáo gian lận.

**Yêu cầu chi tiết**

- Ghi log **append-only** (WORM), không có API xóa/sửa, kể cả quyền cao nhất.
- Ghi tối thiểu: ai, thao tác gì, thời điểm (có timezone), IP/thiết bị, giá trị trước–sau.
- Các sự kiện bắt buộc log: tạo kỳ, duyệt/từ chối ủy quyền, check-in, khai mạc, mở/đóng phiếu, ghi phiếu, công bố kết quả, xuất hồ sơ.
- Chuỗi bản ghi phiếu được hash liên kết (hash chain) để phát hiện can thiệp.
- Xuất log ra file ký số phục vụ thanh tra.

**AC**

- [ ] AC1: Thử sửa trực tiếp DB → công cụ kiểm tra hash chain phát hiện được và chỉ ra bản ghi bị đổi.
- [ ] AC2: Xuất log dạng CSV/JSON cho một kỳ hội nghị bất kỳ.

---

### M1-11 · Tách bạch quỹ bảo trì 2%

**User story**
> Là **cư dân**, tôi cần biết quỹ bảo trì được chi đúng kế hoạch đã được Hội nghị thông qua.

**Yêu cầu chi tiết**

- Sổ quỹ bảo trì **tách hoàn toàn** khỏi phí quản lý vận hành và các khoản thu khác (R9).
- Mỗi khoản chi quỹ bảo trì **bắt buộc** liên kết tới một hạng mục trong **kế hoạch bảo trì đã được nghị quyết hội nghị thông qua** — không có liên kết thì không lưu được.
- Chi ngoài kế hoạch → luồng phê duyệt riêng, yêu cầu căn cứ (khẩn cấp / hội nghị bất thường).
- Báo cáo thu–chi quỹ bảo trì công khai cho cư dân.
- Lưu ý: chi phí trông giữ xe **không** thuộc kinh phí quỹ bảo trì — cấu hình chart of accounts phải phản ánh đúng.

**AC**

- [ ] AC1: Tạo phiếu chi quỹ bảo trì không gắn hạng mục kế hoạch → bị chặn.
- [ ] AC2: Truy vết ngược từ một khoản chi → hạng mục → nghị quyết → biên bản hội nghị.
- [ ] AC3: Số dư quỹ bảo trì và quỹ vận hành không bao giờ bù trừ lẫn nhau trong mọi báo cáo.

---

## 6. NHÓM M2 — BẮT BUỘC VẬN HÀNH

| Mã | Yêu cầu | AC tóm tắt |
|---|---|---|
| **M2-01** | Quản lý tòa nhà / cụm, căn hộ, cư dân, phương tiện, thẻ ra vào | Hỗ trợ cụm nhiều tòa với ngưỡng và biểu phí độc lập |
| **M2-02** | Tính và phát hành thông báo phí theo kỳ (phí quản lý, điện, nước, gửi xe) | Chạy batch 2.000 căn < 5 phút; có bản nháp trước khi phát hành |
| **M2-03** | Công nợ, đối soát thu, nhắc nợ tự động | Tuổi nợ theo nhóm 0–30 / 31–60 / 61–90 / >90 ngày |
| **M2-04** | Phản ánh cư dân: tiếp nhận → phân công → SLA → đóng → đánh giá | Cảnh báo quá hạn SLA; báo cáo theo nhân sự |
| **M2-05** | Phân quyền theo vai trò (§3.2) | BQT xem được tài chính, không sửa được vận hành |
| **M2-06** | Multi-tenant: một đơn vị vận hành quản nhiều dự án | Dữ liệu cách ly tuyệt đối giữa các tenant |
| **M2-07** | Quản lý tài liệu pháp lý tòa nhà (hồ sơ CĐT bàn giao cho BQT) | Danh mục hồ sơ chuẩn, đánh dấu thiếu/đủ |

---

## 7. NHÓM S / C / W

### 7.1 Nhóm S — Cần có để cạnh tranh

| Mã | Yêu cầu | Lý do |
|---|---|---|
| S-01 | App cư dân + tích hợp **Zalo OA** | Kênh có tỷ lệ đọc cao nhất tại VN, đối thủ đã có |
| S-02 | Thanh toán online (VietQR, ví điện tử), đối soát tự động | Yêu cầu phổ biến của cư dân |
| S-03 | Đặt tiện ích (bể bơi, BBQ, nhà sinh hoạt cộng đồng, gym) | Tính năng chuẩn của thị trường |
| S-04 | Đăng ký chuyển đồ / thi công sửa chữa căn hộ | Giảm tải cho lễ tân |
| S-05 | Quản lý tài sản + lịch bảo trì định kỳ, gắn kế hoạch bảo trì đã duyệt | Nối M1-11 với vận hành thực tế |
| S-06 | Báo cáo tài chính công khai cho cư dân | **Minh bạch là lý do chính cư dân ủng hộ đổi phần mềm** |
| S-07 | Tích hợp bãi xe, kiểm soát ra vào qua API | Đối thủ (Landsoft, CyHome) đã có |
| S-08 | **Mô phỏng quorum trước hội nghị** | "Đã thu được x% — còn thiếu y m², nên vận động những căn nào" — tính năng khác biệt, chi phí thấp |

### 7.2 Nhóm C — Mở rộng / khác biệt hóa

| Mã | Yêu cầu | Ghi chú |
|---|---|---|
| C-01 | **Chữ ký số cho biên bản hội nghị** | Lợi thế lớn nhất so với toàn bộ đối thủ — nhưng phải chốt pháp lý trước (§9) |
| C-02 | Kiosk / tablet check-in tại sảnh, in phiếu tại chỗ, máy quét mã vạch | Trải nghiệm ngày hội nghị |
| C-03 | Livestream + Q&A có kiểm duyệt cho hội nghị kết hợp | Chỉ dùng trong trường hợp R5 |
| C-04 | Heatmap tầng/khối theo tỷ lệ tham dự | Công cụ vận động cho BQT |
| C-05 | Kho mẫu biểu pháp lý cập nhật theo văn bản mới | Bán như dịch vụ thuê bao — doanh thu định kỳ |
| C-06 | Cổng tra cứu công khai cho UBND cấp xã | Tăng uy tín, hỗ trợ trường hợp R8 |
| C-07 | AI tóm tắt ý kiến cư dân, sinh dự thảo biên bản từ ghi âm | Người dùng phải rà soát trước khi ký |
| C-08 | Đa ngôn ngữ Việt / Anh / Hàn / Nhật | Chung cư có nhiều cư dân nước ngoài |

### 7.3 Nhóm W — Chưa làm trong bản này

| Yêu cầu | Lý do loại |
|---|---|
| Bỏ phiếu on-chain / blockchain | Chi phí giải thích lớn, không tăng giá trị pháp lý theo quy định hiện hành |
| Định danh sinh trắc học thay chữ ký trên biên bản | Chưa có cơ sở pháp lý rõ cho biên bản hội nghị nhà chung cư |
| **Thay thế hoàn toàn họp trực tiếp bằng biểu quyết qua app** | **Trái quy định hiện hành** ngoài trường hợp dịch bệnh / thiên tai (R5) |
| Sàn giao dịch mua bán, cho thuê căn hộ | Ngoài phạm vi, cần giấy phép riêng |

---

## 8. Yêu cầu phi chức năng

### 8.1 Toàn vẹn dữ liệu bầu cử — ưu tiên cao nhất

- WORM log, hash chain cho chuỗi bản ghi phiếu.
- Không cho sửa/xóa phiếu sau khi công bố kết quả — **không có ngoại lệ theo quyền**.
- Backup snapshot cử tri + phiếu mỗi 60 giây trong thời gian hội nghị diễn ra.

### 8.2 Bảo vệ dữ liệu cá nhân

- Mã hóa at-rest: CCCD, số điện thoại, biển số xe, thông tin tình trạng khuyết tật.
- Log truy cập dữ liệu cá nhân; cơ chế tiếp nhận và xử lý yêu cầu xóa/chỉnh sửa.
- Nguyên tắc tối thiểu hóa: chỉ thu thập trường thực sự cần cho nghiệp vụ.
- Ẩn/che một phần dữ liệu trên các màn hình không cần thiết (masking).

### 8.3 Hiệu năng

| Kịch bản | Yêu cầu |
|---|---|
| Check-in cao điểm | 500–2.000 lượt trong 45 phút, độ trễ quorum < 3s |
| Batch tính phí | 2.000 căn < 5 phút |
| Xuất hồ sơ hội nghị | < 60 giây |
| App check-in mất mạng | Hoạt động offline-first, đồng bộ không sinh bản ghi trùng |

### 8.4 Khả năng khôi phục

- Mất điện/mất mạng giữa hội nghị: khôi phục snapshot quorum và phiếu **trong 5 phút**.
- Có quy trình dự phòng thủ công (in danh sách cử tri + phiếu giấy) làm phương án B — tài liệu hướng dẫn kèm sản phẩm.

### 8.5 Khả dụng và trải nghiệm

- Web admin responsive; app cư dân iOS + Android.
- Màn hình chiếu quorum/kết quả tối ưu cho hội trường (font lớn, tương phản cao).
- Hỗ trợ người lớn tuổi: cỡ chữ lớn, luồng check-in tối đa 3 bước.

---

## 9. Vấn đề cần chốt với pháp chế

**Bắt buộc chốt trước khi khởi động Phase 2:**

1. **Giá trị pháp lý của biên bản ký số** — biên bản hội nghị ký bằng chữ ký số (theo Luật Giao dịch điện tử 2023) có được cơ quan nhà nước chấp nhận khi làm thủ tục công nhận Ban quản trị hay không? Nếu không, phải giữ luồng ký tươi.
2. **Giá trị pháp lý của phiếu biểu quyết điện tử** — phiếu bấm trên app/kiosk tại hội nghị trực tiếp có được coi là "biểu quyết hoặc bỏ phiếu" theo Luật Nhà ở Đ.145 khoản 5 không? Phương án an toàn: điện tử là công cụ **hỗ trợ kiểm đếm**, phiếu giấy vẫn là bản gốc lưu hồ sơ.
3. **Tư cách và trọng số biểu quyết của chủ đầu tư** — khi CĐT còn nắm phần diện tích lớn, việc tính theo m² có thể khiến CĐT chi phối hội nghị. Đây là vướng mắc thực tế đã phải xin ý kiến Bộ Xây dựng. Hệ thống cần: hiển thị minh bạch tỷ trọng phiếu của CĐT, và cho phép cấu hình cách xác định mẫu số theo hướng dẫn của cơ quan quản lý.
4. **Trần ủy quyền** — luật không quy định trần; cần khuyến nghị đưa vào Bản nội quy/Quy chế bầu cử của từng tòa để hệ thống có căn cứ áp dụng.
5. **Định nghĩa "căn hộ đã bàn giao"** áp dụng cho hội nghị thường niên và bất thường — có đồng nhất với hội nghị lần đầu không? Ảnh hưởng trực tiếp đến mẫu số quorum.
6. **Rà soát văn bản mới** — kiểm tra đã có sửa đổi, bổ sung Thông tư 05/2024/TT-BXD tính đến thời điểm phát triển hay chưa.

---

## 10. Mô hình dữ liệu cốt lõi

### 10.1 Nhóm nền tảng

```
Building (tòa/cụm)
 ├─ Unit (căn hộ / phần diện tích khác)
 │   ├─ unit_code, unit_type [APARTMENT | COMMERCIAL | OFFICE | TOURIST]
 │   ├─ private_area_sqm        DECIMAL(10,2)   -- cơ sở tính phiếu
 │   ├─ handover_status         [DELIVERED | NOT_DELIVERED | DEVELOPER_RETAINED]
 │   └─ handover_date
 ├─ Ownership (temporal)
 │   ├─ unit_id, owner_id, share_ratio
 │   ├─ representative_id       -- bắt buộc khi đồng sở hữu
 │   └─ valid_from, valid_to
 └─ Occupancy
     └─ unit_id, user_id, role [OWNER | TENANT | RESIDENT], valid_from, valid_to
```

### 10.2 Nhóm hội nghị

```
Conference
 ├─ type              [FIRST | ANNUAL | EXTRAORDINARY]
 ├─ format            [IN_PERSON | ONLINE | HYBRID]
 ├─ format_reason     [EPIDEMIC | NATURAL_DISASTER | NULL]
 ├─ format_evidence_file
 ├─ quorum_threshold_pct        -- cấu hình theo tòa × loại
 ├─ status            [DRAFT | NOTIFIED | CHECKIN | OPENED | VOTING | CLOSED | PUBLISHED]
 └─ opened_at, closed_at

VoterRoll  (BẤT BIẾN — tạo tại thời điểm khai mạc)
 ├─ conference_id, snapshot_hash, created_at
 └─ VoterRollEntry
     ├─ unit_id, voter_id
     ├─ voting_weight_sqm       DECIMAL(10,2)   -- CHỐT tại khai mạc
     ├─ attendance_type         [IN_PERSON | BY_PROXY | HOME_BALLOT | ABSENT]
     └─ eligibility_flag

Proxy
 ├─ conference_id, grantor_id, grantee_id
 ├─ scope             [ALL | SPECIFIC]
 ├─ scope_items[]     -- khi scope = SPECIFIC
 ├─ status            [PENDING | VALID | REJECTED | REVOKED]
 ├─ reject_reason, scan_file, valid_until
 └─ UNIQUE(conference_id, unit_id) WHERE status = 'VALID'

Attendance
 └─ conference_id, unit_id, checkin_at, method, device_id, operator_id

AgendaItem
 ├─ conference_id, seq, title, item_type [APPROVAL | ELECTION | DISMISSAL]
 ├─ pass_threshold_pct, denominator_basis [PRESENT_SQM | TOTAL_SQM]
 └─ Option (phương án / ứng viên)

Ballot
 ├─ agenda_item_id, opened_at, closed_at, opened_by, closed_by
 └─ Vote
     ├─ voter_roll_entry_id, option_id
     ├─ choice          [FOR | AGAINST | ABSTAIN | NOT_VOTED]
     ├─ weight_sqm      -- copy từ VoterRollEntry
     ├─ source          [ONSITE | HOME_BALLOT | PAPER]
     └─ prev_hash, record_hash    -- hash chain

Resolution
 └─ conference_id, agenda_item_id, result, published_at, document_file

AuditLog  (APPEND-ONLY)
 └─ actor_id, action, entity, before, after, at, ip, device, prev_hash, record_hash
```

### 10.3 Ràng buộc dữ liệu quan trọng

| # | Ràng buộc |
|---|---|
| DB1 | `SUM(private_area_sqm)` toàn tòa là hằng số kiểm tra; thay đổi phải có chứng từ |
| DB2 | Mỗi `unit_id` chỉ có tối đa 01 `Proxy` trạng thái `VALID` trong một `Conference` |
| DB3 | `VoterRoll` và `Vote` không có API UPDATE/DELETE |
| DB4 | `Vote.weight_sqm` bắt buộc copy từ `VoterRollEntry`, không join động tới `Unit` |
| DB5 | Phiếu chi quỹ bảo trì bắt buộc có `maintenance_plan_item_id` hợp lệ |
| DB6 | Tổng weight của 4 trạng thái choice = tổng weight `VoterRoll` |

---

## 11. Lộ trình triển khai

| Phase | Nội dung | Kết quả kỳ vọng | Ước lượng |
|---|---|---|---|
| **1** | M2 nền tảng: căn hộ, cư dân, phí, công nợ, phản ánh, phân quyền | Có khách dùng hằng ngày, tạo dữ liệu nền cho Phase 2 | 3–4 tháng |
| **2** | Toàn bộ M1 — module hội nghị | Vũ khí khác biệt; bán vào mùa hội nghị thường niên | 3–4 tháng |
| **3** | S: app cư dân, Zalo OA, thanh toán, tích hợp bãi xe, báo cáo minh bạch | Giữ chân khách, tăng ARPU | 3 tháng |
| **4** | C: chữ ký số, kiosk, AI, cổng UBND | Gói premium, nâng hạng thương hiệu | 4+ tháng |

**Điều kiện tiên quyết của Phase 2:** hoàn tất §9 (rà soát pháp chế). Không bắt đầu code module biểu quyết khi chưa chốt mục 1, 2 và 3.

**Chiến lược go-to-market:** mùa hội nghị thường niên tập trung vào một số tháng nhất định trong năm — Phase 2 nên hoàn thành trước mùa cao điểm ít nhất 2 tháng để có thời gian chạy thử với 2–3 tòa nhà pilot.

---

## 12. Thuật ngữ

| Thuật ngữ | Định nghĩa |
|---|---|
| **Hội nghị nhà chung cư** | Hội nghị của các chủ sở hữu hoặc người sử dụng nhà chung cư (nếu chủ sở hữu không tham dự) — cơ quan quyết định cao nhất về quản lý, sử dụng nhà chung cư |
| **Diện tích sở hữu riêng** | Diện tích thuộc sở hữu riêng của chủ sở hữu căn hộ hoặc phần diện tích khác — cơ sở tính trọng số phiếu |
| **Quorum** | Tỷ lệ tham dự tối thiểu để hội nghị đủ điều kiện tiến hành |
| **Trọng số phiếu** | Số phiếu của một chủ thể = diện tích sở hữu riêng (m²), theo nguyên tắc 1m² = 01 phiếu |
| **Snapshot cử tri (VoterRoll)** | Bản đóng băng danh sách cử tri và trọng số tại thời điểm khai mạc |
| **BQT** | Ban quản trị nhà chung cư |
| **BQL** | Ban quản lý / đơn vị quản lý vận hành |
| **CĐT** | Chủ đầu tư dự án |
| **Quỹ bảo trì 2%** | Kinh phí bảo trì phần sở hữu chung, chỉ dùng theo kế hoạch bảo trì được Hội nghị thông qua |

---

*Tài liệu này là bản nháp phục vụ thảo luận nội bộ. Mọi ngưỡng, tỷ lệ và mẫu biểu phải được đối chiếu với toàn văn Luật Nhà ở 2023 và Thông tư 05/2024/TT-BXD (kể cả các văn bản sửa đổi, bổ sung nếu có) trước khi đưa vào phát triển.*
