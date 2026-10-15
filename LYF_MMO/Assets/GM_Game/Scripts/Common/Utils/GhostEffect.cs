using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色残影（拖尾）特效组件：在角色移动或攻击时把当前网格烘焙成半透明鬼影，
/// 并按剩余时间线性衰减菲涅尔、透明度与主图强度，用于表现高速动作的视觉残留。
/// </summary>
public class GhostEffect : MonoBehaviour
{
    /// <summary>残影 Shader 中菲涅尔强度属性的缓存ID，避免字符串查找。</summary>
    private static readonly int m_IdFresnelStrength = Shader.PropertyToID("_FresnelStrength");

    /// <summary>残影 Shader 中菲涅尔颜色强度属性的缓存ID。</summary>
    private static readonly int m_IdFresnelColorStrength = Shader.PropertyToID("_FresnelColorStrength");

    /// <summary>残影 Shader 中主图强度属性的缓存ID。</summary>
    private static readonly int m_IdMainTexStrength = Shader.PropertyToID("_MainTexStrength");

    /// <summary>残影 Shader 中主图纹理属性的缓存ID。</summary>
    private static readonly int m_IdMainTex = Shader.PropertyToID("_MainTex");

    /// <summary>残影 Shader 中整体透明度属性的缓存ID。</summary>
    private static readonly int m_IdAlpha = Shader.PropertyToID("_Alpha");

    /// <summary>残影 Shader 中颜色属性的缓存ID。</summary>
    private static readonly int m_IdColor = Shader.PropertyToID("_Color");

    /// <summary>残影绘制所在的层，首次 OnEnable 时按 "Default" 层名解析，-1 表示未初始化。</summary>
    private static int m_DefaultLayer = -1;

    /// <summary>跨残影实例复用的材质属性块，每次刷新前清空再写入参数。</summary>
    private static MaterialPropertyBlock m_MaterialPropertyBlock = null;

    /// <summary>当前存活的残影快照列表，每帧倒序更新并移除已结束项。</summary>
    private List<GhostObject> m_GhostShadowList = new List<GhostObject>();

    /// <summary>缓存的角色所有 SkinnedMeshRenderer，首次访问时从子节点收集。</summary>
    private List<SkinnedMeshRenderer> m_SkinnedMeshRendererList = null;

    /// <summary>懒加载收集本对象及其子节点的所有 SkinnedMeshRenderer。</summary>
    private List<SkinnedMeshRenderer> cachedSkinnedMeshRenderer
    {
        get
        {
            if (m_SkinnedMeshRendererList == null)
            {
                m_SkinnedMeshRendererList = new List<SkinnedMeshRenderer>();
                gameObject.GetComponentsInChildren(m_SkinnedMeshRendererList);
            }
            return m_SkinnedMeshRendererList;
        }
    }

    /// <summary>缓存的角色所有 MeshFilter，首次访问时从子节点收集。</summary>
    private List<MeshFilter> m_MeshFilterList = null;

    /// <summary>懒加载收集本对象及其子节点的所有 MeshFilter。</summary>
    private List<MeshFilter> cachedMeshFilterList
    {
        get
        {
            if (m_MeshFilterList == null)
            {
                m_MeshFilterList = new List<MeshFilter>();
                gameObject.GetComponentsInChildren(m_MeshFilterList);
            }
            return m_MeshFilterList;
        }
    }

    /// <summary>残影专用材质，按 "SGame/Character/Character_Ghost" Shader 懒加载创建。</summary>
    private Material m_GhostMaterial = null;

    /// <summary>懒加载创建残影材质；Shader 缺失时由 OnEnable 报错误并禁用组件。</summary>
    private Material ghostMaterial
    {
        get
        {
            if (m_GhostMaterial == null)
            {
                m_GhostMaterial = new Material(Shader.Find("SGame/Character/Character_Ghost"));
            }
            return m_GhostMaterial;
        }
    }

    #region UnityMethods
    private void OnEnable()
    {
        if (m_DefaultLayer == -1)
        {
            m_DefaultLayer = LayerMask.NameToLayer("Default");
        }
        if (m_MaterialPropertyBlock == null)
        {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }

        if (ghostMaterial == null)
        {
            Debug.LogError(string.Format("[GhostEffect](OnEnable) ghostMaterial is null:[{0}]", gameObject.name));
            this.enabled = false;
            return;
        }
    }

    private void Update()
    {
        for (int i = 0; i < m_GhostShadowList.Count;)
        {
            GhostObject obj = m_GhostShadowList[i];

            if (obj.Update(ghostMaterial))
            {
                m_GhostShadowList.RemoveAt(i);
                DestroyGhostShadowObj(obj);
            }
            else
            {
                ++i;
            }
        }
    }

    private void OnDisable()
    {
        CleanUp();
    }

    #endregion

    #region LocalMethods
    /// <summary>模型失活时销毁未淡出的残影并解除渲染器缓存，重新启用后按当前模型收集。</summary>
    private void CleanUp()
    {
        // i为当前残影快照索引，仅销毁此组件自己烘焙的网格，不销毁模型共享网格。
        for (int i = 0; i < m_GhostShadowList.Count; ++i)
        {
            DestroyGhostShadowObj(m_GhostShadowList[i]);
        }
        // 模型池失活后必须重新收集渲染器，保留已清空的非空缓存会让复用模型只能生成空网格。
        m_SkinnedMeshRendererList = null;
        m_MeshFilterList = null;
        m_GhostShadowList.Clear();
    }

    private void DestroyGhostShadowObj(GhostObject obj)
    {
        List<MeshInfo> meshInfoList = obj.meshInfoList;
        for (int i = 0; i < meshInfoList.Count; ++i)
        {
            if (!meshInfoList[i].isSharedMesh)
            {
                Destroy(meshInfoList[i].mesh);
            }
        }
    }

    #endregion

    #region API
    // Summary:
    //     ///
    //     Create a ghost images of the previous locations of the object.
    //     ///
    //
    // params:
    //   color:
    //     Ghost color.
    //
    //   fresnelStrength:
    //     Strength of fresnel.
    //
    //   fresnelColorStrength:
    //     Strength of color
    //     
    //   alpha:
    //     Ghost alpha.
    //
    //   fadeoutTime:
    //     Fadeout time of the ghost effect object
    //     
    //   mainTexStrength:
    //     Strength of the MainTexture, 0 to 1.
    public void CreateGhostEffectObject(Color color, float fresnelStrength, float fresnelColorStrength, float alpha, float fadeoutTime, float mainTexStrength = 0f)
    {
        GhostObject obj = new GhostObject();

        bool combineMesh = mainTexStrength <= 0;
        if (combineMesh)
        {
            List<CombineInstance> combines = new List<CombineInstance>();

            for (int i = 0; i < cachedSkinnedMeshRenderer.Count; i++)
            {
                SkinnedMeshRenderer renderer = cachedSkinnedMeshRenderer[i];
                if (renderer.sharedMesh == null)
                {
                    Debug.LogErrorFormat("[GhostEffect]CreateGhostShadowObj mesh null!");
                    continue;
                }
                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh);
                combines.Add(new CombineInstance()
                {
                    mesh = mesh,
                    transform = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one),
                    subMeshIndex = 0
                });
            }

            MeshInfo info = new MeshInfo();
            info.mesh = new Mesh();
            info.mesh.name = "Ghost";
            info.mesh.CombineMeshes(combines.ToArray(), true);
            info.matrix = Matrix4x4.identity;
            obj.meshInfoList.Add(info);

            for (int i = 0; i < combines.Count; ++i)
            {
                Destroy(combines[i].mesh);
            }
        }
        else
        {
            foreach (var mr in cachedSkinnedMeshRenderer)
            {
                MeshInfo info = new MeshInfo();
                info.mesh = new Mesh();
                mr.BakeMesh(info.mesh);
                info.matrix = Matrix4x4.TRS(mr.transform.position, mr.transform.rotation, Vector3.one);
                info.material = mr.sharedMaterial;
                obj.meshInfoList.Add(info);
            }
        }

        foreach (var mf in cachedMeshFilterList)
        {
            if (mf.sharedMesh == null)
            {
                continue;
            }
            MeshInfo info = new MeshInfo();
            info.mesh = mf.sharedMesh;
            info.isSharedMesh = true;
            info.matrix = mf.transform.localToWorldMatrix;
            obj.meshInfoList.Add(info);
        }

        obj.remainingTime = fadeoutTime;
        obj.totalTime = fadeoutTime;
        obj.color = color;
        obj.fresnelStrength = fresnelStrength;
        obj.originFresnelColorStrength = fresnelColorStrength;
        obj.originAlpha = alpha;
        obj.originMainTexStrength = mainTexStrength;
        m_GhostShadowList.Add(obj);
    }
    #endregion

    /// <summary>单个残影网格的引用信息：网格、世界矩阵、是否共享网格及其原始材质。</summary>
    private struct MeshInfo
    {
        /// <summary>要绘制的网格实例。</summary>
        public Mesh mesh;

        /// <summary>该网格绘制时的世界变换矩阵。</summary>
        public Matrix4x4 matrix;

        /// <summary>true 表示引用的是共享网格，销毁残影时不能 Destroy 它。</summary>
        public bool isSharedMesh;

        /// <summary>网格对应的原始材质，用于取回主图纹理。</summary>
        public Material material;
    };

    /// <summary>一次残影快照所需的全部网格与衰减参数，Update 时按剩余时间渐变。</summary>
    private class GhostObject
    {
        /// <summary>本快照包含的全部网格引用列表。</summary>
        public List<MeshInfo> meshInfoList = new List<MeshInfo>();

        /// <summary>残影颜色。</summary>
        public Color color;

        /// <summary>菲涅尔边缘强度，整段残影保持恒定。</summary>
        public float fresnelStrength;

        /// <summary>主图强度（0 时合并为单一菲涅尔残影，大于0时按原材质贴图显示）。</summary>
        public float mainTexStrength;

        /// <summary>本次残影的总存活时长（秒），用于线性衰减分母。</summary>
        public float totalTime;

        /// <summary>剩余存活时间（秒），每帧按 deltaTime 递减到0即结束。</summary>
        public float remainingTime;

        /// <summary>初始的菲涅尔颜色强度，用于随剩余时间从该值衰减到0。</summary>
        public float originFresnelColorStrength;

        /// <summary>初始整体透明度，用于随剩余时间衰减到0。</summary>
        public float originAlpha;

        /// <summary>初始主图强度，用于随剩余时间衰减到0。</summary>
        public float originMainTexStrength;

        /// <summary>按剩余时间刷新一次残影材质参数并绘制所有网格。</summary>
        /// <param name="material">残影专用材质。</param>
        /// <returns>剩余时间耗尽返回 true，调用方应回收本残影。</returns>
        public bool Update(Material material)
        {
            remainingTime -= Time.deltaTime;
            float fresnelColorStrength = Mathf.Lerp(0, originFresnelColorStrength, remainingTime / totalTime);
            float alpha = Mathf.Lerp(0, originAlpha, remainingTime / totalTime);
            float mainTexStrength = Mathf.Lerp(0, originMainTexStrength, remainingTime / totalTime);

            m_MaterialPropertyBlock.Clear();
            m_MaterialPropertyBlock.SetFloat(m_IdFresnelStrength, fresnelStrength);
            m_MaterialPropertyBlock.SetFloat(m_IdFresnelColorStrength, fresnelColorStrength);
            m_MaterialPropertyBlock.SetFloat(m_IdAlpha, alpha);
            m_MaterialPropertyBlock.SetColor(m_IdColor, color);
            if (mainTexStrength > 0)
            {
                m_MaterialPropertyBlock.SetFloat(m_IdMainTexStrength, mainTexStrength);
            }

            for (int i = 0; i < meshInfoList.Count; ++i)
            {
                MeshInfo info = meshInfoList[i];
                if (mainTexStrength > 0 && info.material != null)
                {
                    Texture mainTex = info.material.GetTexture(m_IdMainTex);
                    if (mainTex != null)
                    {
                        m_MaterialPropertyBlock.SetTexture(m_IdMainTex, mainTex);
                    }
                }
                Graphics.DrawMesh(info.mesh, info.matrix, material, m_DefaultLayer, null, 0, m_MaterialPropertyBlock, false, false, false);
            }

            if (remainingTime <= 0)
            {
                return true;
            }

            return false;
        }
    };
}

