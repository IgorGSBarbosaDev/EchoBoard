$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$assetsDirectory = Join-Path $PSScriptRoot '..\src\EchoBoard.App\Assets'
$sourcePath = Join-Path $assetsDirectory 'EchoBoard.png'

function New-FittedBitmap {
    param(
        [System.Drawing.Image] $SourceImage,
        [int] $TargetWidth,
        [int] $TargetHeight
    )

    $output = [System.Drawing.Bitmap]::new(
        $TargetWidth,
        $TargetHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $output.SetResolution(96, 96)

    $graphics = [System.Drawing.Graphics]::FromImage($output)
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $scale = [Math]::Min(
            $TargetWidth / [double] $SourceImage.Width,
            $TargetHeight / [double] $SourceImage.Height)
        $drawWidth = [int] [Math]::Round($SourceImage.Width * $scale)
        $drawHeight = [int] [Math]::Round($SourceImage.Height * $scale)
        $left = [int] [Math]::Floor(($TargetWidth - $drawWidth) / 2)
        $top = [int] [Math]::Floor(($TargetHeight - $drawHeight) / 2)

        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.DrawImage(
            $SourceImage,
            [System.Drawing.Rectangle]::new($left, $top, $drawWidth, $drawHeight),
            0,
            0,
            $SourceImage.Width,
            $SourceImage.Height,
            [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $graphics.Dispose()
    }

    return $output
}

function Save-Png {
    param(
        [System.Drawing.Image] $Image,
        [string] $Path
    )

    try {
        $Image.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $Image.Dispose()
    }
}

function Get-PngBytes {
    param(
        [System.Drawing.Image] $SourceImage,
        [int] $Size
    )

    $bitmap = New-FittedBitmap $SourceImage $Size $Size
    $stream = [System.IO.MemoryStream]::new()
    try {
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $bitmap.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "The source icon was not found: $sourcePath"
}

$source = [System.Drawing.Bitmap]::new($sourcePath)
try {
    $squareAssets = [ordered] @{
        'Square44x44Logo.png' = 44
        'Square71x71Logo.png' = 71
        'Square150x150Logo.png' = 150
        'Square310x310Logo.png' = 310
        'StoreLogo.png' = 50
        'EchoBoard-16.png' = 16
        'EchoBoard-24.png' = 24
        'EchoBoard-32.png' = 32
        'EchoBoard-48.png' = 48
        'EchoBoard-64.png' = 64
        'EchoBoard-128.png' = 128
        'EchoBoard-256.png' = 256
        'EchoBoard-512.png' = 512
    }

    foreach ($asset in $squareAssets.GetEnumerator()) {
        Save-Png (New-FittedBitmap $source $asset.Value $asset.Value) (Join-Path $assetsDirectory $asset.Key)
    }

    Save-Png (New-FittedBitmap $source 310 150) (Join-Path $assetsDirectory 'Wide310x150Logo.png')
    Save-Png (New-FittedBitmap $source 620 300) (Join-Path $assetsDirectory 'SplashScreen.png')

    $iconFrames = foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
        [pscustomobject] @{
            Size = $size
            Bytes = Get-PngBytes $source $size
        }
    }

    $iconPath = Join-Path $assetsDirectory 'EchoBoard.ico'
    $headerSize = 6
    $directorySize = 16 * @($iconFrames).Count
    $offset = $headerSize + $directorySize
    $file = [System.IO.File]::Open(
        $iconPath,
        [System.IO.FileMode]::Create,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    $writer = [System.IO.BinaryWriter]::new($file)
    try {
        $writer.Write([uint16] 0)
        $writer.Write([uint16] 1)
        $writer.Write([uint16] @($iconFrames).Count)

        foreach ($frame in $iconFrames) {
            $dimension = if ($frame.Size -ge 256) { [byte] 0 } else { [byte] $frame.Size }
            $writer.Write($dimension)
            $writer.Write($dimension)
            $writer.Write([byte] 0)
            $writer.Write([byte] 0)
            $writer.Write([uint16] 1)
            $writer.Write([uint16] 32)
            $writer.Write([uint32] $frame.Bytes.Length)
            $writer.Write([uint32] $offset)
            $offset += $frame.Bytes.Length
        }

        foreach ($frame in $iconFrames) {
            $writer.Write([byte[]] $frame.Bytes)
        }
    }
    finally {
        $writer.Dispose()
        $file.Dispose()
    }
}
finally {
    $source.Dispose()
}

Get-ChildItem -LiteralPath $assetsDirectory -File |
    Sort-Object Name |
    Select-Object Name, Length
