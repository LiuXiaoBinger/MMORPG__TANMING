# 实体系统实现说明

## 修改文件

- `LYF_Server/MMoRpgCommon/SceneEntity`：共享实体 ID、类型、生命周期、快照和组件契约。
- `LYF_Server/GameServer/SceneEntity`：服务端 `ROObject`、实体组件容器、实体世界、工厂和 Role/Player/Monster 等实体。
- `LYF_Server/GameServer/Entity/Role/OnlineRole.cs`：仅增加场景实体 ID 绑定，保持在线角色为持久化数据根对象。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity`：客户端对应实体、组件和 Builder 骨架。

## 设计

实体运行时基类为 `ROObject`，实体组件继承 `EntityComponentBase`，与背包/次数等 `RoleComponentBase` 完全隔离。`EntityFactory` 按实体类型装配组件，`EntityWorld` 负责注册、更新、快照和延迟销毁。`OnlineRole` 不继承实体基类，仅保存实体 ID 绑定。

## 验证

- MMoRpgCommon 构建：0 错误，4 个既有警告。
- GameServer 构建：0 错误，7 个既有警告。
- 新增实体目录静态检查：无 `foreach`、无三目运算符。

## 未解决

客户端工程仍引用 `Assets/GM_Game/Plugins/Common/MMoRpgCommon.dll` 的旧副本，尚未替换为本次命名空间修正后的共享 DLL；进入/离开场景接入 `RoleManager` 仍需单独实现。
