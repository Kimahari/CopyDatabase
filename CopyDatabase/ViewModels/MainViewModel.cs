using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using System.Collections.ObjectModel;
using CopyDatabase.Core.Requests;
using CopyDatabase.Core;

namespace CopyDatabase.ViewModels;

[INotifyPropertyChanged]
internal sealed partial class MainViewModel {
    private readonly ServiceProvider provider;
    private readonly IMediator mediatr;

    [ObservableProperty] DatabaseServerViewModel source;

    [ObservableProperty] DatabaseServerViewModel destination;

    [ObservableProperty] ObservableCollection<string> databases = new();

    [ObservableProperty] ObservableCollection<string> tableNames = new();

    [ObservableProperty] ObservableCollection<string> viewNames = new();
    
    [ObservableProperty] string selectedDatabase = "";

    public MainViewModel() {
        ServiceCollection collection = CreateServices();

        provider = collection.BuildServiceProvider();

        mediatr = provider.GetRequiredService<IMediator>();

        source = new(mediatr);
        destination = new(mediatr);
    }

    private static ServiceCollection CreateServices() {
        ServiceCollection collection = new();
        collection.AddMediatR(typeof(GetDatabaseList).Assembly);
        collection.RegisterCoreServices();
        return collection;
    }

    [RelayCommand]
    async Task LoadDatabases() {
        var data = await mediatr.Send(new GetDatabaseList() { ServerCredentials = source.Credentials });

        if (data is null) return;

        databases.Clear();

        foreach (var db in data) databases.Add(db);
    }

    async partial void OnSelectedDatabaseChanged(string value)
    {
        tableNames.Clear();
        viewNames.Clear();

        if (string.IsNullOrWhiteSpace(value)) return;

        var tables = await mediatr.Send(new GetSchemaObjectList
        {
            ServerCredentials = source.Credentials,
            DatabaseName = value,
            ObjectType = SchemaObjectType.Table
        });

        var views = await mediatr.Send(new GetSchemaObjectList
        {
            ServerCredentials = source.Credentials,
            DatabaseName = value,
            ObjectType = SchemaObjectType.View
        });

        foreach (var table in tables) tableNames.Add(table);
        foreach (var view in views) viewNames.Add(view);
    }
}
