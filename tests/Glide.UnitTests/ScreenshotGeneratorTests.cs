using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Glide.App.ViewModels;
using Glide.App.Views;
using Glide.Core.Enums;
using Xunit;

namespace Glide.UnitTests;

public class ScreenshotGeneratorTests
{
    [Fact]
    public void GenerateAppScreenshots_ShouldExportCleanPngs()
    {
        Exception? threadEx = null;

        var thread = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new Glide.App.App();
                    app.InitializeComponent();
                }

                string solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\.."));
                string screenshotsDir = Path.Combine(solutionDir, "assets", "screenshots");
                Directory.CreateDirectory(screenshotsDir);

                var vm = new MainViewModel();

                var viewsToExport = new (string fileName, FrameworkElement view)[]
                {
                    ("glide-home.png", new HomeView { DataContext = new HomeViewModel(vm) }),
                    ("glide-connect.png", new ConnectView { DataContext = new ConnectViewModel(vm) }),
                    ("glide-notifications.png", new NotificationsView { DataContext = new NotificationsViewModel(vm) }),
                    ("glide-settings.png", new SettingsView { DataContext = new SettingsViewModel(vm) })
                };

                foreach (var (fileName, contentControl) in viewsToExport)
                {
                    // Monta o frame completo da aplicação com TopBar e Sidebar para fidelidade máxima
                    var rootGrid = new Grid
                    {
                        Width = 1180,
                        Height = 740,
                        Background = (Brush)Application.Current.Resources["GlideBgBrush"]
                    };

                    rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
                    rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    // Top Bar
                    var topBar = new Grid
                    {
                        Background = (Brush)Application.Current.Resources["GlideSurfaceBrush"]
                    };
                    topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var idPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(14, 0, 0, 0)
                    };

                    var logo = new Image
                    {
                        Width = 28,
                        Height = 28,
                        Margin = new Thickness(0, 0, 10, 0),
                        Source = new BitmapImage(new Uri("pack://application:,,,/Glide.App;component/Assets/glide-logo.png"))
                    };
                    idPanel.Children.Add(logo);

                    idPanel.Children.Add(new TextBlock
                    {
                        Text = "GLIDE",
                        Foreground = (Brush)Application.Current.Resources["GlideTextPrimaryBrush"],
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    });

                    idPanel.Children.Add(new TextBlock
                    {
                        Text = "• Gaming Companion",
                        Foreground = (Brush)Application.Current.Resources["GlideTextMutedBrush"],
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    Grid.SetColumn(idPanel, 0);
                    topBar.Children.Add(idPanel);
                    Grid.SetRow(topBar, 0);
                    rootGrid.Children.Add(topBar);

                    // Body: Sidebar + Content
                    var bodyGrid = new Grid();
                    bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
                    bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Sidebar
                    var sidebarBorder = new Border
                    {
                        Background = (Brush)Application.Current.Resources["GlideSidebarGradient"],
                        BorderBrush = (Brush)Application.Current.Resources["GlideBorderBrush"],
                        BorderThickness = new Thickness(0, 0, 1, 0),
                        Padding = new Thickness(12, 16, 12, 16)
                    };

                    var sidebarMenu = new StackPanel();
                    sidebarMenu.Children.Add(new TextBlock
                    {
                        Text = "MENU PRINCIPAL",
                        Foreground = (Brush)Application.Current.Resources["GlideTextMutedBrush"],
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(14, 0, 0, 12)
                    });

                    string[] menuItems = { "Início", "Áudio", "Vídeo", "Glide Connect", "Notificações", "Configurações" };
                    foreach (var item in menuItems)
                    {
                        bool isCurrent = (fileName.Contains("home") && item == "Início") ||
                                         (fileName.Contains("connect") && item == "Glide Connect") ||
                                         (fileName.Contains("notif") && item == "Notificações") ||
                                         (fileName.Contains("settings") && item == "Configurações");

                        var itemBorder = new Border
                        {
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(14, 10, 14, 10),
                            Margin = new Thickness(0, 0, 0, 4),
                            Background = isCurrent
                                ? new SolidColorBrush(Color.FromArgb(40, 0, 240, 255))
                                : Brushes.Transparent
                        };

                        itemBorder.Child = new TextBlock
                        {
                            Text = item,
                            Foreground = isCurrent
                                ? (Brush)Application.Current.Resources["GlideAccentMintBrush"]
                                : (Brush)Application.Current.Resources["GlideTextSecondaryBrush"],
                            FontSize = 13,
                            FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Normal
                        };

                        sidebarMenu.Children.Add(itemBorder);
                    }

                    sidebarBorder.Child = sidebarMenu;
                    Grid.SetColumn(sidebarBorder, 0);
                    bodyGrid.Children.Add(sidebarBorder);

                    // Content View Host
                    var contentBorder = new Border
                    {
                        Background = (Brush)Application.Current.Resources["GlideBgBrush"],
                        Child = contentControl
                    };
                    Grid.SetColumn(contentBorder, 1);
                    bodyGrid.Children.Add(contentBorder);

                    Grid.SetRow(bodyGrid, 1);
                    rootGrid.Children.Add(bodyGrid);

                    // Renderização WPF de alta fidelidade
                    rootGrid.Measure(new Size(1180, 740));
                    rootGrid.Arrange(new Rect(0, 0, 1180, 740));
                    rootGrid.UpdateLayout();

                    var rtb = new RenderTargetBitmap(1180, 740, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(rootGrid);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    string outPath = Path.Combine(screenshotsDir, fileName);
                    using var fs = File.Create(outPath);
                    encoder.Save(fs);
                }
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new Exception($"Falha ao gerar screenshots: {threadEx.Message}", threadEx);
        }
    }
}
