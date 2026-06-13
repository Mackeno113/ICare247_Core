# Cài đặt Redis (cho audit log + cache L2)

> Audit log (nhật ký hoạt động) dùng **Redis Stream** làm bộ đệm bền: request đẩy log vào Redis,
> tiến trình nền ghi xuống `NK_NhatKyHoatDong`. Redis **chưa bắt buộc để chạy app** — chưa có Redis
> thì pipeline tự ghi thẳng DB (kém bền hơn). Bật Redis để có độ bền + scale-out nhiều node.
> Cùng Redis này cũng dùng cho cache L2 (`ConnectionStrings:Redis`).

## Chọn cách cài (Windows)

| Cách | Khi nào dùng | Ghi chú |
|---|---|---|
| **Docker Desktop** (khuyên dùng) | Có Docker | Nhanh, sạch, dễ xoá |
| **Memurai** | Không dùng Docker | Bản Redis-compatible chạy native Windows (có bản Developer free) |
| **WSL2 + redis-server** | Đã có WSL | Cài `apt install redis` trong Ubuntu WSL |
| Redis trên server Linux | Đã có sẵn server | Trỏ connstring tới host đó |

### Cách 1 — Docker (nhanh nhất)
```powershell
docker run -d --name ic247-redis -p 6379:6379 --restart unless-stopped redis:7
# kiểm tra:
docker exec -it ic247-redis redis-cli ping     # → PONG
```

### Cách 2 — Memurai (native Windows)
1. Tải Memurai Developer: https://www.memurai.com/get-memurai
2. Cài (chạy như Windows Service, cổng mặc định 6379).
3. Kiểm tra: `memurai-cli ping` → `PONG`.

### Cách 3 — WSL2
```bash
sudo apt update && sudo apt install -y redis-server
sudo service redis-server start
redis-cli ping        # → PONG
```

## Khai báo connection string (ngoài repo)

Mở `%APPDATA%\ICare247\Api\appsettings.local.json`, điền `ConnectionStrings:Redis`:
```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```
- Có mật khẩu: `localhost:6379,password=your_password`.
- Server khác: `10.0.0.5:6379` hoặc `redis.example.com:6379,ssl=true`.
- Cú pháp đầy đủ theo StackExchange.Redis: https://stackexchange.github.io/StackExchange.Redis/Configuration

> Để **trống** = không dùng Redis → audit ghi thẳng DB (fallback), cache chỉ còn L1 in-memory.

## Kiểm tra app đã nhận Redis

Khởi động API, xem console log lúc bootstrap:
- `ConnectionChecker` in trạng thái Redis.
- Audit pipeline log: dùng `Redis Stream` hay `ghi trực tiếp DB (fallback)`.

Sau khi đăng nhập vài lần, kiểm Redis Stream + bảng nhật ký:
```powershell
docker exec -it ic247-redis redis-cli XLEN ic247:audit      # số entry trong stream
```
```sql
SELECT TOP 20 * FROM dbo.NK_NhatKyHoatDong ORDER BY Id DESC;  -- trên DB Audit riêng (vd ICare247_Solution_Audit)
```

## Gỡ / dừng
```powershell
docker stop ic247-redis ; docker rm ic247-redis     # Docker
```
