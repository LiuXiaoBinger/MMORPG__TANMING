Shader "LYF/MjsBarrierRim"
{
    Properties
    {
        _TintColor ("护罩边缘颜色和透明度", Color) = (0.30, 0.71, 1, 0.58)
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
                return fixed4(_TintColor.rgb, _TintColor.a * rim);
            }
            ENDCG
        }
    }
}
