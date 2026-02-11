$root = 'C:\Users\anagha.kini\OneDrive - AVEVA Solutions Limited\dev\aveva-invoice\src>'
$outFile = 'C:\Users\anagha.kini\OneDrive - AVEVA Solutions Limited\dev\invoice-poc'
 
# Code file extensions to include
$extensions = @(
  '.cs', '.py', '.js', '.ts', '.tsx',
  '.json', '.yml', '.yaml',
  '.html', '.css', '.scss',
  '.md', '.sql',
  '.ps1', '.sh'
)
 
# Remove old output
if (Test-Path $outFile) {
  Remove-Item $outFile -Force
}
 
Get-ChildItem -Path $root -Recurse -File |
Where-Object { $extensions -contains $_.Extension.ToLower() } |
Sort-Object FullName |
ForEach-Object {
 
@"
============================================================
FILE: $($_.FullName)
============================================================
"@ | Out-File -Append -FilePath $outFile -Encoding utf8
 
Get-Content $_.FullName -Raw |
Out-File -Append -FilePath $outFile -Encoding utf8
 
"`n" | Out-File -Append -FilePath $outFile -Encoding utf8
}
 
Write-Host "Done."
Write-Host "Output file: $outFile"

has context menu