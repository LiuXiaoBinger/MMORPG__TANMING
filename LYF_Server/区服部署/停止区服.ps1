# 按本机工程输出目录与启动时的区服配置路径停止指定区的进程。
param(
    # 必须与启动该区时使用的文件对应。
    [string]$RealmFile = (Join-Path $PSScriptRoot '配置/1区.xml'),
    # 仅停止这个构建配置下的可执行文件。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    # 最后一个区下线后，按启动公共服务时的配置路径一并停止。
    [switch]$IncludeShared,
    # 仅操作所选区的独立交易服，玩家所在的 GS 和网关继续运行。
    [switch]$TradeOnly
)
$ErrorActionPreference = 'Stop'
# 启动脚本为配置绝对路径保留双引号，因此区号或目录名相似不会被模糊匹配。
$configPath = (Resolve-Path -LiteralPath $RealmFile).Path
$configArgument = '"' + $configPath + '"'
# 每区只涉及自身网关、游戏节点、调度与社交进程。
$names = @('GateServer', 'GameServer', 'MasterServer', 'ControlServer', 'TradeServer')
if ($TradeOnly) { $names = @('TradeServer') }
elseif ($IncludeShared) { $names += @('LoginServer', 'CenterServer') }
# 仅匹配项目输出目录下的可执行文件和本次选定的配置。
$serverRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot '区服程序.ps1')
for ($i = 0; $i -lt $names.Count; $i++) {
    # 同名 GS 可能属于不同区，继续按路径和完整参数筛选。
    $name = $names[$i]
    $expectedExe = [IO.Path]::GetFullPath((Join-Path $serverRoot "$name/bin/$Configuration/$name.exe"))
    $processes = @(Get-CimInstance Win32_Process -Filter ("Name = '" + $name + ".exe'"))
    for ($j = 0; $j -lt $processes.Count; $j++) {
        # 通过本次部署程序路径和配置参数找到实际的进程号。
        $process = $processes[$j]
        if ((Get-RealmDeploymentPath $serverRoot $process.ExecutablePath) -ne $expectedExe -or !$process.CommandLine) { continue }
        if ($process.CommandLine.IndexOf($configArgument,[StringComparison]::OrdinalIgnoreCase) -lt 0) {
            # 直接运行交易服时，Windows 对无空格的最后一个 XML 参数不加引号。
            if ($name -ne 'TradeServer') { continue }
            if (!$process.CommandLine.TrimEnd().EndsWith(' ' + $configPath,[StringComparison]::OrdinalIgnoreCase)) { continue }
        }
        Stop-Process -Id $process.ProcessId -Force
        Write-Output ("{0} PID={1}" -f $name, $process.ProcessId)
    }
}
if ($IncludeShared -and !$TradeOnly) {
    & (Join-Path $serverRoot 'Redis/ProjectRedis.ps1') -RealmFile $configPath -Stop
}
