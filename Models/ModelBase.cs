using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseCompare.Models {
    public abstract class ModelBase : BindableBase, IDataErrorInfo {

        private bool isBusy;

        public bool IsBusy {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        private string message;

        public string Message {
            get { return message; }
            set { SetProperty(ref message, value); }
        }


        private string error;

        public string Error {
            get { return error; }
            set { SetProperty(ref error, value); }
        }


        public string this[string columnName] => OnValidateProperty(columnName);

        public ModelBase() {

        }

        protected virtual string OnValidateProperty(string propertyName) {
            return string.Empty;
        }
    }
}
