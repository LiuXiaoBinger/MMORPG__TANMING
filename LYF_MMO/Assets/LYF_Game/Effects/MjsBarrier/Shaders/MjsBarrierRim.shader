Shader "LYF/MjsBarrierRim"
{
    Properties
    {
        _TintColor ("护罩边缘颜色和透明度", Color) = (0.30, 0.71, 1, 0.58)
        [PerRendererData] _OpenTime ("本次开盾的关卡时间（秒）", Float) = -10
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
            // 与 Unity 的关卡时钟同源，供各个角色独立播放开盾亮带。
            float _OpenTime;

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
                half3 normal = normalize(sample.worldNormal); // 球面朝向，用来定位两侧银蓝高光和下缘反光。
                half grazing = 1 - saturate(dot(normal, viewDirection)); // 正面接近零，轮廓处接近一。
                half haze = grazing * grazing; // 轮廓向内缓慢衰减的一层薄雾。
                half edge = pow(grazing, 7.0h); // 紧贴几何轮廓的细白边。
                half drift = sin(dot(normal, half3(5, 3, 2)) + _Time.y * 0.62); // 沿球面缓慢移动的反光。
                half glint = smoothstep(0.28h, 0.89h, drift); // 左右明暗变化使球罩保持玻璃质感。
                float age = max(0.0, _Time.y - _OpenTime); // 本次开盾经过的秒数。
                half formed = smoothstep(0.18, 0.7, age); // 起手横扫后显出持续罩面。
                half sweep = saturate(1 - abs(normal.y - (age * 3.0 - 1.4)) * 7); // 由下而上掠过球面的一线亮光。
                half opacity = (0.012h + haze * (0.34h + 0.55h * glint) + 0.62h * edge) * formed;
                // 透出角色和场景，只有弧光经过处以及球罩边缘泛出近白色。
                half3 silver = lerp(_TintColor.rgb, half3(0.90h, 0.95h, 1), 0.44h + 0.48h * glint);
                return fixed4(silver, saturate(_TintColor.a * opacity + sweep * (0.06h + 0.3h * haze)));
            }
            ENDCG
        }
    }
}
