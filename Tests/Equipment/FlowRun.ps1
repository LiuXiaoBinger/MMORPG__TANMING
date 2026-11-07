# 测试根目录与独立构建产物目录，避免和主 Agent 或其他子 Agent 的输出互相覆盖。
$flowRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$flowOutput = Join-Path $flowRoot '_temp/equipment-gene-20261107/equipment-flow/build'
New-Item -ItemType Directory -Force -Path $flowOutput | Out-Null
# 本机 .NET Framework 4.8 项目使用 Visual Studio 安装的 MSBuild。
$flowVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$flowMsbuild = & $flowVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $flowMsbuild (Join-Path $flowRoot 'LYF_Server/GameServer/GameServer.csproj') /t:Build /p:Configuration=Debug "/p:OutDir=$flowOutput/bin/" "/p:BaseIntermediateOutputPath=$flowOutput/obj/" /v:minimal /nologo *> (Join-Path $flowOutput 'build-game.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $flowOutput 'build-game.log') -Tail 20; exit $LASTEXITCODE }
# 测试引用真实编译出的服务端、公共类型、协议和 protobuf 依赖。
$flowCompiler = Join-Path (Split-Path $flowMsbuild) 'Roslyn/csc.exe'
# protobuf 的 Span 接口需要 .NET Framework 4.8 自带的 netstandard 兼容引用。
$flowStandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
& $flowCompiler /nologo /target:exe "/out:$flowOutput/bin/FlowTests.exe" "/r:Server=$flowOutput/bin/GameServer.exe" "/r:$flowOutput/bin/Network.dll" "/r:$flowOutput/bin/MMoRpgCommon.dll" "/r:$flowOutput/bin/Google.Protobuf.dll" "/r:$flowOutput/bin/System.Memory.dll" "/r:$flowOutput/bin/System.Runtime.CompilerServices.Unsafe.dll" "/r:$flowStandard" (Join-Path $PSScriptRoot 'FlowTests.cs') *> (Join-Path $flowOutput 'build-tests.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $flowOutput 'build-tests.log') -Tail 20; exit $LASTEXITCODE }
# 沿用真实游戏服的依赖版本重定向，避免测试程序选择另一版 protobuf 依赖。
Copy-Item -LiteralPath (Join-Path $flowOutput 'bin/GameServer.exe.config') -Destination (Join-Path $flowOutput 'bin/FlowTests.exe.config') -Force
& (Join-Path $flowOutput 'bin/FlowTests.exe') *> (Join-Path $flowOutput 'flow-tests.log')
$flowResult = $LASTEXITCODE
Get-Content (Join-Path $flowOutput 'flow-tests.log') -Tail 12
exit $flowResult
