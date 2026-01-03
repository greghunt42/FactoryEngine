$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir '..')
$feTools = Join-Path $repoRoot 'src/Tools/FeTools'
$configPath = Join-Path $repoRoot 'build/validate-all.json'

dotnet run --project $feTools -- `
    validate-all `
    --config $configPath `
    --stop-on-failure `
    @args
