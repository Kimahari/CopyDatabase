using DataBaseCompare.Tools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {
    public class ConnectionModel : ModelBase {
        private bool changes = false;

        internal string serverInstance;
        public string ServerInstance { get => serverInstance; set { changes = true; SetProperty(ref serverInstance, value); ConnectionError = ""; TestConnection.RaiseCanExecuteChanged(); } }

        public ConnectionModel() {
            this.TestConnection = new DelegateCommand(onTestConnection, canTestConnection);
            this.EditConfigurationCommand = new DelegateCommand(() => {
                ShowConfiguration = !ShowConfiguration;
            });
        }

        public DelegateCommand TestConnection { get; }
        public DelegateCommand EditConfigurationCommand { get; }

        private string connectionError;

        public string ConnectionError {
            get { return connectionError; }
            set { SetProperty(ref connectionError, value); }
        }

        private bool showConfiguration;

        public bool ShowConfiguration {
            get { return showConfiguration; }
            set { SetProperty(ref showConfiguration, value); }
        }


        private string userName;

        public string UserName {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }

        private SecureString password;

        public SecureString Password {
            get { return password; }
            set { SetProperty(ref password, value); }
        }

        private bool canTestConnection() {
            return !this.IsBusy && !String.IsNullOrEmpty(ServerInstance);
        }

        private async void onTestConnection() {
            await TestDBConnection();
        }

        internal async Task TestDBConnection() {
            setIsBusy(true);
            this.Message = "Teting Connection ...";
            this.ConnectionError = "";

            await Task.Factory.StartNew(() => {
                using (var connection = new SqlConnection(this.BuildConnection())) {
                    try {
                        connection.Open();
                    } catch (Exception ex) {
                        this.ConnectionError = ex.Message;
                    }
                }
            });

            this.Message = String.Empty;
            setIsBusy(false);
        }

        private void setIsBusy(bool value) {
            this.IsBusy = value;
            this.TestConnection.RaiseCanExecuteChanged();
        }

        protected override string OnValidateProperty(string propertyName) {
            if (changes && String.IsNullOrEmpty(ServerInstance)) {
                return "Server Instance Required";
            }

            return base.OnValidateProperty(propertyName);
        }
    }
}
