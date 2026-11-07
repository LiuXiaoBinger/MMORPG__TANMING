# 从测试位置解析项目根目录，构建和运行证据都留在本任务独立临时目录。
$geneRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# 此脚本不使用正式 bin/obj，不覆盖运行中的游戏服或客户端 DLL。
$geneOutput = Join-Path $geneRoot '_temp/equipment-gene-20261107/gene-traits/build'
New-Item -ItemType Directory -Force -Path $geneOutput | Out-Null
# 使用本机 .NET Framework 4.8 对应的 Visual Studio 构建工具。
$geneVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
# 实际 MSBuild 路径由安装器发现，不硬编码安装版本。
$geneMsbuild = & $geneVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $geneMsbuild (Join-Path $geneRoot 'LYF_Server/GameServer/GameServer.csproj') /t:Build /p:Configuration=Debug "/p:OutDir=$geneOutput/bin/" "/p:BaseIntermediateOutputPath=$geneOutput/obj/" /v:minimal /nologo *> (Join-Path $geneOutput 'build-game.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $geneOutput 'build-game.log') -Tail 20; exit $LASTEXITCODE }
# 测试编译器与项目构建使用同一安装版本。
$geneCompiler = Join-Path (Split-Path $geneMsbuild) 'Roslyn/csc.exe'
# protobuf 使用的 .NET Standard 接口由框架兼容程序集提供。
$geneStandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
& $geneCompiler /nologo /target:exe "/out:$geneOutput/bin/GeneEquipmentTests.exe" "/r:Server=$geneOutput/bin/GameServer.exe" "/r:$geneOutput/bin/Network.dll" "/r:$geneOutput/bin/MMoRpgCommon.dll" "/r:$geneOutput/bin/Google.Protobuf.dll" "/r:$geneOutput/bin/System.Memory.dll" "/r:$geneOutput/bin/System.Runtime.CompilerServices.Unsafe.dll" "/r:$geneStandard" /r:System.Numerics.dll (Join-Path $PSScriptRoot 'GeneEquipmentTests.cs') *> (Join-Path $geneOutput 'build-tests.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $geneOutput 'build-tests.log') -Tail 20; exit $LASTEXITCODE }
# 复用正式游戏服重定向，防止测试选择另一版 protobuf 依赖。
Copy-Item -LiteralPath (Join-Path $geneOutput 'bin/GameServer.exe.config') -Destination (Join-Path $geneOutput 'bin/GeneEquipmentTests.exe.config') -Force
& (Join-Path $geneOutput 'bin/GeneEquipmentTests.exe') *> (Join-Path $geneOutput 'equipment-tests.log')
# 保留真实退出码，输出只显示末尾验收结果或失败证据。
$geneResult = $LASTEXITCODE
Get-Content (Join-Path $geneOutput 'equipment-tests.log') -Tail 12
exit $geneResult
