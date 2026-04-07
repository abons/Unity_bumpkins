Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::new('c:\Users\axelb\beasts\Unity_bumpkins\screenshots\1_leftbottom.png')
Write-Host "Image size: $($bmp.Width) x $($bmp.Height)"

$regions = @(
    @{ x1=300; y1=650; x2=500; y2=720; lbl="CandSand1" },
    @{ x1=400; y1=600; x2=600; y2=680; lbl="CandSand2" },
    @{ x1=500; y1=550; x2=700; y2=630; lbl="CandSand3" },
    @{ x1=200; y1=620; x2=400; y2=700; lbl="CandSand4" },
    @{ x1=600; y1=650; x2=900; y2=750; lbl="CandSand5" },
    @{ x1=350; y1=670; x2=600; y2=740; lbl="CandSand6" }
)

foreach ($reg in $regions) {
    $r=0; $g=0; $b=0; $n=0
    for ($y = $reg.y1; $y -lt $reg.y2; $y++) {
        for ($x = $reg.x1; $x -lt $reg.x2; $x++) {
            $c = $bmp.GetPixel($x, $y)
            if ($c.A -gt 128) { $r += $c.R; $g += $c.G; $b += $c.B; $n++ }
        }
    }
    $avgR = [int]($r / $n); $avgG = [int]($g / $n); $avgB = [int]($b / $n)
    Write-Host "$($reg.lbl): R=$avgR G=$avgG B=$avgB ($n px)"
}
$bmp.Dispose()
