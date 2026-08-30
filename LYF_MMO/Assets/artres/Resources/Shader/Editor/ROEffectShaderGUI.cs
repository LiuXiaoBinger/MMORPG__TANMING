using UnityEngine;
using System.Collections;
using UnityEditor;

public class ROEffectShaderGUI : ShaderGUI
{
    public enum BlendMode
    {
        None = 0,//One Zero fog one
        AlphaBlend = 1,//SrcAlpha OneMinusSrcAlpha fog one
        AlphaBlendPremultiply = 2,//One OneMinusSrcAlpha fog zero
        Additive = 3,//SrcAlpha One fog zero
        AdditivePremultiply = 4,//One One fog zero
        RenderTexture = 5//One SrcAlpha
    }

    public enum BlendModeUIOrDistort
    {
        None = 0,//One Zero fog one
        AlphaBlend = 1,//SrcAlpha OneMinusSrcAlpha fog one
        AlphaBlendHard = 2,//One OneMinusSrcAlpha fog zero
        Additive = 3,//SrcAlpha One fog zero
        AdditiveHard = 4//One One fog zero
    }

    public enum BlendModeProjector
    {
        None = 0,//One Zero fog one
        AlphaBlend = 1,//SrcAlpha OneMinusSrcAlpha fog one
        AlphaBlendHard = 2,//One OneMinusSrcAlpha fog zero
        Additive = 3,//SrcAlpha One fog zero
        AdditiveHard = 4,//One One fog zero
        Lighten = 5
    }

    public enum BlendModeRT
    {
        None = 0,//One Zero fog one
        AlphaBlend = 1,
        Additive = 3//SrcAlpha One fog zero
    }

    public enum ChannelMask
    {
        R = 0,
        G,
        B,
        A
    }

    public enum ShaderMode
    {
        Base = 0,
        Distortion = 1,
        Projector = 2,
        UI = 3,
        RT = 4
    }

    MaterialProperty worldUVToggle = null;
    MaterialProperty hdrToggle = null;
    MaterialProperty customDataToggle = null;
    MaterialProperty multiColorToggle = null;
    MaterialProperty blackLessToggle = null;
    MaterialProperty tintColor = null;
    MaterialProperty ColorA = null;
    MaterialProperty ColorB = null;
    MaterialProperty Multiple = null;

    MaterialProperty bumpStrength = null;

    MaterialProperty mainTex = null;
    MaterialProperty mainColor = null;

    MaterialProperty maskType = null;
    MaterialProperty maskColor = null;
    MaterialProperty maskTex = null;
    MaterialProperty maskChannel = null;

    MaterialProperty strength = null;
    MaterialProperty cutoff = null;
    MaterialProperty hardFade = null;
    MaterialProperty edgeColor = null;
    MaterialProperty edgeWidth = null;

    MaterialProperty hue = null;
    MaterialProperty saturate = null;
    MaterialProperty luminance = null;
    MaterialProperty contrast = null;

    MaterialProperty scrollType = null;
    MaterialProperty scrollMain = null;
    MaterialProperty scrollMask = null;
    MaterialProperty scrollMain2 = null;
    MaterialProperty scrollMask2 = null;

    MaterialProperty msk2Toggle = null;
    MaterialProperty msk2Tex = null;
    MaterialProperty msk2Channel = null;
    MaterialProperty msk2Strength = null;

    MaterialProperty lifeCtrlByAlpha = null;
    MaterialProperty preserveVertAlpha = null;

    MaterialProperty rimToggle = null;
    MaterialProperty rimColor = null;
    MaterialProperty rimPower = null;
    MaterialProperty rimScatter = null;

    MaterialProperty fog = null;
    MaterialProperty cull = null;
    MaterialProperty blendMode = null;
    MaterialProperty srcBlend = null;
    MaterialProperty dstBlend = null;
    MaterialProperty zwrite = null;
    MaterialProperty ztest = null;

    MaterialEditor m_MaterialEditor;
    Material m_Material;

    ShaderMode shaderMode;

    public void FindProperties(Material material, MaterialProperty[] props)
    {
        if(material.HasProperty("_WorldUV"))
            worldUVToggle = FindProperty("_WorldUV", props);

        if(material.HasProperty("_HDR"))
            hdrToggle = FindProperty("_HDR", props);

        if(material.HasProperty("_CustomData"))
            customDataToggle = FindProperty("_CustomData",props);

        if(material.HasProperty("_SecondColor"))
            multiColorToggle = FindProperty("_SecondColor", props);

        if(material.HasProperty("_BlackLess"))
            blackLessToggle = FindProperty("_BlackLess",props);

        if (material.HasProperty("_TintColor"))
            tintColor = FindProperty("_TintColor", props);

        if (material.HasProperty("_ColorA"))
            ColorA = FindProperty("_ColorA", props);

        if (material.HasProperty("_ColorB"))
            ColorB = FindProperty("_ColorB", props);

        if(material.HasProperty("_Multiple"))
            Multiple = FindProperty("_Multiple",props);

        if (material.HasProperty("_MainTex"))
            mainTex = FindProperty("_MainTex", props);

        if (material.HasProperty("_MainColor"))
            mainColor = FindProperty("_MainColor", props);

        if (material.HasProperty("_BumpStrength"))
            bumpStrength = FindProperty("_BumpStrength", props);


        if (material.HasProperty("_SecondType"))
            maskType = FindProperty("_SecondType", props);

        if (material.HasProperty("_MaskChannel"))
            maskChannel = FindProperty("_MaskChannel", props);

        if (material.HasProperty("_MaskTex"))
            maskTex = FindProperty("_MaskTex", props);

        if (material.HasProperty("_MaskColor"))
            maskColor = FindProperty("_MaskColor", props);

        if (material.HasProperty("_Strength"))
            strength = FindProperty("_Strength", props);


        if (material.HasProperty("_ThirdMap"))
            msk2Toggle = FindProperty("_ThirdMap", props);

        if (material.HasProperty("_Msk2Tex"))
            msk2Tex = FindProperty("_Msk2Tex", props);

        if (material.HasProperty("_Msk2Channel"))
            msk2Channel = FindProperty("_Msk2Channel", props);

        if (material.HasProperty("_Msk2Strength"))
            msk2Strength = FindProperty("_Msk2Strength", props);

        if (material.HasProperty("_Cutoff"))
            cutoff = FindProperty("_Cutoff", props);

        if (material.HasProperty("_HardFade"))
            hardFade = FindProperty("_HardFade", props);

        if (material.HasProperty("_EdgeColor"))
            edgeColor = FindProperty("_EdgeColor", props);

        if (material.HasProperty("_EdgeWidth"))
            edgeWidth = FindProperty("_EdgeWidth", props);


        if (material.HasProperty("_Hue"))
            hue = FindProperty("_Hue", props);

        if (material.HasProperty("_Saturate"))
            saturate = FindProperty("_Saturate", props);

        if (material.HasProperty("_Luminance"))
            luminance = FindProperty("_Luminance", props);

        if (material.HasProperty("_Contrast"))
            contrast = FindProperty("_Contrast", props);


        if (material.HasProperty("_ScrollType"))
            scrollType = FindProperty("_ScrollType", props);

        if (material.HasProperty("_ScrollMain"))
            scrollMain = FindProperty("_ScrollMain", props);

        if (material.HasProperty("_ScrollMask"))
            scrollMask = FindProperty("_ScrollMask", props);

        if (material.HasProperty("_ScrollMain2"))
            scrollMain2 = FindProperty("_ScrollMain2", props);

        if (material.HasProperty("_ScrollMask2"))
            scrollMask2 = FindProperty("_ScrollMask2", props);

        if (material.HasProperty("_VertColorAlphaIsLifeTime"))
            lifeCtrlByAlpha = FindProperty("_VertColorAlphaIsLifeTime", props);

        if (material.HasProperty("_PreserveVertAlpha"))
            preserveVertAlpha = FindProperty("_PreserveVertAlpha", props);

        if (material.HasProperty("_Rim"))
            rimToggle = FindProperty("_Rim", props);

        if (material.HasProperty("_RimColor"))
            rimColor = FindProperty("_RimColor", props);

        if (material.HasProperty("_RimPower"))
            rimPower = FindProperty("_RimPower", props);

        if (material.HasProperty("_RimScatter"))
            rimScatter = FindProperty("_RimScatter", props);

        if (material.HasProperty("_Fog"))
            fog = FindProperty("_Fog", props);

        if (material.HasProperty("_Cull"))
            cull = FindProperty("_Cull", props);

        if (material.HasProperty("_Mode"))
            blendMode = FindProperty("_Mode", props);

        if (material.HasProperty("_SrcBlend"))
            srcBlend = FindProperty("_SrcBlend", props);

        if (material.HasProperty("_DstBlend"))
            dstBlend = FindProperty("_DstBlend", props);

        if (material.HasProperty("_ZWrite"))
            zwrite = FindProperty("_ZWrite", props);

        if (material.HasProperty("_ZTest"))
            ztest = FindProperty("_ZTest", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        m_MaterialEditor = materialEditor;
        m_Material = materialEditor.target as Material;

        FindProperties(m_Material, props);

        if (m_Material.shader.name.IndexOf("Distortion") > -1)
        {
            shaderMode = ShaderMode.Distortion;
        }
        else if (m_Material.shader.name.IndexOf("Projector") > -1)
        {
            shaderMode = ShaderMode.Projector;
        }
        else if (m_Material.shader.name.IndexOf("UI") > -1)
        {
            shaderMode = ShaderMode.UI;
        }
        else if (m_Material.shader.name.IndexOf("RT") > -1)
        {
            shaderMode = ShaderMode.RT;
        }
        else
        {
            shaderMode = ShaderMode.Base;
        }

        if (tintColor != null)
            m_MaterialEditor.ShaderProperty(tintColor, "最终色调");
        if(hdrToggle != null)
            m_MaterialEditor.ShaderProperty(hdrToggle, "HDR");
        if(customDataToggle != null)
            m_MaterialEditor.ShaderProperty(customDataToggle, "使用custom data!");
        if(multiColorToggle != null)
            m_MaterialEditor.ShaderProperty(multiColorToggle,"启用主纹理第二颜色");
        if(worldUVToggle != null)
            m_MaterialEditor.ShaderProperty(worldUVToggle, "开启世界UV");

        if(m_Material.IsKeywordEnabled("_SECONDCOLOR_ON"))
        {
            if( 
                ColorA != null &&
                ColorB != null && 
                Multiple != null &&
                blackLessToggle != null
                )
            {
                m_MaterialEditor.ShaderProperty(blackLessToggle, "去黑");
                m_MaterialEditor.ShaderProperty(ColorA, "主色");
                m_MaterialEditor.ShaderProperty(ColorB, "副色");
                m_MaterialEditor.ShaderProperty(Multiple, "局部加强");
            }
        }
        
        if (mainTex != null)
        {
            if (bumpStrength != null)
            {
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("扭曲纹理"), mainTex, bumpStrength);
                m_MaterialEditor.TextureScaleOffsetProperty(mainTex);
            }
            else if (mainColor != null)
            {
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("主纹理"), mainTex, mainColor);
                m_MaterialEditor.TextureScaleOffsetProperty(mainTex);
            }
        }

        EditorGUILayout.Space();
        MaskField();
        EditorGUILayout.Space();
        LifeTimeField();
        EditorGUILayout.Space();
        ScrollField();
        EditorGUILayout.Space();
        RimField();
        EditorGUILayout.Space();
        FogField();
        EditorGUILayout.Space();
        CullField();
        BlendField();
        ZField();
        QueneField();
    }

    void MaskField()
    {
        if (maskType == null) return;

        m_MaterialEditor.ShaderProperty(maskType, "叠加纹理类型");

        if (m_Material.IsKeywordEnabled("_SECONDTYPE_ALPHAMASK"))
        {
            m_MaterialEditor.TexturePropertySingleLine(
                new GUIContent(string.Format("遮罩纹理 ({0})", (ChannelMask)maskChannel.floatValue)), maskTex, maskColor, maskChannel);

            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);

            m_MaterialEditor.ShaderProperty(strength, "遮罩强度");
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_DISSOLVE"))
        {
            m_MaterialEditor.TexturePropertySingleLine(
                new GUIContent(string.Format("消散纹理 ({0})", (ChannelMask)maskChannel.floatValue)), maskTex, maskColor, maskChannel);

            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);

            m_MaterialEditor.ShaderProperty(hardFade, "使用硬消散?");

            if (lifeCtrlByAlpha.floatValue == 0)
                m_MaterialEditor.ShaderProperty(strength, "可见度");

            m_MaterialEditor.ShaderProperty(cutoff, "消散阈值");
            m_MaterialEditor.ShaderProperty(edgeColor, "消散边缘色");
            m_MaterialEditor.ShaderProperty(edgeWidth, "消散边缘宽度");
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_ADDITIVE"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("叠亮纹理"), maskTex, maskColor, strength);
            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_ALPHABLEND"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("混合透明纹理"), maskTex, maskColor, strength);
            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_COLORBLEND"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("色彩混合纹理"), maskTex, maskColor, strength);
            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_DISTORT"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("扭曲纹理(法线)"), maskTex, strength);
            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_FLOWMAP"))
        {
            m_MaterialEditor.TexturePropertySingleLine(new GUIContent("扰流纹理(法线)"), maskTex, strength);
            m_MaterialEditor.TextureScaleOffsetProperty(maskTex);
        }
        else if (m_Material.IsKeywordEnabled("_SECONDTYPE_COLORGRAD"))
        {
            m_MaterialEditor.ShaderProperty(hue, "色相");
            m_MaterialEditor.ShaderProperty(saturate, "饱和");
            m_MaterialEditor.ShaderProperty(luminance, "亮度");
            m_MaterialEditor.ShaderProperty(contrast, "对比");
        }

        EditorGUILayout.Space();
        if (msk2Toggle != null)
            m_MaterialEditor.ShaderProperty(msk2Toggle, "启用第三纹理");
        
        if (m_Material.IsKeywordEnabled("_THIRDMAP_ON"))
        {
            if (m_Material.IsKeywordEnabled("_SECONDTYPE_DISTORT") || m_Material.IsKeywordEnabled("_SECONDTYPE_FLOWMAP"))
            {
                m_MaterialEditor.TexturePropertySingleLine(
                    new GUIContent(string.Format("消散纹理 ({0})", (ChannelMask)msk2Channel.floatValue)), msk2Tex, maskChannel);

                m_MaterialEditor.TextureScaleOffsetProperty(msk2Tex);

                m_MaterialEditor.ShaderProperty(hardFade, "使用硬消散?");

                if (lifeCtrlByAlpha.floatValue == 0)
                    m_MaterialEditor.ShaderProperty(msk2Strength, "可见度");

                m_MaterialEditor.ShaderProperty(cutoff, "消散阈值");
                m_MaterialEditor.ShaderProperty(edgeColor, "消散边缘色");
                m_MaterialEditor.ShaderProperty(edgeWidth, "消散边缘宽度");
            }
            else
            {
                m_MaterialEditor.TexturePropertySingleLine(
                    new GUIContent(string.Format("遮罩纹理 ({0})", (ChannelMask)msk2Channel.floatValue)), msk2Tex, maskChannel);

                m_MaterialEditor.TextureScaleOffsetProperty(msk2Tex);

                m_MaterialEditor.ShaderProperty(msk2Strength, "遮罩强度");
            }
        }
    }

    void ScrollField()
    {
        if (scrollType == null) return;

        m_MaterialEditor.ShaderProperty(scrollType, "UV动画");
        if (!m_Material.IsKeywordEnabled("_SCROLLTYPE_NONE"))
        {
            if (m_Material.IsKeywordEnabled("_SCROLLTYPE_SCROLL"))
            {
                EditorGUILayout.LabelField("主纹理 UV 滚动", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "平铺 (xy), 移速 (zw)");
            }
            else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_ANISPRITES"))
            {
                EditorGUILayout.LabelField("主纹理 序列图播放", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "横纵格 (xy), 速度 (z)");
            }
            else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_SCALE"))
            {
                EditorGUILayout.LabelField("主纹理 UV 缩放", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "比例 (xy) 中心 (zw)");
            }
            else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_ROTATE"))
            {
                EditorGUILayout.LabelField("主纹理 UV 旋转", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "初角 (x) 转速 (y) 中心 (zw)");
            }
            else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_RANDOM"))
            {
                EditorGUILayout.LabelField("主纹理 UV 随机", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "平铺 (xy), 移速 (zw)");
                m_MaterialEditor.ShaderProperty(scrollMain2, "平铺 (xy), 移速 (zw)");
               
            }
            else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_MULTIBLEND"))
            {
                EditorGUILayout.LabelField("主纹理 UV 多层", EditorStyles.boldLabel);
                m_MaterialEditor.ShaderProperty(scrollMain, "(次数|间隔|循环时间|缩放)");
                m_MaterialEditor.ShaderProperty(scrollMain2, "(透明变化|透明阈值|初角|旋转)");
            }

            if (!m_Material.IsKeywordEnabled("_SECONDTYPE_NONE"))
            {
                if (m_Material.IsKeywordEnabled("_SCROLLTYPE_SCROLL"))
                {
                    EditorGUILayout.LabelField("次纹理 UV 滚动", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMask, "平铺 (xy), 移速 (zw)");
                }
                else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_ANISPRITES"))
                {
                    EditorGUILayout.LabelField("次纹理 序列图播放", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMask, "横纵格 (xy), 速度 (z)");
                }
                else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_SCALE"))
                {
                    EditorGUILayout.LabelField("次纹理 UV 缩放", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMask, "比例 (xy) 中心 (zw)");
                }
                else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_ROTATE"))
                {
                    EditorGUILayout.LabelField("次纹理 UV 旋转", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMask, "初角 (x) 转速 (y) 中心 (zw)");
                }
                else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_RANDOM"))
                {
                    EditorGUILayout.LabelField("主纹理 UV 随机", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMain, "平铺 (xy), 移速 (zw)");
                    m_MaterialEditor.ShaderProperty(scrollMain2, "平铺 (xy), 移速 (zw)");

                }
                else if (m_Material.IsKeywordEnabled("_SCROLLTYPE_MULTIBLEND"))
                {
                    EditorGUILayout.LabelField("次纹理 UV 多层", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(scrollMask, "(次数|间隔|循环时间|缩放)");
                    m_MaterialEditor.ShaderProperty(scrollMask2, "(透明变化|透明阈值|初角|旋转)");
                }
            }
        }
    }


    void RimField()
    {
        if (rimToggle == null) return;

        m_MaterialEditor.ShaderProperty(rimToggle, "启用边缘光?");
        if (m_Material.IsKeywordEnabled("_RIM_ON"))
        {
            m_MaterialEditor.ShaderProperty(rimColor, "边缘色");
            m_MaterialEditor.ShaderProperty(rimPower, "边缘锐度");
            m_MaterialEditor.ShaderProperty(rimScatter, "边缘消散");
        }
    }

    void LifeTimeField()
    {
        if (!m_Material.IsKeywordEnabled("_SECONDTYPE_NONE"))
        {
            if (lifeCtrlByAlpha != null)
                m_MaterialEditor.ShaderProperty(lifeCtrlByAlpha, "使用顶点透明代替功能强度");

            if (preserveVertAlpha != null)
                m_MaterialEditor.ShaderProperty(preserveVertAlpha, "保留顶点透明度");
        }
    }

    void FogField()
    {
        if (fog != null) m_MaterialEditor.ShaderProperty(fog, "雾影响");
    }

    void BlendField()
    {
        if (blendMode == null) return;

        int mode = 0;
        EditorGUI.BeginChangeCheck();
        if (shaderMode == ShaderMode.Base)
        {
            var m = (BlendMode)EditorGUILayout.EnumPopup("混合模式", (BlendMode)blendMode.floatValue);
            mode = (int)m;
        }
        else if (shaderMode == ShaderMode.Distortion)
        {
            var m = (BlendModeUIOrDistort)EditorGUILayout.EnumPopup("混合模式", (BlendModeUIOrDistort)blendMode.floatValue);
            mode = (int)m;
        }
        else if (shaderMode == ShaderMode.Projector)
        {
            var m = (BlendModeProjector)EditorGUILayout.EnumPopup("混合模式", (BlendModeProjector)blendMode.floatValue);
            mode = (int)m;
        }
        else if (shaderMode == ShaderMode.RT || shaderMode == ShaderMode.UI)
        {
            var m = (BlendModeRT)EditorGUILayout.EnumPopup("混合模式", (BlendModeRT)blendMode.floatValue);
            mode = (int)m;
        }

        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");

            switch (mode)
            {
                case 0:// blend one zero
                    {
                        blendMode.floatValue = mode;
                        if (shaderMode == ShaderMode.Projector)
                        {
                            m_Material.SetOverrideTag("RenderType", "Transparent");
                            m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        }
                        else
                        {
                            m_Material.SetOverrideTag("RenderType", "Opaque");
                            m_Material.renderQueue = m_Material.renderQueue >= 2500 ? 2000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.Zero;
                            if (shaderMode == ShaderMode.Base)
                            {
                                m_Material.DisableKeyword("_PRE_ON");
                            }
                        }
                    }
                    break;
                case 1://blend alpha blend
                    {
                        blendMode.floatValue = mode;
                        m_Material.SetOverrideTag("RenderType", "Transparent");
                        m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                        srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        if (shaderMode == ShaderMode.Base)
                        {
                            m_Material.DisableKeyword("_PRE_ON");
                        }
                    }
                    break;
                case 2://blend premultiply
                    {
                        if (shaderMode != ShaderMode.Projector)
                        {
                            blendMode.floatValue = mode;
                            m_Material.SetOverrideTag("RenderType", "Transparent");
                            m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                            if (shaderMode == ShaderMode.Base)
                            {
                                m_Material.EnableKeyword("_PRE_ON");
                            }
                        }
                    }
                    break;
                case 3://alpha additive
                    {
                        blendMode.floatValue = mode;
                        m_Material.SetOverrideTag("RenderType", "Transparent");
                        m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                        srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                        dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                        if (shaderMode == ShaderMode.Base)
                        {
                            m_Material.DisableKeyword("_PRE_ON");
                        }
                    }
                    break;
                case 4://additive
                    {
                        if (shaderMode != ShaderMode.Projector)
                        {
                            blendMode.floatValue = mode;
                            m_Material.SetOverrideTag("RenderType", "Transparent");
                            m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                            if (shaderMode == ShaderMode.Base)
                            {
                                m_Material.EnableKeyword("_PRE_ON");
                            }
                        }
                    }
                    break;
                case 5:
                    {
                        if (shaderMode == ShaderMode.Projector)//Lighten Mode
                        {
                            blendMode.floatValue = mode;
                            m_Material.SetOverrideTag("RenderType", "Opaque");
                            m_Material.renderQueue = m_Material.renderQueue >= 2500 ? 2000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.DstColor;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                        }
                        else if (shaderMode == ShaderMode.Base)//RT Mode
                        {
                            blendMode.floatValue = mode;
                            m_Material.SetOverrideTag("RenderType", "Transparent");
                            m_Material.renderQueue = m_Material.renderQueue < 2500 ? 3000 : m_Material.renderQueue;
                            srcBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.One;
                            dstBlend.floatValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                            if (shaderMode == ShaderMode.Base)
                            {
                                m_Material.DisableKeyword("_PRE_ON");
                            }
                        }
                    }
                    break;
            }

            EditorUtility.SetDirty(m_Material);
        }
    }

    void CullField()
    {
        if (cull != null) m_MaterialEditor.ShaderProperty(cull, "剔除");
    }

    void ZField()
    {
        if (zwrite != null) m_MaterialEditor.ShaderProperty(zwrite, "写入深度");
        if (ztest != null) m_MaterialEditor.ShaderProperty(ztest, "测试深度");
    }

    void QueneField()
    {
        m_MaterialEditor.RenderQueueField();
    }
}
