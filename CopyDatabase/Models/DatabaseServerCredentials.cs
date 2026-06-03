namespace CopyDatabase.Models;

[INotifyPropertyChanged]
internal partial class DatabaseServerCredentials : IDatabaseServerCredentials {
    [ObservableProperty]
    string dataSource = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseWindowsAuth))]
    string userName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseWindowsAuth))]
    SecureString password = new();

    public bool UseWindowsAuth => userName.Length == 0 && password.Length == 0;
}
