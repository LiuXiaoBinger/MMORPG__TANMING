using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
public class ROSceneShaderGUI : ShaderGUI
{
    public enum AlphaMode
    {
        Opaque = 0,
        AlphaTest = 1,
        Transparent = 2
    }

    MaterialProperty normalToggle = null;
    MaterialProperty mainTex = null;
    MaterialProperty normalTex = null;
    MaterialProperty specTex = null;
    MaterialProperty bumpIntense = null;
    MaterialProperty specIntense = null;
    MaterialProperty diffuse = null;
    MaterialProperty mse = null;
    MaterialProperty mseTex = null;
    MaterialProperty metallic = null;
    MaterialProperty smoothness = null;
    MaterialProperty emission = null;
    MaterialProperty alphaMode = null;
    MaterialProperty cutoff = null;
    MaterialProperty opacity = null;
    MaterialProperty lmFactor = null;
    MaterialProperty fogFactor = null;
    MaterialProperty waveFactor = null;
    MaterialProperty litFactor = null;
    MaterialProperty snowFactor = null;
    MaterialProperty skinAnimMode = null;
    MaterialProperty skinningOffset = null;
    MaterialProperty skinningMap = null;
    MaterialProperty skinningFrameShift = null;
    MaterialProperty cull = null;
    MaterialProperty srcBlend = null;
    MaterialProperty dstBlend = null;
    MaterialProperty zwrite = null;
    MaterialProperty stencil = null;
    MaterialEditor m_MaterialEditor;
    Material m_Material;

    public void FindProperties(MaterialProperty[] props)
    {
        normalToggle = FindProperty("_NORMALMAP",props);
        normalTex = FindProperty("_BumpMap",props);
        specTex = FindProperty("_SpecTex",props);
        bumpIntense = FindProperty("_BumpIntense",props);
        specIntense = FindProperty("_SpecIntense", props);
        diffuse = FindProperty("_Diffuse",props);

        mainTex = FindProperty("_MainTex", props);
        mse = FindProperty("_MSE", props);
        mseTex = FindProperty("_MSETex", props);
        metallic = FindProperty("_Metallic", props);
        smoothness = FindProperty("_Smoothness", props);
        emission = FindProperty("_Emission", props);
        alphaMode = FindProperty("_AlphaMode", props);
        opacity = FindProperty("_Opacity", props);
        cutoff = FindProperty("_Cutoff", props);
        lmFactor = FindProperty("_LMFactor", props);
        fogFactor = FindProperty("_FogFactor", props);
        waveFactor = FindProperty("_WaveFactor", props);
        litFactor = FindProperty("_LightFactor", props);
        snowFactor = FindProperty("_SnowFactor", props);
        skinAnimMode = FindProperty("_GpuAnim", props);
        skinningMap = FindProperty("_AnimTex", props);
        skinningOffset = FindProperty("_Offset", props);
        skinningFrameShift = FindProperty("_FrameShift", props);
        cull = FindProperty("_Cull", props);
        srcBlend = FindProperty("_SrcBlend", props);
        dstBlend = FindProperty("_DstBlend", props);
        zwrite = FindProperty("_ZWrite", props);
        stencil = FindProperty("_Stencil", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindProperties(props);
        m_MaterialEditor = materialEditor;
        m_Material = materialEditor.target as Material;

        if(normalToggle != null)
            m_MaterialEditor.ShaderProperty(normalToggle, "开启法线高光");

        if(m_Material.IsKeywordEnabled("_NORMALMAP_ON"))
        {
            m_MaterialEditor.ShaderProperty(normalTex, "法线");
            m_MaterialEditor.ShaderProperty(specTex, "高光");
            m_MaterialEditor.ShaderProperty(bumpIntense, "法线强度");
            m_MaterialEditor.ShaderProperty(specIntense, "高光强度");
            m_MaterialEditor.ShaderProperty(diffuse, "环境光");
        }

        m_MaterialEditor.TexturePropertySingleLine(new GUIContent("主纹理"), mainTex);
        m_MaterialEditor.TextureScaleOffsetProperty(mainTex);

        m_MaterialEditor.ShaderProperty(mse, "使用质感纹理");
        if (m_Material.IsKeywordEnabled("_MSE_ON"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("质感纹理(金属=X, 光滑=Y, 发光=Z)"), mseTex);
        }
        else
        {
            m_MaterialEditor.ShaderProperty(metallic, "金属");
            m_MaterialEditor.ShaderProperty(smoothness, "光滑");
            m_MaterialEditor.ShaderProperty(emission, "发光");
        }

        EditorGUILayout.Space();
        AlphaControl();

        EditorGUILayout.Space();
        CullControl();

        EditorGUILayout.Space();
        AnimControl();

        EditorGUILayout.Space();
        LightmapControl();

        EditorGUILayout.Space();
        StencialControl();

        EditorGUILayout.Space();
        Instancing();
        DoubleGI();
        QueueControl();
    }

    void AlphaControl()
    {
        EditorGUI.BeginChangeCheck();
        var mode = (AlphaMode)EditorGUILayout.EnumPopup("透明模式", (AlphaMode)alphaMode.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Alpha Mode");
            alphaMode.floatValue = (float)mode;
            if (alphaMode.floatValue < 2)
            {
                srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.Zero;
                zwrite.floatValue = 1;

                if (alphaMode.floatValue == 1)
                {
                    m_Material.SetOverrideTag("RenderType", "TransparentCutout");
                    m_Material.EnableKeyword("_ALPHAMODE_CUTOUT");
                    m_Material.renderQueue = Mathf.Min(2490, m_Material.renderQueue);
                    if (m_Material.renderQueue == 2000)
                    {
                        m_Material.renderQueue = 1990;
                    }
                }
                else
                {
                    m_Material.DisableKeyword("_ALPHAMODE_CUTOUT");
                    m_Material.SetOverrideTag("RenderType", "Opaque");
                    m_Material.renderQueue = Mathf.Min(2490, m_Material.renderQueue);
                }
            }
            else
            {
                m_Material.DisableKeyword("_ALPHAMODE_CUTOUT");

                srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                zwrite.floatValue = 0;
                m_Material.SetOverrideTag("RenderType", "Transparent");
                if (m_Material.renderQueue < 2500)
                    m_Material.renderQueue = 3000;
            }

            EditorUtility.SetDirty(m_Material);
        }

        if (alphaMode.floatValue == 1)
        {
            m_MaterialEditor.ShaderProperty(cutoff, "透明剔除阈值");
        }

        if (alphaMode.floatValue == 2)
        {
            m_MaterialEditor.ShaderProperty(opacity, "透明度");
        }
    }

    void LightmapControl()
    {
        m_MaterialEditor.ShaderProperty(lmFactor, "烘培色影响");
        m_MaterialEditor.ShaderProperty(fogFactor, "雾影响");
        m_MaterialEditor.ShaderProperty(litFactor, "场景光影响");
        m_MaterialEditor.ShaderProperty(snowFactor, "覆雪强度");
    }

    void StencialControl()
    {
        m_MaterialEditor.ShaderProperty(stencil, "特效穿透");
    }

    void AnimControl()
    {
        m_MaterialEditor.ShaderProperty(skinAnimMode, "顶点动画模式");
        if (m_Material.IsKeywordEnabled("_GPUANIM_BONEMAP") || m_Material.IsKeywordEnabled("_GPUANIM_VERTEXMAP"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("预制动画纹理（非矩阵组模式）"), skinningMap);
            m_MaterialEditor.ShaderProperty(skinningOffset, "蒙皮动画参数(X=像素偏移, Y=帧数, Z=骨骼数, W=FPS)");
            m_MaterialEditor.ShaderProperty(skinningFrameShift, "错帧");
        }
        else if (m_Material.IsKeywordEnabled("_GPUANIM_WIND"))
        {
            m_MaterialEditor.ShaderProperty(waveFactor, "摆动幅度");
        }
    }

    void CullControl()
    {
        m_MaterialEditor.ShaderProperty(cull, "剔除");
    }

    void Instancing()
    {
        m_MaterialEditor.EnableInstancingField();
    }

    void DoubleGI()
    {
        m_MaterialEditor.LightmapEmissionProperty();
        m_MaterialEditor.DoubleSidedGIField();
    }

    void QueueControl()
    {

        m_MaterialEditor.RenderQueueField();
    }
}
