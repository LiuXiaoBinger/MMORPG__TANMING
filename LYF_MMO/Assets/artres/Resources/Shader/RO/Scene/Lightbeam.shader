// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Lightbeam/Lightbeam" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Width ("Width", Float) = 8.71
		_Tweak ("Tweak", Float) = 0.65
	}
	SubShader {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True"}
		Pass {
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Lighting Off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			fixed4 _Color;
			fixed _Width;
			fixed _Tweak;

			struct v2f 
			{
			    float4 pos : SV_POSITION;
			    float4 falloffUVs : TEXCOORD0;
			    float4 screenPos : TEXCOORD1;
			};
			
			v2f vert (appdata_tan v)
			{
			    v2f o;			    		
			    o.pos = UnityObjectToClipPos( v.vertex );
								
				// Generate the falloff texture UVs
				TANGENT_SPACE_ROTATION;
				float3 refVector = mul(rotation, normalize(ObjSpaceViewDir(v.vertex)));

				fixed z = sqrt((refVector.z + _Tweak) * _Width);
				fixed2 xy = (refVector.xy / z) + 0.5;

				o.falloffUVs = fixed4(xy.x, v.texcoord.y, xy);
				
				o.screenPos = ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.screenPos.z);
								
			    return o;
			}
			
			
			fixed4 frag( v2f In ) : COLOR
			{			
				fixed falloff1 = tex2D(_MainTex, In.falloffUVs.xy).r;
				fixed falloff2 = tex2D(_MainTex, In.falloffUVs.zw).g;
				
				fixed4 c = _Color;
				c.a *= falloff1 * falloff2;

				// Fade when near the camera
				c.a *=  saturate(In.screenPos.z * 0.2);

			    return c;
			}
			
			ENDCG
		}
	} 
}
