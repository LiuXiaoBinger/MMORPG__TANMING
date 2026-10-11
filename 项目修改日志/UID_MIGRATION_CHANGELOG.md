# UID 统一迁移变更说明

## 修改文件范围

- 客户端 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity`：实体基类、实体子类、Builder、实体工厂、实体世界、实体池及投射物组件。
- 客户端 `Manger/NpcManager.cs`：NPC 配置与异步回调使用实体 `UID`。
- 公共库 `LYF_Server/MMoRpgCommon/SceneEntity`：快照字段改为 `ulong UID`，删除 `EntityId.cs` 并移除工程登记。
- 服务端 `LYF_Server/GameServer/SceneEntity`：ROObject、实体世界、工厂和 UID 分配器；`OnlineRole` 的场景绑定属性改为 `SceneUid`。

## 设计思路

实体运行时身份只有一个 `ulong UID`。`ConfigId` 仅表示配置表类型，`RoleId` 仅表示持久角色身份，所有世界字典、异步请求校验、对象池复用和快照同步均使用 UID。

## 验证结果

公共库、客户端和 GameServer 均构建成功，0 个编译错误。最终构建产物位于 `_temp/common-uid-final`、`_temp/uid-client-final`、`_temp/uid-server-final`。

源码扫描确认 `LYF_Server`、`LYF_MMO` 的业务源码和工程文件不再包含 `EntityId`、`EntityIdAllocator`、`OwnerEntityId`、`SceneEntityId`、`AllocateEntityId` 或 `TargetEntityId` 残留；`RoleId`、`ConfigId` 保留各自持久化和配置表语义。

## 未解决问题

尚未执行 Unity Play Mode、真实模型/资源异步回调、对象池多轮复用及四端网络联调；这些运行态验证由主 Agent 后续安排。

## 2026-09-15 去除实体兼容 UID 生成

- 删除 `PlayerEntity(MainRoleInfo)` 与 `NpcEntity()` 两个会自行生成 UID 的兼容构造器，以及对应的 `_legacyUid/AllocateLegacyUid` 静态计数器。
- 测试脚本 `KnapsackWindowMockServer` 改为通过专用 `SceneEntityWorld.Create(EntityType.Player, ...)` 创建玩家，由实体世界统一分配并注入 `UID`。
- 测试角色销毁改为从专用实体世界按 UID 移除，避免直接构造实体造成世界索引和对象池生命周期脱节。
- 客户端构建成功：0 个错误，31 个既有警告；构建输出位于 `_temp/uid-duplicate-cleanup-build`。

## 最终复验补充

- 客户端、公共库和 GameServer 均重新构建通过；客户端 0 错误、31 个既有警告，公共库 0 错误，GameServer 0 错误、7 个既有警告。
- 静态检查确认实体子类不再声明或生成第二套自身 UID；世界分配器产生的 UID 通过 Builder/Pool 注入并由 `MObject.UID` 保存。
- `OwnerUID`、`TargetUID`、`SceneUid` 均为关系/绑定语义，未作为实体自身身份重复使用。
- 按用户要求已移除 `OwnerUID` 字段及 `ownerUid` 构造参数；宠物、召唤物、镜像和投射物如需关联对象，统一通过已有关系组件中的 `UID` 引用。
