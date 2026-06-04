using CopyDatabase.MsSQLServer.Requests.SqlServer;

namespace CopyDatabase.MsSQLServer.Tests.Requests;

public sealed class SqlServerDataCopierTests
{
    [Fact]
    public void BuildSelectAllSql_UsesEscapedCatalogIdentifiers()
    {
        var table = new SqlServerSchemaObject("reporting]schema", "orders]archive", "");

        string sql = SqlServerDataCopier.BuildSelectAllSql(table);

        Assert.Equal("SELECT * FROM [reporting]]schema].[orders]]archive];", sql);
    }

    [Fact]
    public void BuildDeleteAllSql_UsesEscapedCatalogIdentifiers()
    {
        var table = new SqlServerSchemaObject("reporting]schema", "orders]archive", "");

        string sql = SqlServerDataCopier.BuildDeleteAllSql(table);

        Assert.Equal("DELETE FROM [reporting]]schema].[orders]]archive];", sql);
    }
}
