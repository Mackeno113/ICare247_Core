# DỰ ÁN PHẦN MỀM QUẢN LÝ NHÀ CHUNG CƯ
## Bộ tài liệu thiết kế — Bản chỉ mục

| | |
|---|---|
| **Phiên bản bộ tài liệu** | 0.1 (Draft) |
| **Cập nhật** | 23/08/2026 |
| **Trạng thái** | Chờ rà soát pháp lý và chốt với team kỹ thuật |
| **Số tài liệu** | 11 tệp · 1 đặc tả · 1 ADR · 3 sơ đồ · 6 bộ wireframe (31 màn hình) |

---

## 1. Bắt đầu từ đâu

Tùy vai trò, thứ tự đọc khác nhau:

| Vai trò | Đọc theo thứ tự |
|---|---|
| **Product Owner / BA** | §3.1 Đặc tả → §3.2 Sơ đồ chức năng → §3.4 Wireframe hội nghị |
| **Kỹ thuật / Kiến trúc sư** | §3.1 Đặc tả (§10 Mô hình dữ liệu) → §3.3 State machine → §3.6 ADR |
| **Thiết kế UI/UX** | §3.2 Sơ đồ chức năng → toàn bộ §3.4 và §3.5 Wireframe |
| **Pháp chế** | §3.1 Đặc tả (§2 Căn cứ pháp lý, §9 Vấn đề cần chốt) |
| **Kinh doanh** | §2 Tóm tắt định vị → §3.4 Wireframe hội nghị → §3.5.5 Cổng UBND |

---

## 2. Tóm tắt định vị sản phẩm

### 2.1 Vấn đề

Thị trường phần mềm quản lý chung cư Việt Nam đã có nhiều sản phẩm (CyHome, Landsoft Building, Resident, Building Care, Luci Building, Pihome…) nhưng phần lớn tập trung vào **thu phí và vận hành**. Module **Hội nghị nhà chung cư** — hoạt động có rủi ro pháp lý cao nhất, thường phát sinh tranh chấp — vẫn sơ sài ở hầu hết sản phẩm.

### 2.2 Cơ hội

Thông tư 05/2024/TT-BXD (hiệu lực 01/8/2024) thay thế Quy chế theo Thông tư 02/2016, thay đổi quy tắc cốt lõi — đáng chú ý nhất là **cách tính phiếu biểu quyết: 1m² diện tích sở hữu riêng = 01 phiếu**. Sản phẩm nào bám sát quy định mới sẽ có lợi thế.

### 2.3 Định vị

> Công cụ **hỗ trợ hội nghị trực tiếp** — check-in, kiểm đếm quorum, kiểm phiếu, sinh hồ sơ. **Không phải** nền tảng thay thế hội nghị bằng biểu quyết qua app (trái quy định hiện hành ngoài trường hợp dịch bệnh, thiên tai).

### 2.4 Lập luận bán hàng mạnh nhất

Vòng lặp khép kín mà đối thủ chưa làm được:

```
Phản ánh cư dân  ─┐
Thiết bị quá hạn ─┼→ Kế hoạch bảo trì → Hội nghị thông qua
Đề xuất BQT      ─┘                          ↓
                                    Phiếu chi quỹ bảo trì
                                             ↓
                                    Báo cáo minh bạch cho cư dân
```

**Mọi đồng chi ra đều truy được về một phản ánh cụ thể của cư dân và một lá phiếu tại hội nghị.**

---

## 3. Danh mục tài liệu

### 3.1 Đặc tả yêu cầu nghiệp vụ

**Tệp:** `dac-ta-nghiep-vu-quan-ly-chung-cu.md`

Tài liệu gốc, 12 phần. Nội dung chính:

| Phần | Nội dung | Dùng khi |
|---|---|---|
| §2.2 | **10 quy tắc pháp lý R1–R10** cần mã hóa | Viết business rule |
| §3.2 | Ma trận phân quyền 6 vai trò | Thiết kế RBAC |
| §5 | **11 mục M1** — user story + tiêu chí nghiệm thu | Viết ticket, nghiệm thu |
| §6 | 7 mục M2 bắt buộc vận hành | Lập kế hoạch Phase 1 |
| §7 | Nhóm S / C / W | Quyết định phạm vi release |
| §8 | Yêu cầu phi chức năng | Thiết kế hạ tầng, kiểm thử tải |
| §9 | **6 vấn đề cần chốt với pháp chế** | Điều kiện tiên quyết Phase 2 |
| §10 | Mô hình dữ liệu + 6 ràng buộc DB | Thiết kế CSDL |
| §11 | Lộ trình 4 phase | Lập kế hoạch |

### 3.2 Sơ đồ chức năng tổng thể

**Tệp:** `so-do-chuc-nang-tong-the.mermaid`

Toàn bộ chức năng chia 6 nhóm A–F, mỗi chức năng gắn nhãn ưu tiên M1/M2/S/C theo màu. Dùng để chốt phạm vi với khách hàng và chia việc cho team.

### 3.3 Luồng nghiệp vụ và state machine

| Tệp | Nội dung |
|---|---|
| `luong-nghiep-vu-hoi-nghi.mermaid` | Luồng end-to-end 5 giai đoạn: chuẩn bị → ngày hội nghị → kiểm phiếu → hậu hội nghị → ràng buộc liên tục |
| `state-machine-ky-hoi-nghi.mermaid` | 9 trạng thái của một kỳ hội nghị, kèm điều kiện chuyển trạng thái và 2 điểm không quay lại |

### 3.4 Wireframe · Module hội nghị

**Tệp:** `wireframe-man-hinh-hoi-nghi.html` — 8 màn hình

| Mã | Màn hình | Ưu tiên |
|---|---|---|
| MH-01 | Bảng điều khiển kỳ hội nghị | M1 |
| MH-02 | Tạo kỳ · cấu hình ngưỡng và hình thức họp | M1-04, M1-07 |
| MH-03 | Duyệt giấy ủy quyền | M1-03 |
| MH-04 | Check-in tại sảnh (tablet, offline-first) | M1-04 |
| MH-05 | **Bảng quorum điều hành** | M1-04 |
| MH-06 | Màn hình chiếu hội trường | S |
| MH-07 | Kiểm phiếu theo trọng số m² | M1-05 |
| MH-08 | Xuất hồ sơ và biên bản | M1-09 |

### 3.5 Wireframe · Các nhóm còn lại

| Tệp | Nhóm | Màn hình |
|---|---|---|
| `wireframe-A-nen-tang-du-lieu.html` | **A · Nền tảng dữ liệu** | Danh mục đơn vị · Hồ sơ sở hữu · Import Excel · Lịch sử theo thời gian · Phương tiện |
| `wireframe-C-tai-chinh.html` | **C · Tài chính** | Biểu phí · Chạy kỳ phí · Công nợ · **Phiếu chi quỹ bảo trì** · Báo cáo minh bạch |
| `wireframe-D-van-hanh.html` | **D · Vận hành** | Điều phối phản ánh · **Kế hoạch bảo trì trình hội nghị** · Tài sản và lịch bảo trì · Đặt tiện ích và thi công |
| `wireframe-E-cong-cu-dan.html` | **E · Cổng cư dân** | App + Zalo OA · Tra cứu phí và thanh toán · Cổng hội nghị · **Lập ủy quyền** · Gửi phản ánh |
| `wireframe-F-he-thong.html` | **F · Hệ thống** | Ma trận phân quyền · **Audit log và hash chain** · Multi-tenant · Cổng UBND cấp xã |

### 3.6 Quyết định thiết kế (ADR)

**Tệp:** `ADR-dot-1-nen-tang-du-lieu.md`

| Mã | Quyết định |
|---|---|
| ADR-001 | Gộp "phần diện tích khác" chung bảng `Unit` với căn hộ |
| ADR-002 | Import danh mục theo nguyên tắc all-or-nothing, tách luồng import bổ sung |

---

## 4. Mười quy tắc pháp lý cốt lõi

Bảng tra nhanh. Chi tiết tại Đặc tả §2.2.

| # | Quy tắc | Ảnh hưởng hệ thống |
|---|---|---|
| **R1** | Hội nghị lần đầu trong 12 tháng kể từ bàn giao **và** ≥50% căn đã bàn giao | Cảnh báo deadline |
| **R2** | Điều kiện tiến hành lần đầu: ≥50% đại diện chủ sở hữu căn đã nhận bàn giao | Khóa nút Khai mạc |
| **R3** | Thường niên: mỗi năm 1 lần, ≥30% hoặc thấp hơn nếu HN lần đầu thống nhất | Ngưỡng cấu hình theo tòa |
| **R4** | **1m² diện tích sở hữu riêng = 01 phiếu** | Engine tính phiếu |
| **R5** | Mặc định họp trực tiếp; trực tuyến chỉ khi dịch bệnh, thiên tai | Bắt buộc chọn lý do + văn bản căn cứ |
| **R6** | Người khuyết tật không dự được → lấy phiếu tại căn hộ | Luồng phiếu ngoài phòng họp |
| **R7** | Quyết định theo đa số; biên bản có chữ ký chủ trì và thư ký | Sinh biên bản, chỗ ký |
| **R8** | Quá 12 tháng, CĐT không tổ chức, có đơn đề nghị → UBND cấp xã tổ chức | Vai trò UBND trong hệ thống |
| **R9** | Quỹ bảo trì chỉ dùng theo kế hoạch đã được hội nghị thông qua | Ràng buộc chi ↔ nghị quyết |
| **R10** | Chủ sở hữu phần diện tích khác cũng có quyền dự họp và biểu quyết theo m² | CĐT giữ diện tích → có phiếu |

---

## 5. Mười quyết định thiết kế quan trọng nhất

Rút từ chú thích các bộ wireframe. Đây là danh sách cần bảo vệ khi review với team.

| # | Quyết định | Ở đâu | Vì sao |
|---|---|---|---|
| 1 | **Hai thanh đo quorum riêng biệt, không gộp** | MH-05 | Tỷ lệ đại diện quyết định có được khai mạc; tỷ lệ m² là mẫu số kiểm phiếu. Gộp là sai luật |
| 2 | **Hệ thống từ chối tự xử khi bằng phiếu** | MH-07 | Máy chọn thay người là rủi ro pháp lý. Chỉ cảnh báo và nêu phương án theo quy chế |
| 3 | **Không tải được bản chính thức trước khi ký** | MH-08 | Watermark BẢN NHÁP là ràng buộc kỹ thuật, không phải trang trí |
| 4 | **Phần diện tích khác nằm chung bảng với căn hộ** | A-01 | Tách bảng dẫn tới quên `UNION` khi tính phiếu. 6 đơn vị = 8,7% trọng số |
| 5 | **Import danh mục all-or-nothing** | A-03 | Lỗi tổng diện tích không tự lộ ra, chỉ phát hiện đúng ngày hội nghị |
| 6 | **Chuỗi truy vết hiển thị khi lập phiếu chi** | C-04 | Khác biệt giữa "hệ thống ghi sổ" và "hệ thống ràng buộc" |
| 7 | **Không tự cắt dịch vụ khi nợ phí** | C-03 | Xử lý nợ theo hợp đồng dịch vụ BQT đã ký. Phần mềm chỉ nhắc và lập hồ sơ |
| 8 | **Hiển thị quyền biểu quyết bằng số cụ thể** | E-03 | Đòn bẩy tăng tỷ lệ tham dự mạnh nhất |
| 9 | **Cảnh báo "không bỏ phiếu qua app"** | E-03 | Thiếu nó, cư dân tưởng đã bầu xong và không đến hội nghị |
| 10 | **Tách lập phiếu chi (BQL) và phê duyệt (BQT)** | F-01 | Gộp một vai trò là mở đường lạm dụng quỹ bảo trì |

---

## 6. Sáu vấn đề cần chốt với pháp chế

**Điều kiện tiên quyết của Phase 2. Không bắt đầu code module biểu quyết khi chưa chốt mục 1, 2, 3.**

| # | Vấn đề | Ảnh hưởng nếu chưa chốt |
|---|---|---|
| 1 | Giá trị pháp lý của biên bản ký số | Phải giữ luồng ký tươi (hiện đang thiết kế theo phương án này) |
| 2 | Giá trị pháp lý của phiếu biểu quyết điện tử | Phương án an toàn: điện tử hỗ trợ kiểm đếm, phiếu giấy là bản gốc |
| 3 | Tư cách và trọng số biểu quyết của chủ đầu tư | Ảnh hưởng cách xác định mẫu số |
| 4 | Trần ủy quyền | Cần đưa vào nội quy từng tòa để có căn cứ áp dụng |
| 5 | Định nghĩa "căn hộ đã bàn giao" cho HN thường niên | Ảnh hưởng trực tiếp mẫu số quorum |
| 6 | Rà soát văn bản sửa đổi Thông tư 05/2024/TT-BXD | Toàn bộ ngưỡng phải cấu hình được, không hard-code |

---

## 7. Lộ trình

| Phase | Nội dung | Kết quả kỳ vọng | Ước lượng |
|---|---|---|---|
| **1** | M2 nền tảng: căn hộ, cư dân, phí, công nợ, phản ánh, phân quyền | Có khách dùng hằng ngày, tạo dữ liệu nền cho Phase 2 | 3–4 tháng |
| **2** | Toàn bộ M1 — module hội nghị | Vũ khí khác biệt; bán vào mùa hội nghị thường niên | 3–4 tháng |
| **3** | S: app cư dân, Zalo OA, thanh toán, tích hợp bãi xe, báo cáo minh bạch | Giữ chân khách, tăng ARPU | 3 tháng |
| **4** | C: chữ ký số, kiosk, AI, cổng UBND | Gói premium, nâng hạng thương hiệu | 4+ tháng |

**Go-to-market:** mùa hội nghị thường niên tập trung vào một số tháng nhất định — Phase 2 nên xong trước mùa cao điểm ít nhất 2 tháng để chạy thử với 2–3 tòa pilot.

---

## 8. Việc còn thiếu

| # | Hạng mục | Ghi chú |
|---|---|---|
| 1 | **ERD trực quan** | Mô hình dữ liệu hiện ở dạng text trong Đặc tả §10 |
| 2 | **ADR cho đợt 2–5** | Mới có ADR đợt 1. Các quyết định đợt sau đang nằm rải trong chú thích wireframe |
| 3 | **Wireframe hi-fi** | Chỉ nên làm sau khi chốt luồng với 2–3 tòa pilot |
| 4 | Kiến trúc hệ thống | Chưa vẽ |
| 5 | Kế hoạch kiểm thử | Đặc biệt bộ test 20 tình huống trọng số (M1-05 AC1) |
| 6 | Tài liệu vận hành dự phòng | Phương án B khi mất điện giữa hội nghị (§8.4) |

---

## 9. Quy ước dùng chung

### 9.1 Mức độ ưu tiên

| Ký hiệu | Nghĩa | Hệ quả nếu thiếu |
|---|---|---|
| **M1** | Bắt buộc pháp lý | Nghị quyết có nguy cơ vô hiệu; không được release |
| **M2** | Bắt buộc vận hành | BQL không dùng được sản phẩm hằng ngày |
| **S** | Cần có | Thua đối thủ khi chào hàng |
| **C** | Mở rộng | Khác biệt hóa, bán gói cao cấp |
| **W** | Chưa làm | Rủi ro pháp lý hoặc ROI thấp |

### 9.2 Màu trong wireframe

| Màu | Nghĩa |
|---|---|
| Đỏ nhạt | Ràng buộc pháp lý, hoặc trạng thái quá hạn |
| Vàng nhạt | Cảnh báo, trạng thái khóa, chờ xử lý |
| Xanh nhạt | Hợp lệ, đã đạt điều kiện |
| Xám | Trạng thái thường |

### 9.3 Thuật ngữ viết tắt

| Viết tắt | Nghĩa |
|---|---|
| **BQL** | Ban quản lý / đơn vị quản lý vận hành |
| **BQT** | Ban quản trị nhà chung cư |
| **CĐT** | Chủ đầu tư dự án |
| **HN** | Hội nghị nhà chung cư |
| **VoterRoll** | Snapshot cử tri đóng băng tại thời điểm khai mạc |
| **Quorum** | Tỷ lệ tham dự tối thiểu để hội nghị đủ điều kiện tiến hành |

---

## 10. Đề xuất tổ chức repo

```
/docs
  ├─ index.md                        ← tài liệu này
  ├─ dac-ta-nghiep-vu.md
  ├─ /adr
  │   ├─ ADR-001-gop-phan-dien-tich-khac.md
  │   ├─ ADR-002-import-all-or-nothing.md
  │   └─ ADR-003…                    ← bổ sung cho đợt 2–5
  ├─ /diagrams
  │   ├─ so-do-chuc-nang-tong-the.mermaid
  │   ├─ luong-nghiep-vu-hoi-nghi.mermaid
  │   ├─ state-machine-ky-hoi-nghi.mermaid
  │   └─ erd.mermaid                 ← còn thiếu
  └─ /wireframes
      ├─ hoi-nghi.html
      ├─ A-nen-tang-du-lieu.html
      ├─ C-tai-chinh.html
      ├─ D-van-hanh.html
      ├─ E-cong-cu-dan.html
      └─ F-he-thong.html
```

Sơ đồ Mermaid render trực tiếp trên GitHub/GitLab nên tài liệu và bản vẽ luôn đồng bộ — không xảy ra tình trạng sơ đồ trong Figma cũ hơn đặc tả vài phiên bản.

---

*Toàn bộ bộ tài liệu là bản nháp phục vụ thảo luận nội bộ. Mọi ngưỡng, tỷ lệ và mẫu biểu phải được đối chiếu với toàn văn Luật Nhà ở 2023 và Thông tư 05/2024/TT-BXD (kể cả văn bản sửa đổi, bổ sung nếu có) trước khi đưa vào phát triển.*
