using System.Security;
using CopyDatabase.Core.Tests.Setup;
using CopyDatabase.Common;
namespace CopyDatabase.Core.Requests.Tests {

    public class GetDatabaseListCommandHandlerTests {
        private UTDbConnection dbConnectionMock;
        private GetDatabaseListCommandHandler sut;

        public GetDatabaseListCommandHandlerTests() {
            this.dbConnectionMock = new UTDbConnection();

            this.sut = new GetDatabaseListCommandHandler(
                Mock.Of<IDbConnectionFactory>(oo => oo.GetDbConnection(It.IsAny<SecureString>(), It.IsAny<DatabaseProvider>()) == dbConnectionMock),
                Mock.Of<IConnectionStringBuilderFactory>(oo => oo.GetConnectionStringBuilder(It.IsAny<DatabaseProvider>()) == Mock.Of<IConnectionStringBuilder>())
            );
        }

        [Fact()]
        public async Task ShouldGiveEmptyArrayWhenCredentialsIsNotProvided() {
            var result = await this.sut.Handle(new GetDatabaseList(), CancellationToken.None);
            Assert.Empty(result);
        }

        [Fact()]
        public async Task ShouldGiveCallDatabaseReaderToGetDatabaseListWhenCredentialsIsProvided() {
            var result = await this.sut.Handle(new GetDatabaseList() {
                ServerCredentials = new DatabaseServerTestCredentials { UseWindowsAuth = true, DataSource = "." },
            }, CancellationToken.None);
            Assert.Empty(result);
        }
    }
}