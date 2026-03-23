# Debug Logger — Rule

## Quy tắc

Mọi log debug/trace trong code (KHÔNG phải Serilog) **bắt buộc** dùng `DebugLogger` thay vì `Console.WriteLine`.

```csharp
// ❌ SAI
Console.WriteLine($"[LocalConfig] Nạp cấu hình từ: {path}");

// ✅ ĐÚNG
DebugLogger.Log("LocalConfig", $"Nạp cấu hình từ: {path}");
DebugLogger.Warn("LocalConfig", $"File chưa tồn tại — tạo template tại: {path}");
DebugLogger.Error("LocalConfig", $"Lỗi đọc file: {ex.Message}");
```

## Format output

```
[HH:mm:ss.fff] [Module] Message
[10:25:33.142] [LocalConfig] Nạp cấu hình từ: C:\Users\...\appsettings.local.json
[10:25:33.145] [LocalConfig] WARN File chưa tồn tại — tạo template
[10:25:33.150] [DB] ERROR Không kết nối được SQL Server
```

## Cấu hình — appsettings.local.json

```json
"DebugLog": {
  "Enabled": true,
  "WriteToFile": true,
  "FilePath": ""
}
```

- `Enabled`: bật/tắt toàn bộ logger (default: true)
- `WriteToFile`: ghi thêm ra file (default: false)
- `FilePath`: để trống → tự dùng `%APPDATA%\ICare247\Api\debug.log`

## Khi nào dùng DebugLogger vs Serilog

| Tình huống | Dùng |
|---|---|
| Trước khi Serilog sẵn sàng (startup, LocalConfigLoader) | `DebugLogger` |
| Blazor WASM (không có Serilog) | `DebugLogger` |
| Sau khi app đã boot (controllers, services, repositories) | `Serilog` (`ILogger<T>`) |
| Thông tin debug tạm thời khi dev | `DebugLogger` |

## Implementation

- Class: `ICare247.Api.DebugLogger` (static, thread-safe)
- File: `src/backend/src/ICare247.Api/DebugLogger.cs`
- Configure sau khi load config: `DebugLogger.Configure(configuration)`
