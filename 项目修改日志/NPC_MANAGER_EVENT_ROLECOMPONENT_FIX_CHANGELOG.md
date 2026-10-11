# NPC 管理器、事件类型拆分与角色组件强类型修复

日期：2026-09-14

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/NpcManager.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/MEventHandler.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/EventCallbackHandler.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/EventCallbackHandler.cs.meta`
- `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Component/RoleComponentBase.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

1. NPC 正式创建路径改为统一场景实体世界入口，由 Builder 负责实体池、组件池、动作状态池和模型池初始化。
2. NPC 配置写入实体运行时字段，场景位置通过 `EntityTransformComponent` 更新；异步表现回调固定捕获实体身份并校验管理器版本、世界登记和对象引用，失效对象立即销毁。
3. 管理器销毁时按快照遍历 NPC 实体并按 `EntityId` 调用 `SceneWorld.RemoveEntity`，使普通组件、专用组件、状态和模型按实体生命周期回收。
4. `EventCallbackHandler` 委托从 `MEventHandler.cs` 拆出为独立顶级类型文件，并更新工程编译清单。
5. 删除 `RoleComponentBase.Load(object)` 万能载荷接口；背包、次数等组件继续通过具名强类型方法装载协议数据。

## 设计思路

- NPC 业务配置与场景实体身份分离：NPC 配置 ID 作为 `ConfigId`，运行时 `EntityId` 仍由场景世界分配。
- 异步资源回调不能依赖对象当前可变状态，必须校验“同一管理器生命周期 + 同一实体 ID + 同一实体引用 + 世界仍登记”。
- 角色组件基类不声明无法表达领域类型的万能载荷；需要数据时由具体组件声明明确协议类型入口。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/npc-rules-fix-20260914/build`：0 错误，31 个既有警告。
- 目标文件静态扫描：无 `namespace`、`foreach`、三目运算符、`??`、业务 `object`/`object[]`/`dynamic`。
- `git diff --check` 通过。

## 未解决问题

- 尚未在 Unity Play Mode 验证 NPC 资源延迟回调、管理器销毁和实体池多轮复用；后续由测试 Agent 执行专项回归。
