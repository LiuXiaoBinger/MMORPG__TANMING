# 客户端实体事件系统变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEventType.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEventHandler.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEventArgs.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEventDispatcher.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/EntityInputEventBridge.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/ROObject.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/EntityComponentBase.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/EntityComponentContainer.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/SceneEntityWorld.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityTransformComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

- 参考 `Ro_cs422nb` 的 `MEventType`、`MEventHandler`、`MEventDispatcher` 与 `MObject.AddEventListener`，为每个 `ROObject` 建立独立事件分发器。
- 实体组件基类提供 `RegisterEvent`、`UnregisterEvent`、`DispatchEvent`；组件容器在关闭组件后强制取消其全部事件订阅。
- 事件支持多监听器、重复订阅拦截、回调中注销自身、单个监听异常隔离和实体销毁时清理。
- `PlayerInputCtr` 将 Shift、Jump、Movement 的新版 Input System 回调，以及文本输入转发到 `Global.Instance.SceneWorld.LocalRole`；实体核心不轮询 `Input`。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-input-build`：0 个错误，31 个既有工程警告。
- `SceneEntity` 静态扫描：未发现 `namespace`、`foreach` 或三目运算符；每个新增顶级类型均独立文件。

## 未解决问题

- 未进入 Unity Play Mode 验证实际按键、移动和文本输入的运行时回调。
- 受击、战斗、属性、技能和飞行物命中协议尚未接入各自实体组件派发点。
