# 事件对象池与主城角色同步测试变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/IEventArg.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/InputEventArgs.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/EntityStateChangedEventArgs.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/MainCityOtherRoleMock.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

- `InputEventArgs.CreateAction` 和 `InputEventArgs.CreateText` 从 `MEventPool<InputEventArgs>` 取得实例；动作输入和文本输入不再直接构造事件参数。
- `EntityStateChangedEventArgs.Create` 从 `MEventPool<EntityStateChangedEventArgs>` 取得状态切换快照；状态机不再直接构造状态事件参数。
- 两类参数的 `Recycle` 会先清空上次数据，再归还各自对象池。`MEventPool` 用 `Deprecated` 阻止同一实例重复入池；派发器仍在所有监听器回调结束后统一回收。
- `MainCityMgr.HandleOtherRoleOnline(RoleBaseInfo)` 收敛其他玩家上线的实体写入与表现创建。网络回调和测试组件都调用该入口，首次同步才异步创建角色预制体。
- 新增 `MainCityOtherRoleMock` 测试组件。该组件只在 `UI/test` 中存在，不自动挂载场景或预制体；支持 Inspector 的 ContextMenu 和启动延迟。销毁时只删除它自身首次创建且 UID 仍匹配的角色。

## 设计说明

事件参数属于一次派发的短生命周期数据。创建端从类型专属池获得参数，监听器同步读取；若监听器需要跨帧保存，应调用 `Clone` 获得独立池化副本并自行 `Recycle`。分发器负责原始参数的统一归还，避免输入和状态切换频繁产生托管垃圾。

主城测试不能绕过正式路径直接创建 `RoleEntity`，否则无法覆盖角色世界登记、重复角色判断和异步表现绑定。本测试组件仅构造与协议相同的 `RoleBaseInfo`，其余流程与服务器同步完全一致。

## 验证结果

- 构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/pool-maincity-mock-build`。
- 结果：0 个错误、39 个项目既有警告。
- 静态检查：未发现直接 `new InputEventArgs(...)` 或 `new EntityStateChangedEventArgs(...)`；新增测试代码未使用 `namespace`、`foreach`、三目运算符、`object`、`object[]` 或 `dynamic`。

## 未解决问题

- 尚未在 Unity Play Mode 手动挂载 `MainCityOtherRoleMock` 验证预制体异步加载和场景表现。
- 尚未执行对象池多轮复用与监听器跨帧 Clone 的运行态回归。

## 审核返工

- `PlayerInputCtr.DispatchMovement` 现在分别检查本地角色和相机。只有两者有效时才调用 `MMoveEventArgs.Create` 从对象池取得参数；计算或派发异常时会回收仍未移交给分发器的参数。
- `EntityInputEventBridge.DispatchInput` 与 `DispatchInputMove` 在目标实体为空时主动回收参数。成功调用实体分发器后，参数所有权转交给分发器统一结束生命周期。
- 主城管理器把网络同步入口、测试同步入口和共享核心链路分开：真实 `HandleOtherRoleOnline` 收到相同角色 ID 后会移除测试所有权；`HandleTestOtherRoleOnline` 只为本次首次创建的实体登记 `RoleId -> UID`。
- `MainCityOtherRoleMock` 不再直接查询场景世界或删除角色。组件销毁时调用 `MainCityMgr.RemoveTestOtherRole`，由管理器同时验证测试所有权、角色 ID、UID 和当前世界实体后再删除。
- 重新构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-build`，0 个错误、41 个项目既有警告。

## 最终异常回收修复

- `MEventDispatcher.DispatchEvent` 以最外层 `try/finally` 管理事件参数生命周期，任何提前返回、快照创建异常或未知分发异常都会调用 `Recycle()`。
- 两个输入桥接入口在实体为空时直接回收参数；实体有效时仅移交分发器，避免重复回收。
- 最终构建结果为 0 个错误、45 个既有警告；未执行 Unity Play Mode 与真实网络联调。
- 最终复验：补齐返工范围内局部变量中文语义注释后再次执行同一构建命令，仍为 0 个错误、41 个项目既有警告。

## 事件回收异常路径修复

- `MEventDispatcher.DispatchEvent` 将事件参数回收收敛到最外层 `finally`。无监听器、监听器快照创建失败和监听器回调异常均会执行一次 `Recycle`。
- 单个监听器异常仍在循环内记录，不会中断其余监听器；参数生命周期不再依赖正常返回路径。
- `EntityInputEventBridge` 在实体为空时由桥接层回收参数；实体非空时所有权转交 `MEventDispatcher`，其 `finally` 负责异常路径的唯一回收，避免桥接层重复入池。
- 构建验证产物：`_temp/maincity-mock-build/`。

## 最终独立审核

- 审核结论：通过。`MEventDispatcher` 的最外层 `finally` 覆盖全部派发路径；输入桥接器与分发器的参数所有权边界无泄漏或重复回收。
- 主城模拟确认：正式 `.unity`、`.prefab` 中未引用 `MainCityOtherRoleMock`；真实网络同步同角色 ID 会撤销测试清理权，组件销毁不会误删已被网络接管的实体。
- 独立复现构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-mock-final-audit`，0 个错误、41 个既有警告；日志 `_temp/maincity-mock-final-audit.log`。
- 未执行 Unity Play Mode 与真实网络联调；该组件只允许挂入专用测试场景或临时测试对象。
