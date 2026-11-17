// 为 Role_MJS 提供不依赖场景光源的角色颜色：用法线与视线的夹角计算明暗和轮廓色。
// 颜色 Pass 不读取灯光、环境光、光照探针或光照贴图；另一个 Pass 提供与透明度裁剪一致的深度/阴影轮廓。
Shader "LYF/RoleMjsAlwaysLit"
{
    Properties
    {
        _MainTex ("基础色贴图", 2D) = "white" {}              // 角色反照率贴图，保留肤色与衣服花纹区别
        _Color ("基础色叠加", Color) = (1,1,1,1)              // 与贴图相乘的整体染色，默认不改变原色
        _Brightness ("整体亮度", Range(0.2, 4)) = 1.28        // 常亮倍率，数值越大越亮
        _ShadeMin ("边缘保底系数", Range(0.2, 1.5)) = 0.88    // 法线与视线垂直时使用的系数，控制侧缘明暗
        _ShadeMax ("正面增强系数", Range(0.5, 2)) = 1.05      // 法线与视线平行或反向平行时使用的系数；未被剔除的背面也适用
        _Lift ("暗部抬升", Range(0, 0.5)) = 0.08              // 给计算后的 RGB 全部加上同一值，暗部和亮部都会变亮
        _RimColor ("轮廓光颜色", Color) = (0.10,0.13,0.18,1)  // 边缘额外叠加的 RGB 颜色，Alpha 不参与颜色计算
        _RimPower ("轮廓光收敛", Range(0.5, 8)) = 5.0         // 越大轮廓越细
        _Cutoff ("Alpha 裁剪阈值", Float) = -0.01             // 丢弃贴图 Alpha 乘染色 Alpha 后低于此值的像素；负值通常保留非负 Alpha 像素
        _Cull ("剔除模式", Float) = 2                          // 0 不剔除，1 剔除正面，2 剔除背面
    }
    SubShader
    {
        // 颜色按不透明物体排序；Alpha 只用来裁剪，保留下来的像素输出 Alpha 为 1。
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
            float4 _MainTex_ST;          // xy 是 UV 缩放，zw 是 UV 偏移
            fixed4 _Color;               // 基础色叠加
            float _Brightness;           // 整体亮度倍率
            float _ShadeMin;             // 朝向权重为 0 时的明暗系数
            float _ShadeMax;             // 朝向权重为 1 时的明暗系数，不区分正反法线
            float _Lift;                 // 无条件加到最终 RGB 上的亮度值
            fixed4 _RimColor;            // 轮廓叠加颜色，仅使用 RGB
            float _RimPower;             // 轮廓光收敛指数
            float _Cutoff;               // Alpha 裁剪阈值

            // 模型顶点输入，位置用于投影，法线用于朝向明暗，UV 用于取基础色。
            struct appdata
            {
                float4 vertex : POSITION;    // 模型空间顶点
                float3 normal : NORMAL;      // 模型空间法线
                float2 uv : TEXCOORD0;       // 主贴图 UV
            };

            // 传给片元的插值数据，在相机空间比较法线和视线，避免依赖场景灯光。
            struct v2f
            {
                float4 pos : SV_POSITION;      // 裁剪空间位置
                float2 uv : TEXCOORD0;         // 主贴图 UV
                float3 viewNormal : TEXCOORD1; // 视空间法线，用于体积明暗
                float3 viewDir : TEXCOORD2;    // 视空间视线方向（由表面指向相机）
            };

            // 变换 v 的位置、UV 和法线；返回的 o 供光栅化后逐像素计算颜色。
            v2f vert(appdata v)
            {
                // 当前顶点输出，保留未归一化的法线和视线，片元阶段再归一化。
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 逆转置模型视图矩阵用于正确变换法线，包含模型缩放的影响。
                o.viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                // 当前顶点的世界位置，用来计算从表面指向相机的方向。
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = mul((float3x3)UNITY_MATRIX_V, _WorldSpaceCameraPos - worldPos.xyz);
                return o;
            }

            // i 是当前像素的插值结果；先按 Alpha 裁剪，再计算基础亮度与轮廓叠加。
            fixed4 frag(v2f i) : SV_Target
            {
                // 基础贴图乘整体染色；RGB 参与着色，Alpha 只参与下面的裁剪。
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                clip(tex.a - _Cutoff);

                // 插值后重新归一化的相机空间法线。
                float3 n = normalize(i.viewNormal);
                // 从当前表面指向相机的单位方向，与法线处在同一坐标空间。
                float3 v = normalize(i.viewDir);
                // 取法线与视线点积的绝对值：面向和背向相机都接近 1，侧缘接近 0，不读取灯光方向。
                float ndv = saturate(abs(dot(n, v)));
                // 按朝向在两个可调系数之间插值，不额外保证 Min 参数小于 Max。
                float shade = lerp(_ShadeMin, _ShadeMax, ndv);
                // 侧缘权重；指数越大，非侧缘部分衰减越快，轮廓带通常越窄。
                float rim = pow(1.0 - ndv, _RimPower);

                // 先乘贴图、整体亮度和朝向系数，再对所有 RGB 加 Lift，最后叠加轮廓色。
                float3 col = tex.rgb * _Brightness * shade + _Lift + _RimColor.rgb * rim;
                return fixed4(col, 1.0);
            }
            ENDCG
        }

        // 为 Unity 深度和阴影绘制提供角色轮廓；使用与颜色 Pass 相同的贴图 Alpha 和阈值裁剪。
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            sampler2D _MainTex; // 与颜色 Pass 共用的基础贴图，Alpha 用于匹配可见轮廓。
            float4 _MainTex_ST; // 与颜色 Pass 共用的贴图缩放和偏移。
            fixed4 _Color;     // 仅使用 Alpha，使深度裁剪与材质染色的透明度一致。
            float _Cutoff;     // 与颜色 Pass 共用的裁剪阈值，低于阈值的片元不写入深度/阴影。

            // 深度 Pass 的顶点插值数据，覆盖方向光/聚光灯与点光源阴影变体。
            struct ShadowInterpolator
            {
                V2F_SHADOW_CASTER;      // Unity 定义的裁剪空间位置及点光源深度所需向量。
                float2 uv : TEXCOORD1; // 基础贴图采样坐标，用于与颜色 Pass 一致的 Alpha 裁剪。
            };

            // v 是渲染器提供的位置、法线和 UV；把变换与阴影偏移交给 Unity 宏，再输出对应贴图坐标。
            ShadowInterpolator vertShadow(appdata_base v)
            {
                ShadowInterpolator o; // 当前顶点的深度位置与基础贴图坐标。
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            // i 是当前片元的插值结果；丢弃与颜色 Pass 相同的像素后输出 Unity 标准阴影深度。
            float4 fragShadow(ShadowInterpolator i) : SV_Target
            {
                half alpha = tex2D(_MainTex, i.uv).a * _Color.a; // 当前片元实际参与裁剪的透明度。
                clip(alpha - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack Off
}
