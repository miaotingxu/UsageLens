# 底部状态字号统一设计

## 目标

统一四套悬浮窗 UI 底部额度更新时间和 Token 更新时间的字号，消除切换样式时的视觉跳变。

## 方案选择

- 不统一为 8：虽然最节省空间，但当前反馈说明可读性不足。
- 采用统一为 9：与重置时间字号一致，可读性和紧凑度平衡最好。
- 不统一为 10：底栏左右各占一列，较长的失败状态更容易提前截断。

## 决定

- `InstrumentDashboard`、`GlassDashboard`、`TimelineDashboard`、`TerminalDashboard` 中的 `StatusValue` 和 `TokenStatusValue` 统一使用 `FontSize="9"`。
- Terminal 样式中的 `Q`、`T` 前缀同步使用 `FontSize="9"`，避免同一行基线和大小不一致。
- 不修改底栏高度、列宽、边距、颜色、字体系列、文本内容或 `TextTrimming`。
- 不修改窗口尺寸、其他内容字号以及任何数据和交互逻辑。

## 验收标准

- 四套 UI 的八个底部状态字段均为 9 号字体。
- Terminal 的 `Q`、`T` 前缀与状态文本字号一致。
- 底部状态文本不与其他内容重叠，长状态仍按现有规则截断。
- 四套样式切换、顶部水平拖拽、自动折叠和刷新功能保持正常。
- Release 单文件覆盖现有 `release-floating-auto` 目录并正常启动。
