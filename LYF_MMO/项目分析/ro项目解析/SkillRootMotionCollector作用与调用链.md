# SkillRootMotionCollector 作用与调用链

核对日期：2026-10-21。本文分析 Unity 客户端当前源码。

## 用途

此组件收集技能动画自带的水平根位移，将其交给实体移动组件统一应用。例如动画中的角色向前踏步或突进，Animator 会给出这次动画求值产生的 `deltaPosition`；本组件截取 X/Z 位移，让角色的位置和技能动作一起前进。

采集的位移单位为米，已经是世界空间位移增量，消费时不再乘一次帧间隔。实际前进距离还取决于动画自身的根运动数据、配置倍率和碰撞结果。原地动画没有水平根位移时，本组件不会凭空生成突进距离。

## 调用链

```text
MModel.FinishLoad
  → 在原生 Animator 同节点获取或添加采集组件
  → Init(model, animator)，保存原开关并启用 applyRootMotion

MArtSkill.TriggerAnim
  → 在满足当前技能与模型条件后 Bind(this)

Unity 进行动画求值
  → OnAnimatorMove
  → 读取 Animator.deltaPosition，清除 Y 分量
  → 乘以技能时间窗口的倍率，累加到 _translation

MMoveComponent 更新
  → ConsumeDelta，取出累计位移并清零
  → 合并输入、重力及程序位移
  → Entity.MoveCharacter → MModel.MoveCharacter
  → 存在且启用 CharacterController 时提交 Move；否则直接修改模型位置
```

上述链路说明生产者和消费者，不能据此认定每次动画求值的数据一定在同一帧消费；实际采集与消费时机还取决于 Unity 动画更新模式和脚本执行顺序。

## OnAnimatorMove 的具体处理

1. 检查已初始化、存在 Animator 和技能，并通过 `OwnsRootMotion` 确认技能仍拥有当前模型动作及已就绪剪辑。
2. 只处理第 0 层 `Skill` 状态；过渡时读取目标状态，目标不是 `Skill` 则丢弃累计位移。
3. 使用 `normalizedTime × Clip.length` 得到原动画播放秒数。
4. 调用 `MArtSkill.RootMotionScale(seconds)`，从 `RootMotionSegments` 查找时间窗口。窗口含起点、不含终点，范围外返回 0。
5. 读取 `Animator.deltaPosition`，把 `y` 置为 0，再按倍率累计。根旋转没有采集。

比如配置窗口 `[0.2秒, 0.5秒)`，倍率为 1.5，动画在窗口内一次评估向前移动 0.1 米，就累计 0.15 米。窗口外倍率为 0，不累计该段根位移。

## 生命周期

- `Bind`：切换技能时清掉前一个技能遗留的位移；重复绑定同一实例保留尚未消费的数据。
- `ConsumeDelta`：读出后立即清零，且再次检查技能所有权；失效时返回零，避免重复或错误应用。
- `Release`：技能退出动作槽时只解除自己的绑定；由 `MArtSkill.ReleaseAnimation` 调用。
- `OnDisable`：清掉未消费数据。
- `Unbind`：恢复原 `applyRootMotion`，清除技能、模型和 Animator 引用；模型解绑或组件销毁时执行。

## 与程序位移的区别

此脚本的位移来源是动画求值结果。`MDisplacementComponent` 则根据配置的程序位移段产生增量。两者的结果都由 `MMoveComponent` 消费，统一走模型移动入口；本脚本本身没有调用 `CharacterController.Move`，也没有负责服务器的命中或位置权威结算。

## 关键文件

- `Assets/GM_Game/Scripts/SceneEntity/3DModel/SkillRootMotionCollector.cs`：采集、缓存与消费接口。
- `Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs`：安装、初始化、解绑采集器及模型碰撞移动。
- `Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：技能绑定、所有权判定、时间窗口倍率和释放。
- `Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimeRootMotionSegment.cs`：窗口起止秒数与倍率。
- `Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：合并并提交位移。

## 核查范围

已静态核对以上实现及调用位置，未修改业务代码，未启动 Unity 实测具体动画的位移距离和碰撞效果。
