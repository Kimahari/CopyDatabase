using Dapper;
using DataBaseCompare.Models;
using DataBaseCompare.Tools;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataBaseCompare.ViewModels {

    public class MainViewModel : ModelBase {

        #region Fields

        private bool copyData = true;
        private bool copyTables = true;
        private string title = "Copy Databases";

        #endregion Fields

        #region Constructors

        public MainViewModel() {
            this.SourceConnection = new ConnectionModel { ServerInstance = @".\SQLExpress16" };
            this.DestinationConnection = new ConnectionModel { ServerInstance = @".\SQLExpress" };
            this.LoadSourceDatabases = new DelegateCommand(OnLoadSourceTablesAsync);
            this.CopySelectedDatabases = new DelegateCommand(OnCopySelectedDatabasesAsync);
            this.SelectAll = new DelegateCommand(() => {
                foreach (var item in Databases) {
                    item.IsSelected = true;
                }
            });
        }

        #endregion Constructors

        #region Properties

        public CancellationTokenSource CancelationSource { get; private set; }

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

        #endregion Properties

        #region Methods

        private async Task<IEnumerable<DataBaseModel>> GetConnectionDatabasesAsync(ConnectionModel con) {
            try {
                return await Task<IEnumerable<DataBaseModel>>.Factory.StartNew(() => {
                    using (var connection = new SqlConnection(con.BuildConnection())) {
                        connection.Open();
                        const string sql = @"SELECT name FROM sys.Databases WHERE name not in ('master','tempdb','model','msdb')";
                        return connection.Query<DataBaseModel>(sql);
                    }
                });
            } catch (Exception ex) {
                this.Error = ex.Message;
                this.CancelationSource.Cancel();
                return new List<DataBaseModel>();
            }
        }

        private async void OnCopySelectedDatabasesAsync() {
            StartOperation();
            this.CancelationSource = new CancellationTokenSource();

            var selectedDatabases = this.Databases.Where(db => db.IsSelected).ToList();

            if (this.CancelationSource.IsCancellationRequested) return;

            var destinationDatabases = await GetConnectionDatabasesAsync(DestinationConnection);

            if (this.CancelationSource.IsCancellationRequested) return;

            var counter = 1;

            foreach (var database in selectedDatabases) {
                if (this.CancelationSource.IsCancellationRequested) break;
                Message = $"Copying {counter} from {selectedDatabases.Count} ({database.Name})";
                try {
                    await database.CopyToAsync(DestinationConnection, destinationDatabases, this.copyData, this.copyTables, CancelationSource.Token);
                    counter++;
                } catch (Exception ex) {
                    Error = ex.Message;
                    this.CancelationSource.Cancel();
                }
            }
            IsBusy = false;
        }

        private async void OnLoadSourceTablesAsync() {
            this.IsBusy = true;

            await SourceConnection.TestDBConnectionAsync();

            this.Databases.Clear();
            System.Windows.Forms.Application.DoEvents();

            var instance = SourceConnection.ServerInstance;

            var databases = await GetConnectionDatabasesAsync(SourceConnection);

            foreach (var item in databases) {
                item.ConnectionModel = SourceConnection;
                this.Databases.Add(item);
                System.Windows.Forms.Application.DoEvents();
            }

            foreach (var item in databases) {
                item.RefreshDatabaseTables.Execute();
                System.Windows.Forms.Application.DoEvents();
            }

            this.IsBusy = false;
        }

        #endregion Methods
    }
}
