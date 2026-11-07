# 从测试目录定位项目根目录，所有编译产物统一放入 _temp。
$geneRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# 本次运行的程序集、缓存与日志输出位置。
$geneOutput = Join-Path $geneRoot '_temp/gene-runtime-tests'
New-Item -ItemType Directory -Force -Path $geneOutput | Out-Null
# 使用本机 Visual Studio 提供的 .NET Framework 构建工具。
$geneVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
# 定位支持项目目标框架的 MSBuild 实际安装路径。
$geneMsbuild = & $geneVswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $geneMsbuild (Join-Path $geneRoot 'LYF_Server/GameServer/GameServer.csproj') /t:Build /p:Configuration=Debug "/p:OutDir=$geneOutput/bin/" "/p:BaseIntermediateOutputPath=$geneOutput/obj/" /v:minimal /nologo *> (Join-Path $geneOutput 'build.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $geneOutput 'build.log') -Tail 25; exit $LASTEXITCODE }
# 使用同一套构建工具中的 C# 编译器，测试引用真实服务端程序集。
$geneCompiler = Join-Path (Split-Path $geneMsbuild) 'Roslyn/csc.exe'
& $geneCompiler /nologo /target:exe "/out:$geneOutput/bin/GeneRuntimeTests.exe" "/r:Server=$geneOutput/bin/GameServer.exe" "/r:$geneOutput/bin/Network.dll" "/r:$geneOutput/bin/MMoRpgCommon.dll" (Join-Path $PSScriptRoot 'GeneRuntimeTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $geneOutput 'bin/GeneRuntimeTests.exe') $geneRoot
exit $LASTEXITCODE
