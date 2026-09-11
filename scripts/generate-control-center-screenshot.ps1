$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputDirectory = Join-Path $root 'docs\screenshots'
$outputPath = Join-Path $outputDirectory 'control-center.png'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Drawing.Common -ErrorAction SilentlyContinue

function New-RoundedPath([System.Drawing.RectangleF]$rect, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Fill-Rounded([System.Drawing.Graphics]$graphics, [System.Drawing.RectangleF]$rect, [float]$radius, [System.Drawing.Color]$color) {
    $brush = [System.Drawing.SolidBrush]::new($color)
    $path = New-RoundedPath $rect $radius
    try { $graphics.FillPath($brush, $path) } finally { $path.Dispose(); $brush.Dispose() }
}

function Draw-Text([System.Drawing.Graphics]$graphics, [string]$text, [float]$x, [float]$y, [float]$size, [System.Drawing.Color]$color, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    $font = [System.Drawing.Font]::new('Segoe UI', $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = [System.Drawing.SolidBrush]::new($color)
    try { $graphics.DrawString($text, $font, $brush, $x, $y) } finally { $brush.Dispose(); $font.Dispose() }
}

$bitmap = [System.Drawing.Bitmap]::new(1200, 780, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 11, 19, 31))

        # Neutral grid background.
        $gridPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(38, 74, 95, 116), 1)
        try {
            for ($x = 0; $x -lt 1200; $x += 48) { $graphics.DrawLine($gridPen, $x, 0, $x, 780) }
            for ($y = 0; $y -lt 780; $y += 48) { $graphics.DrawLine($gridPen, 0, $y, 1200, $y) }
        } finally { $gridPen.Dispose() }

        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(28, 24, 1144, 732)) 18 ([System.Drawing.Color]::FromArgb(248, 15, 28, 42))
        $sidebarBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 20, 36, 52))
        try { $graphics.FillRectangle($sidebarBrush, 28, 24, 214, 732) } finally { $sidebarBrush.Dispose() }

        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(51, 51, 34, 34)) 9 ([System.Drawing.Color]::FromArgb(255, 121, 229, 183))
        Draw-Text $graphics 'U' 61 55 18 ([System.Drawing.Color]::FromArgb(255, 8, 23, 16)) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics 'UsageLens' 98 51 18 ([System.Drawing.Color]::White) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics 'AI usage control center' 98 76 10 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))

        $nav = @('◈  总览', '◒  额度窗口', '✦  Token 用量', '☷  会话来源', '⚙  设置')
        for ($i = 0; $i -lt $nav.Count; $i++) {
            $top = 138 + ($i * 53)
             if ($i -eq 0) {
                 Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(44, $top, 182, 40)) 10 ([System.Drawing.Color]::FromArgb(255, 30, 60, 75))
                 $navColor = [System.Drawing.Color]::FromArgb(255, 121, 229, 183)
             } else {
                 $navColor = [System.Drawing.Color]::FromArgb(255, 145, 161, 183)
             }
             Draw-Text $graphics $nav[$i] 60 ($top + 11) 13 $navColor
        }
        Draw-Text $graphics '●  本机数据 · 已保护' 50 704 10 ([System.Drawing.Color]::FromArgb(255, 121, 229, 183))
        Draw-Text $graphics '不读取或保存凭据' 50 724 9 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))

        Draw-Text $graphics '总览' 278 52 27 ([System.Drawing.Color]::FromArgb(255, 242, 247, 255)) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics '额度、Token 与本地状态一目了然' 278 88 13 ([System.Drawing.Color]::FromArgb(255, 143, 161, 183))
        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(734, 45, 254, 48)) 11 ([System.Drawing.Color]::FromArgb(255, 24, 46, 43))
        Draw-Text $graphics '已检测到 Codex 登录状态' 748 53 11 ([System.Drawing.Color]::FromArgb(255, 121, 229, 183))
        Draw-Text $graphics '复用本机登录态；不会保存凭据' 748 71 9 ([System.Drawing.Color]::FromArgb(255, 143, 161, 183))
        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(1002, 45, 42, 42)) 10 ([System.Drawing.Color]::FromArgb(35, 255, 255, 255))
        Draw-Text $graphics '↻' 1013 51 23 ([System.Drawing.Color]::FromArgb(255, 175, 192, 210))
        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(1052, 45, 42, 42)) 10 ([System.Drawing.Color]::FromArgb(35, 255, 255, 255))
        Draw-Text $graphics '×' 1064 51 22 ([System.Drawing.Color]::FromArgb(255, 175, 192, 210))

        $cards = @(
            @{X=278; Label='5 小时额度'; Value='96%'; Color=[System.Drawing.Color]::FromArgb(255, 121, 229, 183); Hint='RESET  ·  2h 29m · 09/11 05:20'},
            @{X=507; Label='7 天额度'; Value='61%'; Color=[System.Drawing.Color]::FromArgb(255, 247, 222, 107); Hint='RESET  ·  6d 1h · 09/17 12:40'},
            @{X=736; Label='今日 Token'; Value='12.4M'; Color=[System.Drawing.Color]::FromArgb(255, 126, 203, 255); Hint='约 $8.42 · 本机统计'}
        )
        foreach ($card in $cards) {
            Fill-Rounded $graphics ([System.Drawing.RectangleF]::new($card.X, 124, 214, 120)) 14 ([System.Drawing.Color]::FromArgb(255, 23, 40, 56))
            Draw-Text $graphics $card.Label ($card.X + 16) 142 12 ([System.Drawing.Color]::FromArgb(255, 143, 161, 183))
            Draw-Text $graphics $card.Value ($card.X + 16) 169 28 $card.Color ([System.Drawing.FontStyle]::Bold)
            Draw-Text $graphics $card.Hint ($card.X + 16) 214 10 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))
        }

        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(278, 264, 816, 174)) 14 ([System.Drawing.Color]::FromArgb(255, 19, 35, 51))
        Draw-Text $graphics '额度窗口' 298 285 14 ([System.Drawing.Color]::FromArgb(255, 226, 235, 247)) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics '更新时间 12:42:08' 964 287 10 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))
        Draw-Text $graphics '5 小时' 298 329 12 ([System.Drawing.Color]::FromArgb(255, 186, 200, 216))
        Draw-Text $graphics '7 天额度' 298 384 12 ([System.Drawing.Color]::FromArgb(255, 186, 200, 216))
        $track = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 38, 54, 73))
        try { $graphics.FillRectangle($track, 390, 335, 594, 6); $graphics.FillRectangle($track, 390, 390, 594, 6) } finally { $track.Dispose() }
        $green = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 85, 214, 164)); $yellow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 247, 222, 107))
        try { $graphics.FillRectangle($green, 390, 335, 570, 6); $graphics.FillRectangle($yellow, 390, 390, 362, 6) } finally { $green.Dispose(); $yellow.Dispose() }
        Draw-Text $graphics '96%' 1008 326 12 ([System.Drawing.Color]::FromArgb(255, 121, 229, 183)) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics '61%' 1008 381 12 ([System.Drawing.Color]::FromArgb(255, 247, 222, 107)) ([System.Drawing.FontStyle]::Bold)

        Fill-Rounded $graphics ([System.Drawing.RectangleF]::new(278, 458, 816, 244)) 14 ([System.Drawing.Color]::FromArgb(255, 19, 35, 51))
        Draw-Text $graphics 'Token 周期' 298 479 14 ([System.Drawing.Color]::FromArgb(255, 226, 235, 247)) ([System.Drawing.FontStyle]::Bold)
        Draw-Text $graphics '输入 + 输出 · 估算价格' 930 481 10 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))
        $periods = @(@{Label='当日'; Total='12.4M'; Price='约 $8.42'}, @{Label='近 7 天'; Total='84.7M'; Price='约 $56.19'}, @{Label='近 30 天'; Total='286.2M'; Price='约 $184.80'})
        for ($i = 0; $i -lt $periods.Count; $i++) {
            $x = 298 + ($i * 264)
            if ($i -gt 0) { $line = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 45, 61, 78), 1); try { $graphics.DrawLine($line, $x - 18, 510, $x - 18, 670) } finally { $line.Dispose() } }
            Draw-Text $graphics $periods[$i].Label $x 518 12 ([System.Drawing.Color]::FromArgb(255, 143, 161, 183))
            Draw-Text $graphics $periods[$i].Total $x 548 24 ([System.Drawing.Color]::FromArgb(255, 126, 203, 255)) ([System.Drawing.FontStyle]::Bold)
            Draw-Text $graphics 'IN  9.2M' $x 594 11 ([System.Drawing.Color]::FromArgb(255, 126, 203, 255))
            Draw-Text $graphics 'OUT  3.2M' $x 616 11 ([System.Drawing.Color]::FromArgb(255, 199, 164, 255))
            Draw-Text $graphics $periods[$i].Price $x 650 12 ([System.Drawing.Color]::FromArgb(255, 121, 229, 183))
        }
        Draw-Text $graphics 'Preview uses neutral sample values; no account or session data.' 278 722 9 ([System.Drawing.Color]::FromArgb(255, 116, 135, 158))
    }
    finally { $graphics.Dispose() }
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $bitmap.Dispose() }

Write-Host "已生成 $outputPath"
