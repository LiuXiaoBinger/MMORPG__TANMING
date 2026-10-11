using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>沿RO模型声音组件持有三维声音实例；普通动作按编辑声音轨道播放，不依赖技能协议。</summary>
public sealed class SoundComponent
{
    /// <summary>MJS普通动作导出资产的固定资源地址。</summary>
    private const string LocomotionLocation = "Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimeLocomotionConfig_MJS.asset";
    /// <summary>仍被模型持有的普通动作共享加载记录，最后模型释放时归还。</summary>
    private static LocomotionCacheEntry _sharedEntry;
    /// <summary>普通动作加载记录的生命周期独立于模型，迟到回调只操作该记录。</summary>
    private sealed class LocomotionCacheEntry
    {
        /// <summary>当前引用该记录的模型数量，非负。</summary>
        public int Owners;
        /// <summary>已发起的异步资源句柄，加载前为空。</summary>
        public AssetOperationHandle Handle;
        /// <summary>句柄是否已完成，待完成记录由回调归还。</summary>
        public bool Completed;
        /// <summary>加载成功的普通动作只读配置。</summary>
        public SkillRuntimeLocomotionConfig Config;
        /// <summary>资源清单或加载错误已经报告，避免每帧重复。</summary>
        public bool Failed;
    }
    /// <summary>当前声音所属模型，重置时解除。</summary>
    private MModel _host;
    /// <summary>本模型持有的共享动作资产记录。</summary>
    private LocomotionCacheEntry _entry;
    /// <summary>当前仍播放的普通动作声音实例，重置时全部停止。</summary>
    private readonly List<AudioSource> _sources = new List<AudioSource>();
    /// <summary>本次实际播放的剪辑路径，用于检测动作切换。</summary>
    private string _actionLocation;
    /// <summary>当前声音轨道下一条未触发事件的下标。</summary>
    private int _nextEventIndex;
    /// <summary>上次观测的动作时间，单位秒，用于检测循环回绕。</summary>
    private float _previousSeconds;
    /// <summary>当前动作是否已经播放，首次动作从零开始消费声音。</summary>
    private bool _hasAction;

    /// <summary>绑定模型并持有普通动作声音配置，配置冷加载不会捕获实体池对象。</summary>
    /// <param name="host">管理该组件生命周期的模型。</param>
    public void Init(MModel host)
    {
        Reset();
        _host = host;
        if (_sharedEntry == null)
        {
            _sharedEntry = new LocomotionCacheEntry();
        }
        _entry = _sharedEntry;
        _entry.Owners++;
        BeginLoad(_entry);
    }

    /// <summary>从实际播放剪辑的时钟派发普通动作音效；技能动画不匹配这些配置。</summary>
    public void Update()
    {
        PruneSources();
        if (_host == null || _host.Ator == null || _entry == null)
        {
            return;
        }
        BeginLoad(_entry);
        // 已就绪且实际播放的剪辑信息；准备中的旧剪辑不能冒充新动作。
        MAnimator.MAnimInfo info = _host.Ator.GetCurrentAnimInfo();
        if (info == null || !info.IsClipReady || _entry.Config == null)
        {
            return;
        }
        // 普通动作只按当前真实资源路径选择，其他职业或技能不会误播MJS声音。
        SkillRuntimeLocomotionAction action = ResolveAction(info.ReadyClipPath);
        if (action == null)
        {
            ClearAction();
            return;
        }
        // 原动画时钟换算为秒，循环剪辑的NormalizedTime已由动画器折返。
        float seconds = info.NormalizedTime * info.Length;
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
        {
            ClearAction();
            return;
        }
        if (!_hasAction || _actionLocation != action.AnimationLocation)
        {
            ClearAction();
            _hasAction = true;
            _actionLocation = action.AnimationLocation;
        }
        else if (seconds < _previousSeconds)
        {
            // 循环跨尾时先消费尾端，然后从新循环起点重新消费；不丢失最后一个脚步点。
            if (info.IsLooping)
            {
                Dispatch(action, action.DurationSeconds);
            }
            _nextEventIndex = 0;
        }
        Dispatch(action, seconds);
        _previousSeconds = seconds;
    }

    /// <summary>RO状态触发入口，用于移动中落地直接切回走跑时仍播放落地声音。</summary>
    /// <param name="stateName">本次物理动作状态，只支持Jump_End即时落地触发。</param>
    public void Play(string stateName)
    {
        if (stateName != MAnimator.STATE_JUMP_END || _host == null || _host.Ator == null ||
            _entry == null || _entry.Config == null)
        {
            return;
        }
        // 立即切移动时不会进入落地动画；只执行零秒事件，后续非零事件仍由实际动画决定。
        SkillRuntimeLocomotionAction action = _entry.Config.JumpEnd;
        if (action == null || action.AudioEvents == null)
        {
            return;
        }
        // 即时落地同样匹配当前模型已登记的实际剪辑，其他职业不套用MJS声音。
        MAnimator.MAnimInfo animation = _host.Ator.GetAnimInfo(MAnimator.STATE_JUMP_END);
        if (animation == null || !animation.IsClipReady || animation.ReadyClipPath != action.AnimationLocation)
        {
            return;
        }
        // index为落地声音列表的零起始下标。
        for (int index = 0; index < action.AudioEvents.Count; index++)
        {
            // 当前零秒落地单次声音，非零事件不在即时入口补发。
            SkillRuntimeAudioEvent audioEvent = action.AudioEvents[index];
            if (audioEvent != null && audioEvent.TimeSeconds == 0f)
            {
                PlayEvent(audioEvent);
            }
        }
    }

    /// <summary>动作和模型销毁时停止全部声音，归还共享普通动作配置。</summary>
    public void Reset()
    {
        // index从尾部遍历所有并行声音，保证每个实例均停止后才解除资产依赖。
        for (int index = _sources.Count - 1; index >= 0; index--)
        {
            StopSource(_sources[index]);
        }
        _sources.Clear();
        ClearAction();
        if (_entry != null)
        {
            _entry.Owners--;
            if (_entry.Owners == 0)
            {
                if (ReferenceEquals(_sharedEntry, _entry))
                {
                    _sharedEntry = null;
                }
                if (_entry.Handle != null && _entry.Completed)
                {
                    _entry.Handle.Release();
                    _entry.Handle = null;
                }
                _entry.Config = null;
            }
        }
        _entry = null;
        _host = null;
    }

    /// <summary>创建跟随模型的单次三维声源；调用方持有音频配置到实例停止。</summary>
    /// <param name="host">声音所属的活跃模型。</param>
    /// <param name="audioEvent">含剪辑和零至一音量的单次声音。</param>
    /// <returns>已播放的实例；模型或事件无效时返回空。</returns>
    public static AudioSource PlayAudioInstance(MModel host, SkillRuntimeAudioEvent audioEvent)
    {
        if (host == null || host.GameObject == null || host.GameObject.Transform == null ||
            audioEvent == null || audioEvent.Clip == null ||
            float.IsNaN(audioEvent.Volume) || float.IsInfinity(audioEvent.Volume) ||
            audioEvent.Volume < 0f || audioEvent.Volume > 1f)
        {
            return null;
        }
        // 声源使用独立子节点，使同一时刻多条技能声音可以并行播放。
        GameObject soundObject = new GameObject("SkillAudio_" + audioEvent.Clip.name);
        soundObject.transform.SetParent(host.GameObject.Transform, false);
        // 三维声源随模型移动，最大距离之外不会成为全场景声音。
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.clip = audioEvent.Clip;
        source.volume = audioEvent.Volume;
        source.Play();
        return source;
    }

    /// <summary>停止单个声音并解除剪辑引用，避免资源归还后继续播放。</summary>
    /// <param name="source">已生成的单次声源，可以为空或已销毁。</param>
    public static void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }
        source.Stop();
        source.clip = null;
        Object.Destroy(source.gameObject);
    }

    /// <summary>首次资源系统就绪时加载动作配置；旧记录迟到回调只清理其自身句柄。</summary>
    /// <param name="entry">当前被模型持有的加载记录。</param>
    private static void BeginLoad(LocomotionCacheEntry entry)
    {
        if (entry.Handle != null || entry.Failed || Global.Instance == null || Global.Instance.YooPackage == null)
        {
            return;
        }
        if (!Global.Instance.YooPackage.CheckLocationValid(LocomotionLocation))
        {
            entry.Failed = true;
            Debug.LogWarning("普通动作声音配置未进入资源清单：" + LocomotionLocation);
            return;
        }
        entry.Handle = Global.Instance.YooPackage.LoadAssetAsync<SkillRuntimeLocomotionConfig>(LocomotionLocation);
        entry.Handle.Completed += handle =>
        {
            // handle为该加载记录完成的资源句柄，不引用已回收模型。
            entry.Completed = true;
            if (entry.Owners == 0 || !ReferenceEquals(_sharedEntry, entry))
            {
                handle.Release();
                entry.Handle = null;
                return;
            }
            if (handle.Status != EOperationStatus.Succeed)
            {
                entry.Failed = true;
                Debug.LogWarning("普通动作声音配置加载失败：" + LocomotionLocation);
                return;
            }
            entry.Config = handle.GetAssetObject<SkillRuntimeLocomotionConfig>();
        };
    }

    /// <summary>按已安装剪辑完整路径匹配普通动作，避免通过名称猜测角色职业。</summary>
    /// <param name="location">当前实际已就绪剪辑的资源地址。</param>
    /// <returns>匹配的普通动作，没有匹配返回空。</returns>
    private SkillRuntimeLocomotionAction ResolveAction(string location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return null;
        }
        // 已加载的普通、战斗走跑与跳跃动作配置，路径必须与原角色表现表相同。
        SkillRuntimeLocomotionConfig config = _entry.Config;
        if (config.Walk != null && config.Walk.AnimationLocation == location) return config.Walk;
        if (config.Run != null && config.Run.AnimationLocation == location) return config.Run;
        if (config.BattleWalk != null && config.BattleWalk.AnimationLocation == location) return config.BattleWalk;
        if (config.BattleRun != null && config.BattleRun.AnimationLocation == location) return config.BattleRun;
        if (config.JumpStart != null && config.JumpStart.AnimationLocation == location) return config.JumpStart;
        if (config.JumpEnd != null && config.JumpEnd.AnimationLocation == location) return config.JumpEnd;
        return null;
    }

    /// <summary>执行当前动作所有已到时刻事件，跨多帧仍保持同一轨道顺序。</summary>
    /// <param name="action">实际剪辑对应的普通动作配置。</param>
    /// <param name="seconds">本次动作时钟，单位秒。</param>
    private void Dispatch(SkillRuntimeLocomotionAction action, float seconds)
    {
        if (action.AudioEvents == null)
        {
            return;
        }
        while (_nextEventIndex < action.AudioEvents.Count)
        {
            // 待触发声音；无效记录不阻塞后续合法事件。
            SkillRuntimeAudioEvent audioEvent = action.AudioEvents[_nextEventIndex];
            if (audioEvent != null && audioEvent.TimeSeconds > seconds)
            {
                break;
            }
            if (audioEvent != null && !float.IsNaN(audioEvent.TimeSeconds) &&
                !float.IsInfinity(audioEvent.TimeSeconds) && audioEvent.TimeSeconds >= 0f &&
                audioEvent.TimeSeconds <= action.DurationSeconds)
            {
                PlayEvent(audioEvent);
            }
            _nextEventIndex++;
        }
    }

    /// <summary>为当前普通动作播放声音并保持实例到播放结束或模型回收。</summary>
    /// <param name="audioEvent">单次声音的资源与音量。</param>
    private void PlayEvent(SkillRuntimeAudioEvent audioEvent)
    {
        // 当前新声源，不与前一脚步或跳跃声共享播放槽位。
        AudioSource source = PlayAudioInstance(_host, audioEvent);
        if (source != null)
        {
            _sources.Add(source);
        }
    }

    /// <summary>清理播放完成的声源，限制长期走跑的子节点数量。</summary>
    private void PruneSources()
    {
        // index从尾部遍历，移除不会改变尚未访问的下标。
        for (int index = _sources.Count - 1; index >= 0; index--)
        {
            if (_sources[index] == null || !_sources[index].isPlaying)
            {
                StopSource(_sources[index]);
                _sources.RemoveAt(index);
            }
        }
    }

    /// <summary>清理普通动作时钟；已播放单次尾音继续由模型持有。</summary>
    private void ClearAction()
    {
        _actionLocation = null;
        _nextEventIndex = 0;
        _previousSeconds = 0f;
        _hasAction = false;
    }
}
