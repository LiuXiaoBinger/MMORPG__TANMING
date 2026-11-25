Shader "LYF/LostArk/StoneAndTimber"
{
    Properties
    {
        // 构造面使用原包石料、木板、屋瓦的可平铺底色。
        _MainTex ("饰面纹理", 2D) = "white" {}
        _Color ("饰面颜色", Color) = (1,1,1,1)
        // 纹理重复一次覆盖的世界距离，单位为米。
        _Meters ("纹理尺度（米）", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        // 不同尺寸的立面构件共用材质与世界坐标尺度。
        sampler2D _MainTex;
        fixed4 _Color;
        float _Meters;
        // 世界位置用于铺纹，表面朝向决定各投影的采样比例。
        struct Input { float3 worldPos; float3 worldNormal; };
        // i 为当前构造面的位置与法线，o 输出漫反射颜色。
        void surf(Input i, inout SurfaceOutput o)
        {
            // 三轴按法线权重混合，避免斜屋面与拱石的纹理拉伸。
            float3 weight = pow(abs(i.worldNormal),4);
            weight /= max(.001,weight.x + weight.y + weight.z);
            // 换算为以材质尺度为单位的世界铺纹坐标。
            float3 p = i.worldPos / _Meters;
            fixed3 tex = tex2D(_MainTex,p.zy).rgb * weight.x + tex2D(_MainTex,p.xz).rgb * weight.y + tex2D(_MainTex,p.xy).rgb * weight.z;
            o.Albedo = tex * _Color.rgb;
            o.Emission = o.Albedo * .08;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
