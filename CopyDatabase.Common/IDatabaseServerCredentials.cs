namespace CopyDatabase.Common; 

public interface IDatabaseServerCredentials {
    public string DataSource { get; set; }
    public string UserName { get; set; }
    public SecureString Password { get; set; }
    public bool UseWindowsAuth { get; }
}
