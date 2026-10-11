# RO 技能时间线注册、回调消费与移动职责

本报告为主 Agent 安排归档的真实源码职责分析（2026-10-11）。前文表格与原文记录修正前状态；最终实现和独立验收另见末章“本轮最终修正与验收”。只读参考未修改RO或任何业务/资产；本轮四份业务源由开发Agent修改、独立Agent审核，分析归档仅增量文档。读取协作入口后检查真实文件，未遍历roab；未使用历史结论代替源码。

## 结论

用户指出 MArtSkill.Update 同时重新调用 FireEvents、处理等待消费、绑定根运动的职责不合适，真实 RO 源码支持这一质疑。虽然上一轮 _eventsRegistered 防止了重复 SetTimer，它仍在技能 Update 里每帧尝试注册，并维护另一条每帧已到期指令消费路径，不能称为完整沿用 RO 的启动注册、计时器直接回调职责。

RO ChargeList/ECharge 是独立的配置位移，使用曲线、距离、开始和结束秒数，经实体 MDisplacementComponent 每帧积分。它不是本项目 Animator.deltaPosition 根运动。此次应修正表现的启动、资源就绪、模型就绪职责，保留既有真实动画根位移与移动组件合并碰撞入口；不应为了同名回调伪造 Charge 或重写尚未接入的位移轨道。

## RO 真实源码与签名

以下路径均位于 F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/：

| 路径与行号 | 实际实现 |
| --- | --- |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:1016 | protected override void Start()，调用 base.Start 后第1021行 FireEvents()。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:264 | protected virtual void FireEvents()，ChargeList逐条按 At * _timeScale 注册计时器；第270～274行 SetTimer 返回token并 AddedTimerToken。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Utility/MTimer.cs:217 | public uint SetTimer(float interval, TimerEventHandler handler, params object[] args)，转setTimer；秒配置传入管理器。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Utility/MTimer.cs:176 | public void Update(float fDeltaT)，第182行RoundToInt(fDeltaT * 1000)，第203行cell.Fire直接派发到期回调。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs:225 | protected void OnProcessTimer(uint token, object[] args)，token去除后第241行直接InnerProcessTimer((int)args[0], args[1])。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:80 | protected override bool InnerProcessTimer(int id, object param)，第96～98行ECharge调用charge(param)后return true；EFx/EAudio同样直接fx/audio。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:217 | public override bool Update(float fDeltaT)，仅针对_rotateData处理角色朝向，第225行return base.Update(fDeltaT)，没有FireEvents、队列Drain、动画根位移绑定。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs:552 | public virtual bool Update(float fDeltaT)，推进技能时钟、追随位置、超时、弹体更新；若_isAnimInited则Execute。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs:609 | public void Execute()，_casting && !_execute保障第615行Start只执行一次。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:588 | private void charge(object o)，MSkillChargeData包含CurveX/Z、DistanceX/Z、At/End；第619行totalTime=End-At、第620行angle=SkillTowardsTo、第622行发布MEvent_Displacement。鹰分支第604行MEvent_Displacement_Fly。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:81 | protected override void EventSubscribe()，订阅MEvent_Displacement/Displacement_Fly/DisplacementStop。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:119 | private void onStart(IEventArg e)，取得曲线与距离，保存起点、总时间、角度、UUID后置_displacementing。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:259 | public override void Update(float fDeltaT)，p=min(duration/totalTime,1)，第272/276行按曲线进度乘距离，经过踢墙、高度、空气墙后第295行赋Entity.Position。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:466 | 有charge时Stop发布DisplacementStop，skillUuid过滤归属，cleanup决定强停语义。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Entity/MEntity.cs:964 | 实体创建独立MDisplacementComponent，未把持续位移计算放技能上。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Entity/MEntity.cs:1092 | AppendMove累积普通移动；Update:1118在base驱动组件后第1122行Move消费。此入口与MDisplacementComponent直接Position曲线位移应分别理解。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs:113 | public virtual void TriggerAnim()，AnimList非空留给定时动画覆盖，否则播放SkillClips/ClipName，未采集Animator.deltaPosition。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:491 | void OverrideAnim(uint token, object[] args)，计时器驱动多段动画覆盖，未做动画根delta移动。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Model3D/MModel.cs:1122 | 取Unity Animator并初始化封装MAnimator；Update:1974第1994行_mAtor.Update()。 |
| F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/AsyncObj/MAnimator.cs:840 | internal virtual void Update()，第845行只推进MAnimInfo.NormalizedTime。 |

在 RO MoonClient 的 Skill、Components、AsyncObj、Model3D、Entity 五个限定目录中搜索 OnAnimatorMove、deltaPosition、applyRootMotion、rootMotion、RootMotion，未命中正式C#动画根位移链。此结论限于检查的C#职责，不据此推断所有 FBX/Prefab 或外部 C++ 引擎默认行为。

RO object[] 回调载荷是参考事实，不适用于本项目强类型约束；本项目继续MSkillTimerArgs，函数名沿用。

## 本项目修正前职责

根目录为 D:/unitypro/LYFMMORGP，以下为本轮修改前读取行号，开发后可改变：

| 项目根目录相对路径与行号 | 现有行为与建议 |
| --- | --- |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:46 | Start在暖配置时调用FireEvents；冷配置靠Update反复TryResolve后再FireEvents。改为启动或资源就绪一次触发，带UUID和施法代次保护异步生命周期。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:57 | Update推进时钟、清声音尾项后，再注册、BindRootMotion、Drain两轨。移除后三类职责。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:131 | InnerProcessTimer在已到期轨道加入pending后立即Drain。保留到期回调作为派发入口；缺模型/挂点时改显式就绪重试，不能丢已到期事件或改未来事件时间。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:440 | TriggerAnim在调用统一基类动画入口后TryResolve并BindRootMotion，若配置迟到/模型未ready，目前靠Update补绑。归属管理应交模型或采集器且覆盖这些迟到情况。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:452 | OwnsRootMotion保护CurrentSkill、Model、_boundAnimator及真实readyclip路径；修正不得削弱旧实例/切段保护。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:469 | RootMotionScale只按真实动画秒选半开窗口的倍率，保留窗口规则即可，不是未来Fx/Audio每帧扫描。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs:247 | FinishLoad初始化真正的Animator节点并第274行RootMotion.Init；适合作为模型就绪恢复点。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/SkillRootMotionCollector.cs:72 | OnAnimatorMove取真实Animator.deltaPosition仅XZ，按RootMotionScale积累；此为本项目已有动画根运动实现。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs:399 | PlayerUpdate在第450行ConsumeDelta，合并输入与重力后第454行统一MoveCharacter，保留该碰撞权责。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs:714 | PlayAnim→CurrentSkill.TriggerAnim；OnStop:740→ReleaseAnimation:752释放当前动作，旧技能实例继续表现尾项；资源就绪通知要覆盖旧实例但不得重绑旧动作。 |

## 开发与审核必要关注

1. 暖资产Start一次注册；冷资产完成通知一次注册，并减已累计ElapsedSeconds补发到期事件，不能按加载后重新起算。
2. Timer回调直接处理声音/粒子；缺模型和挂点的已到期pending以模型/挂点ready通知恢复，同轨顺序及跨轨独立性保持，取消清空并移除所有等待登记。
3. 异步ready通知必须以UUID与_fireGeneration过滤；同UUID复用、同步0秒回调Reset/再Fire、旧缓存记录完成不能污染新施法。
4. 动画根采集保留Collector→MMove；归属必须由当前槽、模型/真实readyclip、配置ready共同确认。动作退出及Reset丢旧buffer，旧_skills不覆盖新技能。
5. 声音自然结束清理允许在Update做生命周期维护，这与扫描未来timeline或每帧Drain补播不同。
6. RO只覆盖本项目已运行声音/粒子函数职责；Charge数据制作能力与动画根运动区别在交付说明中明确，不冒称新Charge/服务器权威位移已经接入。

本轮修改记录见 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`，任务及验证状态见 `Docs/AI/CURRENT_TASK.md` 与 `Docs/AI/TEST_REPORT.md`。原声音/根运动说明见 `项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md`。临时证据 `_temp/skill-timeline-responsibility-20261011/reference/ro-source-evidence.md` 保留，由产品经理确认后清理。

## 参考源码原文片段

下面片段直接读取外部 RO 源码，行号对应取证时文件；仅用于说明职责，原弱类型与原语法不作为本项目新业务代码的规范。

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:217：

```csharp
        public override bool Update(float fDeltaT)
        {
            if (_rotateData != null && _skillComp.Entity.Model.Trans != null)
            {
                _skillComp.Entity.Rotation = Quaternion.LookRotation
                    (MCommonFunctions.HorizontalRotateVetor3(_skillComp.Entity.Model.Trans.forward,
                        _rotateData.AngularVelocity * fDeltaT, true));
            }
            return base.Update(fDeltaT);
        }
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:1016：

```csharp
        protected override void Start()
        {
            base.Start();

            _setCameraEffect = false;
            FireEvents();
        }
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:264：

```csharp
        protected virtual void FireEvents()//注册各种timeline回调
        {
            if (_data.ChargeList != null)
            {
                for (int i = 0; i < _data.ChargeList.Count; i++)
                {
                    var token = MTimerMgr.singleton.SetTimer(
                        _data.ChargeList[i].At * _timeScale, 
                        _onProcessTimer, EArtsSkillTimerCb.ECharge, 
                        _data.ChargeList[i]);
                    AddedTimerToken(token);
                }
            }
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:80：

```csharp
        protected override bool InnerProcessTimer(int id, object param)
        {
            if (!base.InnerProcessTimer(id, param))
            {
                EArtsSkillTimerCb fun = (EArtsSkillTimerCb)id;
                switch (fun)
                {
                    case EArtsSkillTimerCb.EFx:
                        fx(param);
                        return true;
                    case EArtsSkillTimerCb.EAudio:
                        audio(param);
                        return true;
                    case EArtsSkillTimerCb.EWarning:
                        //Warning(param);
                        return true;
                    case EArtsSkillTimerCb.ECharge:
                        charge(param);
                        return true;
                    case EArtsSkillTimerCb.EShake:
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:613：

```csharp
            _haveCharge = true;
            var displacementArgs = MEventPool<MDisplacementEventArgs>.GetEvent();
            displacementArgs.curveX = data.CurveX;
            displacementArgs.distanceX = distanceX;
            displacementArgs.curveZ = data.CurveZ;
            displacementArgs.distanceZ = distanceZ;
            displacementArgs.totalTime = data.End - data.At;
            displacementArgs.angle = MCommonFunctions.AngleToFloat(SkillTowardsTo);
            displacementArgs.skillUuid = GetUUID;
            MEventMgr.singleton.FireEvent(MEventType.MEvent_Displacement, Firer, displacementArgs);
        }
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Skill/MSkill.cs:225：

```csharp
        protected void OnProcessTimer(uint token, object[] args)
        {
            if (token > 0)
            {
                _tokens.Remove(token);
            }
            if(!MEntity.ValideEntity(Firer))
            {
                return;
            }
            //if (!Firer.IsLoaded)
            //{
            //    return;
            //}
            if (args.Length == 2)
            {
                InnerProcessTimer((int)args[0], args[1]);
            }
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Utility/MTimer.cs:176：

```csharp
        public void Update(float fDeltaT)
        {
            if (_isPause)
            {
                return;
            }
            _curTime = _elapsedTime + UnityEngine.Mathf.RoundToInt(fDeltaT * 1000);

            MProfiler.BeginSample("--MTimerMgr.singleton.Update");
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Utility/MTimer.cs:200：

```csharp
                    it = head.Begin();
                    var cell = it.data;
                    head.Delete(it);
                    cell.Fire();
                    returnBackToPool(cell);
                }

                _elapsedTime++;
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:259：

```csharp
        public override void Update(float fDeltaT)
        {
            if (!_displacementing)
            {
                return;
            }
            _duration += fDeltaT;
            float p = Math.Min(_duration / _totalTime, 1);
            if (type == ChargeType.Normal)
            {
                UnityEngine.Vector3 distance = new UnityEngine.Vector3(p * _distanceX, 0, p * _distanceZ);
                if (_curveX)
                {
                    distance.x = _curveX.Evaluate(p) * _distanceX;
                }
                if (_curveZ)
                {
                    distance.z = _curveZ.Evaluate(p) * _distanceZ;
                }
                UnityEngine.Vector3 dest = _startPos + MCommonFunctions.HorizontalRotateVetor3(distance, _angle, false), end;
```

F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Components/Common/MDisplacementComponent.cs:287：

```csharp
                {
                    StopDisplacement();
                    return;
                }

                //end.y = Mathf.Lerp(_startPos.y, _targetY, p);
                if (MAirWallMgr.singleton.CheckWall(Entity.Position, end))
                {
                    Entity.Position = end;
                }
                else
                {
                    kickBlocked = true;
                }

                if (kickBlocked || _totalTime <= _duration)
                {
                    StopDisplacement();
                    return;
```

## 本轮最终修正与验收

本章记录开发末稿和独立审核通过后的事实；前文“本项目修正前职责”表与RO原文保持为历史对照。主在本轮承认上轮职责迁移不完整，guard只能防重，不能代替从Update移出注册、表现补播和根绑定。

| 本项目最终入口（项目根目录相对路径） | 本轮最终职责 |
| --- | --- |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:109 | Update只base.Update与PruneAudio，后者回收播完声源，不注册或消费事件、不查资产、不绑根。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:53 | Start订阅可取消资源/模型Ready；暖或冷缓存终态一次FireEvents，计时按本次ElapsedSeconds差值补发。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:174 | MTimerMgr到期经OnProcessTimer/InnerProcessTimer直接消费；模型迟到pending只在PresentationReady恢复。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:458 | OnReset解除缓存和模型监听，原计时票据、UUID/施法代次、资源身份守卫保持，旧listener不能污染同UUID池实例。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs:489 | TriggerAnim登记根归属，不依赖Update补绑；实际clip/config/当前动作门控仍由OwnsRootMotion供Collector使用。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/SkillRuntimePresentationCache.cs:111 | NotifyResourcePackageReady在包取得时一次启动旧记录；暖终态同步、冷终态一次、失败空值通知，解除等待及旧记录身份保护保持。 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs:249 | 真实FinishLoad在模型/Animator/声音/根采集和外部回调完成后发布PresentationReady，逐个监听后核session，防旧会话重入。 |
| LYF_MMO/Assets/GM_Game/Scripts/Global.cs:99 | 取得YooPackage后一次通知缓存，原全局MTimerMgr唯一毫秒驱动保持，无Update轮询替代。 |

实际水平动画增量仍由SkillRootMotionCollector.OnAnimatorMove取得Animator.deltaPosition，仅XZ；MMoveComponent合并重力后统一碰撞提交，两份原业务代码本轮字节保持。没有把RO ECharge改造为本项目动画根运动，也未新增完整Charge或服务器位移结算。TimelineDurationSeconds独立制作范围、自然End保未来票据/pending/声尾、Interrupted立即清理、Q旧新施法及粒子独立租约保持。

开发57项通过，独立94/94通过（45项当前后动画生命周期重新验证＋49项真实MModel/Cache职责专项），不是上轮85项结论。267完整GM_Game源Rebuild 0错/37既有警告；26份完整生产运行源含真实MModel/Cache。单独驱动Timer能消费，到期pending由真实Cache/MModelReady补发无需技能Update；冷/暖/失败、取消/同UUID旧闭包、模型Load/Reset/reload重入和异常隔离、clip/config根门控、自然End后F45/F46与完整声尾、20秒长音、真正Interrupted均通过。

当前保护2788路径（历史2786清单补纳本就存在MTimerMgr源/meta），2784字节保持/4计划既有源变化，无新增/删除/未计划改变。四源开发manifest、独立冻结与当前SHA主支持重算一致；26运行源再核0差异，267编译输入独立核0差异。两独立报告PASS且主已读取，无剩余代码阻断；两个夹具失败仅_temp修正，失败与后续通过日志保留。

缺固定挂点保持pending，带齐层级的模型reloadReady补发且声音不重复已验；当前无正式动态挂点入口，不扩新轮询。正式Unity GUI/设备听感/PlayMode/真实网络/服务器/Bundle未验，本轮未检测MCP，不援引旧无实例结果。原生对象、Animator状态/delta及YooAsset完成通过薄边界交付，不能扩称正式场景体验。下一步原正式入口体验画面与声音，不扩大已通过的隔离测试。

最终证据：`_temp/skill-timeline-responsibility-20261011/review/independent-review.md`、`_temp/skill-timeline-responsibility-20261011/review/independent-test-report.md`、`_temp/skill-timeline-responsibility-20261011/review/runtime-final.log`、`_temp/skill-timeline-responsibility-20261011/review/GM_Game-build.log`、`_temp/skill-timeline-responsibility-20261011/review/independent-protection.json`、`_temp/skill-timeline-responsibility-20261011/review/source-consistency.json`、`_temp/skill-timeline-responsibility-20261011/review/verified-source-hashes.json`、`_temp/skill-timeline-responsibility-20261011/develop/final-manifest.json`、`_temp/skill-timeline-responsibility-20261011/root-manifest-check.json`。正式修改记录 `项目修改日志/2026-10-11-SKILL-TIMELINE-RESPONSIBILITY-CHANGELOG.md`，协作状态与验证见四份`Docs/AI`入口；临时资料保留，由产品经理确认后清理。
## 本轮RO直接结构精简与FireEvents入口恢复

唯一业务修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`。FireEvents仍protected virtual void，末稿117（前置116因新增参数中文注释增行）；Start53直接65调用。主链Start→FireEvents→SetTimer→OnProcessTimer→switch→fx/audio，两个for直接AddedTimerToken(SetTimer)。删除TryResolve间接层但完整资产/全轨校验集中FireEvents，删除票据/args/UUID重复局部检查；Ready短捕获代次交具名方法，fx/audio是原实际消费，不新建薄wrapper。仅保本项目强类型SO、冷cache/model就绪及UUID代次必要适配；Update仍仅base+Prune，根原TriggerAnim/Collector→MMove不变。其它业务、用户资产/场景/协议/meta/GUID保持。

本轮独立99/99＝45最新后动画重新执行＋49职责重新执行＋5新增Start直接FireEvents扩展点，不用旧94充数。267完整源Rebuild0错/37基线既有警告，26完整生产运行源当前SHA主辅助重算0差异、267编译输入独立0差异，一源开发/独立manifest/当前SHA一致。2788当前保护冻结中2787保持/1计划源变，无新增删或未计划。两独立报告PASS主已读，无代码阻断。

同口径净减：Start方法体有效29→7，注册/Ready/消费方法体含原TryResolve100→79，全文件有效301→281、物理588→559、复合条件112→96、分号169→158。分号含for头部，仅符号统计，不当精确独立语句数；开发含声明109→89不混口径。仅首次.NET9 BadImageFormat为环境异常，原命令重试通过，不称旧业务复现。未验正式Unity画面/设备听感/联机/PlayMode/Bundle，未新查MCP。

正式记录 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-SIMPLIFY-CHANGELOG.md`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。证据 `_temp/skill-timeline-ro-simplify-20261011/` 中review两报告/build/runtime/保护/source-consistency/structure-measure、develop/final-manifest.json、develop/simplification-metrics.json、root-manifest-check.json；临时保留由产品经理确认后清理。下一步原正式入口体验，不重跑已通过测试或自动发布资产。

## 最新纠正：到期指令与模型 GO 加载边界

本节替代前文技能启动时订阅 PresentationReady、维护 pending 以及缺挂点等待重载的方案；前文保留为历史过程。用户指出的 `_presentationModel`、`_modelReady` 和启动代次闭包确实没有遵循 RO 的具体操作入口。最新实现删除这些代码，Start 只调用 base.Start 与 FireEvents。FireEvents 仍为 protected virtual void，负责一次登记 MTimerMgr；冷配置终态返回该入口，模型加载不再重新登记时间线。

真实参考位于 `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/`：`Skill/MSkill.cs` 的 OnProcessTimer 移除 timer token、核验实体后调用 InnerProcessTimer；`Skill/SkillLogic/MArtSkill.cs` 的 switch 调用 fx/audio。RO 的 fx 在计时器已经交付该条数据后调用 Model.AddLoadGoCallback，暖 GO 同步执行并返回零，冷 GO 返回可取消票据，Stop 通过 RemoveLoadGoCallback 撤销。`Model3D/MModel.cs` 的 onGoLoadFinished 在挂点、碰撞初始化后交付回调，早于完整模型加载收尾，不能等同上一轮的全组件 PresentationReady 事件。

RO audio 直接使用 Model.PlayAudio/PlayAudioInstance：单次声音可能在模型未就绪时跳过，区间实例由模型音频层持有，不能声称声音也沿 RO FX 的 GO 回调。当前项目采用 Unity AudioSource，创建三维声源需要 Transform，同时已有冷模型不丢声音的契约，所以本项目 audio 到期后也按需申请一次 GO 回调；这是明确的音频适配。

本项目最新职责如下：

| 入口 | 职责 |
| --- | --- |
| MArtSkill.Start / FireEvents | 启动时一次注册；冷配置完成后按原锁定行及累计时钟注册，不订阅模型就绪。 |
| MSkill.OnProcessTimer / MArtSkill.InnerProcessTimer | 核验 UUID 与施法代次，分发这一条到期粒子或声音。 |
| MArtSkill.fx / audio | 模型暖时立即执行，模型冷时申请 AddLoadGoCallback，保存本条可取消票据；不扫描或消费其他事件。 |
| MModel.AddLoadGoCallback / RemoveLoadGoCallback | 持有尚未交付的 GO 操作；暖返回零，冷返回非零票据；取消与实际登记顺序独立于字典槽位。 |
| MModel.FinishLoad / Reset | 基础模型组件初始化并核验加载会话后交付一次操作；真正归池取消全部 GO 请求；同模型重新加载保留尚未交付请求。 |
| MSkill.TriggerAnim / MArtSkill.ReleaseAnimation | 登记或解除当前动作所有者，冷模型完成只恢复当前动画；特效和声音不由动画恢复入口消费。 |
| MArtSkill.Update | 只推进基类施法时钟、回收已结束声源。 |

GO 回调执行前检查施法代次及模型身份，归池后旧闭包不得操作同 UUID 的新施法。自然 End 释放动画所有者，保留未来计时器、已到期 GO 请求和完整音尾；Interrupted 立即注销等待并停止声音。粒子仍持独立资源租约。动画水平 delta 仍由 SkillRootMotionCollector 采集、MMoveComponent 合并重力走原碰撞链；RO ECharge 是配置曲线位移，不等于 Animator 根运动。

完整已加载模型缺固定挂点现在报告错误并跳过该粒子，不再无限等待重载，也不阻塞后续粒子或声音。冷模型仍等待基础 GO 初始化后正常执行。这是本轮明确的行为变化。

本轮正式修改及最终验证归档 `项目修改日志/2026-10-11-SKILL-TIMELINE-RO-CALLBACK-CHANGELOG.md`，协作验收见 `Docs/AI/TEST_REPORT.md`；真实参考原文保留在 `_temp/skill-timeline-ro-callback-20261011/reference/ro-callback-boundaries.md`。正式 Unity 画面、设备听感、网络及 Bundle 发布另有未验边界，不用隔离测试代替现场体验。

最终独立92/92（45当前生命周期＋47新GO职责）通过，267真实GM_Game源编译零错、37既有警告；主读取两独立报告并核开发3/独立3/运行26/编译267共299项SHA零差异。2788当前保护中2785保持、恰三计划源变化，无新增删除；中文注释及增改规范通过。字典取消后再登记导致回调倒序的实测失败已返工，以实际登记顺序表修复并覆盖整数回绕；无剩余代码阻断。证据 `_temp/skill-timeline-ro-callback-20261011/review/independent-review.md`、`independent-test-report.md`、`final-manifest-check.json` 与主 `root-manifest-check.json`。本轮不援引旧99项；26完整生产逻辑的Unity原生边界仍为薄夹具，正式现场体验待原入口验证。
