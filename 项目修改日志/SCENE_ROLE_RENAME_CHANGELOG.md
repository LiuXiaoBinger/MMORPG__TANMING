# 场景角色命名迁移变更说明

## 修改文件范围

- 客户端角色业务模型、角色组件、背包/商城/控制器引用。
- 客户端全局入口、主城角色表现管理器和网络断线清理入口。
- 客户端 `GM_Game.csproj` 类型文件清单。

## 实现内容

- 将本地角色业务模型从 `ClientRole` 改为 `PlayerEntity`。
- 将其他玩家业务模型改为 `RoleEntity`，本地模型和其他玩家模型由 `SceneWorldSystem` 统一管理。
- 将 `RoleWorldSystem` 改为 `SceneWorldSystem`，全局入口改为 `Global.SceneWorld`。
- `PlayerEntity` 和 `RoleEntity` 本身就是场景实体，均直接继承 `ROObject`；不再存在“业务角色模型再关联另一个场景实体”的双对象设计。
- `SceneWorldSystem` 内部使用单个 `SceneEntityWorld` 统一管理本地玩家、其他角色和其他实体；没有公开 `EntityWorld` 属性。
- 主城表现层通过 `TryGetEntity` 查询实体并绑定实体组件；背包、次数等角色业务组件仅作为本地 `PlayerEntity` 的业务组件容器，不改变实体身份。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/player-entity-fix3`：成功，0 个错误，警告为既有工程警告。
- 静态扫描未发现 `ClientRole`、`RoleWorldSystem`、`RoleWorld` 或公开 `.EntityWorld` 的旧引用。
- 目标客户端范围未发现新增 `namespace`、`foreach` 或三目运算符；Unity 刷新编译后控制台错误为 0。

## 未解决问题

- 尚未执行 Unity Play Mode、资源加载和真实服务器四端联调。
- 服务端本轮按要求不修改；工作树中已有服务端实体文件和 `OnlineRole` 绑定改动保持原样。
