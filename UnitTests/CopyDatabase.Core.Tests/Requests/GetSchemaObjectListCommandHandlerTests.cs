using CopyDatabase.Core.Tests.Setup;
using System.Security;

namespace CopyDatabase.Core.Requests.Tests;

public sealed class GetSchemaObjectListCommandHandlerTests
{
    [Fact]
    public async Task ShouldRequestBaseTablesFromSelectedDatabase()
    {
        var connection = new UTDbConnection();
        var connectionStringBuilder = new Mock<IConnectionStringBuilder>();
        connectionStringBuilder
            .Setup(oo => oo.BuildConnection(It.IsAny<IDatabaseServerCredentials>(), "CopyDatabaseSourceTest"))
            .Returns("table-connection".ToSecureString());

        var sut = new GetSchemaObjectListCommandHandler(
            Mock.Of<IDbConnectionFactory>(oo => oo.GetDbConnection(It.IsAny<SecureString>(), DatabaseProvider.MsSQLServer) == connection),
            Mock.Of<IConnectionStringBuilderFactory>(oo => oo.GetConnectionStringBuilder(DatabaseProvider.MsSQLServer) == connectionStringBuilder.Object));

        await sut.Handle(new GetSchemaObjectList
        {
            DatabaseName = "CopyDatabaseSourceTest",
            ObjectType = SchemaObjectType.Table,
            ServerCredentials = new DatabaseServerTestCredentials { DataSource = "127.0.0.1,14333" }
        }, CancellationToken.None);

        Assert.Contains("INFORMATION_SCHEMA.TABLES", connection.dbCommand.CommandText);
        Assert.Contains("TABLE_TYPE = 'BASE TABLE'", connection.dbCommand.CommandText);
        connectionStringBuilder.Verify(oo => oo.BuildConnection(It.IsAny<IDatabaseServerCredentials>(), "CopyDatabaseSourceTest"));
    }

    [Fact]
    public async Task ShouldRequestViewsFromSelectedDatabase()
    {
        var connection = new UTDbConnection();
        var connectionStringBuilder = new Mock<IConnectionStringBuilder>();
        connectionStringBuilder
            .Setup(oo => oo.BuildConnection(It.IsAny<IDatabaseServerCredentials>(), "CopyDatabaseSourceTest"))
            .Returns("view-connection".ToSecureString());

        var sut = new GetSchemaObjectListCommandHandler(
            Mock.Of<IDbConnectionFactory>(oo => oo.GetDbConnection(It.IsAny<SecureString>(), DatabaseProvider.MsSQLServer) == connection),
            Mock.Of<IConnectionStringBuilderFactory>(oo => oo.GetConnectionStringBuilder(DatabaseProvider.MsSQLServer) == connectionStringBuilder.Object));

        await sut.Handle(new GetSchemaObjectList
        {
            DatabaseName = "CopyDatabaseSourceTest",
            ObjectType = SchemaObjectType.View,
            ServerCredentials = new DatabaseServerTestCredentials { DataSource = "127.0.0.1,14333" }
        }, CancellationToken.None);

        Assert.Contains("INFORMATION_SCHEMA.VIEWS", connection.dbCommand.CommandText);
        connectionStringBuilder.Verify(oo => oo.BuildConnection(It.IsAny<IDatabaseServerCredentials>(), "CopyDatabaseSourceTest"));
    }
}
