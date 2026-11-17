/*
*      Author: Starking.
*      Version: 18.01.29
*/

Shader "RO/Scene/Water" {
	Properties {
		[NoScaleOffset] _WaterColor ("水色LUT (RGB) 穿透LUT (A) ", 2D) = "gray" {}
		[NoScaleOffset] _BumpMap ("水纹 (凹凸或法线)", 2D) = "bump" {}
		[HDR]_NearColor("岸边色", COLOR) = (0.26, 1.6, 1.1, 1)
		[HDR]_FarColor("地平线色", COLOR) = (0.26, 1.6, 1.1, 1)
		_DepthFade("颜色深度渐变", range(0, 30)) = 0.4
        _AlphaFade("透明深度渐变", range(0, 1)) = 0.1
		_BlurStart("虚化起始", Float) = 150
		_BlurEnd("虚化结束", Float) = 300
		_SoftDistance("弱化距离", Float) = 50

		[Space]
		_WaveScale("波纹参数(1平铺,2平铺,比重,强度)", Vector) = (0.063,0.25,0.5,1)
		_WaveSpeed("流速 (1x,1y,2x,2y)", Vector) = (19,9,-16,-7)
		_WaterClarity("清澈度", range(0,5)) = 2.5
		_RefractDistort("折射扭曲", Range(0,1)) = 0.1
		_ReflectDistort("反射扭曲", Range(0,1)) = 0.2

		[Space]
		_WaterRefl("反光度", range(0,1)) = 1
		_ReflectionTex("倒影 (CUBE)", CUBE) = "" {}

		[Space]
		_SpecularColor("高光色", color) = (1,1,1,1)

		[Space]
		_FogFactor("雾影响", Range(0,1)) = 1

		[Space]
		_FoamTex ("浪花", 2D) = "white" {}
		_FoamFade("浪花宽度", range(0,5)) = 1
		_FoamDistort("浪花扭曲", Range(0,1)) = 1
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Lighting.cginc"

	#ifdef WATER_REFLECTION
		sampler2D _FoamTex; half4 _FoamTex_ST;
		half _FoamDistort;
		sampler2D _GrabTexture;
		sampler2D_float _CameraDepthTexture;
		half _DepthFade;
        half _AlphaFade;
		half _FoamFade;
		half4 _NearColor;
		half _RefractDistort;
		half _WaterClarity;
		half _SoftDistance;

		sampler2D _MirrorReflectionTex;
    	half _WaterRefl;
		half4 _SpecularColor;

		#define SCENE_MAP_ON
		#include "../Include/SceneMap.cginc"
		half _FogFactor;
	#endif

    half4 _ROCLightColor0;
    half4 _FarColor;
    half _BlurStart;
	half _BlurEnd;

	UNITY_DECLARE_TEXCUBE(_ReflectionTex);
	half4 _ReflectionTex_HDR;
	half _ReflectDistort;

	UNITY_DECLARE_TEX2D(_WaterColor);
    UNITY_DECLARE_TEX2D(_BumpMap);
    half4 _WaveScale;
    half4 _WaveSpeed;

	static const half G = -0.999;
	static const half G2 = 0.998001;

	struct appdata
	{
		float3 pos : POSITION;
        half4 color : COLOR;
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 worldPos : TEXCOORD0;
		float4 bumpUV : TEXCOORD1;
        float viewDepth : TEXCOORD2;
        half4 color : COLOR;

		#ifdef WATER_REFLECTION
			float4 screenUV : TEXCOORD3;
		#endif
	};

	v2f vert(appdata v)
	{
		v2f o;
        o.color = v.color;
		o.worldPos = mul(unity_ObjectToWorld, float4(v.pos, 1));
        o.viewDepth = -mul(UNITY_MATRIX_V, o.worldPos).z;

		float4 bump = (o.worldPos.xzxz + _WaveSpeed * _Time.xxxx) * _WaveScale.xxyy;
    	o.bumpUV = bump.xywz + float4(0, 0.5, 0.2, 0.1);

		#ifdef WATER_REFLECTION	
            o.pos = UnityObjectToClipPos(v.pos);
			o.screenUV = ComputeScreenPos(o.pos);
		#else
            o.pos = UnityObjectToClipPos(v.pos);
		#endif

		return o;
	}

	half4 frag(v2f i) : SV_Target
	{
		half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
        half viewDepth = i.viewDepth;

		// normal
		half2 bumpP = saturate(_WaveScale.zw);
        half3 bump1 = UnpackNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.bumpUV.xy));
		half3 bump2 = UnpackNormal(UNITY_SAMPLE_TEX2D(_BumpMap, i.bumpUV.zw));
        half3 bump = lerp(bump1, bump2, bumpP.x);
		bump.xy *= bumpP.y;

		// reflection
		half2 reflectDistort = bump.xy * _ReflectDistort;
		half3 normal = normalize(bump);
    	half3 cubeColor = DecodeHDR(UNITY_SAMPLE_TEXCUBE(_ReflectionTex, reflect(viewDir, normal)), _ReflectionTex_HDR).rgb;

        // blur
		half blur = 1 - saturate((viewDepth - _BlurStart) / (_BlurEnd - _BlurStart));

		#ifdef WATER_REFLECTION
			// depth fade
			half2 screenUV = i.screenUV.xy / i.screenUV.w;
			half sceneViewDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.screenUV.xy / i.screenUV.w));
			half depthDiff = sceneViewDepth - viewDepth;
			half colorFade = saturate(depthDiff / _DepthFade);
            half alphaFade = saturate(depthDiff / _AlphaFade);

			// fresnel
			half fresnel = pow(1.0 - saturate(dot(half3(0, 1, 0), viewDir)), _WaterClarity);

			// color
			half3 baseColor = UNITY_SAMPLE_TEX2D(_WaterColor, half2(fresnel, 0.5));
			half3 waterColor = lerp(_NearColor, baseColor, colorFade);

			// refraction
			half2 refractDistort = bump.xy * _RefractDistort;
			half2 refractUV = screenUV + lerp(0, refractDistort, colorFade);
			refractUV.x = clamp(refractUV.x, 0.001, 0.999);
			refractUV.y = clamp(refractUV.y, 0.001, 0.999);
			half3 refractColor = lerp(tex2D(_GrabTexture, refractUV).rgb, waterColor, colorFade);

			// reflectioin
			half2 reflectUV = screenUV + reflectDistort;
			half3 reflectColor = lerp(lerp(tex2D(_MirrorReflectionTex, reflectUV).rgb, cubeColor, _WaterRefl), _FarColor, saturate((viewDepth - _SoftDistance) / _SoftDistance));

			// specular
			half3 specularDir = normalize(reflect(viewDir, normal));
			half cosine = dot(-_ROCLightDir1, specularDir);
			half miePhase = 1.5 * ((1.0 - G2) / (2.0 + G2)) * (1.0 + cosine * cosine) / pow(1.0 + G2 - 2.0 * G * cosine, 1.5);
			half3 specularColor = _SpecularColor * min(2, miePhase * saturate((bump1.r - bump2.g) * bump2.r));

			// foam
			half noise = tex2D(_FoamTex, (i.bumpUV.xy + bump.xy * _FoamDistort) * _FoamTex_ST.xy + _FoamTex_ST.zw).r;
			half foam_cutoff = saturate(depthDiff / _FoamFade);
			half foam = smoothstep(foam_cutoff - 0.01, foam_cutoff + 0.01, noise);
			half3 foam_color = foam * _ROCSceneColor.rgb * 0.75;

			// final color
			half3 color = lerp(refractColor, reflectColor, fresnel) + specularColor + foam_color;
			color *= lerp(_FarColor, half3(1, 1, 1), blur);

			// final alpha
            half alpha = min(alphaFade, blur);
		#else
			half3 color = _ROCLightColor0.rgb * lerp(_FarColor, cubeColor, blur);
            half alpha = min(i.color.a, blur);
		#endif

		return half4(color, alpha);
	}
	ENDCG

	Subshader {
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue" = "Transparent-10" }
		LOD 600

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			ZTest LEqual ZWrite Off Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			ENDCG
		}
	}

	Subshader {
		Tags { "RenderType"="Transparent" "IgnoreProjector" = "True" "Queue" = "Transparent-10" }
		LOD 400

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual ZWrite Off Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#pragma multi_compile __ WATER_REFLECTION
			ENDCG
		}
	}

	Subshader {
		Tags { "RenderType"="Transparent" "IgnoreProjector" = "True" "Queue" = "Transparent-10" }
		LOD 300

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual ZWrite Off Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			ENDCG
		}
	}

	Subshader {
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue" = "Transparent-10" }
		LOD 200

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			ZTest LEqual ZWrite Off Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			ENDCG
		}
	}

	Subshader {
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue" = "Transparent-10" }
		LOD 100

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			ZTest LEqual ZWrite Off Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	Fallback Off
}
