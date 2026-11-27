// 逐月的剑气和残影共用一张材质，颜色与淡出由各自粒子时间轴传入。
Shader "LYF/Skills/MoonChaseLight"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 粒子顶点位置、颜色、表面法线和约定的剑痕纹理坐标。
            struct ParticleVertex
            {
                float4 vertex : POSITION;  // 粒子网格顶点。
                fixed4 color : COLOR;      // 原色与粒子生命周期透明度。
                float3 normal : NORMAL;    // 人形残影的表面方向。
                float2 uv : TEXCOORD0;     // x 沿剑光长度，负值标记残影。
            };

            // 投影后的插值数据，用于在屏幕像素内绘制剑锋或残影。
            struct SurfaceVertex
            {
                float4 pos : SV_POSITION;  // 裁剪空间坐标。
                fixed4 color : COLOR;      // 当前阶段色及淡出权重。
                float2 uv : TEXCOORD0;     // y 从剑锋外缘向内变化。
                float rim : TEXCOORD1;     // 接近轮廓时趋近一。
            };

            // 计算单个输入顶点投影和人形轮廓的侧光强度。
            SurfaceVertex vert(ParticleVertex input)
            {
                SurfaceVertex output; // 交给像素阶段的剑光数据。
                output.pos = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                float3 view = normalize(WorldSpaceViewDir(input.vertex)); // 从顶点指向相机。
                float3 normal = UnityObjectToWorldNormal(input.normal);  // 世界空间单位法线。
                output.rim = 1.0 - abs(dot(view, normal));
                return output;
            }

            // 按插值坐标绘制窄白锋、蓝紫内纹和半透明人形。
            fixed4 frag(SurfaceVertex input) : SV_Target
            {
                if (input.uv.x < -0.5)
                {
                    float rim = pow(saturate(input.rim), 2.6); // 保留衣袖和肩颈的侧缘。
                    return fixed4(lerp(input.color.rgb, float3(0.76, 0.94, 1), rim),
                        input.color.a * (0.12 + 0.72 * rim));
                }
                float edge = saturate(input.uv.y); // 零表示月牙锐利的外锋。
                float core = exp2(-9.0 * edge); // 仅在外侧形成白色细线。
                float vein = pow(0.5 + 0.5 * cos(40.0 * edge + 3.4 * sin(input.uv.x * 12.0)), 3.0); // 随剑弧弯曲的丝纹。
                float taper = saturate(sin(3.14159 * saturate(input.uv.x)) * 6.0); // 两端自然收尖。
                float opacity = taper * (1.0 - edge) * (0.76 + 0.18 * vein + 0.26 * core); // 每条剑痕向内变透。
                return fixed4(lerp(input.color.rgb, float3(0.90, 0.98, 1.0), core * 0.92), input.color.a * opacity);
            }
            ENDCG
        }
    }
}
