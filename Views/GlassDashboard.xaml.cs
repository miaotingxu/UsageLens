using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace CodexQuotaFloat.Views;

public partial class GlassDashboard : UserControl, ICollapsibleDashboard
{
    private readonly Effect? _cardShadow;

    public GlassDashboard()
    {
        InitializeComponent();
        _cardShadow = DashboardCard.Effect;
    }

    public void SetCardShadowVisible(bool isVisible) =>
        DashboardCard.Effect = isVisible ? _cardShadow : null;
}
