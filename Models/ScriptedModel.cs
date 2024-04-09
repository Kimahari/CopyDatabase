using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {

    public class ScriptedModel : ModelBase {

        #region Protected Methods

        protected static async Task ExecuteSQL(string sql, SqlConnection connection, SqlTransaction transaction) {
            using (SqlCommand command = new SqlCommand(sql, connection, transaction)) {
                await command.ExecuteNonQueryAsync();
            }
        }

        #endregion Protected Methods

        #region Internal Methods

        internal virtual async Task CopyToAsync(CopyToArguments args, Action<long> callback = null) {
            await ExecuteSQL(Script, args.Connection, args.Transaction);
        }

        #endregion Internal Methods

        #region Public Properties

        public string DatabaseName { get; set; }
        public string Name { get; set; }
        public string Schema { get; set; } = "dbo";
        public string Script { get; set; }

        #endregion Public Properties
    }
}
