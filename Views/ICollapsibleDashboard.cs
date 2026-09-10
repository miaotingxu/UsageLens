namespace UsageLens.Views;

/// <summary>
/// 为自动折叠状态提供视觉切换，避免卡片阴影投射到顶部把手区域。
/// </summary>
public interface ICollapsibleDashboard
{
    void SetCardShadowVisible(bool isVisible);
}
