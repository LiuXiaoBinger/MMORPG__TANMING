Shader "LYF/MjsBarrierRim"
{
    Properties
    {
        _TintColor ("护罩边缘颜色和透明度", Color) = (0.30, 0.71, 1, 0.58)
        [PerRendererData] _Burst ("单次盾爆的开裂进度", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // RGB 是边缘色，A 是正切处的最大不透明度。
            fixed4 _TintColor;
            // 独立罩体的零至一爆裂进度，零保持完整的球面。
            half _Burst;

            // 球面片元绘制所需的插值数据。
            struct RimSample
            {
                float4 clipPosition : SV_POSITION; // 相机裁剪坐标。
                float3 worldPosition : TEXCOORD0; // 球面采样点的世界坐标，单位米。
                float3 worldNormal : TEXCOORD1; // 球面采样点的世界法线。
            };

            // 将球面网格顶点和物体空间法线变换到渲染使用的坐标。
            RimSample vert(appdata_base vertex) // vertex 包含球面顶点的原始位置和法线。
            {
                RimSample sample; // 当前顶点以这份数据插值到片元。
                sample.clipPosition = UnityObjectToClipPos(vertex.vertex);
                sample.worldPosition = mul(unity_ObjectToWorld, vertex.vertex).xyz;
                sample.worldNormal = UnityObjectToWorldNormal(vertex.normal);
                return sample;
            }

            // 按片元的视角绘制薄蓝边缘，让正对相机的罩面接近透明。
            fixed4 frag(RimSample sample) : SV_Target // sample 是当前片元插值后的坐标和法线。
            {
                // 从球面采样点指向相机的单位向量。
                float3 viewDirection = normalize(UnityWorldSpaceViewDir(sample.worldPosition));
                // 视线越贴近球面切线，边缘强度越接近 1。
                half rim = pow(1.0h - saturate(dot(normalize(sample.worldNormal), viewDirection)), 4.0h);
                if (_Burst > 0)
                {
                    // 用球面的方向划分壳片，裂口跨过网格接缝时依然连续。
                    half3 normal = normalize(sample.worldNormal);
                    half nearest = -1, runnerUp = -1; // 最接近的两片壳面中心与法线的点积。
                    half fadeOrder = 0; // 各壳片独立的消散次序。
                    // index 是绕球面分布的壳片序号；固定采样点可在编译时展开。
                    [unroll] for (int index = 0; index < 24; index++)
                    {
                        float height = 1.0 - (index + 0.5) / 12.0; // 该中心在单位球上的高度。
                        float ring = sqrt(1.0 - height * height); // 该高度的纬圈半径。
                        float angle = index * 2.399963 + 0.31 * sin(index * 1.71); // 稍微错开相邻中心的经度。
                        half alignment = dot(normal, float3(ring * cos(angle), height, ring * sin(angle))); // 中心方向与此片元的接近程度。
                        if (alignment > nearest)
                        {
                            runnerUp = nearest;
                            nearest = alignment;
                            fadeOrder = frac((index + 1) * 0.618034);
                        }
                        else runnerUp = max(runnerUp, alignment);
                    }
                    half gap = nearest - runnerUp; // 两片中心等距处为零，对应实际裂缝。
                    half opening = lerp(0.002h, 0.12h, _Burst); // 爆开时裂缝从细线向两侧张开。
                    half surface = smoothstep(opening, opening + 0.01h, gap); // 裂缝内剔除壳面。
                    half edge = 1 - smoothstep(opening + 0.01h, opening + 0.045h, gap); // 壳片新露出的亮边。
                    half fade = 1 - smoothstep(0.38h + 0.23h * fadeOrder, 0.74h + 0.26h * fadeOrder, _Burst); // 各片逐一淡出。
                    return fixed4(lerp(_TintColor.rgb, fixed3(0.84h, 0.96h, 1), 0.3h + 0.7h * edge),
                        saturate(_TintColor.a * (0.29h + rim + 0.9h * edge) * surface * fade));
                }
                return fixed4(_TintColor.rgb, _TintColor.a * rim);
            }
            ENDCG
        }
    }
}
