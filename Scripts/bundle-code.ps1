$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
$outFile   = Join-Path $repoRoot "code_bundle.txt"

$extensions = @(".cs", ".csproj", ".razor", ".cshtml")

$files = Get-ChildItem -LiteralPath $repoRoot -Recurse -File |
    Where-Object {
        $_.Extension -in $extensions -and
        $_.FullName -notmatch '\\(obj|bin|\.git|\.vs|\.vscode|node_modules)\\'
    } |
    Sort-Object FullName

$bundle = foreach ($file in $files) {
    $relPath = $file.FullName.Substring($repoRoot.Length + 1)
    "=== $relPath ===`r`n" + (Get-Content -LiteralPath $file.FullName -Raw)
}

$bundle -join "`r`n" | Set-Content -LiteralPath $outFile -Encoding UTF8

$sizeKB = [math]::Round((Get-Item -LiteralPath $outFile).Length / 1KB, 1)
Write-Host "Bundled $($files.Count) files to $outFile ($sizeKB KB)" -ForegroundColor Green
