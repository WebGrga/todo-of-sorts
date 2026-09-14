$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutput = Join-Path $taskRoot 'src/ToDoOfSorts.App/Resources/Raw'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
function Write-Impact([string]$name, [bool]$win) {
    $rate = 44100
    $seconds = if ($win) { 0.65 } else { 0.28 }
    $samples = [int]($rate * $seconds)
    $rng = [Random]::new(72)
    $stream = [IO.File]::Create((Join-Path $taskOutput $name))
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $writer.Write([int](36 + $samples * 2))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt ')); $writer.Write([int]16)
        $writer.Write([int16]1); $writer.Write([int16]1); $writer.Write([int]$rate); $writer.Write([int]($rate * 2))
        $writer.Write([int16]2); $writer.Write([int16]16); $writer.Write([Text.Encoding]::ASCII.GetBytes('data')); $writer.Write([int]($samples * 2))
        for ($i = 0; $i -lt $samples; $i++) {
            $t = $i / $rate
            $attack = [Math]::Min(1, $t / 0.002)
            $sample = $attack * (0.55 * [Math]::Sin(2 * [Math]::PI * (100 * $t - 80 * $t * $t)) * [Math]::Exp(-26 * $t) + 0.22 * ($rng.NextDouble() * 2 - 1) * [Math]::Exp(-65 * $t))
            if ($win) {
                $freqs = @(330, 440, 660)
                for ($j = 0; $j -lt 3; $j++) {
                    $dt = $t - (0.12 + 0.07 * $j)
                    if ($dt -ge 0) { $sample += 0.1 * [Math]::Min(1, $dt / 0.008) * [Math]::Sin(2 * [Math]::PI * $freqs[$j] * $dt) * [Math]::Exp(-10 * $dt) }
                }
            }
            $writer.Write([int16]([Math]::Clamp($sample, -1, 1) * 28000))
        }
    } finally { $writer.Dispose(); $stream.Dispose() }
}
Write-Impact 'stamp.wav' $false
Write-Impact 'day-win.wav' $true
