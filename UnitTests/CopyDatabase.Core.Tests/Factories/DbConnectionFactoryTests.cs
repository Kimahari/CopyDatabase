

namespace CopyDatabase.Core.Factories.Tests; 

public class DbConnectionFactoryTests {
    private DbConnectionFactory sut;

    public DbConnectionFactoryTests() {
        this.sut = new DbConnectionFactory();
    }

    [Fact()]
    public void ShouldBeAbleToGetSQLConnection() {
        var connectionString = "".ToSecureString();
        using var connection = sut.GetDbConnection(connectionString, DatabaseProvider.MsSQLServer);
        Assert.NotNull(connection);
    }

    [Fact()]
    public void ShouldThrowExceptionWhenProviderIsNotSupported() {
        var connectionString = "".ToSecureString();
        Assert.Throws<NotImplementedException>(() => sut.GetDbConnection(connectionString, DatabaseProvider.NotKnown));
    }
}