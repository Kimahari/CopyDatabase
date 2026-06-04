namespace CopyDatabase.Core.Requests.Tests;

public sealed class TestDBConnectionHandlerTests
{
    [Fact]
    public async Task ReturnTrueWhenProviderCanConnect()
    {
        var connectionTester = new FakeDatabaseConnectionTester { CanConnect = true };
        var sut = new TestDBConnectionHandler(
            new DatabaseServerCredentialValidator(),
            Mock.Of<IDatabaseConnectionTesterFactory>(
                oo => oo.GetDatabaseConnectionTester(DatabaseProvider.MsSQLServer) == connectionTester));

        var result = await sut.Handle(new TestDBConnection
        {
            Credentials = new DatabaseServerTestCredentials
            {
                UseWindowsAuth = true,
                DataSource = "."
            },
        }, CancellationToken.None);

        Assert.True(result);
        Assert.True(connectionTester.ConnectionTested);
    }

    [Fact]
    public async Task ReturnFalseWhenProviderCannotConnect()
    {
        var connectionTester = new FakeDatabaseConnectionTester { CanConnect = false };
        var sut = new TestDBConnectionHandler(
            new DatabaseServerCredentialValidator(),
            Mock.Of<IDatabaseConnectionTesterFactory>(
                oo => oo.GetDatabaseConnectionTester(DatabaseProvider.MsSQLServer) == connectionTester));

        var result = await sut.Handle(new TestDBConnection
        {
            Credentials = new DatabaseServerTestCredentials
            {
                UseWindowsAuth = true,
                DataSource = "."
            },
        }, CancellationToken.None);

        Assert.False(result);
        Assert.True(connectionTester.ConnectionTested);
    }

    private sealed class FakeDatabaseConnectionTester : IDatabaseConnectionTester
    {
        public bool CanConnect { get; set; }
        public bool ConnectionTested { get; private set; }

        public Task<bool> TestConnectionAsync(IDatabaseServerCredentials credentials, CancellationToken cancellationToken)
        {
            ConnectionTested = true;
            return Task.FromResult(CanConnect);
        }
    }
}
