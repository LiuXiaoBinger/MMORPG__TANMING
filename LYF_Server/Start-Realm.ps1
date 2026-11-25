# 启动一组同机部署的区服进程。账号登录服与数据库中心由开发环境单独启动。
param(
    # 逻辑区服、网关、调度和物理节点共用的部署文件。
    [string]$RealmFile = (Join-Path $PSScriptRoot 'Config/RealmServers.xml'),
    # 选择已编译的服务端输出目录。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
# 绝对路径供每个进程在自己的二进制目录下读取同一份配置。
$configPath = (Resolve-Path -LiteralPath $RealmFile).Path
[xml]$topology = Get-Content -Raw -Encoding UTF8 -LiteralPath $configPath
# 首先检查所有可执行文件，可避免因缺少后续项目而只启动半组进程。
$programs = @{}
for ($i = 0; $i -lt 4; $i++) {
    # .NET Framework 依赖版本由各服务自己的输出目录解析。
    $name = @('ControlServer', 'MasterServer', 'GameServer', 'GateServer')[$i]
    $programs[$name] = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "$name/bin/$Configuration/$name.exe")).Path
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
Start-RealmProcess 'ControlServer' @($quotedConfig)
Start-RealmProcess 'MasterServer' @($quotedConfig)
# 区服目录中的各物理 GS 使用各自的端口与实例号。
$nodes = @($topology.realm.game)
for ($i = 0; $i -lt $nodes.Count; $i++) {
    Start-RealmProcess 'GameServer' @($quotedConfig, [string]$nodes[$i].id)
}
Start-RealmProcess 'GateServer' @($quotedConfig)
