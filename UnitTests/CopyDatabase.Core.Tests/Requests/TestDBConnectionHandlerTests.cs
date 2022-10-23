using Xunit;
using CopyDatabase.Core.Requests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CopyDatabase.Core.Tests.Setup;
using System.Security;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace CopyDatabase.Core.Requests.Tests {
    public class TestDBConnectionHandlerTests {
        [Fact()]
        public async Task ReturnTrueWhenDatabaseCanConnect() {
            var dbConnectionMock = new UTDbConnection();

            var sut = new TestDBConnectionHandler(
                new DatabaseServerCredentialValidator(),
                Mock.Of<IDbConnectionFactory>(oo => oo.GetDbConnection(It.IsAny<SecureString>(), It.IsAny<DatabaseProvider>()) == dbConnectionMock),
                Mock.Of<IConnectionStringBuilderFactory>(oo => oo.GetConnectionStringBuilder(It.IsAny<DatabaseProvider>()) == Mock.Of<IConnectionStringBuilder>())
            );

            var result = await sut.Handle(new TestDBConnection() {
                Credentials = new DatabaseServerTestCredentials() {
                    UseWindowsAuth = true,
                    DataSource = "."
                },
            }, CancellationToken.None);

            Assert.True(result);
        }


        [Fact()]
        public async Task ReturnTrueWhenDatabaseCannotConnect() {
            var dbConnectionMock = new UTDbConnection() { CanConnect = false };

            var sut = new TestDBConnectionHandler(
                new DatabaseServerCredentialValidator(),
                Mock.Of<IDbConnectionFactory>(oo => oo.GetDbConnection(It.IsAny<SecureString>(), It.IsAny<DatabaseProvider>()) == dbConnectionMock),
                Mock.Of<IConnectionStringBuilderFactory>(oo => oo.GetConnectionStringBuilder(It.IsAny<DatabaseProvider>()) == Mock.Of<IConnectionStringBuilder>())
            );

            var result = await sut.Handle(new TestDBConnection() {
                Credentials = new DatabaseServerTestCredentials() {
                    UseWindowsAuth = true,
                    DataSource = "."
                },
            }, CancellationToken.None);

            Assert.False(result);
        }
    }
}