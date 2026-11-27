# 随公共服务启停项目随附的 Redis，仅管理本机对应端口和可执行文件。
param(
    # 区服 XML 可以内嵌地址，也可引用所有区共用的公共服务 XML。
    [Parameter(Mandatory=$true)][string]$RealmFile,
    # 最后一个公共服务停止时传入，其他区独立停服时继续共享缓存。
    [switch]$Stop
)
$ErrorActionPreference = 'Stop'
# 多区和老版单文件部署使用同一组配置读取规则。
$realmPath = (Resolve-Path -LiteralPath $RealmFile).Path
[xml]$realmDocument = Get-Content -Raw -Encoding UTF8 -LiteralPath $realmPath
$settings = $realmDocument.realm
if ($settings.HasAttribute('shared')) {
    $sharedPath = Join-Path (Split-Path -Parent $realmPath) $settings.shared
    [xml]$sharedDocument = Get-Content -Raw -Encoding UTF8 -LiteralPath $sharedPath
    $settings = $sharedDocument.services
}
# 远程地址由其宿主机启动。本项目随附的 Windows 配置监听本地回环。
$redisHost = [string]$settings.redisHost
if (!$redisHost) { $redisHost = [string]$settings.centerHost }
if (!$redisHost) { $redisHost = [string]$realmDocument.realm.host }
if ($redisHost -notin @('127.0.0.1','localhost')) { return }
$port = 6380
if ($settings.HasAttribute('redisPort')) { $port = [int]$settings.redisPort }
# 进程标识来自该端口的实际监听者，停止时保护其他目录的 Redis。
$exe = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'redis-server.exe')).Path
$listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($listener) {
    $owner = Get-Process -Id $listener.OwningProcess
    if ($owner.Path -ne $exe) {
        if ($Stop) { return }
        throw "端口 $port 已由其他程序使用，项目 Redis 无法在该端口启动。"
    }
    if ($Stop) {
        Stop-Process -Id $owner.Id
        $owner.WaitForExit(5000) | Out-Null
    }
    return
}
if ($Stop) { return }
# Windows 版 Redis 通过文件接收配置。认证沿用验证码服务的值，命令行只带本次配置文件名。
$verifyConfig = Join-Path (Split-Path -Parent $PSScriptRoot) 'VarifyCodeServer/config.json'
$auth = (Get-Content -Raw -Encoding UTF8 -LiteralPath $verifyConfig | ConvertFrom-Json).redis.passwd
$config = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'redis.conf')
# 端口专属的运行目录放在项目临时区，旧安装的快照不会被新实例载入。
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$runtime = Join-Path $projectRoot ("_temp/redis-runtime/" + $port)
[void][IO.Directory]::CreateDirectory($runtime)
$lines = @($config, ("port " + $port))
if ($auth) { $lines += 'requirepass ' + (ConvertTo-Json ([string]$auth) -Compress) }
[IO.File]::WriteAllLines((Join-Path $runtime 'redis.conf'), [string[]]$lines, (New-Object Text.UTF8Encoding($false)))
$redisProcess = Start-Process -FilePath $exe -WorkingDirectory $runtime -ArgumentList 'redis.conf' -WindowStyle Hidden -PassThru
# 只有项目进程确实监听端口后，公共服务启动器才继续启动 CenterServer。
for ($attempt = 0; $attempt -lt 10; $attempt++) {
    if ($redisProcess.HasExited) { throw "项目 Redis 提前退出，退出码 $($redisProcess.ExitCode)。" }
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener -and $listener.OwningProcess -eq $redisProcess.Id) {
        Write-Output ("项目 Redis 端口={0} PID={1}" -f $port, $redisProcess.Id)
        return
    }
    Start-Sleep -Milliseconds 100
}
throw "项目 Redis 未能在端口 $port 开始监听。"
