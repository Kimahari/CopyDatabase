using Dapper;
using DataBaseCompare.Tools;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {

    public class DataBaseModel : ModelBase {

        #region Fields

        private bool isSelected;
        private string name;
        private bool showTables;

        #endregion Fields

        #region Constructors

        public DataBaseModel() {
            RefreshDatabaseTables = new DelegateCommand(OnLoadDatabaseTablesAsync);
            ShowTablesCommand = new DelegateCommand(() => ShowTables = true);
            HideTablesCommand = new DelegateCommand(() => ShowTables = false);
        }

        #endregion Constructors

        #region Properties

        public ConnectionModel ConnectionModel { get; internal set; }

        public DelegateCommand HideTablesCommand { get; }

        public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }

        public string Name { get => name; set => SetProperty(ref name, value); }

        public DelegateCommand RefreshDatabaseTables { get; }

        public ObservableCollection<RoutineModel> Routines { get; private set; } = new ObservableCollection<RoutineModel>();

        public bool ShowTables { get => showTables; set => SetProperty(ref showTables, value); }

        public DelegateCommand ShowTablesCommand { get; }

        public ObservableCollection<TableModel> Tables { get; private set; } = new ObservableCollection<TableModel>();

        public ObservableCollection<ScriptedModel> Views { get; private set; } = new ObservableCollection<ScriptedModel>();

        #endregion Properties

        #region Methods

        public override string ToString() => $"{Name}";

        internal async Task CopyToAsync(ConnectionModel destinationConnection, IEnumerable<DataBaseModel> destinationDatabases, bool copyTables, bool copyData, CancellationToken token) {
            this.IsBusy = true;
            Error = "";

            try {
                if (destinationDatabases.Any(db => db.Name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase)))
                    await EnsureDeletedAsync(destinationConnection, token).ConfigureAwait(false);

                await EnsureCreatedAsync(destinationConnection, token).ConfigureAwait(false);

                if (copyTables) {
                    using (SqlConnection connection = new SqlConnection(destinationConnection.BuildConnection(name))) {
                        await connection.OpenAsync().ConfigureAwait(false);
                        var transaction = connection.BeginTransaction();

                        transaction = await CopyTablesToDestinationAsync(destinationConnection, copyData, connection, transaction, token).ConfigureAwait(false);

                        transaction = await CopyRoutinesToDestinationAsync(destinationConnection, copyData, connection, transaction, token).ConfigureAwait(false);

                        transaction = await CopyViewsToDestinationAsync(destinationConnection, copyData, connection, transaction, token).ConfigureAwait(false);

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

        private void CleanEnries() {
            Tables.Clear();
            Views.Clear();
            Routines.Clear();
        }

        private SqlTransaction CommitTransactionIfNoErrors(SqlConnection connection, SqlTransaction transaction) {
            if (String.IsNullOrEmpty(Error)) {
                transaction.Commit();
                transaction = connection.BeginTransaction();
            }

            return transaction;
        }

        private async Task<SqlTransaction> CopyRoutinesToDestinationAsync(ConnectionModel destinationConnection, bool copyData, SqlConnection connection, SqlTransaction transaction, CancellationToken token) {
            try {
                foreach (var routine in Routines) {
                    if (!String.IsNullOrEmpty(Error)) break;
                    this.Message = $"Copying ({routine.Type}) {routine.Name}";
                    try {
                        await routine.CopyToAsync(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction).ConfigureAwait(false);
                    } catch (Exception ex) {
                        this.Error = $"Faled to copy ({routine.Type}) [{routine.Name}] - {ex.Message}";
                    }
                }

                transaction = CommitTransactionIfNoErrors(connection, transaction);
            } catch (Exception ex) {
                this.Error = ex.Message;
            }

            return transaction;
        }

        private async Task<SqlTransaction> CopyTablesToDestinationAsync(ConnectionModel destinationConnection, bool copyData, SqlConnection connection, SqlTransaction transaction, CancellationToken token) {
            try {
                var tableCount = Tables.Count;
                var counter = 1;

                foreach (var table in Tables) {
                    if (!String.IsNullOrEmpty(Error)) break;
                    this.Message = $"Copying Table {counter} of {tableCount} ({table.Name})";
                    try {
                        await table.CopyToAsync(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction, (rows) => {
                            this.Message = $"Copying Table {counter} of {tableCount} ({table.Name}) - ({rows} Rows Copied) ";
                        }).ConfigureAwait(false);
                        counter++;
                    } catch (Exception ex) {
                        this.Error = $"Faled to copy table [{table.Name}] - {ex.Message}";
                    }
                }

                transaction = CommitTransactionIfNoErrors(connection, transaction);
            } catch (Exception ex) {
                this.Error = ex.Message;
            }

            return transaction;
        }

        private async Task<SqlTransaction> CopyViewsToDestinationAsync(ConnectionModel destinationConnection, bool copyData, SqlConnection connection, SqlTransaction transaction, CancellationToken token) {
            try {
                foreach (var view in Views) {
                    if (!String.IsNullOrEmpty(Error)) break;
                    this.Message = $"Copying View {view.Name}";
                    try {
                        await view.CopyToAsync(this.ConnectionModel, destinationConnection, name, copyData, token, connection, transaction).ConfigureAwait(false);
                    } catch (Exception ex) {
                        this.Error = $"Faled to copy View [{view.Name}] - {ex.Message}";
                    }
                }

                transaction = CommitTransactionIfNoErrors(connection, transaction);
            } catch (Exception ex) {
                this.Error = ex.Message;
            }

            return transaction;
        }

        private async Task EnsureCreatedAsync(ConnectionModel destinationConnection, CancellationToken token) {
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
            }).ConfigureAwait(false);
        }

        private async Task EnsureDeletedAsync(ConnectionModel destinationConnection, CancellationToken token) {
            this.Message = "Removing Database from destination";
            await Task.Factory.StartNew(() => {
                using (var connection = new SqlConnection(destinationConnection.BuildConnection())) {
                    connection.Open();
                    using (var cmd = new SqlCommand($@"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqlCommand($@"DROP DATABASE [{Name}]", connection)) cmd.ExecuteNonQuery();
                }
            }).ConfigureAwait(false);
        }

        private async Task<IEnumerable<RoutineModel>> GetDatabaseRoutinesAsync() => await Task<IEnumerable<RoutineModel>>.Factory.StartNew(() => {
            const string sql = "SELECT SPECIFIC_SCHEMA AS [Schema] , SPECIFIC_NAME as [Name], B.definition as SCRIPT, A.ROUTINE_TYPE AS [Type] FROM INFORMATION_SCHEMA.ROUTINES A\r\n\tINNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(SPECIFIC_SCHEMA+'.'+SPECIFIC_NAME)";
            using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                connection.Open();
                return connection.Query<RoutineModel>(sql);
            }
        });

        private async Task<IEnumerable<TableModel>> GetDatabaseTablesAsync() => await Task<IEnumerable<TableModel>>.Factory.StartNew(() => {
            using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                connection.Open();
                return connection.Query<TableModel>(@"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'BASE TABLE'");
            }
        });

        private async Task<IEnumerable<ScriptedModel>> GetDatabaseViewsAsync() => await Task<IEnumerable<ScriptedModel>>.Factory.StartNew(() => {
            const string sql = @"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema], B.[definition] as SCRIPT FROM INFORMATION_SCHEMA.TABLES
INNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(TABLE_SCHEMA+'.'+TABLE_NAME)
WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'View' collate database_default";

            using (var connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                connection.Open();
                return connection.Query<RoutineModel>(sql);
            }
        });

        private void LoadRoutines(IEnumerable<RoutineModel> data3) {
            foreach (var item in data3) {
                item.DatabaseName = name;
                this.Routines.Add(item);
            }
        }

        private void LoadTables(IEnumerable<TableModel> data) {
            foreach (var item in data) {
                item.DatabaseName = name;
                this.Tables.Add(item);
            }
        }

        private void LoadViews(IEnumerable<ScriptedModel> data2) {
            foreach (var item in data2) {
                item.DatabaseName = name;
                this.Views.Add(item);
            }
        }

        private async void OnLoadDatabaseTablesAsync() {
            IsBusy = true;

            CleanEnries();

            LoadTables(await GetDatabaseTablesAsync());
            LoadViews(await GetDatabaseViewsAsync());
            LoadRoutines(await GetDatabaseRoutinesAsync());

            IsBusy = false;
        }

        #endregion Methods
    }
}
