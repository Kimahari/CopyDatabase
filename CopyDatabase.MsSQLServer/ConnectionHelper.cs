using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDatabase.MsSQLServer {
    public static class ConnectionHelper {
        public static DbConnection GetSqlConnection(string connectionString) => new SqlConnection(connectionString);
    }
}
