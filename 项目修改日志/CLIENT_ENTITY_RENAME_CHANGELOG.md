# 客户端实体命名迁移变更说明

## 修改文件范围

- `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/PlayerEntity.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/RoleEntity.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs`
- 客户端全局入口、主城管理器、网络清理入口、角色组件、背包组件、商城组件和 UI 控制器中的角色类型引用。

## 实现内容

- 本地角色业务数据类型由 `ClientRole` 改为 `PlayerEntity`。
- 其他玩家业务数据使用 `RoleEntity`，继承 `PlayerEntity`，不承担场景实体职责。
- `RoleWorldSystem` 改为 `SceneWorldSystem`，全局入口改为 `Global.SceneWorld`。
- 场景实体容器改为管理器内部私有成员，主城通过 `TryGetEntity` 查询，移除公开 `EntityWorld` 属性。
- 主城同步、角色组件、背包和商城引用完成类型迁移；网络断开清理入口改为 `ClearSceneWorld`。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-entity-rename-build-final3` 成功。
- 构建结果：0 个错误，26 个既有警告。
- 客户端源码中已无 `ClientRole`、`RoleWorldSystem`、`RoleWorld` 或公开 `EntityWorld` 引用。

## 未解决问题

- 尚未执行 Unity Editor 刷新、Play Mode 和真实服务器联调。
- 构建中的依赖版本冲突、弃用 API 和未使用字段警告为既有问题，本次未扩大范围处理。
