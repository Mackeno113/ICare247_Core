// File    : TenantContextTests.cs
// Module  : MultiTenancy
// Layer   : Tests
// Purpose : Unit tests cho TenantContext.Assign — bất biến cô lập tenant (AUDIT-3): Tenant_Id + 3
//           connection string luôn đến từ CÙNG một TenantConnections, gán lại thay TOÀN BỘ (không lẫn).

using ICare247.Application.Interfaces;

namespace ICare247.Application.Tests.MultiTenancy;

public sealed class TenantContextTests
{
    [Fact]
    public void Assign_SetsAllFieldsFromOneSource()
    {
        var ctx = new TenantContext();
        ctx.Assign(new TenantConnections(7, "cfgConn", "dataConn", "auditConn"));

        Assert.Equal(7, ctx.TenantId);
        Assert.Equal("cfgConn", ctx.ConfigConnectionString);
        Assert.Equal("dataConn", ctx.DataConnectionString);
        Assert.Equal("auditConn", ctx.AuditConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Assign_EmptyAudit_FallsBackToData(string audit)
    {
        var ctx = new TenantContext();
        ctx.Assign(new TenantConnections(3, "cfg", "theDataConn", audit));

        Assert.Equal("theDataConn", ctx.AuditConnectionString);
    }

    /// <summary>
    /// Cốt lõi cô lập tenant: gán lại từ tenant khác thay TOÀN BỘ field — không thể còn
    /// Tenant_Id/connection của tenant trước lẫn vào (id A + connection B = rò rỉ chéo).
    /// </summary>
    [Fact]
    public void Assign_Reassign_ReplacesEveryField_NoStaleMix()
    {
        var ctx = new TenantContext();
        ctx.Assign(new TenantConnections(1, "cfgA", "dataA", "auditA"));
        ctx.Assign(new TenantConnections(2, "cfgB", "dataB", "auditB"));

        Assert.Equal(2, ctx.TenantId);
        Assert.Equal("cfgB", ctx.ConfigConnectionString);
        Assert.Equal("dataB", ctx.DataConnectionString);
        Assert.Equal("auditB", ctx.AuditConnectionString);
        // Không còn dấu vết tenant A.
        Assert.DoesNotContain("A", ctx.ConfigConnectionString);
        Assert.DoesNotContain("A", ctx.DataConnectionString);
        Assert.DoesNotContain("A", ctx.AuditConnectionString);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Assign_InvalidTenantId_Throws(int badId)
    {
        var ctx = new TenantContext();
        Assert.Throws<ArgumentException>(
            () => ctx.Assign(new TenantConnections(badId, "cfg", "data", "audit")));
    }

    [Fact]
    public void Assign_Null_Throws()
    {
        var ctx = new TenantContext();
        Assert.Throws<ArgumentNullException>(() => ctx.Assign(null!));
    }

    [Fact]
    public void Default_BeforeAssign_HasEmptyStringsAndZeroId()
    {
        var ctx = new TenantContext();

        Assert.Equal(0, ctx.TenantId);
        Assert.Equal("", ctx.ConfigConnectionString);
        Assert.Equal("", ctx.DataConnectionString);
        Assert.Equal("", ctx.AuditConnectionString);
    }
}
