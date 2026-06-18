#!/usr/bin/env node
// Hook: PostToolUse (Write|Edit)
// Mục đích: Tạo flag file khi source code thay đổi để nhắc cập nhật memory

const { execSync } = require('child_process');
const fs = require('fs');

let data = '';
process.stdin.on('data', chunk => data += chunk);
process.stdin.on('end', () => {
  try {
    const input = JSON.parse(data);
    const filePath = (input.tool_input || {}).file_path || '';

    // Bỏ qua nếu đang sửa memory hoặc không phải source file
    const isMemoryFile = filePath.includes('.claude/memory') || filePath.includes('.claude\\memory');
    const isSourceFile = /\.(cs|xaml|csproj|slnx|json|md)$/.test(filePath);

    if (filePath && isSourceFile && !isMemoryFile) {
      const root = execSync('git rev-parse --show-toplevel', { encoding: 'utf8' }).trim();
      const flagPath = root + '/.claude/.memory_needs_update';
      fs.writeFileSync(flagPath, new Date().toISOString());
    }
  } catch (_) {
    // Bỏ qua lỗi — hook không được làm gián đoạn workflow
  }
});
