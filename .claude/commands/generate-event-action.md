# /generate-event-action — Sinh Evt_Definition + Evt_Action

**Mục đích:** Sinh event handler (trigger + action) sinh UiDelta cho client.
**Input:** `$ARGUMENTS` = trigger + hành động (vd "khi LoaiHopDong=ThoiVu thì ẩn field NgayKetThuc").
**Output:** Evt_Definition + Evt_Action (theo Evt_Action_Type + param schema) + ghi Evt_Execution_Log.
**Agent gọi:** `event-engine`.

Thực hiện:
1. Đọc `docs/spec/04_ENGINE_SPEC.md` + `05_ACTION_RULE_PARAM_SCHEMA.md`.
2. Gọi `Agent` với `subagent_type: event-engine`, yêu cầu sinh definition + action cho `$ARGUMENTS` (điều kiện qua AST whitelist; tái dùng action type; mọi action log execution).
3. Output là UiDelta (client áp), không render server-side. KHÔNG tự commit.
