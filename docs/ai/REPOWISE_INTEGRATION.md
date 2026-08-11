# RepoWise Integration — ICare247

## Trạng thái

- Chế độ: pilot bổ sung cho GitNexus, không thay thế.
- Phiên bản pin: `repowise==0.34.0`.
- Triển khai: self-hosted, MCP `stdio`, index local.
- LLM/provider: tắt; chỉ dùng `--index-only`.
- Telemetry: ép tắt bằng `REPOWISE_TELEMETRY_DISABLED=1` trong wrapper.
- Hook/plugin/dashboard/hosted service: không dùng.

## Phân vai

| Nhu cầu | Công cụ chính |
|---|---|
| Symbol impact, caller/callee, execution flow, PDG/taint, rename | GitNexus |
| Hotspot, co-change, ownership, change risk, code health | RepoWise |
| Ảnh hưởng metadata runtime trong DB/AST JSON | Spec canonical + DB thật |

Quy tắc `impact` trước khi sửa symbol và `detect_changes` trước commit trong `AGENTS.md`
vẫn bắt buộc. Kết quả RepoWise chỉ là tín hiệu bổ sung.

## Tool MCP được phép

- `get_overview`
- `get_context`
- `get_risk`
- `get_change_risk`
- `get_why`
- `get_health`

`get_dead_code`, refactoring code generation và execution-flow của RepoWise bị tắt trong pilot.
Kết quả dead-code không được dùng để xóa code vì ICare247 có DI, Prism navigation, Blazor renderer
và quan hệ metadata được resolve động.

## Cài đặt trên máy mới

Cần Python 3.12. Tạo virtual environment riêng, không cài vào Python của ứng dụng:

```powershell
python -m venv .local-tools\repowise
.\.local-tools\repowise\Scripts\python.exe -m pip install "repowise==0.34.0"
```

Tạo index:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\repowise\init-index.ps1
```

Restart Codex/Claude sau khi cài để nạp MCP project-local.

## Vận hành

```powershell
# Kiểm tra index
powershell -NoProfile -ExecutionPolicy Bypass -File tools\repowise\invoke.ps1 status

# Cập nhật index sau khi HEAD thay đổi
powershell -NoProfile -ExecutionPolicy Bypass -File tools\repowise\invoke.ps1 update

# Xem health
powershell -NoProfile -ExecutionPolicy Bypass -File tools\repowise\invoke.ps1 health

# Đánh giá risk cho commit hoặc range
powershell -NoProfile -ExecutionPolicy Bypass -File tools\repowise\invoke.ps1 risk HEAD~1..HEAD
```

## Baseline ngày 2026-08-09

- 1.028 file được index.
- 9.524 symbol.
- Graph: 11.675 node, 18.388 edge.
- Git: 702 file, 76 hotspot.
- Health trung bình: 8,33/10; 1.336 finding.
- Index local: khoảng 54 MB.
- Full index lần đầu: 4 phút 19 giây trên máy hiện tại.
- Decision intelligence: chưa bật trong pilot (`index-only` cho kết quả 0 decision).
- Dead-code baseline: 142 file unreachable và 219 unused export; chỉ xem là candidate.

## Bảo mật và governance

1. Không đặt API key trong `.repowise/`, `.mcp.json` hoặc script.
2. `.repowise/` và virtual environment local phải nằm trong `.gitignore`.
3. Không chạy `repowise init --codex`; lệnh này có thể tạo hook và managed `AGENTS.md`.
4. Không chạy `repowise hook install` hoặc `repowise hook rewrite install`.
5. Không dùng hosted service với private source khi chưa có phê duyệt security/license riêng.
6. Không coi wiki/decision do công cụ sinh là SSOT; `BRAIN.md`, ADR và `docs/spec/` vẫn canonical.

## Rollback

1. Xóa entry MCP RepoWise trong `.mcp.json` và `.codex/config.toml`.
2. Xác minh đúng đường dẫn rồi xóa `.repowise/` và `.local-tools/repowise/`.
3. Xóa `tools/repowise/` và tài liệu này.
4. Không có migration DB, NuGet dependency hoặc runtime code cần phục hồi.
