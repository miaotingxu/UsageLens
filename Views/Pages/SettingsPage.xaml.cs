using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using UsageLens.Models;
using UsageLens.ViewModels;

namespace UsageLens.Views.Pages;

public partial class SettingsPage : System.Windows.Controls.UserControl
{
    public SettingsPage() => InitializeComponent();

    private void PageOnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel settings)
        {
            settings.PropertyChanged += SettingsOnPropertyChanged;
            ApplyStyleSelection(settings.SelectedStyle);
        }
    }

    private void PageOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel settings)
        {
            settings.PropertyChanged -= SettingsOnPropertyChanged;
        }
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SelectedStyle) && sender is SettingsViewModel settings)
        {
            ApplyStyleSelection(settings.SelectedStyle);
        }
    }

    private void ApplyStyleSelection(FloatingStyleKind style)
    {
        InstrumentStyleButton.IsChecked = style == FloatingStyleKind.Instrument;
        GlassStyleButton.IsChecked = style == FloatingStyleKind.Glass;
        TimelineStyleButton.IsChecked = style == FloatingStyleKind.Timeline;
        TerminalStyleButton.IsChecked = style == FloatingStyleKind.Terminal;
    }

    private void StyleButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel settings &&
            sender is FrameworkElement button &&
            Enum.TryParse<FloatingStyleKind>(button.Tag?.ToString(), out var style))
        {
            settings.SelectedStyle = style;
        }
    }
}
