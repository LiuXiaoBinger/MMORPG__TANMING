# 复用已经构建的正式游戏服，避免为了协议样本重复构建未变输入。
param([string]$ServerBuildDirectory, [string]$FixtureDirectory)
# 安装器发现的实际编译器。
$syncVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$syncCompiler = & $syncVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
# Framework 提供的 protobuf 兼容引用。
$syncStandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
& $syncCompiler /nologo /target:exe "/out:$ServerBuildDirectory/GeneSyncTests.exe" "/r:Server=$ServerBuildDirectory/GameServer.exe" "/r:$ServerBuildDirectory/Network.dll" "/r:$ServerBuildDirectory/MMoRpgCommon.dll" "/r:$ServerBuildDirectory/Google.Protobuf.dll" "/r:$ServerBuildDirectory/System.Memory.dll" "/r:$ServerBuildDirectory/System.Runtime.CompilerServices.Unsafe.dll" "/r:$syncStandard" (Join-Path $PSScriptRoot 'GeneSyncTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath "$ServerBuildDirectory/GameServer.exe.config" -Destination "$ServerBuildDirectory/GeneSyncTests.exe.config" -Force
& "$ServerBuildDirectory/GeneSyncTests.exe" $FixtureDirectory
exit $LASTEXITCODE
