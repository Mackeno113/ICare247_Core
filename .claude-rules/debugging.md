# Debugging Rules — ICare247

## Nguyên tắc cốt lõi

**KHÔNG fix ở nơi lỗi hiện ra — truy ngược đến root cause rồi mới sửa.**
Vá tại điểm triệu chứng chỉ che dấu vấn đề, bug sẽ tái xuất ở chỗ khác.
Xem thêm [[feedback-fix-root-logic-not-cache]] (memory).

## Quy trình truy root cause

1. **Quan sát triệu chứng** — đọc kỹ error/log/stack trace, đừng lướt qua.
2. **Tìm nguyên nhân trực tiếp** — dòng code nào ném lỗi/trả sai kết quả?
3. **Hỏi "cái gì gọi nó?"** — đi ngược 1 tầng caller.
4. **Lặp lại bước 3** cho đến khi tìm được điểm phát sinh dữ liệu/trạng thái gốc
   (VD: `Tenant_Id` null vì query cha chưa filter → filter cha đó lại rỗng vì
   session chưa set → session chưa set vì middleware auth chạy sai thứ tự).
5. **Sửa tại điểm gốc**, không sửa tại nơi triệu chứng hiện ra.

**Khi không truy được bằng mắt** — thêm log tạm tại từng biên (boundary) của
luồng đa lớp (API → Handler → Repository → DB) để xem dữ liệu sai từ lớp nào:

```csharp
DebugLogger.Log("Trace", $"Input={input}, Tenant_Id={tenantId}, CallStack={Environment.StackTrace}");
```

(dùng `DebugLogger`, không `Console.WriteLine` — xem `.claude-rules/debug-logger.md`)

## Quy tắc dừng lại — nghi ngờ kiến trúc

**≥ 3 lần fix thất bại cho cùng 1 bug = không phải bug nữa, là vấn đề kiến trúc.**

- Mỗi fix thất bại lộ ra 1 chỗ coupling/shared-state khác → dấu hiệu kiến trúc sai.
- KHÔNG thử fix lần thứ 4. Dừng lại, trình bày với user: pattern hiện tại có ổn
  không, hay cần refactor.
- 1 giả thuyết tại 1 thời điểm — sửa 1 thứ, verify, rồi mới sửa tiếp. Không gộp
  nhiều thay đổi rồi chạy test 1 lần (không biết cái nào có tác dụng).

## Defense-in-depth — validate nhiều lớp sau khi tìm ra root cause

Sau khi sửa tại gốc, validate thêm ở MỌI lớp dữ liệu đi qua để bug **không thể**
tái diễn dù bị bypass do code path khác/refactor sau này:

| Lớp | Vai trò | Ví dụ |
|---|---|---|
| 1. Entry point | Chặn input rõ ràng sai tại biên API/Controller | `[Required]`, FluentValidation trên Command/Query |
| 2. Business logic | Đảm bảo dữ liệu hợp lý cho đúng nghiệp vụ | Handler kiểm tra `Tenant_Id`/`Ma` không rỗng trước khi query |
| 3. Environment guard | Chặn thao tác nguy hiểm theo context | Chặn ghi vào DB Config từ code chạy trong context tenant |
| 4. Debug logging | Lưu vết để điều tra nếu 3 lớp trên vẫn lọt | `DebugLogger.Warn(...)` trước thao tác nhạy cảm |

Một lớp validate = "đã fix bug". Nhiều lớp = "bug không thể xảy ra nữa" — vì
mock, refactor, hoặc code path khác đều có thể bypass 1 lớp đơn lẻ.

## Cờ đỏ — dừng lại nếu đang nghĩ

| Suy nghĩ | Thực tế |
|---|---|
| "Vá nhanh rồi điều tra sau" | Vá trước = quên điều tra sau, bug quay lại |
| "Chắc là do X, sửa X xem sao" | Đoán ≠ điều tra. Truy ngược trước khi sửa |
| "Đổi vài chỗ cùng lúc rồi chạy thử" | Không biết cái nào có tác dụng, dễ sinh bug mới |
| "Đơn giản mà, không cần truy kỹ" | Bug đơn giản vẫn có root cause riêng |
| "Thử thêm 1 lần nữa" (đã thử ≥ 2 lần) | ≥ 3 lần thất bại = vấn đề kiến trúc, không phải "thử thêm" |

## Liên kết

- `.claude-rules/debug-logger.md` — dùng `DebugLogger` cho lớp 4 (debug logging)
- Memory `feedback-fix-root-logic-not-cache` — verify DB/state live trước khi đoán stale
- Memory `feedback-verify-live-db-schema` — root cause trong DB phải đọc schema thật, không đoán theo file `.sql` cũ
