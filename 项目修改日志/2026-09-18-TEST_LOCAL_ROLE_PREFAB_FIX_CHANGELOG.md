# 测试场景本地主角模型修正

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`

## 修改内容

将测试场景 `MainCityOtherRoleMock` 的 `_simulateOnStart` 从 `1` 改为 `0`。该组件仍保留，手动启用时继续用于验证其他玩家同步；场景启动时不再自动创建 `Role/Role_MJS_Other`。

## 角色模型规则

- 登录或测试主角包：`Global.SubmitLocalRoleLoad` -> `SceneWorld.LoadLocalRole` -> `MainCityMgr.CreateLocalRole` -> `Role/Role_MJS`。
- 其他玩家同步：`MainCityMgr.HandleOtherRoleOnline` -> `Role/Role_MJS_Other`。

## 验证

`GM_Game.csproj` 构建通过，0 个错误、39 个既有警告。未执行 Unity Play Mode。

## 未解决问题

需要 Unity 编辑器空闲后运行 `New Scene`，确认登录主角实例名称为 `Role_MJS(Clone)`；其他玩家测试需手动启用 `MainCityOtherRoleMock`。
