# 实体组件池实现说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MComponentMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityBuffComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntitySkillComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/base/EntityComponentBase.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/ROObject.cs`
- `LYF_MMO/Assets/GM_Game/GM_Game.csproj`

## 实现内容

组件管理器按组件实际类型维护队列，支持创建、回收、重新绑定宿主和额外组件类型注册。`MEntity` 的 MoveComp、Buff、Skill、Machine 作为专用快捷组件单独管理，不进入普通组件容器；普通组件仍由 `EntityComponentContainer` 管理生命周期。

## 验证结果

`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/component-pool-build6` 成功，0 个错误，31 个既有警告。目标文件未使用 `namespace`、`foreach` 或三目运算符。

## 未解决问题

`EntityComponentType` 当前没有 `Machine` 枚举值，状态机暂使用 `Sync` 类型标识，后续如共享协议增加专用类型需同步替换。
