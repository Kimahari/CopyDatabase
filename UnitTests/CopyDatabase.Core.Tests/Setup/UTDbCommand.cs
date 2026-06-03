using System.Data.Common;
using System.Data;

namespace CopyDatabase.Core.Tests.Setup
{
    public class UTDbCommand : DbCommand
    {
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }

        private DbParameterCollection @params = Mock.Of<DbParameterCollection>();

        protected override DbParameterCollection DbParameterCollection => @params;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {

        }

        public override int ExecuteNonQuery()
        {
            return 0;
        }

        public override object? ExecuteScalar()
        {
            return null;
        }

        public override void Prepare()
        {

        }

        protected override DbParameter CreateDbParameter()
        {
            return Mock.Of<DbParameter>();
        }

        internal Mock<DbDataReader> reader = CreateReader();

        private static Mock<DbDataReader> CreateReader()
        {
            var readerMock = new Mock<DbDataReader>();
            readerMock.Setup(oo => oo.ReadAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            return readerMock;
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return reader.Object;
        }
    }
}