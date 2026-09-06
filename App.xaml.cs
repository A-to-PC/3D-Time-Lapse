using System.Windows;

namespace TimeLapse3D;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.ApplyResources();
        base.OnStartup(e);
    }
}
