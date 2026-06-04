namespace CopyDatabase.Core.Requests.Tests;

public sealed class GetSchemaObjectListCommandHandlerTests
{
    [Fact]
    public async Task ShouldRequestBaseTablesFromSelectedDatabaseThroughProviderCatalog()
    {
        var catalogProvider = new FakeDatabaseCatalogProvider { SchemaObjectNames = ["dbo.CopyDatabaseUiTable"] };
        var sut = new GetSchemaObjectListCommandHandler(Mock.Of<IDatabaseCatalogProviderFactory>(
            oo => oo.GetDatabaseCatalogProvider(DatabaseProvider.MsSQLServer) == catalogProvider));

        var result = await sut.Handle(new GetSchemaObjectList
        {
            DatabaseName = "CopyDatabaseSourceTest",
            ObjectType = SchemaObjectType.Table,
            ServerCredentials = new DatabaseServerTestCredentials { DataSource = "127.0.0.1,14333" }
        }, CancellationToken.None);

        Assert.Equal(["dbo.CopyDatabaseUiTable"], result);
        Assert.Equal("CopyDatabaseSourceTest", catalogProvider.DatabaseName);
        Assert.Equal(SchemaObjectType.Table, catalogProvider.ObjectType);
    }

    [Fact]
    public async Task ShouldRequestViewsFromSelectedDatabaseThroughProviderCatalog()
    {
        var catalogProvider = new FakeDatabaseCatalogProvider { SchemaObjectNames = ["dbo.CopyDatabaseUiView"] };
        var sut = new GetSchemaObjectListCommandHandler(Mock.Of<IDatabaseCatalogProviderFactory>(
            oo => oo.GetDatabaseCatalogProvider(DatabaseProvider.MsSQLServer) == catalogProvider));

        var result = await sut.Handle(new GetSchemaObjectList
        {
            DatabaseName = "CopyDatabaseSourceTest",
            ObjectType = SchemaObjectType.View,
            ServerCredentials = new DatabaseServerTestCredentials { DataSource = "127.0.0.1,14333" }
        }, CancellationToken.None);

        Assert.Equal(["dbo.CopyDatabaseUiView"], result);
        Assert.Equal("CopyDatabaseSourceTest", catalogProvider.DatabaseName);
        Assert.Equal(SchemaObjectType.View, catalogProvider.ObjectType);
    }

    private sealed class FakeDatabaseCatalogProvider : IDatabaseCatalogProvider
    {
        public string[] SchemaObjectNames { get; set; } = [];
        public string DatabaseName { get; private set; } = "";
        public SchemaObjectType ObjectType { get; private set; }

        public Task<string[]> GetDatabaseNamesAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string[]> GetSchemaObjectNamesAsync(
            IDatabaseServerCredentials credentials,
            string databaseName,
            SchemaObjectType objectType,
            CancellationToken cancellationToken)
        {
            DatabaseName = databaseName;
            ObjectType = objectType;
            return Task.FromResult(SchemaObjectNames);
        }
    }
}
