using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>沿角色当前导航折线等距放置 RO 路标，复用预制体里的样式和已创建的图标。</summary>
public sealed class MinimapRouteTrail
{
    /// <summary>序列化在底图下的方向标样板。</summary>
    private readonly Image _template;
    /// <summary>随导航结束或超出显示范围而回收的方向标。</summary>
    private readonly List<Image> _marks = new List<Image>();
    /// <summary>当前导航组件剩余转折点，单位为世界米。</summary>
    private readonly List<Vector3> _points = new List<Vector3>();
    /// <summary>相邻箭头之间的画布像素间距。</summary>
    private const float Spacing = 16f;

    /// <summary>绑定随窗口销毁的图标样板。</summary><param name="template">默认隐藏的橙色箭头。</param>
    public MinimapRouteTrail(Image template) { _template = template; }

    /// <summary>裁去圆形视口外的方向标，转角处延续前一段未走完的间距。</summary>
    /// <param name="role">当前追踪玩家。</param><param name="map">地图世界坐标投影。</param>
    /// <param name="scale">底图每个归一化纵向单位对应的画布像素。</param><param name="radius">圆形视口半径。</param>
    public void Refresh(PlayerEntity role, MinMapHelp map, float scale, float radius)
    {
        // 已使用的图标数量；本帧多出的旧图标统一隐藏。
        int used = 0;
        _points.Clear();
        if (role != null && role.NavComp != null) role.NavComp.CopyRemainingWaypoints(_points);
        if (_points.Count > 0)
        {
            // 画布局部坐标以制图中心为零，角色始终处于视口中心。
            Vector2 origin = map.WorldToMap(role.Position) * scale;
            Vector2 start = origin;
            float next = Spacing;
            for (int index = 0; index < _points.Count; index++)
            {
                // 当前道路的终点、长度和单位方向。
                Vector2 end = map.WorldToMap(_points[index]) * scale;
                Vector2 delta = end - start;
                float length = delta.magnitude;
                if (length < 0.001f) continue;
                Vector2 direction = delta / length;
                while (next <= length)
                {
                    // 标记落在真实折线上，只为当前能看到的范围取出样板。
                    Vector2 point = start + direction * next;
                    if ((point - origin).sqrMagnitude <= radius * radius)
                    {
                        if (used == _marks.Count) _marks.Add(Object.Instantiate(_template, _template.transform.parent));
                        Image mark = _marks[used++];
                        mark.gameObject.SetActive(true);
                        mark.rectTransform.anchoredPosition = point;
                        mark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180f);
                    }
                    next += Spacing;
                }
                next -= length;
                start = end;
            }
        }
        for (int index = used; index < _marks.Count; index++) _marks[index].gameObject.SetActive(false);
    }
}
