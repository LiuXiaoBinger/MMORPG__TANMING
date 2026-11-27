# 启动一组同机部署的区服进程。首次启动时加 -IncludeShared，同时启动共享登录服与中心服。
param(
    # 逻辑区服、网关、调度和物理节点共用的部署文件。
    [string]$RealmFile = (Join-Path $PSScriptRoot '配置/1区.xml'),
    # 选择已编译的服务端输出目录。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    # 开发机首次启动此组区服时同时启动公共服务。
    [switch]$IncludeShared,
    # 仅操作所选区的独立交易服，玩家所在的 GS 和网关继续运行。
    [switch]$TradeOnly,
    # 双击开服时为每个服务显示独立控制台，以便查看实时输出。
    [switch]$ShowConsole
)
$ErrorActionPreference = 'Stop'
# 绝对路径供每个进程在自己的二进制目录下读取同一份配置。
$configPath = (Resolve-Path -LiteralPath $RealmFile).Path
[xml]$topology = Get-Content -Raw -Encoding UTF8 -LiteralPath $configPath
# 首先检查所有可执行文件，可避免因缺少后续项目而只启动半组进程。
$programs = @{}
# 各服务工程位于部署目录的上一级。
$serverRoot = Split-Path -Parent $PSScriptRoot
# 公共服务只需启动一次，其他区可独立开启。
$names = @('ControlServer', 'MasterServer', 'GameServer', 'GateServer')
if ([int]$topology.realm.tradePort -gt 0) { $names += 'TradeServer' }
if ($TradeOnly) {
    if ([int]$topology.realm.tradePort -le 0) { throw '此区未启用交易服，请先配置 tradePort。' }
    $names = @('TradeServer')
} elseif ($IncludeShared) { $names += @('CenterServer', 'LoginServer') }
# 开发目录先做增量编译，避免客户端已发送新门号、网关却仍运行上一次的程序。
$solution = Join-Path $serverRoot 'LYF_Server.sln'
if (Test-Path -LiteralPath $solution -PathType Leaf) {
    # 交易服单独启动时只构建它和工程引用，其余情况统一更新区服程序及共享配置。
    $buildTarget = $solution
    if ($TradeOnly) { $buildTarget = Join-Path $serverRoot 'TradeServer/TradeServer.csproj' }
    # 优先使用开发命令行里的 MSBuild，普通双击通过 Visual Studio 安装器定位构建工具。
    $msbuild = (Get-Command 'MSBuild.exe' -ErrorAction SilentlyContinue).Source
    if (!$msbuild) {
        # 启动脚本同样支持仅安装 Visual Studio Build Tools 的机器。
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
        }
    }
    if ($msbuild) {
        & $msbuild $buildTarget /nologo /verbosity:minimal "/p:Configuration=$Configuration"
    } else {
        # 已安装 .NET SDK 的开发环境也可以构建同一工程。
        & dotnet msbuild $buildTarget /nologo /verbosity:minimal "/p:Configuration=$Configuration"
    }
    if ($LASTEXITCODE -ne 0) { throw "区服编译失败（$LASTEXITCODE），请查看上方构建输出。" }
}
for ($i = 0; $i -lt $names.Count; $i++) {
    # 每个服务从自己的输出目录加载依赖。
    $name = $names[$i]
    $programs[$name] = (Resolve-Path -LiteralPath (Join-Path $serverRoot "$name/bin/$Configuration/$name.exe")).Path
}
# 双击入口要求显示窗口，自动部署沿用后台启动。
$windowStyle = 'Hidden'
if ($ShowConsole) { $windowStyle = 'Normal' }
# 用服务名 Name 定位程序，ServiceArgs 传入区服配置和可选的 GS 节点编号。
function Start-RealmProcess([string]$Name, [string[]]$ServiceArgs) {
    # 从已检查的服务清单中取出本进程的可执行文件路径。
    $exe = $programs[$Name]
    # 保存本次启动的进程以便输出其 PID。
    $process = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -ArgumentList $ServiceArgs -WindowStyle $windowStyle -PassThru
    Write-Output ("{0} PID={1}" -f $Name, $process.Id)
}
# Start-Process 会把参数拼成一个命令行，保留配置路径两侧的引号以支持含空格路径。
$quotedConfig = '"' + $configPath + '"'
if ($TradeOnly) {
    Start-RealmProcess 'TradeServer' @($quotedConfig)
    return
}
# 各区即使分别启动，也使用与项目 Redis、验证码服务一致的认证值。
$verifyConfig = Join-Path $serverRoot 'VarifyCodeServer/config.json'
if (Test-Path -LiteralPath $verifyConfig) {
    $env:LYF_REDIS_PASSWORD = [string](Get-Content -Raw -Encoding UTF8 -LiteralPath $verifyConfig | ConvertFrom-Json).redis.passwd
}
if ($IncludeShared) {
    & (Join-Path $serverRoot 'Redis/ProjectRedis.ps1') -RealmFile $configPath
    Start-RealmProcess 'CenterServer' @($quotedConfig)
    Start-RealmProcess 'LoginServer' @($quotedConfig)
}
Start-RealmProcess 'ControlServer' @($quotedConfig)
if ([int]$topology.realm.tradePort -gt 0) { Start-RealmProcess 'TradeServer' @($quotedConfig) }
Start-RealmProcess 'MasterServer' @($quotedConfig)
# 区服目录中的各物理 GS 使用各自的端口与实例号。
$nodes = @($topology.realm.game)
for ($i = 0; $i -lt $nodes.Count; $i++) {
    Start-RealmProcess 'GameServer' @($quotedConfig, [string]$nodes[$i].id)
}
Start-RealmProcess 'GateServer' @($quotedConfig)
