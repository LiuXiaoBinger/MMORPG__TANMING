using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>沿RO管理器入口读取当前地图公共三角网，并在Unity与纯C#查询间转换坐标。</summary>
public sealed class MNavigationMgr
{
    /// <summary>当前进程唯一的地图查询入口，切换地图时替换查询实例。</summary>
    public static readonly MNavigationMgr singleton = new MNavigationMgr();
    /// <summary>沿RO路径查询使用的最近面搜索半盒，XYZ分别为4、8、4米。</summary>
    private readonly MNavigationPoint _halfExtents = new MNavigationPoint(4f, 8f, 4f);
    /// <summary>纯C#查询返回的临时路点，主线程同步查询期间复用。</summary>
    private readonly List<MNavigationPoint> _pathPoints = new List<MNavigationPoint>();
    /// <summary>当前已加载地图的查询器；创角等没有数据的场景为空。</summary>
    private MNavigation _cppNav;

    /// <summary>管理器仅由同名单例持有，不新建场景对象或独立更新循环。</summary>
    private MNavigationMgr() { }

    /// <summary>场景成功加载后读取同名文件；Windows Player从StreamingAssets随包数据读取。</summary>
    /// <param name="sceneName">已成功加载的Unity场景名称。</param>
    public void OnSceneLoaded(string sceneName)
    {
        _cppNav = null;
        _pathPoints.Clear();
        // 同一公共文件部署到StreamingAssets，运行时代码不访问开发机项目根目录。
        string filePath = Path.Combine(Application.streamingAssetsPath,
            "NavigationData", sceneName + ".navmesh.bytes");
        if (!File.Exists(filePath))
        {
            return;
        }
        _cppNav = new MNavigation(MNavigationMapSerializer.Load(filePath));
    }

    /// <summary>同步计算完整可达路线，保留查询器生成的世界高度与路点次序。</summary>
    /// <param name="start_pos">角色当前世界位置，单位米。</param>
    /// <param name="end_pos">本次目的世界位置，单位米。</param>
    /// <param name="path">调用方持有的Unity路线列表，查询前清空。</param>
    /// <returns>当前地图有数据且目标完整可达时为true。</returns>
    public bool TryGetPath(Vector3 start_pos, Vector3 end_pos, List<Vector3> path)
    {
        path.Clear();
        if (_cppNav == null)
        {
            return false;
        }
        if (!_cppNav.PathFind(new MNavigationPoint(start_pos.x, start_pos.y, start_pos.z),
            new MNavigationPoint(end_pos.x, end_pos.y, end_pos.z), _pathPoints, _halfExtents))
        {
            return false;
        }
        // 公共路点索引保持查询输出顺序，不把不可达原始终点额外追加到路线。
        for (int index = 0; index < _pathPoints.Count; index++)
        {
            // 当前世界路点，Unity与服务端保持相同XYZ轴及米制长度。
            MNavigationPoint point = _pathPoints[index];
            path.Add(new Vector3(point.X, point.Y, point.Z));
        }
        return path.Count > 0;
    }

    /// <summary>沿RO高度适配入口将世界位置吸附至搜索半盒内的最近可走面。</summary>
    /// <param name="pos">输入世界位置；成功时改成最近导航面上的XYZ，单位米。</param>
    /// <returns>当前地图存在搜索范围内的导航面时为true。</returns>
    public bool TrySetHeight(ref Vector3 pos)
    {
        if (_cppNav == null)
        {
            return false;
        }
        // 纯C#坐标副本仅用于接口边界，失败时保持原Unity位置。
        MNavigationPoint point = new MNavigationPoint(pos.x, pos.y, pos.z);
        if (!_cppNav.TrySetHeight(ref point, _halfExtents))
        {
            return false;
        }
        pos = new Vector3(point.X, point.Y, point.Z);
        return true;
    }

    /// <summary>沿RO地形射线入口使用项目现有Geometry层碰撞体，不依赖原生导航对象是否激活。</summary>
    /// <param name="start_pos">射线起点，世界单位为米。</param>
    /// <param name="end_pos">射线终点，世界单位为米。</param>
    /// <param name="res">命中的地图表面位置，失败时为零。</param>
    /// <returns>射线命中地图Geometry层时为true。</returns>
    public bool TryRaycastTerrain(Vector3 start_pos, Vector3 end_pos, out Vector3 res)
    {
        // 本次有限射线的方向与总长度，沿RO点击距离最多1000米。
        Vector3 direction = end_pos - start_pos;
        // Geometry层承载主城地形、道路和建筑，排除角色及UI碰撞体。
        int terrainMask = LayerMask.GetMask("Geometry");
        // 第一个地图表面命中结果，避免穿过前方建筑去点击后方地面。
        RaycastHit hit;
        if (Physics.Raycast(start_pos, direction.normalized, out hit,
            direction.magnitude, terrainMask, QueryTriggerInteraction.Ignore))
        {
            res = hit.point;
            return true;
        }
        res = Vector3.zero;
        return false;
    }
}
