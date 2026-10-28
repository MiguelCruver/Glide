using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Glide.App.ViewModels;
using Microsoft.Win32;

namespace Glide.App.Views;

public partial class ConnectView : UserControl
{
    public ConnectView()
    {
        InitializeComponent();
    }

    private void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.webp;*.gif)|*.png;*.jpg;*.jpeg;*.webp;*.gif|Todos os Arquivos (*.*)|*.*",
            Title = "Selecionar Imagem para o Glide Connect"
        };

        if (dialog.ShowDialog() == true && DataContext is ConnectViewModel vm)
        {
            vm.AddAttachment(dialog.FileName);
        }
    }

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ConnectViewModel vm)
        {
            if (vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
            }
        }
    }
}
