Shader "LYF/MjsBarrierLight"
{
    SubShader
    {
        Tags { "Queue"="Transparent+15" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ColorMask RGB
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 粒子系统用四格横向图集坐标选择白闪、冰片、星点或冲击环。
            struct LightSample
            {
                float4 position : SV_POSITION; // 面向镜头或水平放置的粒子裁剪坐标。
                float2 uv : TEXCOORD0; // 四格横向坐标，纵向保持零到一。
                fixed4 tint : COLOR; // 当前粒子生命周期曲线给出的颜色及透明度。
            };

            // 由粒子的实际姿态展开光片，保留发射器传来的颜色和图集格。
            LightSample vert(appdata_full vertex) // vertex 是当前粒子四边形的原始顶点。
            {
                LightSample sample; // 需要在屏幕空间插值的顶点结果。
                sample.position = UnityObjectToClipPos(vertex.vertex);
                sample.uv = vertex.texcoord.xy;
                sample.tint = vertex.color;
                return sample;
            }

            // 在同一材质内绘制各发射器的光片轮廓。
            fixed4 frag(LightSample sample) : SV_Target // sample 是当前片元的图集位置与粒子颜色。
            {
                float frame = min(sample.uv.x * 4.0, 3.99999); // 整数部分为光片类型，小数部分为格内横坐标。
                float2 p = float2(frac(frame), sample.uv.y) * 2 - 1; // 格心为原点，边缘为正负一。
                half radius = length(p); // 星芒和圆环相对于自身半径的位置。
                half angle = atan2(p.y, p.x); // 围绕格心的角度，用于错开射线和环面高光。
                half alpha; // 此格的透明度轮廓。
                half ice; // 高亮部位从浅蓝混向白色的比例。
                if (frame < 1)
                {
                    half rays = pow(abs(cos(7 * angle + 1.4h * sin(3 * angle))), 22); // 长短不一的放射状锐光。
                    half core = exp2(-12 * radius * radius); // 闪光中心的柔亮面积。
                    alpha = saturate(core + rays * (1 - smoothstep(0.08h, 0.97h, radius)));
                    ice = saturate(core * 2 + rays * 0.6h);
                }
                else if (frame < 2)
                {
                    float crossCut = p.x - 0.18 * p.y; // 冰晶的斜轴，让长尖与短底错开。
                    float envelope = min(0.53 * (1.0 - p.y), 0.96 * (p.y + 0.91)); // 两个斜切端相交形成晶柱尖端。
                    float inside = min(envelope, 0.53) - abs(crossCut); // 平行的侧棱和斜切端围出的晶体轮廓。
                    float ridge = crossCut + 0.15 - 0.16 * p.y; // 偏心棱线分出明暗大小不同的切面。
                    half face = smoothstep(-0.035, 0.035, ridge); // 朝光的晶面比背光面明亮。
                    half edge = 1.0h - smoothstep(0.015h, 0.105h, min(inside, abs(ridge))); // 外边和内棱上的冰白反光。
                    ice = saturate(0.12h + 0.36h * face + 0.65h * edge);
                    alpha = smoothstep(-0.028, 0.028, inside) * (0.42h + 0.46h * face + 0.12h * edge);
                }
                else if (frame < 3)
                {
                    half core = exp2(-15 * dot(p, p)); // 小冰屑白色的中心。
                    half spike = exp2(-12 * (abs(p.x) * 3 + abs(p.y) * 0.33)); // 细长的星点拖光。
                    alpha = saturate(core + spike) * (1 - smoothstep(0.7h, 1.0h, radius));
                    ice = 0.48h * saturate(core + spike * 0.4h);
                }
                else
                {
                    half crest = exp2(-70 * abs(radius - 0.86h)); // 横向冲击波的细亮波峰。
                    half mist = exp2(-14 * abs(radius - 0.77h)); // 波峰后较宽的淡蓝雾光。
                    half broken = 0.66h + 0.34h * sin(3 * angle + sin(5 * angle)); // 环上强弱交替的流光。
                    alpha = saturate((crest + 0.38h * mist) * broken) * (1 - smoothstep(0.94h, 1, radius));
                    ice = crest * 0.72h;
                }
                return fixed4(lerp(sample.tint.rgb, fixed3(0.88h, 0.97h, 1), ice), alpha * sample.tint.a);
            }
            ENDCG
        }
    }
}
