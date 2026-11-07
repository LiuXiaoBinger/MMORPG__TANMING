# 网关定向构建与回环 TCP 测试使用单独输出目录，不重复执行已通过的物品事务测试。
$flowGateRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$flowGateOutput = Join-Path $flowGateRoot '_temp/equipment-gene-20261107/equipment-flow/gate-build'
New-Item -ItemType Directory -Force -Path $flowGateOutput | Out-Null
# 用本机 Visual Studio 提供的 .NET Framework 构建工具。
$flowGateVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$flowGateMsbuild = & $flowGateVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $flowGateMsbuild (Join-Path $flowGateRoot 'LYF_Server/GateServer/GateServer.csproj') /t:Build /p:Configuration=Debug "/p:OutDir=$flowGateOutput/bin/" "/p:BaseIntermediateOutputPath=$flowGateOutput/obj/" /v:minimal /nologo *> (Join-Path $flowGateOutput 'build-gate.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $flowGateOutput 'build-gate.log') -Tail 15; exit $LASTEXITCODE }
# 测试只引用真实网关、网络封包与 protobuf 依赖。
$flowGateCompiler = Join-Path (Split-Path $flowGateMsbuild) 'Roslyn/csc.exe'
$flowGateStandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
& $flowGateCompiler /nologo /target:exe "/out:$flowGateOutput/bin/FlowGateTests.exe" "/r:Gate=$flowGateOutput/bin/GateServer.exe" "/r:$flowGateOutput/bin/Network.dll" "/r:$flowGateOutput/bin/MMoRpgCommon.dll" "/r:$flowGateOutput/bin/Google.Protobuf.dll" "/r:$flowGateOutput/bin/System.Memory.dll" "/r:$flowGateStandard" (Join-Path $PSScriptRoot 'FlowGateTests.cs') *> (Join-Path $flowGateOutput 'build-tests.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $flowGateOutput 'build-tests.log') -Tail 15; exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $flowGateOutput 'bin/GateServer.exe.config') -Destination (Join-Path $flowGateOutput 'bin/FlowGateTests.exe.config') -Force
& (Join-Path $flowGateOutput 'bin/FlowGateTests.exe') *> (Join-Path $flowGateOutput 'gate-tests.log')
$flowGateResult = $LASTEXITCODE
Get-Content (Join-Path $flowGateOutput 'gate-tests.log') -Tail 8
exit $flowGateResult
