using Dapper;
using DataBaseCompare.Models;
using DataBaseCompare.Tools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DataBaseCompare.ViewModels {
    public class MainViewModel : ModelBase {
        private string title = "Copy Databases";

        public string Title {
            get { return title; }
            set {
                SetProperty(ref title, value);
            }
        }

        private bool copyTables = true;

        public bool CopyTables {
            get { return copyTables; }
            set { SetProperty(ref copyTables, value); }
        }

        private bool copyData = true;

        public bool CopyData {
            get { return copyData; }
            set { SetProperty(ref copyData, value); }
        }

        public ConnectionModel SourceConnection { get; }
        public ConnectionModel DestinationConnection { get; }
        public DelegateCommand LoadSourceDatabases { get; }
        public DelegateCommand CopySelectedDatabases { get; }
        public DelegateCommand SelectAll { get; }
        public ObservableCollection<DataBaseModel> Databases { get; set; } = new ObservableCollection<DataBaseModel>();
        public CancellationTokenSource CancelationSource { get; private set; }

        public MainViewModel() {
            this.SourceConnection = new ConnectionModel() { ServerInstance = @".\SQLExpress16" };
            this.DestinationConnection = new ConnectionModel() { ServerInstance = @".\SQLExpress" };
            this.LoadSourceDatabases = new DelegateCommand(onLoadSourceTables);
            this.CopySelectedDatabases = new DelegateCommand(onCopySelectedDatabases);
            this.SelectAll = new DelegateCommand(() => {
                foreach (var item in Databases) {
                    item.IsSelected = true;
                }
            });
        }

        private async void onLoadSourceTables() {
            this.IsBusy = true;

            Parallel.ForEach(new[] { SourceConnection, DestinationConnection }, async (conn) => {
                await conn.TestDBConnection();
                System.Windows.Forms.Application.DoEvents();
            });

            this.Databases.Clear();
            System.Windows.Forms.Application.DoEvents();

            var instance = SourceConnection.ServerInstance;

            IEnumerable<DataBaseModel> databases = await GetConnectionDatabases(SourceConnection);

            foreach (var item in databases) {
                System.Windows.Forms.Application.DoEvents();
                item.ConnectionModel = SourceConnection;
                System.Windows.Forms.Application.DoEvents();
                this.Databases.Add(item);
                //Thread.Sleep(1);
                System.Windows.Forms.Application.DoEvents();
            }

            foreach (var item in databases) {
                System.Windows.Forms.Application.DoEvents();
                item.RefreshDatabaseTables.Execute();
                System.Windows.Forms.Application.DoEvents();
                //Thread.Sleep(1);
            }

            this.IsBusy = false;
        }

        private async void onCopySelectedDatabases() {
            IsBusy = true;
            Message = "";
            Error = "";

            this.CancelationSource = new CancellationTokenSource();

            var selectedDatabases = this.Databases.Where(db => db.IsSelected).ToList();

            if (this.CancelationSource.IsCancellationRequested) return;

            var destinationDatabases = await GetConnectionDatabases(DestinationConnection);

            if (this.CancelationSource.IsCancellationRequested) return;

            var counter = 1;

            foreach (var database in selectedDatabases) {
                if (this.CancelationSource.IsCancellationRequested) break;
                Message = $"Copying {counter} from {selectedDatabases.Count} ({database.Name})";
                try {
                    await database.CopyTo(DestinationConnection, destinationDatabases, this.copyData, this.copyTables, CancelationSource.Token);
                    counter++;
                } catch (Exception ex) {
                    Error = ex.Message;
                    this.CancelationSource.Cancel();
                }
            }

            Message = "";
            IsBusy = false;
        }

        private async Task<IEnumerable<DataBaseModel>> GetConnectionDatabases(ConnectionModel con) {
            try {
                return await Task<IEnumerable<DataBaseModel>>.Factory.StartNew(() => {
                    using (var connection = new SqlConnection(con.BuildConnection())) {
                        connection.Open();
                        var sql = @"SELECT name FROM sys.Databases WHERE name not in ('master','tempdb','model','msdb')";
                        return connection.Query<DataBaseModel>(sql);
                    }
                });
            } catch (Exception ex) {
                this.Error = ex.Message;
                this.CancelationSource.Cancel();
                return new List<DataBaseModel>();
            }

        }


    }
}
