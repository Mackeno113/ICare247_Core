// File    : DocProcRunner.cs
// Module  : DocTemplate
// Layer   : Infrastructure (Documents)
// Purpose : Chạy stored proc lấy dữ liệu (Data DB) → DataTable; khám phá cột (sp_describe_first_result_set).
//           An toàn: tên proc validate regex + caller đã check whitelist; tham số hóa 100%.
// Spec    : docs/spec/28_DOC_TEMPLATE_SPEC.md §5.

using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Dapper;
using ICare247.Application.Interfaces;

namespace ICare247.Infrastructure.Documents.Internal;

/// <summary>Thực thi stored proc trên Data DB tenant (chỉ đọc) → DataTable / danh sách cột.</summary>
internal sealed partial class DocProcRunner
{
    private readonly IDataDbConnectionFactory _dataDb;

    public DocProcRunner(IDataDbConnectionFactory dataDb) => _dataDb = dataDb;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SafeProcRegex();

    /// <summary>
    /// Chạy proc với tham số → DataTable (mọi dòng).
    /// Sự kiện theo sau: ném <see cref="InvalidOperationException"/> nếu tên proc sai định dạng.
    /// </summary>
    public async Task<DataTable> ExecuteAsync(string procName, DynamicParameters parameters, CancellationToken ct)
    {
        EnsureSafe(procName);
        using var conn = _dataDb.CreateConnection();
        using var reader = await conn.ExecuteReaderAsync(new CommandDefinition(
            "dbo." + procName, parameters, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }

    /// <summary>
    /// Khám phá cột kết quả của proc qua <c>sp_describe_first_result_set</c> (không side-effect).
    /// Tự truyền tham số proc = NULL để proc có tham số bắt buộc vẫn mô tả được.
    /// Sự kiện theo sau: trả danh sách (tên cột, kiểu DB).
    /// </summary>
    public async Task<IReadOnlyList<(string Name, string DbType)>> DescribeAsync(string procName, CancellationToken ct)
    {
        EnsureSafe(procName);
        using var conn = _dataDb.CreateConnection();

        // Lấy tham số proc (nếu có) → dựng EXEC ... @p=NULL để sp_describe không lỗi thiếu tham số.
        var paramNames = (await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT name FROM sys.parameters WHERE object_id = OBJECT_ID(@obj) AND is_output = 0 ORDER BY parameter_id",
            new { obj = "dbo." + procName }, cancellationToken: ct))).AsList();

        var argList = paramNames.Count == 0
            ? ""
            : " " + string.Join(", ", paramNames.Select(p => $"{p} = NULL"));
        var tsql = $"EXEC dbo.{procName}{argList}";

        var cols = await conn.QueryAsync(new CommandDefinition(
            "sys.sp_describe_first_result_set", new { tsql, @params = (string?)null, browse_information_mode = 0 },
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        var list = new List<(string, string)>();
        foreach (var row in cols)
        {
            var d = (IDictionary<string, object?>)row;
            var name = d.TryGetValue("name", out var n) ? n?.ToString() ?? "" : "";
            var type = d.TryGetValue("system_type_name", out var t) ? t?.ToString() ?? "" : "";
            if (!string.IsNullOrEmpty(name)) list.Add((name, type));
        }
        return list;
    }

    private static void EnsureSafe(string procName)
    {
        if (!SafeProcRegex().IsMatch(procName))
            throw new InvalidOperationException($"Tên stored proc không hợp lệ: '{procName}'.");
    }
}
