using System.IO;
using UnityEditor;
using UnityEngine.AI;

/// <summary>将单份已烘焙的Unity导航资产导出为两端共用三角网格，不重新烘焙或修改场景。</summary>
public static class MNavigationMapExporter
{
    /// <summary>临时注册所选资产并导出；仅在当前没有已加载导航网格时执行，避免混入其他地图或角色类型。</summary>
    /// <param name="sceneName">生成文件内记录的地图场景名，主城与各副本分别导出。</param>
    /// <param name="navMeshData">所选地图已烘焙的导航资产，使用资产保存的位置和旋转。</param>
    /// <param name="outputPath">公共导航文件的绝对路径，父目录不存在时创建。</param>
    /// <returns>本次写入文件的世界坐标三角网格，供调用方显示导出结果。</returns>
    public static MNavigationMapData Export(string sceneName, NavMeshData navMeshData, string outputPath)
    {
        // Unity的三角化接口读取全局导航世界；已有数据时不能准确按场景或角色类型筛选。
        if (NavMesh.CalculateTriangulation().indices.Length != 0)
        {
            throw new System.InvalidOperationException("当前已有导航网格，请先打开Boot或空场景，再选择地图导航资产导出，避免混入其他地图。");
        }

        // 只持有本次临时加载的实例，结束后移除；不卸载用户已有数据或保存任何场景。
        NavMeshDataInstance dataInstance = NavMesh.AddNavMeshData(navMeshData, navMeshData.position, navMeshData.rotation);
        try
        {
            if (!dataInstance.valid)
            {
                throw new System.InvalidOperationException("所选导航资产加载失败，请检查该资产是否已成功烘焙。");
            }

            // 此时全局只有所选资产，输出顶点已经处于资产烘焙后的世界坐标系。
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.indices.Length == 0)
            {
                throw new System.InvalidOperationException("所选导航资产没有可导出的三角形，请先完成地图烘焙。");
            }

            // 公共数据仅包含基础数值类型，服务端不需要引用Unity程序集。
            MNavigationMapData mapData = new MNavigationMapData();
            mapData.SceneName = sceneName;
            // Unity未公开NavMeshData的角色类型属性，在编辑器序列化边界读取资产实际烘焙设置。
            SerializedObject serializedData = new SerializedObject(navMeshData);
            mapData.AgentTypeID = serializedData.FindProperty("m_NavMeshBuildSettings.agentTypeID").intValue;
            mapData.Vertices = new MNavigationPoint[triangulation.vertices.Length];
            mapData.Indices = triangulation.indices;
            mapData.Areas = triangulation.areas;

            // 保留每个顶点的原始高度、顺序和重复项，不将上下层投影到同一平面。
            for (int vertexIndex = 0; vertexIndex < triangulation.vertices.Length; vertexIndex++)
            {
                // 此坐标直接来自Unity三角化结果，单位与主城或副本场景的世界单位一致。
                UnityEngine.Vector3 vertex = triangulation.vertices[vertexIndex];
                mapData.Vertices[vertexIndex] = new MNavigationPoint(vertex.x, vertex.y, vertex.z);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            MNavigationMapSerializer.Save(outputPath, mapData);
            return mapData;
        }
        finally
        {
            dataInstance.Remove();
        }
    }
}
