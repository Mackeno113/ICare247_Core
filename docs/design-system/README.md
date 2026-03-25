# ICare247 Design System v1.0

**Brand:** "I Care 24/7" | **Style:** Colorful · Playful · Approachable
**Stack:** Blazor WASM + DevExpress | **Mode:** Light only

---

## Brand Story

> **"I Care 24/7"** = Tôi luôn ở đây, luôn quan tâm đến bạn

| Yếu tố | Giá trị |
|---|---|
| Personality | Ấm áp · Tận tâm · Luôn sẵn sàng |
| Style | Colorful · Playful · Approachable |
| Color strategy | Multi-color brand, 3 màu chủ đạo |
| Logo | Gradient 135deg: Coral → Violet → Teal |
| UI | Discrete per-module · Light base |

---

## Brand Colors

| Màu | Hex | Ý nghĩa |
|---|---|---|
| **Coral** | `#FF6B6B` | "I Care" — Warmth, humanity, energy |
| **Violet** | `#845EF7` | Platform — Creative, innovative, bridge |
| **Teal** | `#00C9A7` | "247" — Reliable, always fresh |

### Logo Gradient
```css
background: linear-gradient(135deg, #FF6B6B 0%, #845EF7 50%, #00C9A7 100%);
```

---

## Typography

| Role | Font | Weight |
|---|---|---|
| Heading (H1–H3) | Plus Jakarta Sans | 700 |
| Subheading (H4–H6) | Plus Jakarta Sans | 600 |
| Label / Button | Plus Jakarta Sans | 500–600 |
| Body text | Inter | 400 |
| Caption / Helper | Inter | 400 |
| Code | JetBrains Mono | 400 |

### Type Scale
| Token | Size | Dùng cho |
|---|---|---|
| `--text-xs` | 12px | Caption, helper text |
| `--text-sm` | 14px | Body, label, button |
| `--text-base` | 16px | Body dài |
| `--text-lg` | 18px | Lead text |
| `--text-xl` | 20px | H4–H5 |
| `--text-2xl` | 24px | H3 |
| `--text-3xl` | 30px | H2 |
| `--text-4xl` | 36px | H1 |
| `--text-5xl` | 48px | Display / Hero |

---

## Spacing (8px base)

| Token | Value | Dùng cho |
|---|---|---|
| `--space-1` | 4px | Micro gap |
| `--space-2` | 8px | Icon gap, tight padding |
| `--space-3` | 12px | Input padding vertical |
| `--space-4` | 16px | Card padding, section gap |
| `--space-6` | 24px | Card padding large |
| `--space-8` | 32px | Section spacing |
| `--space-12` | 48px | Page section |
| `--space-16` | 64px | Large section |

---

## Border Radius

| Token | Value | Dùng cho |
|---|---|---|
| `--radius-sm` | 4px | Badge nhỏ, tag |
| `--radius-md` | 8px | **Button, Input** ← default |
| `--radius-lg` | 12px | **Card, Dropdown** |
| `--radius-xl` | 16px | Panel, Sidebar items |
| `--radius-2xl` | 24px | **Modal, Dialog** |
| `--radius-full` | 9999px | Pill badge, Avatar |

---

## Component Specs

### Button

| State | Visual |
|---|---|
| Default | bg: `--color-violet-500`, text: white |
| Hover | bg: `--color-violet-600` |
| Active | bg: `--color-violet-700` + scale 0.98 |
| Focus | outline 2px `--color-violet-500` + offset 2px |
| Disabled | bg: `--color-neutral-200`, text: `--color-neutral-400` |
| Loading | Spinner + opacity 0.8 + pointer-events: none |

**Sizes:** `sm` 32px · `md` 40px ← default · `lg` 48px

### Input

| State | Visual |
|---|---|
| Default | border: `--border-default` |
| Hover | border: `--color-neutral-300` |
| Focus | border: `--color-violet-500` + shadow brand |
| Error | border: `--color-error` + shadow error |
| Disabled | bg: `--color-neutral-100`, text: `--text-disabled` |

### Card
- bg: white, border: `--border-default`, radius: `--radius-lg`
- shadow: `--shadow-sm` → hover: `--shadow-md`
- padding: `--space-4` (sm) · `--space-6` (md) · `--space-8` (lg)

### Table
- Header: bg `--color-neutral-50`, text uppercase 12px semibold
- Row height: 48px
- Hover: `--color-neutral-50`
- Selected: `--color-violet-50`

---

## Module Color System

Module colors được assign khi có danh sách module. Placeholder trong `tokens.css`:

```css
--color-module-1: var(--color-coral-500);   /* TBD */
--color-module-2: var(--color-violet-500);  /* TBD */
--color-module-3: var(--color-teal-500);    /* TBD */
```

Khi thêm module mới (> 3), mở rộng palette với màu bổ sung:
- Module 4: `#F59E0B` (Amber)
- Module 5: `#3B82F6` (Sky Blue)

---

## Accessibility

- Text contrast: ≥ 4.5:1 (WCAG AA)
- UI elements: ≥ 3:1
- Focus indicator: 2px outline `--color-violet-500`
- Touch target: tối thiểu 44×44px
- Disabled: dùng `aria-disabled` thay `disabled` attribute

---

## Cách dùng

### 1. Import tokens
```html
<!-- wwwroot/index.html -->
<link rel="stylesheet" href="css/tokens.css" />
```

### 2. Dùng trong component
```css
.my-button {
  height: var(--btn-height-md);
  background: var(--btn-primary-bg);
  border-radius: var(--btn-radius);
  font-family: var(--font-heading);
}
```

### 3. Module accent
```html
<div class="accent-module-1">
  <!-- UI dùng var(--module-color) cho accent elements -->
</div>
```

---

## Files

| File | Mục đích |
|---|---|
| `tokens.css` | Toàn bộ CSS custom properties |
| `README.md` | Documentation này |
