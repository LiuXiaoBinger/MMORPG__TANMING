using System;
using System.Collections.Generic;

/// <summary>共用地图的纯C#导航查询；沿用RO的PathFind/TrySetHeight入口，提供三角走廊A*与共享边路点。</summary>
public sealed class MNavigation
{
    /// <summary>世界单位中的接缝浮点误差上限，只连接近似共线且高度一致的边。</summary>
    private const float SeamTolerance = 0.001f;
    /// <summary>接缝候选边空间桶宽度，单位为世界米，不改变网格几何。</summary>
    private const float CellSize = 8f;
    /// <summary>当前地图的三角形和有向连接，地图切换时由管理器替换整个查询对象。</summary>
    private readonly MNavigationTriangle[] _triangles;
    /// <summary>A*各三角形从起点累计的路径代价。</summary>
    private readonly float[] _costs;
    /// <summary>A*各三角形的总代价g+h，供二叉堆排序。</summary>
    private readonly float[] _scores;
    /// <summary>本次查询首次进入各三角形的门户位置，沿RO按此位置累计路程，避免长细面的重心绕路代价。</summary>
    private readonly MNavigationPoint[] _positions;
    /// <summary>到达三角形的上一个三角形编号，-1表示未建立前驱。</summary>
    private readonly int[] _parents;
    /// <summary>各三角形前驱到自身所穿过的共享边。</summary>
    private readonly MNavigationPortal[] _parentPortals;
    /// <summary>已出开放表并完成扩展的三角形标记。</summary>
    private readonly bool[] _closed;
    /// <summary>三角形在开放堆中的下标，-1表示当前不在堆内。</summary>
    private readonly int[] _heapIndices;
    /// <summary>当前同步查询的最小代价堆，不跨线程并发使用同一查询对象。</summary>
    private readonly List<int> _openList = new List<int>();

    /// <summary>从公共地图创建运行查询，只在加载地图时构建邻接。</summary>
    /// <param name="mapData">已由公共读取器读取的世界坐标网格。</param>
    public MNavigation(MNavigationMapData mapData)
    {
        _triangles = new MNavigationTriangle[mapData.Indices.Length / 3];
        // 三个索引直接取原坐标，查询不修改公共数组或再次收缩角色半径。
        for (int i = 0; i < _triangles.Length; i++)
        {
            // 当前导出面缓存，重心用于接缝两侧判定及门户左右排序，不替代实际边界。
            MNavigationTriangle triangle = new MNavigationTriangle();
            triangle.A = mapData.Vertices[mapData.Indices[i * 3]];
            triangle.B = mapData.Vertices[mapData.Indices[i * 3 + 1]];
            triangle.C = mapData.Vertices[mapData.Indices[i * 3 + 2]];
            triangle.Center = new MNavigationPoint((triangle.A.X + triangle.B.X + triangle.C.X) / 3f,
                (triangle.A.Y + triangle.B.Y + triangle.C.Y) / 3f, (triangle.A.Z + triangle.B.Z + triangle.C.Z) / 3f);
            triangle.MinX = Math.Min(triangle.A.X, Math.Min(triangle.B.X, triangle.C.X));
            triangle.MaxX = Math.Max(triangle.A.X, Math.Max(triangle.B.X, triangle.C.X));
            triangle.MinY = Math.Min(triangle.A.Y, Math.Min(triangle.B.Y, triangle.C.Y));
            triangle.MaxY = Math.Max(triangle.A.Y, Math.Max(triangle.B.Y, triangle.C.Y));
            triangle.MinZ = Math.Min(triangle.A.Z, Math.Min(triangle.B.Z, triangle.C.Z));
            triangle.MaxZ = Math.Max(triangle.A.Z, Math.Max(triangle.B.Z, triangle.C.Z));
            _triangles[i] = triangle;
        }
        BuildConnections();
        _costs = new float[_triangles.Length];
        _scores = new float[_triangles.Length];
        _positions = new MNavigationPoint[_triangles.Length];
        _parents = new int[_triangles.Length];
        _parentPortals = new MNavigationPortal[_triangles.Length];
        _closed = new bool[_triangles.Length];
        _heapIndices = new int[_triangles.Length];
    }

    /// <summary>把世界点吸附到搜索半盒内的最近导航面，按三维距离保留最近高度层。</summary>
    /// <param name="point">输入世界点；成功时改为网格上的最近三维位置。</param>
    /// <param name="halfExtents">XYZ搜索盒半尺寸，单位为世界米。</param>
    public bool TrySetHeight(ref MNavigationPoint point, MNavigationPoint halfExtents)
    {
        // 临时承接最近面投影，失败不更改调用方位置。
        MNavigationPoint closest;
        if (FindNearestTriangle(point, halfExtents, out closest) < 0)
        {
            return false;
        }
        point = closest;
        return true;
    }

    /// <summary>同步查找完整可达路线；输出保留跨共享边交点，不把不可达目标强行追加到路线。</summary>
    /// <param name="start_pos">角色当前位置，保留高度用于选择所在层。</param>
    /// <param name="end_pos">请求目标位置，按搜索半盒吸附到最近导航面。</param>
    /// <param name="path">本次路点输出，调用时清空，失败保持为空。</param>
    /// <param name="halfExtents">起终点定位所用XYZ搜索盒半尺寸，RO常规为(4,8,4)。</param>
    public bool PathFind(MNavigationPoint start_pos, MNavigationPoint end_pos, List<MNavigationPoint> path, MNavigationPoint halfExtents)
    {
        path.Clear();
        // 起点投影保留实际导航面高度，避免跳到同XZ的其他层。
        MNavigationPoint firstPoint;
        // 目标投影用于完整可达判定和末路点。
        MNavigationPoint lastPoint;
        // 起点和终点所在的三角形编号。
        int startTriangle = FindNearestTriangle(start_pos, halfExtents, out firstPoint);
        int endTriangle = FindNearestTriangle(end_pos, halfExtents, out lastPoint);
        if (startTriangle < 0 || endTriangle < 0)
        {
            return false;
        }
        _openList.Clear();
        // 每次同步查询重置上次搜索缓存，不增加请求队列或等待状态。
        for (int i = 0; i < _triangles.Length; i++)
        {
            _costs[i] = float.MaxValue;
            _parents[i] = -1;
            _closed[i] = false;
            _heapIndices[i] = -1;
        }
        _costs[startTriangle] = 0f;
        _positions[startTriangle] = firstPoint;
        // RO以实际起点到目标的距离估算剩余路程，0.999系数沿用参考查询。
        _scores[startTriangle] = Distance(firstPoint, lastPoint) * 0.999f;
        PushOrUpdate(startTriangle);
        while (_openList.Count > 0)
        {
            // 当前总代价最小的待扩展三角形。
            int current = PopFirst();
            if (current == endTriangle)
            {
                BuildPath(startTriangle, endTriangle, firstPoint, lastPoint, path);
                return true;
            }
            _closed[current] = true;
            // 当前面所有真实共享边，不通过距离近似直连分离面。
            List<MNavigationPortal> portals = _triangles[current].Portals;
            for (int i = 0; i < portals.Count; i++)
            {
                // 本次尝试穿过的共享边以及另一侧的三角形。
                MNavigationPortal portal = portals[i];
                int next = portal.Triangle;
                if (_closed[next])
                {
                    continue;
                }
                // RO首次访问邻面时将当前节点水平投到共享边，避免三角剖分重心左右摆动导致错误绕路。
                if (_costs[next] == float.MaxValue)
                {
                    // 共享边沿世界X轴的长度分量，单位米。
                    float dx = portal.B.X - portal.A.X;
                    // 共享边沿世界Z轴的长度分量，单位米。
                    float dz = portal.B.Z - portal.A.Z;
                    // 水平最近点的边内比例；沿RO限制在0.1～0.9，查询节点不落在门户尖端。
                    float t = ((_positions[current].X - portal.A.X) * dx +
                        (_positions[current].Z - portal.A.Z) * dz) / (dx * dx + dz * dz);
                    _positions[next] = Lerp(portal.A, portal.B, Math.Max(0.1f, Math.Min(0.9f, t)));
                }
                // 门户节点之间的三维实际路程；终面另计进入门户后到投影目标的末段。
                float cost = _costs[current] + Distance(_positions[current], _positions[next]);
                // RO以门户节点到目标的三维直距估算剩余代价，单位米。
                float heuristic = Distance(_positions[next], lastPoint) * 0.999f;
                if (next == endTriangle)
                {
                    cost += Distance(_positions[next], lastPoint);
                    heuristic = 0f;
                }
                if (cost >= _costs[next])
                {
                    continue;
                }
                _costs[next] = cost;
                _scores[next] = cost + heuristic;
                _parents[next] = current;
                _parentPortals[next] = portal;
                PushOrUpdate(next);
            }
        }
        return false;
    }

    /// <summary>先按完全相同XYZ连接共享边，再处理分块边界的共线重叠段。</summary>
    private void BuildConnections()
    {
        // 焊接字典只消除完全重复的导出顶点，保留原网格及高度层。
        Dictionary<MNavigationPoint, int> vertices = new Dictionary<MNavigationPoint, int>(new MNavigationPointComparer());
        // 按较小顶点编号分组后查找相同边，避免64位拼接键的哈希聚集。
        Dictionary<int, List<MNavigationEdge>> edgeGroups = new Dictionary<int, List<MNavigationEdge>>();
        // 所有原三角边仅在初始化阶段保留。
        List<MNavigationEdge> edges = new List<MNavigationEdge>();
        for (int i = 0; i < _triangles.Length; i++)
        {
            // 当前面三个原始世界点，依次创建三条边。
            MNavigationPoint[] points = { _triangles[i].A, _triangles[i].B, _triangles[i].C };
            for (int j = 0; j < 3; j++)
            {
                // 当前边的两个焊接编号及世界端点。
                MNavigationEdge edge = new MNavigationEdge();
                edge.Triangle = i;
                edge.A = points[j];
                edge.B = points[(j + 1) % 3];
                edge.VertexA = GetVertex(vertices, edge.A);
                edge.VertexB = GetVertex(vertices, edge.B);
                if (edge.VertexA > edge.VertexB)
                {
                    // 只规范编号，不改变边的世界端点方向。
                    int swap = edge.VertexA;
                    edge.VertexA = edge.VertexB;
                    edge.VertexB = swap;
                }
                // 同一较小顶点编号关联的短列表。
                List<MNavigationEdge> group;
                if (!edgeGroups.TryGetValue(edge.VertexA, out group))
                {
                    group = new List<MNavigationEdge>();
                    edgeGroups.Add(edge.VertexA, group);
                }
                for (int k = 0; k < group.Count; k++)
                {
                    if (group[k].VertexB == edge.VertexB)
                    {
                        Connect(edge, group[k], edge.A, edge.B);
                        edge.Connected = true;
                        group[k].Connected = true;
                    }
                }
                group.Add(edge);
                edges.Add(edge);
            }
        }
        // 仅边界边参与空间查询，网格内部精确邻边不再两两比较。
        Dictionary<MNavigationPoint, List<int>> cells = new Dictionary<MNavigationPoint, List<int>>(new MNavigationPointComparer());
        // 每次当前边比较候选时去除跨多个桶产生的重复编号。
        int[] visited = new int[edges.Count];
        for (int i = 0; i < edges.Count; i++)
        {
            // 当前边界候选及覆盖的整数空间桶范围。
            MNavigationEdge edge = edges[i];
            if (edge.Connected)
            {
                continue;
            }
            int minX = (int)Math.Floor((Math.Min(edge.A.X, edge.B.X) - SeamTolerance) / CellSize);
            int maxX = (int)Math.Floor((Math.Max(edge.A.X, edge.B.X) + SeamTolerance) / CellSize);
            int minZ = (int)Math.Floor((Math.Min(edge.A.Z, edge.B.Z) - SeamTolerance) / CellSize);
            int maxZ = (int)Math.Floor((Math.Max(edge.A.Z, edge.B.Z) + SeamTolerance) / CellSize);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // 使用XYZ强类型键承载空间桶坐标，Y固定0，不量化实际导航高度。
                    MNavigationPoint key = new MNavigationPoint(x, 0f, z);
                    List<int> candidates;
                    if (!cells.TryGetValue(key, out candidates))
                    {
                        candidates = new List<int>();
                        cells.Add(key, candidates);
                    }
                    for (int k = 0; k < candidates.Count; k++)
                    {
                        // 当前已入桶的边编号；i+1作为本轮去重标记避免零初值冲突。
                        int candidate = candidates[k];
                        if (visited[candidate] == i + 1)
                        {
                            continue;
                        }
                        visited[candidate] = i + 1;
                        TryConnectSeam(edge, edges[candidate]);
                    }
                    candidates.Add(i);
                }
            }
        }
    }

    /// <summary>取得完全相同世界坐标的唯一编号。</summary>
    /// <param name="vertices">本地图初始化期间的顶点字典。</param>
    /// <param name="point">原三角网的世界坐标。</param>
    private static int GetVertex(Dictionary<MNavigationPoint, int> vertices, MNavigationPoint point)
    {
        // 首次出现时使用字典现有数量作为连续编号。
        int index;
        if (!vertices.TryGetValue(point, out index))
        {
            index = vertices.Count;
            vertices.Add(point, index);
        }
        return index;
    }

    /// <summary>在两面建立双向共享边。</summary>
    /// <param name="first">第一侧三角边。</param>
    /// <param name="second">第二侧三角边。</param>
    /// <param name="a">双方可穿越重叠段的第一端点。</param>
    /// <param name="b">双方可穿越重叠段的第二端点。</param>
    private void Connect(MNavigationEdge first, MNavigationEdge second, MNavigationPoint a, MNavigationPoint b)
    {
        _triangles[first.Triangle].Portals.Add(new MNavigationPortal { Triangle = second.Triangle, A = a, B = b });
        _triangles[second.Triangle].Portals.Add(new MNavigationPortal { Triangle = first.Triangle, A = a, B = b });
    }

    /// <summary>连接浮点误差内共线且高度一致的正长度重叠边，涵盖分块T形接缝。</summary>
    /// <param name="first">当前边界边。</param>
    /// <param name="second">同一空间桶中的另一条边界边。</param>
    private void TryConnectSeam(MNavigationEdge first, MNavigationEdge second)
    {
        if (first.Triangle == second.Triangle)
        {
            return;
        }
        // 第一边水平长度和第二边两端在其直线上的投影参数。
        float dx = first.B.X - first.A.X;
        float dz = first.B.Z - first.A.Z;
        float lengthSquared = dx * dx + dz * dz;
        if (lengthSquared < SeamTolerance * SeamTolerance)
        {
            return;
        }
        float t0 = ((second.A.X - first.A.X) * dx + (second.A.Z - first.A.Z) * dz) / lengthSquared;
        float t1 = ((second.B.X - first.A.X) * dx + (second.B.Z - first.A.Z) * dz) / lengthSquared;
        // 两端垂线偏差必须均小于毫米级误差，不能跨过障碍缺口。
        float cross0 = (second.A.X - first.A.X) * dz - (second.A.Z - first.A.Z) * dx;
        float cross1 = (second.B.X - first.A.X) * dz - (second.B.Z - first.A.Z) * dx;
        float toleranceSquared = SeamTolerance * SeamTolerance * lengthSquared;
        if (cross0 * cross0 > toleranceSquared || cross1 * cross1 > toleranceSquared)
        {
            return;
        }
        // 正长度重叠区间；仅点接触不能让角色穿过。
        float from = Math.Max(0f, Math.Min(t0, t1));
        float to = Math.Min(1f, Math.Max(t0, t1));
        if ((to - from) * Math.Sqrt(lengthSquared) <= SeamTolerance)
        {
            return;
        }
        // 双方重叠端点高度均需吻合，避免连接同XZ的桥上下层。
        MNavigationPoint a = Lerp(first.A, first.B, from);
        MNavigationPoint b = Lerp(first.A, first.B, to);
        float secondAHeight = second.A.Y + (second.B.Y - second.A.Y) * ((from - t0) / (t1 - t0));
        float secondBHeight = second.A.Y + (second.B.Y - second.A.Y) * ((to - t0) / (t1 - t0));
        if (Math.Abs(a.Y - secondAHeight) > SeamTolerance || Math.Abs(b.Y - secondBHeight) > SeamTolerance)
        {
            return;
        }
        // 两个三角形须位于公共边的两侧，拒绝重叠面或同侧贴边误连。
        float sideFirst = Area(first.A, first.B, _triangles[first.Triangle].Center);
        float sideSecond = Area(first.A, first.B, _triangles[second.Triangle].Center);
        if (sideFirst * sideSecond >= 0f)
        {
            return;
        }
        Connect(first, second, a, b);
    }

    /// <summary>在搜索半盒内找三维距离最近的三角投影。</summary>
    /// <param name="point">待定位的世界坐标。</param>
    /// <param name="halfExtents">各轴允许的最大坐标偏差。</param>
    /// <param name="closest">成功时的世界导航面位置。</param>
    private int FindNearestTriangle(MNavigationPoint point, MNavigationPoint halfExtents, out MNavigationPoint closest)
    {
        closest = point;
        // 当前最近面的编号及平方距离，避免每面进行开方。
        int nearest = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < _triangles.Length; i++)
        {
            // 当前面及水平投影后的真实面高度。
            MNavigationTriangle triangle = _triangles[i];
            if (triangle.MaxX < point.X - halfExtents.X || triangle.MinX > point.X + halfExtents.X)
            {
                continue;
            }
            if (triangle.MaxZ < point.Z - halfExtents.Z || triangle.MinZ > point.Z + halfExtents.Z)
            {
                continue;
            }
            if (triangle.MaxY < point.Y - halfExtents.Y || triangle.MinY > point.Y + halfExtents.Y)
            {
                continue;
            }
            MNavigationPoint candidate = ClosestPoint(point, triangle);
            if (Math.Abs(candidate.X - point.X) > halfExtents.X || Math.Abs(candidate.Z - point.Z) > halfExtents.Z)
            {
                continue;
            }
            if (Math.Abs(candidate.Y - point.Y) > halfExtents.Y)
            {
                continue;
            }
            float distance = DistanceSquared(point, candidate);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = i;
                closest = candidate;
            }
        }
        return nearest;
    }

    /// <summary>水平落在面内时按重心恢复高度，否则找三条边上的最近三维位置。</summary>
    /// <param name="point">待吸附世界坐标。</param>
    /// <param name="triangle">候选导航三角形。</param>
    private static MNavigationPoint ClosestPoint(MNavigationPoint point, MNavigationTriangle triangle)
    {
        // 面积比是XZ重心权重，保留斜面Y而非简单取重心高度。
        float denominator = Area(triangle.A, triangle.B, triangle.C);
        if (Math.Abs(denominator) > 0.000001f)
        {
            float a = Area(point, triangle.B, triangle.C) / denominator;
            float b = Area(triangle.A, point, triangle.C) / denominator;
            float c = 1f - a - b;
            if (a >= 0f && b >= 0f && c >= 0f)
            {
                return new MNavigationPoint(point.X, triangle.A.Y * a + triangle.B.Y * b + triangle.C.Y * c, point.Z);
            }
        }
        // 水平外点或退化面按三维线段投影选最近边。
        MNavigationPoint closest = ClosestEdge(point, triangle.A, triangle.B);
        MNavigationPoint candidate = ClosestEdge(point, triangle.B, triangle.C);
        if (DistanceSquared(point, candidate) < DistanceSquared(point, closest))
        {
            closest = candidate;
        }
        candidate = ClosestEdge(point, triangle.C, triangle.A);
        if (DistanceSquared(point, candidate) < DistanceSquared(point, closest))
        {
            closest = candidate;
        }
        return closest;
    }

    /// <summary>取得三维线段上的最近点。</summary>
    /// <param name="point">待投影世界坐标。</param>
    /// <param name="a">线段起点。</param>
    /// <param name="b">线段终点。</param>
    private static MNavigationPoint ClosestEdge(MNavigationPoint point, MNavigationPoint a, MNavigationPoint b)
    {
        // 三维投影参数限制在线段闭区间。
        float lengthSquared = DistanceSquared(a, b);
        if (lengthSquared == 0f)
        {
            return a;
        }
        float t = ((point.X - a.X) * (b.X - a.X) + (point.Y - a.Y) * (b.Y - a.Y) + (point.Z - a.Z) * (b.Z - a.Z)) / lengthSquared;
        return Lerp(a, b, Math.Max(0f, Math.Min(1f, t)));
    }

    /// <summary>向开放堆插入三角形或上浮其已降低的代价。</summary>
    /// <param name="triangle">代价刚被更新的三角形编号。</param>
    private void PushOrUpdate(int triangle)
    {
        // 已在堆中的面直接降低键值，新面追加到末端。
        int index = _heapIndices[triangle];
        if (index < 0)
        {
            index = _openList.Count;
            _openList.Add(triangle);
            _heapIndices[triangle] = index;
        }
        while (index > 0)
        {
            // 二叉堆父结点下标。
            int parent = (index - 1) / 2;
            if (_scores[_openList[parent]] <= _scores[triangle])
            {
                break;
            }
            _openList[index] = _openList[parent];
            _heapIndices[_openList[index]] = index;
            index = parent;
        }
        _openList[index] = triangle;
        _heapIndices[triangle] = index;
    }

    /// <summary>取出总代价最小的三角形并向下恢复开放堆。</summary>
    private int PopFirst()
    {
        // 保存堆顶和用于补位的末尾三角形编号。
        int result = _openList[0];
        int last = _openList[_openList.Count - 1];
        _openList.RemoveAt(_openList.Count - 1);
        _heapIndices[result] = -1;
        if (_openList.Count == 0)
        {
            return result;
        }
        int index = 0;
        while (index * 2 + 1 < _openList.Count)
        {
            // 在左右孩子中选择代价较小的一项。
            int child = index * 2 + 1;
            if (child + 1 < _openList.Count)
            {
                if (_scores[_openList[child + 1]] < _scores[_openList[child]])
                {
                    child++;
                }
            }
            if (_scores[last] <= _scores[_openList[child]])
            {
                break;
            }
            _openList[index] = _openList[child];
            _heapIndices[_openList[index]] = index;
            index = child;
        }
        _openList[index] = last;
        _heapIndices[last] = index;
        return result;
    }

    /// <summary>从A*前驱恢复走廊，漏斗提取转角后补齐各共享边交点高度。</summary>
    /// <param name="startTriangle">起点所在三角形。</param>
    /// <param name="endTriangle">终点所在三角形。</param>
    /// <param name="start">吸附后的起点。</param>
    /// <param name="end">吸附后的终点。</param>
    /// <param name="path">本次输出的世界路点。</param>
    private void BuildPath(int startTriangle, int endTriangle, MNavigationPoint start, MNavigationPoint end, List<MNavigationPoint> path)
    {
        // 从终点回溯的三角编号，翻转后即实际行走顺序。
        List<int> corridor = new List<int>();
        int current = endTriangle;
        while (current != startTriangle)
        {
            corridor.Add(current);
            current = _parents[current];
        }
        corridor.Reverse();
        // 左右共享边按前进方向排序；首尾用零宽门户表达起终点。
        List<MNavigationPoint> left = new List<MNavigationPoint>();
        List<MNavigationPoint> right = new List<MNavigationPoint>();
        left.Add(start);
        right.Add(start);
        current = startTriangle;
        for (int i = 0; i < corridor.Count; i++)
        {
            // 当前走廊门户和前进方向所用的两侧重心。
            MNavigationPortal portal = _parentPortals[corridor[i]];
            MNavigationPoint from = _triangles[current].Center;
            MNavigationPoint to = _triangles[corridor[i]].Center;
            if (Area(from, to, portal.A) < Area(from, to, portal.B))
            {
                left.Add(portal.A);
                right.Add(portal.B);
            }
            else
            {
                left.Add(portal.B);
                right.Add(portal.A);
            }
            current = corridor[i];
        }
        left.Add(end);
        right.Add(end);
        // 漏斗转角以及转角所在门户编号，供后续保留ALL_CROSSINGS。
        List<MNavigationPoint> corners = new List<MNavigationPoint>();
        List<int> cornerIndices = new List<int>();
        FindCorners(left, right, corners, cornerIndices);
        path.Add(start);
        int firstPortal = 1;
        for (int i = 1; i < corners.Count; i++)
        {
            // 这一段直线依次穿过的门户，交点Y由门户端点插值恢复。
            for (int j = firstPortal; j <= cornerIndices[i]; j++)
            {
                AddPoint(path, Crossing(corners[i - 1], corners[i], left[j], right[j]));
            }
            firstPortal = cornerIndices[i] + 1;
        }
        AddPoint(path, end);
    }

    /// <summary>沿RO直线路点职责用标准门户漏斗提取走廊转角，不生成样条或边中点锯齿。</summary>
    /// <param name="left">按走向排序的每个门户左端点。</param>
    /// <param name="right">按走向排序的每个门户右端点。</param>
    /// <param name="corners">输出起点、必要转角和终点。</param>
    /// <param name="indices">每个转角对应的门户编号。</param>
    private static void FindCorners(List<MNavigationPoint> left, List<MNavigationPoint> right, List<MNavigationPoint> corners, List<int> indices)
    {
        // 当前漏斗顶点以及左右约束射线的端点和门户编号。
        MNavigationPoint apex = left[0];
        MNavigationPoint portalLeft = apex;
        MNavigationPoint portalRight = apex;
        int apexIndex = 0;
        int leftIndex = 0;
        int rightIndex = 0;
        corners.Add(apex);
        indices.Add(0);
        for (int i = 1; i < left.Count; i++)
        {
            // 新门户两端用于收窄当前可见范围。
            MNavigationPoint nextLeft = left[i];
            MNavigationPoint nextRight = right[i];
            if (Area(apex, portalRight, nextRight) <= 0f)
            {
                if (SameHorizontal(apex, portalRight) || Area(apex, portalLeft, nextRight) > 0f)
                {
                    portalRight = nextRight;
                    rightIndex = i;
                }
                else
                {
                    corners.Add(portalLeft);
                    indices.Add(leftIndex);
                    apex = portalLeft;
                    apexIndex = leftIndex;
                    portalLeft = apex;
                    portalRight = apex;
                    leftIndex = apexIndex;
                    rightIndex = apexIndex;
                    i = apexIndex;
                    continue;
                }
            }
            if (Area(apex, portalLeft, nextLeft) >= 0f)
            {
                if (SameHorizontal(apex, portalLeft) || Area(apex, portalRight, nextLeft) < 0f)
                {
                    portalLeft = nextLeft;
                    leftIndex = i;
                }
                else
                {
                    corners.Add(portalRight);
                    indices.Add(rightIndex);
                    apex = portalRight;
                    apexIndex = rightIndex;
                    portalLeft = apex;
                    portalRight = apex;
                    leftIndex = apexIndex;
                    rightIndex = apexIndex;
                    i = apexIndex;
                }
            }
        }
        corners.Add(left[left.Count - 1]);
        indices.Add(left.Count - 1);
    }

    /// <summary>计算直线段与共享边的水平交点，并沿共享边插值实际导航高度。</summary>
    /// <param name="from">本段漏斗转角起点。</param>
    /// <param name="to">本段漏斗转角终点。</param>
    /// <param name="a">共享边第一端点。</param>
    /// <param name="b">共享边第二端点。</param>
    private static MNavigationPoint Crossing(MNavigationPoint from, MNavigationPoint to, MNavigationPoint a, MNavigationPoint b)
    {
        // 水平二维叉积解共享边插值参数，避免跨坡面用整段Y直插。
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        float ex = b.X - a.X;
        float ez = b.Z - a.Z;
        float denominator = dx * ez - dz * ex;
        if (Math.Abs(denominator) < 0.000001f)
        {
            if (DistanceSquared(a, to) < DistanceSquared(b, to))
            {
                return a;
            }
            return b;
        }
        float t = ((a.X - from.X) * dz - (a.Z - from.Z) * dx) / denominator;
        return Lerp(a, b, Math.Max(0f, Math.Min(1f, t)));
    }

    /// <summary>追加路点并去除连续重复端点，保留高度不同的点。</summary>
    /// <param name="path">当前输出路点。</param>
    /// <param name="point">新共享边交点或目的点。</param>
    private static void AddPoint(List<MNavigationPoint> path, MNavigationPoint point)
    {
        if (DistanceSquared(path[path.Count - 1], point) > 0.00000001f)
        {
            path.Add(point);
        }
    }

    /// <summary>判断水平端点是否重合，作为漏斗零宽门户判断。</summary>
    /// <param name="a">第一世界点。</param>
    /// <param name="b">第二世界点。</param>
    private static bool SameHorizontal(MNavigationPoint a, MNavigationPoint b)
    {
        return Math.Abs(a.X - b.X) < 0.00001f && Math.Abs(a.Z - b.Z) < 0.00001f;
    }

    /// <summary>计算XZ有向面积的相反数，与Detour门户漏斗的方向约定一致。</summary>
    /// <param name="a">面积基点。</param>
    /// <param name="b">第一条射线端点。</param>
    /// <param name="c">第二条射线端点。</param>
    private static float Area(MNavigationPoint a, MNavigationPoint b, MNavigationPoint c)
    {
        return (c.X - a.X) * (b.Z - a.Z) - (b.X - a.X) * (c.Z - a.Z);
    }

    /// <summary>按参数在线段上取得世界坐标，保留高度线性变化。</summary>
    /// <param name="a">线段起点。</param>
    /// <param name="b">线段终点。</param>
    /// <param name="t">从起点到终点的比例，通常为0～1。</param>
    private static MNavigationPoint Lerp(MNavigationPoint a, MNavigationPoint b, float t)
    {
        return new MNavigationPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
    }

    /// <summary>求三维距离平方，供最近面和去重比较。</summary>
    /// <param name="a">第一世界坐标。</param>
    /// <param name="b">第二世界坐标。</param>
    private static float DistanceSquared(MNavigationPoint a, MNavigationPoint b)
    {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
    }

    /// <summary>求三维世界距离，作为A*累计及启发代价。</summary>
    /// <param name="a">第一世界坐标。</param>
    /// <param name="b">第二世界坐标。</param>
    private static float Distance(MNavigationPoint a, MNavigationPoint b)
    {
        return (float)Math.Sqrt(DistanceSquared(a, b));
    }
}
