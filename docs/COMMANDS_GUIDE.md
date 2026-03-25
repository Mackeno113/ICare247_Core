# ICare247 — Hướng dẫn Slash Commands

> Các lệnh `/command` gõ trực tiếp trong chat với Claude Code.
> Mỗi lệnh là một "kịch bản" tự động — Claude sẽ thực hiện tuần tự các bước được định nghĩa sẵn.

---

## Tổng quan nhanh

| Command | Khi nào dùng |
|---|---|
| `/start-session` | **Đầu mỗi buổi làm việc** — đặc biệt khi chuyển máy |
| `/pick-task` | Không biết làm gì tiếp theo |
| `/finish-task` | Vừa xong một task, muốn commit + cập nhật |
| `/review-changes` | Kiểm tra code trước khi commit |
| `/save-memory` | Lưu quyết định / feedback quan trọng |
| `/design` | Thiết kế UI/UX, Design System, components |

---

## Chi tiết từng command

---

### `/start-session`

**Dùng khi:** Bắt đầu buổi làm việc mới — đặc biệt khi **chuyển giữa 2 máy**.

**Claude sẽ làm:**
1. `git fetch` + kiểm tra branch có bị behind remote không
2. Cảnh báo nếu có uncommitted changes
3. Đọc memory để biết session trước làm gì, đang ở phase nào
4. Đọc TASKS.md — liệt kê task đang dở + 3 task ưu tiên tiếp theo
5. Hỏi hôm nay làm task nào

**Ví dụ output:**
```
Git: master đã sync với remote ✅
Session trước: viết migration 005-009 (chưa chạy DB)
Task đang dở: không có
Nên làm tiếp: 1. Chạy migrations, 2. Cập nhật Domain entities, 3. Repositories
Hôm nay làm task nào?
```

---

### `/pick-task`

**Dùng khi:** Muốn chọn task tiếp theo một cách có hệ thống (theo dependency + priority).

**Claude sẽ làm:**
1. Đọc TASKS.md phần Todo
2. Kiểm tra task nào đang In Progress (nếu có → hỏi bỏ qua hay hoàn thành trước)
3. Liệt kê top 5 task, mỗi task ghi rõ:
   - Ước lượng S (< 1h) / M (1-3h) / L (> 3h)
   - Dependencies (cần làm gì trước)
   - Files cần đọc trước khi code
4. Khi user chọn → tự động move sang 🔴 In Progress

**Khác `/start-session`:** `/pick-task` chuyên biệt hơn — chỉ tập trung chọn task, không check git/memory.

---

### `/finish-task`

**Dùng khi:** Vừa hoàn thành một task, muốn "đóng task" đúng quy trình.

**Claude sẽ làm:**
1. Chạy `dotnet build ICare247.slnx` — nếu fail thì **dừng lại**, sửa lỗi trước
2. Move task từ 🔴 In Progress → ✅ Done trong TASKS.md
3. Cập nhật `last_session.md` — ghi task vừa xong + gợi ý tiếp theo
4. Cập nhật `project_current_phase.md` nếu phase thay đổi
5. Stage tất cả file liên quan
6. Đề xuất commit message → hỏi confirm
7. Hỏi có push không (quan trọng nếu chuyển máy)

**Lưu ý:** Luôn chạy `/finish-task` trước khi tắt máy để máy kia có thể `git pull` đầy đủ.

---

### `/review-changes`

**Dùng khi:** Muốn kiểm tra code trước khi commit — đảm bảo đúng chuẩn ICare247.

**Claude sẽ làm:**
1. Chạy `git diff` + `git diff --cached`
2. Kiểm tra checklist theo từng category:

| Category | Những gì kiểm tra |
|---|---|
| **Architecture** | Layer dependency đúng không, không cross-import |
| **Dapper** | Parameterized query, có Tenant_Id, có Is_Active |
| **Async** | Async/await xuyên suốt, có CancellationToken |
| **Naming** | PascalCase, _camelCase, suffix Async |
| **Comments** | File header, XML doc, tiếng Việt |
| **Security** | Không commit secrets, không hardcode |

3. Báo ✅/❌ từng mục + đề xuất fix cụ thể (file, dòng, cách sửa)

---

### `/save-memory`

**Dùng khi:** Có quyết định thiết kế quan trọng, feedback, hay thay đổi preference cần nhớ qua sessions.

**Claude sẽ làm:**
1. Hỏi muốn lưu gì (nếu chưa rõ)
2. Phân loại và ghi vào đúng file:

| Loại | File |
|---|---|
| Quyết định kiến trúc | `.claude/memory/architecture_decisions.md` |
| Feedback code style | `.claude/memory/coding_style_feedback.md` |
| Tiến độ phase | `.claude/memory/project_current_phase.md` |
| Preference của user | `.claude/memory/user_profile.md` |

3. Nhắc commit + push để sync sang máy khác

**Quan trọng:** Memory nằm trong repo (`.claude/memory/`) — phải push để máy kia đọc được.

---

### `/design [yêu cầu]`

**Dùng khi:** Cần thiết kế UI/UX, Design System, component specs cho ICare247.

**Ví dụ:**
```
/design color palette cho module Nhân sự
/design spec component DxTextBox: states, tokens, accessibility
/design UX pattern cho form có tab
/design dark mode tokens từ palette hiện tại
```

**Claude sẽ làm:**
- Kích hoạt design-agent chuyên biệt
- Output: CSS tokens dùng được ngay, specs đủ states, DevExpress integration notes

Nếu gọi `/design` không có argument → Claude hỏi chọn 1 trong 8 loại thiết kế.

---

## Workflow điển hình

### Buổi sáng, bắt đầu làm việc

```
/start-session
```
→ Biết ngay session trước làm gì, task nào cần làm tiếp.

---

### Chuyển máy (ví dụ: từ máy văn phòng về máy nhà)

**Trước khi rời máy cũ:**
```
/finish-task   ← commit + push
```

**Khi ngồi vào máy mới:**
```
/start-session  ← tự động kiểm tra git, hỏi pull nếu cần
```

---

### Trong khi code

```
/pick-task          ← chọn task tiếp theo
... (code) ...
/review-changes     ← review trước commit
/finish-task        ← đóng task + commit
```

---

### Khi có quyết định quan trọng

```
/save-memory
```
→ Claude hỏi lưu gì, tự ghi vào đúng file memory.

---

## Lưu ý quan trọng

1. **Memory nằm trong repo** — phải commit + push thì máy kia mới đọc được
2. **`/finish-task` luôn build trước** — không commit code lỗi
3. **`/start-session` ≠ `/pick-task`** — cái đầu là "khởi động buổi làm", cái sau là "chọn task tiếp"
4. **Commands có thể gọi bất cứ lúc nào** — không chỉ đầu/cuối session

---

## Vị trí files

```
.claude/
├── commands/           ← định nghĩa các command
│   ├── start-session.md
│   ├── pick-task.md
│   ├── finish-task.md
│   ├── review-changes.md
│   ├── save-memory.md
│   └── design.md
└── memory/             ← dữ liệu nhớ qua sessions (git-tracked)
    ├── MEMORY.md           (index)
    ├── last_session.md     (session trước làm gì)
    ├── project_current_phase.md
    ├── architecture_decisions.md
    ├── coding_style_feedback.md
    └── user_profile.md
```
