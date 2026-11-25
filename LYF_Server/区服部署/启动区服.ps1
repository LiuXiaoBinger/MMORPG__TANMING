# 启动一组同机部署的区服进程。首次启动时加 -IncludeShared，同时启动共享登录服与中心服。
param(
    # 逻辑区服、网关、调度和物理节点共用的部署文件。
    [string]$RealmFile = (Join-Path $PSScriptRoot '配置/1区.xml'),
    # 选择已编译的服务端输出目录。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    # 开发机首次启动此组区服时同时启动公共服务。
    [switch]$IncludeShared,
    # 仅操作所选区的独立交易服，玩家所在的 GS 和网关继续运行。
    [switch]$TradeOnly
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
for ($i = 0; $i -lt $names.Count; $i++) {
    # 每个服务从自己的输出目录加载依赖。
    $name = $names[$i]
    $programs[$name] = (Resolve-Path -LiteralPath (Join-Path $serverRoot "$name/bin/$Configuration/$name.exe")).Path
}
# 按调度、区级社交、各 GS、网关的顺序启动。
function Start-RealmProcess([string]$Name, [string[]]$ServiceArgs) {
    # 本进程拥有独立目录与启动参数，输出 PID 便于查找日志和进程。
    $exe = $programs[$Name]
    $process = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -ArgumentList $ServiceArgs -WindowStyle Hidden -PassThru
    Write-Output ("{0} PID={1}" -f $Name, $process.Id)
}
# Start-Process 会把参数拼成一个命令行，保留配置路径两侧的引号以支持含空格路径。
$quotedConfig = '"' + $configPath + '"'
if ($TradeOnly) {
    Start-RealmProcess 'TradeServer' @($quotedConfig)
    return
}
if ($IncludeShared) {
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
