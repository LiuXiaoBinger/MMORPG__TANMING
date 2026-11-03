using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在被调用时复制角色当前姿势的网格，停留在原位置并逐渐淡出，形成高速动作的残影。
/// 边缘亮度、整体不透明度和贴图强度随时间降低，边缘形状参数保持不变。
/// </summary>
public class GhostEffect : MonoBehaviour
{
    /// <summary>控制残影边缘效果形状的 Shader 属性编号，预先缓存以免反复按名称查找。</summary>
    private static readonly int m_IdFresnelStrength = Shader.PropertyToID("_FresnelStrength");

    /// <summary>控制残影边缘颜色亮度的 Shader 属性编号。</summary>
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

    /// <summary>尚未消失的残影；从列表头逐个更新，移除后仍检查同一位置的下一项。</summary>
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
    /// <summary>启用时准备绘制层和共享材质参数，材质不可用时关闭组件。</summary>
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

    /// <summary>更新每份残影，移除已淡出的记录并销毁为它创建的网格。</summary>
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

    /// <summary>停用时立即清理残影，避免角色隐藏后仍留有图像。</summary>
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

    /// <summary>销毁 obj 中为残影新建的网格；模型共享网格仍归原资源所有。</summary>
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
    /// <summary>按角色当前姿势和世界位置生成一份残影，后续每帧绘制直到淡出。</summary>
    /// <param name="color">残影的整体颜色。</param>
    /// <param name="fresnelStrength">边缘效果强度，残影存活期间不变。</param>
    /// <param name="fresnelColorStrength">边缘颜色的初始亮度，会随时间降到 0。</param>
    /// <param name="alpha">初始不透明度，会随时间降到 0。</param>
    /// <param name="fadeoutTime">淡出所用的秒数。</param>
    /// <param name="mainTexStrength">原贴图的显示强度；小于等于 0 时合并蒙皮网格，不显示原贴图。</param>
    public void CreateGhostEffectObject(Color color, float fresnelStrength, float fresnelColorStrength, float alpha, float fadeoutTime, float mainTexStrength = 0f)
    {
        // 保存这一次姿势的网格、世界位置和淡出参数。
        GhostObject obj = new GhostObject();

        // 不显示原贴图时，可把蒙皮网格合并成一份绘制。
        bool combineMesh = mainTexStrength <= 0;
        if (combineMesh)
        {
            // 临时保存各蒙皮部件烘焙结果及其世界变换。
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
            // 按剩余时长比例降低边缘亮度。
            float fresnelColorStrength = Mathf.Lerp(0, originFresnelColorStrength, remainingTime / totalTime);
            // 当前帧的不透明度。
            float alpha = Mathf.Lerp(0, originAlpha, remainingTime / totalTime);
            // 当前帧保留原贴图的强度。
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

