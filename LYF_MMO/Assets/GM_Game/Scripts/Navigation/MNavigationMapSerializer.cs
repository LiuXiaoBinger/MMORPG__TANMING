using System.IO;
using System.Text;

/// <summary>公共.navmesh.bytes文件的两端共用读写入口，仅依赖.NET基础库；不负责算路、地图管理或网络同步。</summary>
public static class MNavigationMapSerializer
{
    /// <summary>文件头的LYFN标记，BinaryWriter按小端顺序写入四字节。</summary>
    private const int FileMagic = 0x4E46594C;

    /// <summary>当前文件布局版本，修改布局后必须同步两端源码并更新该值。</summary>
    private const int FileVersion = 1;

    /// <summary>将场景名、角色类型、世界顶点、三角形和区域写成一份两端共用的二进制文件。</summary>
    /// <param name="filePath">目标文件路径，父目录由导出工具或调用方提前创建。</param>
    /// <param name="data">本次导出的单个场景三角网格，数组顺序保持Unity烘焙输出。</param>
    public static void Save(string filePath, MNavigationMapData data)
    {
        if (data.Indices.Length % 3 != 0)
        {
            throw new InvalidDataException("导航三角形索引数量必须是三的倍数。");
        }

        if (data.Areas.Length != data.Indices.Length / 3)
        {
            throw new InvalidDataException("导航区域数量必须与三角形数量一致。");
        }

        // 当前文件写入器由本次保存持有，退出作用域时关闭并刷新文件。
        using (BinaryWriter writer = new BinaryWriter(File.Create(filePath), Encoding.UTF8))
        {
            writer.Write(FileMagic);
            writer.Write(FileVersion);
            writer.Write(data.SceneName);
            writer.Write(data.AgentTypeID);
            writer.Write(data.Vertices.Length);
            writer.Write(data.Areas.Length);

            // 顶点序号对应后续三角形索引，逐点原样保存三个世界坐标分量。
            for (int vertexIndex = 0; vertexIndex < data.Vertices.Length; vertexIndex++)
            {
                writer.Write(data.Vertices[vertexIndex].X);
                writer.Write(data.Vertices[vertexIndex].Y);
                writer.Write(data.Vertices[vertexIndex].Z);
            }

            // 索引序号按每三个一组写入，保留原三角形绕序和重复顶点。
            for (int index = 0; index < data.Indices.Length; index++)
            {
                writer.Write(data.Indices[index]);
            }

            // 区域序号与三角形序号一一对应，供后续服务端查询区分地形区域。
            for (int areaIndex = 0; areaIndex < data.Areas.Length; areaIndex++)
            {
                writer.Write(data.Areas[areaIndex]);
            }
        }
    }

    /// <summary>从公共文件恢复三角网格，两端使用同一字段顺序，不依赖Unity资产反序列化。</summary>
    /// <param name="filePath">由编辑器导出的.navmesh.bytes文件路径。</param>
    /// <returns>保留文件内世界坐标、三角形索引和区域编号的地图数据。</returns>
    public static MNavigationMapData Load(string filePath)
    {
        // 本次读取器独占一次加载过程，完成或读取失败均自动关闭文件。
        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8))
        {
            if (reader.ReadInt32() != FileMagic)
            {
                throw new InvalidDataException("文件不是本项目导出的公共导航网格。");
            }

            if (reader.ReadInt32() != FileVersion)
            {
                throw new InvalidDataException("导航文件版本与当前读取代码不一致。");
            }

            // 返回调用方的单张地图，后续数组长度全部取自同一文件头。
            MNavigationMapData data = new MNavigationMapData();
            data.SceneName = reader.ReadString();
            data.AgentTypeID = reader.ReadInt32();
            // 顶点数用于分配XYZ坐标数组，不能是负数。
            int vertexCount = reader.ReadInt32();
            // 三角形数同时决定三倍索引数量和区域数组数量。
            int triangleCount = reader.ReadInt32();
            if (vertexCount < 0 || triangleCount < 0)
            {
                throw new InvalidDataException("导航顶点和三角形数量不能为负数。");
            }

            // 每个顶点12字节，每个三角形含12字节索引和4字节区域；使用长整数核对完整载荷。
            long payloadBytes = (long)vertexCount * 12 + (long)triangleCount * 16;
            if (reader.BaseStream.Length - reader.BaseStream.Position != payloadBytes)
            {
                throw new InvalidDataException("导航文件长度与顶点、三角形数量不符。");
            }

            data.Vertices = new MNavigationPoint[vertexCount];
            data.Indices = new int[checked(triangleCount * 3)];
            data.Areas = new int[triangleCount];

            // 按原序号恢复三维顶点，不把高度不同但XZ重合的点合并。
            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                data.Vertices[vertexIndex] = new MNavigationPoint(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            // 每个索引必须引用本文件的顶点，避免后续查询访问文件以外的顶点。
            for (int index = 0; index < data.Indices.Length; index++)
            {
                data.Indices[index] = reader.ReadInt32();
                if (data.Indices[index] < 0 || data.Indices[index] >= vertexCount)
                {
                    throw new InvalidDataException("导航三角形引用了不存在的顶点。");
                }
            }

            // 按三角形序号恢复区域，不把区域编号当成行走代价。
            for (int areaIndex = 0; areaIndex < triangleCount; areaIndex++)
            {
                data.Areas[areaIndex] = reader.ReadInt32();
            }

            return data;
        }
    }
}
