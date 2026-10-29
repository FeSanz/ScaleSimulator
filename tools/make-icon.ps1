# Generates ScaleSim.App\app.ico (rounded blue tile with a white platform-scale glyph).
# Re-run only if you want to change the artwork; the .ico is committed.
Add-Type -AssemblyName System.Drawing

function New-Frame([int]$size) {
    $bmp = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([Drawing.Color]::Transparent)
    $u = $size / 64.0

    $r = 14 * $u
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($size - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($size - $r * 2, $size - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $size - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $grad = New-Object Drawing.Drawing2D.LinearGradientBrush ([Drawing.PointF]::new(0, 0)), ([Drawing.PointF]::new($size, $size)), ([Drawing.Color]::FromArgb(255, 76, 141, 255)), ([Drawing.Color]::FromArgb(255, 31, 78, 190))
    $g.FillPath($grad, $path)

    $white = [Drawing.Brushes]::White
    # Base plate
    $g.FillPath($white, (& {
        $p = New-Object Drawing.Drawing2D.GraphicsPath
        $p.AddPolygon(@([Drawing.PointF]::new(13 * $u, 49 * $u), [Drawing.PointF]::new(51 * $u, 49 * $u), [Drawing.PointF]::new(47 * $u, 56 * $u), [Drawing.PointF]::new(17 * $u, 56 * $u)))
        $p
    }))
    # Pan
    $g.FillRectangle($white, 10 * $u, 42 * $u, 44 * $u, 5 * $u)
    # Dial housing + needle
    $g.FillEllipse($white, 20 * $u, 9 * $u, 24 * $u, 24 * $u)
    $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 36, 86, 205)), (3 * $u)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $g.DrawLine($pen, 32 * $u, 22 * $u, 38 * $u, 15 * $u)
    $g.FillEllipse([Drawing.Brushes]::DarkBlue, 29.5 * $u, 19.5 * $u, 5 * $u, 5 * $u)
    # Stem
    $g.FillRectangle($white, 29 * $u, 33 * $u, 6 * $u, 9 * $u)
    $g.Dispose()
    $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) {
    $bmp = New-Frame $s
    $ms = New-Object IO.MemoryStream
    $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
    , $ms.ToArray()
}

$out = Join-Path (Split-Path -Parent $PSScriptRoot) "ScaleSim.App\app.ico"
$fs = [IO.File]::Create($out)
$bw = New-Object IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Close()

# A 256px preview next to the icon for a quick visual check.
(New-Frame 256).Save((Join-Path (Split-Path -Parent $PSScriptRoot) "docs\icon-preview.png"), [Drawing.Imaging.ImageFormat]::Png)
Write-Host "Wrote $out"
