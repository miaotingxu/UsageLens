using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UsageLens.Models;
using UsageLens.ViewModels;

namespace UsageLens.Views;

public partial class ControlCenterWindow : Window
{
    private bool _allowClose;

    public ControlCenterWindow(ControlCenterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        UpdatePage(viewModel.SelectedPage);
    }

    public bool HideOnClose { get; set; } = true;

    public void AllowClose() => _allowClose = true;

    private void WindowOnLoaded(object sender, RoutedEventArgs e)
    {
        if (Owner is null && Application.Current.MainWindow is MainWindow mainWindow)
        {
            Owner = mainWindow;
        }
    }

    private void WindowOnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        if (HideOnClose)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void WindowOnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CloseButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (HideOnClose)
        {
            Hide();
        }
        else
        {
            Close();
        }
    }

    private void WindowOnClosed(object? sender, EventArgs e)
    {
        if (DataContext is ControlCenterViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlCenterViewModel.SelectedPage) && DataContext is ControlCenterViewModel viewModel)
        {
            UpdatePage(viewModel.SelectedPage);
        }
    }

    private void UpdatePage(ControlCenterPage page)
    {
        var viewModel = (ControlCenterViewModel)DataContext;
        UpdateNavigationSelection(page);
        PageContent.Content = page switch
        {
            ControlCenterPage.Overview => new Pages.OverviewPage { DataContext = viewModel },
            ControlCenterPage.Quota => new Pages.QuotaPage { DataContext = viewModel },
            ControlCenterPage.TokenUsage => new Pages.TokenUsagePage { DataContext = viewModel },
            ControlCenterPage.SessionSource => new Pages.SessionSourcePage { DataContext = viewModel },
            _ => new Pages.SettingsPage { DataContext = viewModel.Settings }
        };
    }

    private void UpdateNavigationSelection(ControlCenterPage page)
    {
        var buttons = new[] { OverviewNavButton, QuotaNavButton, TokenNavButton, SessionNavButton, SettingsNavButton };
        foreach (var button in buttons)
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
        }

        var selected = page switch
        {
            ControlCenterPage.Overview => OverviewNavButton,
            ControlCenterPage.Quota => QuotaNavButton,
            ControlCenterPage.TokenUsage => TokenNavButton,
            ControlCenterPage.SessionSource => SessionNavButton,
            _ => SettingsNavButton
        };
        selected.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3C, 0x4B));
        selected.BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x79, 0xE5, 0xB7));
        selected.BorderThickness = new Thickness(1);
    }
}
