using Prism.Mvvm;
using System.ComponentModel;

namespace DataBaseCompare.Models {

    public abstract class ModelBase : BindableBase, IDataErrorInfo {

        #region Fields

        private string error;
        private bool isBusy;

        private string message;

        #endregion Fields

        #region Properties

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

        #endregion Properties

        #region Indexers

        public string this[string columnName] => OnValidateProperty(columnName);

        #endregion Indexers

        #region Methods

        protected virtual string OnValidateProperty(string propertyName) => string.Empty;

        #endregion Methods
    }
}
