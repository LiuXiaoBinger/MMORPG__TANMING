# 2026-09-14 实体事件强类型参数重构

## 修改范围

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/ROObject.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MObject.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/base/EntityComponentBase.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityTransformComponent.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

- 删除万能载荷 `MEventArgs.cs` 及其工程编译项；`IEventArg` 移除 `Init(params object[])` 与废弃状态字段，只保留复制和回收契约。
- 新增 `KeyInputEventArgs`、`InputAxisChangedEventArgs`、`InputActionEventArgs`、`TextInputEventArgs`、`MouseButtonEventArgs`、`TouchInputEventArgs`，每个事件载荷独立文件并使用明确字段类型。
- `ROObject`、`MObject`、`MEventDispatcher` 和 `EntityComponentBase` 均改为传递 `IEventArg`；移动和位置变化为无载荷事件，统一传递 `null`。
- 文本输入新增独立的 `MEventType.TextInput`，不再伪装成按键按下事件。
- 分发器为每个监听器克隆一份不可变载荷；不自动回收调用方传入的参数，防止 `MActionComponent` 或后续延迟逻辑保存事件时发生对象池复用污染。
- 审核返工：新增 `InputActionStarted`、`InputActionPerformed`、`InputActionCanceled`。`PlayerInputCtr` 的 Shift、Jump、Movement 回调只使用这三类事件；`InputKeyDown/InputKeyUp/InputKeyHeld` 仅由 `KeyInputEventArgs` 派发，`InputAxisChanged` 仅由 `InputAxisChangedEventArgs` 派发。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/entity-event-strongly-typed-build`：成功，0 个错误、31 个既有警告。
- 静态扫描：客户端 `MEventArgs` 引用为 0；目标事件范围未发现 `object`、`object[]`、`dynamic`、`Init(params object[])`、`foreach`、三目运算符或新增命名空间。
- 输入映射静态核对：`PlayerInputCtr` 未向 `InputKey*` 或 `InputAxisChanged` 派发 `InputActionEventArgs`；输入桥仅用键盘与输入轴对应的具名参数派发旧事件。
- 独立代码审核：通过。`InputKey*`、`InputAxisChanged` 与 `InputActionStarted/Performed/Canceled` 分别只对应一种具名参数类型，输入桥的动作事件白名单可阻止后续混用。

## 未解决问题

- 未进入 Unity Play Mode 验证真实 Input System、触摸设备和动作状态组件的运行时监听链路；需要在后续客户端输入回归时覆盖。
- 本次未处理工程既有的依赖版本和类型冲突警告。
