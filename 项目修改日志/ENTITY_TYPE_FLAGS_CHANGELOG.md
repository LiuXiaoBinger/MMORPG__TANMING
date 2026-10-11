# EntityType 位标记迁移

## 修改文件

- `LYF_Server/MMoRpgCommon/SceneEntity/EntityType.cs`
- `LYF_MMO/Assets/GM_Game/Plugins/Common/MMoRpgCommon.dll`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/SceneEntityFactory.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Builder/PlayerEntityBuilder.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MObject.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs`

## 实现内容

- 将公共 `EntityType` 改为 `[Flags]` 位标记，`None = 0`，保留 `Projectile` 命名。
- 本地玩家统一使用 `EntityType.Role | EntityType.Player`，角色类别判断可以识别本地玩家。
- 客户端工厂按位判断实体类别，并优先解析 `Player`，避免组合值被 `Role` 分支截获。
- 客户端 `MObject.IsPlayer`、`MObject.IsRole` 和状态机实体分类改为位判断。

## 设计思路

基础类别使用独立位，组合类别由实体创建入口显式合成；未采用把 `Player` 常量定义为复合值的方式，避免破坏单个位检测和协议序列化语义。服务端业务实体本轮未修改。

## 验证结果

- `MMoRpgCommon` Debug 构建成功，0 个错误，保留 4 个既有警告。
- `GM_Game` Debug 构建成功，0 个错误，保留工程既有警告。
- 客户端精确 `EntityType ==`/`!=` 判断已清理，除复合键自身的枚举相等性比较外无残留。

## 未解决问题

- 服务端实体工厂和 `ROObject` 的 Flags 适配由服务端 Agent 负责，需与本公共枚举同步复验。
- 尚未执行 Unity Play Mode 与网络快照运行态验证。
