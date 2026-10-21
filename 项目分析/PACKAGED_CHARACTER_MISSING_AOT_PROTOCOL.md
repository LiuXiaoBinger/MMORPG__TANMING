# 打包程序人物消失：跳跃协议热更与AOT主包版本不配套

日期：2026-10-19。状态：根因确认，独立临时完整主包构建与接口验证通过，正式登录画面待验。本文记录本轮证据，不把此前跳跃专项通过等同于新主包已经运行成功。

## 玩家看到的现象与最先失败的位置

用户反馈打包程序人物消失；截图还可见前景模型和上方部分模型，但技能栏所有图标为空。截图本身不能确定可见模型所属账号。实际Player.log记录本地角色ID2进入世界实体容器，随后Role/Role_MJS开始加载，模型绑定失败；因此先按本地创建异常查，而不是仅根据截图判定美术资源缺失。

证据为 `_temp/packaged-character-missing-20261019/review/Player.snapshot.log`：

- 第49行加载GM_Game.dll，568320字节。
- 第188行：角色表现绑定失败，实体ID1，System.MissingMethodException: MethodNotFind MoveOperationReq::set_JumpCount。
- 第205行起PlayerEntity.Update持续抛同一缺失方法异常。
- 本次日志不是前轮KnapsackType的TypeLoadException，也不是Shader或角色Prefab不存在。

## 为什么一个协议字段会造成角色消失和技能栏空

1. 主城MainCityMgr.CreateRole沿原异步资源入口取得模型。
2. 原流程依次SetGameObject、绑定兼容控制器、注册主UI输入、设置相机跟随。
3. 为让服务器先知道出生坐标，流程先调用localRole.UpdateMove，再发送EnterWroldReq。
4. 新热更UpdateMove需要调用MoveOperationReq.JumpCount setter；旧主包AOT里的Network没有该方法，立即抛MissingMethodException。
5. 原CreateRole回调的catch执行DestroyLoadedRole，销毁此次本地主角实例。此时EnterWroldReq尚未发送。
6. 服务器原EnterWorld链负责附近玩家同步，并透传中心服；中心服CenterRoleCtrl.OnEnterWroldHandle取RoleSkillInfo并回11080。入场请求未发出，本端拿不到这次技能快照，所以HUD槽位保留而图标空。
7. 后续每帧实体Update继续触达缺失方法，反复刷错；重试加载模型或给catch加吞错都不会让旧AOT产生新setter。

关键文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`、`LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`、`LYF_Server/GameServer/manger/RoleManager.cs`、`LYF_Server/CenterServer/Ctrl/CenterRoleCtrl.cs`。

## 配套版本的证据

独立测试核实原 `LYF_MMO/Build/GameAssembly.dll` 为2026-10-19 11:29，新 `Assets/GM_Game/Plugins/Net/Network.dll` 为11:46；旧Build备份协议没有Req/Ntf JumpCount，实际旧global-metadata.dat类型表也没有JumpCount，当前插件及最新裁剪AOT有。原Build当前缓存manifest与Bundles匹配2026-10-19-895、产物16:48，未HTTP查当前资源服务器；真实895资源包内GM_Game568320字节、SHA以057685A6开头，与正式Dlls逐字一致，实际UpdateMove IL包含新setter调用，实际CreateRole IL也包含异常后的销毁分支。闭环报告为 `_temp/packaged-character-missing-20261019/review/old-package-audit.json`、`actual-CreateRole.il.txt`、`actual-UpdateMove.il.txt`。日志中29896次缺失方法是同一错误的重复，不等于29896个独立故障；本地绑定失败1次、成功0次。

HybridCLRSettings只把GM_Game.asmdef设为热更程序集。Network属于AOT依赖，已被编译到GameAssembly.dll；Boot.Load只加载GM_Game与三份基础补充元数据。补充元数据不能给已发布AOT增加本轮新方法，拷一份托管Network.dll到旧IL2CPP目录也不会替换原生实现。

因此本轮故障是“新业务热更调用新协议，实际主包仍旧AOT”。此前源码、协议回环、动画方法专项证明新代码逻辑与接口正确，不能证明用户已经更新完整配套主包。

## 最小处理与实际构建范围

保留已实现跳跃同步。不增加反射绕过、不吞MissingMethod、不跳过出生位置包、不扩展版本兼容框架，也不改变catch来掩盖失配。处理对象是完整Windows64 IL2CPP主包。

临时验证前只读Unity状态：非Play、非编译、非构建，活动目标StandaloneWindows64；唯一启用Boot场景为HostPlayMode，原主备资源地址保持，场景无未保存修改；当前加载协议有JumpCount。原MethodBridge.cpp存在且DEVELOPMENT=0匹配设置；Assets/HybridCLRGenerate/link.xml保留两消息类型全部成员，无需重GenerateAll。

本轮使用原BuildPipeline经MCP manage_build构建，输出 `_temp/packaged-character-missing-20261019/player/GM_MMO.exe`，非开发构建、只加详细报告，无auto_run。原Build目录、业务源码、正式资源、配置及服务器进程不修改，不发布资源。构建原流程会正常更新Unity/HybridCLR派生产物，不能把这些缓存当成正式发布。

## 原流程操作和验收

以后协议AOT有字段/方法变更时，确认当前Network插件、热更引用与生成裁剪/桥接一致；必要时运行HybridCLR/Generate/All，必须以真实返回判断成功。Build/BuildDll用于编译/复制热更和补充元数据，不会更新旧主包的GameAssembly。原YooAsset资源包与完整File/Build Settings主程序应配套，程序交付需整个输出目录，不能只替换exe。

本轮临时包应独立核新GameAssembly/Metadata和协议方法，再用对应版本进入主城，检查“角色表现绑定成功”、EnterWorld与11080、人物可见/能动/技能图标恢复，并回归双端跳跃。尚未实际登录的新包不能宣称画面通过。正式GameServer仍需加载上轮新协议透传才会向另一端发跳跃段；这与本轮主角创建异常分别验收。

## 当前验证结果与边界

- 真实日志与生产调用链已核，根因确认。
- 构建前7个正式输入已冻结SHA，见 `_temp/packaged-character-missing-20261019/client/before-inputs.json`。
- 临时完整主包BuildReport Succeeded，158.8911883秒、0错4警告；manage_build任务墙钟160.416秒。实际报告 `_temp/packaged-character-missing-20261019/client/build-report.txt`，无需重复编译。
- 独立实际新Metadata类型表Req33/Ntf35含四JumpCount存取，匹配真实895热更两方法引用；GameAssembly52490752字节SHA8AB5E4A6…，Metadata11537512字节SHAC4653C6D…；`_temp/packaged-character-missing-20261019/review/final-audit.json` PASS。
- 4警告为Mogo/Skybox旧式SetTexture语法一条、Common SDF.asset缺脚本一条、FriendChatPrefab的img缺脚本两条。未证实历史性，不称旧警告；本轮缺方法已有直接根因，未顺带修字体/聊天/Shader。
- 7冻结输入SHA保持，含原Build GameAssembly/Metadata和正式Network/热更/Boot/设置；5核心输出存在、本轮新增段根相对引用无缺失、限定diff-check通过。机器清单 `_temp/packaged-character-missing-20261019/client/delivery-verification.json`。
- 未发布、未覆盖原Build、未自动运行、未登录用户账号、未改数据库或重启服务；正式双端画面未验收。

可手动测试的新包入口：`D:/unitypro/LYFMMORGP/_temp/packaged-character-missing-20261019/player/GM_MMO.exe`。须保留完整player目录，不单拷exe；Boot原服务器地址保持，用户登录后按上节验收主角与技能栏。

日志：`项目修改日志/2026-10-19-PACKAGED-CHARACTER-MISSING-CHANGELOG.md`。关联：`项目分析/JUMP_ANIMATION_SYNC.md`、`项目分析/BUILD_LOGIN_STUCK_AOT_MISMATCH.md`；两次AOT失配触发点不同，不互相冒用证据。
