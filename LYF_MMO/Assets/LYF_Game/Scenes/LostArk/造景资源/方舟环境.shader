Shader "LYF/LostArk/Environment"
{
    Properties
    {
        // 原模型的漫反射图和手绘明暗，保留树叶与布边的透明通道。
        _MainTex ("表面颜色与轮廓", 2D) = "white" {}
        // 切线空间法线和法线强度用于表现砖缝、木纹和石刻。
        [Normal] _BumpMap ("表面法线", 2D) = "bump" {}
        _BumpScale ("法线强度", Range(0,1)) = 0.48
        // 材质色用于木梁、檐口和石块的同纹理异色。
        _Color ("材质色", Color) = (1,1,1,1)
        // 实心材质设为零，树叶等镂空材质各自指定裁剪阈值。
        _Cutoff ("轮廓阈值", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert addshadow fullforwardshadows
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        // 颜色贴图、法线贴图及各材质的色调和裁剪参数。
        sampler2D _MainTex, _BumpMap;
        fixed4 _Color;
        half _Cutoff, _BumpScale;
        // 模型自带的两张纹理共享第一套 UV。
        struct Input { float2 uv_MainTex; };
        // 以颜色图的透明轮廓生成树叶、布边和对应的实时投影。
        // i 提供当前像素的模型 UV，o 交给 Unity 的光照与雾流程。
        void surf(Input i, inout SurfaceOutput o)
        {
            // 当前像素的手绘底色以及透明覆盖率。
            fixed4 sampleColor = tex2D(_MainTex, i.uv_MainTex) * _Color;
            clip(sampleColor.a - _Cutoff);
            o.Albedo = sampleColor.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, i.uv_MainTex), _BumpScale);
            o.Emission = sampleColor.rgb * 0.10;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
