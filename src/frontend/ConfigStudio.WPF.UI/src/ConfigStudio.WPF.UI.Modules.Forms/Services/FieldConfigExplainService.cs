// File    : FieldConfigExplainService.cs
// Module  : Forms / Services
// Layer   : Presentation (logic thuần — sinh text, không đụng UI/DB)
// Purpose : REFACTOR-B1 — tách khỏi FieldConfigViewModel: sinh diễn giải tiếng Việt từ snapshot
//           cấu hình LookupBox (nút "Diễn giải cấu hình"). Text output giữ NGUYÊN từng ký tự
//           so với trước refactor.

using System.Text;
using ConfigStudio.WPF.UI.Modules.Forms.Models;

namespace ConfigStudio.WPF.UI.Modules.Forms.Services;

/// <summary>Sinh diễn giải cấu hình LookupBox — giúp admin kiểm tra cấu hình có đúng ý định không.</summary>
public static class FieldConfigExplainService
{
    /// <summary>Snapshot cấu hình LookupBox — VM chụp state đưa vào.</summary>
    public sealed class ExplainInput
    {
        public bool IsVirtual { get; init; }
        public string QueryMode { get; init; } = "table";
        public string? FkValueField { get; init; }
        public string? FkDisplayField { get; init; }
        public string? FkTableName { get; init; }
        public string? FkFilterSql { get; init; }
        public string? FkFunctionName { get; init; }
        public string? FkSelectSql { get; init; }
        public bool FkSearchEnabled { get; init; }
        public IReadOnlyList<FkFilterParam> FilterParams { get; init; } = [];
        public IReadOnlyList<FunctionParam> FunctionParams { get; init; } = [];
        public IReadOnlyList<string> ReloadOnChangeFields { get; init; } = [];
        public IReadOnlyList<DataSourceCondition> DataSourceConditions { get; init; } = [];
        public IReadOnlyList<FkColumnConfig> PopupColumns { get; init; } = [];
        public IReadOnlyList<string> CascadeWarnings { get; init; } = [];
    }

    /// <summary>Sinh diễn giải tiếng Việt từ snapshot cấu hình (thân cũ của ExecuteExplainConfig).</summary>
    public static string BuildExplanation(ExplainInput r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 DIỄN GIẢI CẤU HÌNH LOOKUP");
        sb.AppendLine(new string('─', 50));
        sb.AppendLine();

        // ── Thông tin chung ──
        if (r.IsVirtual)
            sb.AppendLine("🔮  Field ảo: KHÔNG lưu DB (chỉ để lọc/tham chiếu cascade).");
        sb.AppendLine($"⚙  Chế độ truy vấn: {r.QueryMode switch { "table" => "Bảng / View", "function" => "Table-Valued Function (TVF)", "sql" => "SQL tùy chỉnh", _ => r.QueryMode }}");
        if (!string.IsNullOrWhiteSpace(r.FkValueField))
            sb.AppendLine($"    Lưu vào DB: cột \"{r.FkValueField}\" (FK int)");
        if (!string.IsNullOrWhiteSpace(r.FkDisplayField))
            sb.AppendLine($"    Hiển thị trong ô: cột \"{r.FkDisplayField}\"");
        sb.AppendLine();

        switch (r.QueryMode)
        {
            case "table":
                sb.AppendLine($"🗄  Bảng / View nguồn: \"{r.FkTableName}\"");
                if (!string.IsNullOrWhiteSpace(r.FkFilterSql))
                {
                    sb.AppendLine("🔍  Điều kiện lọc (WHERE):");
                    sb.AppendLine($"    {r.FkFilterSql}");
                }
                if (r.FilterParams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚡  Tham số từ field trong form:");
                    foreach (var p in r.FilterParams)
                        sb.AppendLine($"    @{p.Param} ← field \"{p.FieldRef}\" (kiểu {p.Type})");
                }
                sb.AppendLine();
                sb.AppendLine("    → SQL runtime:");
                sb.AppendLine($"    SELECT {r.FkValueField}, {r.FkDisplayField}");
                sb.AppendLine($"    FROM   {r.FkTableName}");
                sb.AppendLine($"    WHERE  {(string.IsNullOrWhiteSpace(r.FkFilterSql) ? "(không có filter)" : r.FkFilterSql)}");
                break;

            case "function":
                sb.AppendLine($"⚡  TVF: \"{r.FkFunctionName}\"");
                if (r.FunctionParams.Count > 0)
                {
                    sb.AppendLine("    Tham số (theo thứ tự):");
                    for (int i = 0; i < r.FunctionParams.Count; i++)
                    {
                        var p = r.FunctionParams[i];
                        var src = p.SourceType == "field"
                            ? $"field \"{p.FieldRef}\" trong form"
                            : $"hệ thống {p.SystemKey}";
                        sb.AppendLine($"    [{i + 1}] @{p.Name} ({p.Type}) ← {src}");
                    }
                    var paramList = string.Join(", ", r.FunctionParams.Select(p => $"@{p.Name}"));
                    sb.AppendLine();
                    sb.AppendLine("    → SQL runtime:");
                    sb.AppendLine($"    SELECT {r.FkValueField}, {r.FkDisplayField}");
                    sb.AppendLine($"    FROM   {r.FkFunctionName}({paramList})");
                }
                break;

            case "sql":
                sb.AppendLine("📝  Full SQL tùy chỉnh:");
                sb.AppendLine($"    {r.FkSelectSql}");
                if (r.FilterParams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚡  Tham số từ field trong form:");
                    foreach (var p in r.FilterParams)
                        sb.AppendLine($"    @{p.Param} ← field \"{p.FieldRef}\" (kiểu {p.Type})");
                }
                break;
        }

        // ── Tham số hệ thống ──
        sb.AppendLine();
        sb.AppendLine("🔧  Tham số hệ thống tự inject:");
        sb.AppendLine("    @TenantId = Tenant hiện tại  |  @Today = Ngày hôm nay  |  @CurrentUser = User đăng nhập");

        // ── Reload on change ──
        if (r.ReloadOnChangeFields.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🔄  Tự động reload khi field thay đổi:");
            foreach (var f in r.ReloadOnChangeFields)
                sb.AppendLine($"    • Field \"{f}\" thay đổi giá trị → reload lại danh sách");
            sb.AppendLine("    ⚠  Giá trị đang chọn sẽ bị xoá nếu không còn trong danh sách mới.");
        }

        // ── DataSource conditions ──
        if (r.DataSourceConditions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🔀  Đổi bảng nguồn theo điều kiện:");
            foreach (var c in r.DataSourceConditions)
            {
                sb.AppendLine($"    • Nếu field \"{c.WhenField}\" {c.WhenOpLabel} \"{c.WhenValue}\":");
                sb.AppendLine($"      → Lấy từ bảng \"{c.TableName}\", hiển thị cột \"{c.DisplayField}\"");
                if (!string.IsNullOrWhiteSpace(c.FilterSql))
                    sb.AppendLine($"      → Filter: {c.FilterSql}");
            }
            sb.AppendLine($"    • Các trường hợp còn lại → dùng bảng mặc định \"{r.FkTableName}\"");
        }

        // ── Cột popup ──
        if (r.PopupColumns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("📊  Cột hiển thị trong popup chọn:");
            foreach (var col in r.PopupColumns)
                sb.AppendLine($"    • [{col.CaptionKey}] (cột DB: {col.FieldName}, rộng: {col.Width}px)");
        }

        // ── Search ──
        sb.AppendLine();
        sb.AppendLine(r.FkSearchEnabled
            ? "🔎  Cho phép tìm kiếm trong danh sách."
            : "⛔  Không cho phép tìm kiếm.");

        // ── Cảnh báo cascade (P2/P3) — @param sai hoặc thiếu reload ──
        if (r.CascadeWarnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("🛑  CẢNH BÁO CASCADE — sửa trước khi lưu:");
            foreach (var w in r.CascadeWarnings)
                sb.AppendLine($"    {w}");
        }

        return sb.ToString();
    }
}
