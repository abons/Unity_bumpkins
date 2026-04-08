# Move kid idle sprites into Unity Resources
# Run from the repo root: .\MoveKidSprites.ps1

$src    = "Sprites\Units"
$sdst   = "Bumpkins\Assets\Resources\Sprites\Units"
$hddst  = "Bumpkins\Assets\Resources\Sprites-HD\Units"

Move-Item "$src\boystill.png"                       "$sdst\boystill.png"
Move-Item "$src\boystill_waifu2x_2x_2n_png.png"    "$hddst\boystill.png"
Move-Item "$src\girlstil.png"                       "$sdst\girlstil.png"
Move-Item "$src\girlstil_waifu2x_2x_2n_png.png"    "$hddst\girlstil.png"

Write-Host "Done. Open Unity to let it generate .meta files."
