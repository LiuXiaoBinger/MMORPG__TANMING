# EntityType Flags 服务端变更说明

## 修改文件

- `LYF_Server/MMoRpgCommon/SceneEntity/EntityType.cs`
- `LYF_Server/GameServer/SceneEntity/PlayerEntity.cs`
- `LYF_Server/GameServer/SceneEntity/EntityFactory.cs`

## 实现内容

- 公共 `EntityType` 使用 `[Flags]` 位标记，每个实体类别占用独立位。
- 玩家实体统一使用 `EntityType.Role | EntityType.Player`，保证玩家同时满足角色和玩家类别判断。
- 服务端工厂改为位标记判断，支持传入组合类别，不再依赖 `switch` 的精确枚举值匹配。
- 组件装配分支改为按类别位判断，保持原有实体组件组合和怪物品质分支。

## 验证结果

- `dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entitytype-server-build`
- 构建成功，0 个错误，7 个既有警告。

## 未解决问题

- 客户端其他 `EntityType` 使用点由客户端 Agent 继续迁移。
- 尚未执行 Unity Play Mode 与真实网络快照联调。
