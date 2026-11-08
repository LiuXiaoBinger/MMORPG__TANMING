using System.Collections.Generic;
using UnityEngine;

/// <summary>原MDrawCurve的60段三次贝塞尔连线，保留925预制体的四个控制点引用。</summary>
public sealed class MSceneMonsterLinkCurve : MonoBehaviour
{
    /// <summary>原预制体四个控制点，顺序决定曲线形状。</summary>
    public List<Transform> m_allPoints;
    /// <summary>原起点父节点。</summary>
    public Transform StartPosition;
    /// <summary>原终点父节点。</summary>
    public Transform EndPosition;
    /// <summary>原预制体中负责绘线的Renderer。</summary>
    private LineRenderer line;

    /// <summary>沿原实现以61个采样点画60段曲线。</summary>
    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 61;
    }

    /// <summary>保持原控制点维护公式与三米拱高，连接真实施加者和目标。</summary>
    /// <param name="startPosition">施加者原挂点世界坐标。</param>
    /// <param name="endPosition">目标原挂点世界坐标。</param>
    public void SetStartPosition(Vector3 startPosition, Vector3 endPosition)
    {
        StartPosition.position = startPosition;
        EndPosition.position = endPosition;
        m_allPoints[1].localPosition = StartPosition.position - EndPosition.position + new Vector3(0f, 3f, 0f);
        for (int index = 0; index <= 60; index++)
        {
            // 原三次贝塞尔当前采样比例及剩余权重。
            float t = index / 60f;
            // 起点一侧的剩余权重。
            float u = 1f - t;
            // 原四点按标准三次公式组合后的世界位置。
            Vector3 position = u * u * u * m_allPoints[0].position + 3f * u * u * t * m_allPoints[1].position;
            position += 3f * u * t * t * m_allPoints[2].position + t * t * t * m_allPoints[3].position;
            line.SetPosition(index, position);
        }
    }
}
