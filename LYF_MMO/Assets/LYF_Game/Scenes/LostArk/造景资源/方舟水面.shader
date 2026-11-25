Shader "LYF/LostArk/Water"
{
    Properties
    {
        // 水体主体颜色与掠射角的天空反光颜色。
        _Color ("水色", Color) = (.10,.38,.40,1)
        _SkyTint ("天光", Color) = (.45,.70,.76,1)
        // 同一张无缝水面图在两个方向叠加，表现不同尺度的水纹。
        _MainTex ("水纹", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            // 材质参数分别描述水体、天空及水面的纹理反射。
            fixed4 _Color, _SkyTint;
            sampler2D _MainTex;
            // 顶点着色器输出世界坐标，供各段水面共享连续波纹。
            struct Interpolator { float4 pos : SV_POSITION; float3 world : TEXCOORD0; UNITY_FOG_COORDS(1) };
            // v 是水面顶点，返回裁剪空间坐标及雾距离。
            Interpolator vert(appdata_base v)
            {
                // 像素阶段所需的世界坐标和雾数据。
                Interpolator o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            // i 指向当前水面位置，根据视线倾角与波峰显示反光。
            fixed4 frag(Interpolator i) : SV_Target
            {
                // 世界坐标下的横纵水纹频率，单位为每米重复数。
                float2 uv = i.world.xz * float2(.053,.14);
                // 两个流向的波形及它们形成的水面亮度。
                half a = tex2D(_MainTex,uv + _Time.y * float2(.011,.009)).r;
                half b = tex2D(_MainTex,uv.yx * float2(.47,1.62) - _Time.y * .006).g;
                half crest = a * .71 + b * .29;
                // 视线越贴近水面，天光反射比例越高。
                half angle = 1 - saturate(normalize(_WorldSpaceCameraPos - i.world).y);
                fixed4 color = lerp(_Color,_SkyTint,saturate(.06 + .55 * angle * angle + .27 * crest));
                color.rgb += (crest-.5)*.13 + smoothstep(.66,.84,crest)*.17;
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
}
