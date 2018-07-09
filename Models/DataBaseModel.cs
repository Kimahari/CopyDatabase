using Dapper;
using DataBaseCompare.Tools;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DataBaseCompare.Models {

    public class DataBaseModel : ModelBase {
        private string name;

        public DataBaseModel() {
            this.RefreshDatabaseTables = new DelegateCommand(onLoadDatabaseTables);
            this.ShowTablesCommand = new DelegateCommand(() => ShowTables = true);
            this.HideTablesCommand = new DelegateCommand(() => ShowTables = false);
        }

        private async void onLoadDatabaseTables() {
            IsBusy = true;
            Tables.Clear();
            Views.Clear();
            Routines.Clear();

            var data = await GetDatabaseTables();
            var data2 = await GetDatabaseViews();
            var data3 = await GetDatabaseRoutines();

            foreach (var item in data) {
                item.DatabaseName = name;
                this.Tables.Add(item);
            }

            foreach (var item in data2) {
                item.DatabaseName = name;
                this.Views.Add(item);
            }

            foreach (var item in data3) {
                item.DatabaseName = name;
                this.Routines.Add(item);
            }

            IsBusy = false;
        }

        private async Task<IEnumerable<TableModel>> GetDatabaseTables() {
            return await Task<IEnumerable<TableModel>>.Factory.StartNew(() => {
                using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    connection.Open();
                    return connection.Query<TableModel>(@"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'BASE TABLE'");
                }
            });
        }

        private async Task<IEnumerable<RoutineModel>> GetDatabaseRoutines() {
            return await Task<IEnumerable<RoutineModel>>.Factory.StartNew(() => {
                var sql = @"SELECT SPECIFIC_SCHEMA AS [Schema] , SPECIFIC_NAME as [Name], B.definition as SCRIPT, A.ROUTINE_TYPE AS [Type] FROM INFORMATION_SCHEMA.ROUTINES A
	INNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(SPECIFIC_SCHEMA+'.'+SPECIFIC_NAME)";
                using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    connection.Open();
                    return connection.Query<RoutineModel>(sql);
                }
            });
        }

        private async Task<IEnumerable<ScriptedModel>> GetDatabaseViews() {
            return await Task<IEnumerable<ScriptedModel>>.Factory.StartNew(() => {
                var sql = @"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema], B.[definition] as SCRIPT FROM INFORMATION_SCHEMA.TABLES 
INNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(TABLE_SCHEMA+'.'+TABLE_NAME)
WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'View' collate database_default";

                using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    connection.Open();
                    return connection.Query<RoutineModel>(sql);
                }
            });
        }

        public string Name {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private bool showTables;

        public bool ShowTables {
            get { return showTables; }
            set { SetProperty(ref showTables, value); }
        }

        private bool isSelected;

        public bool IsSelected {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        public ConnectionModel ConnectionModel { get; internal set; }
        public DelegateCommand RefreshDatabaseTables { get; }
        public DelegateCommand ShowTablesCommand { get; }
        public DelegateCommand HideTablesCommand { get; }

        public ObservableCollection<TableModel> Tables { get; set; } = new ObservableCollection<TableModel>();
        public ObservableCollection<ScriptedModel> Views { get; set; } = new ObservableCollection<ScriptedModel>();
        public ObservableCollection<RoutineModel> Routines { get; set; } = new ObservableCollection<RoutineModel>();

        internal async Task CopyTo(ConnectionModel destinationConnection, IEnumerable<DataBaseModel> destinationDatabases, bool copyTables, bool copyData, CancellationToken token) {
            this.IsBusy = true;
            Error = "";

            try {
                if (destinationDatabases.Any(db => db.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase)))
                    await EnsureDeleted(destinationConnection, token);

                await EnsureCreated(destinationConnection, token);

                if (copyTables) {
                    using (var connection = new SqlConnection(destinationConnection.BuildConnection(name))) {
                        connection.Open();
                        var transaction = connection.BeginTransaction();

                        try {
                            var tableCount = Tables.Count;
                            var counter = 1;

                            foreach (var table in Tables) {
                                if (!String.IsNullOrEmpty(Error)) break;
                                this.Message = $"Copying Table {counter} of {tableCount} ({table.Name})";
                                try {
                                    await table.CopyTo(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction, (rows) => {
                                        this.Message = $"Copying Table {counter} of {tableCount} ({table.Name}) - ({rows} Rows Copied) ";
                                    });
                                    counter++;
                                } catch (Exception ex) {
                                    this.Error = $"Faled to copy table [{table.Name}] - {ex.Message}";
                                }
                            }

                            if (String.IsNullOrEmpty(Error)) {
                                transaction.Commit();
                                transaction = connection.BeginTransaction();
                            }

                        } catch (Exception ex) {
                            this.Error = ex.Message;
                        }

                        try {
                            foreach (var routine in Routines) {
                                if (!String.IsNullOrEmpty(Error)) break;
                                this.Message = $"Copying ({routine.Type}) {routine.Name}";
                                try {
                                    await routine.CopyTo(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction);
                                } catch (Exception ex) {
                                    this.Error = $"Faled to copy ({routine.Type}) [{routine.Name}] - {ex.Message}";
                                }
                            }


                            if (String.IsNullOrEmpty(Error)) {
                                transaction.Commit();
                                transaction = connection.BeginTransaction();
                            }

                        } catch (Exception ex) {
                            this.Error = ex.Message;
                        }

                        try {
                            foreach (var view in Views) {
                                if (!String.IsNullOrEmpty(Error)) break;
                                this.Message = $"Copying View {view.Name}";
                                try {
                                    await view.CopyTo(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction);
                                } catch (Exception ex) {
                                    this.Error = $"Faled to copy View [{view.Name}] - {ex.Message}";
                                }
                            }

                            if (String.IsNullOrEmpty(Error)) {
                                transaction.Commit();
                                transaction = connection.BeginTransaction();
                            }
                        } catch (Exception ex) {
                            this.Error = ex.Message;
                        }

                        if (String.IsNullOrEmpty(Error)) transaction.Commit(); else transaction.Rollback();

                        if (!String.IsNullOrEmpty(Error)) throw new Exception(Error);
                    }
                }

            } catch (Exception ex) {
                this.Error = ex.Message;
                throw;
            } finally {
                this.Message = "";
                this.IsBusy = false;
            }
        }

        private async Task EnsureCreated(ConnectionModel destinationConnection, CancellationToken token) {
            this.Message = "Craeting Database on Destination";
            await Task.Factory.StartNew(() => {
                using (var connection = new SqlConnection(destinationConnection.BuildConnection())) {
                    connection.Open();
                    using (var cmd = new SqlCommand($@"CREATE DATABASE [{Name}]", connection)) cmd.ExecuteNonQuery();
                }

                foreach (var schema in Tables.GroupBy(ii => ii.Schema).Select(ii => ii.Key)) {
                    if (schema == "dbo") continue;
                    using (var connection = new SqlConnection(destinationConnection.BuildConnection(name))) {
                        connection.Open();
                        using (var cmd = new SqlCommand($@"CREATE SCHEMA [{schema}]", connection)) cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        private async Task EnsureDeleted(ConnectionModel destinationConnection, CancellationToken token) {
            this.Message = "Removing Database from destination";
            await Task.Factory.StartNew(() => {
                using (var connection = new SqlConnection(destinationConnection.BuildConnection())) {
                    connection.Open();
                    using (var cmd = new SqlCommand($@"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqlCommand($@"DROP DATABASE [{Name}]", connection)) cmd.ExecuteNonQuery();
                }
            });
        }

        public override string ToString() => $"{Name}";
    }
}
