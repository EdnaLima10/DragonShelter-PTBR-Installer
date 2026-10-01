$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'compilacao/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot 'compilacao/nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $projectRoot 'compilacao/nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $projectRoot 'compilacao/nuget-plugins'
$env:TEMP = Join-Path $projectRoot 'compilacao/temp'
$env:TMP = $env:TEMP
$env:LOCALAPPDATA = Join-Path $projectRoot 'compilacao/localappdata'
$env:APPDATA = Join-Path $projectRoot 'compilacao/appdata'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_NOLOGO = '1'
$payload = Join-Path $projectRoot 'payload/localization-string-tables-english(en)_assets_all_PTBR_PATCH1.bundle'
if (-not (Test-Path -LiteralPath $payload -PathType Leaf)) {
    throw 'Payload privado do Patch 1 ausente. Consulte README.md; ele não é distribuído neste repositório.'
}
if ((Get-Item -LiteralPath $payload).Length -ne 136062 -or
    (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne '141b5e97717f6708e4ab27f0b95423cae59a57394defe4805faecb8c2f2fdd3d') {
    throw 'Payload diferente da versão Patch 1 aprovada. Compilação/testes interrompidos.'
}
foreach ($directory in @($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:NUGET_HTTP_CACHE_PATH,
    $env:NUGET_PLUGINS_CACHE_PATH, $env:TEMP, $env:LOCALAPPDATA, $env:APPDATA)) {
    if ($directory) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
}

foreach ($fixture in @('catalog.original.json', 'bundle.original.bundle',
    'pre-patch1/catalog.original.json', 'pre-patch1/bundle.original.bundle')) {
    if (-not (Test-Path -LiteralPath (Join-Path $projectRoot "testes/fontes/$fixture") -PathType Leaf)) {
        throw "Fonte privada de teste ausente: $fixture. Consulte testes/README.md."
    }
}

dotnet run --project (Join-Path $projectRoot 'testes/Testes.csproj') -c Release -- $projectRoot
if ($LASTEXITCODE -ne 0) { throw 'Testes internos falharam.' }
