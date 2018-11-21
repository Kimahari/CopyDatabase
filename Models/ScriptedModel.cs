using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {

    public class ScriptedModel : ModelBase {

        #region Properties

        public string DatabaseName { get; set; }
        public string Name { get; set; }
        public string Schema { get; set; } = "dbo";
        public string Script { get; set; }

        #endregion Properties

        #region Methods

        internal virtual async Task CopyToAsync(CopyToArguments args, Action<long> callback = null) => await ExecuteSQL(Script, args.Connection, args.Transaction);

        protected static ConfiguredTaskAwaitable ExecuteSQL(string sql, SqlConnection connection, SqlTransaction transaction) => Task.Factory.StartNew(() => {
            using (var command = new SqlCommand(sql, connection, transaction)) {
                command.ExecuteNonQuery();
            }
        }).ConfigureAwait(false);

        #endregion Methods
    }
}
