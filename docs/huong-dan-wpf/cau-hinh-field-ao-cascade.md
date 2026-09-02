# Hướng dẫn sử dụng — **Field ảo + Cascade** (chống cấu hình sai) — ConfigStudio

> **Tài liệu này dành cho ai?** Người cấu hình hệ thống (Admin, Business Analyst, IT triển khai) —
> **không cần biết lập trình**. Nếu bạn là lập trình viên/AI cần tra cứu kỹ thuật, đi thẳng xuống
> [Phần B — Tra cứu kỹ thuật](#phần-b--tra-cứu-kỹ-thuật).
>
> **Bài này dùng để làm gì?** Áp dụng cho mọi màn cần **chọn cấp cha để lọc cấp con, nhưng chỉ lưu
> cấp con** vào cơ sở dữ liệu — ví dụ chọn Ngân hàng chỉ để lọc, nhưng thực chất chỉ cần lưu Chi
> nhánh ngân hàng (suy ra được ngân hàng từ chi nhánh).
>
> Ví dụ xuyên suốt cả bài: **Ngân hàng → Chi nhánh ngân hàng**. Cách làm áp dụng y hệt cho
> **Tỉnh/Thành → Phường/Xã** (đổi tên bảng/field).

---

## Vài thuật ngữ cần biết trước khi đọc

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **LookupBox** | Ô nhập cho phép **chọn** 1 bản ghi có sẵn thay vì gõ tay (xem chi tiết [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md)). |
| **Khóa ngoại (FK)** | Cách 1 bảng dữ liệu "trỏ" tới đúng 1 dòng của bảng khác bằng con số Id. |
| **Field ảo** | Field chỉ hiện trên màn hình để người dùng **chọn nhằm lọc** danh sách của field khác — **không lưu** xuống cơ sở dữ liệu. |
| **Field con** | Field **lưu thật** xuống cơ sở dữ liệu; danh sách của nó bị lọc theo giá trị field cha (thường là field ảo) vừa chọn. |
| **Cascade** | Cơ chế: đổi field cha → field con **tự nạp lại** danh sách + xóa giá trị cũ nếu không còn hợp lệ. |
| **Field Code** | Mã định danh của 1 field trên Form — field khác dùng mã này để "gọi tên" tới nó khi lọc theo nhau. |

---

## Phần A — Làm theo từng bước

### Chuẩn bị trước khi bắt đầu

- Xác định trước: cấp nào **chỉ để lọc** (không cần lưu riêng) → làm **field ảo**; cấp nào **thật sự
  cần lưu** → làm **field con** bình thường. Quy tắc: nếu suy ra được cấp cha từ cấp con (ví dụ biết
  Chi nhánh thì biết luôn Ngân hàng), cấp cha đó nên là field ảo.
- Danh mục nguồn (`DM_NganHang`, `DM_ChiNhanhNganHang`) phải **đã cấu hình xong trước**, đúng thứ tự:
  Ngân hàng trước, Chi nhánh sau (xem [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md)).
- Cột lưu dữ liệu của field con (vd `ChiNhanhNganHang_Id`) phải **đã tồn tại sẵn** trong bảng đích.
  Nếu là cột mới hoàn toàn, báo IT kỹ thuật tạo cột trước (xem thêm mục **6. Tiền đề schema** ở
  Phần B).

---

### Bước 1 — Tạo field cha (ảo): Ngân hàng

**Mục đích:** cho người dùng chọn Ngân hàng trước để lọc danh sách Chi nhánh — nhưng **không lưu**
Ngân hàng vào DB vì đã suy ra được từ Chi nhánh.

**Làm gì:**
1. Ở panel Field, bấm **+ Thêm field** (field ảo không ứng với cột nào nên nút **Auto-generate**
   không tự sinh được — phải thêm tay).
2. **Editor**: chọn **LookupBox** (không dùng LookupComboBox — control đó lưu mã chuỗi, không phải
   khóa ngoại).
3. Ở tab **Behavior**, bật **🔮 Field ảo** — ô **Field Code** sẽ hiện ra ngay bên dưới.
4. Gõ **Field Code** = `NganHang_Id` — ghi nhớ **chính xác từng ký tự**, vì field con ở Bước 2 sẽ
   phải gõ lại đúng y hệt.
5. Đặt **Nhãn** qua nút 🌐: "Ngân hàng".
6. **Cấu hình Lookup**: Source `DM_NganHang`, Value `Id`, Display `Ten`, Query Mode **Bảng/View**,
   Filter SQL để **trống** (cho chọn tự do).
7. Bật **Tìm kiếm**.

**Bạn sẽ thấy gì:** field có icon 🔮; ô **Cột DB** trống/mờ (tooltip *"Bỏ trống nếu bật Field ảo"*);
Field Code hiển thị đúng `NganHang_Id`.

**Lỗi thường gặp:** bật Field ảo nhưng **không nhập Field Code** → hệ thống **chặn Lưu**; gõ Field
Code khác so với dự định sẽ dùng ở Bước 2.

---

### Bước 2 — Tạo field con (lưu DB): Chi nhánh ngân hàng

**Mục đích:** đây là field thật sự lưu xuống cơ sở dữ liệu, nhưng danh sách chọn chỉ hiện đúng những
chi nhánh thuộc Ngân hàng vừa chọn ở Bước 1.

**Làm gì:**
1. Field này ứng với cột thật `ChiNhanhNganHang_Id` (nút **Auto-generate** sinh được nếu cột đã có
   sẵn trong bảng).
2. **Editor**: **LookupBox**. Tab Behavior: **Field ảo tắt** (field này phải lưu, không được bật).
3. **Cấu hình Lookup**: Source `DM_ChiNhanhNganHang`, Value `Id`, Display `Ten`.
4. **Filter SQL**: gõ `NganHang_Id = @NganHang_Id` — vế trái là tên cột khóa ngoại **thật** trong bảng
   `DM_ChiNhanhNganHang`, vế phải là `@` + đúng **Field Code** của field cha đã đặt ở Bước 1.
5. Ô **"Tự reload khi field thay đổi"**: gõ `NganHang_Id` — **bắt buộc**, đây là ô hay bị bỏ quên
   nhất khiến cascade không chạy.
6. **ORDER BY**: `Ten ASC`. Bật **Tìm kiếm**.

**Bạn sẽ thấy gì:** ô `@` trong Filter SQL và ô "Tự reload" **cùng** giá trị `NganHang_Id`.

**Lỗi thường gặp:**
- `@param` gõ khác Field Code cha dù chỉ 1 ký tự → chọn Ngân hàng xong, danh sách Chi nhánh **luôn
  rỗng**.
- Bỏ trống ô "Tự reload" → đổi Ngân hàng xong, danh sách Chi nhánh **không tự cập nhật**.
- Dùng nhầm **LookupComboBox** → Chi nhánh lưu ra mã chuỗi lạ, không phải Id.

---

### Bước 3 — Kiểm tra bằng nút Diễn giải trước khi lưu

**Mục đích:** bắt lỗi cấu hình cascade **trước khi lưu**, đỡ phải sửa lại sau khi test trên web.

**Làm gì:** bấm **▶ Diễn giải** ở panel Lookup, đọc bản tóm tắt tiếng Việt, đối chiếu đủ 4 dòng:
- [ ] Field cha hiện **"field ảo — không lưu DB"**.
- [ ] Field con hiện **"lọc theo `@NganHang_Id`"** (đúng Field Code cha).
- [ ] Field con hiện **"tự nạp lại khi `NganHang_Id` đổi"**.
- [ ] Không có cảnh báo "không tìm thấy field cha khớp @param".

Đúng đủ 4 dòng → bấm **💾 Lưu**.

**Bạn sẽ thấy gì:** nếu có dòng cảnh báo màu vàng/đỏ, phần Diễn giải chỉ rõ đang sai ở đâu (tên field,
tên @param...).

**Lỗi thường gặp:** bấm Lưu ngay mà bỏ qua bước Diễn giải — dễ bỏ sót lỗi gõ sai 1 ký tự trong
`@param`.

---

### Bước 4 — Đồng bộ và kiểm tra cascade trên web

**Mục đích:** đưa cấu hình ra môi trường thật và xác nhận cascade chạy đúng.

**Làm gì:**
- Mở ứng dụng web → **Quản trị › Đồng bộ cấu hình** → **Xem trước** → **Áp dụng từ master**.
- Mở màn hình có 2 field vừa cấu hình (vd màn Công ty) → chọn **Ngân hàng** → kiểm tra ô **Chi nhánh**
  chỉ hiện đúng chi nhánh thuộc ngân hàng đó (không hiện toàn bộ).
- Đổi sang **Ngân hàng khác** → ô Chi nhánh phải **tự làm mới** ngay, không cần bấm thêm gì.
- Lưu form → kiểm tra dữ liệu chỉ ghi **Id Chi nhánh** (không có cột nào lưu Ngân hàng).

**Bạn sẽ thấy gì:** danh sách Chi nhánh lọc đúng theo Ngân hàng đã chọn; đổi Ngân hàng thì Chi nhánh
tự nạp lại và xóa lựa chọn cũ nếu không còn hợp lệ. ✅

**Lỗi thường gặp:** cấu hình đúng nhưng màn không đổi → thường do **chưa đồng bộ cấu hình** hoặc
cache cũ — đồng bộ lại rồi mở lại màn.

Xem thêm bảng tổng hợp mọi triệu chứng lỗi cascade ở mục **7. Lỗi thường gặp** trong Phần B.

---

## Phần B — Tra cứu kỹ thuật

> Dành cho người đã quen cách làm ở Phần A, hoặc lập trình viên/AI cần tra nhanh. Nền tảng cơ chế:
> [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md) ·
> [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md).

## 1. Hiểu trong 30 giây

| Khái niệm | Là gì | Trong DB |
|---|---|---|
| **Field cha (ảo)** | LookupBox chỉ để **lọc** danh sách con (vd Ngân hàng). Người dùng chọn nhưng **không lưu**. | ❌ không có cột |
| **Field con** | LookupBox **lưu DB** (vd Chi nhánh), danh sách bị **lọc theo cha**. | ✅ có cột FK |
| **Cascade** | Đổi cha → con **tự nạp lại** + xóa giá trị con cũ nếu không còn hợp lệ. | — |

> **Nguyên tắc:** nếu một cấp **không cần lưu** (chỉ để thu hẹp lựa chọn) → đặt nó là **field ảo**. Chỉ cấp thật sự cần
> lưu mới có cột trong bảng.

---

## 2. Cây quyết định — có cần field ảo không?

```
Người dùng phải chọn A rồi mới chọn B (B phụ thuộc A)?
        │
        ├─ Có, và A CŨNG cần lưu vào bảng      → A = field thật (cascade 2 field thật, KHÔNG ảo)
        │
        └─ Có, nhưng A CHỈ để lọc B (không lưu) → A = FIELD ẢO,  B = field con (tài liệu này)
```

- **Ngân hàng → Chi nhánh:** công ty chỉ cần lưu **chi nhánh** (suy ra ngân hàng qua chi nhánh) ⇒ Ngân hàng = **ảo**.
- **Tỉnh/Thành → Phường/Xã:** công ty chỉ cần lưu **phường/xã** (suy ra tỉnh qua phường/xã) ⇒ Tỉnh = **ảo**.

---

## 3. 🟡 4 QUY TẮC VÀNG (thuộc lòng — 90% lỗi nằm ở đây)

1. **`@param` của con = `Field Code` của cha — TRÙNG TỪNG KÝ TỰ.**
   Filter SQL con viết `NganHang_Id = @NganHang_Id` thì field **cha** phải có `Field Code = NganHang_Id`. Sai 1 ký tự ⇒ **con luôn rỗng**.
2. **Con phải điền ô "Tự reload khi field thay đổi" = Field Code cha.** Bỏ trống ⇒ đổi cha **không** nạp lại con.
3. **Cha ảo phải bật 🔮 Field ảo** (tab Behavior) + **nhập Field Code**. Không bật ⇒ engine cố lưu → **lỗi ghi DB**.
4. **Cha đứng TRÊN con** trong form (chọn cha trước). Bên trái dấu `=` trong Filter SQL là **tên cột FK trong bảng con**, bên phải là `@` + Field Code cha.

> 3 quy tắc đầu chính là 3 lỗi phổ biến nhất ở §7. Đọc kỹ trước khi cấu hình.

---

## 4. Các bước — ví dụ **Ngân hàng → Chi nhánh**

> Làm tương tự cho **Tỉnh/Thành → Phường/Xã** (đổi tên bảng/field). Danh mục nguồn (`DM_NganHang`, `DM_ChiNhanhNganHang`)
> phải **đã đăng ký `Sys_Table`** và cấu hình **trước** (đúng thứ tự: ngân hàng → chi nhánh).

### Bước A — Field cha (ảo): **Ngân hàng**

Panel Field → **+ Thêm field** (field ảo không có cột nên Auto-generate không sinh):

| Mục | Giá trị | ⚠️ Kiểm |
|---|---|---|
| **Editor** | `LookupBox` | KHÔNG dùng LookupComboBox (nó lưu mã chuỗi, không phải FK) |
| tab **Behavior** → **🔮 Field ảo** | **✓** | bật xong ô **Field Code** hiện ra ngay dưới |
| **Field Code** | `NganHang_Id` | ghi nhớ **chính xác** — sẽ thành `@NganHang_Id` ở con |
| **Nhãn** (🌐) | Ngân hàng | dịch qua nút 🌐, không gõ tiếng Việt vào ô `_Key` |
| **Cấu hình Lookup** | Source `DM_NganHang`, Value `Id`, Display `Ten` | Query_Mode = **Bảng/View** |
| **Filter SQL** | *(để trống)* | cha chọn tự do |
| **Tìm kiếm** | ✓ | danh sách dài |

> ✅ **Đúng khi:** field có 🔮, ô **Cột DB** trống/mờ (tooltip: *"Bỏ trống nếu bật Field ảo"*), Field Code = `NganHang_Id`.

### Bước B — Field con (lưu DB): **Chi nhánh ngân hàng**

Field này **có cột** `ChiNhanhNganHang_Id` (Auto-generate sinh được — nếu cột chưa có, xem §6 Tiền đề):

| Mục | Giá trị | ⚠️ Kiểm |
|---|---|---|
| **Editor** | `LookupBox` | |
| **🔮 Field ảo** | **✗ (tắt)** | con PHẢI lưu → không ảo |
| **Cấu hình Lookup** | Source `DM_ChiNhanhNganHang`, Value `Id`, Display `Ten` | |
| **Filter SQL** | `NganHang_Id = @NganHang_Id` | trái = cột FK trong `DM_ChiNhanhNganHang`; phải = `@` + Field Code cha (Bước A) |
| **Tự reload khi field thay đổi** | `NganHang_Id` | = Field Code cha — **bắt buộc** để cascade chạy |
| **ORDER BY** | `Ten ASC` | |
| **Tìm kiếm** | ✓ | |

> ✅ **Đúng khi:** `@` trong Filter SQL và ô Tự reload **cùng** = `NganHang_Id` (Field Code cha).

---

## 5. ✔️ Kiểm tra TRƯỚC khi lưu — nút **▶ Diễn giải**

Bấm **▶ Diễn giải** ở panel Lookup → đọc bản tóm tắt tiếng Việt. Đối chiếu:

- [ ] Field cha: hiện **"field ảo — không lưu DB"**.
- [ ] Field con: hiện **"lọc theo `@NganHang_Id`"** đúng tên Field Code cha.
- [ ] Field con: hiện **"tự nạp lại khi `NganHang_Id` đổi"**.
- [ ] Không có cảnh báo "không tìm thấy field cha khớp @param".

Chỉ khi 4 dòng đúng → **💾 Lưu**. Sau đó **Đồng bộ cấu hình** xuống tenant + mở màn chạy thử.

---

## 6. ⚠️ Tiền đề schema (khi cấp con là cột mới)

Cascade **chỉ ghi được** nếu bảng đích có **cột của field con**. Với Chi nhánh: `TC_CongTy` cần cột
`ChiNhanhNganHang_Id` (FK → `DM_ChiNhanhNganHang`). Nếu chưa có → **migration trước** (thêm cột + cập nhật view),
xem callout Tiền đề ở [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md). Field **cha ảo** thì **không** cần cột.

---

## 7. 🔴 Lỗi thường gặp — triệu chứng → khắc phục

| Triệu chứng | Nguyên nhân | Khắc phục |
|---|---|---|
| **Con luôn rỗng** (chọn cha vẫn trống) | `@param` con ≠ Field Code cha | Sửa cho **trùng từng ký tự** (Quy tắc 1) |
| **Đổi cha, con không nạp lại** | Quên ô **Tự reload** | Điền ô "Tự reload" = Field Code cha (Quy tắc 2) |
| **Lưu báo lỗi cột/không lưu được** | Cha **chưa bật 🔮 Field ảo** → engine cố ghi cột không có | Bật Field ảo cho cha (Quy tắc 3) |
| **Con hiện đủ mọi bản ghi** (không lọc) | Filter SQL trống hoặc **sai tên cột FK** bên trái `=` | Kiểm cột FK thật trong bảng con |
| **Lưu báo thiếu cột `ChiNhanhNganHang_Id`** | Chưa chạy migration Tiền đề | §6 — thêm cột + cập nhật view trước |
| **Con lưu ra mã chuỗi lạ, không phải Id** | Dùng nhầm **LookupComboBox** cho FK | Đổi về **LookupBox** (Value = cột Id) |
| **Lần đầu mở form con đã sai data** | Cha chưa chọn → `@param` NULL | Nên để con **disabled** đến khi có cha (rule/validation) |
| **Cấu hình đúng mà màn không đổi** | Chưa đồng bộ / chưa flush cache | Đồng bộ cấu hình → mở lại màn (cache tự vô hiệu) |

---

## 8. Checklist 1 phút (dán cạnh màn hình khi cấu hình)

- [ ] Cha: Editor **LookupBox** · **🔮 Field ảo = ✓** · **Field Code** đặt rõ · Filter SQL **trống**.
- [ ] Con: Editor **LookupBox** · **Field ảo = ✗** · Filter SQL `= @<FieldCode cha>` · **Tự reload** = `<FieldCode cha>`.
- [ ] `@param` (con) == Field Code (cha), **giống hệt**.
- [ ] Cột đích của con **đã tồn tại** trong bảng (nếu mới → migration trước).
- [ ] **▶ Diễn giải** 4 dòng đúng → Lưu → Đồng bộ → chạy thử cascade + kiểm payload chỉ lưu cấp con.

---

## 9. 🛡️ Guardrail tự động (app tự bắt lỗi — không phụ thuộc trí nhớ)

Hệ đã có sẵn các lớp chặn cấu hình sai, cứ dựa vào chúng:

| Nơi | Guardrail | Bắt lỗi |
|---|---|---|
| **Panel LookupBox** (WPF) | Banner **🛑 Cảnh báo cascade** hiện ngay dưới ô *Tự reload* | `@param` không khớp field cha nào (→ con rỗng); `@param` là cha nhưng chưa đặt *Tự reload* |
| **Tab Behavior** (WPF) | Bật 🔮 Field ảo → ô Cột DB mờ đi; **chặn Lưu** nếu Field ảo thiếu Field Code | Quên Field Code cho field ảo; ghi nhầm Column_Id cho field ảo |
| **▶ Diễn giải** (WPF) | Có dòng "🔮 Field ảo: không lưu DB" + mục "🛑 CẢNH BÁO CASCADE" liệt kê lỗi | Soát tổng thể trước khi lưu |
| **Đồng bộ cấu hình › Xem trước** (web) | Panel **⚠ Cảnh báo cấu hình** liệt kê mọi cascade sai của mọi form | Lưới an toàn cuối trước khi áp xuống tenant |

> Cảnh báo là **advisory** (không chặn cứng Lưu/Đồng bộ) để không cản người cấu hình có chủ đích, nhưng hiện rõ ràng
> ở 4 nơi → rất khó bỏ sót. Thấy banner vàng ⇒ dừng lại sửa theo §3/§7.

## 10. Liên quan

- [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md) — cơ chế runtime (Filter SQL `@FieldCode` + ReloadTriggerField).
- [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) — áp dụng cụ thể màn Công ty (2 cascade + Tiền đề schema).
- [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) — tham chiếu từng ô của panel LookupBox.
- [cau-hinh-bo-loc-lien-ket.md](cau-hinh-bo-loc-lien-ket.md) — cascade ở **panel lọc lưới** (khác: lọc dòng, không phải field trong form).
