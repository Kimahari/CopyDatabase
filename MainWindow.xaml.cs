using MahApps.Metro.Controls;
using System;

namespace DataBaseCompare {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow {

        #region Public Constructors

        public MainWindow() {
            InitializeComponent();
        }

        #endregion Public Constructors

        private void MetroWindow_Closed(object sender, System.EventArgs e) {
            if (this.DataContext is IDisposable disposable) disposable.Dispose();
        }
    }
}
