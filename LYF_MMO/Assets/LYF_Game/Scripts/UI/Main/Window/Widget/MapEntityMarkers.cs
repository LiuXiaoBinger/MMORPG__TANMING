using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>按 RO 的实体 UID 维护一块底图上的活动图标。小地图和区域图各持有自己的图层。</summary>
public sealed class MapEntityMarkers
{
    /// <summary>预制体内隐藏的图标样板，其父节点与当前底图完全重合。</summary>
    private readonly Image _template;
    /// <summary>按当前场景同一实体编号找到已创建的图标。</summary>
    private readonly Dictionary<ulong, Image> _markers = new Dictionary<ulong, Image>();
    /// <summary>从实体世界复制的原实体引用，复用列表以避免每帧分配。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();
    /// <summary>本次刷新前已有的标记，移除仍在场内的编号后即为待删除的图标。</summary>
    private readonly List<ulong> _previous = new List<ulong>();
    /// <summary>记录最近一轮仍然可见的场景实体。</summary>
    private readonly HashSet<ulong> _visible = new HashSet<ulong>();

    /// <summary>将标记列表绑定到该地图在正式预制体中配置的图标样板。</summary>
    /// <param name="template">隐藏的白色标记，运行中的副本复用其大小和形状。</param>
    public MapEntityMarkers(Image template)
    {
        _template = template;
    }

    /// <summary>将标记放在与底图共用的坐标系内，并回收死亡或离场实体的图标。</summary>
    /// <param name="map">当前场景的投影基准。</param>
    /// <param name="world">本客户端当前可见实体来源。</param>
    /// <param name="aspect">底图宽高比，小地图为一。</param>
    public void Refresh(MinMapHelp map, SceneWorldSystem world, float aspect)
    {
        _visible.Clear();
        if (world != null)
        {
            world.CopyEntities(_entities);
            for (int index = 0; index < _entities.Count; index++)
            {
                // 主角使用独立的方向箭头，其他图标只跟随已加载且可见的模型。
                MEntity entity = _entities[index];
                if (entity == world.LocalRole || entity.IsDead || entity.Model == null) continue;
                if (!entity.Model.IsLoaded || !entity.Model.GameObject.NativeObject.activeInHierarchy) continue;
                // 非角色类的特效和投射物不占据地图上的地标位置。
                Color32 ink;
                if (entity is NpcEntity) ink = new Color32(244, 192, 87, 255);
                else if (entity is MonsterEntity) ink = new Color32(225, 89, 73, 255);
                else if (entity is RoleEntity) ink = new Color32(77, 225, 209, 255);
                else continue;
                _visible.Add(entity.UID);
                if (!_markers.TryGetValue(entity.UID, out Image mark))
                {
                    mark = Object.Instantiate(_template, _template.transform.parent, false);
                    mark.color = ink;
                    mark.gameObject.SetActive(true);
                    _markers.Add(entity.UID, mark);
                }
                MinMapHelp.Place(mark.rectTransform, map.WorldToMap(entity.Position), aspect);
            }
        }
        _previous.Clear();
        _previous.AddRange(_markers.Keys);
        for (int index = 0; index < _previous.Count; index++)
        {
            // 不在本轮实体集合的编号已经离场、隐藏或死亡。
            ulong id = _previous[index];
            if (_visible.Contains(id)) continue;
            Object.Destroy(_markers[id].gameObject);
            _markers.Remove(id);
        }
    }
}
