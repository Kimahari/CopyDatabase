using System;
using Microsoft.Data.SqlClient;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using DataBaseCompare.Tools;

using Prism.Commands;

namespace DataBaseCompare.Models {

    public class ConnectionModel : ModelBase {

        #region Private Fields

        private bool changes;
        private string connectionError;
        private SecureString password;
        private bool showConfiguration;
        private string userName;

        #endregion Private Fields

        #region Private Methods

        private bool CanTestConnection() {
            return !IsBusy && !string.IsNullOrEmpty(ServerInstance);
        }

        private async void OnTestConnectionAsync() {
            await TestDBConnectionAsync();
        }

        private void SetIsBusy(bool value, string message = null) {
            if (!value) {
                Message = message ?? string.Empty;
            }

            IsBusy = value;
            TestConnection.RaiseCanExecuteChanged();
        }

        #endregion Private Methods

        #region Protected Methods

        protected override string OnValidateProperty(string propertyName) {
            if (changes && string.IsNullOrEmpty(ServerInstance)) {
                return "Server Instance Required";
            }

            return base.OnValidateProperty(propertyName);
        }

        #endregion Protected Methods

        #region Internal Fields

        internal string serverInstance;

        #endregion Internal Fields

        #region Internal Methods

        internal async Task TestDBConnectionAsync() {
            SetIsBusy(true, "Teting Connection ...");
            ConnectionError = "";

            await Task.Factory.StartNew(() => {
                using (SqlConnection connection = new SqlConnection(BuildConnection())) {
                    try {
                        connection.Open();
                    } catch (Exception ex) {
                        ConnectionError = ex.Message;
                    }
                }
            }).ConfigureAwait(false);

            SetIsBusy(false);
        }

        #endregion Internal Methods

        #region Public Constructors

        public ConnectionModel() {
            TestConnection = new DelegateCommand(OnTestConnectionAsync, CanTestConnection);
            EditConfigurationCommand = new DelegateCommand(() => {
                ShowConfiguration = !ShowConfiguration;
            });
        }

        #endregion Public Constructors

        #region Public Properties

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

        #endregion Public Properties

        #region Public Methods

        public string BuildConnection(string databaseName = "") {
            string intergrated = string.IsNullOrEmpty(UserName) ? "SSPI" : "False";

            StringBuilder builder = new StringBuilder($"Data Source={serverInstance};Integrated Security={intergrated};");

            if (!string.IsNullOrEmpty(UserName)) {
                builder.Append($"User ID={UserName};Password={Password.SecureStringToString()};");
            }

            if (!string.IsNullOrEmpty(databaseName)) {
                builder.Append($"Initial Catalog={databaseName};");
            }

            return builder.ToString();
        }

        #endregion Public Methods
    }
}
