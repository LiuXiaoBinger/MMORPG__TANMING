Shader "LYF/UI/CombatHudUpperPlate"
{
    Properties
    {
        [PerRendererData] _MainTex ("图标图集", 2D) = "white" {}
        _StencilComp ("模板比较", Float) = 8
        _Stencil ("模板编号", Float) = 0
        _StencilOp ("模板操作", Float) = 0
        _StencilWriteMask ("模板写入掩码", Float) = 255
        _StencilReadMask ("模板读取掩码", Float) = 255
        _ColorMask ("颜色通道掩码", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("透明裁剪", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // RawImage 传入的顶点、颜色和图集切片坐标。
            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            // 保留投影前坐标供 RectMask2D 判断裁剪范围。
            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uiPosition : TEXCOORD1;
            };

            // 上排战斗控件采样的原始图集。
            sampler2D _MainTex;
            // UI 提供的裁剪矩形，和投影前坐标使用同一坐标系。
            float4 _ClipRect;

            // 将界面顶点转换到裁剪空间，并传递位置和图集坐标。
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uiPosition = input.vertex.xy;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            // 按图集的底色范围淡化暗底，保留图案和亮色描边。
            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 art = tex2D(_MainTex, input.uv);
                // 暗底最大 sRGB 通道为 36/255。保留从 56/255 开始的亮色。
                half brightness = max(art.r, max(art.g, art.b));
                #ifndef UNITY_COLORSPACE_GAMMA
                brightness = LinearToGammaSpaceExact(brightness);
                #endif
                half foreground = smoothstep(0.15, 0.22, brightness);
                fixed4 color = art * input.color;
                color.a *= lerp(0.55, 1.0, foreground);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.uiPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
