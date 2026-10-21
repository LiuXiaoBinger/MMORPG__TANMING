# 技能退出过早释放剪辑造成瞬间下沉

日期：2026-10-13。范围：用户反馈两端所有技能出现模型向下移动后马上复原，并补充释放一次后无法再次施法。

## 已确认故障

主职责通过正式施法入口释放无耗蓝滑步10080，采集真实角色层级高度，证据为 `_temp/skill-height-dip-20261013/root-live-height-skill10080.txt`：

- 实体世界Y始终为18.73892，没有角色碰撞根节点突然下落。
- 退出技能后，逻辑已回Default，但原生动画仍采样Slider_F。
- 约0.4204秒时真实剪辑被替换为Controller原始空`Skill`，动画绑定`ready=False`，目标状态为Idle；左脚世界Y18.32197，比实体地面低约0.41695米，Hips也明显降低。
- 约0.4754秒Idle完全接管后，脚恢复到18.85254、Hips恢复19.90951。

因此本次下沉发生在动画退出过渡，原因是参与混合的真实Skill剪辑提前归还，临时改用空占位剪辑。不是本轮根运动同步添加了世界Y，也不能通过冻结骨盆、修改所有动画高度或取消重力处理。

## 调用与语义

RO `MAnimator.InState` 比较`_playStateName`，表示最近一次播放请求。前轮为修复快速停走，本项目也令`InState`比较已有`_currentState`，这是正确的请求去重语义。

本项目额外的 `TryReleasePendingSkillAnim` 原先复用了`InState(STATE_SKILL)`判断资源能否释放。最新请求已改为Idle时，Animator尚未求值新的CrossFade，仍可能采样Skill且`IsInTransition`尚为false；此时不能用最新请求判断旧剪辑是否还被使用。

## 最小修正

仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs` 的 `TryReleasePendingSkillAnim`：改读原生`GetCurrentAnimatorStateInfo(0).IsName(STATE_SKILL)`，并保留已有`IsInTransition(0)`保护。真实Skill仍在当前状态或任意过渡未结束时保留剪辑；真正退出后沿原入口归还。

`InState`最新请求语义、Play/CrossFade、技能生命周期、根运动、协议、动画及配置资产保持，没有增加字段、队列或额外等待机制。静态发现的冷加载空槽路径没有对应当前故障证据，不顺带修改。

## 施法后无法继续释放

主职责现场只读门禁显示角色MP=0，CurrentSkill为空、默认状态、未跳跃、计时器未暂停、技能CD为0且`CanSkill=True`。普通技能因法力成本不满足而`core.CanStart=False`；成本0的滑步可正常再次释放。这一时刻没有技能状态锁死证据，不能靠去掉法力校验或重置生命周期掩盖。

权威MP初值、消耗及回包来源由后端职责继续核查；本客户端修复不更改数值或法力规则。

## 验证边界

开发280源Rebuild零错误、38既有警告；独立280源零错误、39既有警告且警告签名保持。真实释放方法与InState抽取修前10项复现/回归、末稿10项通过，独立`review/final-audit.json`为PASS。20份冻结输入只有上述MAnimator变化，方法外逐字保持、编译输入无漂移，见 `client/scope-audit.json`。

只读原生18组正式技能/待机×根运动开关采样证明完整真实剪辑的根世界Y恒0、deltaY恒0，双脚均高于根基准，不能把骨盆下蹲当成故障。

用户自然退出Play后，Boot域重载已确认实际IL为末稿；主执行正式Humanoid/AOC及生产MAnimator释放方法隔离验证PASS。最新请求FIdle、原生当前仍Skill且`IsInTransition=False`的请求/评估缝隙保留Slider_F，随后12帧原生过渡仍保留真实剪辑，第12帧实际退出后释放。脚相对根最低0.1100968米，没有临时空槽下沉；此次控制台零错误警告。证据 `client/native-exit-release-result.txt`。

此原生测试的ClipInfo与待释放调用为独立装配，没有网络、完整技能生命周期或正式双客户端全量画面，不据此扩大验收范围。

日志：`项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CLIENT-CHANGELOG.md`。证据根：`_temp/skill-height-dip-20261013`。
