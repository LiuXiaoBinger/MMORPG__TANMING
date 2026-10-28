// RoleMjsAlwaysLit.shader
// 用途：魔神塔场景 Role_MJS 测试实例专用「常亮」着色器（第2轮修订）。
// 特点：完全不采样 Unity 方向光/点光/环境光/SH/探针/光照贴图，只按视空间法线做体积明暗，
//       因此关闭全部场景灯光、环境光设黑后角色亮度不变，且正面/背面/左右都清晰可见。
Shader "LYF/RoleMjsAlwaysLit"
{
    Properties
    {
        _MainTex ("基础色贴图", 2D) = "white" {}              // 角色反照率贴图，保留肤色与衣服花纹区别
        _Color ("基础色叠加", Color) = (1,1,1,1)              // 与贴图相乘的整体染色，默认不改变原色
        _Brightness ("整体亮度", Range(0.2, 4)) = 1.28        // 常亮倍率，数值越大越亮
        _ShadeMin ("边缘保底系数", Range(0.2, 1.5)) = 0.88    // 侧面与背面的最低亮度系数，避免死黑
        _ShadeMax ("正面增强系数", Range(0.5, 2)) = 1.05      // 正对相机时的亮度系数，形成体积层次
        _Lift ("暗部抬升", Range(0, 0.5)) = 0.08              // 加法抬升暗部，让深色皮革也看得见而不冲掉亮部细节
        _RimColor ("轮廓光颜色", Color) = (0.10,0.13,0.18,1)  // 边缘叠加的冷色轮廓，强化剪影
        _RimPower ("轮廓光收敛", Range(0.5, 8)) = 5.0         // 越大轮廓越细
        _Cutoff ("Alpha 裁剪阈值", Float) = -0.01             // 负值表示不做裁剪，仅头发等卡片才启用
        _Cull ("剔除模式", Float) = 2                          // 0=双面 1=正面 2=背面，按原材质沿用
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull [_Cull]
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;          // 基础色贴图采样器
            float4 _MainTex_ST;          // 贴图缩放与偏移
            fixed4 _Color;               // 基础色叠加
            float _Brightness;           // 整体亮度倍率
            float _ShadeMin;             // 边缘保底系数
            float _ShadeMax;             // 正面增强系数
            float _Lift;                 // 暗部抬升量
            fixed4 _RimColor;            // 轮廓光颜色
            float _RimPower;             // 轮廓光收敛指数
            float _Cutoff;               // Alpha 裁剪阈值

            struct appdata
            {
                float4 vertex : POSITION;    // 模型空间顶点
                float3 normal : NORMAL;      // 模型空间法线
                float2 uv : TEXCOORD0;       // 主贴图 UV
            };

            struct v2f
            {
                float4 pos : SV_POSITION;      // 裁剪空间位置
                float2 uv : TEXCOORD0;         // 主贴图 UV
                float3 viewNormal : TEXCOORD1; // 视空间法线，用于体积明暗
                float3 viewDir : TEXCOORD2;    // 视空间视线方向（由表面指向相机）
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = mul((float3x3)UNITY_MATRIX_V, _WorldSpaceCameraPos - worldPos.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                clip(tex.a - _Cutoff);

                float3 n = normalize(i.viewNormal);
                float3 v = normalize(i.viewDir);
                // 用 |N·V| 作为朝向权重：正对相机接近 1，轮廓处接近 0，与任何灯光无关
                float ndv = saturate(abs(dot(n, v)));
                float shade = lerp(_ShadeMin, _ShadeMax, ndv);
                float rim = pow(1.0 - ndv, _RimPower);

                float3 col = tex.rgb * _Brightness * shade + _Lift + _RimColor.rgb * rim;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
