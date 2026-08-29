# ICare247_Core — Technical Audit (2026-08-29)

Đây là kết quả audit kỹ thuật toàn diện dự án, thực hiện read-only bằng 9 agent song song (kiến trúc backend, kiến trúc Blazor, kiến trúc WPF ConfigStudio, bảo mật, hiệu năng, database/migration, test coverage, tài liệu & mâu thuẫn, công việc dở dang/TODO). Không có file source nào bị sửa trong quá trình audit.

## Cách đọc report

1. **Cách nhanh nhất:** chạy 1 local server rồi mở `index.html` qua `http://localhost` (xem mục "Chạy dashboard" bên dưới). Dashboard đọc dữ liệu từ `data/*.json` bằng `fetch()` — trình duyệt chặn `fetch()` khi mở trực tiếp bằng `file://`, nên bắt buộc phải qua HTTP.
2. **Đọc nhanh dạng text:** mở [`AUDIT_SUMMARY.md`](AUDIT_SUMMARY.md) — tóm tắt điều hành, top vấn đề, roadmap đề xuất.
3. **Tra cứu chi tiết theo mục:** mở trực tiếp file JSON tương ứng trong `data/` (mỗi file có schema nhất quán, ID ổn định để so sánh giữa các đợt audit).

## Chạy dashboard

Trình duyệt chặn `fetch()` khi mở `index.html` bằng `file://`. Chạy 1 trong các cách sau từ thư mục `docs/project-audit/`:

```bash
python -m http.server 8791
```

hoặc dùng extension "Live Server" của VS Code, hoặc bất kỳ static file server nào khác. Sau đó mở `http://localhost:8791`.

## Cấu trúc dữ liệu (`data/*.json`)

| File | Nội dung |
|---|---|
| `project.json` | Metadata dự án: tech stack, module chính, overall health |
| `architecture.json` | Dependency graph thật, bảng tuân thủ hard constraint (BRAIN.md), bản đồ 4 engine, architecture drift, danh sách file lớn |
| `documents.json` | Kiểm kê ~185 file tài liệu theo nhóm, bản đồ Single-Source-of-Truth theo domain |
| `conflicts.json` | 8 mâu thuẫn tài liệu (DOC-001..008) — file A vs file B, bằng chứng, đề xuất xử lý |
| `tasks.json` | Trạng thái công việc thật (đối chiếu code vs TASKS.md/ADR) — DONE/PARTIALLY_DONE/TODO/BLOCKED/UNKNOWN |
| `security.json` | Finding bảo mật (SEC-001..007) theo OWASP + phần "reviewed, no issue" |
| `performance.json` | Finding hiệu năng (PERF-001..008), phân biệt rõ "confirmed" vs "potential risk" |
| `database.json` | Timeline 113 migration, phân loại Config DB/Data DB, finding DB (DB-001..006) |
| `code-quality.json` | TODO/FIXME/HACK sweep, kiểm tra interface không implementation, bảng chưa dùng |
| `tests.json` | Test inventory (175 test, backend only), coverage theo module, finding (TEST-001..004) |
| `dependencies.json` | Ghi chú — audit dependency KHÔNG được thực hiện đầy đủ trong đợt này, xem ghi chú trong file |
| `technical-debt.json` | Nợ kỹ thuật (DEBT-001..012) — file lớn, dead code, tính nhất quán |
| `recommendations.json` | Ưu tiên P0-P3 + roadmap 6 phase |

Mọi finding dùng chung schema: `id, category, title, severity, confidence, status, module, description, evidence[], impact, recommendation, relatedItems[]`. ID ổn định giữa các lần audit (không đổi số khi re-run) để có thể so sánh tiến độ.

## Cập nhật audit lần sau

- **Không ghi đè thư mục này ngay.** Tạo snapshot bằng cách copy `docs/project-audit/` sang `docs/project-audit-YYYY-MM-DD/` trước khi audit lại, hoặc dùng git để track lịch sử thay đổi của các file JSON (đã là git-tracked nếu bạn commit thư mục này).
- Giữ nguyên ID (`SEC-001`, `ARCH-001`, ...) cho finding chưa xử lý; chỉ thêm ID mới cho finding mới phát hiện. Khi 1 finding đã fix, cập nhật `status` thành `"resolved"` thay vì xóa — để giữ lịch sử.
- `recommendations.json` nên được đối chiếu lại mỗi lần: finding nào đã fix thì bỏ khỏi danh sách ưu tiên.

## Giới hạn của audit này

- **Dependency audit chưa đầy đủ** — xem `data/dependencies.json`, cần chạy `dotnet list package --vulnerable --outdated` riêng.
- **docs/backend-debug/, docs/codes/, docs/reference/** chưa được spot-check nội dung (chỉ kiểm kê).
- Một số finding hiệu năng ở mức DB (vd `sp_SinhMa` lock contention) được đánh dấu "potential" vì không chạy execution plan thật.
- Audit không chạy code, không kết nối DB thật — mọi kết luận dựa trên đọc source/migration/tài liệu tĩnh.
