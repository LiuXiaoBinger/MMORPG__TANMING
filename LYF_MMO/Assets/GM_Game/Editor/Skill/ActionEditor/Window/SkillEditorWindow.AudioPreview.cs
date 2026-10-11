using System.Collections.Generic;
using UnityEngine;

/// <summary>窗口独占的时间轴声音预览；每个事件拥有独立声源，暂停和定位不操作正式角色。</summary>
public partial class SkillEditorWindow
{
    /// <summary>隐藏声音父对象，随关闭窗口、停止预览或进入游戏统一销毁。</summary>
    private GameObject _previewAudioObject;

    /// <summary>单个声音事件的预览状态，同一音频在不同帧出现时仍有独立播放位置。</summary>
    private sealed class PreviewAudioEntry
    {
        /// <summary>原配置中的声音事件引用，不复制或重写作者数据。</summary>
        public Global.FxAndSound effect;
        /// <summary>窗口创建的二维声源，只属于隐藏预览对象。</summary>
        public AudioSource source;
        /// <summary>是否由窗口暂停，恢复时使用UnPause保留当前采样点。</summary>
        public bool paused;
        /// <summary>特效副本自带且原本自动播放的声音；这类声音没有独立作者事件。</summary>
        public bool embedded;
        /// <summary>内嵌声音所属特效生成时的时间轴帧，零起始；独立声音仍读取原事件触发帧。</summary>
        public int triggerFrame;
    }

    /// <summary>当前时间轴声音事件的独立声源，允许多段声音重叠而不互相覆盖。</summary>
    private readonly List<PreviewAudioEntry> _previewAudioEntries = new List<PreviewAudioEntry>();

    /// <summary>接管副本原本自动播放的内嵌音源，保留原音量、音调及循环设置，手动音源不凭空触发。</summary>
    /// <param name="instance">刚生成的隐藏特效或飞行物副本，只接管其原AudioSource克隆。</param>
    private void RegisterEmbeddedPreviewAudio(GameObject instance)
    {
        // 副本内包含未激活节点的原生声音源快照。
        AudioSource[] sources=instance.GetComponentsInChildren<AudioSource>(true);
        // 原组件快照下标，不改变正式资源的playOnAwake属性。
        for(int index=0;index<sources.Length;index++)
        {
            // 当前特效克隆携带的原生音源。
            AudioSource source=sources[index];
            source.Stop();
            if(!source.playOnAwake || !source.enabled || !source.gameObject.activeInHierarchy || source.clip==null) continue;
            // 内嵌声音的触发帧来自此次特效生成点，重建时不能错误使用最终目标帧。
            int trigger=frameSelectIndex;
            if(_isSamplingPreview) trigger=_samplingPreviewFrame;
            source.Play();
            source.Pause();
            _previewAudioEntries.Add(new PreviewAudioEntry{source=source,embedded=true,triggerFrame=trigger,paused=true});
        }
    }

    /// <summary>确保窗口具有隐藏声音根；保留旧as1引用供第三方接口读取，时间轴不再使用PlayOneShot。</summary>
    private void EnsurePreviewAudioSource()
    {
        if (Application.isPlaying) return;
        if (_previewAudioObject == null)
        {
            _previewAudioObject = new GameObject("SkillEditorAudioPreview");
            _previewAudioObject.hideFlags = HideFlags.HideAndDontSave;
            as1 = _previewAudioObject.AddComponent<AudioSource>();
            as1.playOnAwake = false;
            as1.spatialBlend = 0f;
            as1.loop = false;
        }
        else if (as1 == null) as1 = _previewAudioObject.GetComponent<AudioSource>();
    }

    /// <summary>停止全部声音并清空事件声源，但保留隐藏父节点供下一次定位使用。</summary>
    private void ClearPreviewAudioEntries()
    {
        // 独立声源的零起始下标，删除组件不改变正在遍历的列表。
        for (int index = 0; index < _previewAudioEntries.Count; index++)
        {
            // 本窗口拥有的声源状态，原声音资源不销毁。
            PreviewAudioEntry entry = _previewAudioEntries[index];
            if (entry.source == null) continue;
            entry.source.Stop();
            DestroyImmediate(entry.source);
        }
        _previewAudioEntries.Clear();
        if (as1 != null) as1.Stop();
    }

    /// <summary>在原采样点暂停或恢复全部时间轴声音，不把暂停误当作停止重播。</summary>
    /// <param name="playing">真为继续播放，假为冻结所有已创建声源。</param>
    private void SetPreviewAudioPlaying(bool playing)
    {
        // 按索引操作现有声源，不在暂停过程中新增或重置事件。
        for (int index = 0; index < _previewAudioEntries.Count; index++)
        {
            // 正在暂停或恢复的窗口声源条目。
            PreviewAudioEntry entry = _previewAudioEntries[index];
            if (entry.source == null) continue;
            if (playing && entry.paused)
            {
                entry.source.UnPause();
                entry.paused = false;
            }
            else if (!playing && !entry.paused)
            {
                entry.source.Pause();
                entry.paused = true;
            }
        }
    }

    /// <summary>按时间轴帧定位所有仍在播放范围的声音；前后拖动均定位到对应音频采样点。</summary>
    /// <param name="frame">当前零起始时间轴帧，用统一时间轴FPS换算秒。</param>
    /// <param name="seek">真表示编辑定位或重建，强制对齐音频；连续播放只修正超过两帧的偏差。</param>
    private void SynchronizePreviewAudio(int frame, bool seek)
    {
        if (Application.isPlaying) return;
        EnsurePreviewAudioSource();
        // 统一时间轴每秒帧数，声音起点不采用音频自身采样率作为时间轴单位。
        float fps = GetPreviewFrameRate();
        // 反向清理已结束、被删除或替换音频资源的事件声源。
        for (int index = _previewAudioEntries.Count - 1; index >= 0; index--)
        {
            // 原事件和窗口声源的绑定状态。
            PreviewAudioEntry entry = _previewAudioEntries[index];
            if(entry.embedded)
            {
                if(entry.source==null){ _previewAudioEntries.RemoveAt(index); continue; }
                // 内嵌声音的采样时间包含原组件音调倍率；负音调原生行为不在此预览中反向重播。
                float embeddedElapsed=(frame-entry.triggerFrame)/fps*Mathf.Max(0f,entry.source.pitch);
                if(entry.source.clip==null || embeddedElapsed<0f || (!entry.source.loop && embeddedElapsed>=entry.source.clip.length))
                {
                    entry.source.Stop();
                    _previewAudioEntries.RemoveAt(index);
                    continue;
                }
                if(entry.source.loop) embeddedElapsed=Mathf.Repeat(embeddedElapsed,entry.source.clip.length);
                if(seek || entry.paused || Mathf.Abs(entry.source.time-embeddedElapsed)>2f/fps)
                    entry.source.timeSamples=Mathf.Clamp(Mathf.RoundToInt(embeddedElapsed*entry.source.clip.frequency),0,entry.source.clip.samples-1);
                if(playFrame && entry.paused){ entry.source.UnPause(); entry.paused=false; }
                else if(!playFrame && !entry.paused){ entry.source.Pause(); entry.paused=true; }
                continue;
            }
            // 此声音从触发帧起已过去的秒数。
            float elapsed = (frame - entry.effect.keyNumber) / fps;
            if (entry.source == null || !fxAndSoundList.Contains(entry.effect) || entry.effect.keyNumber>GetPreviewContentEndFrame() || entry.effect.audioClip == null ||
                entry.source.clip != entry.effect.audioClip || elapsed < 0f || elapsed >= entry.effect.audioClip.length)
            {
                if (entry.source != null) { entry.source.Stop(); DestroyImmediate(entry.source); }
                _previewAudioEntries.RemoveAt(index);
            }
        }
        // 原效果列表下标，同帧多个声音分别创建预览声源。
        for (int index = 0; index < fxAndSoundList.Count; index++)
        {
            // 原声音事件引用；结束帧哨兵不决定音频显示或单次播放时长。
            Global.FxAndSound effect = fxAndSoundList[index];
            if (effect == null || effect.audioClip == null) continue;
            // 正式END或Complete之后的新声音不因编辑器保留尾迹而补发。
            if(effect.keyNumber>GetPreviewContentEndFrame()) continue;
            // 当前帧相对声音触发帧的秒数，负数表示尚未触发。
            float elapsed = (frame - effect.keyNumber) / fps;
            if (elapsed < 0f || elapsed >= effect.audioClip.length) continue;
            // 按原对象身份找到现有声源，同资源的不同事件不共享时钟。
            PreviewAudioEntry entry = null;
            // 当前已绑定事件的列表下标。
            for (int voiceIndex = 0; voiceIndex < _previewAudioEntries.Count; voiceIndex++)
            {
                if (_previewAudioEntries[voiceIndex].effect == effect) { entry = _previewAudioEntries[voiceIndex]; break; }
            }
            if (entry == null)
            {
                entry = new PreviewAudioEntry { effect = effect, source = _previewAudioObject.AddComponent<AudioSource>() };
                entry.source.playOnAwake = false;
                entry.source.spatialBlend = 0f;
                entry.source.loop = false;
                entry.source.clip = effect.audioClip;
                entry.source.volume = Mathf.Clamp01(effect.baseVolume);
                entry.source.Play();
                _previewAudioEntries.Add(entry);
                entry.source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(elapsed * effect.audioClip.frequency), 0, effect.audioClip.samples - 1);
            }
            else if (seek || Mathf.Abs(entry.source.time - elapsed) > 2f / fps)
            {
                entry.source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(elapsed * effect.audioClip.frequency), 0, effect.audioClip.samples - 1);
            }
            entry.source.volume = Mathf.Clamp01(effect.baseVolume);
            if (!playFrame && !entry.paused) { entry.source.Pause(); entry.paused = true; }
            else if (playFrame && entry.paused) { entry.source.UnPause(); entry.paused = false; }
        }
    }

    /// <summary>停止并销毁全部隐藏声音对象，不清理正式场景的任何AudioSource。</summary>
    private void DestroyPreviewAudioSource()
    {
        ClearPreviewAudioEntries();
        if (_previewAudioObject != null) DestroyImmediate(_previewAudioObject);
        _previewAudioObject = null;
        as1 = null;
    }
}
