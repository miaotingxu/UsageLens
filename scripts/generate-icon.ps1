$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetsDirectory = Join-Path $repositoryRoot 'Assets'
$iconPath = Join-Path $assetsDirectory 'UsageLens.ico'
New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Drawing.Common -ErrorAction SilentlyContinue

$pngs = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in @(16, 24, 32, 48, 256)) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.Clear([System.Drawing.Color]::Transparent)

            $scale = $size / 256.0
            $margin = [float](22 * $scale)
            $radius = [float](52 * $scale)
            $background = [System.Drawing.RectangleF]::new($margin, $margin, $size - 2 * $margin, $size - 2 * $margin)
            $backgroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 46, 141, 106))
            $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
            try {
                $diameter = $radius * 2
                $path.AddArc($background.X, $background.Y, $diameter, $diameter, 180, 90)
                $path.AddArc($background.Right - $diameter, $background.Y, $diameter, $diameter, 270, 90)
                $path.AddArc($background.Right - $diameter, $background.Bottom - $diameter, $diameter, $diameter, 0, 90)
                $path.AddArc($background.X, $background.Bottom - $diameter, $diameter, $diameter, 90, 90)
                $path.CloseFigure()
                $graphics.FillPath($backgroundBrush, $path)
            }
            finally {
                $path.Dispose()
                $backgroundBrush.Dispose()
            }

            $lensSize = [float](94 * $scale)
            $lensX = [float](62 * $scale)
            $lensY = [float](62 * $scale)
            $lensPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [float](20 * $scale))
            try { $graphics.DrawEllipse($lensPen, $lensX, $lensY, $lensSize, $lensSize) } finally { $lensPen.Dispose() }

            $handlePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [float](20 * $scale))
            $handlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $handlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            try { $graphics.DrawLine($handlePen, [float](145 * $scale), [float](145 * $scale), [float](194 * $scale), [float](194 * $scale)) } finally { $handlePen.Dispose() }
        }
        finally { $graphics.Dispose() }

        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngs.Add($stream.ToArray())
        }
        finally { $stream.Dispose() }
    }
    finally { $bitmap.Dispose() }
}

$directorySize = 6 + (16 * $pngs.Count)
$offset = $directorySize
$output = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$pngs.Count)
    for ($index = 0; $index -lt $pngs.Count; $index++) {
        $size = @(16, 24, 32, 48, 256)[$index]
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$pngs[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $pngs[$index].Length
    }
    foreach ($png in $pngs) { $writer.Write($png) }
    [System.IO.File]::WriteAllBytes($iconPath, $output.ToArray())
}
finally {
    $writer.Dispose()
    $output.Dispose()
}

Write-Host "已生成 $iconPath"
