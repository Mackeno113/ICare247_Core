// File    : attachment-upload.js
// Module  : ICare247_UI (Attachment control)
// Purpose : Upload tệp từ trình duyệt bằng XHR để CÓ progress thật (HttpClient WASM không báo % upload),
//           nén ảnh phía client (canvas) giảm băng thông trước khi gửi (server vẫn chuẩn hoá lại), và
//           đính Bearer + X-Tenant-Id thủ công. Báo trạng thái về .NET qua DotNetObjectReference.

let seq = 0;

/// Mở hộp chọn tệp (input ẩn) — gọi từ .NET khi bấm nút.
export function pick(inputEl) {
    if (inputEl) inputEl.click();
}

/// Upload mọi tệp đang chọn trong inputEl. opts = AttachmentUploadOptions (xem C#).
export function uploadSelected(inputEl, dotnetRef, opts) {
    const files = Array.from(inputEl?.files || []);
    inputEl.value = ''; // reset để chọn lại cùng tệp vẫn kích hoạt onchange
    for (const f of files) uploadOne(f, dotnetRef, opts);
}

async function uploadOne(file, dotnetRef, opts) {
    const id = ++seq;
    await dotnetRef.invokeMethodAsync('OnStart', id, file.name, file.size);

    // Nén ảnh phía client (không bắt buộc — lỗi thì gửi bản gốc).
    let payload = file;
    if (opts.compressImages && isCompressibleImage(file.type)) {
        try { payload = await compressImage(file, opts.maxDimension || 2000, opts.quality || 0.85); }
        catch { payload = file; }
    }

    const form = new FormData();
    form.append('file', payload, file.name);
    if (opts.loai) form.append('loai', opts.loai);
    if (opts.ownerTable) form.append('ownerTable', opts.ownerTable);
    if (opts.ownerId) form.append('ownerId', String(opts.ownerId));
    if (opts.fieldMa) form.append('fieldMa', opts.fieldMa);

    const xhr = new XMLHttpRequest();
    xhr.open('POST', opts.url, true);
    if (opts.token) xhr.setRequestHeader('Authorization', 'Bearer ' + opts.token);
    if (opts.tenantId) xhr.setRequestHeader('X-Tenant-Id', opts.tenantId);

    xhr.upload.onprogress = (e) => {
        if (e.lengthComputable) {
            const pct = Math.round((e.loaded * 100) / e.total);
            dotnetRef.invokeMethodAsync('OnProgress', id, pct);
        }
    };
    xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
            dotnetRef.invokeMethodAsync('OnDone', id, xhr.responseText || '');
        } else {
            dotnetRef.invokeMethodAsync('OnError', id, parseError(xhr));
        }
    };
    xhr.onerror = () => dotnetRef.invokeMethodAsync('OnError', id, 'Lỗi mạng khi tải lên.');
    xhr.send(form);
}

function isCompressibleImage(type) {
    return type === 'image/png' || type === 'image/jpeg' || type === 'image/webp';
}

/// Resize theo cạnh dài tối đa + re-encode qua canvas. Trả Blob.
function compressImage(file, maxDim, quality) {
    return new Promise((resolve, reject) => {
        const url = URL.createObjectURL(file);
        const img = new Image();
        img.onload = () => {
            URL.revokeObjectURL(url);
            const scale = Math.min(1, maxDim / Math.max(img.width, img.height));
            const w = Math.max(1, Math.round(img.width * scale));
            const h = Math.max(1, Math.round(img.height * scale));
            const canvas = document.createElement('canvas');
            canvas.width = w; canvas.height = h;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, w, h);
            // PNG giữ trong suốt; còn lại JPEG cho nhẹ.
            const outType = file.type === 'image/png' ? 'image/png' : 'image/jpeg';
            canvas.toBlob(b => b ? resolve(b) : reject(new Error('toBlob null')), outType, quality);
        };
        img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('decode fail')); };
        img.src = url;
    });
}

/// Tải tệp về kèm Bearer (fetch → blob → anchor) — endpoint yêu cầu JWT nên không mở trực tiếp bằng URL.
export async function downloadWithToken(url, token, tenantId, fileName) {
    const headers = {};
    if (token) headers['Authorization'] = 'Bearer ' + token;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;
    const resp = await fetch(url, { headers });
    if (!resp.ok) throw new Error('Tải thất bại: ' + resp.status);
    const blob = await resp.blob();
    const objUrl = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = objUrl;
    a.download = fileName || 'download';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(objUrl), 10000);
}

function parseError(xhr) {
    try {
        const j = JSON.parse(xhr.responseText);
        return j.error || ('Lỗi ' + xhr.status);
    } catch {
        return 'Lỗi ' + xhr.status;
    }
}
