#!/usr/bin/env node
// Hook: Stop
// Mục đích: Nhắc nhở cập nhật memory nếu có code thay đổi chưa được ghi nhận

const { execSync } = require('child_process');
const fs = require('fs');

try {
  const root = execSync('git rev-parse --show-toplevel', { encoding: 'utf8' }).trim();
  const flagPath = root + '/.claude/.memory_needs_update';

  if (fs.existsSync(flagPath)) {
    // Đọc thời điểm flag được tạo
    const flagTime = fs.readFileSync(flagPath, 'utf8').trim();
    const message = flagTime
      ? `⚠️  Memory chưa cập nhật (code thay đổi từ ${flagTime.substring(0, 16).replace('T', ' ')}). Chạy /finish-task!`
      : '⚠️  Code đã thay đổi nhưng memory chưa cập nhật! Chạy /finish-task trước khi kết thúc session.';

    process.stdout.write(JSON.stringify({ systemMessage: message }));
  }
} catch (_) {
  // Bỏ qua lỗi — hook không được làm gián đoạn workflow
}
