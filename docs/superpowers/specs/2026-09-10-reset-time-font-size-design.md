# 额度重置时间字号设计

## 目标

提升四套悬浮窗 UI 中额度重置倒计时和日期时间的可读性，同时保持窗口尺寸、布局密度和数据显示不变。

## 决定

- `InstrumentDashboard`、`GlassDashboard`、`TimelineDashboard`、`TerminalDashboard` 中的 `FiveHourResetValue` 和 `WeeklyResetValue` 统一使用 `FontSize="9"`。
- Terminal 样式中的 `RESET` 标签继续使用 `FontSize="7"`，让提示标签与实际时间保持视觉层级。
- 不修改额度标题、百分比、Token 数据、底部更新时间、行高、控件间距或窗口尺寸。
- 保留当前 `TextTrimming` 和 `TextWrapping` 策略；如果实际长时间文本出现截断，再单独调整对应列宽或间距。

## 验收标准

- 四套 UI 的两组额度重置时间均为 9 号字体。
- 长倒计时、日期和时间不与百分比、进度条或相邻内容重叠。
- 四套 UI、左右切换、水平拖拽、自动折叠和数据刷新保持正常。
- Release 单文件覆盖现有 `release-floating-auto` 目录并能正常启动。
