using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using DataBaseCompare.Models;

using Newtonsoft.Json.Linq;

using Prism.Commands;

namespace DataBaseCompare.ViewModels {

    public class MainViewModel : ModelBase {

        #region Private Fields

        private bool copyData = true;
        private bool copyTables = true;
        private bool dropDatabase = true;
        private string title = "Copy Databases";

        #endregion Private Fields

        #region Private Methods

        private static void RefreshDatabases(IEnumerable<DataBaseModel> databases) {
            foreach (DataBaseModel item in databases) {
                item.RefreshDatabaseTables.Execute();
            }
        }

        private async Task<int> CopyDatabasesAsync(List<DataBaseModel> selectedDatabases, IEnumerable<DataBaseModel> destinationDatabases) {
            int counter = 1;

            foreach (DataBaseModel database in selectedDatabases) {
                if (CancelationSource.IsCancellationRequested) {
                    break;
                }

                Message = $"Copying {counter} from {selectedDatabases.Count} ({database.Name})";
                try {
                    await database.CopyToAsync(DestinationConnection, destinationDatabases, copyData, copyTables, dropDatabase, CancelationSource.Token);
                    counter++;
                } catch (Exception ex) {
                    Error = ex.Message;
                    CancelationSource.Cancel();
                }
            }

            return counter;
        }

        private async Task<IEnumerable<DataBaseModel>> GetConnectionDatabasesAsync(ConnectionModel con) {
            try {
                return await Task<IEnumerable<DataBaseModel>>.Factory.StartNew(() => {
                    using (SqlConnection connection = new SqlConnection(con.BuildConnection())) {
                        connection.Open();
                        const string sql = @"SELECT name FROM sys.Databases WHERE name not in ('master','tempdb','model','msdb')";
                        return connection.Query<DataBaseModel>(sql);
                    }
                });
            } catch (Exception ex) {
                Error = ex.Message;
                CancelationSource?.Cancel();
                return new List<DataBaseModel>();
            }
        }

        private void LoadDatabases(IEnumerable<DataBaseModel> databases) {
            int counter = 0;
            foreach (DataBaseModel item in databases) {
                item.ConnectionModel = SourceConnection;
                Databases.Add(item);
                if (counter % 3 == 0) {
                    System.Windows.Forms.Application.DoEvents();
                }
                counter++;
            }
        }

        private async void OnCopySelectedDatabasesAsync() {
            StartOperation();
            CancelationSource = new CancellationTokenSource();

            List<DataBaseModel> selectedDatabases = Databases.Where(db => db.IsSelected).ToList();

            foreach (DataBaseModel db in selectedDatabases) {
                await db.OnLoadDatabaseTablesAsync();
            }

            IEnumerable<DataBaseModel> destinationDatabases = await GetConnectionDatabasesAsync(DestinationConnection);

            await CopyDatabasesAsync(selectedDatabases, destinationDatabases);

            Message = string.Empty;
            IsBusy = false;
        }

        private async void OnLoadSourceTablesAsync() {
            IsBusy = true;

            await SourceConnection.TestDBConnectionAsync();

            Databases.Clear();

            string instance = SourceConnection.ServerInstance;

            IEnumerable<DataBaseModel> databases = await GetConnectionDatabasesAsync(SourceConnection);

            LoadDatabases(databases);

            //RefreshDatabases(databases);

            IsBusy = false;
        }

        #endregion Private Methods

        #region Public Constructors

        public MainViewModel() {
            SourceConnection = new ConnectionModel { ServerInstance = @".\SQLExpress16" };
            DestinationConnection = new ConnectionModel { ServerInstance = @".\SQLExpress" };

            if (File.Exists("./server-source.conf")) {
                JObject conf = JObject.Parse(File.ReadAllText("./server-source.conf"));
                SourceConnection.ServerInstance = conf["ServerInstance"].ToString();
                SourceConnection.UserName = conf["UserName"].ToString();
            }

            if (File.Exists("./server-dest.conf")) {
                JObject conf = JObject.Parse(File.ReadAllText("./server-dest.conf"));
                DestinationConnection.ServerInstance = conf["ServerInstance"].ToString();
                DestinationConnection.UserName = conf["UserName"].ToString();
            }

            LoadSourceDatabases = new DelegateCommand(OnLoadSourceTablesAsync);
            CopySelectedDatabases = new DelegateCommand(OnCopySelectedDatabasesAsync);
            SelectAll = new DelegateCommand(() => {
                foreach (DataBaseModel item in Databases) {
                    item.IsSelected = true;
                }
            });
        }

        ~MainViewModel() {
            File.WriteAllText("./server-source.conf", JObject.FromObject(new {
                SourceConnection.ServerInstance,
                SourceConnection.UserName
            }).ToString());

            File.WriteAllText("./server-dest.conf", JObject.FromObject(new {
                DestinationConnection.ServerInstance,
                DestinationConnection.UserName
            }).ToString());
        }

        #endregion Public Constructors

        #region Public Properties

        public CancellationTokenSource CancelationSource { get; private set; }

        public bool DropDatabase {
            get => dropDatabase;
            set => SetProperty(ref dropDatabase, value);
        }

        public bool CopyData {
            get => copyData;
            set => SetProperty(ref copyData, value);
        }

        public DelegateCommand CopySelectedDatabases { get; }

        public bool CopyTables {
            get => copyTables;
            set => SetProperty(ref copyTables, value);
        }

        public ObservableCollection<DataBaseModel> Databases { get; set; } = new ObservableCollection<DataBaseModel>();

        public ConnectionModel DestinationConnection { get; }

        public DelegateCommand LoadSourceDatabases { get; }

        public DelegateCommand SelectAll { get; }

        public ConnectionModel SourceConnection { get; }

        public string Title {
            get => title;
            set => SetProperty(ref title, value);
        }

        #endregion Public Properties
    }
}
