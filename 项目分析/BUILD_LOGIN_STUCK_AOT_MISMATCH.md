# 打包后停在更新界面：主程序与热更资源不配套

日期：2026-10-12。检查目标为用户截图中进度完成、显示“没有资源更新”却不进入登录的客户端。

## 已确认的失败点

实际运行路径为`LYF_MMO/Build/GM_MMO.exe`。本机`Player.log`记录资源初始化成功，加载557056字节的GM_Game.dll，三份AOT补充元数据均返回OK，然后`Assembly.Load`抛出`TypeLoadException: Could not load type 'KnapsackType' from assembly 'MMoRpgCommon'`。异常发生于`Assets/Boot/Load.cs`的InitCode，尚未执行EnterGame加载登录场景。

“没有资源更新”只是零下载分支设置的最后一条提示，并不表示已成功加载热更代码。问题无需通过修改残影、技能或登录布局处理。

## 主程序与当前产物的差异

正在运行的`GM_MMO.exe`、`GameAssembly.dll`及`GM_MMO_Data/il2cpp_data/Metadata/global-metadata.dat`时间为2026-08-26；当前`Assets/GM_Game/Dlls/GM_Game.dll.bytes`和裁剪AOT产物为2026-10-12。旧主包的ScriptingAssemblies.json未包含MMoRpgCommon，旧构建备份也没有该程序集；当前HybridCLR裁剪产物已有MMoRpgCommon及KnapsackType，当前生成link.xml亦已保留该类型。

当前HybridCLR仅把GM_Game设为热更程序集，MMoRpgCommon属于主程序AOT依赖。资源更新无法往已发布主程序中添加此前不存在的AOT程序集。不能通过在Load中重复Assembly.Load共享DLL来绕过配套构建。

## 本项目重新构建顺序

1. 在Unity确认构建目标为Windows x64、IL2CPP，并将正式启动场景`Assets/Boot/Scene_Boot.unity`的Load运行模式设置为HostPlayMode及对应资源服务器地址。当前源场景序列化模式为EditorSimulateMode，发布前需切换。
2. 执行`HybridCLR/Generate/All`。该菜单生成裁剪AOT临时工程、link及桥接代码，输出在HybridCLRData；它不会更新用户当前运行的Build目录。
3. 执行`Build/BuildDll`，把当前热更DLL和补充元数据复制到`Assets/GM_Game/Dlls`。
4. 构建YooAsset的DefaultPackage，使DLL、配置和场景资源属于同一版本。联机模式使用的资源目录应与此次产物一致。
5. 执行`File/Build Settings`中的Build，生成真正的客户端主程序。退出旧进程后运行新输出目录的程序，使用完整目录，包括GameAssembly.dll及GM_MMO_Data，不能只替换exe。

验收时检查新主包包含MMoRpgCommon；Player.log不再出现上述TypeLoadException，并出现登录场景加载完成。随后以实际登录界面作为运行验收。

## 验证边界

本轮为现有Player日志、运行路径、旧主包及当前生成产物的只读核对。Unity MCP未发现可连接的Editor会话；没有重新构建主包、切换场景设置、关闭现有进程或发布服务器资源，不能声称新主包已运行通过。

记录见`项目修改日志/2026-10-12-BUILD-LOGIN-STUCK-CHECK-CHANGELOG.md`；证据归档到`_temp/build-login-stuck-20261012/client`。
