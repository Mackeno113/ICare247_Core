// File    : SchemaMaintenanceService.cs
// Module  : Infrastructure
// Layer   : Presentation
// Purpose : Thực thi DDL (ALTER) lên Target DB qua Dapper + Microsoft.Data.SqlClient.
//           Mỗi batch chạy trong cùng 1 transaction → hoặc xong hết, hoặc rollback hết.

using Dapper;
using Microsoft.Data.SqlClient;
using ConfigStudio.WPF.UI.Core.Interfaces;

namespace ConfigStudio.WPF.UI.Infrastructure;

/// <summary>
/// Thực thi các batch ALTER idempotent lên Target DB trong 1 transaction.
/// </summary>
public sealed class SchemaMaintenanceService : ISchemaMaintenanceService
{
    /// <inheritdoc />
    public async Task<int> ExecuteStatementsAsync(
        string connectionString,
        IReadOnlyList<string> statements,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || statements.Count == 0)
            return 0;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var executed = 0;
            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                await conn.ExecuteAsync(new CommandDefinition(stmt, transaction: tx, cancellationToken: ct));
                executed++;
            }

            await tx.CommitAsync(ct);
            return executed;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
