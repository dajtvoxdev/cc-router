Add-Type -AssemblyName System.Drawing

$src = "D:\SwitchAPIClaude\logo.png"
$dst = "D:\SwitchAPIClaude\Resources\app.ico"

$img = [System.Drawing.Image]::FromFile($src)
Write-Host "Source: $($img.Width) x $($img.Height)"

# Crop to a centered square focused on the character (upper portion)
# The image has character at top + "CCRouter" text at bottom — use top square
$side = [Math]::Min($img.Width, $img.Height)
$cropX = [int](($img.Width - $side) / 2)
$cropY = 0  # bias to top to capture the character
# But if image taller than wide, top crop is fine. If wider, we still want the character.
# Actually inspect: image likely ~1500x1000, character occupies top ~800px square centered.
# Use min dimension and bias to top.
if ($img.Height -gt $img.Width) {
    $side = $img.Width
    $cropX = 0
    $cropY = [int](($img.Height - $side) * 0.1)  # slight top bias
} else {
    $side = [int]($img.Height * 0.8)  # take 80% of height as character region
    $cropX = [int](($img.Width - $side) / 2)
    $cropY = [int](($img.Height - $side) * 0.1)
}

$srcRect = [System.Drawing.Rectangle]::new($cropX, $cropY, $side, $side)
Write-Host "Crop: $cropX, $cropY, $side x $side"

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($sz in $sizes) {
    $bmp = [System.Drawing.Bitmap]::new($sz, $sz)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($img, [System.Drawing.Rectangle]::new(0, 0, $sz, $sz), $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()

    $ms = [System.IO.MemoryStream]::new()
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , @{ Size = $sz; Bytes = $ms.ToArray() }
    $bmp.Dispose()
    $ms.Dispose()
}
$img.Dispose()

# Build .ico file format manually
$out = [System.IO.File]::Open($dst, 'Create')
$bw = [System.IO.BinaryWriter]::new($out)

# ICONDIR
$bw.Write([UInt16]0)            # reserved
$bw.Write([UInt16]1)            # type = 1 icon
$bw.Write([UInt16]$pngs.Count)  # count

# ICONDIRENTRY entries
$dataOffset = 6 + (16 * $pngs.Count)
foreach ($p in $pngs) {
    $sz = $p.Size
    $bw.Write([Byte]($(if ($sz -ge 256) {0} else {$sz})))   # width
    $bw.Write([Byte]($(if ($sz -ge 256) {0} else {$sz})))   # height
    $bw.Write([Byte]0)              # color count
    $bw.Write([Byte]0)              # reserved
    $bw.Write([UInt16]1)            # planes
    $bw.Write([UInt16]32)           # bpp
    $bw.Write([UInt32]$p.Bytes.Length)  # size
    $bw.Write([UInt32]$dataOffset)  # offset
    $dataOffset += $p.Bytes.Length
}

# PNG data blocks
foreach ($p in $pngs) {
    $bw.Write($p.Bytes)
}

$bw.Flush()
$bw.Close()
$out.Close()

$icoSize = (Get-Item $dst).Length
Write-Host "Wrote $dst ($icoSize bytes)"
