namespace UsageLens.Models;

[Flags]
public enum RefreshScope
{
    None = 0,
    Quota = 1,
    TokenUsage = 2,
    All = Quota | TokenUsage
}
