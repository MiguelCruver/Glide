using System.Threading;
using Glide.App;
using Glide.App.ViewModels;
using Xunit;

namespace Glide.UnitTests;

public class WindowInitializationTests
{
    [Fact]
    public void Windows_ShouldInstantiateWithoutExceptions_OnStaThread()
    {
        Exception? threadException = null;

        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new Glide.App.App();
                    app.InitializeComponent();
                }

                var mainWindow = new MainWindow();
                Assert.NotNull(mainWindow);
                Assert.NotNull(mainWindow.DataContext);

                var vm = new MainViewModel();
                var miniWindow = new MiniPlayerWindow(mainWindow, vm);

                Assert.NotNull(miniWindow);
                Assert.NotNull(miniWindow.DataContext);

                // Testa todas as Views individualmente para garantir integridade do XAML
                var homeView = new Glide.App.Views.HomeView { DataContext = new HomeViewModel(vm) };
                Assert.NotNull(homeView);

                var settingsView = new Glide.App.Views.SettingsView { DataContext = new SettingsViewModel(vm) };
                Assert.NotNull(settingsView);

                var notifView = new Glide.App.Views.NotificationsView { DataContext = new NotificationsViewModel(vm) };
                Assert.NotNull(notifView);

                var connectView = new Glide.App.Views.ConnectView { DataContext = new ConnectViewModel(vm) };
                Assert.NotNull(connectView);

                var floatWindow = new VideoFloatingWindow("https://www.youtube.com");
                Assert.NotNull(floatWindow);

                floatWindow.Close();
                miniWindow.Close();
                mainWindow.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(5));

        Assert.Null(threadException);
    }
}
