using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>沿原技能时间轴播放公共怪物Animator、原特效和声音；不计算任何伤害。</summary>
public sealed class MSceneMonsterSkillPlayer
{
    /// <summary>一次吟唱或FireSkill阶段，快照更新仍复用该次阶段的已消费轨道。</summary>
    private sealed class Playback
    {
        /// <summary>最近权威快照及收到时刻。</summary>
        public MSceneMonsterMotion Motion;
        /// <summary>对应原技能的只读表现数据。</summary>
        public MSceneMonsterSkillData Data;
        /// <summary>异步完成后实际可播的原剪辑。</summary>
        public AnimationClip Clip;
        /// <summary>下一条未处理特效下标。</summary>
        public int FxIndex;
        /// <summary>下一条未处理声音下标。</summary>
        public int AudioIndex;
        /// <summary>下一条未处理弹道结果下标。</summary>
        public int ResultIndex;
        /// <summary>本阶段是否已首次启动实际剪辑。</summary>
        public bool AnimationStarted;
    }

    /// <summary>已实例化的单个特效，包括预警Shader或原非权威弹道。</summary>
    private sealed class Effect
    {
        /// <summary>通过ResourceMgr异步实例化的正式原预制体。</summary>
        public GameObject Model;
        /// <summary>本实例应回收的本地实时时刻，单位秒。</summary>
        public double End;
        /// <summary>吟唱或持续效果所属阶段，切换时立即回收。</summary>
        public Playback Singing;
        /// <summary>原预警参数，普通特效为空。</summary>
        public MSceneMonsterFxData Warning;
        /// <summary>预警起点，用于晚加入恢复填充比例。</summary>
        public double Started;
        /// <summary>预警使用的原Renderer集合。</summary>
        public Renderer[] Renderers;
        /// <summary>非权威弹道追随的真实目标，普通特效为空。</summary>
        public RoleEntity Target;
        /// <summary>原弹道速度，单位米每秒。</summary>
        public float Speed;
        /// <summary>本弹道原参数；普通效果为空。</summary>
        public MSceneMonsterProjectileData Projectile;
        /// <summary>施法时固定世界原点，后续移动不改变原弹道起点。</summary>
        public Vector3 Origin;
        /// <summary>施法时固定朝向，将原局部控制点转换到世界空间。</summary>
        public Quaternion Rotation;
        /// <summary>原直线或Unreal发射方向。</summary>
        public Vector3 Direction;
        /// <summary>服务器指定地面点或目标位置的快照。</summary>
        public Vector3 Destination;
    }

    /// <summary>模型所属原MEntity（怪物或NPC）。</summary>
    private readonly MEntity monster;
    /// <summary>模型控制器也是异步完成时的生命周期依据。</summary>
    private readonly RoleCtrlBase host;
    /// <summary>当前吟唱或攻击阶段。</summary>
    private Playback active;
    /// <summary>原技能状态结束后仍执行至Time的尾轨道，可与下一技能并存。</summary>
    private readonly List<Playback> playbacks = new List<Playback>();
    /// <summary>本模型生存期间持有的异步动画/声音资源句柄。</summary>
    private readonly Dictionary<string, AssetOperationHandle> resources = new Dictionary<string, AssetOperationHandle>();
    /// <summary>待按原寿命回收的实例特效。</summary>
    private readonly List<Effect> effects = new List<Effect>();
    /// <summary>经SoundComponent创建的三维声源及其停止时刻。</summary>
    private readonly Dictionary<AudioSource, double> sounds = new Dictionary<AudioSource, double>();
    /// <summary>记录吟唱循环声音所属阶段，中断时立即停止而不等预定时限。</summary>
    private readonly Dictionary<AudioSource, Playback> soundOwners = new Dictionary<AudioSource, Playback>();
    /// <summary>当前模型是否已离场，迟到异步回调据此放弃表现。</summary>
    private bool disposed;
    /// <summary>技能前公共动画倍率，阶段结束时恢复。</summary>
    private float previousSpeed;
    /// <summary>是否已经保存本次技能前的倍率。</summary>
    private bool ownsSpeed;

    /// <summary>绑定现有模型实体，不建立新的角色世界或控制器。</summary>
    /// <param name="owner">正式怪物控制器。</param>
    /// <param name="entity">同一控制器持有的怪物实体。</param>
    public MSceneMonsterSkillPlayer(RoleCtrlBase owner, MEntity entity)
    {
        host = owner;
        monster = entity;
    }

    /// <summary>读取新阶段或更新中途快照，技能时间不从模型加载完成时重新计时。</summary>
    /// <param name="motion">带真实接收时刻的权威动作。</param>
    public void Apply(MSceneMonsterMotion motion)
    {
        if (motion.Action.Phase == MonsterActionPhase.Dead) { Clear(); return; }
        if (motion.Action.Phase != MonsterActionPhase.Attack && motion.Action.Phase != MonsterActionPhase.Singing)
        {
            EndPhase();
            return;
        }
        // 原正在进行的阶段快照只校正时钟，不重复触发已经播过的结果。
        if (active != null && motion.Action.ElapsedMs > 0)
        {
            if (active.Motion.Action.SkillId == motion.Action.SkillId && active.Motion.Action.Phase == motion.Action.Phase)
            {
                active.Motion = motion;
                return;
            }
        }
        EndPhase();
        // 原技能配置号不命中时不借用另一怪物普攻。
        MSceneMonsterSkillData data = MSceneMonsterSkillCatalog.Find(motion.Action.SkillId);
        if (data == null) return;
        // 本次独立阶段的时间轴与已消费事件游标。
        Playback playback = new Playback { Motion = motion, Data = data };
        active = playback;
        playbacks.Add(playback);
        // 吟唱独立于攻击时间轴；未配置吟唱剪辑时保持公共待机。
        string clipPath = data.ClipPath;
        if (motion.Action.Phase == MonsterActionPhase.Singing) clipPath = data.SingingClipPath;
        if (!string.IsNullOrEmpty(clipPath))
            Load(clipPath, handle =>
            {
                if (active != playback || disposed) return;
                playback.Clip = handle.AssetObject as AnimationClip;

            });
        if (motion.Action.Phase == MonsterActionPhase.Singing)
            PlayAudio(data.SingingAudioPath, monster, 0f, true, playback);
    }

    /// <summary>通过当前项目的YooAsset句柄异步加载原动画或声音。</summary>
    /// <param name="path">正式资源完整Location。</param>
    /// <param name="ready">资源完成后使用同一句柄；模型离场时统一释放。</param>
    private void Load(string path, Action<AssetOperationHandle> ready)
    {
        // 同一怪物重复用同一技能时复用已持有的资源引用。
        AssetOperationHandle handle;
        if (!resources.TryGetValue(path, out handle))
        {
            handle = Global.Instance.YooPackage.LoadAssetAsync(path);
            resources.Add(path, handle);
        }
        if (handle.IsDone) ready(handle);
        else handle.Completed += ready;
    }

    /// <summary>把权威剩余/已过时间转换回原MSkillData的秒数。</summary>
    /// <param name="playback">正在播放的独立阶段。</param>
    private static float Timeline(Playback playback)
    {
        // 原阶段实时时间，包含模型与资源异步加载耗时。
        float seconds = (float)(Time.realtimeSinceStartupAsDouble - playback.Motion.ReceivedAt) + playback.Motion.Action.ElapsedMs * 0.001f;
        if (playback.Motion.Action.Phase == MonsterActionPhase.Singing) return seconds;
        return seconds * playback.Data.SkillStateTime * 1000f / (playback.Motion.Action.DurationMs + playback.Motion.Action.ElapsedMs);
    }

    /// <summary>更新实际特效寿命和原技能轨道，返回当前阶段是否仍占用动作。</summary>
    /// <param name="allowAnimation">受击态由既有状态机播放时为false。</param>
    public bool Update(bool allowAnimation)
    {
        PruneEffects();
        for (int index = playbacks.Count - 1; index >= 0; index--)
        {
            // 正在执行的原吟唱或FireSkill时间轴，包括上一动作的尾轨道。
            Playback playback = playbacks[index];
            // 以原秒数表示的进度，不将SkillStateTime与Time混为一个值。
            float time = Timeline(playback);
            // 吟唱使用协议自身时限，FireSkill尾事件使用原Time。
            bool singing = playback.Motion.Action.Phase == MonsterActionPhase.Singing;
            // 当前快照后的本地毫秒。
            double ageMs = (Time.realtimeSinceStartupAsDouble - playback.Motion.ReceivedAt) * 1000d;
            if (singing && ageMs >= playback.Motion.Action.DurationMs)
            {
                FinishPlayback(playback);
                continue;
            }
            if (!singing && time > playback.Data.Time)
            {
                FinishPlayback(playback);
                continue;
            }
            // 原吟唱与攻击使用独立的特效表。
            MSceneMonsterFxData[] list = playback.Data.Effects;
            if (singing) list = playback.Data.SingingEffects;
            while (playback.FxIndex < list.Length && list[playback.FxIndex].At <= time)
            {
                // 下一条已到期但尚未消费的原特效指令。
                MSceneMonsterFxData fx = list[playback.FxIndex++];
                // 晚加入时跳过已经完全过期的效果。
                float missed = time - fx.At;
                if (fx.Duration < 0 || missed < fx.Duration) SpawnFx(fx, monster, missed, playback);
            }
            if (singing) continue;
            while (playback.AudioIndex < playback.Data.Audio.Length && playback.Data.Audio[playback.AudioIndex].At <= time)
            {
                // 原时间轴中的一次声音事件，LoopCount已按原间隔展开。
                MSceneMonsterAudioData audio = playback.Data.Audio[playback.AudioIndex++];
                if (time - audio.At < 0.15f) PlayAudio(audio.Path, monster, audio.Duration, audio.Loop, playback);
            }
            while (playback.ResultIndex < playback.Data.Results.Length && playback.Data.Results[playback.ResultIndex].At <= time)
            {
                // 该结果只生成视觉弹道；伤害始终等服务器通知。
                MSceneMonsterResultData result = playback.Data.Results[playback.ResultIndex++];
                SpawnProjectile(result, playback, time - result.At);
            }
        }
        if (active == null) return false;
        // 动画占用到原SkillStateTime为止，剩余尾轨道继续由上面的列表执行。
        double animationAgeMs = (Time.realtimeSinceStartupAsDouble - active.Motion.ReceivedAt) * 1000d;
        if (animationAgeMs >= active.Motion.Action.DurationMs) { EndPhase(); return false; }
        if (allowAnimation && active.Clip != null)
        {
            // 沿同一公共Animator的Skill槽绑定当前真实剪辑。
            MAnimator animator = monster.Model.Ator;
            if (!ownsSpeed) { previousSpeed = animator.Speed; ownsSpeed = true; }
            // 攻速由状态时间换算；吟唱剪辑按原速度循环。
            float speed = 1f;
            if (active.Motion.Action.Phase != MonsterActionPhase.Singing)
                speed = active.Data.SkillStateTime * 1000f / (active.Motion.Action.DurationMs + active.Motion.Action.ElapsedMs);
            animator.Speed = speed;
            if (!active.AnimationStarted || animator.CurrentState != MAnimator.STATE_SKILL)
            {
                animator.GetOrCreateOverrideController()[MAnimator.STATE_SKILL] = active.Clip;
                animator.Play(MAnimator.STATE_SKILL, 0, Timeline(active) / active.Clip.length);
                active.AnimationStarted = true;
            }
        }
        return true;
    }

    /// <summary>播放服务器确认命中的原结果特效和声音。</summary>
    /// <param name="hit">权威结果，含原技能与ResultList序号。</param>
    /// <param name="target">实际命中的角色实体。</param>
    public void PlayResult(MonsterPlayerHitData hit, RoleEntity target)
    {
        // 此原技能编号对应的只读结果配置。
        MSceneMonsterSkillData data = MSceneMonsterSkillCatalog.Find(hit.SkillId);
        if (data == null || hit.ResultIndex >= data.Results.Length) return;
        // 按服务器零基序号选择的原结果点。
        MSceneMonsterResultData result = data.Results[hit.ResultIndex];
        // 本次实际命中的普通或暴击原特效。
        MSceneMonsterFxData fx = result.HitFx;
        // 本次实际命中的普通或暴击声音地址。
        string audio = result.HitAudio;
        if (hit.Critical)
        {
            // JsonUtility可能把JSON null嵌套对象读成空实例，原Fx地址才是已配置的依据。
            if (result.CriticalFx != null && !string.IsNullOrEmpty(result.CriticalFx.Path)) fx = result.CriticalFx;
            if (!string.IsNullOrEmpty(result.CriticalAudio)) audio = result.CriticalAudio;
        }
        if (fx != null) SpawnFx(fx, target, 0f, null);
        PlayAudio(audio, target, 0f, false, null);
    }

    /// <summary>沿原特效加载和SoundComponent显示HeadShow5，不复用技能编号。</summary>
    public void ShowHeadExpression()
    {
        // 原表裁剪配置包含两秒寿命、原挂点偏移和原FX/音频地址。
        MSceneMonsterNoticeCatalog data = MSceneMonsterNoticeCatalog.Load();
        SpawnFx(data.HeadExpression, monster, 0f, null);
        PlayAudio(data.HeadAudio, monster, data.HeadExpression.Duration, false, null);
    }

    /// <summary>通过既有ResourceMgr实例化原特效，过期或离场的加载结果立即回收。</summary>
    /// <param name="data">原特效指令。</param>
    /// <param name="target">施法者或真实受击者。</param>
    /// <param name="elapsed">原特效已过秒数，用于快照恢复。</param>
    /// <param name="playback">所属阶段，真实受击效果为空。</param>
    private void SpawnFx(MSceneMonsterFxData data, MEntity target, float elapsed, Playback playback)
    {
        // 原结果没有Fx时JsonUtility仍可能构造空对象，不向资源系统提交空地址。
        if (string.IsNullOrEmpty(data.Path)) return;
        // 请求资源时的真实秒数，加载耗时继续计入效果寿命。
        double requested = Time.realtimeSinceStartupAsDouble;
        ResourceMgr.Instance.LoadPrefabAsync(data.Path, model =>
        {
            if (disposed || host == null || monster.IsDead) { UnityEngine.Object.Destroy(model); return; }
            if (target.Model.GameObject.Transform == null) { UnityEngine.Object.Destroy(model); return; }
            if (playback != null && !playbacks.Contains(playback)) { UnityEngine.Object.Destroy(model); return; }
            // 原效果到本次异步完成为止已经过去的秒数。
            float age = elapsed + (float)(Time.realtimeSinceStartupAsDouble - requested);
            if (data.Duration > 0 && age >= data.Duration) { UnityEngine.Object.Destroy(model); return; }
            // 目标模型的原挂点，未指定骨骼时使用模型根。
            Transform anchor = target.Model.GameObject.Transform;
            if (!string.IsNullOrEmpty(data.Bone))
            {
                // 当前模型层级中的原骨骼引用。
                Transform[] bones = anchor.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < bones.Length; index++)
                    if (bones[index].name == data.Bone) { anchor = bones[index]; break; }
            }
            model.transform.position = anchor.TransformPoint(data.Offset) + Vector3.up * target.Height * data.HeightRatio;
            model.transform.rotation = anchor.rotation * Quaternion.Euler(0f, data.Face, 0f);
            if (data.Follow) model.transform.SetParent(anchor, true);
            model.transform.localScale = data.Scale;
            // 此实际特效实例的归属、时钟与回收记录。
            Effect effect = new Effect { Model = model, End = double.PositiveInfinity, Started = requested - elapsed };
            if (data.Duration > 0) effect.End = effect.Started + data.Duration;
            if (playback != null)
                if (data.Duration < 0 || playback.Motion.Action.Phase == MonsterActionPhase.Singing) effect.Singing = playback;
            if (data.WarningType != 0)
            {
                effect.Warning = data;
                effect.Renderers = model.GetComponentsInChildren<Renderer>(true);
                model.transform.localScale = Vector3.one;
            }
            // 原粒子快照从已经过去的秒数恢复；不从零再播一次预警。
            ParticleSystem[] particles = model.GetComponentsInChildren<ParticleSystem>(true);
            for (int index = 0; index < particles.Length; index++)
            {
                particles[index].Simulate(age, false, true, true);
                particles[index].Play(false);
            }
            effects.Add(effect);
        });
    }

    /// <summary>原四类弹道只作视觉运动，碰撞与伤害始终由服务器处理。</summary>
    /// <param name="result">原ResultList中的弹道设置。</param>
    /// <param name="playback">发出弹道的技能尾轨道。</param>
    /// <param name="elapsed">弹道触发后已经过去的原秒数，供晚加入恢复。</param>
    private void SpawnProjectile(MSceneMonsterResultData result, Playback playback, float elapsed)
    {
        // 当前结果有真实弹道资源时才实例化。
        MSceneMonsterProjectileData data = result.Projectile;
        // 原无LongAttackData的JSON null会成为空对象，以实际原Prefab地址判定有无弹道。
        if (data == null || string.IsNullOrEmpty(data.Path)) return;
        if (data.Kind == 2 && elapsed >= data.InitTime) return;
        if (data.TotalTime > 0 && elapsed >= data.TotalTime) return;
        // 原目标角色可能为空；地面技能使用权威施法坐标。
        RoleEntity target = Global.Instance.SceneWorld.LocalRole;
        if (target == null || target.GetID() != playback.Motion.Action.TargetRoleId)
            Global.Instance.SceneWorld.TryGetRole(playback.Motion.Action.TargetRoleId, out target);
        // 资源加载等待期间固定施法瞬间的世界原点与朝向。
        Vector3 origin = monster.Position;
        // 旋转只取原权威面向，不依赖客户端后续转身。
        Quaternion rotation = Quaternion.Euler(0f, playback.Motion.Action.Face, 0f);
        // 无角色目标时的实际地面施法点。
        Vector3 destination = new Vector3(playback.Motion.Action.CastX, playback.Motion.Action.CastY, playback.Motion.Action.CastZ);
        if (target != null)
        {
            destination = target.Position;
            if (data.AimTargetCenter) destination += Vector3.up * target.Height * 0.5f;
        }
        // 异步资源等待仍计入原弹道已过时间。
        double requested = Time.realtimeSinceStartupAsDouble;
        ResourceMgr.Instance.LoadPrefabAsync(data.Path, model =>
        {
            if (disposed || !playbacks.Contains(playback)) { UnityEngine.Object.Destroy(model); return; }
            // 生成的是非权威视觉对象，与原角色世界共用ResourceMgr。
            Effect effect = new Effect { Model = model, Started = requested - elapsed, Projectile = data,
                Origin = origin, Rotation = rotation, Target = target, Destination = destination };
            model.transform.position = origin + rotation * data.Start;
            effect.Direction = (destination - model.transform.position).normalized;
            if (data.Kind == 3 || data.Kind == 4) effect.Direction = (rotation * (data.End - data.Start)).normalized;
            // 实体弹道按原TotalTime，Anim按InitTime；Unreal在到达实际目标后回收。
            effect.End = double.PositiveInfinity;
            if (data.Kind == 2) effect.End = effect.Started + data.InitTime;
            else if (data.TotalTime > 0) effect.End = effect.Started + data.TotalTime;
            // 迟到实例直接恢复当前原轨迹位置，不重新飞完整一程。
            UpdateProjectile(effect, Time.realtimeSinceStartupAsDouble);
            if (Time.realtimeSinceStartupAsDouble >= effect.End) { UnityEngine.Object.Destroy(model); return; }
            effects.Add(effect);
            if (elapsed < 0.15f) PlayAudio(data.Audio, monster, 0f, false, playback);
        });
    }

    /// <summary>按RO当前闭包实际使用的Unreal、Anim、Real和Rect公式更新弹道。</summary>
    /// <param name="effect">带原参数与施法快照的视觉弹道。</param>
    /// <param name="now">当前真实秒数。</param>
    private static void UpdateProjectile(Effect effect, double now)
    {
        // 当前弹道的原静态配置。
        MSceneMonsterProjectileData data = effect.Projectile;
        // 该弹道已飞行的秒数。
        float time = (float)(now - effect.Started);
        // 先保留上一帧位置，后续用真实位移确定朝向。
        Vector3 before = effect.Model.transform.position;
        // 原目标当前中部；没有角色目标时保留服务器地面点。
        Vector3 destination = effect.Destination;
        if (effect.Target != null)
        {
            destination = effect.Target.Position;
            if (data.AimTargetCenter) destination += Vector3.up * effect.Target.Height * 0.5f;
        }
        if (data.Kind == 2)
        {
            // 原MAnimBullet对归一化时间套用导出的Z曲线。
            float t = Mathf.Clamp01(time / data.InitTime);
            if (data.Curve.Length > 0) t = data.Evaluate(t);
            // 原三次贝塞尔的剩余权重。
            float u = 1f - t;
            // 原InitAngle对后两控制点的Z轴旋转。
            Quaternion angle = Quaternion.Euler(0f, 0f, data.InitAngle);
            // 原三个控制点均相对施法瞬间的位置。
            Vector3 p1 = effect.Origin + effect.Rotation * data.P1;
            // 第二控制点按原顺序应用施法方向和初始角。
            Vector3 p2 = effect.Origin + angle * effect.Rotation * data.P2;
            // 第三控制点决定尾段进入目标的曲率。
            Vector3 p3 = effect.Origin + angle * effect.Rotation * data.P3;
            effect.Model.transform.position = u * u * u * p1 + 3f * u * u * t * p2 + 3f * u * t * t * p3 + t * t * t * destination;
        }
        else
        {
            if (data.Kind == 4 && data.Revolution)
            {
                // 原本次Rect只有公转，没有自转，保留其起点半径和世界中心。
                Vector3 center = effect.Origin + effect.Rotation * data.RevolutionCenter;
                // 原客户端公转半径只取XZ平面，保持公转中心原高度。
                Vector3 radial = effect.Rotation * (data.Start - data.RevolutionCenter);
                radial.y = 0f;
                effect.Model.transform.position = center + Quaternion.Euler(0f, data.RevolutionSpeed * time, 0f) * radial;
                effect.Model.transform.rotation = Quaternion.Euler(0f, data.RevolutionSpeed * time, 0f) * Quaternion.LookRotation(effect.Direction);
                return;
            }
            effect.Model.transform.position = effect.Origin + effect.Rotation * data.Start + effect.Direction * data.DistanceAt(time);
            if (data.Kind == 1)
            {
                // 原Unreal沿发射方向飞行，越过原目标距离后仅回收视觉对象。
                if (Vector3.Dot(effect.Destination - effect.Model.transform.position, effect.Direction) <= 0f) effect.End = now;
            }
        }
        // 没有实际位移时不对零向量执行LookRotation。
        Vector3 delta = effect.Model.transform.position - before;
        if (delta.sqrMagnitude > 0.000001f) effect.Model.transform.rotation = Quaternion.LookRotation(delta);
    }

    /// <summary>通过原项目SoundComponent播放导出的真实事件PCM。</summary>
    /// <param name="path">原事件对应AudioClip的正式路径。</param>
    /// <param name="target">声音跟随的真实模型。</param>
    /// <param name="duration">指定停止秒数；零使用剪辑自然长度。</param>
    /// <param name="loop">是否在当前阶段内循环。</param>
    /// <param name="playback">阶段生命周期，命中声音为空。</param>
    private void PlayAudio(string path, MEntity target, float duration, bool loop, Playback playback)
    {
        if (string.IsNullOrEmpty(path)) return;
        Load(path, handle =>
        {
            if (disposed || host == null || monster.IsDead) return;
            if (playback != null && !playbacks.Contains(playback)) return;
            // 通过当前资源句柄取得的实际AudioClip。
            AudioClip clip = handle.AssetObject as AudioClip;
            if (clip == null) return;
            // 由既有SoundComponent创建并管理的三维声源。
            AudioSource source = SoundComponent.PlayAudioInstance(target.Model, new SkillRuntimeAudioEvent { Clip = clip, Volume = 1f });
            if (source == null) return;
            source.loop = loop;
            // 该声源的本地停止时刻，单位真实秒。
            double end = Time.realtimeSinceStartupAsDouble + clip.length;
            if (duration > 0) end = Time.realtimeSinceStartupAsDouble + duration;
            if (loop && playback != null) end = playback.Motion.ReceivedAt + playback.Motion.Action.DurationMs * 0.001d;
            sounds.Add(source, end);
            soundOwners.Add(source, playback);
        });
    }

    /// <summary>逐帧清理到期表现，并更新原预警材质参数和弹道位置。</summary>
    private void PruneEffects()
    {
        // 当前真实秒数，供实例寿命与预警进度共用。
        double now = Time.realtimeSinceStartupAsDouble;
        for (int index = effects.Count - 1; index >= 0; index--)
        {
            // 此实际特效实例的归属、时钟与回收记录。
            Effect effect = effects[index];
            if (effect.Model == null || now >= effect.End)
            {
                UnityEngine.Object.Destroy(effect.Model);
                effects.RemoveAt(index);
                continue;
            }
            if (effect.Projectile != null) UpdateProjectile(effect, now);
            if (effect.Warning != null)
            {
                // 当前Renderer的临时属性块，保留同材质其他属性。
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                // 原预警内外径比例，对应初始填充进度。
                float initial = effect.Warning.WarningSize.y / effect.Warning.WarningSize.x;
                // 本帧原预警填充比例，从内径比例推进到1。
                float progress = Mathf.Lerp(initial, 1f, Mathf.Clamp01((float)(now - effect.Started) / effect.Warning.Duration));
                for (int item = 0; item < effect.Renderers.Length; item++)
                {
                    // 原预警实例中实际参与渲染的对象。
                    Renderer renderer = effect.Renderers[item];
                    renderer.GetPropertyBlock(block);
                    block.SetVector("_Params", effect.Warning.WarningSize);
                    block.SetFloat("_Progress", progress);
                    renderer.SetPropertyBlock(block);
                }
            }
        }
        // 本模型声源引用快照，允许遍历时删除已停止声音。
        List<AudioSource> voices = new List<AudioSource>(sounds.Keys);
        for (int index = 0; index < voices.Count; index++)
        {
            // 当前需要检查或停止的实际AudioSource。
            AudioSource voice = voices[index];
            if (voice != null && now < sounds[voice]) continue;
            SoundComponent.StopSource(voice);
            sounds.Remove(voice);
            soundOwners.Remove(voice);
        }
    }

    /// <summary>恢复公共Animator倍率，受击状态不继承怪物攻速。</summary>
    public void RestoreSpeed()
    {
        if (!ownsSpeed) return;
        monster.Model.Ator.Speed = previousSpeed;
        ownsSpeed = false;
    }

    /// <summary>结束动画占用；原FireSkill尾轨道继续执行，吟唱切换则立即回收。</summary>
    private void EndPhase()
    {
        if (active != null)
            if (active.Motion.Action.Phase == MonsterActionPhase.Singing) FinishPlayback(active);
        active = null;
        RestoreSpeed();
    }

    /// <summary>原总时间结束或吟唱终止时，仅回收对应轨道的持续效果。</summary>
    /// <param name="playback">已完成的技能轨道。</param>
    private void FinishPlayback(Playback playback)
    {
        playbacks.Remove(playback);
        for (int index = effects.Count - 1; index >= 0; index--)
        {
            if (effects[index].Singing != playback) continue;
            UnityEngine.Object.Destroy(effects[index].Model);
            effects.RemoveAt(index);
        }
        // 同一吟唱的循环声随阶段立即停止，普通一次性尾声音自然结束。
        List<AudioSource> voices = new List<AudioSource>(sounds.Keys);
        for (int index = 0; index < voices.Count; index++)
        {
            // 当前原阶段创建的声源。
            AudioSource voice = voices[index];
            if (soundOwners[voice] != playback || voice == null) continue;
            if (!voice.loop) continue;
            SoundComponent.StopSource(voice);
            sounds.Remove(voice);
            soundOwners.Remove(voice);
        }
        if (active == playback) { active = null; RestoreSpeed(); }
    }

    /// <summary>死亡或离场立即取消原技能所有未到事件和已生成表现。</summary>
    public void Clear()
    {
        EndPhase();
        playbacks.Clear();
        for (int index = 0; index < effects.Count; index++) UnityEngine.Object.Destroy(effects[index].Model);
        effects.Clear();
        // 本模型声源引用快照，允许遍历时删除已停止声音。
        List<AudioSource> voices = new List<AudioSource>(sounds.Keys);
        for (int index = 0; index < voices.Count; index++) SoundComponent.StopSource(voices[index]);
        sounds.Clear();
        soundOwners.Clear();
    }

    /// <summary>模型释放时归还自身持有的全部YooAsset引用。</summary>
    public void Dispose()
    {
        Clear();
        disposed = true;
        // 本模型持有的动画与音频资源句柄快照。
        List<AssetOperationHandle> handles = new List<AssetOperationHandle>(resources.Values);
        for (int index = 0; index < handles.Count; index++) handles[index].Release();
        resources.Clear();
    }
}
