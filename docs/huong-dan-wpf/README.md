# Hướng dẫn WPF — ConfigStudio

> Mục lục các tài liệu **hướng dẫn cấu hình trên ConfigStudio (WPF)** — cách dùng app và cách cấu hình no-code từng loại màn/field. Gom riêng khỏi `docs/spec/` (đặc tả kỹ thuật) và `docs/codes/` (luồng mã nguồn).

## Bắt đầu
- [ConfigStudio_User_Guide.md](ConfigStudio_User_Guide.md) — Hướng dẫn sử dụng toàn bộ app: Form Manager, Field Config, Rule/Event Editor, Expression Builder, i18n Manager, Publish, Settings.

## Cấu hình màn hình (no-code)
- [cau-hinh-man-danh-muc.md](cau-hinh-man-danh-muc.md) — Màn danh mục engine-driven (master data phẳng).
- [cau-hinh-man-cong-ty.md](cau-hinh-man-cong-ty.md) — Màn Công ty (lưới cây + popup, master-detail tổ chức).
- [cau-hinh-man-quan-ly-view.md](cau-hinh-man-quan-ly-view.md) — Màn Quản lý View (Grid / Tree Grid).
- [cau-hinh-menu.md](cau-hinh-menu.md) — Màn Quản lý menu (HT_ChucNang).

## Cấu hình field / control
- [cau-hinh-lookupbox.md](cau-hinh-lookupbox.md) — Tham chiếu đầy đủ **từng ô** của panel LookupBox/TreeLookupBox.
- [cau-hinh-luoi-tham-chieu.md](cau-hinh-luoi-tham-chieu.md) — Dựng lưới dữ liệu tham chiếu (khóa ngoại): **Cách A** FK auto-JOIN (no-code, không cần view) + **Cách B** SQL View tay (escape hatch).
- [cau-hinh-field-ao-cascade.md](cau-hinh-field-ao-cascade.md) — **Hướng dẫn sử dụng field ảo + cascade** (chọn cha để lọc con, chỉ lưu con) — quy tắc vàng + bảng lỗi thường gặp để **chống cấu hình sai**.
- [cau-hinh-bo-loc-lien-ket.md](cau-hinh-bo-loc-lien-ket.md) — Bộ lọc liên kết (cascade) + lọc theo tài khoản + đổ giá trị Thêm mới.

## Tham chiếu liên quan (vẫn ở `docs/spec/` — giữ đánh số spec)
- [09_FIELD_CONFIG_GUIDE.md](../spec/09_FIELD_CONFIG_GUIDE.md) — Tổng quan tab Cơ bản / Control Props / Rules / Events.
- [12_CASCADE_LOOKUP_GUIDE.md](../spec/12_CASCADE_LOOKUP_GUIDE.md) — Cascade Tỉnh/Thành → Xã/Phường.
- [13_LOOKUP_ADD_NEW_GUIDE.md](../spec/13_LOOKUP_ADD_NEW_GUIDE.md) — Thêm mới entity ngay trên LookupBox.
- [23_VALIDATION_RULE_GUIDE.md](../spec/23_VALIDATION_RULE_GUIDE.md) — Cấu hình Validation Rule.
