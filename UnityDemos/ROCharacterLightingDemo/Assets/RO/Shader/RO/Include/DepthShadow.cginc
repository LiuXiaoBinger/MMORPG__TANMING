#ifndef DEPTHSHADOW_INCLUDE
#define DEPTHSHADOW_INCLUDE

#include "unityCG.cginc"

#define FAKE_API_LEVEL ((SHADER_TARGET < 30) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X))

#if defined(DEPTH_SHADOW_ON) && !FAKE_API_LEVEL

CBUFFER_START(ROCommonShadowMap)

	sampler2D_half _DepthShadowMap;
	float4x4 _SMLightV;
	float4x4 _SMLightP;
	float3 _SMLightFwd;
CBUFFER_END

//d = 该像素距离光源的距离
//z = 遮挡物距离光源的距离

inline half VSM_Filter(half2 moments, half d) {

	half variance = moments.y - (moments.x * moments.x);
	variance = max(variance, 2.0e-4);
	half mD = (moments.x - d);
	half p = variance / (variance + mD * mD);
	p = p * (p>0.8);
	return max(p, step(d, moments.x));//p为产生阴影的概率
}

inline half ShadowMapFactor(float3 worldPos)
{
	float4 vPosInLight = mul(_SMLightV, float4(worldPos, 1.0));
	float4 sPosInLight = mul(_SMLightP, vPosInLight);

	half2 uvInLight = sPosInLight.xy;
	uvInLight = uvInLight * 0.5h + 0.5h;

	#if UNITY_UV_STARTS_AT_TOP
	uvInLight.y = 1.0h - uvInLight.y;
	#endif
   
	//距离光源位置0米=1, 30米=0
	half d = 1 - saturate(-vPosInLight.z / 30);

	#if defined(DEPTH_SHADOW_HARD)
		half z = tex2Dlod(_DepthShadowMap, half4(uvInLight, 0, 0)).x;
		//half shadow = step(d + 0.002, z);//ssm
		half shadow = 1 - saturate(exp(120 * (d - z)));//esm
	#else
		half2 moments = tex2Dlod(_DepthShadowMap, half4(uvInLight, 0, 1)).xy;
		half shadow = VSM_Filter(moments, d);//vsm
		half z = moments.x;
	#endif

	//边缘淡化
	half afc = min(0.5, length(uvInLight - 0.5));
	afc = 0.1 - max(0, afc - 0.4);
	half afd = max(0, d - 0.08);

	half fade = saturate(afc * afd * 100);

	return shadow * fade;
}

#endif

#if defined(DEPTH_SHADOW_UI) && !FAKE_API_LEVEL

CBUFFER_START(ROCommonShadowMapUI)

	sampler2D_half _DepthShadowMapUI;
	float4x4 _SMLightVUI;
	float4x4 _SMLightPUI;

CBUFFER_END

half ShadowMapFactor(float3 worldPos)
{
	float4 vPosInLight = mul(_SMLightVUI, float4(worldPos, 1.0));
	float4 sPosInLight = mul(_SMLightPUI, vPosInLight);

	half2 uvInLight = sPosInLight.xy;
	uvInLight = uvInLight * 0.5h + 0.5h;

	#if UNITY_UV_STARTS_AT_TOP
	uvInLight.y = 1.0h - uvInLight.y;
	#endif
   
	//距离光源位置0米=0, 30米=1
	half d = 1 - saturate(-vPosInLight.z / 30);
	half z = tex2D(_DepthShadowMapUI, half4(uvInLight, 0, 0)).x;
	half shadow = 1 - saturate(exp(-120 * (z - d)));//esm

	return saturate(shadow);
}

#endif

#endif