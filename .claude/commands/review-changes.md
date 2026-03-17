Review code thay đổi hiện tại theo tiêu chuẩn ICare247. Thực hiện:

1. Chạy `git diff` để xem tất cả thay đổi
2. Kiểm tra từng file theo checklist:

**Architecture:**
- [ ] Layer dependency đúng (Domain ← App ← Infra)
- [ ] Không import Infrastructure trong Api (trừ Program.cs)

**Dapper:**
- [ ] Parameterized query (không string interpolation)
- [ ] Có Tenant_Id trong WHERE
- [ ] Có Is_Active = 1 cho soft-delete tables
- [ ] Dùng CommandDefinition với CancellationToken
- [ ] Không SELECT *

**Async:**
- [ ] Async/await xuyên suốt
- [ ] CancellationToken truyền đủ
- [ ] Không .Result, .Wait()

**Naming:**
- [ ] PascalCase cho class/method/property
- [ ] _camelCase cho private field
- [ ] Suffix Async cho async method

**Comments:**
- [ ] File header tiếng Việt
- [ ] XML doc cho public method
- [ ] Logic block comments

3. Báo cáo kết quả: pass/fail cho mỗi mục
4. Đề xuất fix nếu có issue
