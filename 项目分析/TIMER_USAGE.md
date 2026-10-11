# 服务端定时器使用说明

## 设计来源

参考 `Ro_cs422nb` 的 `ITimer`、`CTimerMgr` 和 `SaveRoleDataTimer` 关系，本项目将定时器分为两层：

- `MMoRpgCommon/Time/ITimer.cs`：最小回调接口，只描述定时器到期时执行的函数。
- `MMoRpgCommon/Time/CTimerMgr.cs`：公共定时器管理器，集中保存句柄、间隔、重复次数和剩余时间，并在逻辑线程统一派发回调。
- `GameServer/Entity/SaveRoleDataTimer.cs`：角色保存业务触发器，仍属于 GameServer。它只实现 `ITimer`，到期后设置待保存标记，不负责公共计时和网络发送。

参考项目中，`SaveRoleDataTimer` 到期后只设置角色的保存标记，角色后续固定帧再组包并发送数据库请求。本项目保留这个职责边界。

## 主循环接入

`GameGlobal.UpdateLogicFrame` 每次固定逻辑帧调用一次：

```csharp
CTimerMgr.Instance.Update(deltaMilliseconds);
```

因此定时器回调始终在 GameServer 逻辑线程执行。`SetTimer`、`KillTimer`、`GetTimeLeft`、`IsActive`、`Update` 和 `Clear` 也只能由该线程调用；网络线程和其他工作线程不得直接操作管理器，需要跨线程触发时应先投递到 GameServer 的命令队列。

每个定时器在一次 `Update` 中最多触发一次。`GameGlobal` 使用单调时钟累计真实经过时间，并按 50 毫秒固定步长推进逻辑；不足一帧的余量会保留，单次主循环最多追赶 5 帧，剩余时间继续留到下一轮，避免掉帧时产生回调风暴。

## 注册定时器

实现一个独立的 `ITimer` 类型，然后通过 `SetTimer` 注册：

```csharp
public sealed class ExampleTimer : ITimer
{
    public void OnTimer(uint timerId, uint triggerCount)
    {
        // 这里只处理到期业务，不执行跨线程操作。
    }
}

ExampleTimer timer = new ExampleTimer();
long handle = CTimerMgr.Instance.SetTimer(
    timer,
    0,
    1000,
    CTimerMgr.InfiniteRepeatCount);
```

参数说明：

- `timer`：回调对象，不能为空。
- `timerId`：业务标识，原样传给 `OnTimer`。
- `intervalMilliseconds`：触发间隔，必须大于零。
- `repeatCount`：触发次数；`CTimerMgr.InfiniteRepeatCount`（值为 `-1`）表示无限循环，正数表示有限次数。
- 返回值：正数句柄表示注册成功，`CTimerMgr.InvalidTimer`（值为 `0`）表示参数无效。

省略重复次数时可以使用三参数重载，它注册一个无限循环定时器：

```csharp
long handle = CTimerMgr.Instance.SetTimer(timer, 0, 1000);
```

## 停止和查询

```csharp
CTimerMgr.Instance.KillTimer(handle);
bool active = CTimerMgr.Instance.IsActive(handle);
int leftMilliseconds = CTimerMgr.Instance.GetTimeLeft(handle);
```

`KillTimer` 可以重复调用，重复取消不会产生副作用。服务器退出时调用 `CTimerMgr.Instance.Clear()` 清理剩余定时器。

## SaveRoleDataTimer 使用方式

`SaveRoleDataTimer` 不放入 `MMoRpgCommon`，因为它依赖 GameServer 的 `OnlineRole` 和角色保存流程。`OnlineRole` 创建后绑定自身并启动：

```csharp
_saveRoleDataTimer.SetRole(this);
_saveRoleDataTimer.Start();
```

`Start` 内部向 `CTimerMgr` 注册 60 秒无限循环定时器；`Stop` 内部调用 `KillTimer`。到期时 `OnTimer` 只设置 `_needSave`，角色每秒逻辑检查 `ConsumeNeedSave` 后才调用 `RoleNetSender.SendRoleDataToDB`。

新增业务定时器时，优先复用 `CTimerMgr` 的注册、取消和查询接口；业务类只保留状态与回调，不要重复实现倒计时字段或另起线程。
