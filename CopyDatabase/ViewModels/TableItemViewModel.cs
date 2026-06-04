namespace CopyDatabase.ViewModels;

internal sealed partial class TableItemViewModel : ObservableObject
{
    [ObservableProperty] string name;
    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string error = "";

    public TableItemViewModel(string name)
    {
        this.name = name;
    }

    public string AutomationId => $"TableItem_{Name.Replace(" ", "_").Replace(".", "_")}";
    public string StatusText => !string.IsNullOrWhiteSpace(Error) ? $"Error: {Error}" : Status;

    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(StatusText));

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(StatusText));
}
