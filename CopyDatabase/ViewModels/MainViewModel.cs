using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using System.Collections.ObjectModel;
using CopyDatabase.Core.Requests;

namespace CopyDatabase.ViewModels;

[INotifyPropertyChanged]
internal partial class MainViewModel {
    private readonly ServiceProvider provider;
    private readonly IMediator mediatr;

    [ObservableProperty] DatabaseServerViewModel source;

    [ObservableProperty] DatabaseServerViewModel destination;

    [ObservableProperty] ObservableCollection<string> databases = new();

    [ObservableProperty] string selectedDatabase = "";

    public MainViewModel() {
        ServiceCollection collection = new();
        collection.AddMediatR(typeof(GetDatabaseList).Assembly);
        provider = collection.BuildServiceProvider();
        
        mediatr = provider.GetRequiredService<IMediator>();
        
        source = new(mediatr);
        destination = new(mediatr);
    }

    [RelayCommand]
    async Task LoadDatabases() {
        var data = await mediatr.Send(new GetDatabaseList() { ServerCredentials = source.Credentials });

        if (data is null) return;

        databases.Clear();

        foreach (var db in data) databases.Add(db);
    }
}
