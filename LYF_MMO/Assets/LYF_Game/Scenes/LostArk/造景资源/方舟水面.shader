Shader "LYF/LostArk/Water"
{
    Properties
    {
        // 水体主体颜色与掠射角的天空反光颜色。
        _Color ("水色", Color) = (.10,.38,.40,1)
        _SkyTint ("天光", Color) = (.45,.70,.76,1)
        // 同一张无缝水面图在两个方向叠加，表现不同尺度的水纹。
        _MainTex ("水纹", 2D) = "gray" {}
        // 使用本场景的天空反射纹理，掠射角会显露云层的颜色。
        _SkyCube ("天空倒影", Cube) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            // 材质参数分别描述水体、天空及水面的纹理反射。
            fixed4 _Color, _SkyTint;
            sampler2D _MainTex;
            // 反射方向采样的天空立方体。
            samplerCUBE _SkyCube;
            // 顶点着色器输出世界坐标，供各段水面共享连续波纹。
            struct Interpolator { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float upright : TEXCOORD2; UNITY_FOG_COORDS(1) };
            // v 是水面顶点，返回裁剪空间坐标及雾距离。
            Interpolator vert(appdata_base v)
            {
                // 像素阶段所需的世界坐标和雾数据。
                Interpolator o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.upright=abs(UnityObjectToWorldNormal(v.normal).y);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            // i 指向当前水面位置，根据视线倾角与波峰显示反光。
            fixed4 frag(Interpolator i) : SV_Target
            {
                // 三组行进方向不同的细波改变反射法线，单位为世界米。
                float2 p = i.world.xz;
                // 游戏时间，单位为秒。
                float clock = _Time.y;
                // 低频偏移用于扭曲相邻波峰。
                float bend = sin(p.x*.19 + p.y*.24 - clock*.43);
                // 两个水平分量描述小波的斜率，低频扰动让波峰自然弯曲。
                float sx = .105*cos(dot(p,float2(1.65,.83))-clock*1.13+bend) + .088*cos(dot(p,float2(2.06,-1.86))+clock*1.47);
                // 纵向斜率与横向斜率共同决定反射方向。
                float sz = .16*cos(dot(p,float2(.73,2.81))-clock*1.52+bend)+.085*cos(dot(p,float2(-1.12,4.04))+clock*1.84);
                // 归一化后的水面世界法线。
                float3 n = normalize(float3(sx,1,sz));
                // 从水面指向观察者的方向决定天空反射强度。
                float3 view = normalize(_WorldSpaceCameraPos-i.world);
                // 视线关于水面法线的镜面反射方向。
                float3 ray = reflect(-view,n);
                ray.y = abs(ray.y);
                // 当前反射方向的天空颜色。
                fixed3 sky = texCUBE(_SkyCube,ray).rgb;
                // 掠射角增大时加强天空倒影的权重。
                half fresnel = pow(1-saturate(dot(view,n)),2.5);
                // 流动纹理只调制底水浓淡，避免明亮方格覆盖倒影。
                half tide = tex2D(_MainTex,p*.046+clock*float2(.016,.038)).r;
                fixed4 color = fixed4(lerp(_Color.rgb*(.85+.21*tide),sky,.12+.65*fresnel),1);
                // 天光在狭窄波峰形成零散细光。
                color.rgb += .28*pow(saturate(dot(ray,normalize(float3(-.32,.70,.64)))),72);
                // 泉体原模型的下垂水帘沿高度滚动，垂直面使用独立流向。
                float2 fallUV = float2((p.x+p.y)*.5,i.world.y*.26+clock*.68);
                // 沿帘面下落的高光强度。
                half streak = tex2D(_MainTex,fallUV.yx).r;
                fixed3 curtain = lerp(_Color.rgb*.8,_SkyTint.rgb,smoothstep(.25,.8,streak))+.12*pow(saturate(streak),4);
                color.rgb = lerp(curtain,color.rgb,smoothstep(.22,.78,i.upright));
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
    }
}
