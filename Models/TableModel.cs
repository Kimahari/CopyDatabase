﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using DataBaseCompare.Tools;

namespace DataBaseCompare.Models {

    public class TableModel : ScriptedModel {

        #region Private Methods

        private void ConfigureBulkCopy(SqlBulkCopy bulkCopy) {
            bulkCopy.DestinationTableName = $"[{Schema}].[{Name}]";
            bulkCopy.SqlRowsCopied += OnRowsCopied;
            bulkCopy.BulkCopyTimeout = 9000;
            bulkCopy.NotifyAfter = 1;
            bulkCopy.EnableStreaming = true;
        }

        private void CopyTableData(SqlConnection connection, SqlTransaction transaction, SqlDataReader reader) {
            if (reader.HasRows) {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)) {
                    ConfigureBulkCopy(bulkCopy);
                    bulkCopy.WriteToServer(reader);
                    UnConfigureBulkCopy(bulkCopy);
                }
            }
        }

        private async Task CreateTableAsync(CopyToArguments args, Action<long> callback) {
            string sql = await ScriptTableAsync(args.SourceModel, args.DatabaseName);
            CurrentReportCallback = callback;
            await ExecuteSQL(sql, args.Connection, args.Transaction);
        }

        private void OnRowsCopied(object sender, SqlRowsCopiedEventArgs e) {
            CurrentReportCallback?.Invoke(e.RowsCopied);
        }

        private Task<string> ScriptTableAsync(ConnectionModel sourceModel, string databaseName) {
            return Task.Factory.StartNew(() => {
                using (SqlConnection connection = new SqlConnection(sourceModel.BuildConnection(databaseName))) {
                    connection.Open();
                    return connection.Query<string>(Constants.CreateTableScript.Replace("{Schema}.{Name}", $"{Schema}.{Name}")).FirstOrDefault()?.Replace("#TABLENAME", $"[{Name}]")?.Replace("IDENTITY(*,1)", $"IDENTITY(1,1)");
                }
            });
        }

        private void UnConfigureBulkCopy(SqlBulkCopy bulkCopy) {
            bulkCopy.SqlRowsCopied -= OnRowsCopied;
        }

        #endregion Private Methods

        #region Internal Methods

        internal override async Task CopyToAsync(CopyToArguments args, Action<long> callback = null) {
            await CreateTableAsync(args, callback);

            if (args.CopyData) {
                await Task.Factory.StartNew(() => {
                    using (SqlConnection sourceConnection = new SqlConnection(args.SourceModel.BuildConnection(args.DatabaseName))) {
                        sourceConnection.Open();
                        using (SqlCommand cmd = new SqlCommand($"SELECT * FROM [{Schema}].[{Name}]", sourceConnection)) {
                            using (SqlDataReader reader = cmd.ExecuteReader()) {
                                CopyTableData(args.Connection, args.Transaction, reader);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        #endregion Internal Methods

        #region Public Properties

        public Action<long> CurrentReportCallback { get; private set; }

        #endregion Public Properties
    }
}
