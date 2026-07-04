# Hướng dẫn sử dụng — **Field ảo + Cascade** (chống cấu hình sai) — ConfigStudio

> Áp dụng mọi màn cần **chọn cấp cha để lọc cấp con, nhưng chỉ lưu cấp con** vào DB.
> Ví dụ xuyên suốt: **Ngân hàng → Chi nhánh ngân hàng** và **Tỉnh/Thành → Phường/Xã** (màn Công ty).
>
> Mục tiêu tài liệu: làm **đúng ngay lần đầu**. Mỗi bước kèm **lỗi hay gặp** + cách phát hiện sớm.
> Nền tảng cơ chế: [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md) · [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md).

---

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
