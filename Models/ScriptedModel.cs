using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {

    public class ScriptedModel : ModelBase {

        #region Protected Methods

        protected static ConfiguredTaskAwaitable ExecuteSQL(string sql, SqlConnection connection, SqlTransaction transaction) {
            return Task.Factory.StartNew(() => {
                using (SqlCommand command = new SqlCommand(sql, connection, transaction)) {
                    command.ExecuteNonQuery();
                }
            }).ConfigureAwait(false);
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
