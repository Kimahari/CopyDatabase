using System.Data.Common;
using System.Data;

namespace CopyDatabase.Core.Tests.Setup {
    public class UTDbConnection : DbConnection {
        public override string ConnectionString { get; set; } = string.Empty;

        private string database = "";
        private string dataSource = "";
        private string databaseVersion = "";
        private ConnectionState state = ConnectionState.Closed;

        public override string Database => database;

        public override string DataSource => dataSource;

        public override string ServerVersion => databaseVersion;

        public override ConnectionState State => state;

        public bool CanConnect { get; set; } = true;

        public override void ChangeDatabase(string databaseName) {
            database = databaseName;
        }

        public override void Close() {
            state = ConnectionState.Closed;
        }

        public override void Open() {
            if (!CanConnect) return;
            state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) {
            return Mock.Of<DbTransaction>();
        }

        internal UTDbCommand dbCommand = new UTDbCommand();

        protected override DbCommand CreateDbCommand() {
            return dbCommand;
        }
    }
}