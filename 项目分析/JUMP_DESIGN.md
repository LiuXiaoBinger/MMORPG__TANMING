# 客户端跳跃功能设计

## 1. 本次范围

本方案只解决客户端本地角色的跳跃表现和运动逻辑，不加入网络协议、服务器校验、位置广播或同步回滚。后续接入移动同步时，跳跃只需要把起跳、再次起跳和落地作为离散事件补充到已有移动快照流程。

当前项目已有 `PlayerInputCtr` 的 `Jump` 输入、`PlayerEntity` 场景实体、`MMoveComponent` 移动组件和 `MModel` 的 `CharacterController` 引用。跳跃应接入这条现代实体链路，不继续使用旧 `MainRoleCtrl`/`RoleFSM` 的并行控制。

审核确认项目目前还存在旧 `RoleCtrlBase/MainRoleCtrl` 的 CharacterController 运动入口。后续实现必须选择唯一的位移权威：正式本地主角使用 `PlayerEntity -> MEntity -> MMoveComponent -> MModel`，由 `MMoveComponent` 统一调用一次 `CharacterController.Move`；旧控制器只能保留输入兼容桥接，不能再次积分重力或移动。否则同一帧会出现两次重力、重复碰撞和落地事件。

## 2. 方案选择

采用“向上初速度 + 重力积分 + CharacterController.Move”的运动学方案。

- 起跳时设置垂直速度 `verticalVelocity`。
- 每个逻辑帧执行 `verticalVelocity += gravity * deltaTime`。
- 每帧将水平位移和垂直位移合成为一个位移，交给角色的 `CharacterController.Move`。
- 发生地面碰撞后把垂直速度限制为小的负值，避免角色悬浮，并重置本轮起跳次数。

不采用 Rigidbody 物理作为主方案：角色实体由对象池和自有 `MModel` 管理，Rigidbody 会把速度、碰撞回调和 Unity FixedUpdate 引入另一套生命周期，难以与现有 `SceneEntityWorld.Update` 对齐。

不采用预设贝塞尔曲线或固定高度曲线：曲线适合表现，但不适合与 CharacterController 碰撞结果组合；初速度加重力更容易调节高度、滞空时间和多段跳，也便于未来服务器复现基础运动结果。

## 3. 运动参数

建议由角色移动配置提供以下参数，初版可先使用客户端配置默认值：

| 参数 | 含义 | 建议范围 |
| --- | --- | --- |
| `Gravity` | 垂直重力，单位为 Unity 单位/秒平方，必须为负数 | `-20` 到 `-40` |
| `FirstJumpHeight` | 一段跳目标高度，单位为 Unity 单位 | `1.5` 到 `3` |
| `SecondJumpHeight` | 二段跳再次起跳高度 | 不高于一段跳或按职业配置 |
| `ThirdJumpHeight` | 三段跳再次起跳高度 | 不高于二段跳或按职业配置 |
| `MaxJumpCount` | 本轮允许的总起跳次数，只允许 `1/2/3` | `1`、`2`、`3` |
| `CoyoteTime` | 离开地面后仍允许触发一段跳的时间，单位秒 | `0.08` 到 `0.15` |
| `JumpBufferTime` | 提前按下跳跃时缓存到落地的时间，单位秒 | `0.08` 到 `0.15` |
| `AirControl` | 空中水平移动控制比例 | `0` 到 `1` |

每段跳跃的起跳速度由目标高度计算：

```text
initialVelocity = sqrt(2 * abs(Gravity) * jumpHeight)
```

这样不需要手写三组互相矛盾的速度和重力参数。若项目世界单位不是米，只把高度和速度理解为项目世界单位即可。

## 4. 状态模型

物理状态和动画状态分开保存：

### 4.1 物理状态

- `Grounded`：角色在地面，`JumpCount = 0`。
- `Ascending`：垂直速度大于零。
- `Descending`：垂直速度小于零但尚未落地。
- `Landing`：本帧检测到从空中回到地面，只持续一次逻辑更新。

### 4.2 起跳计数

`JumpCount` 表示本轮空中序列已经消耗的起跳次数，不表示当前动画阶段：

1. 地面按下跳跃：`JumpCount = 1`，使用 `FirstJumpHeight`。
2. 空中再次按下且 `MaxJumpCount >= 2`：`JumpCount = 2`，重新设置垂直初速度，使用 `SecondJumpHeight`。
3. 空中第三次按下且 `MaxJumpCount >= 3`：`JumpCount = 3`，重新设置垂直初速度，使用 `ThirdJumpHeight`。
4. `JumpCount == MaxJumpCount` 后继续按键直接忽略，不允许无限连跳。
5. 落地后把 `JumpCount` 清零，下一次地面起跳重新从一段跳开始。

二段跳和三段跳采用“重新赋予该段起跳速度”，而不是在当前垂直速度上继续叠加。这样每次空中起跳高度稳定，不会因为按键时机不同导致高度失控。

## 5. 输入和判定流程

1. `PlayerInputCtr` 只负责把 `Jump.started` 转成现有强类型 `InputKey` 事件；`Jump.canceled` 不负责触发物理起跳，避免长按造成重复起跳。
2. `MMoveComponent` 订阅 `InputKey`，只处理 `ActionName == "Jump"` 且阶段为 `Started` 的输入。
3. 组件先检查死亡、眩晕、技能锁定、交互、骑乘和不可移动状态；不满足条件时丢弃请求。
4. 若当前落地，立即执行一段跳；若当前空中且剩余起跳次数大于零，执行二段或三段跳。
5. 若刚离地时间不超过 `CoyoteTime`，允许把一次地面跳跃补偿为一段跳；土狼时间只适用于一段跳，不增加空中跳次数。
6. 若角色尚未落地但输入发生在 `JumpBufferTime` 内，记录一次待处理请求；落地后只消费一次缓存请求。
7. 每帧先更新水平移动，再更新垂直速度，最后一次性调用 `CharacterController.Move`，避免水平和垂直两次移动造成碰撞状态不一致。

## 6. 地面检测和落地

- 首选 `CharacterController.isGrounded` 和 `Move` 返回的 `CollisionFlags.Below`。
- 地面状态必须在本帧位移前后都更新：位移前用于判断是否允许起跳，位移后用于判断是否刚刚落地。
- 落地时把垂直速度设置为轻微负值，例如 `-2`，不能直接设置为零，否则斜坡或连续帧可能出现短暂悬浮。
- 只有从空中状态转为地面状态时派发一次 `JumpLanded`，不能每个地面帧重复派发。
- 当前项目不具备统一地形查询接口，第一版不自行实现射线地面系统；角色预制体必须带有效的 `CharacterController`。缺失时应记录明确错误并禁用跳跃，而不是伪造地面高度。

## 7. 状态机和动画接入

物理由 `MMoveComponent` 负责，动画由动作状态机负责，不能在输入控制器里直接调用 Animator。

- 第一阶段不强制新增共享 `StateDefine.kStateJump`，避免立即修改服务端共享状态枚举和状态许可矩阵；先使用现有 `MEventType.JumpStarted/JumpEnded` 与 `MMoveComponent.IsJump` 驱动表现。
- 后续动画资源稳定后，再新增独立的 `MJumpActionComponent`。状态机只负责进入、播放和退出跳跃动画，不能计算位移。
- `MMoveComponent` 在一段、二段、三段起跳和落地时派发强类型事件，动作状态根据事件刷新动画。
- 如果当前职业表没有跳跃动画字段，第一阶段允许使用默认动作动画作为回退，但必须保证战斗待机不会覆盖 `IsJump` 状态。
- 动画至少区分 `JumpStart`、`JumpLoop`、`JumpEnd` 三个表现阶段；没有完整资源时可先由同一剪辑回退，不改变物理流程。
- 跳跃动作被死亡、眩晕、强制位移打断时，组件必须清理垂直速度和待处理输入，避免回收后对象池残留状态。

## 8. 推荐接口职责

这些是后续编码时的职责边界，不是本次要提交的代码：

- `MMoveComponent.RequestJump()`：接收一次跳跃请求，执行资格检查和起跳次数判断。
- `MMoveComponent.UpdateVerticalMovement(float deltaSeconds)`：更新重力、垂直速度和落地转换。
- `MMoveComponent.TryStartJump(int jumpIndex)`：按第几次起跳选择高度并设置初速度。
- `MMoveComponent.IsGrounded`：对外提供当前地面状态，只读。
- `MMoveComponent.JumpCount`：对外提供本轮已消耗次数，只读。
- `JumpStartedEventArgs`：携带本次起跳序号和起跳位置，使用 `MEventPool` 管理生命周期。
- `JumpLandedEventArgs`：携带落地位置和本轮最终起跳次数，使用 `MEventPool` 管理生命周期。
- `MJumpActionComponent`：后续需要独立跳跃动画时再加入，只负责动作状态和动画，不计算位移。

## 9. 后续网络同步预留

本次不实现同步，但事件字段应保留以下语义：起跳序号、起跳位置、起跳时刻、落地时刻和是否被强制打断。未来接入移动同步时，只把这些事件作为移动快照的 `SyncReason`，不把客户端的每帧重力结果当作服务器可信数据。

## 10. 验收用例

1. `MaxJumpCount = 1`：地面可以跳一次，空中再次按键无效，落地后可再次跳。
2. `MaxJumpCount = 2`：地面一段跳，空中一次二段跳，第三次按键无效。
3. `MaxJumpCount = 3`：地面一段跳，空中依次二段、三段跳，第四次按键无效。
4. 空中落地时起跳次数清零，下一次起跳再次从一段跳开始。
5. 起跳过程中持续移动，水平速度不被垂直运动清零；空中水平控制按 `AirControl` 生效。
6. 离地后在 `CoyoteTime` 内按键可以补跳，超过窗口不能补跳。
7. 落地前在 `JumpBufferTime` 内按键，落地后只触发一次起跳。
8. 死亡、眩晕、技能锁定、骑乘和缺少 `CharacterController` 时不能起跳。
9. 对象池回收后再次生成角色，垂直速度、起跳次数、缓存输入和跳跃状态全部恢复默认值。
