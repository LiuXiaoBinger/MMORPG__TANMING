# 输入事件 RO 风格重构记录

## 修改内容

- 所有键盘、轴、动作、文本、鼠标和触摸输入统一派发为 `MEventType.Input`。
- `InputEventArgs` 作为唯一输入事件参数，直接承载动作名称、动作阶段、移动向量、按键和文本字符等业务字段，不再使用来源枚举分支。
- 删除键盘、轴、动作、鼠标和触摸来源专用桥接 API，避免事件类型与输入来源耦合。
- 从 `IEventArg` 和状态事件参数中移除 `Init(params object[])`，事件接口只保留 `Clone`、`Recycle` 和对象池生命周期标记。
- `PlayerInputCtr` 直接创建统一参数并派发，保留 `Movement`、`OnEnable`、`OnDisable` 兼容接口；移除重复移动时间累加和空轮询逻辑。

## 设计思路

参考 RO 项目的 `MEventDispatcher` 与 `IEventArg`：事件类别由 `MEventType` 决定，事件数据由实现 `IEventArg` 的具名参数类承载。监听器只需要订阅 `Input` 并读取统一参数。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore`：0 个错误，保留项目既有警告。
- 分发器将同一个参数实例传给所有监听器，并在无监听器或派发完成后统一调用 `Recycle`。
- `MMoveEventArgs.Clone` 已补齐 `isLocalMove` 字段复制；对象池首次回收会先初始化队列，避免空队列异常。
- verify5 构建：0 个错误；输出位于 `_temp/event-ro-final-verify5/`。
- SceneEntity 事件目录扫描未发现 `MEventArgs`、`params object[]` 或旧输入事件枚举引用。

## 未解决问题

- 尚未在 Unity Play Mode 中验证真实 Input System 的键盘、动作和触摸回调。
