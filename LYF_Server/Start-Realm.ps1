# 原启动入口继续转发到区服部署目录。默认启动一区。
param(
    # 可选目标区服的 XML 路径。
    [string]$RealmFile = (Join-Path $PSScriptRoot '区服部署/配置/1区.xml'),
    # 已编译的服务端配置。
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    # 首区开服时可连同公共服务一起启动。
    [switch]$IncludeShared,
    # 仅操作所选区的独立交易服，玩家所在的 GS 和网关继续运行。
    [switch]$TradeOnly
)
& (Join-Path $PSScriptRoot '区服部署/启动区服.ps1') -RealmFile $RealmFile -Configuration $Configuration -IncludeShared:$IncludeShared -TradeOnly:$TradeOnly
