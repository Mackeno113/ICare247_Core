# File: D:/ICare247_Core/start-claude.ps1
# Chạy file này mỗi khi bắt đầu làm việc

# Tạo thư mục logs nếu chưa có
New-Item -ItemType Directory -Force -Path ".\logs" | Out-Null

# Tên file log theo ngày
$date = Get-Date -Format "yyyy-MM-dd"
$logFile = ".\logs\session-$date.txt"

# Ghi timestamp bắt đầu session
"=" * 60 | Tee-Object -FilePath $logFile -Append
"SESSION START: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Tee-Object -FilePath $logFile -Append
"=" * 60 | Tee-Object -FilePath $logFile -Append

# Khởi động Claude Code, tee output ra log
claude | Tee-Object -FilePath $logFile -Append
