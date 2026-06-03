using System.Windows.Controls;

namespace CopyDatabase.Controls; 
/// <summary>
/// Interaction logic for DatabaseServerCredentials.xaml
/// </summary>
public partial class EditDatabaseServerCredentials : UserControl {
    public EditDatabaseServerCredentials() {
        InitializeComponent();
    }

    internal void FocusControls() {
        dsInput.Focus();
    }
}
