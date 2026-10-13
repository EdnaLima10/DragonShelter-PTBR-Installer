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

$payload = Join-Path $projectRoot 'payload/localization-string-tables-english(en)_assets_all_PTBR_PATCH3.bundle'

if (-not (Test-Path -LiteralPath $payload -PathType Leaf)) {
    throw 'Payload privado do Patch 3 ausente. Consulte README.md; ele não é distribuído neste repositório.'
}

if ((Get-Item -LiteralPath $payload).Length -ne 138206 -or
    (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne '8f2f33ebf439752ab3c41d80951a5ef6c422a9264e1c0c53025575104afd2e22') {
    throw 'Payload diferente da versão Patch 3 aprovada. Compilação/testes interrompidos.'
}

foreach ($directory in @(
    $env:DOTNET_CLI_HOME,
    $env:NUGET_PACKAGES,
    $env:NUGET_HTTP_CACHE_PATH,
    $env:NUGET_PLUGINS_CACHE_PATH,
    $env:TEMP,
    $env:LOCALAPPDATA,
    $env:APPDATA
)) {
    if ($directory) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

dotnet publish (Join-Path $projectRoot 'codigo/DragonShelterPTBR.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o (Join-Path $projectRoot 'distribuicao')

if ($LASTEXITCODE -ne 0) {
    throw 'Compilação falhou.'
}

$licenseOutput = Join-Path $projectRoot 'distribuicao/licencas'

New-Item -ItemType Directory -Path $licenseOutput -Force | Out-Null

Copy-Item `
    -Path (Join-Path $projectRoot 'licencas/*.txt') `
    -Destination $licenseOutput `
    -Force
