using System.Windows;
using System.Windows.Controls;
using Glide.App.ViewModels;
using Microsoft.Win32;

namespace Glide.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void ChangeAvatarBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Todos os Arquivos (*.*)|*.*",
            Title = "Selecionar Foto de Perfil do Glide"
        };

        if (dialog.ShowDialog() == true && DataContext is SettingsViewModel vm)
        {
            vm.UpdateAvatar(dialog.FileName);
        }
    }

    private void ChangeBannerBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Todos os Arquivos (*.*)|*.*",
            Title = "Selecionar Banner de Perfil do Glide"
        };

        if (dialog.ShowDialog() == true && DataContext is SettingsViewModel vm)
        {
            vm.UpdateBanner(dialog.FileName);
        }
    }
}
