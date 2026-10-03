using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using MMoRpgCommon.SceneEntity;
    /// <summary>
    /// 动作状态组件抽象基类，挂载于具备动作状态机的实体（MEntity）上。
    /// 封装一个动作状态的生命周期：获得许可(OnGetPermission)、开始(OnStart)、逐帧更新(OnUpdate)、
    /// 正常结束(Finish/OnFinished)、强制停止(Stop/OnStop)以及结束后清理(AfterFinish)。
    /// 具体动作（移动、攻击、攀爬、采集、跳舞等）均继承本类，覆写相应钩子完成各自状态逻辑。
    /// </summary>
    public abstract class MActionComponent : EntityComponentBase, IMStateTransform
    {
        /// <summary>动作状态归入同步类别，但由状态机专用池独立管理。</summary>
        public override EntityComponentType ComponentType
        {
            get { return EntityComponentType.Sync; }
        }
        /// <summary>获取或设置该状态是否已废弃（废弃后状态机不再选用）。</summary>
        public bool Deprecated { get; set; }
        /// <summary>获取是否为“预表现”状态（正式生效前的前置表现阶段），默认 false。</summary>
        public virtual bool IsPreperformance { get { return false; } }

        /// <summary>当前实体所处的状态定义，默认为默认状态。</summary>
        protected StateDefine _selfState = StateDefine.kStateDefault;
        /// <summary>当前状态是否已结束（true 表示空闲/未激活）。</summary>
        protected bool _isFinished = true;
        /// <summary>状态转移令牌，用于标识本次状态切换的唯一编号。</summary>
        protected long _token = 0;
        protected bool _isOverrideSuccess = true;  // 是否成功OverrideAnim，失败调用idle动作
        protected IEventArg _evt; //切状态的时候暂存的evt数据
        /// <summary>动作事件处理回调，注册后用于监听状态转移消息。</summary>
        protected MEventHandler _onActionEvent;
        private readonly MEventHandler _onFinishEvent;

        /// <summary>获取该状态是否需要与服务端做预测同步，默认 true。TODO: 后续接入 syncMode 配置。</summary>
        public virtual bool SyncPredicted { get { return true; } }
        /// <summary>获取当前组件对应的状态定义。</summary>
        public StateDefine SelfState { get { return _selfState; } }
        /// <summary>获取当前动作状态是否已结束。</summary>
        public bool IsFinished { get { return _isFinished; } }
        /// <summary>获取动作播放速度倍率，默认 1 倍速。</summary>
        public virtual float Speed { get { return 1f; } }
        /// <summary>获取当前状态下实体所在的碰撞层，默认使用实体默认层。</summary>
        public virtual int CollisionLayer { get { return Entity.DefaultLayer; } }
        /// <summary>获取是否使用运动曲线驱动位移，默认 false。</summary>
        public virtual bool IsUsingCurve { get { return false; } }
        /// <summary>获取该状态对应的动画机状态名（子类必须实现）。</summary>
        public abstract string StateName { get; }

        /// <summary>构造函数，初始化动作事件处理器。</summary>
        public MActionComponent(ROObject owner) : base(owner)
        {
            _onActionEvent = new MEventHandler(OnActionEvent);
            _onFinishEvent = new MEventHandler(OnFinishEvent);
        }

        /// <summary>
        /// 组件初始化入口，注册具体动作状态关心的事件。
        /// 由组件管理器在附加到宿主实体后调用。
        /// </summary>
        public override void Initialize()
        {
            RegisterActionEvents();
        }

        /// <summary>由具体动作登记具名事件入口，无载荷事件统一传递 null。</summary>
        protected virtual void RegisterActionEvents()
        {
        }

        /// <summary>
        /// 注册动作转移事件：收到该事件时由 OnActionEvent 判定是否切入本状态。
        /// </summary>
        /// <param name="eventType">触发状态转移的事件类型。</param>
        protected void RegisterActionEvent(MEventType eventType)
        {
            RegisterEvent(eventType, _onActionEvent);
        }

        /// <summary>
        /// 注册动作结束事件：收到该事件时自动调用 Finish() 正常结束本状态。
        /// </summary>
        /// <param name="eventType">触发状态正常结束的事件类型。</param>
        protected void RegisterFinishEvent(MEventType eventType)
        {
            RegisterEvent(eventType, _onFinishEvent);
        }

        /// <summary>
        /// 从宿主实体摘除时的回调，重置结束标记并调用基类清理。
        /// </summary>
        public override void OnDetachFromHost()
        {
            _isFinished = true;
            _evt = null;
            base.OnDetachFromHost();
        }

        /// <summary>对象池回收时清理动作令牌和临时事件。</summary>
        public override void Shutdown()
        {
            _isFinished = true;
            _token = 0;
            _evt = null;
            _isOverrideSuccess = true;
            base.Shutdown();
        }

        /// <summary>
        /// 状态成功获得转移许可后调用：标记未结束、通知状态机已触发，
        /// 若当前处于战斗坐骑上则处理坐骑显隐，最后回调 OnStart 开始状态逻辑。
        /// </summary>
        //状态成功转移
        public void OnGetPermission()
        {
            _isFinished = false;
            Entity.Machine.MarkTriggered();
            /*if (Entity.BattleVehicleID > 0)
            {
                TakeBattleVehilce();
            }*/
            OnStart();
        }

        /// <summary>
        /// 强制停止当前状态。
        /// </summary>
        /// <param name="toDefault">停止后是否强制状态机回到默认状态，默认 true。</param>
        /// <param name="selfTrans">是否由自身状态转移触发，默认 false。</param>
        //状态强制停止
        public virtual void Stop(bool toDefault = true, bool selfTrans = false)
        {
            if (!_isFinished)
            {
                _isFinished = true;
                _isOverrideSuccess = true;
                OnStop(selfTrans);
                AfterFinish(selfTrans);
                if (toDefault && Entity != null && Entity.Machine != null)
                {
                    Entity.Machine.ForceToDefault();
                }
            }
        }

        /// <summary>
        /// 状态正常播放完毕后调用，标记结束并依次回调 OnFinished 与 AfterFinish。
        /// </summary>
        //状态正常结束
        public virtual void Finish()
        {
            if (!_isFinished)
            {
                _isFinished = true;
                _isOverrideSuccess = true;
                OnFinished();
                AfterFinish();
            }
        }

        /// <summary>
        /// 状态机逐帧驱动入口：仅当状态未结束时才向下分发 OnUpdate。
        /// </summary>
        /// <param name="deltaTime">距上一帧的时间（秒）。</param>
        public void StateUpdate(float deltaTime)
        {
            if (!IsFinished)
            {
                OnUpdate(deltaTime);
            }
        }

        /// <summary>
        /// 状态开始时播放对应动画。若 OverrideAnim 失败则退回 idle 动画；
        /// 根据是否允许混合到默认动作，选择 CrossFade 过渡或直接 Play，
        /// 并对人物模型与坐骑模型分别进行动画切换。
        /// </summary>
        /// <param name="preState">上一个动画机状态名（保留参数）。</param>
        /// <returns>实际播放的动画机状态名。</returns>
        //状态播放动画逻辑
        public virtual string PlayAnim(string preState)
        {
            // 本次动作准备播放的 Animator 状态名；安装失败时会切换为待机状态。
            string stateName = StateName;
            _isOverrideSuccess = PrepareStateAnimation(stateName);
            if (!_isOverrideSuccess)
            {
                stateName = MAnimator.STATE_IDLE;
                PrepareFallbackAnimation(stateName);
                // 缺少真实动画时不仅播放 Idle，还要把状态机从 Special/其他动作状态归一到默认状态，
                // 防止下一帧再次以无效状态请求动画并持续刷错误日志。
                if (Entity != null && Entity.Machine != null &&
                    Entity.Machine.CurrentState != Entity.Machine.DefaultState)
                {
                    Entity.Machine.ForceToDefault();
                }
            }
            if (Entity == null || Entity.Model == null || Entity.Model.Ator == null)
            {
                return stateName;
            }
            Entity.AtorSpeed = Speed;
            if (Entity.IsMixToDefaultAction)
            {
                Entity.Model.Ator.CrossFade(stateName, 0.1f);
            }
            else
            {
                Entity.Model.Ator.Play(stateName);
            }
            return stateName;
        }

        /// <summary>
        /// 从当前实体表现表解析并安装目标状态的真实动画剪辑。
        /// 技能、特殊动作等没有通用表现表字段的状态必须由专用系统先提供剪辑，不能回退到 Controller 默认剪辑。
        /// </summary>
        /// <param name="stateName">需要安装剪辑的 Animator 状态名。</param>
        /// <returns>找到真实剪辑且成功提交异步准备时返回 true；缺少配置或索引时返回 false。</returns>
        private bool PrepareStateAnimation(string stateName)
        {
            if (Entity == null || Entity.Model == null || Entity.Model.Ator == null)
            {
                return false;
            }

            // 专用技能或交互系统可能已先行安装的真实剪辑绑定；准备中状态同样属于有效安装。
            MAnimator.MAnimInfo installedAnimInfo = Entity.Model.Ator.GetAnimInfo(stateName);
            if (installedAnimInfo != null &&
                !string.IsNullOrEmpty(installedAnimInfo.ClipPath))
            {
                return true;
            }

            // 目标状态在表现表中配置的动画短名或完整 Location。
            string clipName = ResolveClipName(stateName);
            if (string.IsNullOrEmpty(clipName))
            {
                Debug.LogError("动作状态缺少真实动画配置，已回退待机。状态=" + stateName);
                return false;
            }

            // 动画信息同时代表已经就绪或正在异步准备的真实剪辑绑定。
            MAnimator.MAnimInfo animInfo = Entity.OverrideAnim(stateName, clipName);
            if (animInfo == null)
            {
                Debug.LogError("动作动画未登记到 animInfo.txt，已回退待机。状态=" +
                    stateName + "，动画=" + clipName);
                return false;
            }
            return true;
        }

        /// <summary>安装动作失败时使用的真实待机剪辑，避免直接播放未覆盖的 Controller 占位剪辑。</summary>
        /// <param name="fallbackStateName">回退 Animator 状态名，当前固定为 Idle。</param>
        private void PrepareFallbackAnimation(string fallbackStateName)
        {
            if (Entity == null || Entity.Model == null || Entity.Model.Ator == null)
            {
                return;
            }

            // 表现表中用于失败回退的真实待机动画名。
            string fallbackClipName = ResolveClipName(fallbackStateName);
            if (string.IsNullOrEmpty(fallbackClipName))
            {
                Debug.LogError("动作回退失败：表现表未配置待机动画。状态=" + StateName);
                return;
            }

            // 回退待机动画的异步绑定信息；为空表示索引未登记该资源。
            MAnimator.MAnimInfo fallbackAnimInfo = Entity.OverrideAnim(
                fallbackStateName, fallbackClipName);
            if (fallbackAnimInfo == null)
            {
                Debug.LogError("动作回退失败：待机动画未登记到 animInfo.txt。动画=" +
                    fallbackClipName);
            }
        }

        /// <summary>按 RO 的表现表字段映射解析常规动作状态使用的真实动画名。</summary>
        /// <param name="stateName">Animator 中稳定的状态槽名称。</param>
        /// <returns>表现表动画短名或完整 Location；当前状态没有通用字段时返回空字符串。</returns>
        private string ResolveClipName(string stateName)
        {
            if (Entity == null || Entity.AttrComp == null)
            {
                return string.Empty;
            }

            // 当前实体职业或外观对应的表现表，字段值由动画索引解析为 YooAsset Location。
            PresentTable presentData = Entity.AttrComp.PresentDataView;
            if (presentData == null)
            {
                return string.Empty;
            }

            if (stateName == MAnimator.STATE_IDLE)
            {
                return presentData.IdleAnim;
            }
            if (stateName == MAnimator.STATE_FIDLE)
            {
                return presentData.IdleFAnim;
            }
            if (stateName == MAnimator.STATE_ZD_MOVE_WALK)
            {
                return presentData.WalkFAnim;
            }
            if (stateName == MAnimator.STATE_ZD_MOVE_RUN)
            {
                return presentData.RunFAnim;
            }
            if (stateName == MAnimator.STATE_NONZD_MOVE_WALK)
            {
                return presentData.WalkAnim;
            }
            if (stateName == MAnimator.STATE_NONZD_MOVE_RUN)
            {
                return presentData.RunAnim;
            }
            if (stateName == MAnimator.STATE_BEHIT)
            {
                return presentData.BeHitAnim;
            }
            if (stateName == MAnimator.STATE_STUN)
            {
                return presentData.StunAnim;
            }
            if (stateName == MAnimator.STATE_SINGING)
            {
                return presentData.SingingAnim;
            }
            if (stateName == MAnimator.STATE_DEAD)
            {
                return presentData.DeadAnim;
            }
            if (stateName == MAnimator.STATE_BORN)
            {
                return presentData.BornAnim;
            }
            if (stateName == MAnimator.STATE_SPECIAL)
            {
                // Special 状态使用表现表中的休闲动作；该字段为空时再使用 NPC 欢迎动作。
                if (!string.IsNullOrEmpty(presentData.FreeStyleAnim))
                {
                    return presentData.FreeStyleAnim;
                }
                if (!string.IsNullOrEmpty(presentData.OpenAnim))
                {
                    return presentData.OpenAnim;
                }
                // 未配置特殊动作属于无效状态请求，返回空值并由 PlayAnim 统一回退 Idle。
                return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// 状态转移消息监听器：调用子类 OnGetEvent 判定是否可切入本状态，
        /// 允许则暂存事件并驱动状态机转移到本状态，否则强制回到默认状态。
        /// </summary>
        /// <param name="e">触发状态转移的事件参数。</param>
        //状态转移消息监听
        protected void OnActionEvent(IEventArg e)
        {
            IMStateTransform last = Entity.Machine.CurrentState;

            if (OnGetEvent(e, last.SelfState))
            {
                _evt = e;
                Entity.Machine.TransferToState(this);
                _evt = null;
            }
            else
            {
                Entity.Machine.ForceToDefault();
            }
        }

        /// <summary>
        /// 动作结束事件回调：收到结束事件时调用 Finish() 正常结束当前状态。
        /// </summary>
        /// <param name="eventArg">触发事件的参数。</param>
        private void OnFinishEvent(IEventArg eventArg)
        {
            Finish();
        }
        /// <summary>获取该状态下实体是否仍可移动，默认 true。</summary>
        //是否可以移动
        public virtual bool IsActiveMovable  { get { return true; } }

        /// <summary>获取是否可以被主动打断（如收到移动指令时停止），默认 false。</summary>
        //是否可以主动打断该状态
        public virtual bool IsActiveInterrupt  { get { return false; } }

        /// <summary>获取转移到该状态时是否需要保留战斗坐骑显示（子类必须实现）。</summary>
        public abstract bool WithBattleVehicle { get; }

        /// <summary>状态转移判定钩子：子类实现是否接受事件 e 切入本状态。</summary>
        /// <param name="e">触发事件参数。</param>
        /// <param name="last">上一个状态定义。</param>
        /// <returns>true 表示允许转移到本状态。</returns>
        //状态转移判定
        protected abstract bool OnGetEvent(IEventArg e, StateDefine last);

        /// <summary>状态开始执行时的回调钩子，子类可选覆写。</summary>
        //状态开始执行
        protected virtual void OnStart() { }

        /// <summary>状态持续期间逐帧回调。</summary>
        /// <param name="deltaTime">距上一帧时间（秒）。</param>
        //状态持续中的update
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>状态正常播放结束后的回调钩子。</summary>
        //状态正常结束后回调
        protected virtual void OnFinished() { }

        /// <summary>状态被强制停止后的回调钩子。</summary>
        /// <param name="selfTrans">是否由自身状态转移触发。</param>
        //状态强制结束后回调
        protected virtual void OnStop(bool selfTrans = false) { }

        /// <summary>状态结束后的统一回调，调用顺序在 OnStop/OnFinished 之后。</summary>
        /// <param name="selfTrans">是否由自身状态转移触发。</param>
        //状态结束后回调，顺序在onstop和onfinish后面
        protected virtual void AfterFinish(bool selfTrans = false) { }

        /// <summary>
        /// 根据 WithBattleVehicle 决定临时隐藏或显示战斗坐骑，
        /// 通过派发 MBattleVehicleTemporaryHiding 事件通知坐骑表现层。
        /// </summary>
        //隐藏或者显示战斗坐骑
        protected void TakeBattleVehilce()
        {
            /*var args = MEventPool<MBattleVehicleTemporaryHiding>.GetEvent();
            args.hide = !WithBattleVehicle;
            MEventMgr.singleton.FireEvent(MEventType.MEvent_BattleVehicleTemporaryHiding, _entity, args);*/
        }

        /// <summary>
        /// 收到移动消息后的处理：若当前状态可被主动打断且不允许移动，则主动停止本状态。
        /// </summary>
        //收到移动消息后是否需要结束当前状态
        public virtual void Move()
        {
            if (IsActiveInterrupt && !IsActiveMovable)
            {
                Stop();
            }
        }


    }
