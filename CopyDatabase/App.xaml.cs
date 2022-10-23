using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace CopyDatabase; 
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();
    }
}
