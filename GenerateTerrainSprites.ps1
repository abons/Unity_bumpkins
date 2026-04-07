Add-Type -AssemblyName System.Drawing

$workspace      = "c:\Users\axelb\beasts\Unity_bumpkins"
$screenshotPath = "$workspace\screenshots\1_leftbottom.png"
$grassPath      = "$workspace\Bumpkins\Assets\Resources\Sprites-HD\Terrain\Grass.png"
$hdDir          = "$workspace\Bumpkins\Assets\Resources\Sprites-HD\Terrain"
$sdDir          = "$workspace\Bumpkins\Assets\Resources\Sprites\Terrain"

if (-not (Test-Path $screenshotPath)) { Write-Error "Screenshot not found: $screenshotPath"; exit 1 }
if (-not (Test-Path $grassPath))      { Write-Error "Grass.png not found: $grassPath"; exit 1 }

$shot  = [System.Drawing.Bitmap]::new($screenshotPath)
$grass = [System.Drawing.Bitmap]::new($grassPath)
$W = $grass.Width
$H = $grass.Height
Write-Host "Grass.png dimensions: ${W}x${H}"

# Crop a rectangle from $sourceBmp, scale it to W×H (bicubic), then apply
# Grass.png's alpha channel as a diamond mask (alpha<10 → transparent, else full alpha).
function New-CropScaleMask {
    param(
        [System.Drawing.Bitmap]$sourceBmp,
        [System.Drawing.Bitmap]$maskBmp,
        [int]$cropX, [int]$cropY, [int]$cropW, [int]$cropH,
        [string]$label
    )
    $tw = $maskBmp.Width
    $th = $maskBmp.Height

    # Step 1: scale crop region → tw×th using high-quality bicubic
    $dst = [System.Drawing.Bitmap]::new($tw, $th, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $srcRect = [System.Drawing.Rectangle]::new($cropX, $cropY, $cropW, $cropH)
    $dstRectF = [System.Drawing.RectangleF]::new(0, 0, $tw, $th)
    $g.DrawImage($sourceBmp, $dstRectF, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()

    # Step 2: apply diamond mask — transparent where Grass alpha < 10, else force alpha=255
    for ($y = 0; $y -lt $th; $y++) {
        for ($x = 0; $x -lt $tw; $x++) {
            if ($maskBmp.GetPixel($x, $y).A -lt 10) {
                $dst.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            } else {
                $c = $dst.GetPixel($x, $y)
                $dst.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
            }
        }
    }

    Write-Host "  $label : ${tw}x${th} generated"
    return $dst
}

# Water: x=0..120, y=780..900  (pure deep blue-purple sea, no contamination)
Write-Host "Generating Water.png..."
$water = New-CropScaleMask -sourceBmp $shot -maskBmp $grass `
    -cropX 0 -cropY 780 -cropW 120 -cropH 120 -label "Water"
$water.Save("$hdDir\Water.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$hdDir\Water.png" "$sdDir\Water.png" -Force
Write-Host "  Saved HD + SD Water.png"

$water.Dispose()

# Sand: x=620..880, y=675..750  (pure golden-brown beach, green already filtered by crop selection)
Write-Host "Generating Sand.png..."
$sand = New-CropScaleMask -sourceBmp $shot -maskBmp $grass `
    -cropX 620 -cropY 675 -cropW 260 -cropH 75 -label "Sand"
$sand.Save("$hdDir\Sand.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$hdDir\Sand.png" "$sdDir\Sand.png" -Force
$sand.Dispose()
Write-Host "  Saved HD + SD Sand.png"

$grass.Dispose()
$shot.Dispose()
Write-Host "Done - Water.png and Sand.png written to HD and SD Terrain folders."
