// 公共室内静态建筑采样 RO 原始色图与第二套 UV 上的烘焙光照。
// EXR 的 RGB 为线性照度，Alpha 是 RO 自用数据；人物和未单独展开光照 UV 的小件取样本场景烘焙探针。
Shader "LYF/Scene/RO Interior Baked"
{
    Properties
    {
        _MainTex ("建筑色图", 2D) = "white" {}
        _BakedLight ("原场景烘焙光照", 2D) = "white" {}
        _BakedWeight ("是否使用本批网格的烘焙 UV", Range(0,1)) = 1
        _Cutoff ("镂空材质透明阈值", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;       // 建筑原始漫反射贴图。
            sampler2D _BakedLight;    // 线性 HDR 贴图；独立于平台原生光照图编码。
            float4 _MainTex_ST;       // 原材质的平铺与偏移。
            half _BakedWeight;        // 未展开光照 UV 的小件改用烘焙探针。
            half _Cutoff;             // 铁艺或布边的镂空阈值。

            // 静态合并网格的顶点，以及原色图和烘焙图的两套坐标。
            struct RoomVertex
            {
                float4 position : POSITION;      // 模型空间顶点，单位米。
                float3 normal : NORMAL;          // 用于小件探针取光的表面法线。
                float2 uv : TEXCOORD0;           // 手绘材质坐标。
                float2 lightUV : TEXCOORD1;      // 原烘焙图集内的坐标。
            };
            // 裁剪空间位置与插值后的两套采样坐标。
            struct RoomVarying
            {
                float4 position : SV_POSITION;   // 相机裁剪空间顶点。
                float2 uv : TEXCOORD0;           // 插值后用于漫反射采样的坐标。
                float2 lightUV : TEXCOORD1;      // 插值后用于光照图采样的坐标。
                half3 probe : TEXCOORD2;         // 由房间烘焙探针给出的线性漫射光。
            };

            // 将建筑顶点投影到相机，烘焙图集的缩放偏移已写入第二套 UV。
            RoomVarying vert(RoomVertex vertex)
            {
                RoomVarying output; // 当前顶点传给片元的插值数据。
                output.position = UnityObjectToClipPos(vertex.position);
                output.uv = TRANSFORM_TEX(vertex.uv, _MainTex);
                output.lightUV = vertex.lightUV;
                output.probe = ShadeSH9(half4(UnityObjectToWorldNormal(vertex.normal), 1));
                return output;
            }

            // 用原场景照度还原门厅的明暗，并给手绘纹理保留可读的阴影细节。
            half4 frag(RoomVarying input) : SV_Target
            {
                half4 color = tex2D(_MainTex, input.uv); // 表面原色与镂空遮罩。
                clip(color.a - _Cutoff);
                half3 light = tex2D(_BakedLight, input.lightUV).rgb; // 线性 RGB 照度。
                half3 illumination = clamp(0.26h + 1.12h * light, 0.26h, 1.85h); // 压住灯烛和窗边的高光。
                color.rgb *= lerp(clamp(input.probe, 0.26h, 1.85h), illumination, _BakedWeight);
                return half4(color.rgb, 1);
            }
            ENDCG
        }
    }
}