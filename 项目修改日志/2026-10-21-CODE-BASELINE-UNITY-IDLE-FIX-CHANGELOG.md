# 2026-10-21 先提交当前代码基线，再修Unity待机动画报错

## 用户授权与范围

用户要求先提交代码，禁止提交或合入main，然后检查并修改Unity bug。当前分支codex/feature-development，先保存已有代码与必需资源、配置、协议、UI和服务端分类迁移及其记录；没有改写用户既有内容。开发修正另行提交，便于前后对照。

本次是已有改动的基线提交，不声称历史内容由本轮开发。排除UserSettings、本地生成VisualScripting桩、热更产物GM_Game.dll.bytes、yoo构建清单和hfs临时文件；Network.dll是现有协议配套插件，随协议源和生成C#一起保存。没有加入_temp、凭据或外部执行器机器配置。

## 提交前验证

- 当前291份客户端真实源码隔离编译0错误、38既有警告，证据_temp/navigation-test-entry-20261021/review/pre-unity-bug；未替正式DLL。
- 当前Unity日志仍有Idle真实动画配置和回退失败报错；该基线带已知运行问题，不冒称运行验收通过。先提交后由开发子Agent最小修复，主Agent复测。
- 本轮Unity MCP资源可访问但实例列表为0，没有连接到Editor；日志备份_temp/unity-idle-bug-20261021/editor-before.log。

## 逐项文件与操作

以下是基线相对当前HEAD的完整路径清单；删除项和新增同名文件属于已有SceneEntity分类迁移，Git提交时自动检测重命名。详细业务历史见相应原日志；本次不新增业务功能。

| 操作 | 文件 | 内容说明 |
| --- | --- | --- |
| 修改 | `AIconfig/agent/code_reviewer.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `AIconfig/agent/game_server.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `AIconfig/agent/product_manager.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `AIconfig/agent/unity_client.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `Docs/AI/CURRENT_TASK.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `Docs/AI/TEST_REPORT.md` | 当前角色约束、协作上下文和验收记录 |
| 修改 | `LYF_MMO/Assets/Boot/Scene_Boot.unity` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` | 已有协议及配套生成插件 |
| 修改 | `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity.unity` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Common/EnumDefine.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/IEventArg.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/KnapsackWindow.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleAttriibuteWindow.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_MMO/Assets/GM_Game/UISprites/Font/FeiHuaSongTi-2 SDF.asset` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/Assets/artres/Resources/UI/Font/Common SDF.asset` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/ProjectSettings/HybridCLRSettings.asset` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_MMO/ProjectSettings/NavMeshAreas.asset` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 修改 | `LYF_Server/GameServer/Components/ROMoveComponent.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_Server/GameServer/GameServer.csproj` | 已有服务端编译项路径适配 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/AiComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/AttributesComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/BeHitComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/BossEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/BuffComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/CollectionComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/CollectionEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/CombatComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/DeadComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/DummyEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EagleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EliteEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EntityComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EntityComponentBase.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EntityComponentContainer.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EntityFactory.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/EntityWorld.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/FightGroupComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/HatredComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/LifetimeComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MercenaryComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MercenaryEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MirrorRoleComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MirrorRoleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MonsterComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MonsterEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/MoveComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/NavigationComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/NpcComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/NpcEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/PetEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/PlayerEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ProjectileComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ProjectileEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ROObject.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ROSkill.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ROSkillCore.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/Role.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/RoleComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/RoleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/RuntimeEntityComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/SceneComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/SceneUidAllocator.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/SkillComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/SummonComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/SyncComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/TrapComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/TrapEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 删除/迁移旧路径 | `LYF_Server/GameServer/SceneEntity/ViewComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 修改 | `LYF_Server/Network/Proto/RequestEntity.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_Server/Network/Proto/ResultEntity.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 修改 | `LYF_Server/ProtoBuf/proto/RequestEntity.proto` | 已有协议及配套生成插件 |
| 修改 | `LYF_Server/ProtoBuf/proto/ResultEntity.proto` | 已有协议及配套生成插件 |
| 修改 | `Luaban/MiniTemplate/Datas/#SkillEffectInfo.xlsx` | 已有技能配置表 |
| 修改 | `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx` | 已有技能配置表 |
| 修改 | `Luaban/MiniTemplate/Datas/#SkillPresentationInfo.xlsx` | 已有技能配置表 |
| 修改 | `项目分析/README.md` | 已有专题说明、架构/难点或使用记录 |
| 修改 | `项目分析/项目难点.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity/NavMesh-MainCityNavigation.asset` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity/NavMesh-MainCityNavigation.asset.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SettingPanel.cs` | 当前已有客户端/服务端业务或生成协议源码；保存修复前基线 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SettingPanel.cs.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Knapsack.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Knapsack.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Role.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Role.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Setting.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Setting.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Skill.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/MainMenu/Menu_Skill.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Role.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Role.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Role1.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Role1.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Set.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Set.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Skill_UI.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/Skill_UI.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Common_Yun_01.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Common_Yun_01.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Common_Yun_02.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Common_Yun_02.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Setting_Img01.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Setting_Img01.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Setting_Img02.png` | 已有UI、场景、字体、导航制作或项目配置资源 |
| 新增 | `LYF_MMO/Assets/GM_Game/UISprites/Setting/UI_Setting_Img02.png.meta` | 对应脚本、资源或目录的Unity GUID标识 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Base/EntityComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Base/EntityComponentBase.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Base/EntityComponentContainer.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Base/ROObject.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Base/RuntimeEntityComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/AiComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/AttributesComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/BeHitComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/BuffComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/CollectionComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/CombatComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/DeadComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/FightGroupComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/HatredComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/LifetimeComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/MercenaryComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/MirrorRoleComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/MonsterComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/MoveComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/NavigationComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/NpcComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/ProjectileComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/RoleComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/SceneComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/SkillComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/SummonComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/SyncComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/TrapComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Component/ViewComponent.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/BossEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/CollectionEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/DummyEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/EagleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/EliteEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/MercenaryEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/MirrorRoleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/MonsterEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/NpcEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/PetEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/PlayerEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/ProjectileEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/Role.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/RoleEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Entity/TrapEntity.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Skill/ROSkill.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/Skill/ROSkillCore.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/World/EntityFactory.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/World/EntityWorld.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `LYF_Server/GameServer/SceneEntity/World/SceneUidAllocator.cs` | 已有服务端实体目录分类，内容与验证见SceneEntity整理日志 |
| 新增 | `项目修改日志/2026-10-11-SKILL-HUD-CENTER-INSET-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-11-SKILL-HUD-LAYOUT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-11-SKILL-HUD-QI-STAMINA-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-11-SKILL-HUD-REFINE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-11-SKILL-HUD-RESOURCE-INITIAL-FULL-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-BOOLEAN-CONDITION-RULE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-BOOT-HFS-PROGRESS-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-BUILD-LOGIN-STUCK-CHECK-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-ENTER-GAME-MOVE-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-F-ROLL-REFINE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-F-ROLL-RIGHT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-F-ROLL-SLIDE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-JUMP-CAST-BLOCK-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-MOVE-CITY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-MOVE-SYNC-SERVER-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-SKILL-COMPONENTS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-SKILL-SEND-MODULES-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-TARGET-RANGE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-TARGET-SELECTION-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-TARGET-SELECTION-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-RO-TARGET-SELECTION-SERVER-BOUNDARY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-ROLE-SKILL-DB-DATA-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-ROLL-LOCAL-MOVE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-ROLL-SPECIAL-SKILL-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-ROLL-STATEBLOCKED-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-SCENE-ACTIVE-COMMIT-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-SCENE-SIMPLE-FLOW-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-SKILL-HUD-STAMINA-FULL-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-SLIDE-CATEGORY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-SLIDE-GHOST-FRAME-CANCEL-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端SceneEntity-注释补充与废代码分析-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端其余模块-注释补充与废代码分析-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端生命周期函数排序-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端生命周期排序-SceneEntity-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端生命周期排序-UI-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-客户端生命周期排序-其余模块-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-服务端GameServer-注释补充与废代码分析-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-服务端公共库与网络-注释补充与废代码分析-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-12-服务端分服与测试-注释补充与废代码分析-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-BOOLEAN-CONDITION-REFACTOR-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-BURST-JOBS-ILPP-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-DEFAULT-ACTION-RUNTIME-FIX-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-MOVE-IDLE-SLIDE-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-PROJECT-DIFFICULTIES-DOC-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-PROJECT-INTERVIEW-SUMMARY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-RO-AIR-WALL-ANALYSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-RO-SEND-CONFIRM-SEMANTICS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-RO-SKILL-COMBO-ANALYSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-RO-TARGET-SELECTION-ANALYSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SHADER-COMPILER-IPC-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-APPEARANCE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-INDEPENDENT-REVIEW-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-PACKAGE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-PLAYBACK-CHECK-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-RUNTIME-ACCEPTANCE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-FX-SHARED-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-ROOT-SYNC-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-ROOT-SYNC-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-ROOT-SYNC-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-SKILL-STUCK-SERVER-READONLY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-STANDARD-SURFACE-SHADER-IPC-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-SERVER-CHECK-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-14-INTERVIEW-ENHANCED-DOC-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-14-INTERVIEW-ENHANCED-DOC-CLARIFY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-14-PROJECT-INTERVIEW-DETAIL-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-15-INTERVIEW-CONCISE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-15-OPENSOURCE-MMO-ANALYSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-EDITOR-ILPP-DIAGNOSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-SERVER-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-PACKAGED-CHARACTER-MISSING-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-19-PACKAGED-CHARACTER-MISSING-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-FPS-CPU-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-FPS-INVESTIGATION-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-LANDING-WALK-SLIDE-CLIENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-LANDING-WALK-SLIDE-INDEPENDENT-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-ARROW-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-ARROW-CLIENT.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-ARROW-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-CLIENT.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CLIENT.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-MAIN-MENU-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-FPS-DISPLAY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CLIENT.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-PATHFINDING-ANALYSIS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-SETTINGS-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-SETTINGS-TEST.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CLIENT.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-REVIEW.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-21-DOUBAO-CLI-WINDOWS-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-21-DOUBAO-DESKTOP-RESEARCH-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-21-SCENEENTITY-DIRECTORY-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目修改日志/2026-10-21-WORKBUDDY-GATEWAY-SETUP-CHANGELOG.md` | 已有功能变更与验证记录归档 |
| 新增 | `项目分析/BOOT_HFS_VERSION_REQUEST.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/BUILD_LOGIN_STUCK_AOT_MISMATCH.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/DEFAULT_ACTION_SHORT_CIRCUIT_RUNTIME_FIX.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/F_ROLL_SLIDE_DESIGN.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/JUMP_ANIMATION_SYNC.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/JUMP_SKILL_CAST_GATE.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/LANDING_WALK_ANIMATION_HANDOFF.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/MAIN_CITY_FPS_RUNTIME_20261020.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/MAIN_CITY_PERFORMANCE.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/MAIN_FUNCTION_MENU_UI.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/MOVE_IDLE_SLIDE_ANIMATION.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_AIR_WALL_SKILL_CHECK.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_FPS_DISPLAY.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_MOVE_SYNC_CLIENT.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_MOVE_SYNC_SERVER.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_SETTINGS_UI_INTEGRATION.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_SKILL_COMBO_TABLE_CONFIG.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_SKILL_COMPONENT_PARITY.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_SKILL_SEND_MODULE_FLOW.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_TARGET_SELECTION_DISTANCE.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_TARGET_SELECTION_FLOW.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/RO_TARGET_SELECTION_SERVER_BOUNDARY.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SCENE_ACTIVE_COMMIT_FIX.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SCENE_SIMPLE_LOGIN_FLOW.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SERVER_LOGIC_CLOCK_MS_REMAINDER.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SHADER_COMPILER_IPC_REVIEW.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_FX_APPEARANCE_SHADER_VARIANTS.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_FX_PACKAGE_RUNTIME_DIAGNOSIS.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_FX_PLAYBACK_APPEARANCE_CHECK.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_ROOT_MOTION_REMOTE_SYNC.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_ROOT_MOTION_SYNC.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/SKILL_STUCK_SERVER_MP_DIAGNOSIS.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/STANDARD_SURFACE_SHADER_COMPILER_IPC.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/TWO_ACCOUNT_CITY_CLIENT_FLOW.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/TWO_ACCOUNT_CITY_SERVER_POSITION_ORDER.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析与注释补充总报告-2026-10-12.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析报告-客户端-SceneEntity.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析报告-客户端-其余模块.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析报告-服务端-GameServer.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析报告-服务端-公共库与网络.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/废代码分析报告-服务端-分服与测试.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ET/ET-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ET/assets/et-architecture-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ET/assets/et-fiber-02.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/FishMMO/FishMMO-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/FishMMO/assets/fishmmo-login-flow-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/FishMMO/assets/fishmmo-topology-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/Intersect-Engine/Intersect-Engine-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/Intersect-Engine/assets/intersect-architecture-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/Intersect-Engine/assets/intersect-layers-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/NoahGameFrame/NoahGameFrame-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/NoahGameFrame/assets/nf-plugin-arch-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/NoahGameFrame/assets/nf-topology-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/OpenCoreMMO/OpenCoreMMO-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/OpenCoreMMO/assets/opencoremmo-topology-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/OpenCoreMMO/assets/opencoremmo-worldmodel-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/README.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ServUO/ServUO-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ServUO/assets/servuo-architecture-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/ServUO/assets/servuo-mainloop-02.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/UnityMMO/UnityMMO-源码解析.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/UnityMMO/assets/unitymmo-aoi-sync-02.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/UnityMMO/assets/unitymmo-client-layer-03.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/开源MMORPG项目解析/UnityMMO/assets/unitymmo-server-topology-01.png` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/面试点总结-2026-10-12.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/项目难点 - 副本.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/项目难点与亮点-面试版-2026-10-14.md` | 已有专题说明、架构/难点或使用记录 |
| 新增 | `项目分析/项目难点与亮点-面试精简版-2026-10-15.md` | 已有专题说明、架构/难点或使用记录 |

## Git与后续

- 分支codex/feature-development，仅提交推送该功能分支，不操作main。
- 本次提交完成后再进入Idle缺配置故障诊断；源码修复由开发子Agent实施，失败证据退回原任务。
- 提交号、上传结果与修复/验收结论将在本日志后续章节补充；此时仅为准备基线提交。
