# /design — Web UI Design Agent

Kích hoạt **design-agent** để thiết kế giao diện web cho ICare247.

## Cách dùng

```
/design [yêu cầu của bạn]
```

### Ví dụ

```
/design thiết kế color palette cho ICare247 — healthcare app
/design tạo Design System tokens đầy đủ cho Blazor + DevExpress
/design spec component Button: primary, secondary, danger variants
/design UX pattern cho form validation và error feedback
/design layout system: grid, spacing scale, breakpoints
/design configuration control spec cho DxTextBox
/design dark mode tokens từ palette hiện tại
```

---

Hãy phân tích yêu cầu sau và đưa ra thiết kế hoàn chỉnh:

**Yêu cầu:** $ARGUMENTS

Nếu `$ARGUMENTS` trống, hỏi user muốn thiết kế phần nào:
1. **Color Palette & Brand Identity** — màu sắc, nhận diện thương hiệu
2. **Design Tokens** — toàn bộ tokens: color, typography, spacing, shadow
3. **Layout System** — grid, breakpoints, spacing scale
4. **Component Spec** — một hoặc nhiều components cụ thể
5. **UX Patterns** — patterns tương tác, feedback, navigation
6. **Design Controls** — visual specs cho controls
7. **Configuration Controls** — JSON config params cho controls
8. **Full Design System** — toàn bộ từ đầu đến cuối

Output theo đúng format của design-agent: tokens có thể dùng ngay, specs đủ states, DevExpress integration notes, accessibility checklist.
