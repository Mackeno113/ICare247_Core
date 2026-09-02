#!/usr/bin/env node
/*
 * Sinh 1 trang HTML "menu" duy nhất (site.html) từ toàn bộ file .md trong thư mục này.
 *
 * Nguồn dữ liệu DUY NHẤT vẫn là các file .md (AI agent/dev đọc thẳng, dễ diff qua git).
 * Script này KHÔNG sinh thêm nội dung — chỉ đọc + chuyển thành HTML có menu điều hướng,
 * kiểu chữ, bảng, callout dễ đọc cho end-user. Sửa nội dung → sửa .md → chạy lại script.
 *
 * Chạy:  node docs/huong-dan-wpf/build-docs-site.js
 * Ra:    docs/huong-dan-wpf/site.html (mở trực tiếp bằng trình duyệt, không cần server)
 *
 * Thêm bài hướng dẫn mới — CHỈ 1 việc: tạo file .md mới trong thư mục này rồi thêm 1 dòng
 * bullet `- [ten-file.md](ten-file.md) — mô tả ngắn` vào đúng mục (##) trong README.md
 * (việc vốn đã làm mỗi khi thêm bài mới). Script này ĐỌC THẲNG mục lục từ README.md để
 * dựng menu — không cần sửa gì trong file .js này. Quên cập nhật README.md cũng không sao:
 * file mới tự rơi vào nhóm "Khác (chưa gom nhóm)" ở cuối menu, không bị thiếu.
 */
const fs = require('fs');
const path = require('path');

const DOCS_DIR = __dirname;
const OUT_FILE = path.join(DOCS_DIR, 'site.html');
const README_FILE = path.join(DOCS_DIR, 'README.md');

// Đọc README.md → dựng nhóm menu từ chính mục lục đang có (mỗi heading `## ` = 1 nhóm,
// mỗi bullet `- [file.md](file.md)` trỏ tới file cục bộ = 1 bài). Bỏ qua bullet trỏ ra
// ngoài thư mục (vd `../spec/...`) — đó là tham chiếu chéo, không phải bài của thư mục này.
function readGroupsFromReadme() {
  if (!fs.existsSync(README_FILE)) return [];
  const md = fs.readFileSync(README_FILE, 'utf8');
  const lines = md.split(/\r?\n/);
  const groups = [];
  let current = null;
  for (const line of lines) {
    const heading = /^##\s+(.+)$/.exec(line);
    if (heading) {
      current = { title: heading[1].trim(), files: [] };
      groups.push(current);
      continue;
    }
    const item = /^-\s*\[([^\]]+)\]\(([^)]+)\)/.exec(line);
    if (item && current) {
      const href = item[2].trim();
      // chỉ nhận file .md cục bộ (không có "/" — nằm ngay trong thư mục này)
      if (/^[\w.-]+\.md$/.test(href)) current.files.push(href);
    }
  }
  return groups.filter((g) => g.files.length > 0);
}

const GROUPS = readGroupsFromReadme();

// ---------- Tiện ích ----------

function slugify(s) {
  return s
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '') // bỏ dấu
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function docSlug(filename) {
  return 'doc-' + slugify(filename.replace(/\.md$/, ''));
}

function escapeHtml(s) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}

// Áp inline markdown (code/bold/italic/link) lên text ĐÃ escape HTML.
function renderInline(raw, currentFile) {
  let text = escapeHtml(raw);

  // Link nội bộ tới file .md khác trong cùng thư mục -> nhảy neo trong site.
  // Link #anchor thuần (trỏ heading cùng file, tay viết bằng cả dấu tiếng Việt lẫn không dấu) ->
  // chuẩn hoá lại qua CHÍNH slugify() dùng để sinh id heading, để khớp bất kể tác giả gõ kiểu nào.
  // Link khác (http, ../spec/...) giữ nguyên.
  text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (m, label, url) => {
    const trimmed = url.trim();
    const simple = /^([\w.-]+\.md)(#(.*))?$/.exec(trimmed);
    if (simple) {
      const targetFile = simple[1];
      const fragment = simple[3];
      const target = fragment
        ? `${docSlug(targetFile)}--${slugify(fragment)}`
        : docSlug(targetFile);
      return `<a href="#${target}">${label}</a>`;
    }
    if (trimmed.startsWith('#')) {
      return `<a href="#${docSlug(currentFile)}--${slugify(trimmed.slice(1))}">${label}</a>`;
    }
    return `<a href="${trimmed}" target="_blank" rel="noopener">${label}</a>`;
  });

  text = text.replace(/`([^`]+)`/g, '<code>$1</code>');
  text = text.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  text = text.replace(/(^|[^*])\*([^*\n]+)\*(?!\*)/g, '$1<em>$2</em>');

  return text;
}

// ---------- Markdown -> HTML (parser dòng-theo-dòng, đủ dùng cho bộ tài liệu này) ----------

function mdToHtml(md, currentFile) {
  const lines = md.replace(/\r\n/g, '\n').split('\n');
  let out = [];
  let i = 0;
  let usedIds = new Set();

  // Prefix bằng docSlug(file) để id DUY NHẤT toàn trang site.html (nhiều file dùng chung tiêu đề
  // như "Phần B — Tra cứu kỹ thuật" sẽ không còn đụng id nhau — nếu không, neo #anchor sẽ luôn
  // nhảy tới lần xuất hiện ĐẦU TIÊN của id đó trên toàn trang, tức nhảy nhầm sang file khác).
  function uniqueId(text) {
    let base = `${docSlug(currentFile)}--${slugify(text) || 'muc'}`;
    let id = base;
    let n = 2;
    while (usedIds.has(id)) id = `${base}-${n++}`;
    usedIds.add(id);
    return id;
  }

  function isTableSep(line) {
    return /^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)+\|?\s*$/.test(line);
  }

  function splitRow(line) {
    let t = line.trim();
    if (t.startsWith('|')) t = t.slice(1);
    if (t.endsWith('|')) t = t.slice(0, -1);
    return t.split('|').map((c) => c.trim());
  }

  while (i < lines.length) {
    const line = lines[i];

    // Fenced code block
    if (/^```/.test(line.trim())) {
      const codeLines = [];
      i++;
      while (i < lines.length && !/^```/.test(lines[i].trim())) {
        codeLines.push(lines[i]);
        i++;
      }
      i++; // đóng ```
      out.push(`<pre><code>${escapeHtml(codeLines.join('\n'))}</code></pre>`);
      continue;
    }

    // Header
    const h = /^(#{1,6})\s+(.*)$/.exec(line);
    if (h) {
      const level = h[1].length;
      const text = h[2].trim();
      const id = uniqueId(text);
      out.push(`<h${level} id="${id}">${renderInline(text, currentFile)}</h${level}>`);
      i++;
      continue;
    }

    // Horizontal rule
    if (/^(-{3,}|\*{3,})\s*$/.test(line.trim())) {
      out.push('<hr>');
      i++;
      continue;
    }

    // Blockquote (callout) — gom mọi dòng liên tiếp bắt đầu bằng '>'
    if (/^>\s?/.test(line)) {
      const inner = [];
      while (i < lines.length && /^>\s?/.test(lines[i])) {
        inner.push(lines[i].replace(/^>\s?/, ''));
        i++;
      }
      out.push(`<blockquote class="callout">${mdToHtml(inner.join('\n'), currentFile)}</blockquote>`);
      continue;
    }

    // Table
    if (line.includes('|') && i + 1 < lines.length && isTableSep(lines[i + 1])) {
      const header = splitRow(line);
      i += 2;
      const rows = [];
      while (i < lines.length && lines[i].includes('|') && lines[i].trim() !== '') {
        rows.push(splitRow(lines[i]));
        i++;
      }
      let t = '<div class="table-wrap"><table><thead><tr>';
      t += header.map((c) => `<th>${renderInline(c, currentFile)}</th>`).join('');
      t += '</tr></thead><tbody>';
      for (const r of rows) {
        t += '<tr>' + r.map((c) => `<td>${renderInline(c, currentFile)}</td>`).join('') + '</tr>';
      }
      t += '</tbody></table></div>';
      out.push(t);
      continue;
    }

    // Unordered list
    if (/^\s*[-*]\s+/.test(line)) {
      const items = [];
      while (i < lines.length && /^\s*[-*]\s+/.test(lines[i])) {
        items.push(lines[i].replace(/^\s*[-*]\s+/, ''));
        i++;
      }
      out.push('<ul>' + items.map((it) => `<li>${renderInline(it, currentFile)}</li>`).join('') + '</ul>');
      continue;
    }

    // Ordered list
    if (/^\s*\d+\.\s+/.test(line)) {
      const items = [];
      while (i < lines.length && /^\s*\d+\.\s+/.test(lines[i])) {
        items.push(lines[i].replace(/^\s*\d+\.\s+/, ''));
        i++;
      }
      out.push('<ol>' + items.map((it) => `<li>${renderInline(it, currentFile)}</li>`).join('') + '</ol>');
      continue;
    }

    // Blank line
    if (line.trim() === '') {
      i++;
      continue;
    }

    // Paragraph — gom các dòng liên tiếp không rơi vào loại nào ở trên
    const para = [];
    while (
      i < lines.length &&
      lines[i].trim() !== '' &&
      !/^#{1,6}\s+/.test(lines[i]) &&
      !/^```/.test(lines[i].trim()) &&
      !/^>\s?/.test(lines[i]) &&
      !/^\s*[-*]\s+/.test(lines[i]) &&
      !/^\s*\d+\.\s+/.test(lines[i]) &&
      !(lines[i].includes('|') && i + 1 < lines.length && isTableSep(lines[i + 1])) &&
      !/^(-{3,}|\*{3,})\s*$/.test(lines[i])
    ) {
      para.push(lines[i]);
      i++;
    }
    out.push(`<p>${renderInline(para.join(' '), currentFile)}</p>`);
  }

  return out.join('\n');
}

function extractTitle(md) {
  const m = /^#\s+(.+)$/m.exec(md);
  return m ? m[1].trim() : 'Không có tiêu đề';
}

// Bỏ ký hiệu markdown (**, `) khỏi text dùng ở chỗ không render HTML (sidebar, <title>).
function plainText(text) {
  return text
    .replace(/\*\*([^*]+)\*\*/g, '$1')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/\*([^*]+)\*/g, '$1');
}

// ---------- Đọc toàn bộ file + build sidebar/nội dung ----------

const allMdFiles = fs
  .readdirSync(DOCS_DIR)
  .filter((f) => f.endsWith('.md') && f !== 'README.md');

const declared = new Set(GROUPS.flatMap((g) => g.files));
const leftover = allMdFiles.filter((f) => !declared.has(f));
const groups = leftover.length ? [...GROUPS, { title: 'Khác (chưa gom nhóm)', files: leftover }] : GROUPS;

let sidebarHtml = '';
let contentHtml = '';

for (const group of groups) {
  sidebarHtml += `<div class="nav-group"><div class="nav-group-title">${escapeHtml(group.title)}</div><ul>`;
  for (const file of group.files) {
    const fp = path.join(DOCS_DIR, file);
    if (!fs.existsSync(fp)) {
      console.warn(`[build-docs-site] Bỏ qua — không thấy file: ${file}`);
      continue;
    }
    const md = fs.readFileSync(fp, 'utf8');
    const title = plainText(extractTitle(md));
    const slug = docSlug(file);

    sidebarHtml += `<li><a href="#${slug}" data-search="${escapeHtml(slugify(title + ' ' + file))}">${escapeHtml(title)}</a></li>`;

    const bodyHtml = mdToHtml(md, file);
    contentHtml += `<section class="doc" id="${slug}">${bodyHtml}<div class="doc-source">Nguồn: <code>${escapeHtml(file)}</code></div></section>\n`;
  }
  sidebarHtml += '</ul></div>';
}

const generatedAt = new Date().toISOString().slice(0, 10);

const template = `<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<title>Hướng dẫn cấu hình ICare247 ConfigStudio</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
  :root{
    --accent:#0F6CBD; --bg:#ffffff; --bg-side:#f6f8fa; --text:#1a1f26; --text-dim:#5b6572;
    --border:#e3e7ec; --code-bg:#f2f4f7; --callout-bg:#eef6fd; --callout-border:#0F6CBD;
  }
  *{box-sizing:border-box;}
  html{scroll-behavior:smooth;}
  body{
    margin:0; font-family:"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif;
    color:var(--text); background:var(--bg); line-height:1.65; font-size:16px;
  }
  .layout{display:flex; min-height:100vh;}
  .sidebar{
    width:300px; flex:0 0 300px; background:var(--bg-side); border-right:1px solid var(--border);
    position:sticky; top:0; height:100vh; overflow-y:auto; padding:16px 0 40px;
  }
  .sidebar-header{padding:0 16px 12px; border-bottom:1px solid var(--border); margin-bottom:8px;}
  .sidebar-header h1{font-size:16px; margin:0 0 4px; color:var(--accent);}
  .sidebar-header p{font-size:12px; color:var(--text-dim); margin:0;}
  #search{
    width:calc(100% - 32px); margin:8px 16px 4px; padding:8px 10px; border:1px solid var(--border);
    border-radius:6px; font-size:13px;
  }
  .nav-group{margin-top:14px;}
  .nav-group-title{
    font-size:11px; text-transform:uppercase; letter-spacing:.04em; color:var(--text-dim);
    padding:0 16px; margin-bottom:4px; font-weight:600;
  }
  .sidebar ul{list-style:none; margin:0; padding:0;}
  .sidebar li a{
    display:block; padding:7px 16px; color:var(--text); text-decoration:none; font-size:13.5px;
    border-left:3px solid transparent;
  }
  .sidebar li a:hover{background:#eaf1fb; color:var(--accent);}
  .sidebar li.hidden{display:none;}
  .nav-group.hidden{display:none;}
  .main{flex:1 1 auto; padding:32px 48px 120px; max-width:900px;}
  .doc{padding-bottom:56px; margin-bottom:40px; border-bottom:1px dashed var(--border);}
  .doc:last-child{border-bottom:none;}
  h1{font-size:26px; color:var(--accent); margin-top:0; scroll-margin-top:16px;}
  h2{font-size:20px; margin-top:36px; padding-top:8px; scroll-margin-top:16px;}
  h3{font-size:16.5px; margin-top:26px; scroll-margin-top:16px;}
  h4{font-size:14.5px; margin-top:20px; scroll-margin-top:16px;}
  h5{font-size:13px; margin-top:16px; scroll-margin-top:16px; color:var(--text-dim); text-transform:uppercase; letter-spacing:.03em;}
  h6{font-size:12.5px; margin-top:14px; scroll-margin-top:16px; color:var(--text-dim); font-style:italic;}
  p{margin:12px 0;}
  code{background:var(--code-bg); padding:1px 5px; border-radius:4px; font-size:0.92em; font-family:"Cascadia Code",Consolas,monospace;}
  pre{background:var(--code-bg); padding:14px 16px; border-radius:8px; overflow-x:auto;}
  pre code{background:none; padding:0;}
  blockquote.callout{
    background:var(--callout-bg); border-left:4px solid var(--callout-border);
    margin:16px 0; padding:10px 16px; border-radius:0 6px 6px 0; color:var(--text);
  }
  blockquote.callout p{margin:6px 0;}
  .table-wrap{overflow-x:auto; margin:16px 0;}
  table{border-collapse:collapse; width:100%; font-size:14px;}
  th,td{border:1px solid var(--border); padding:8px 10px; text-align:left; vertical-align:top;}
  th{background:#f2f6fa; font-weight:600;}
  tr:nth-child(even) td{background:#fafbfc;}
  a{color:var(--accent);}
  hr{border:none; border-top:1px solid var(--border); margin:28px 0;}
  ul,ol{padding-left:22px;}
  li{margin:4px 0;}
  .doc-source{margin-top:24px; font-size:12px; color:var(--text-dim);}
  .back-top{position:fixed; right:24px; bottom:24px; background:var(--accent); color:#fff; border:none;
    padding:10px 14px; border-radius:24px; cursor:pointer; font-size:13px; box-shadow:0 2px 8px rgba(0,0,0,.15);}
  @media (max-width:860px){
    .layout{flex-direction:column;}
    .sidebar{position:static; width:100%; height:auto;}
    .main{padding:24px 20px 80px;}
  }
  @media (prefers-color-scheme: dark){
    :root{
      --bg:#14181d; --bg-side:#1b2027; --text:#e7ebf0; --text-dim:#93a0ad;
      --border:#2a2f37; --code-bg:#1f242b; --callout-bg:#182534;
    }
    th{background:#20262d;} tr:nth-child(even) td{background:#191d23;}
    .sidebar li a:hover{background:#20304a;}
  }
</style>
</head>
<body>
<div class="layout">
  <nav class="sidebar">
    <div class="sidebar-header">
      <h1>Hướng dẫn ConfigStudio</h1>
      <p>Toàn bộ tài liệu cấu hình ICare247 · cập nhật ${generatedAt}</p>
    </div>
    <input id="search" type="text" placeholder="Tìm bài hướng dẫn...">
    ${sidebarHtml}
  </nav>
  <main class="main">
    ${contentHtml}
  </main>
</div>
<button class="back-top" onclick="window.scrollTo({top:0,behavior:'smooth'})">↑ Đầu trang</button>
<script>
  const search = document.getElementById('search');
  search.addEventListener('input', () => {
    const q = search.value.toLowerCase().trim();
    document.querySelectorAll('.nav-group').forEach((group) => {
      let anyVisible = false;
      group.querySelectorAll('li').forEach((li) => {
        const a = li.querySelector('a');
        const hay = (a.dataset.search || '') + ' ' + a.textContent.toLowerCase();
        const match = !q || hay.toLowerCase().includes(q);
        li.classList.toggle('hidden', !match);
        if (match) anyVisible = true;
      });
      group.classList.toggle('hidden', !anyVisible);
    });
  });
</script>
</body>
</html>
`;

fs.writeFileSync(OUT_FILE, template, 'utf8');
console.log(`[build-docs-site] Đã sinh: ${OUT_FILE}`);
console.log(`[build-docs-site] Tổng số bài: ${groups.reduce((n, g) => n + g.files.length, 0)}`);
