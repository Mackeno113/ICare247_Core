# Hướng dẫn cấu hình **Quy tắc sinh mã** (ConfigStudio)

> **Đối tượng:** người cấu hình bảng danh mục trong ConfigStudio (WPF).
> **Phạm vi:** cách dùng màn **Quy tắc sinh mã** (list + popup Sửa) để bật sinh mã tự động cho cột `Ma` của một bảng.
> **Liên quan:** đặc tả kỹ thuật đầy đủ (thuật toán cấp số, điểm chạm backend…) → [32_SINH_MA_TU_DONG_SPEC.md](../spec/32_SINH_MA_TU_DONG_SPEC.md)

---

## 0. Quy tắc sinh mã là gì

Nhiều bảng danh mục (`TC_PhongBan`, `TC_CongTy`…) có cột `Ma` phải nhập tay và duy nhất. Quy tắc sinh mã cho phép hệ thống **tự ghép `Ma`** từ nhiều nguồn thông tin (chữ cố định, ngày, field khác, tra bảng khác, số thứ tự) — người dùng không cần tự nghĩ mã nữa.

Mỗi quy tắc gắn với **đúng 1 cặp (Bảng, Cột mã)** và gồm **nhiều ĐOẠN** ghép lại theo thứ tự để ra `Ma` cuối cùng. Ví dụ: `CT01-PHONG-007` = 3 đoạn tra bảng/chữ cố định + 1 số thứ tự.

---

## 1. Màn danh sách

Mở menu **Quy tắc sinh mã** → thấy lưới toàn bộ quy tắc đã cấu hình.

| Thao tác | Cách làm |
|---|---|
| **Tìm quy tắc** | Gõ vào ô lọc ngay dưới tiêu đề từng cột (Bảng, Cột mã…) — lọc kiểu "chứa", gõ tới đâu lọc tới đó. |
| **Tạo quy tắc mới** | Bấm **"+ Tạo mới"** (góc trên phải) hoặc `Ctrl+N` → mở popup trắng. |
| **Sửa quy tắc** | Double-click vào dòng, hoặc chọn dòng rồi bấm **"Sửa"**. |
| **Xóa quy tắc** | Chọn dòng → bấm **"Xóa"** → xác nhận trong hộp thoại hiện ra. Quy tắc **hệ thống** (`Is_System`) không xóa được — nút Xóa tự khóa. |
| **Làm mới danh sách** | Bấm **"Làm mới"** hoặc `F5`. |

---

## 2. Popup Sửa/Tạo quy tắc

### 2.1. Đích áp dụng

| Ô | Ý nghĩa |
|---|---|
| **Bảng \*** | Bảng dữ liệu đích sẽ áp dụng sinh mã, ví dụ `TC_PhongBan`. |
| **Cột mã \*** | Cột trong bảng đích được gán mã sinh ra — thường là `Ma`. |
| **Bước nhảy** | Số tăng thêm mỗi lần cấp số thứ tự (đoạn `SEQ`). Thường để `1`. |
| **Cho gõ tay đè** | Bật: người dùng được sửa mã dự kiến trên form Thêm mới. Tắt: mã luôn do hệ thống cấp, không sửa được. |
| **Đang hoạt động** | Tắt để tạm ngưng quy tắc mà không cần xóa hẳn. |
| **Mô tả** | Ghi chú nội bộ cho người cấu hình sau này, không hiển thị cho người dùng cuối. |

### 2.2. Các đoạn ghép mã

Mã hoàn chỉnh = ghép các **đoạn** trong lưới theo đúng thứ tự (dùng nút **Lên/Xuống** để đổi thứ tự, **+ Thêm đoạn**/**Xóa đoạn** để thêm/bớt). Chọn 1 dòng trong lưới đoạn để sửa thuộc tính ở panel bên dưới — panel đổi theo **Loại** đoạn đang chọn:

| Loại | Ý nghĩa | Ô cấu hình |
|---|---|---|
| `LITERAL` | Chữ cố định, ví dụ `'CT-'` | **Chữ cố định** |
| `DATE` | Ngày giờ hiện tại (giờ địa phương VN) | **Định dạng ngày**: `yyyy`=năm 4 số · `yy`=năm 2 số · `MM`=tháng · `dd`=ngày · `yyyyMM`=năm+tháng |
| `FIELD` | Lấy thẳng giá trị 1 cột khác trong bản ghi đang lưu | **Field nguồn** = mã cột |
| `LOOKUP` | Lấy giá trị 1 field khác làm khóa, tra sang bảng khác lấy về 1 cột hiển thị | **Field nguồn** (khóa) · **Bảng tra** · **Cột khóa (=)** (thường `Id`) · **Cột lấy giá trị** (thường `Ma`) |
| `SEQ` | Số thứ tự tự tăng, hệ thống tự cấp lúc lưu | *(không có ô riêng — dùng khối "chuẩn hóa" bên dưới)* |

**Khối chuẩn hóa** (áp cho mọi đoạn trừ `LITERAL`):

| Ô | Ý nghĩa |
|---|---|
| **Cắt từ ký tự** | Bỏ qua N ký tự đầu của giá trị trước khi ghép (để trống = lấy từ đầu). |
| **Độ rộng cố định** | Ép đoạn về đúng số ký tự này — đệm thêm nếu thiếu, cắt bớt nếu dư. |
| **Ký tự đệm** | Ký tự dùng để đệm, ví dụ `'0'`. |
| **Đệm phía** | `L` = đệm bên trái (`0007`), `R` = đệm bên phải (`7000`). |
| **Biến đổi chữ** | Chuyển chữ HOA/thường trước khi ghép. |

> Bấm **"📖 Hướng dẫn chi tiết cách ghép mã"** ngay trong popup để xem lại đúng nội dung này kèm ví dụ, không cần rời màn hình.

### 2.3. Hai ràng buộc bắt buộc (hệ thống chặn khi Lưu)

1. **Đúng 1 đoạn `SEQ`** trong toàn quy tắc — không có thì mọi bản ghi ra cùng 1 mã (trùng ngay dòng thứ hai); có 2 đoạn thì hệ thống không biết đoạn nào mang số.
2. **Mọi đoạn đứng TRƯỚC đoạn `SEQ` phải có độ dài xác định** — hoặc là `LITERAL`, hoặc đặt **Độ rộng cố định** > 0. Nếu không, hệ thống không biết mã cũ dài bao nhiêu để cắt đúng phần số khi tính số tiếp theo.

### 2.4. Ví dụ minh họa — mã phòng ban `CT01-PHONG-007`

| # | Loại | Cấu hình | Ra |
|---|---|---|---|
| 1 | `LOOKUP` | `CongTy_Id` → tra `TC_CongTy.Ma` | `CT01` |
| 2 | `LITERAL` | `'-'` | `-` |
| 3 | `LOOKUP` | `Cap_Id` → tra `TC_CapPhongBan.Ma` | `PHONG` |
| 4 | `LITERAL` | `'-'` | `-` |
| 5 | `SEQ` | Độ rộng = 3, đệm `'0'` | `007` |

**Phạm vi đánh số tự suy ra** từ các đoạn đứng trước `SEQ` — không có ô cấu hình riêng. Ở ví dụ trên, số `007` đếm **riêng cho từng cặp (công ty, cấp)** vì phần đứng trước `SEQ` là `CT01-PHONG-`; đổi công ty/cấp là tự đánh số lại từ đầu. Muốn mỗi công ty đánh số riêng thì mã **bắt buộc** phải chứa mã công ty (đoạn `LOOKUP`/`FIELD`/`LITERAL` công ty phải đứng trước `SEQ`).

### 2.5. Xem trước & lưu ý index

- **Ô "Xem trước"** dựng mã mẫu ngay khi sửa (tính ở client, không đụng DB) — đoạn `FIELD`/`LOOKUP` hiện placeholder vì ConfigStudio không đọc dữ liệu thật ở Data DB.
- **Bảng đích PHẢI có index thường trên cột mã** trước khi bật quy tắc chạy thật — filtered unique index sẵn có (`WHERE IsDeleted=0`) KHÔNG đủ vì engine dò cả bản ghi đã xóa mềm khi tính số tiếp theo.
- Nếu cột mã đã là đích của 1 sự kiện `SET_VALUE`/`CLEAR_VALUE` (Event Engine), popup sẽ cảnh báo (không chặn): quy tắc sinh mã chỉ chạy khi ô để trống — giá trị do sự kiện đặt luôn được ưu tiên.

---

## 3. Bảng nào KHÔNG nên bật

Mã chuẩn quốc tế/ngành thì **tuyệt đối không** tự sinh — sinh ra sẽ phá liên thông dữ liệu: mã quốc gia (`VN`, `US`), đơn vị tính (`KG`, `CAI`), ngân hàng (`VCB`), mã hành chính (tỉnh/phường), hay mã ngữ nghĩa cố định (`TONGCT`, `KHOI`, `PHONG`). Chỉ bật cho mã nội bộ do khách tự đặt (mã công ty, mã phòng ban, mã người dùng…).

---

## 4. Tham chiếu liên quan

- [32_SINH_MA_TU_DONG_SPEC.md](../spec/32_SINH_MA_TU_DONG_SPEC.md) — đặc tả kỹ thuật đầy đủ: thuật toán cấp số, khóa phạm vi, điểm chạm backend, tương tác với Event Engine.
- [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) — nếu cần đối chiếu cách cấu hình tra bảng (LookupBox) trên form, khác với đoạn `LOOKUP` của quy tắc sinh mã (đoạn `LOOKUP` chỉ tra 1 giá trị lúc ghép mã, không phải control nhập liệu).
