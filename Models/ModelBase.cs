using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace DataBaseCompare.Models {

    public abstract class ModelBase : BindableBase, IDataErrorInfo {

        #region Private Fields

        private string error;
        private bool isBusy;

        private string message;

        #endregion Private Fields

        #region Protected Methods

        protected virtual string OnValidateProperty(string propertyName) {
            return string.Empty;
        }

        protected void StartOperation() {
            IsBusy = true;
            Message = Error = String.Empty;
        }

        #endregion Protected Methods

        #region Public Properties

        public string Error {
            get => error;
            set => SetProperty(ref error, value);
        }

        public bool IsBusy {
            get => isBusy;
            set => SetProperty(ref isBusy, value);
        }

        public string Message {
            get => message;
            set => SetProperty(ref message, value);
        }

        #endregion Public Properties

        #region Public Indexers

        public string this[string columnName] => OnValidateProperty(columnName);

        #endregion Public Indexers
    }
}
