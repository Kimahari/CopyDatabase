namespace CopyDatabase.MsSQLServer.Tests.MsSQLServer;

public sealed class MsSQLConnectionStringBuilderTests
{
    [Fact]
    public void ShouldKeepLocalSqlServerConnectionsCompatibleWithMicrosoftDataSqlClient()
    {
        var sut = new MsSQLConnectionStringBuilder();

        var connectionString = sut.BuildConnection(new DatabaseServerTestCredentials
        {
            DataSource = "localhost,14333",
            UserName = "sa",
            Password = "CopyDb_Test_12345!".ToSecureString()
        }).FromSecureString();

        Assert.Contains("Data Source=localhost,14333;", connectionString);
        Assert.Contains("Integrated Security=False;", connectionString);
        Assert.Contains("User ID=sa;", connectionString);
        Assert.Contains("Password=CopyDb_Test_12345!;", connectionString);
        Assert.Contains("Encrypt=False;", connectionString);
    }
}
