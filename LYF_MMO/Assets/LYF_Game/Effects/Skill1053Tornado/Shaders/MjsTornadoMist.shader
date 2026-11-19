Shader "LYF/Effects/MjsTornadoMist"
{
    Properties
    {
        _BaseRGBA ("风纹贴图", 2D) = "white" {}
        _ShapeRGBA ("风柱软边遮罩", 2D) = "white" {}
        _Alpha ("原动画的淡入淡出", Range(0, 10)) = 1
        _MistTint ("风雾色调和透明度上限", Color) = (0.80, 0.92, 1, 0.6)
        _SpriteMask ("粒子按纹理亮度去黑底", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _BaseRGBA; // 原风纹的颜色和 Alpha。
            sampler2D _ShapeRGBA; // 使用原 Animator 播放的柱体遮罩。
            float4 _BaseRGBA_ST; // 风纹平铺和随时间推进的 UV 偏移。
            float4 _ShapeRGBA_ST; // 风柱遮罩平铺和随时间推进的 UV 偏移。
            half _Alpha; // 原特效动画中记录的阶段透明度。
            half4 _MistTint; // 线性混合中的风雾 RGB 和单层透明度上限。
            half _SpriteMask; // 零用于风柱网格，一用于原粒子贴图。

            // 来自原网格或粒子顶点流的采样数据。
            struct WindVertex
            {
                float4 position : POSITION; // 渲染对象局部空间中的顶点。
                float2 uv : TEXCOORD0; // 已含粒子纹理序列偏移的 UV。
                fixed4 color : COLOR; // 原粒子颜色曲线，使用 Alpha 保留消散过程。
            };

            // 传给片元的裁剪坐标和两层可独立滚动的 UV。
            struct WindSample
            {
                float4 position : SV_POSITION; // 相机裁剪空间坐标。
                float2 windUV : TEXCOORD0; // 套用风纹材质偏移后的坐标。
                float2 shapeUV : TEXCOORD1; // 套用遮罩材质偏移后的坐标。
                fixed opacity : TEXCOORD2; // 粒子顶点 Alpha，范围为零至一。
            };

            // 保持原特效动画控制的材质平铺与偏移。
            WindSample Vert(WindVertex input) // 输入为单个网格或粒子顶点。
            {
                WindSample sample; // 输出为这个顶点的片元插值数据。
                sample.position = UnityObjectToClipPos(input.position);
                sample.windUV = TRANSFORM_TEX(input.uv, _BaseRGBA);
                sample.shapeUV = TRANSFORM_TEX(input.uv, _ShapeRGBA);
                sample.opacity = input.color.a;
                return sample;
            }

            // 用原纹理的形状着色为浅色风纹，黑色像素只贡献零透明度。
            fixed4 Frag(WindSample sample) : SV_Target // 当前片元的双层 UV 和顶点 Alpha。
            {
                half4 wind = tex2D(_BaseRGBA, sample.windUV); // 原风纹或粒子贴片采样。
                half shape = tex2D(_ShapeRGBA, sample.shapeUV).r; // 红色通道提供风柱的软边形状。
                half brightness = max(wind.r, max(wind.g, wind.b)); // RGB 峰值用于滤掉粒子的纯黑底。
                half coverage = wind.a * lerp(shape, brightness, _SpriteMask); // 叠加原贴图的 Alpha 边缘。
                half vertexOpacity = lerp(1, sample.opacity, _SpriteMask); // 网格只用材质淡出，粒子叠加顶点淡出。
                half alpha = smoothstep(0.035h, 0.78h, coverage) * saturate(_Alpha) * vertexOpacity * _MistTint.a; // 过滤后的软边透明度。
                half3 color = _MistTint.rgb * lerp(0.72h, 1.0h, brightness); // 风纹保留明暗起伏，暗部仍是浅蓝灰色。
                return fixed4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
