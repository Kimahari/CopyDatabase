
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CopyDatabase.Controls;
using CopyDatabase.Core.Requests;
using CopyDatabase.Models;

using MahApps.Metro.Controls.Dialogs;

namespace CopyDatabase.ViewModels;

internal sealed partial class DatabaseServerViewModel : ObservableObject
{
    private CancellationTokenSource ts = new();
    private IMediator mediator;
    private readonly IDialogCoordinator _dialogCoordinator;

    [ObservableProperty] DatabaseServerCredentials credentials = new();

    [ObservableProperty] bool connected;

    [ObservableProperty] string busyMessage = "";

    [ObservableProperty] bool busy;

    [ObservableProperty] bool hasError;
    [ObservableProperty] bool connectionSuccess;

    [ObservableProperty] string errorMessage = "";

    [ObservableProperty] bool advancedEdit;

    [ObservableProperty] ObservableCollection<string> databaseNames = new ObservableCollection<string>();

    private CustomDialog customDialog;

    public DatabaseServerViewModel(IMediator mediator)
    {
        this.mediator = mediator;
        _dialogCoordinator = DialogCoordinator.Instance;
        this.customDialog = new CustomDialog { Title = "Edit Server Credentials" };
        customDialog.Content = new EditDatabaseServerCredentials() { DataContext = this };
    }

    public DatabaseServerViewModel() { }

    [RelayCommand]
    async Task TestConnection()
    {
        Reset();

        Busy = true;
        BusyMessage = "connecting";

        try
        {
            ConnectionSuccess = await mediator.Send(new TestDBConnection() { Credentials = Credentials }, ts.Token);
        }
        catch (TaskCanceledException)
        {
            SetError("Operation canceled");
        }
        catch (Exception e)
        {
            SetError(e.Message);
        }

        Busy = false;
    }

    [RelayCommand]
    async Task ConnectToDatabase()
    {
        await TestConnection();
        if (!ConnectionSuccess) return;
        var dbList = await mediator.Send(new GetDatabaseList() { ServerCredentials = Credentials }, ts.Token);
        var dbsToRemove = DatabaseNames.Except(dbList);
        var newDbs = dbList.Except(DatabaseNames);

        foreach (var db in dbsToRemove) DatabaseNames.Remove(db);
        foreach (var db in newDbs) DatabaseNames.Add(db);
    }

    [RelayCommand]
    async Task ToggleAdvancedEdit()
    {
        if (!this.AdvancedEdit)
        {
            await _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
            this.AdvancedEdit = true;
            (this.customDialog.Content as EditDatabaseServerCredentials)!.FocusControls();
        }
        else
        {
            await customDialog.RequestCloseAsync();
            await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
            this.AdvancedEdit = false;
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    void Reset()
    {
        ConnectionSuccess = false;
        HasError = false;
        ErrorMessage = "";
    }

    [RelayCommand]
    void CancelTestConnection()
    {
        ts.Cancel();
        ts.Dispose();
        ts = new();
    }
}
