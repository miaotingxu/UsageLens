using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace UsageLens.Views;

public partial class TimelineDashboard : UserControl, ICollapsibleDashboard, IBackgroundOpacityDashboard
{
    private readonly Effect? _cardShadow;
    private readonly System.Windows.Media.Brush _cardBackground;

    public TimelineDashboard()
    {
        InitializeComponent();
        _cardShadow = DashboardCard.Effect;
        _cardBackground = DashboardCard.Background;
    }

    public void SetCardShadowVisible(bool isVisible) =>
        DashboardCard.Effect = isVisible ? _cardShadow : null;

    public void SetCardBackgroundOpacity(double opacity) =>
        DashboardCard.Background = DashboardBackgroundOpacity.WithOpacity(_cardBackground, opacity);
}
