# MTimerMgr 更新时间与回调链路分析

## 结论

`MTimerMgr` 不是 Unity 挂载组件，也不由技能、角色或投射物各自更新。它是全局单例，唯一的时钟推进入口在 `Global.Update()`：

```text
Unity 每帧调用 Global.Update()
    -> 读取 Time.deltaTime
    -> 转为四舍五入后的整数毫秒 deltaMilliseconds
    -> MTimerMgr.singleton.Update(deltaMilliseconds * 0.001f)
    -> SceneWorld.Update(deltaMilliseconds)
```

也就是说，计时器到期回调发生在同一 Unity 帧的 `SceneWorld.Update` 之前。

## 初始化和销毁

- `Global.Awake()` 调用 `MTimerMgr.singleton.Init()`：清空旧条目、递增会话版本、将累计时钟归零并解除暂停。
- `Global.ReleaseRuntimeResources()` 调用 `MTimerMgr.singleton.Uninit()`：清空未到期条目并暂停时钟。

## 注册、更新时间与回调

以 `MArtSkill` 的粒子、声音、位移阶段为例，它调用：

```csharp
MTimerMgr.singleton.SetTimer(延迟秒数, _onProcessTimer, 参数);
```

正延迟注册后的处理步骤如下：

```text
SetTimer(interval, handler, args)
    -> 计算 TriggerTime = 当前 ElapsedTime + 延迟毫秒
    -> 按 TriggerTime 升序插入 _timers

Global.Update()
    -> MTimerMgr.Update(fDeltaT)
    -> ElapsedTime 增加本帧毫秒数
    -> while (最早 TriggerTime <= ElapsedTime)
        -> 从 _timers 移除该条目
        -> timer.Handler(timer.Token, timer.Args)
```

因此 `_onProcessTimer` 等计时器回调并非由 `MArtSkill.Update` 直接调用，而是在 `MTimerMgr.Update` 的到期循环中同步调用。

## 两个容易混淆的点

1. `interval <= 0f` 时，`SetTimer` 立即执行 `handler(0, args)` 并返回 `0`，不会等到下一帧。
2. `IsPause == true` 时，`Update` 直接返回，累计时钟和正延迟回调都停止；已注册计时器保留，恢复后继续按同一累计时间推进。

## 顺序和取消语义

- 同一到期毫秒的条目按照注册顺序执行。
- 回调前先把条目从 `_timers` 移除，所以回调内部即使调用 `KillTimer`、重新注册计时器或重置技能，也不会令当前票据二次执行。
- `KillTimer(token)` 只能取消尚未到期的正延迟条目；立即回调使用 `token == 0`，已触发条目也不在队列中。

## 关键代码位置

- `Assets/GM_Game/Scripts/Global.cs` 的 `Awake`：初始化全局计时器。
- `Assets/GM_Game/Scripts/Global.cs` 的 `Update`：Unity 主线程的更新入口；第 139 行是唯一的 `MTimerMgr.singleton.Update(...)` 调用。
- `Assets/GM_Game/Scripts/Common/MTimerMgr.cs:43`：推进累计时钟并遍历到期计时器。
- `Assets/GM_Game/Scripts/Common/MTimerMgr.cs:90`：注册计时器；零/负延迟在此处立即回调。
- `Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:139`：技能残影、粒子、声音和冲刺阶段的注册示例。

## 当前限制

`ElapsedTime` 使用每帧 `Time.deltaTime` 转成的整数毫秒累加，长时间运行存在毫秒级取整误差；这是当前实现语义，本次未调整。
