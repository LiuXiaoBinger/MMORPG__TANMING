using System.Collections.Generic;
using Ethan.ActionEditor.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>声音独立轨道的真实长度波形及音频采样点选择；仅影响制作界面，不把完整长度写成事件区间。</summary>
public partial class SkillEditorWindow
{
    /// <summary>当前窗口的真实音频包络缓存，关闭窗口或资源变化后释放。</summary>
    private readonly Dictionary<AudioClip, SkillEditorAudioWaveform> _soundWaveforms = new Dictionary<AudioClip, SkillEditorAudioWaveform>();
    /// <summary>声音显示行的已占用区间末尾帧，使用音频真实长度而非点事件结束帧。</summary>
    private readonly List<float> _soundLaneEnds = new List<float>();
    /// <summary>Alt 点选的原声音事件引用；用引用身份防止插入或删除事件后选点串到另一声音。</summary>
    private Global.FxAndSound _soundAlignmentEvent;
    /// <summary>Alt 点选时的真实音频身份，替换音频后原选点不继续生效。</summary>
    private AudioClip _soundAlignmentClip;
    /// <summary>所选音频采样点距声音开始的秒数，编辑窗口非持久状态。</summary>
    private float _soundAlignmentSeconds;

    /// <summary>清空只读波形会话缓存并释放选点；导入更新及窗口关闭时不留下旧声音的峰值。</summary>
    private void ClearSoundWaveforms()
    {
        _soundWaveforms.Clear();
        // 同一项目变更及窗口关闭链清除特效只读估算，防止重导后显示旧资源时长。
        ClearEffectDurationCache();
        // 同GUID重导声音或特效后，下一次重绘重新计算预览尾帧；原技能END不变。
        _previewEndFrameCache=-1;
        _soundAlignmentEvent = null;
        _soundAlignmentClip = null;
        Repaint();
    }

    /// <summary>按真实音频身份读取或复用包络缓存，读取失败同样缓存以避免每帧刷错误。</summary>
    /// <param name="clip">原声音引用；空引用没有可缓存的 PCM。</param>
    /// <returns>当前声音的真实包络，空音频返回空引用。</returns>
    private SkillEditorAudioWaveform GetSoundWaveform(AudioClip clip)
    {
        if (clip == null) return null;
        // 当前音频会话缓存，不使用文件名推测声音身份。
        SkillEditorAudioWaveform waveform;
        if (_soundWaveforms.TryGetValue(clip, out waveform))
        {
            if (waveform.Samples == clip.samples && waveform.Frequency == clip.frequency && waveform.Channels == clip.channels && waveform.LoadState == clip.loadState)
                return waveform;
            _soundWaveforms.Remove(clip);
        }
        // 窗口最多缓存六十四个已访问声音，限制频繁切配置后的缓存积累。
        if (_soundWaveforms.Count >= 64) _soundWaveforms.Clear();
        waveform = SkillEditorAudioWaveform.Read(clip);
        _soundWaveforms.Add(clip, waveform);
        return waveform;
    }

    /// <summary>计算音频完整显示末尾，以真实 PCM 时长换算当前配置 FPS；不改变技能 END 或原末帧。</summary>
    /// <param name="effect">原声音事件，只读其开始帧与音频完整长度。</param>
    /// <returns>纯显示使用的末尾帧，空音频仍有一个可选帧。</returns>
    private float SoundDisplayEnd(Global.FxAndSound effect)
    {
        if (effect == null) return 0f;
        if (effect.audioClip == null) return effect.keyNumber + 1f;
        if (effect.audioClip.length <= 0f) return effect.keyNumber + 1f;
        return effect.keyNumber + effect.audioClip.length * GetPreviewFrameRate();
    }

    /// <summary>完整声音所需的水平滚动范围；隐藏声音轨道时不额外撑开视口。</summary>
    /// <returns>声音可见时的纯显示末尾帧，绝不参与 GetTotalFrames 或技能状态时长。</returns>
    private int SoundTimelineDisplayEnd()
    {
        // 读取本轮实际制作行布局，只检查声音显示状态，不改原布局资产。
        List<TrkInfo> tracks = BuildTracks();
        // 声音轨道是否展开；旧布局恢复显示的临时制作行也参与此判断。
        bool visible = false;
        // 当前显示轨道的零基下标。
        for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
        {
            if (tracks[trackIndex].def.type == Global.TrackType.Sound && tracks[trackIndex].expanded)
            {
                visible = true;
                break;
            }
        }
        if (!visible) return 0;
        // 本轮所有可见声音的最大真实显示末尾，单位当前时间轴帧。
        float maximum = 0f;
        // 原共享列表零基下标，筛选后仍保持原事件身份。
        for (int index = 0; index < fxAndSoundList.Count; index++)
        {
            if (BuiltinTrackRegistry.IsVisibleInSoundTrack(fxAndSoundList[index])) maximum = Mathf.Max(maximum, SoundDisplayEnd(fxAndSoundList[index]));
        }
        return Mathf.CeilToInt(maximum);
    }

    /// <summary>按照声音真实显示长度分配独立行，让长声音和后续声音重叠时仍可分别拖动。</summary>
    /// <param name="lanes">注册表复用的原事件下标到显示行号字典，本函数只更新显示映射。</param>
    /// <returns>同一个显示字典，事件及结束帧均保持原值。</returns>
    private Dictionary<int, int> AssignSoundTimelineLanes(Dictionary<int, int> lanes)
    {
        lanes.Clear();
        _soundLaneEnds.Clear();
        // 原粒子与声音列表的零基下标，用该下标保持选择、复制和删除一致。
        for (int index = 0; index < fxAndSoundList.Count; index++)
        {
            // 当前原事件引用，只有声音占用本轨道显示行。
            Global.FxAndSound effect = fxAndSoundList[index];
            if (!BuiltinTrackRegistry.IsVisibleInSoundTrack(effect)) continue;
            // 当前事件分配的行号，负一表示暂未找到空行。
            int assigned = -1;
            // 当前声音行的零基下标，只接受不与既有显示包络重叠的行。
            for (int lane = 0; lane < _soundLaneEnds.Count; lane++)
            {
                if (_soundLaneEnds[lane] <= effect.keyNumber)
                {
                    assigned = lane;
                    _soundLaneEnds[lane] = SoundDisplayEnd(effect);
                    break;
                }
            }
            if (assigned < 0)
            {
                assigned = _soundLaneEnds.Count;
                _soundLaneEnds.Add(SoundDisplayEnd(effect));
            }
            lanes[index] = assigned;
        }
        return lanes;
    }

    /// <summary>绘制完整声音块、真实包络、触发边界及选点；拖动沿原公共事务，不增加音频裁剪字段。</summary>
    /// <param name="row">滚动内容空间的轨道行区域，横坐标零点为配置第零帧。</param>
    /// <param name="lanes">原共享列表下标到显示行号的只读映射。</param>
    /// <param name="laneCount">本声音轨道实际显示行数，至少一行。</param>
    private void DrawSoundTimeline(Rect row, Dictionary<int, int> lanes, int laneCount)
    {
        // 当前轨道每条显示行的高度，单位窗口像素。
        float laneHeight = row.height / Mathf.Max(1, laneCount);
        // 当前 GUI 事件，消费鼠标后不再让其他轨道处理。
        Event input = Event.current;
        // 当前时间轴每秒像素数，显式 FPS 与横向缩放共同决定波形长度。
        float pixelsPerSecond = pixelsPerFrame * GetPreviewFrameRate();
        // 原共享事件零基下标，只绘制独立声音内容。
        for (int index = 0; index < fxAndSoundList.Count; index++)
        {
            // 当前原声音事件，渲染和拖动都使用同一引用。
            Global.FxAndSound effect = fxAndSoundList[index];
            if (!BuiltinTrackRegistry.IsVisibleInSoundTrack(effect)) continue;
            // 此事件已经分配的声音显示行号，映射缺失时回到首行。
            int lane;
            if (!lanes.TryGetValue(index, out lane)) lane = 0;
            // 原始完整声音块的像素矩形，不写事件 endKeyNumber。
            Rect block = new Rect(row.x + effect.keyNumber * pixelsPerFrame, row.y + lane * laneHeight + 2f,
                Mathf.Max(8f, (SoundDisplayEnd(effect) - effect.keyNumber) * pixelsPerFrame), laneHeight - 4f);
            // 选中颜色仅影响显示，不能修改实际声音参数。
            Color color = C_SND;
            if (selType == SelType.Sound && selIdx == index) color = Color.Lerp(color, Color.white, .3f);
            EditorGUI.DrawRect(block, color);
            if (selType == SelType.Sound && selIdx == index) DrawOutline(block, Color.white);
            EditorGUI.DrawRect(new Rect(block.x, block.y, 2f, block.height), Color.white);
            // 当前音频缓存；空声音块仍可选中并补资源。
            SkillEditorAudioWaveform waveform = GetSoundWaveform(effect.audioClip);
            if (waveform != null && waveform.IsReadable) DrawSoundWaveform(block, waveform, pixelsPerSecond);
            // 音频名保持英文资源名；读取失败时轨道显示明确的不可读状态。
            string label = SkillEditorLocalization.Text("No AudioClip", "未指定音频");
            if (effect.audioClip != null) label = effect.audioClip.name + "  " + effect.audioClip.length.ToString("F3") + "s";
            if (waveform != null && !waveform.IsReadable) label += SkillEditorLocalization.Text(" (waveform unavailable)", "（波形不可读）");
            GUI.Label(new Rect(block.x + 4f, block.y, block.width - 8f, 14f), label, TSty.Item);
            if (input.type == EventType.MouseDown && input.button == 0 && block.Contains(input.mousePosition))
            {
                selType = SelType.Sound;
                selIdx = index;
                if (input.alt && waveform != null && waveform.IsReadable)
                {
                    _soundAlignmentEvent = effect;
                    _soundAlignmentClip = effect.audioClip;
                    // 先映射到源音频采样帧再换回秒；选点以 PCM 采样精度记录，而非整数时间轴帧。
                    int sample = Mathf.Clamp(Mathf.RoundToInt((input.mousePosition.x - block.x) / pixelsPerSecond * waveform.Frequency), 0, waveform.Samples - 1);
                    _soundAlignmentSeconds = (float)sample / waveform.Frequency;
                    // 当前所选声音采样点在技能时间轴中的整数帧，仅界内定位播放头。
                    int target = effect.keyNumber + Mathf.RoundToInt(_soundAlignmentSeconds * GetPreviewFrameRate());
                    if (target <= Mathf.Max(GetTotalFrames(),GetPreviewEndFrame())) SeekPreviewFrame(target);
                    else SetPreviewPlaying(false);
                    Repaint();
                }
                else StartDrag(SelType.Sound, index, effect.keyNumber, effect.endKeyNumber);
                input.Use();
            }
            if (input.type == EventType.ContextClick && block.Contains(input.mousePosition))
            {
                ShowEventContextMenu(GetDefBySel(SelType.Sound), index, effect.keyNumber);
                input.Use();
            }
            if (ReferenceEquals(_soundAlignmentEvent, effect) && _soundAlignmentClip == effect.audioClip)
                EditorGUI.DrawRect(new Rect(block.x + _soundAlignmentSeconds * pixelsPerSecond, block.y, 1f, block.height), Color.yellow);
        }
    }

    /// <summary>每个可见像素列合并真实 PCM 包络极值，缩放后仍可看见挥刀或撞击瞬态。</summary>
    /// <param name="block">完整音频块的滚动内容矩形。</param>
    /// <param name="waveform">成功读取的真实波形缓存，PCM 振幅不归一化。</param>
    /// <param name="pixelsPerSecond">当前时间轴横向比例，单位像素每秒。</param>
    private void DrawSoundWaveform(Rect block, SkillEditorAudioWaveform waveform, float pixelsPerSecond)
    {
        if (Event.current.type != EventType.Repaint || pixelsPerSecond <= 0f) return;
        // 只绘制当前横向视口附近的列，长音频不会每帧遍历整个滚动内容宽度。
        float beginX = Mathf.Max(block.x, timelineScrollPos.x);
        // 窗口宽度作为视口上限，实际 GUI 滚动裁剪负责属性面板等边缘裁切。
        float endX = Mathf.Min(block.x + waveform.DurationSeconds * pixelsPerSecond, timelineScrollPos.x + position.width);
        // 波形中心线避开十四像素名称栏，剩余区域显示负一到一的真实振幅。
        float centerY = block.y + 14f + Mathf.Max(0f, block.height - 14f) * .5f;
        // 半高振幅比例，空白高度不足时至少留两像素辨别瞬态。
        float amplitudeHeight = Mathf.Max(2f, (block.height - 14f) * .5f - 1f);
        // 当前可见包络像素列的内容横坐标，按单像素采样。
        for (float x = beginX; x < endX; x += 1f)
        {
            // 当前显示列覆盖的最小真实振幅。
            float minimum;
            // 当前显示列覆盖的最大真实振幅。
            float maximum;
            waveform.GetEnvelope((x - block.x) / pixelsPerSecond, (x + 1f - block.x) / pixelsPerSecond, out minimum, out maximum);
            EditorGUI.DrawRect(new Rect(x, centerY - Mathf.Clamp(maximum, -1f, 1f) * amplitudeHeight,
                1f, Mathf.Max(1f, (Mathf.Clamp(maximum, -1f, 1f) - Mathf.Clamp(minimum, -1f, 1f)) * amplitudeHeight)), new Color(.04f, .2f, .26f, .95f));
        }
        // 源音频绝对主峰在完整块内的像素位置，以样本时间而非桶位置绘制。
        float peakX = block.x + waveform.PeakSeconds * pixelsPerSecond;
        EditorGUI.DrawRect(new Rect(peakX, block.y + 14f, 1f, Mathf.Max(0f, block.height - 14f)), new Color(1f, .75f, .2f));
    }

    /// <summary>显示音频真实长度、主峰和选点语义，并为不可读或更新后的音频提供只读刷新入口。</summary>
    /// <param name="effect">当前选中的原声音事件，显示不会修改触发帧、结束帧或声音参数。</param>
    private void DrawSoundAlignmentInspector(Global.FxAndSound effect)
    {
        if (effect == null || effect.audioClip == null) return;
        // 当前选中音频的真实 PCM 缓存，只读资源。
        SkillEditorAudioWaveform waveform = GetSoundWaveform(effect.audioClip);
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Full audio length", "音频完整时长"), waveform.DurationSeconds.ToString("F3") + " s");
        if (!waveform.IsReadable) EditorGUILayout.HelpBox(waveform.Error, MessageType.Warning);
        else
        {
            EditorGUILayout.LabelField(SkillEditorLocalization.Text("Strongest peak", "绝对主峰"), waveform.PeakSeconds.ToString("F4") + " s / " + waveform.PeakSample + SkillEditorLocalization.Text(" sample", " 采样帧"));
            if (ReferenceEquals(_soundAlignmentEvent, effect) && _soundAlignmentClip == effect.audioClip)
                EditorGUILayout.LabelField(SkillEditorLocalization.Text("Picked point", "所选音频点"), _soundAlignmentSeconds.ToString("F4") + " s");
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Drag the waveform to shift its trigger. Alt-click to pick a sound point. Alignment uses the picked point, or the strongest peak. The full waveform does not create an event interval; pitch randomization may change actual timing.", "拖动波形可平移触发帧；Alt 点击选择音频点。对齐使用选点，未选点时使用主峰。完整波形不会生成事件区间；随机音高会改变实际听到的峰值时间。"), MessageType.Info);
        }
        if (GUILayout.Button(SkillEditorLocalization.Text("Refresh waveform / reset picked point", "刷新波形／清除选点"))) ClearSoundWaveforms();
    }

    /// <summary>返回选中声音要对齐的源音频时点：有效 Alt 选点优先，否则采用真实 PCM 主峰。</summary>
    /// <param name="effect">要对齐的原声音事件，资源替换后旧选点自动失效。</param>
    /// <param name="seconds">音频内部相对开始秒数，仅本次编辑对齐使用，未读取成功时为零。</param>
    /// <returns>是否存在可用的真实 PCM 对齐时点；静音没有主峰，但允许用户显式选点。</returns>
    private bool TryGetSoundAlignmentSeconds(Global.FxAndSound effect, out float seconds)
    {
        seconds = 0f;
        if (effect == null || effect.audioClip == null) return false;
        // 当前声音的真实波形，读取失败不能假装主峰位于第零秒。
        SkillEditorAudioWaveform waveform = GetSoundWaveform(effect.audioClip);
        if (!waveform.IsReadable) return false;
        if (ReferenceEquals(_soundAlignmentEvent, effect) && _soundAlignmentClip == effect.audioClip)
        {
            seconds = _soundAlignmentSeconds;
            return true;
        }
        if (Mathf.Abs(waveform.PeakAmplitude) <= 0f) return false;
        seconds = waveform.PeakSeconds;
        return true;
    }
}
