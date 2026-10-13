param(
    [string]$RimWorldDir = 'G:\Steam\steamapps\common\RimWorld',
    [string]$HarmonyPath = 'G:\Steam\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll'
)
$ErrorActionPreference = 'Stop'
& dotnet build (Join-Path $PSScriptRoot 'Source\ReadableFonts17.csproj') -c Release "-p:RimWorldDir=$RimWorldDir" "-p:HarmonyPath=$HarmonyPath" --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Font mod build failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Source\bin\Release\net9.0\ReadableFonts17.dll') -Destination (Join-Path $PSScriptRoot '1.7\Assemblies\ReadableFonts17.dll') -Force
Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '1.7\Assemblies\ReadableFonts17.dll') -Algorithm SHA256
