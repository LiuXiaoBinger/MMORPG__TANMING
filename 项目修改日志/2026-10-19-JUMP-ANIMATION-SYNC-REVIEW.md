# 2026-10-19 远端跳跃动画：独立测试与审查

## 范围与结论

只审查用户报告的“A跳跃，B看A上升却播放走路动画”。未修改客户端、协议或服务端业务代码；未调用Unity MCP、停止Play、构建正式包、重启或部署服务。专题为 `项目分析/JUMP_ANIMATION_SYNC.md`，实现职责日志为 `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CLIENT-CHANGELOG.md` 和 `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-SERVER-CHANGELOG.md`。

审查通过，没有剩余阻断问题。客户端初稿把全部零耗时显式上报放行，审查建议只为跳跃段变化放行；开发已采纳。最终普通移动仍保留原正耗时规则与0.1秒节流。

## 根因复现与测试方法

修前保存移动、跳跃、玩家与主城五源中的四份相关文件；移动事件参数使用开发保存的修前完整 `IEventArg.cs`。随后从修前与末稿逐字提取生产方法，不另写同步算法：

- `PlayerEntity.Update` / `UpdateMove`，检查真实发送判断和生成请求。
- `MainCityMgr.SyncMoveHandle`，真实通知解析及移动事件参数装配。
- `MMoveComponent.Update` / `onSyncMoveEvent` / `LagSyncUpdate` / `SetMoving` / `ApplySyncJumpState` / `Shutdown`，检查状态、影子追赶、根位移消费和回池。
- `MJumpComponent.Update` / `OnJumpStarted` / `OnJumpEnded` 及阶段播放方法，检查起跳、循环、落地交接。
- `MDefaultActionComponent.StateName` / `ShouldPlayMovementAnimation` / `IsLandingAnimationPlaying` / `PlayAnim`，检查默认动作是否覆盖跳跃。
- 完整 `MMoveEventArgs` 类型，检查工厂、克隆和回池重置；当前正式生成协议两源及现Google.Protobuf执行真实编解码。

实体容器、事件宿主、向量/旋转、碰撞提交与Animator播放采用薄边界记录器。特别是本地玩家基础Update只隔离为外围边界，未声称验证完整PlayerEntity组件容器、原生Animator采样或物理接地；生产跳跃和移动关键方法仍为原文。

修前6项确认：通知有纯Y位移，远端没有IsJump与JumpStarted；真实默认动作随后选为Walk，坐标继续上升。修后37项通过，取代早期34项，数量不累加。

## 修后覆盖

1. 纯竖跳立即进入JumpStart，默认动作不覆盖为Walk；阶段时长结束进入JumpLoop。
2. 二段、三段各重播一次起跳，同段重复位置通知不重播。
3. 同坐标、零耗时起跳也发包；连续段变化与落地不被0.1秒节流吞掉。普通静止零耗时显式上报仍早退。
4. 到达空中影子目标不提前落地；空中松方向只结束水平移动态；后续纯Y落地仍能追到目标。
5. 零段落地通知到达时暂保跳跃，影子抵达实际落点后才派JumpEnded。静止落地播放JumpEnd后恢复Idle；移动落地直接恢复Walk，保留一次落地声。
6. 远端切段不调用本地起跳、不改变本地垂直速度、不再执行一次碰撞重力；原技能动画优先级与根位移/程序位移合并单次提交保持。
7. 组件回池清当前与待落地段；事件Clone保留段号，池化普通Create不残留旧段。
8. 缺新字段的请求和通知解析后默认JumpCount=0；请求字段8、通知字段9与现协议契约一致。

## 编译与末稿审查

- 独立280个当前GM_Game源Rebuild：0错误、39警告。开发编译为38警告；差一为历史独立夹具已有CS8032：Unity.Properties.SourceGenerator需要System.Runtime6，其他业务警告类型一致。没有为警告数量修改业务或重复构建。
- 独立复核两生成源与本轮protoc产物逐字节一致；服务端6个正式输入末稿哈希与开发清单一致。客户端协议插件与本轮独立Network构建一致，SHA256为 `4429175E2D87C4F8D00CC6E78D6B178955AA7BAE6EAEEAD7428EDED718E6DBBF`。
- 已阅读服务端真实80项回环测试源码和输出：生产队列、组件与网关临时TCP，不连接正式账号/数据库。该80项由服务端职责本轮运行，本审查没有重复执行或增加总数。
- 280编译源、行为测试输入、11个本轮正式路径哈希一致；限定源码 `git diff --check` 通过。新增字段/参数/关键分支有中文说明，未增加多余队列、确认等待或重力权威框架，无新增namespace、foreach、三目或超过三个组合项表达式。

## 证据、失败尝试与边界

证据统一为 `_temp/jump-animation-sync-20261019/review/`：`before-runtime.log`、`final-runtime.log`、两份methods/inputs清单、`client-build.log`、`compiled-source-hashes.json`、`review-source.diff`、`final-manifest.json` 和 `final-audit.json`。执行入口为 `verify_jump.py`、`build_client.py`、`final_audit.py`。

临时夹具首编译缺System.Memory/netstandard引用，且修前测试正文曾引用尚不存在的新事件字段；只补夹具程序集引用、删去修前不适用的断言，未改业务迎合测试。另一夹具首断言把1000毫秒同步推进与落地动画推进放在同一Tick，JumpEnd在这一帧内已经播完，导致“仍在落地阶段”的时机断言失败；改为先检查真实Move.Update刚派发的落地，再单独推进Jump.Update。最终前后结果以上述6/37项为准。

当前正式Play仍加载旧协议/旧移动方法的事实由主Agent只读确认；本轮没有证明正式双客户端画面已生效。旧游戏服会丢新增段字段，需新客户端协议/动画实现与新服务端协议/透传配套加载；没有擅自部署或重启。原生Animator、正式双账号与安装包运行验收待后续实际环境，不将本隔离测试写成已联机发布。
