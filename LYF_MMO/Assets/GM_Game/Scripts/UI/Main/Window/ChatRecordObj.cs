using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>RO 风格的按住录音、滑出取消与限时结束控件。Unity EventSystem 要求实现四个指针回调接口。</summary>
public sealed class ChatRecordObj : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>预制体中的按住说话文本。</summary>
    [SerializeField] private TMP_Text _label;
    /// <summary>长按开始时由聊天控制器检查频道与麦克风。</summary>
    public event Func<bool> RecordStarted;
    /// <summary>停止时传入是否发送录音。</summary>
    public event Action<bool> RecordEnded;
    /// <summary>当前按下的指针，避免另一个触点结束本次录制。</summary>
    private int _pointer;
    /// <summary>本次按下起点，使用不受游戏变速影响的秒数。</summary>
    private float _downAt;
    /// <summary>本次录音开始后的秒数起点。</summary>
    private float _recordAt;
    /// <summary>当前按钮是否被持续按住。</summary>
    private bool _held;
    /// <summary>当前指针是否仍在发送区域内。</summary>
    private bool _inside;
    /// <summary>是否实际成功打开麦克风。</summary>
    private bool _recording;

    /// <summary>记录手势开始，一次只接受一个指针。</summary><param name="e">Unity 指针事件。</param>
    public void OnPointerDown(PointerEventData e)
    {
        if (_held) return;
        _held = true; _inside = true; _pointer = e.pointerId; _downAt = Time.realtimeSinceStartup;
    }

    /// <summary>松手时按按钮位置决定发送或取消。</summary><param name="e">完成手势的指针。</param>
    public void OnPointerUp(PointerEventData e) { if (_held && _pointer == e.pointerId) Finish(_inside); }
    /// <summary>滑回按钮恢复发送。</summary><param name="e">移入的指针。</param>
    public void OnPointerEnter(PointerEventData e) { if (_held && _pointer == e.pointerId) { _inside = true; RefreshLabel(); } }
    /// <summary>滑出按钮后松手取消。</summary><param name="e">移出的指针。</param>
    public void OnPointerExit(PointerEventData e) { if (_held && _pointer == e.pointerId) { _inside = false; RefreshLabel(); } }

    /// <summary>长按半秒打开录音，正式录满 30 秒自动结束。</summary>
    private void Update()
    {
        if (!_held) return;
        if (!_recording && Time.realtimeSinceStartup - _downAt >= 0.5f)
        {
            if (!_inside || RecordStarted == null || !RecordStarted.Invoke()) { Finish(false); return; }
            _recordAt = Time.realtimeSinceStartup; _recording = true; RefreshLabel();
        }
        if (_recording && Time.realtimeSinceStartup - _recordAt >= 30f) Finish(_inside);
    }
    /// <summary>收起输入区时清除手势并取消未完成录音。</summary>
    private void OnDisable() { Finish(false); }

    /// <summary>只结束实际开始的录音。</summary><param name="send">是否保留并发送。</param>
    private void Finish(bool send)
    {
        _held = false;
        if (_recording) { _recording = false; RecordEnded?.Invoke(send); }
        RefreshLabel();
    }

    /// <summary>显示当前手势含义。</summary>
    private void RefreshLabel()
    {
        if (_label == null) return;
        if (!_recording) _label.text = "按住说话";
        else if (_inside) _label.text = "松开发送，移出取消";
        else _label.text = "松开取消";
    }
}
