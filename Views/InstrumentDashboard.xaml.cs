using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace UsageLens.Views;

public partial class InstrumentDashboard : UserControl, ICollapsibleDashboard
{
    private readonly Effect? _cardShadow;

    public InstrumentDashboard()
    {
        InitializeComponent();
        _cardShadow = DashboardCard.Effect;
    }

    public void SetCardShadowVisible(bool isVisible) =>
        DashboardCard.Effect = isVisible ? _cardShadow : null;
}
