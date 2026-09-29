$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'compilacao/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot 'compilacao/nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $projectRoot 'compilacao/nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $projectRoot 'compilacao/nuget-plugins'
$env:TEMP = Join-Path $projectRoot 'compilacao/temp'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:LOCALAPPDATA = Join-Path $projectRoot 'compilacao/localappdata'
$env:APPDATA = Join-Path $projectRoot 'compilacao/appdata'
dotnet publish (Join-Path $projectRoot 'codigo/DragonShelterPTBR.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $projectRoot 'distribuicao')
if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou.' }
