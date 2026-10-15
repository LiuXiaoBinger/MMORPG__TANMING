using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>RO普通技能表现载体，派发粒子、声音、程序位移及原滑行残影回调，按窗口接管水平根位移。</summary>
public class MArtSkill : MSkill
{
    /// <summary>本次表现轨道校验失败后为 true，直到归还；阻止每帧重复报错。</summary>
    private bool _runtimePresentationInvalid;

    /// <summary>本次锁定资产是否已完整注册时间轴，冷就绪后只允许注册一次。</summary>
    private bool _eventsRegistered;

    /// <summary>本代施法等待资源终态的可取消通知，捕获锁定 UUID 与代次，归还前解除。</summary>
    private Action<SkillRuntimePresentation> _presentationReady;

    /// <summary>RO 式已到期操作所在模型，用于真正取消时移除其尚未交付的 GO 回调。</summary>
    private MModel _loadGoModel;

    /// <summary>仅保存已到期但 GO 未加载的声音或粒子票据，模型交付后移除，取消时逐条注销。</summary>
    private readonly List<int> _skillFxTokens = new List<int>();

    /// <summary>本次施法正在播放的并行三维声音；真实取消和回池时停止，普通动作槽退出保留尾音。</summary>
    private readonly List<AudioSource> _audioList = new List<AudioSource>();

    /// <summary>本次施法是否曾成功开始程序位移，离槽或归还时只停止原身份。</summary>
    private bool _haveCharge;

    /// <summary>最近成功开始程序位移的原代次，Reset 自增代次后仍用于解除旧运动。</summary>
    private ulong _chargeGeneration;

    /// <summary>普通技能的 RO 运行类型，零表示 Art。</summary>
    public override int SkillType { get { return SkillLogicType.SkillTypeArt; } }

    /// <summary>自然结束只能在资产已注册、未来票据已交付、缺模型指令已派发且全部声音结束后回池。</summary>
    public override bool IsPresentationFinished
    {
        get
        {
            if (_runtimePresentationInvalid)
            {
                return true;
            }
            // 事件已注册且基类无未来票据，同时待交付特效与声音均清空才视为表现收尾。
            bool eventsRegisteredAndBaseFinished =
                _eventsRegistered && base.IsPresentationFinished;
            bool fxTokensEmpty = _skillFxTokens.Count == 0;
            bool audioListEmpty = _audioList.Count == 0;
            return eventsRegisteredAndBaseFinished && fxTokensEmpty && audioListEmpty;
        }
    }

    /// <summary>沿 RO 的 Start 入口只注册 timeline，到期表现由 OnProcessTimer 交付。</summary>
    protected override void Start()
    {
        base.Start();
        FireEvents();
    }

    /// <summary>只推进施法时钟并回收已经播完的声音；事件注册与派发全部由生命周期和计时回调触发。</summary>
    /// <param name="deltaSeconds">本帧经过的非负秒数，与全局计时器使用相同毫秒来源。</param>
    /// <returns>动作仍占槽、未来事件、冷模型请求或活动音尾仍需保持本实例时为真。</returns>
    public override bool Update(float deltaSeconds)
    {
        // 先解除本帧已经播完的声源，基类再根据动作槽、剩余表现和 RO 生命周期判断存活。
        PruneAudio();
        return base.Update(deltaSeconds);
    }

    /// <summary>RO 同名动作结束实现，使用本次 Fire 冻结的技能状态窗口，连击不会被制作尾段拖住。</summary>
    /// <returns>当前动作达到解锁时长时为真。</returns>
    public override bool IsEnd() { return IsSkillStateFinished; }

    /// <summary>原锁定表现行完成加载后回到 FireEvents，不重选连击段。</summary>
    /// <param name="presentation">加载终态资产，空值表示失败。</param>
    /// <param name="fireGeneration">订阅时的施法代次，取消及池复用后失效。</param>
    private void OnPresentationReady(SkillRuntimePresentation presentation, ulong fireGeneration)
    {
        if (_fireGeneration != fireGeneration)
        {
            return;
        }
        _presentationReady = null;
        if (presentation == null)
        {
            _runtimePresentationInvalid = true;
            return;
        }
        _runtimePresentation = presentation;
        FireEvents();
    }

    /// <summary>把本次粒子、声音、程序位移和原滑行残影采样一次登记到MTimerMgr。</summary>
    protected virtual void FireEvents() // 注册各种 timeline 回调。
    {
        // 已注册、已标记失效或已退出动作槽时不再重复登记；存在待加载通知也等待回调。
        bool alreadyRegistered = _eventsRegistered || _runtimePresentationInvalid;
        bool inactiveOrWaiting = !IsActive || _presentationReady != null;
        if (alreadyRegistered || inactiveOrWaiting)
        {
            return;
        }
        // 暖资产直接注册；冷资产保存一次通知，同 UUID 取消也由代次识别。
        ulong fireGeneration = _fireGeneration;
        if (_runtimePresentation == null)
        {
            _runtimePresentation = SkillRuntimePresentationCache.
                GetReady(_runtimePresentationId);
            if (_runtimePresentation == null)
            {
                // readyAsset 是原行加载终态，闭包只保存不可变代次。
                _presentationReady = readyAsset => OnPresentationReady(readyAsset, fireGeneration);
                SkillRuntimePresentationCache.AddReadyListener(_runtimePresentationId, _presentationReady);
                return;
            }
        }
        // 本代资产快照先整体校验，防止零秒同步回调 Reset 后循环解引用空字段。
        SkillRuntimePresentation presentation = _runtimePresentation;
        // 表现行身份与本次施法锁定的身份不一致即视为资产错位。
        bool presentationIdentityMismatch =
            presentation.PresentationId != _runtimePresentationId ||
            presentation.SkillId != SkillId ||
            presentation.AnimationLocation != _animationLocation;
        // 表现时长必须为正有限秒数，且与锁定时长偏差不超过 0.05 秒。
        bool presentationDurationMismatch =
            !IsFinite(presentation.DurationSeconds) || presentation.DurationSeconds <= 0f ||
            Mathf.Abs(presentation.DurationSeconds - _runtimePresentationDurationSeconds) > 0.05f;
        // 整条事件轨道二次校验，任何数据不可解释即拒绝派发。
        bool presentationRuntimeInvalid = !ValidateRuntimePresentation(presentation);
        if (presentationIdentityMismatch || presentationDurationMismatch ||
            presentationRuntimeInvalid)
        {
            _runtimePresentation = null;
            _runtimePresentationInvalid = true;
            Debug.LogError("技能编辑表现与本次施法配置不一致，停止派发表现。表现行=" + _runtimePresentationId);
            return;
        }
        _eventsRegistered = true;
        // Excel技能主表的类别：3表示滑行，按配置类别注册残影，不依赖具体技能ID。
        cfg.SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(SkillId);
        
        if (skillInfo != null && skillInfo.Type == 3)
        {
            // index为30Hz残影采样的零起始序号；冷登记只保留尚未经过的点，不追补旧姿态。
            for (int index = 0; index < Mathf.
                     CeilToInt(_runtimePresentationDurationSeconds * 30f); index++)
            {
                // 相对施法原点的采样秒数，不追补过期姿态，避免模型就绪时集中生成残影。
                float sampleSeconds = index / 30f;
                if (sampleSeconds < TimelineElapsedSeconds)
                {
                    continue;
                }
                AddedTimerToken(MTimerMgr.singleton.SetTimer(
                    sampleSeconds - TimelineElapsedSeconds,
                    _onProcessTimer, new MSkillTimerArgs(SkillUuid, 
                        (int)EArtsSkillTimerCb.EGhost, fireGeneration)));
            }
        }
        // index 为本段粒子的稳定导出顺序，同时间按此顺序派发。
        for (int index = 0; index < presentation.FxEvents.Count; index++)
        {
            // 本段粒子指令，触发秒数减已累计时钟以补发冷加载过期事件。
            SkillRuntimeFxEvent fxEvent = presentation.FxEvents[index];
            AddedTimerToken(MTimerMgr.singleton.SetTimer
            (fxEvent.TimeSeconds - ElapsedSeconds,
                _onProcessTimer, new MSkillTimerArgs(SkillUuid,
                    (int)EArtsSkillTimerCb.EFx, fxEvent, fireGeneration)));
            // 只有零秒同步回调可能重入，返回票据为零；Reset 必增代次阻止续注册旧批次。
            if (_fireGeneration != fireGeneration)
            {
                return;
            }
        }
        // index 为本段声音的稳定导出顺序，不受粒子挂点等待阻塞。
        for (int index = 0; index < presentation.AudioEvents.Count; index++)
        {
            // 本段单次声音，固定音量与完整音频尾部仍使用已验证资产。
            SkillRuntimeAudioEvent audioEvent = presentation.AudioEvents[index];
            AddedTimerToken(MTimerMgr.singleton.SetTimer(audioEvent.TimeSeconds - ElapsedSeconds,
                _onProcessTimer, new MSkillTimerArgs(SkillUuid, 
                    (int)EArtsSkillTimerCb.EAudio, audioEvent, fireGeneration)));
            if (_fireGeneration != fireGeneration)
            {
                return;
            }
        }
        // index 保留程序段的开始秒顺序；一次大 tick 多段由独立驱动保留前段末差量。
        for (int index = 0; presentation.ChargeList != null && index < presentation.ChargeList.Count; index++)
        {
            // 当前原 RO 同名位移数据，开始和结束均相对冻结的全局计时原点。
            MSkillChargeData chargeData = presentation.ChargeList[index];
            // 冷资产到达时建立当前进度基线，不把历史运动补成瞬移；未来回调使用负一标记。
            float initialElapsedSeconds = -1f;
            if (TimelineElapsedSeconds > chargeData.At)
            {
                initialElapsedSeconds = TimelineElapsedSeconds - chargeData.At;
            }
            AddedTimerToken(MTimerMgr.singleton.SetTimer(chargeData.At - TimelineElapsedSeconds,
                _onProcessTimer, new MSkillTimerArgs(SkillUuid, (int)EArtsSkillTimerCb.ECharge,
                    chargeData, fireGeneration, initialElapsedSeconds)));
            if (_fireGeneration != fireGeneration)
            {
                return;
            }
        }
    }

    /// <summary>沿 RO 同名回调分发到期事件，模型未就绪时保留独立轨道的等待顺序。</summary>
    /// <param name="id">RO粒子/声音/位移编号，或本项目原滑行残影采样编号EGhost。</param>
    /// <param name="param">统一回调已经验证施法 UUID 的强类型事件载荷。</param>
    /// <returns>本类接管粒子、声音、程序位移或原滑行残影采样时返回true。</returns>
    protected override bool InnerProcessTimer(int id, MSkillTimerArgs param)
    {
        if (base.InnerProcessTimer(id, param))
        {
            return true;
        }
        switch ((EArtsSkillTimerCb)id)
        {
            case EArtsSkillTimerCb.EFx when param.FxEvent != null:
                fx(param.FxEvent);
                return true;
            case EArtsSkillTimerCb.ECharge when param.ChargeData != null:
                charge(param.ChargeData, param.ChargeInitialElapsedSeconds);
                return true;
            case EArtsSkillTimerCb.EAudio when param.AudioEvent != null:
                audio(param.AudioEvent);
                return true;
            case EArtsSkillTimerCb.EGhost:
                ghost();
                return true;
        }
        return false;
    }

    /// <summary>只为当前滑行动作生成原白色残影；模型未就绪或动作退槽时直接跳过，不重试。</summary>
    private void ghost()
    {
        // 逐级确认施法者动作槽、动作状态与模型原生对象就绪，缺一不能生成残影。
        if (_firer == null) return;
        if (_firer.Skill == null) return;
        if (_firer.Skill.CurrentSkill != this) return;
        if (IsSkillStateFinished) return;
        if (_firer.Model == null) return;
        if (!_firer.Model.IsLoaded) return;
        if (_firer.Model.GameObject == null) return;
        if (_firer.Model.GameObject.NativeObject == null) return;
        // 残影挂在模型原根节点，沿用现成组件和原参数，生成的网格快照在0.2秒内自行淡出。
        GhostEffect effect = _firer.Model.GameObject.NativeObject.GetComponent<GhostEffect>();
        if (effect == null)
        {
            effect = _firer.Model.GameObject.NativeObject.AddComponent<GhostEffect>();
        }
        effect.CreateGhostEffectObject(Color.white, 0.2f, 0.2f, 0.2f, 0.2f);
    }

    /// <summary>沿 RO charge 入口将已到期的程序运动交独立驱动，技能不逐帧扫描或直接改坐标。</summary>
    /// <param name="data">本次锁定的前向累计曲线指令。</param>
    /// <param name="initialElapsedSeconds">冷注册时已经经过的段内秒数，正常计时回调为负一。</param>
    private void charge(MSkillChargeData data, float initialElapsedSeconds)
    {
        if (_firer == null || _firer.DisplacementComp == null ||
            !OwnsDisplacement(_firer, SkillUuid, _fireGeneration))
        {
            return;
        }
        // 本段方向冻结于 Fire；atan2 与 RO angle 一样使用度，不能当弧度直接旋转。
        float angle = Mathf.Atan2(SkillTowardsTo.x, SkillTowardsTo.z) * Mathf.Rad2Deg;
        // 直接交独立驱动的强类型开始参数；没有 object[] 或通用事件载荷。
        MDisplacementEventArgs args = new MDisplacementEventArgs
        {
            Source = this, skillUuid = SkillUuid, fireGeneration = _fireGeneration, Data = data,
            angle = angle, startTimeMilliseconds = _startedTimerMilliseconds + Mathf.RoundToInt(data.At * 1000f),
            initialElapsedSeconds = initialElapsedSeconds
        };
        if (_firer.DisplacementComp.onStart(args))
        {
            _haveCharge = true;
            _chargeGeneration = _fireGeneration;
        }
    }

    /// <summary>停止自身程序位移，当前动作自然离槽和真正打断均不拉到理论终点。</summary>
    private void StopCharge()
    {
        if (_haveCharge && _firer != null && _firer.DisplacementComp != null)
        {
            // 使用开始时的原代次；Reset 已推进基类代次，不能用新值取消旧运动。
            MDisplacementStopEventArgs args = new MDisplacementStopEventArgs
            {
                Source = this, skillUuid = SkillUuid, fireGeneration = _chargeGeneration
            };
            _firer.DisplacementComp.onStop(args);
        }
        _haveCharge = false;
        _chargeGeneration = 0;
    }

    /// <summary>计时器到期后派发声音；Unity 声源依赖模型 Transform，冷模型用同一 GO 回调边界承接。</summary>
    /// <param name="data">本次锁定的完整单次声音指令。</param>
    private void audio(SkillRuntimeAudioEvent data)
    {
        // 本次到期操作的原模型，暖模型同步完成，不在技能 Start 注册就绪事件。
        MModel model = _firer.Model;
        if (model == null)
        {
            return;
        }
        _loadGoModel = model;
        // GO 回调存活期内施法必须仍是这一代，同 UUID 再 Fire 也不能补旧声音。
        ulong fireGeneration = _fireGeneration;
        // 冷 GO 返回可取消票据，暖 GO 同步执行前仍为零，不加入等待列表。
        int token = 0;
        // go 是到期操作等待的完整原生模型；闭包不扫描或重选时间轴事件。
        token = model.AddLoadGoCallback(go =>
        {
            if (_fireGeneration != fireGeneration || !ReferenceEquals(_firer.Model, model))
            {
                return;
            }
            _skillFxTokens.Remove(token);
            // 本条已到期声音的三维实例，完整音尾由本施法持有到停止。
            AudioSource source = SoundComponent.PlayAudioInstance(model, data);
            if (source != null)
            {
                _audioList.Add(source);
            }
        });
        if (token != 0)
        {
            _skillFxTokens.Add(token);
        }
    }

    /// <summary>沿 RO 的 fx 入口只为这一条到期粒子申请 GO 回调，模型暖时同步播放。</summary>
    /// <param name="data">本次计时器已经交付的粒子指令。</param>
    private void fx(SkillRuntimeFxEvent data)
    {
        // 原施法模型，生命周期内不替换，实体归还前会先取消技能票据。
        MModel model = _firer.Model;
        if (model == null)
        {
            return;
        }
        _loadGoModel = model;
        // 捕获同池实例施法代次，模型加载期间 Reset/Fire 会使此指令失效。
        ulong fireGeneration = _fireGeneration;
        // 只记录冷模型的非零票据；暖模型回调同步交付，保持 RO 返回零语义。
        int token = 0;
        // go 是本次操作需要的原生模型，挂点与基本组件已在模型层完成初始化。
        token = model.AddLoadGoCallback(go =>
        {
            if (_fireGeneration == fireGeneration && ReferenceEquals(_firer.Model, model))
            {
                _skillFxTokens.Remove(token);
                PlayRuntimeEffect(data);
            }
        });
        if (token != 0)
        {
            _skillFxTokens.Add(token);
        }
    }

    /// <summary>在派发前检查完整粒子、声音和根位移轨道，任一数据无效即停止本次编辑表现。</summary>
    /// <param name="presentation">本次 Fire 锁定的编辑器运行资产。</param>
    /// <returns>全部事件可解释且按时间排序时返回 true。</returns>
    private static bool ValidateRuntimePresentation(SkillRuntimePresentation presentation)
    {
        // 三类事件列表必须齐全，缺一不能校验派发。
        bool presentationEventsMissing = presentation.FxEvents == null ||
            presentation.AudioEvents == null || presentation.RootMotionSegments == null;
        bool presentationDurationInvalid =
            !IsFinite(presentation.EffectiveTimelineDurationSeconds) ||
            presentation.EffectiveTimelineDurationSeconds < presentation.DurationSeconds;
        if (presentationEventsMissing || presentationDurationInvalid)
        {
            return false;
        }
        // 上一事件触发时间，单位为秒；相同时间允许按原序号连续派发。
        float previousTime = 0f;
        // 前一事件的原序号；同帧事件按照导出时的稳定顺序派发。
        int previousSourceIndex = -1;
        // index 为运行资产事件列表的零起始下标，校验整条轨道后才允许派发。
        for (int index = 0; index < presentation.FxEvents.Count; index++)
        {
            // 当前粒子事件的运行数据，由导出器写入并在这里二次核验。
            SkillRuntimeFxEvent fxEvent = presentation.FxEvents[index];
            // 粒子事件与粒子资源必须存在，后续字段读取均以此为前提。
            if (fxEvent == null || fxEvent.Particle == null) return false;
            bool fxTimeInvalid = !IsFinite(fxEvent.TimeSeconds) ||
                fxEvent.TimeSeconds < previousTime ||
                fxEvent.TimeSeconds > presentation.EffectiveTimelineDurationSeconds;
            bool fxIndexInvalid = fxEvent.SourceEventIndex < 0 || fxEvent.Frame < 0;
            // 同时间粒子按原序号递增，重复顺序视为非法。
            bool fxOrderInvalid = fxEvent.TimeSeconds == previousTime &&
                fxEvent.SourceEventIndex < previousSourceIndex;
            bool fxForwardInvalid = !IsFinite(fxEvent.ForwardDistance) ||
                fxEvent.ForwardDistance < 0f;
            bool fxPositionInvalid = !IsFinite(fxEvent.LocalPosition.x) ||
                !IsFinite(fxEvent.LocalPosition.y) ||
                !IsFinite(fxEvent.LocalPosition.z);
            bool fxRotationInvalid = !IsFinite(fxEvent.LocalEulerAngles.x) ||
                !IsFinite(fxEvent.LocalEulerAngles.y) ||
                !IsFinite(fxEvent.LocalEulerAngles.z);
            bool fxScaleFiniteInvalid = !IsFinite(fxEvent.LocalScale.x) ||
                !IsFinite(fxEvent.LocalScale.y) ||
                !IsFinite(fxEvent.LocalScale.z);
            bool fxScalePositiveInvalid = fxEvent.LocalScale.x <= 0f ||
                fxEvent.LocalScale.y <= 0f || fxEvent.LocalScale.z <= 0f;
            // 命名挂点必须给出挂点路径。
            bool fxSocketInvalid = fxEvent.PlacementKind == SkillFxPlacementKind.NamedSocket &&
                string.IsNullOrEmpty(fxEvent.SocketPath);
            bool fxPlacementRangeInvalid =
                (int)fxEvent.PlacementKind < (int)SkillFxPlacementKind.ActorRoot ||
                (int)fxEvent.PlacementKind > (int)SkillFxPlacementKind.WorldAbsolute;
            bool fxTimeBad = fxTimeInvalid || fxIndexInvalid || fxOrderInvalid;
            bool fxVectorBad = fxForwardInvalid || fxPositionInvalid || fxRotationInvalid;
            bool fxScaleBad = fxScaleFiniteInvalid || fxScalePositiveInvalid || fxSocketInvalid;
            if (fxTimeBad || fxVectorBad || fxScaleBad)
            {
                return false;
            }
            if (fxPlacementRangeInvalid)
            {
                return false;
            }
            previousTime = fxEvent.TimeSeconds;
            previousSourceIndex = fxEvent.SourceEventIndex;
        }
        previousTime = 0f;
        previousSourceIndex = -1;
        // index为本次声音列表的零起始下标，全部事件合法后才派发任一声音。
        for (int index = 0; index < presentation.AudioEvents.Count; index++)
        {
            // 当前单次声音，触发秒数属于制作时间轴；完整声音尾音可超过动画末尾。
            SkillRuntimeAudioEvent audioEvent = presentation.AudioEvents[index];
            // 声音事件与音频片段必须存在，缺一不能派发。
            if (audioEvent == null || audioEvent.Clip == null) return false;
            bool audioBasicInvalid = audioEvent.Frame < 0 || audioEvent.SourceEventIndex < 0;
            bool audioTimeInvalid = !IsFinite(audioEvent.TimeSeconds) ||
                audioEvent.TimeSeconds < previousTime ||
                audioEvent.TimeSeconds > presentation.EffectiveTimelineDurationSeconds;
            // 同时间声音按原序号递增，重复顺序视为非法。
            bool audioOrderInvalid = audioEvent.TimeSeconds == previousTime &&
                audioEvent.SourceEventIndex < previousSourceIndex;
            bool audioVolumeInvalid = !IsFinite(audioEvent.Volume) ||
                audioEvent.Volume < 0f || audioEvent.Volume > 1f;
            bool audioTimeBad = audioBasicInvalid || audioTimeInvalid || audioOrderInvalid;
            if (audioTimeBad || audioVolumeInvalid)
            {
                return false;
            }
            previousTime = audioEvent.TimeSeconds;
            previousSourceIndex = audioEvent.SourceEventIndex;
        }
        previousTime = 0f;
        // index为非重叠水平根位移窗口的零起始下标。
        for (int index = 0; index < presentation.RootMotionSegments.Count; index++)
        {
            // 半开窗口使用原动画秒数，不把Clip RootT曲线直接当世界米。
            SkillRuntimeRootMotionSegment segment = presentation.RootMotionSegments[index];
            // 半开窗口数据必须存在且起止秒数有限、不早于上一段。
            if (segment == null) return false;
            bool segmentTimeInvalid = !IsFinite(segment.StartSeconds) ||
                !IsFinite(segment.EndSeconds) || segment.StartSeconds < previousTime;
            bool segmentRangeInvalid = segment.EndSeconds <= segment.StartSeconds ||
                segment.EndSeconds > presentation.DurationSeconds;
            bool segmentScaleInvalid = !IsFinite(segment.Scale) || segment.Scale < 0f;
            if (segmentTimeInvalid || segmentRangeInvalid || segmentScaleInvalid)
            {
                return false;
            }
            previousTime = segment.EndSeconds;
        }
        previousTime = 0f;
        // index 按程序段开始秒顺序校验，不允许同段重复推动或与动画根窗口叠加。
        for (int index = 0; presentation.ChargeList != null && index < presentation.ChargeList.Count; index++)
        {
            // 当前前向程序段，RO 尚未接入的横移和追目标字段必须在 IsValid 中拒绝。
            MSkillChargeData chargeData = presentation.ChargeList[index];
            // 段数据存在且通过自身范围校验，且开始时间不早于上一段。
            if (chargeData == null) return false;
            bool chargeInvalid = !chargeData.IsValid(presentation.DurationSeconds) ||
                chargeData.At < previousTime;
            // 非首段开始时间不得与前一段相同，index>0 守卫下标前移。
            bool duplicateStart = index > 0 &&
                chargeData.At == presentation.ChargeList[index - 1].At;
            if (chargeInvalid || duplicateStart)
            {
                return false;
            }
            previousTime = chargeData.End;
            // rootIndex 遍历原动画水平窗口，点事件落在窗口内也属于双驱动冲突。
            for (int rootIndex = 0; rootIndex < presentation.RootMotionSegments.Count; rootIndex++)
            {
                // 当前动画半开区间；其结束时刻可以作为下一个程序段的开始。
                SkillRuntimeRootMotionSegment root = presentation.RootMotionSegments[rootIndex];
                // 程序段起点落在动画窗口结束之前，才可能与窗口发生重叠。
                bool startsBeforeWindowEnds = chargeData.At < root.EndSeconds;
                // 程序段终点越过窗口起点，或瞬时段起点落在窗口起点上，均构成重叠。
                bool overlapsWindow = chargeData.End > root.StartSeconds ||
                    (chargeData.At == chargeData.End && chargeData.At >= root.StartSeconds);
                if (startsBeforeWindowEnds && overlapsWindow)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>检查粒子时间和坐标标量可参与有限范围内的运算。</summary>
    /// <param name="value">待检查的秒数、距离或缩放分量。</param>
    /// <returns>既非无穷也非非数时返回 true。</returns>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>用单次施法锁定的资源生成一条粒子，并为其保留到销毁时的资产租约。</summary>
    /// <param name="fxEvent">需要立即执行的粒子事件。</param>
    /// <returns>粒子已成功生成返回 true；完整模型仍缺挂点时报告配置错误并返回 false，不阻塞后续指令。</returns>
    private bool PlayRuntimeEffect(SkillRuntimeFxEvent fxEvent)
    {
        // 逐级确认施法者模型、原生 GameObject 与 Transform 均就绪，缺一不能挂粒子。
        if (_firer == null) return false;
        if (_firer.Model == null) return false;
        if (_firer.Model.GameObject == null) return false;
        if (_firer.Model.GameObject.Transform == null) return false;
        // 当前实体模型根节点；角色和挂点模式均基于此节点定位。
        Transform roleTransform = _firer.Model.GameObject.Transform;
        // 本次粒子的父节点，世界定位模式保持为空。
        Transform parentTransform = null;
        if (fxEvent.PlacementKind == SkillFxPlacementKind.ActorRoot)
        {
            parentTransform = roleTransform;
        }
        else if (fxEvent.PlacementKind == SkillFxPlacementKind.NamedSocket)
        {
            parentTransform = SkillFxSocketResolver.Resolve(roleTransform, fxEvent.SocketPath);
            if (parentTransform == null)
            {
                Debug.LogError("技能特效挂点缺失：" + fxEvent.SocketPath +
                    "，表现行=" + _runtimePresentation.PresentationId);
                return false;
            }
        }
        // 粒子实例可能长于技能核心，必须在实例销毁前独立保持源资产依赖。
        int presentationId = _runtimePresentation.PresentationId;
        // 本次特效克隆体及其子粒子，由 Unity 在播放尾段完成后销毁。
        ParticleSystem effect = UnityEngine.Object.Instantiate(fxEvent.Particle);
        if (effect == null)
        {
            return false;
        }
        // 技能粒子实例必须活跃，确保播放和销毁回调都进入 Unity 生命周期。
        effect.gameObject.SetActive(true);
        // 资源租约挂在克隆体上，场景退出时销毁也会正常释放。
        SkillRuntimeFxLifetime lifetime =
            effect.gameObject.AddComponent<SkillRuntimeFxLifetime>();
        lifetime.Bind(presentationId);
        if (parentTransform != null)
        {
            effect.transform.SetParent(parentTransform, false);
            effect.transform.localPosition = fxEvent.LocalPosition;
            effect.transform.localEulerAngles = fxEvent.LocalEulerAngles;
        }
        else
        {
            // 世界定位的基础落点，TargetTop 无目标时使用显式前方距离。
            Vector3 worldPosition = fxEvent.LocalPosition;
            if (fxEvent.PlacementKind == SkillFxPlacementKind.CasterForwardWorld)
            {
                worldPosition += roleTransform.position +
                    roleTransform.forward * fxEvent.ForwardDistance;
            }
            else if (fxEvent.PlacementKind == SkillFxPlacementKind.TargetTop)
            {
                worldPosition += roleTransform.position +
                    roleTransform.forward * fxEvent.ForwardDistance;
                if (TargetUid != 0 && Global.Instance != null &&
                    Global.Instance.SceneWorld != null)
                {
                    // 有效目标的模型高度与实体位置共同确定头顶世界落点。
                    ROObject targetObject;
                    if (Global.Instance.SceneWorld.TryGetEntity(TargetUid,
                        out targetObject))
                    {
                        // 当前目标角色实体，非角色对象不改动兜底落点。
                        MEntity target = targetObject as MEntity;
                        if (target != null && target.Model != null)
                        {
                            worldPosition = target.Position +
                                Vector3.up * target.Height + fxEvent.LocalPosition;
                        }
                    }
                }
            }
            effect.transform.position = worldPosition;
            if (fxEvent.PlacementKind == SkillFxPlacementKind.CasterForwardWorld)
            {
                // 对齐旧 skill03_2：向前六米的世界粒子随当前角色水平朝向旋转。
                effect.transform.eulerAngles = new Vector3(fxEvent.LocalEulerAngles.x,
                    roleTransform.eulerAngles.y, roleTransform.eulerAngles.z);
            }
            else
            {
                effect.transform.eulerAngles = fxEvent.LocalEulerAngles;
            }
        }
        effect.transform.localScale = fxEvent.LocalScale;
        effect.Play(true);
        return true;
    }

    /// <summary>归还时解除根位移并停止声音，清除轨道进度；粒子仍由独立租约保持到尾迹结束。</summary>
    protected override void OnReset()
    {
        SkillRuntimePresentationCache.RemoveReadyListener(_runtimePresentationId, _presentationReady);
        _presentationReady = null;
        if (_loadGoModel != null)
        {
            // index 覆盖本次已到期但尚未加载的 GO 票据，取消不会触及其他施法。
            for (int index = 0; index < _skillFxTokens.Count; index++)
            {
                _loadGoModel.RemoveLoadGoCallback(_skillFxTokens[index]);
            }
        }
        _skillFxTokens.Clear();
        _loadGoModel = null;
        ReleaseAnimation();
        StopAudio();
        _runtimePresentationInvalid = false;
        _eventsRegistered = false;
    }

    /// <summary>退出动作槽时解除水平根位移并归还动画租约，声音和未来事件继续收尾，完成后可自然回池。</summary>
    public override void ReleaseAnimation()
    {
        StopCharge();
        ReleaseRootMotion();
        _animationReleased = true;
        if (_animationModel != null)
        {
            _animationModel.ReleaseSkillAnimation(this);
            _animationModel = null;
        }
        if (_boundAnimator != null)
        {
            _boundAnimator.ReleaseSkillAnimAfterExit(_animationLocation);
            _boundAnimator = null;
        }
    }

    /// <summary>统一动画入口完成剪辑绑定后接管声明窗口内的真实水平根运动。</summary>
    public override void TriggerAnim()
    {
        base.TriggerAnim();
        // 动画安装前即可登记当前所有者；真实采集由 OwnsRootMotion 的资源与剪辑就绪条件门控。
        if (!IsActive) return;
        if (_animationReleased) return;
        if (_firer == null) return;
        if (_firer.Skill == null) return;
        if (!ReferenceEquals(_firer.Skill.CurrentSkill, this)) return;
        if (_firer.Model == null) return;
        if (_firer.Model.RootMotion == null) return;
        if (_boundAnimator == null) return;
        if (!ReferenceEquals(_boundAnimator, _firer.Model.Ator)) return;
        _firer.Model.RootMotion.Bind(this);
    }

    /// <summary>确认该实例仍占用当前模型动作槽，防止旧技能列表和准备中剪辑产生位移。</summary>
    /// <param name="model">请求消费位移的当前模型。</param>
    /// <returns>当前技能、动画和模型全部属于本次施法时为true。</returns>
    public bool OwnsRootMotion(MModel model)
    {
        // 本代施法仍占用动作槽且表现资产已就绪时才核对模型与动画器。
        if (!IsActive) return false;
        if (_animationReleased) return false;
        if (_runtimePresentation == null) return false;
        if (_firer == null) return false;
        if (_firer.Skill == null) return false;
        if (!ReferenceEquals(_firer.Skill.CurrentSkill, this)) return false;
        if (!ReferenceEquals(_firer.Model, model)) return false;
        if (model == null) return false;
        if (model.Ator == null) return false;
        if (!ReferenceEquals(_boundAnimator, model.Ator)) return false;
        // 已安装的Skill槽必须属于本段，异步准备下一剪辑时停止旧段delta。
        MAnimator.MAnimInfo animation = model.Ator.GetAnimInfo(MAnimator.STATE_SKILL);
        return animation != null && animation.IsClipReady && animation.ReadyClipPath == _animationLocation;
    }

    /// <summary>按真实原动画时钟选择水平位移倍率，窗口采用含起点不含终点的语义。</summary>
    /// <param name="seconds">当前原生动画的已播放秒数。</param>
    /// <returns>当前窗口倍率，窗口外或无效时钟为零。</returns>
    public float RootMotionScale(float seconds)
    {
        if (_runtimePresentation == null || !IsFinite(seconds))
        {
            return 0f;
        }
        // index为非重叠窗口的零起始下标。
        for (int index = 0; index < _runtimePresentation.RootMotionSegments.Count; index++)
        {
            // 当前水平根位移窗口，结束时刻属于后续窗口而不是当前窗口。
            SkillRuntimeRootMotionSegment segment = _runtimePresentation.RootMotionSegments[index];
            if (seconds >= segment.StartSeconds && seconds < segment.EndSeconds)
            {
                return segment.Scale;
            }
        }
        return 0f;
    }

    /// <summary>离开动作槽时解除自身的根位移，不影响下一段已完成的绑定。</summary>
    private void ReleaseRootMotion()
    {
        if (_firer != null && _firer.Model != null && _firer.Model.RootMotion != null)
        {
            _firer.Model.RootMotion.Release(this);
        }
    }

    /// <summary>清理已播完声音，限制并行声源的模型子节点数量。</summary>
    private void PruneAudio()
    {
        // index从尾到头遍历，安全移除播放结束或已销毁的实例。
        for (int index = _audioList.Count - 1; index >= 0; index--)
        {
            if (_audioList[index] == null || !_audioList[index].isPlaying)
            {
                SoundComponent.StopSource(_audioList[index]);
                _audioList.RemoveAt(index);
            }
        }
    }

    /// <summary>技能取消或归还时先停止声音，再由基类解除核心和表现资产依赖。</summary>
    private void StopAudio()
    {
        // index覆盖所有并行声音实例，不能只停止最后一个声音。
        for (int index = _audioList.Count - 1; index >= 0; index--)
        {
            SoundComponent.StopSource(_audioList[index]);
        }
        _audioList.Clear();
    }

    /// <summary>普通技能没有组合子节点，按 RO 的普通技能行为返回自身。</summary>
    /// <param name="skillNum">调用方请求的组合内子技能序号，普通技能不使用。</param>
    /// <returns>当前普通技能实例。</returns>
    public override MSkill GetSkill(int skillNum)
    {
        return this;
    }

    /// <summary>清理结束后将普通技能实例归还其具体类型对象池。</summary>
    public override void Recycle()
    {
        MCommonObjectPool<MArtSkill>.Release(this);
    }

    /// <summary>RO粒子、声音、位移回调身份，以及本项目恢复的滑行残影采样编号。</summary>
    protected enum EArtsSkillTimerCb
    {
        /// <summary>RO 原值 10，执行一条已导出的单次粒子指令。</summary>
        EFx = 10,
        /// <summary>RO 原值 11，执行一条已导出的单次三维声音指令。</summary>
        EAudio = 11,
        /// <summary>RO 原值十三，到期交付一条程序水平位移指令。</summary>
        ECharge = 13,
        /// <summary>本项目追加的滑行网格残影采样编号，非RO原事件编号。</summary>
        EGhost = 100
    }
}
