# Blazor UI — Hiệu năng render (input & re-render)

## Quy tắc chính — KHÔNG dùng `@bind:event="oninput"` trên trang/popup nặng

Trên trang hoặc popup có **component nặng** (`DxGrid`/`DxTreeList`, `TreeSelectBox`,
nhiều `DxComboBox`, vòng `@foreach` render nhiều phần tử…), **KHÔNG** dùng
`@bind:event="oninput"` cho input text.

> Mỗi keystroke với `oninput` → cập nhật biến → `StateHasChanged` → **render lại
> TOÀN BỘ component cha** (cả cây, combobox, lưới icon…). Trên trang nặng = lag/khựng khi gõ.

```razor
@* ❌ SAI — mỗi ký tự re-render cả trang (lag) *@
<input class="mb-input" @bind="_ten" @bind:event="oninput" />

@* ✅ ĐÚNG — bind mặc định (onchange): chỉ commit khi rời ô, không re-render lúc gõ *@
<input class="mb-input" @bind="_ten" />
```

- `@bind` mặc định = sự kiện `onchange` (commit khi blur). Bấm nút "Lưu" làm ô mất
  focus trước → `onchange` chạy → biến cập nhật → rồi mới xử lý click ⇒ **lưu vẫn đúng**.
- Trước khi đặt `oninput`, tự hỏi: *"Có thật sự cần phản hồi theo TỪNG ký tự không?"*
  Placeholder/preview phụ trợ cập nhật khi blur là **đủ** — không đáng đánh đổi lag.

## Khi THỰC SỰ cần phản hồi live theo từng ký tự

Chỉ khi có nhu cầu rõ ràng (validate tức thời, preview khóa/slug theo input…), chọn 1 trong:

1. **Debounce** — `DxTextBox` với `BindValueMode="OnInput"` + `InputDelay="300"` (gộp
   keystroke, không render mỗi phím).
2. **Tách component con** — đưa ô input + vùng hiển thị live vào 1 component riêng, để
   `StateHasChanged` chỉ re-render component đó, KHÔNG đụng trang nền (cây/lưới).
3. **Tách popup thành component riêng** — toàn bộ form sửa là 1 component; gõ trong popup
   không re-render trang nền chứa `DxTreeList`.

> Lưu ý: giá trị suy-một-lần (vd khóa i18n `_navKey` tính lúc MỞ popup) thì KHÔNG cần
> live — đừng để nó kéo theo `oninput`.

## Liên quan — chiều cao lưới để GHIM header

`DxGrid`/`DxTreeList` muốn **ghim hàng tiêu đề** khi cuộn: cho component một **chiều cao
giới hạn** (vd `height: calc(100vh - 220px); min-height: 320px;` qua `CssClass`) → DevExpress
sinh vùng cuộn nội bộ + header cố định. Không set height → lưới mọc hết cỡ, cả trang cuộn,
header trôi. Pattern chuẩn: `.dv-dxgrid` (`wwwroot/css/app.css`).

## Checklist review (Blazor)

- [ ] Không có `@bind:event="oninput"` trên input của trang/popup nặng (trừ khi đã debounce/cô lập).
- [ ] Input commit-on-change đủ dùng (không cần live từng ký tự).
- [ ] `DxGrid`/`DxTreeList` cuộn nhiều → đã set height để ghim header.
- [ ] Mọi chuỗi UI qua `Loc.L` (xem skill `icare247-admin-ui` + `references/i18n.md`).

## Tham chiếu

- Bug gốc: màn Quản lý menu (`Pages/Admin/MenuBuilderPage.razor`) — commit `8fb6047`.
- Pattern ghim header: commit `09d14bb`.
- Nguồn chuẩn UI admin (grid/treelist/i18n): skill `.claude/skills/icare247-admin-ui/`.
