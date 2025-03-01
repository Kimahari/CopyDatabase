﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Prism.Commands;

namespace DataBaseCompare.Models {

    public class DataBaseModel :ModelBase {

        #region Private Fields

        private bool isSelected;
        private string name;
        private bool showTables;

        #endregion Private Fields

        #region Private Methods

        private void CleanEnries() {
            Tables.Clear();
            Views.Clear();
            Routines.Clear();
        }

        private void CommitTransactionIfNoErrors(CopyToArguments args) {
            if(string.IsNullOrEmpty(Error)) {
                args.Transaction.Commit();
                args.Transaction = args.Connection.BeginTransaction();
            }
        }

        private async Task CopyDatabaseObjectsAsync(CopyToArguments args) {
            await CopyTablesToDestinationAsync(args).ConfigureAwait(false);

            await CopyRoutinesToDestinationAsync(args).ConfigureAwait(false);

            await CopyViewsToDestinationAsync(args).ConfigureAwait(false);
        }

        private async Task CopyDatabaseObjectToAsync(ScriptedModel Model, CopyToArguments args) {
            try {
                await Model.CopyToAsync(args).ConfigureAwait(false);
            } catch(Exception ex) {
                Error = $"Faled to copy {Model} - {ex.Message}";
            }
        }

        private async Task CopyRoutinesToDestinationAsync(CopyToArguments args) {
            try {
                if (!args.Recreate) return;

                foreach (RoutineModel routine in Routines) {
                    if(!string.IsNullOrEmpty(Error)) {
                        break;
                    }
                    
                    Message = $"Copying ({routine.Type}) {routine.Name}";
                    await CopyDatabaseObjectToAsync(routine, args);
                    CommitTransactionIfNoErrors(args);
                }
                CommitTransactionIfNoErrors(args);
            } catch(Exception ex) {
                Error = ex.Message;
            }
        }

        private async Task CopyTables(CopyToArguments copyArguments, bool copyTables) {
            if(!copyTables) return;

            await CopyDatabaseObjectsAsync(copyArguments);

            if(string.IsNullOrEmpty(Error)) {
                copyArguments.Transaction.Commit();
            } else {
                copyArguments.Transaction.Rollback();
            }

            if(!string.IsNullOrEmpty(Error)) {
                throw new Exception(Error);
            }
        }

        private async Task CopyTablesToDestinationAsync(CopyToArguments args) {
            try {
                int counter = 1;

                foreach(TableModel table in Tables) {
                    if(!string.IsNullOrEmpty(Error)) {
                        break;
                    }

                    Message = $"Copying Table {counter} of {Tables.Count} ({table.Name})";
                    try {
                        await table.CopyToAsync(args, (rows) => {
                            Message = $"Copying Table {counter} of {Tables.Count} ({table.Name}) - ({rows} Rows Copied) ";
                        }).ConfigureAwait(false);
                        counter++;
                    } catch(Exception ex) {
                        Error = $"Faled to copy table [{table.Name}] - {ex.Message}";
                    }
                }

                CommitTransactionIfNoErrors(args);
            } catch(Exception ex) {
                Error = ex.Message;
            }
        }

        private async Task CopyViewsToDestinationAsync(CopyToArguments args) {
            try {
                if (!args.Recreate) return;

                foreach (ScriptedModel view in Views) {
                    if(!string.IsNullOrEmpty(Error)) {
                        break;
                    }

                    Message = $"Copying View {view.Name}";
                    await CopyDatabaseObjectToAsync(view, args);
                }

                CommitTransactionIfNoErrors(args);
            } catch(Exception ex) {
                Error = ex.Message;
            }
        }

        private CopyToArguments CreateArguments(ConnectionModel destinationConnection, bool copyData, SqlConnection connection, CancellationToken token) {
            return new CopyToArguments {
                SourceModel = ConnectionModel,
                DestinationConnection = destinationConnection,
                CopyData = copyData,
                Connection = connection,
                Transaction = connection.BeginTransaction(),
                Token = token,
                DatabaseName = Name
            };
        }

        private async Task EnsureCreatedAsync(ConnectionModel destinationConnection, CancellationToken token) {
            Message = "Craeting Database on Destination";
            await Task.Factory.StartNew(() => {
                using(SqlConnection connection = new SqlConnection(destinationConnection.BuildConnection())) {
                    connection.Open();
                    using(SqlCommand cmd = new SqlCommand($@"CREATE DATABASE [{Name}]", connection)) {
                        cmd.ExecuteNonQuery();
                    }
                }

                EnsureSchemasCreated(destinationConnection);
            }).ConfigureAwait(false);
        }

        private async Task EnsureDeletedAsync(ConnectionModel destinationConnection, CancellationToken token) {
            Message = "Removing Database from destination";
            await Task.Factory.StartNew(() => {
                using(SqlConnection connection = new SqlConnection(destinationConnection.BuildConnection())) {
                    connection.Open();
                    using(SqlCommand cmd = new SqlCommand($@"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection)) {
                        cmd.ExecuteNonQuery();
                    }

                    using(SqlCommand cmd = new SqlCommand($@"DROP DATABASE [{Name}]", connection)) {
                        cmd.ExecuteNonQuery();
                    }
                }
            }).ConfigureAwait(false);
        }

        private void EnsureSchemasCreated(ConnectionModel destinationConnection) {
            foreach(string schema in Tables.GroupBy(ii => ii.Schema).Select(ii => ii.Key)) {
                if(schema == "dbo") {
                    continue;
                }

                using(SqlConnection connection = new SqlConnection(destinationConnection.BuildConnection(name))) {
                    connection.Open();
                    using(SqlCommand cmd = new SqlCommand($@"CREATE SCHEMA [{schema}]", connection)) {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private async Task<IEnumerable<RoutineModel>> GetDatabaseRoutinesAsync() {
            return await Task<IEnumerable<RoutineModel>>.Factory.StartNew(() => {
                const string sql = "SELECT SPECIFIC_SCHEMA AS [Schema] , SPECIFIC_NAME as [Name], B.definition as SCRIPT, A.ROUTINE_TYPE AS [Type] FROM INFORMATION_SCHEMA.ROUTINES A\r\n\tINNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(SPECIFIC_SCHEMA+'.'+SPECIFIC_NAME)";
                using(SqlConnection connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    try {
                        connection.Open();
                    } catch(Exception ex) {
                        Error = ex.Message;
                        return new List<RoutineModel>();
                    }
                    return connection.Query<RoutineModel>(sql);
                }
            });
        }

        private async Task<IEnumerable<TableModel>> GetDatabaseTablesAsync() {
            return await Task<IEnumerable<TableModel>>.Factory.StartNew(() => {
                using(SqlConnection connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    try {
                        connection.Open();
                    } catch(Exception ex) {
                        Error = ex.Message;
                        return new List<TableModel>();
                    }
                    return connection.Query<TableModel>(@"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'BASE TABLE'");
                }
            });
        }

        private async Task<IEnumerable<ScriptedModel>> GetDatabaseViewsAsync() {
            return await Task<IEnumerable<ScriptedModel>>.Factory.StartNew(() => {
                const string sql = @"SELECT TABLE_NAME AS Name, TABLE_SCHEMA AS [Schema], B.[definition] as SCRIPT FROM INFORMATION_SCHEMA.TABLES
INNER JOIN sys.sql_modules B ON object_id =  OBJECT_ID(TABLE_SCHEMA+'.'+TABLE_NAME)
WHERE TABLE_NAME not in ('sysdiagrams') AND TABLE_TYPE  = 'View' collate database_default";

                using(SqlConnection connection = new SqlConnection(ConnectionModel.BuildConnection(Name))) {
                    try {
                        connection.Open();
                    } catch(Exception ex) {
                        Error = ex.Message;
                        return new List<ScriptedModel>();
                    }
                    return connection.Query<RoutineModel>(sql);
                }
            });
        }

        private void LoadRoutines(IEnumerable<RoutineModel> data3) {
            foreach(RoutineModel item in data3) {
                item.DatabaseName = name;
                Routines.Add(item);
            }
        }

        private void LoadTables(IEnumerable<TableModel> data) {
            foreach(TableModel item in data) {
                item.DatabaseName = name;
                Tables.Add(item);
            }
        }

        private void LoadViews(IEnumerable<ScriptedModel> data2) {
            foreach(ScriptedModel item in data2) {
                item.DatabaseName = name;
                Views.Add(item);
            }
        }

        private async Task RecreateDatabase(ConnectionModel destinationConnection, IEnumerable<DataBaseModel> destinationDatabases, CancellationToken token) {
            await RemoveIfExistsAsync(destinationConnection, destinationDatabases, token);
            await EnsureCreatedAsync(destinationConnection, token).ConfigureAwait(false);
        }

        private async Task RemoveIfExistsAsync(ConnectionModel destinationConnection, IEnumerable<DataBaseModel> destinationDatabases, CancellationToken token) {
            if(destinationDatabases.Any(db => db.Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase))) {
                await EnsureDeletedAsync(destinationConnection, token).ConfigureAwait(false);
            }
        }

        #endregion Private Methods

        #region Internal Methods

        internal async Task CopyToAsync(ConnectionModel destinationConnection, IEnumerable<DataBaseModel> destinationDatabases, bool copyTables, bool copyData, bool dropDatabase, CancellationToken token) {
            StartOperation();
            try {
                if (dropDatabase) {
                    await RecreateDatabase(destinationConnection, destinationDatabases, token);
                }
                
                using(SqlConnection connection = new SqlConnection(destinationConnection.BuildConnection(name))) {
                    await connection.OpenAsync().ConfigureAwait(false);
                    CopyToArguments copyArguments = CreateArguments(destinationConnection, copyData, connection, token);
                    copyArguments.Recreate = dropDatabase == true;
                    await CopyTables(copyArguments, copyTables);
                }
            } catch(Exception ex) {
                Error = ex.Message;
                throw;
            } finally {
                IsBusy = false;
                Message = string.Empty;
            }
        }

        #endregion Internal Methods

        #region Public Constructors

        public DataBaseModel() {
            RefreshDatabaseTables = new DelegateCommand(() => OnLoadDatabaseTablesAsync().Wait());
            ShowTablesCommand = new DelegateCommand(() => ShowTables = true);
            HideTablesCommand = new DelegateCommand(() => ShowTables = false);
        }

        #endregion Public Constructors

        #region Public Properties

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

        #endregion Public Properties

        #region Public Methods

        public async Task OnLoadDatabaseTablesAsync() {
            IsBusy = true;

            CleanEnries();

            LoadTables(await GetDatabaseTablesAsync());
            LoadViews(await GetDatabaseViewsAsync());
            LoadRoutines(await GetDatabaseRoutinesAsync());

            IsBusy = false;
        }

        public override string ToString() {
            return $"{Name}";
        }

        #endregion Public Methods
    }
}
