# 客户端模型测试编入真实背包源码，只有场景宿主使用专用替身，不修改正式DLL。
$clientRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$clientBin = Join-Path $clientRoot '_temp/equipment-gene-20261107/equipment-config-client/client-tests/bin'
New-Item -ItemType Directory -Force -Path $clientBin | Out-Null
# 独立进程只拷贝.NET Framework共享依赖，避免Unity编译目录中的运行时门面程序集遮蔽系统库。
$clientSharedBin = Join-Path $clientRoot '_temp/equipment-gene-20261107/equipment-flow/build/bin'
Get-ChildItem -LiteralPath $clientSharedBin -Filter '*.dll' | Copy-Item -Destination $clientBin -Force
$clientCompiler = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/Roslyn/csc.exe'
$clientStandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
# 固定真实客户端模型文件，避免把业务逻辑重新实现到测试替身中。
$clientSources = @('FunlModule/Bag/RoItemComponent.cs','FunlModule/Bag/RoItemContainer.cs','FunlModule/Bag/KnapsackChangeType.cs','FunlModule/Bag/KnapsackChangedEventArgs.cs','FunlModule/Component/RoleComponentBase.cs','FunlModule/Component/ClientDirtyTracker.cs','FunlModule/Component/ClientDirtyModule.cs','FunlModule/Component/ClientDirtySnapshot.cs','FunlModule/Component/ClientChangeContext.cs')
$clientSourcePaths = @((Join-Path $PSScriptRoot 'ClientDeltaTests.cs'),(Join-Path $PSScriptRoot 'Client/PlayerEntity.cs'))
# UpdateType 与网络事务共文件，按原文抽出枚举供纯模型编译，不手写另一套值。
$clientTransition = Get-Content -LiteralPath (Join-Path $clientRoot 'LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Bag/BagTransition.cs') -Raw
$clientEnum = [regex]::Match($clientTransition,'public enum UpdateType\s*\{[^}]+\}').Value
if ([string]::IsNullOrEmpty($clientEnum)) { throw '正式 UpdateType 枚举未找到' }
$clientEnumPath = Join-Path $clientBin '../UpdateType.cs'
[IO.File]::WriteAllText($clientEnumPath,$clientEnum,(New-Object Text.UTF8Encoding($false)))
$clientSourcePaths += $clientEnumPath
for ($clientIndex=0; $clientIndex -lt $clientSources.Count; $clientIndex++) { $clientSourcePaths += Join-Path $clientRoot ('LYF_MMO/Assets/GM_Game/Scripts/'+$clientSources[$clientIndex]) }
& $clientCompiler /nologo /target:exe "/out:$clientBin/ClientDeltaTests.exe" "/r:$clientBin/Network.dll" "/r:$clientBin/MMoRpgCommon.dll" "/r:$clientBin/Google.Protobuf.dll" "/r:$clientBin/System.Memory.dll" "/r:$clientBin/System.Runtime.CompilerServices.Unsafe.dll" "/r:$clientStandard" $clientSourcePaths *> (Join-Path $clientBin '../test-build.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $clientBin '../test-build.log') -Tail 12; exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $clientRoot '_temp/equipment-gene-20261107/equipment-flow/build/bin/GameServer.exe.config') -Destination (Join-Path $clientBin 'ClientDeltaTests.exe.config') -Force
# 独立exe没有Unity的程序集加载器，按本轮复制的共享DLL实际版本重定向System.Memory。
$clientConfigPath = Join-Path $clientBin 'ClientDeltaTests.exe.config'
[xml]$clientConfig = Get-Content -LiteralPath $clientConfigPath -Raw
$clientBinding = $clientConfig.configuration.runtime.assemblyBinding
$clientDependency = $clientConfig.CreateElement('dependentAssembly','urn:schemas-microsoft-com:asm.v1')
$clientIdentity = $clientConfig.CreateElement('assemblyIdentity','urn:schemas-microsoft-com:asm.v1')
$clientIdentity.SetAttribute('name','System.Memory')
$clientIdentity.SetAttribute('publicKeyToken','cc7b13ffcd2ddd51')
$clientIdentity.SetAttribute('culture','neutral')
$clientRedirect = $clientConfig.CreateElement('bindingRedirect','urn:schemas-microsoft-com:asm.v1')
# 只读取程序集清单，不加载客户端依赖；避免旧脚本固定版本与当前服务端输出冲突。
$clientMemoryVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $clientBin 'System.Memory.dll')).Version.ToString()
$clientRedirect.SetAttribute('oldVersion','0.0.0.0-4.0.1.2')
$clientRedirect.SetAttribute('newVersion',$clientMemoryVersion)
$clientDependency.AppendChild($clientIdentity) | Out-Null
$clientDependency.AppendChild($clientRedirect) | Out-Null
$clientBinding.AppendChild($clientDependency) | Out-Null
$clientConfig.Save($clientConfigPath)
& (Join-Path $clientBin 'ClientDeltaTests.exe') *> (Join-Path $clientBin '../client-delta-tests.log')
$clientResult = $LASTEXITCODE
Get-Content (Join-Path $clientBin '../client-delta-tests.log') -Tail 10
exit $clientResult
