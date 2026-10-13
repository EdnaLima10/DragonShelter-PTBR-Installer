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

foreach ($directory in @(
    $env:DOTNET_CLI_HOME,
    $env:NUGET_PACKAGES,
    $env:NUGET_HTTP_CACHE_PATH,
    $env:NUGET_PLUGINS_CACHE_PATH,
    $env:TEMP
)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$testProject = Join-Path $projectRoot 'testes/Testes.csproj'

if (-not (Test-Path -LiteralPath $testProject -PathType Leaf)) {
    throw 'Projeto de testes não encontrado: testes/Testes.csproj'
}

$executionRoot = Join-Path $projectRoot 'testes/execucoes'
New-Item -ItemType Directory -Path $executionRoot -Force | Out-Null

$executionId = Get-Date -Format 'yyyyMMdd-HHmmss'
$executionDirectory = Join-Path $executionRoot $executionId
New-Item -ItemType Directory -Path $executionDirectory -Force | Out-Null

dotnet run `
    --project $testProject `
    -c Release `
    -- `
    $executionDirectory

if ($LASTEXITCODE -ne 0) {
    throw 'Os testes falharam.'
}

Write-Host ''
Write-Host '26 testes do instalador Patch 3 concluídos com sucesso.'
Write-Host "Resultados: $executionDirectory"
