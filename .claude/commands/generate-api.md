# /generate-api — Sinh endpoint + DTO (RFC 7807)

**Mục đích:** Sinh API endpoint + DTO cho 1 use case, đúng contract + response format.
**Input:** `$ARGUMENTS` = mô tả endpoint (method, route, mục đích) hoặc Query/Command có sẵn.
**Output:** endpoint (controller/handler) + request/response DTO + lỗi qua ProblemDetails.
**Agent gọi:** `backend-dapper-expert`.

Thực hiện:
1. Đọc `docs/spec/07_API_CONTRACT.md` + `.claude-rules/api-response.md` + 1 endpoint hiện có cùng module.
2. Gọi `Agent` với `subagent_type: backend-dapper-expert`, yêu cầu sinh endpoint + DTO (Api KHÔNG import Infrastructure; CQRS; ProblemDetails RFC 7807).
3. Báo file đã tạo; gợi ý `/review-security` + `/generate-tests`. KHÔNG tự commit.
