# I18nScanner — công cụ quét key i18n hand-coded

Quét toàn bộ frontend Blazor (`src/frontend`), trích mọi lời gọi
`LocalizationService.L("key", "fallback")` và dò chuỗi tiếng Việt **hardcode chưa bọc i18n**.

## Chạy

```bash
# Dry-run (chỉ in tổng kết, không ghi file)
dotnet run --project tools/I18nScanner

# Ghi thật: catalog.json + merge {lang}.json + báo cáo
dotnet run --project tools/I18nScanner -- --lang en --write
```

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| `--root <dir>` | `src/frontend` | Thư mục frontend cần quét |
| `--lang <code>` | `en` | Ngôn ngữ để merge khung `{lang}.json` |
| `--write` | (tắt) | Bật mới thực sự ghi file |

## Xuất ra (mỗi i18n-root = project có `wwwroot/i18n`)

- **`catalog.json`** — `key → text gốc vi → nơi dùng (file:line) → cờ key động`.
  Màn **Dev ▸ Tra cứu i18n** (`/dev/i18n`) fetch file này để tra ngược *text → key*.
- **`{lang}.json`** — merge: thêm key thiếu (value rỗng = chưa dịch), **giữ nguyên** bản dịch cũ,
  **không xoá** key lạ (chỉ cảnh báo).
- **`i18n-report.md`** — danh sách chuỗi VN hardcode cần bọc + các lời gọi `L()` key dựng động.

## Quan hệ với runtime (phương án A)

Scanner tĩnh **không** thấy key dựng động `L($"x.{code}", …)`. Phần đó do runtime vét:
`LocalizationService.SeenKeys` ghi mọi key thực render; bật **"Soi key"** ở màn `/dev/i18n`
để hiện `value ⟨key⟩` ngay trên mọi màn. Catalog (B) + SeenKeys (A) bù chéo nhau.

> Chạy lại scanner sau khi thêm/sửa chuỗi `L()` để cập nhật catalog + khung dịch.
