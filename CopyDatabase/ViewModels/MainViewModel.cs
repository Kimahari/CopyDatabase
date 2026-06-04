using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CopyDatabase.Core.Requests;
using CopyDatabase.Core;
using CopyDatabase.MsSQLServer;

namespace CopyDatabase.ViewModels;

internal sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private const string SourceServerConfigurationFile = "server-source.conf";
    private const string DestinationServerConfigurationFile = "server-dest.conf";
    private const string MainConfigurationFile = "app-main.conf";

    private readonly ServiceProvider provider;
    private readonly IMediator mediatr;
    private bool disposed;

    [ObservableProperty] DatabaseServerViewModel source;
    [ObservableProperty] DatabaseServerViewModel destination;
    [ObservableProperty] ObservableCollection<string> databases = new();
    [ObservableProperty] ObservableCollection<TableItemViewModel> tableItems = new();
    [ObservableProperty] ObservableCollection<string> viewNames = new();
    [ObservableProperty] string selectedDatabase = "";
    [ObservableProperty] bool isBusy;
    [ObservableProperty] bool copySchema = true;
    [ObservableProperty] bool copyData = true;
    [ObservableProperty] bool dropDestinationDatabase = true;
    [ObservableProperty] bool copyWithNewName;
    [ObservableProperty] string destinationDatabaseName = "";
    [ObservableProperty] string message = "";
    [ObservableProperty] string error = "";

    public string EffectiveDestinationDatabaseName => CopyWithNewName && !string.IsNullOrWhiteSpace(DestinationDatabaseName)
        ? DestinationDatabaseName.Trim()
        : SelectedDatabase;

    public string StatusText => !string.IsNullOrWhiteSpace(Error)
        ? $"Error: {Error}"
        : !string.IsNullOrWhiteSpace(Message)
            ? Message
            : "Idle";

    public MainViewModel()
    {
        ServiceCollection collection = CreateServices();

        provider = collection.BuildServiceProvider();
        mediatr = provider.GetRequiredService<IMediator>();

        Source = new(mediatr);
        Destination = new(mediatr);

        LoadConfiguration();
    }

    public void Dispose()
    {
        if (disposed) return;

        SaveConfiguration();
        provider.Dispose();
        disposed = true;
    }

    private static ServiceCollection CreateServices()
    {
        ServiceCollection collection = new();
        collection.AddLogging();
        collection.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(GetDatabaseList).Assembly));
        collection.RegisterCoreServices();
        collection.RegisterSqlServerServices();
        return collection;
    }

    [RelayCommand]
    async Task LoadSourceDatabases()
    {
        Error = "";
        Message = "Loading databases";
        IsBusy = true;

        try
        {
            var data = await mediatr.Send(new GetDatabaseList { ServerCredentials = Source.Credentials });
            ReplaceItems(Databases, data.Where(IsUserDatabase));

            if (!Databases.Contains(SelectedDatabase))
            {
                SelectedDatabase = Databases.FirstOrDefault() ?? "";
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Message = "";
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task CopySelectedDatabase()
    {
        Error = "";
        Message = "Copying selected database";
        IsBusy = true;
        ClearTableStatus();

        try
        {
            if (string.IsNullOrWhiteSpace(SelectedDatabase))
            {
                throw new InvalidOperationException("Select a source database first.");
            }

            if (CopyWithNewName && string.IsNullOrWhiteSpace(DestinationDatabaseName))
            {
                throw new InvalidOperationException("Destination database name is required when copying with a new name.");
            }

            if (TableItems.Count == 0) await LoadSelectedDatabaseObjects();

            var progress = new Progress<DatabaseCopyProgress>(ApplyCopyProgress);

            await mediatr.Send(new CopyDatabaseRequest
            {
                SourceCredentials = Source.Credentials,
                DestinationCredentials = Destination.Credentials,
                DatabaseName = SelectedDatabase,
                DestinationDatabaseName = EffectiveDestinationDatabaseName,
                CopySchema = CopySchema,
                CopyData = CopyData,
                DropDestinationDatabase = DropDestinationDatabase,
                Progress = progress
            });

            Message = "Copied";
            foreach (var table in TableItems.Where(oo => oo.Status != "Copied")) table.Status = "Copied";
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    async partial void OnSelectedDatabaseChanged(string value)
    {
        OnPropertyChanged(nameof(EffectiveDestinationDatabaseName));
        DestinationDatabaseName = CopyWithNewName ? DestinationDatabaseName : value;

        if (string.IsNullOrWhiteSpace(value))
        {
            tableItems.Clear();
            viewNames.Clear();
            return;
        }

        try
        {
            await LoadSelectedDatabaseObjects();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    partial void OnCopyWithNewNameChanged(bool value)
    {
        if (!value) DestinationDatabaseName = SelectedDatabase;
        OnPropertyChanged(nameof(EffectiveDestinationDatabaseName));
    }

    partial void OnDestinationDatabaseNameChanged(string value) => OnPropertyChanged(nameof(EffectiveDestinationDatabaseName));

    partial void OnMessageChanged(string value) => OnPropertyChanged(nameof(StatusText));

    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(StatusText));

    private async Task LoadSelectedDatabaseObjects()
    {
        Message = "Loading database objects";
        IsBusy = true;

        try
        {
            var tables = await LoadSchemaObjects(SelectedDatabase, SchemaObjectType.Table);
            var views = await LoadSchemaObjects(SelectedDatabase, SchemaObjectType.View);
            TableItems.Clear();
            foreach (var table in tables) TableItems.Add(new TableItemViewModel(table));

            ReplaceItems(ViewNames, views);
        }
        finally
        {
            Message = "";
            IsBusy = false;
        }
    }

    private Task<string[]> LoadSchemaObjects(string databaseName, SchemaObjectType objectType)
    {
        return mediatr.Send(new GetSchemaObjectList
        {
            ServerCredentials = Source.Credentials,
            DatabaseName = databaseName,
            ObjectType = objectType
        });
    }

    private void ApplyCopyProgress(DatabaseCopyProgress progress)
    {
        Message = progress.Message;

        if (!string.Equals(progress.ObjectType, "Table", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrWhiteSpace(progress.ObjectName)) return;

        var table = TableItems.FirstOrDefault(oo => oo.Name.Equals(progress.ObjectName, StringComparison.OrdinalIgnoreCase));
        if (table is null) return;

        table.Status = progress.Message;
        if (progress.Message.StartsWith("Copying data", StringComparison.OrdinalIgnoreCase)
            && !progress.Message.Contains("rows copied", StringComparison.OrdinalIgnoreCase))
        {
            table.Status = "Copying data";
        }
    }

    private void ClearTableStatus()
    {
        foreach (var table in TableItems)
        {
            table.Status = "Idle";
            table.Error = "";
        }
    }

    private void LoadConfiguration()
    {
        LoadCredentials(SourceServerConfigurationFile, Source.Credentials);
        LoadCredentials(DestinationServerConfigurationFile, Destination.Credentials);
        LoadMainConfiguration();
    }

    private void SaveConfiguration()
    {
        SaveCredentials(SourceServerConfigurationFile, Source.Credentials);
        SaveCredentials(DestinationServerConfigurationFile, Destination.Credentials);

        WriteConfiguration(MainConfigurationFile, new
        {
            CopySchema,
            CopyData,
            DropDestinationDatabase,
            CopyWithNewName,
            DestinationDatabaseName
        });
    }

    private static void LoadCredentials(string path, IDatabaseServerCredentials credentials)
    {
        if (!TryReadConfiguration(path, out var root)) return;

        try
        {
            var dataSource = ReadString(root!, "ServerInstance", "DataSource");
            if (dataSource is not null) credentials.DataSource = dataSource;

            var userName = ReadString(root!, "UserName");
            if (userName is not null) credentials.UserName = userName;
        }
        finally
        {
            root?.Dispose();
        }
    }

    private void LoadMainConfiguration()
    {
        if (!TryReadConfiguration(MainConfigurationFile, out var root)) return;

        try
        {
            CopySchema = ReadBoolean(root!, "CopySchema", "CopyTables") ?? CopySchema;
            CopyData = ReadBoolean(root!, "CopyData") ?? CopyData;
            DropDestinationDatabase = ReadBoolean(root!, "DropDestinationDatabase", "DropDatabase") ?? DropDestinationDatabase;
            CopyWithNewName = ReadBoolean(root!, "CopyWithNewName") ?? CopyWithNewName;
            DestinationDatabaseName = ReadString(root!, "DestinationDatabaseName") ?? DestinationDatabaseName;
        }
        finally
        {
            root?.Dispose();
        }
    }

    private static void SaveCredentials(string path, IDatabaseServerCredentials credentials)
    {
        WriteConfiguration(path, new
        {
            ServerInstance = credentials.DataSource,
            credentials.UserName
        });
    }

    private static bool TryReadConfiguration(string path, out JsonDocument? document)
    {
        document = null;

        if (!File.Exists(path)) return false;

        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            document?.Dispose();
            document = null;
            return false;
        }
        catch (IOException)
        {
            document?.Dispose();
            document = null;
            return false;
        }
    }

    private static string? ReadString(JsonDocument document, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind is JsonValueKind.String or JsonValueKind.Null)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static bool? ReadBoolean(JsonDocument document, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (document.RootElement.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.True) return true;
                if (property.ValueKind == JsonValueKind.False) return false;
            }
        }

        return null;
    }

    private static void WriteConfiguration<T>(string path, T configuration)
    {
        var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static bool IsUserDatabase(string databaseName)
    {
        return !databaseName.Equals("master", StringComparison.OrdinalIgnoreCase)
            && !databaseName.Equals("tempdb", StringComparison.OrdinalIgnoreCase)
            && !databaseName.Equals("model", StringComparison.OrdinalIgnoreCase)
            && !databaseName.Equals("msdb", StringComparison.OrdinalIgnoreCase);
    }
}
