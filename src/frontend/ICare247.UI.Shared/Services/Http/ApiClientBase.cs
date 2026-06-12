// File    : ApiClientBase.cs
// Module  : Shared
// Layer   : Frontend (Shared)
// Purpose : Lớp cơ sở cho mọi *ApiService của từng module — bọc HttpClient, chuẩn
//           hóa GET/POST/PUT/DELETE JSON về backend API (đã gắn sẵn header tenant
//           ở tầng HttpClient trong Program.cs của host).

using System.Net.Http.Json;

namespace ICare247.UI.Shared.Services.Http;

/// <summary>
/// Lớp cơ sở chia sẻ cho các service gọi API của từng module nghiệp vụ.
/// Mỗi module (Organization, Hr, Payroll…) tạo 1 service kế thừa lớp này để dùng
/// chung cách gọi JSON + xử lý lỗi, tránh lặp code HttpClient ở mọi nơi.
/// </summary>
public abstract class ApiClientBase
{
    /// <summary>HttpClient đã cấu hình BaseAddress + header X-Tenant-Id ở host.</summary>
    protected HttpClient Http { get; }

    /// <summary>
    /// Khởi tạo với HttpClient được DI cấp. Sự kiện theo sau: service con sẵn sàng
    /// gọi các helper bên dưới.
    /// </summary>
    /// <param name="http">HttpClient trỏ tới backend API.</param>
    protected ApiClientBase(HttpClient http) => Http = http;

    /// <summary>
    /// GET một tài nguyên và deserialize JSON về <typeparamref name="T"/>.
    /// Sự kiện theo sau: trả về đối tượng (hoặc null nếu body rỗng/204).
    /// </summary>
    protected Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        => Http.GetFromJsonAsync<T>(url, ct);

    /// <summary>
    /// POST body JSON, ném lỗi nếu status không thành công, rồi đọc kết quả JSON.
    /// Sự kiện theo sau: trả về <typeparamref name="TResult"/> từ response.
    /// </summary>
    protected async Task<TResult?> PostAsync<TBody, TResult>(string url, TBody body, CancellationToken ct = default)
    {
        var resp = await Http.PostAsJsonAsync(url, body, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TResult>(cancellationToken: ct);
    }

    /// <summary>
    /// PUT body JSON, ném lỗi nếu status không thành công.
    /// Sự kiện theo sau: hoàn tất khi server xác nhận cập nhật.
    /// </summary>
    protected async Task PutAsync<TBody>(string url, TBody body, CancellationToken ct = default)
    {
        var resp = await Http.PutAsJsonAsync(url, body, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// DELETE một tài nguyên, ném lỗi nếu status không thành công.
    /// Sự kiện theo sau: hoàn tất khi server xác nhận xóa.
    /// </summary>
    protected async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var resp = await Http.DeleteAsync(url, ct);
        resp.EnsureSuccessStatusCode();
    }
}
