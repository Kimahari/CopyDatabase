namespace CopyDatabase.Models;

internal partial class DatabaseServerCredentials : ObservableObject, IDatabaseServerCredentials
{
    [ObservableProperty]
    string dataSource = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseWindowsAuth))]
    string userName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseWindowsAuth))]
    SecureString password = new();

    public bool UseWindowsAuth => UserName.Length == 0 && Password.Length == 0;
}
