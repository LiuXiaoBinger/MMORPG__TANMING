// 苍蓝屏障自然到期时，在水平冲击波粒子上绘制扩散的冰沿和结晶霜纹。
Shader "LYF/MjsBarrierGroundFrost"
{
    SubShader
    {
        Tags { "Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 单张水平粒子从中心到边缘使用完整纹理坐标。
            struct FrostSample
            {
                float4 position : SV_POSITION; // 贴地粒子顶点的裁剪空间坐标。
                float2 uv : TEXCOORD0; // 整张霜纹从零到一的平面坐标。
                fixed4 tint : COLOR; // 粒子系统提供的色调和随寿命衰减的透明度。
            };

            // 投影水平粒子，保留片元绘制霜纹所需的数据。
            FrostSample vert(appdata_full vertex) // vertex 为粒子四边形的一个顶点。
            {
                FrostSample sample; // 传给光栅化阶段的投影、纹理坐标及色调。
                sample.position = UnityObjectToClipPos(vertex.vertex);
                sample.uv = vertex.texcoord.xy;
                sample.tint = vertex.color;
                return sample;
            }

            // 计算距冰纹中心线的覆盖率，随屏幕像素宽度平滑边缘。
            float iceLine(float distanceToLine, float width, float pixel) // 依次为垂直距离、冰纹半宽及单像素宽度，均以粒子半径为单位。
            {
                return 1.0 - smoothstep(width, width + pixel, abs(distanceToLine));
            }

            // 每个扇区生成一条折线主脉及两侧晶枝，外围用细碎亮边连成冲击环。
            fixed4 frag(FrostSample sample) : SV_Target // sample 给出地面片元在整张霜纹中的位置和当前粒子颜色。
            {
                float2 p = sample.uv * 2.0 - 1.0; // 以脚下为原点，单位为粒子半径。
                float radius = length(p); // 当前片元与脚下的归一化距离。
                float pixel = max(0.001, max(fwidth(p.x), fwidth(p.y))); // 视距变化时细冰枝所占的一个屏幕像素。
                const float sector = 6.2831853 / 13.0; // 十三束冰脉之间的平均角距，单位为弧度。
                float angle = atan2(p.y, p.x + 0.000001); // 地面极角，供冰脉定位及冰沿破碎使用。
                float index = floor(angle / sector + 0.5); // 与片元最接近的冰脉编号。
                float seed = frac((index - 13.0 * floor(index / 13.0)) * 0.618034 + 0.31); // 首尾相接的扇区变化值。
                float direction = (index + (seed - 0.5) * 0.21) * sector; // 晶束方向略有偏差，避免等分刻度感。
                float2 axis = float2(cos(direction), sin(direction)); // 主脉在地面平面内的单位方向。
                float along = dot(p, axis); // 片元沿主脉距根部的距离。
                float across = dot(p, float2(-axis.y, axis.x)); // 片元到主脉轴线的带符号距离。
                float bend = (seed - 0.5) * (0.15 * along - 0.50 * max(along - 0.43, 0.0)); // 冰脉在中段转折后向两侧生长。
                float stemEnd = 0.69 + 0.15 * seed; // 主脉尖端与外圈之间留有长度不等的断口。
                float veins = iceLine(across - bend, lerp(0.013, 0.002, saturate(along / stemEnd)), pixel)
                    * smoothstep(0.11 + 0.07 * seed, 0.25, along) * (1.0 - smoothstep(stemEnd - 0.05, stemEnd, along)); // 主干从角色脚旁逐渐显现，尖端收细。

                [unroll] for (int branch = 0; branch < 3; branch++) // 同一晶束上由内而外的三对分枝。
                {
                    float reach = along - (0.27 + branch * 0.145 + 0.025 * seed); // 片元距这对分枝根部的纵向距离。
                    float span = (0.115 - branch * 0.012) * (0.85 + 0.32 * seed); // 分枝在主方向上的长度。
                    float side = across - bend; // 相对于折线主干的横向位置。
                    float left = iceLine(side - reach * 0.70, 0.004, pixel); // 第一侧晶枝的亮边。
                    float right = iceLine(side + (reach - 0.023) * 0.57, 0.003, pixel); // 另一侧晶枝错开生长。
                    float tips = smoothstep(0.0, 0.012, reach) * (1.0 - smoothstep(span * 0.56, span, reach)); // 晶枝只出现在根部与收尖处之间。
                    veins = max(veins, max(left, right) * tips);
                }

                float rimCenter = 0.86 + 0.004 * sin(13.0 * angle) + 0.003 * sin(29.0 * angle); // 霜沿在约八成六半径处小幅错落。
                float rim = iceLine(radius - rimCenter, 0.005, pixel); // 最先扫过地面的细亮外沿。
                float fringe = (1.0 - smoothstep(0.0, 0.043, abs(radius - rimCenter))) * (0.32 + 0.11 * cos(21.0 * angle)); // 冰沿内外透出一层淡蓝霜气。
                float ice = saturate(max(veins, rim)); // 将主脉、晶枝和外沿合成最亮的结晶部分。
                float3 color = lerp(float3(0.16, 0.58, 0.87), float3(0.79, 0.98, 1.0), ice); // 阴面偏冰蓝，断面和尖端偏白。
                return fixed4(color * sample.tint.rgb, saturate(ice + fringe) * sample.tint.a);
            }
            ENDCG
        }
    }
}
