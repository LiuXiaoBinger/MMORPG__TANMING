# 原启动入口继续转发到区服部署目录。默认启动一区。
param(
    # 可选目标区服的 XML 路径。
    [string]$RealmFile = (Join-Path $PSScriptRoot '区服部署/配置/1区.xml'),
    # 已编译的服务端配置。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    # 首区开服时可连同公共服务一起启动。
    [switch]$IncludeShared,
    # 仅操作所选区的独立交易服，玩家所在的 GS 和网关继续运行。
    [switch]$TradeOnly,
    # 从双击入口打开整区启停菜单。
    [switch]$Menu
)
if ($Menu) {
    # 两个现有管理命令共用调用者指定的区服配置和构建类型。
    $startScript = Join-Path $PSScriptRoot '区服部署/启动区服.ps1'
    $stopScript = Join-Path $PSScriptRoot '区服部署/停止区服.ps1'
    $realmArgs = @{ RealmFile = $RealmFile; Configuration = $Configuration }
    # 菜单退出时保留最近一次服务操作的结果。
    $lastResult = 0
    while ($true) {
        Write-Host
        Write-Host ("区服管理  ·  " + [IO.Path]::GetFileNameWithoutExtension($RealmFile))
        Write-Host '1  启动'
        Write-Host '2  停止'
        Write-Host '输入序号并按回车：' -NoNewline
        # 关闭输入或直接回车时结束本次菜单。
        $selection = [Console]::ReadLine()
        if ([string]::IsNullOrWhiteSpace($selection)) { exit $lastResult }
        if ($selection -notin @('1', '2')) {
            Write-Host '请输入 1 或 2。'
            continue
        }
        try {
            if ($selection -eq '1') { & $startScript @realmArgs -IncludeShared }
            else { & $stopScript @realmArgs -IncludeShared }
            $lastResult = 0
        } catch {
            Write-Host ("操作失败：" + $_.Exception.Message) -ForegroundColor Red
            $lastResult = 1
        }
    }
}
& (Join-Path $PSScriptRoot '区服部署/启动区服.ps1') -RealmFile $RealmFile -Configuration $Configuration -IncludeShared:$IncludeShared -TradeOnly:$TradeOnly
