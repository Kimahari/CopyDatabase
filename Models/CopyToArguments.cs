﻿using System;
using System.Data.SqlClient;
using System.Threading;

namespace DataBaseCompare.Models {
    public class CopyToArguments {
        public ConnectionModel SourceModel { get; internal set; }
        public ConnectionModel DestinationConnection { get; internal set; }
        public bool CopyData { get; internal set; }

        public CancellationToken Token { get; internal set; }

        public String DatabaseName { get; internal set; }

        public SqlConnection Connection { get; internal set; }
        public SqlTransaction Transaction { get; internal set; }
    }
}