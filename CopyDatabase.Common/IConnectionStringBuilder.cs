namespace CopyDatabase.Common; 
public interface IConnectionStringBuilder {
    public SecureString BuildConnection(IDatabaseServerCredentials credentials, string databaseName = "");
}
