using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>在区域图上接收点击和滚轮。实现 UGUI 的指针接口，拖动由父级 ScrollRect 处理。</summary>
public sealed class MapSurfaceInput : MonoBehaviour, IPointerClickHandler, IScrollHandler
{
    /// <summary>点击坐标以整张地图中心为原点，一个单位等于底图高度。</summary>
    public event Action<Vector2> PointClicked;
    /// <summary>滚轮的正负方向代表放大或缩小一级。</summary>
    public event Action<float> ZoomRequested;

    /// <summary>把有效左键点击换成与场景辅助点共用的水平坐标。</summary>
    /// <param name="eventData">UGUI 的屏幕点击位置与画布相机。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !eventData.eligibleForClick) return;
        // 区域图使用居中轴点，实际绘制宽高随屏幕和缩放变化。
        RectTransform surface = (RectTransform)transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, eventData.position,
            eventData.pressEventCamera, out Vector2 localPoint))
        {
            PointClicked?.Invoke((localPoint - surface.rect.center) / surface.rect.height);
        }
    }

    /// <summary>把系统滚轮交给区域图调整比例尺。</summary>
    /// <param name="eventData">UGUI 本次滚轮的纵向增量。</param>
    public void OnScroll(PointerEventData eventData)
    {
        ZoomRequested?.Invoke(eventData.scrollDelta.y);
    }
}
