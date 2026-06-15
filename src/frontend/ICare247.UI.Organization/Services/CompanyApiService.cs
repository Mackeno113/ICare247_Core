// File    : CompanyApiService.cs
// Module  : ICare247.UI.Organization
// Layer   : Frontend (RCL)
// Purpose : Gọi API công ty (/api/v1/organization/companies). Save/Delete đọc cả body lỗi
//           (422 validation / 409 đang dùng) nên dùng HttpClient trực tiếp, không EnsureSuccess.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ICare247.UI.Shared.Services.Http;

namespace ICare247.UI.Organization.Services;

/// <summary>Client cụm màn Công ty — tree/detail/options/lookup + CRUD.</summary>
public sealed class CompanyApiService : ApiClientBase
{
    private const string Base = "/api/v1/organization/companies";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public CompanyApiService(HttpClient http) : base(http) { }

    /// <summary>Cây công ty (phẳng).</summary>
    public async Task<List<CompanyTreeNode>> GetTreeAsync(CancellationToken ct = default)
        => await GetAsync<List<CompanyTreeNode>>($"{Base}/tree", ct) ?? [];

    /// <summary>Bộ option tham chiếu cho form.</summary>
    public async Task<CompanyFormOptions> GetOptionsAsync(CancellationToken ct = default)
        => await GetAsync<CompanyFormOptions>($"{Base}/options", ct) ?? new();

    /// <summary>Tìm phường-xã (cascade địa chỉ).</summary>
    public async Task<List<LookupOption>> SearchPhuongXaAsync(string? term, CancellationToken ct = default)
        => await GetAsync<List<LookupOption>>($"{Base}/phuong-xa?term={Uri.EscapeDataString(term ?? "")}", ct) ?? [];

    /// <summary>Chi tiết 1 công ty.</summary>
    public Task<CompanyDetail?> GetByIdAsync(long id, CancellationToken ct = default)
        => GetAsync<CompanyDetail>($"{Base}/{id}", ct);

    /// <summary>Thêm/sửa — đọc kết quả cả khi 200 và 422 (validation fail).</summary>
    public async Task<SaveCompanyResult> SaveAsync(long? id, CompanyInput input, CancellationToken ct = default)
    {
        var resp = id is null
            ? await Http.PostAsJsonAsync(Base, input, ct)
            : await Http.PutAsJsonAsync($"{Base}/{id}", input, ct);

        if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.UnprocessableEntity)
            return await resp.Content.ReadFromJsonAsync<SaveCompanyResult>(Json, ct)
                   ?? new SaveCompanyResult { Success = false, Errors = ["common.msg.saveFailed"] };

        resp.EnsureSuccessStatusCode(); // các lỗi khác (401/500) → ném
        return new SaveCompanyResult { Success = false };
    }

    /// <summary>Xóa — 200 thành công; 409 trả lý do (key i18n) khi còn phụ thuộc.</summary>
    public async Task<DeleteCompanyResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var resp = await Http.DeleteAsync($"{Base}/{id}", ct);
        if (resp.IsSuccessStatusCode)
            return await resp.Content.ReadFromJsonAsync<DeleteCompanyResult>(Json, ct)
                   ?? new DeleteCompanyResult { Success = true };

        if (resp.StatusCode == HttpStatusCode.Conflict)
        {
            // ProblemDetails.extensions.reason = key i18n.
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var reason = doc.RootElement.TryGetProperty("reason", out var el) ? el.GetString() : null;
            return new DeleteCompanyResult { Success = false, Reason = reason ?? "organization.company.error.deleteBlocked" };
        }

        resp.EnsureSuccessStatusCode();
        return new DeleteCompanyResult { Success = false };
    }
}
