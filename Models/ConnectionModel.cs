using DataBaseCompare.Tools;
using Prism.Commands;
using System;
using System.Data.SqlClient;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {

    public class ConnectionModel : ModelBase {

        #region Fields

        internal string serverInstance;
        private bool changes;
        private string connectionError;
        private SecureString password;
        private bool showConfiguration;
        private string userName;

        #endregion Fields

        #region Constructors

        public ConnectionModel() {
            TestConnection = new DelegateCommand(OnTestConnectionAsync, CanTestConnection);
            EditConfigurationCommand = new DelegateCommand(() => {
                ShowConfiguration = !ShowConfiguration;
            });
        }

        #endregion Constructors

        #region Properties

        public string ConnectionError { get => connectionError; set => SetProperty(ref connectionError, value); }

        public DelegateCommand EditConfigurationCommand { get; }

        public SecureString Password { get => password; set => SetProperty(ref password, value); }

        public string ServerInstance {
            get => serverInstance; set {
                changes = true; SetProperty(ref serverInstance, value); ConnectionError = ""; TestConnection.RaiseCanExecuteChanged();
            }
        }

        public bool ShowConfiguration { get => showConfiguration; set => SetProperty(ref showConfiguration, value); }

        public DelegateCommand TestConnection { get; }

        public string UserName { get => userName; set => SetProperty(ref userName, value); }

        #endregion Properties

        #region Methods

        internal async Task TestDBConnectionAsync() {
            SetIsBusy(true, "Teting Connection ...");
            ConnectionError = "";

            await Task.Factory.StartNew(() => {
                using (var connection = new SqlConnection(this.BuildConnection())) {
                    try {
                        connection.Open();
                    } catch (Exception ex) {
                        ConnectionError = ex.Message;
                    }
                }
            }).ConfigureAwait(false);

            SetIsBusy(false);
        }

        protected override string OnValidateProperty(string propertyName) {
            if (changes && String.IsNullOrEmpty(ServerInstance)) return "Server Instance Required";
            return base.OnValidateProperty(propertyName);
        }

        private bool CanTestConnection() => !IsBusy && !String.IsNullOrEmpty(ServerInstance);

        private async void OnTestConnectionAsync() => await TestDBConnectionAsync();

        private void SetIsBusy(bool value, string message = null) {
            if (!value) Message = message ?? String.Empty;
            IsBusy = value;
            TestConnection.RaiseCanExecuteChanged();
        }

        public string BuildConnection(string databaseName = "") {
            var intergrated = String.IsNullOrEmpty(UserName) ? "SSPI" : "False";

            var builder = new StringBuilder($"Data Source={serverInstance};Integrated Security={intergrated};");

            if (!String.IsNullOrEmpty(UserName))
                builder.Append($"User ID={UserName};Password={Password.SecureStringToString()};");

            if (!String.IsNullOrEmpty(databaseName))
                builder.Append($"Initial Catalog={databaseName};");

            return builder.ToString();
        }

        #endregion Methods
    }
}
