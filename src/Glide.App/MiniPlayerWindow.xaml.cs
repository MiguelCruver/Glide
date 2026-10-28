using System.Windows;
using System.Windows.Input;
using Glide.App.ViewModels;

namespace Glide.App;

public partial class MiniPlayerWindow : Window
{
    private readonly MainWindow _mainWindow;

    public MiniPlayerWindow(MainWindow mainWindow, MainViewModel viewModel)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        DataContext = viewModel;

        // Posiciona no canto superior direito do monitor principal
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 30;
        Top = workArea.Top + 30;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}

