Review code thay đổi hiện tại theo tiêu chuẩn ICare247. Thực hiện:

1. Chạy `git diff` để xem tất cả thay đổi (staged + unstaged)
2. Chạy `git diff --cached` để xem staged changes riêng
3. Kiểm tra từng file theo checklist:

**Architecture:**
- [ ] Layer dependency đúng (Domain ← App ← Infra)
- [ ] Không import Infrastructure trong Api (trừ Program.cs)
- [ ] Mỗi file = 1 class/interface/record

**Dapper:**
- [ ] Parameterized query (không string interpolation)
- [ ] Có Tenant_Id trong WHERE
- [ ] Có Is_Active = 1 cho soft-delete tables
- [ ] Dùng CommandDefinition với CancellationToken
- [ ] Không SELECT *
- [ ] Dùng IDbConnectionFactory (không new SqlConnection)

**Async:**
- [ ] Async/await xuyên suốt
- [ ] CancellationToken truyền đủ
- [ ] Không .Result, .Wait()

**Naming:**
- [ ] PascalCase cho class/method/property
- [ ] _camelCase cho private field
- [ ] Suffix Async cho async method
- [ ] Query/Command/Handler naming convention

**Comments (tiếng Việt):**
- [ ] File header (File, Module, Layer, Purpose)
- [ ] XML doc cho public method
- [ ] Logic block comments cho code phức tạp

**Security:**
- [ ] Không commit secrets (.env, connection strings, JWT keys)
- [ ] Không hardcode string SQL

4. Báo cáo kết quả: ✅/❌ cho mỗi mục + tổng hợp
5. Đề xuất fix cụ thể nếu có issue (file, dòng, cách sửa)
