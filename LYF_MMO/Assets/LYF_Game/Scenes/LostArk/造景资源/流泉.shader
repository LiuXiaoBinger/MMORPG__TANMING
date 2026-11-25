Shader "LYF/LostArk/Fountain"
{
    Properties
    {
        // 泉柱主体颜色和泡沫颜色共同控制透光度。
        _Color ("泉水", Color) = (0.27,0.70,0.81,0.58)
        _Foam ("泡沫", Color) = (0.91,0.98,1,0.92)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            // 泉体与泡沫颜色，透明度由各自的 alpha 分量给定。
            fixed4 _Color, _Foam;
            // 顶点包含沿水柱长度铺设的 UV，纵向从出水口指向落水处。
            struct StreamPoint { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            // 裁剪空间坐标、水柱纹理坐标以及雾因子供像素阶段使用。
            struct StreamPixel { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            // 将泉柱顶点 v 转为可投影的像素插值数据。
            StreamPixel vert(StreamPoint v)
            {
                // 返回本顶点的裁剪空间位置、流向坐标和雾因子。
                StreamPixel o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            // 根据像素 i 的纵向坐标和游戏时间计算流动的泡沫亮带。
            fixed4 frag(StreamPixel i):SV_Target
            {
                // 泡沫沿抛物线朝落水点前进，横向扰动打散整齐的横条。
                float travel=i.uv.y*9.0-_Time.y*2.15;
                // 两层不同频率的褶皱叠加产生断续高光。
                half foam=smoothstep(0.42,0.95,sin(travel*6.283+sin(i.uv.x*18.85))*.55+sin(travel*12.566-i.uv.x*12.566)*.45);
                // 输出颜色同时参与透明混合与场景雾。
                fixed4 col=lerp(_Color,_Foam,foam);
                UNITY_APPLY_FOG(i.fogCoord,col);
                return col;
            }
            ENDCG
        }
    }
}
