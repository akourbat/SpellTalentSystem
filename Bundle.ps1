(Get-ChildItem -Path src -Recurse -Filter *.cs |
  Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
  ForEach-Object { "=== $($_.Name) ===`r`n" + (Get-Content $_.FullName -Raw) }) -join "`r`n" |
  Set-Content "$env:USERPROFILE\Desktop\code_bundle.txt" -Encoding UTF8
