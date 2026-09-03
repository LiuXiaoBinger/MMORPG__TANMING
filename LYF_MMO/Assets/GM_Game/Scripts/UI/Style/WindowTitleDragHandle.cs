using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在窗口标题栏上，拖动时移动指定的窗口本体。
/// </summary>
[DisallowMultipleComponent]
public class WindowTitleDragHandle : MonoBehaviour, IDragHandler
{
    [SerializeField, Header("拖动目标窗口")] private RectTransform _targetWindow;

    private Canvas _rootCanvas;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 未配置需要拖动的窗口时，不执行移动。
        if (_targetWindow == null)
        {
            return;
        }

        // eventData.delta 是屏幕像素位移。
        // Canvas 缩放后，除以 scaleFactor 转换为 UI 坐标位移。
        float scaleFactor = _rootCanvas == null ? 1f : _rootCanvas.scaleFactor;

        // 累加鼠标本帧的位移，保留鼠标点击标题栏时的相对位置，避免窗口跳到鼠标中心。
        _targetWindow.anchoredPosition += eventData.delta / scaleFactor;
        
    }

}
