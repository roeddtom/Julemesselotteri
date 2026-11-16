using System.Globalization;
using System.IO;
using System.Windows;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public partial class App : Application
{
    private Mutex? _instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
        _instance = new Mutex(true, @"Local\Julemesselotteri-" + Environment.UserName, out var created);
        if (!created)
        {
            MessageBox.Show("Julemesselotteri kjører allerede.", "Julemesselotteri");
            Shutdown();
            return;
        }

        try
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Julemesselotteri");
            var logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Julemesselotteri");
            var store = new EventFileStore(data, logs);
            MainWindow = new MainWindow(new LotteryEngine(store), store);
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Programmet kunne ikke startes. Ingen trekning er utført.\n\n{ex.Message}\n\n" +
                "Kontroller tilstandsfilen i %LOCALAPPDATA%\\Julemesselotteri. Ikke slett historikken for å fortsette.",
                "Oppstart stoppet", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instance?.Dispose();
        base.OnExit(e);
    }
}
