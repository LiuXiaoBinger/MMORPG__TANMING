# 在 _temp 编译并以独立端口运行队伍服，保留本机已有游戏服务。
$ErrorActionPreference = 'Stop'
$serverRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectRoot = Split-Path -Parent $serverRoot
$output = Join-Path $projectRoot ("_temp/team-authority-" + $PID)
$null = New-Item -ItemType Directory -Force -Path $output
$msbuild = (Get-Command MSBuild.exe -ErrorAction SilentlyContinue).Source
if (-not $msbuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) { throw '未定位到 Visual Studio MSBuild' }
# 每个项目用独立中间目录，应用及其依赖汇集在同一个运行目录。
$props = Join-Path $output 'isolation.props'
$body = '<Project><PropertyGroup><BaseIntermediateOutputPath>' + $output + '\obj\$(MSBuildProjectName)\</BaseIntermediateOutputPath><IntermediateOutputPath>$(BaseIntermediateOutputPath)$(Configuration)\</IntermediateOutputPath></PropertyGroup></Project>'
Set-Content -LiteralPath $props -Encoding UTF8 -Value $body
$buildDir = Join-Path $output 'bin'
& $msbuild (Join-Path $serverRoot 'LYF_Server.sln') -m:1 -nologo -v:quiet /p:Configuration=Debug "/p:OutDir=$buildDir\" "/p:CustomBeforeMicrosoftCommonProps=$props" /p:RunPostBuildEvent=Never
if ($LASTEXITCODE -ne 0) { throw '服务端编译失败' }
# 直接引用刚构建的 GS 与协议库，测试不会进入任何正式服务程序集。
$csc = Join-Path (Split-Path -Parent $msbuild) 'Roslyn/csc.exe'
$testExe = Join-Path $buildDir 'TeamAuthoritySmoke.exe'
$compiler = @('/nologo', '/target:exe', "/out:$testExe", '/r:System.Numerics.dll', '/r:System.Configuration.dll')
$compiler += "/r:$(Join-Path $buildDir 'GameServer.exe')"
$facade = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
$compiler += "/r:$facade"
$libs = @('Network.dll','MMoRpgCommon.dll','Google.Protobuf.dll','System.Memory.dll','System.Buffers.dll','System.Runtime.CompilerServices.Unsafe.dll')
for ($i = 0; $i -lt $libs.Count; $i++) { $compiler += "/r:$(Join-Path $buildDir $libs[$i])" }
$combatCompiler = @($compiler | Where-Object { $_ -notlike '/out:*' })
$combatExe = Join-Path $buildDir 'OrcWave18AiSmoke.exe'
$combatCompiler += "/out:$combatExe"
$combatCompiler += Join-Path $PSScriptRoot 'OrcWave18AiSmoke.cs'
$combatCompiler += Join-Path $PSScriptRoot 'MasterTeamProbe.cs'
& $csc @combatCompiler
if ($LASTEXITCODE -ne 0) { throw '副本战斗测试编译失败' }
$tests = @('TeamAuthoritySmoke.cs','MasterTeamProbe.cs','LoopbackLink.cs','DecodedFrame.cs')
for ($i = 0; $i -lt $tests.Count; $i++) { $compiler += Join-Path $PSScriptRoot $tests[$i] }
& $csc @compiler
if ($LASTEXITCODE -ne 0) { throw '组队测试编译失败' }
# 测试与正式 GS 使用相同的 Protobuf 版本重定向。
Copy-Item -LiteralPath (Join-Path $buildDir 'GameServer.exe.config') -Destination ($testExe + '.config')
Copy-Item -LiteralPath ($testExe + '.config') -Destination ($combatExe + '.config')
# 选用空闲回环端口，测试只负责本次启动的子进程。
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$stdout = Join-Path $output 'master.stdout.log'
$stderr = Join-Path $output 'master.stderr.log'
$process = Start-Process -FilePath (Join-Path $buildDir 'MasterServer.exe') -ArgumentList "$port" -WorkingDirectory $buildDir -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
try {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while (-not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
        if ((Get-Item -LiteralPath $stdout).Length -gt 0) { break }
        Start-Sleep -Milliseconds 100
    }
    if ($process.HasExited) { throw "队伍服未启动，证据 $stderr" }
    Push-Location $buildDir
    try {
        & $testExe "$port" > (Join-Path $output 'smoke.log') 2>&1
        $code = $LASTEXITCODE
    } finally { Pop-Location }
    Get-Content -LiteralPath (Join-Path $output 'smoke.log') | Select-String '\[FAIL\]|\[EXCEPTION\]|checks=|SUMMARY|总计|通过.*失败'
    if ($code -ne 0) { throw "组队测试失败，详见 $output\smoke.log" }
    # 由同一独立 MS 另建一个分区，用现有兽人回归验证 GS 战斗、隔离和实例回收。
    Push-Location $buildDir
    try {
        $samples = Join-Path $output 'combat-samples'
        $null = New-Item -ItemType Directory -Force -Path $samples
        & $combatExe $samples "$port" > (Join-Path $output 'combat.log') 2>&1
        $combatCode = $LASTEXITCODE
    } finally { Pop-Location }
    if ($combatCode -ne 0) { throw "副本战斗测试失败，详见 $output\combat.log" }
    Write-Output "组队与副本战斗测试通过，证据 $output"
} finally {
    if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
}
