# 使用已经独立编译的客户端，测试产物必须放在项目 _temp 中。
param([string]$ClientBuildDirectory = '', [string]$FixtureDirectory = '', [string]$UnityEditorDirectory = 'D:/Program Files/2023.2.20f1c1/Editor')
# 项目根目录，不依赖启动脚本时的工作路径。
$geneProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!$ClientBuildDirectory) { $ClientBuildDirectory = Join-Path $geneProject '_temp/client-gene-ui-20261108/client-build/bin' }
# 服务端同步测试已经输出的字节样本，不从客户端手工构造权威结果。
if (!$FixtureDirectory) { $FixtureDirectory = Join-Path $geneProject '_temp/client-gene-ui-20261108/fixtures' }
# 编译器沿用实际 Visual Studio 安装。
$geneVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
# 使用本机 C# 编译器编译专用测试，不进入 Unity 正式资源。
$geneCompiler = & $geneVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
& $geneCompiler /nologo /nostdlib /target:exe "/r:$UnityEditorDirectory/Data/UnityReferenceAssemblies/unity-4.8-api/mscorlib.dll" "/r:$UnityEditorDirectory/Data/UnityReferenceAssemblies/unity-4.8-api/System.dll" "/r:$UnityEditorDirectory/Data/UnityReferenceAssemblies/unity-4.8-api/System.Core.dll" "/out:$ClientBuildDirectory/ClientGeneTests.exe" "/r:Client=$ClientBuildDirectory/GM_Game.dll" "/r:$ClientBuildDirectory/MMoRpgCommon.dll" "/r:$ClientBuildDirectory/Network.dll" "/r:$ClientBuildDirectory/Google.Protobuf.dll" "/r:$ClientBuildDirectory/System.Memory.dll" "/r:$ClientBuildDirectory/UnityEngine.CoreModule.dll" "/r:$ClientBuildDirectory/netstandard.dll" (Join-Path $PSScriptRoot 'ClientGeneTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
# 客户端程序集使用 Unity 的 Mono 运行库；普通 .NET Framework 启动器不能正确加载该程序集集合。
& (Join-Path $UnityEditorDirectory 'Data/MonoBleedingEdge/bin/mono.exe') (Join-Path $ClientBuildDirectory 'ClientGeneTests.exe') $FixtureDirectory
exit $LASTEXITCODE
