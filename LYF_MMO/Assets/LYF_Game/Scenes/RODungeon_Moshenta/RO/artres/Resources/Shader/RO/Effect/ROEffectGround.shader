/*
 *      Author: Starking. 
 *      Version: 18.01.29
 */
 
Shader "RO/Effect/AllInOne Ground" {
	Properties {
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Main Texture", 2D) = "white" {}
		_MainColor("Main Color", Color) = (1,1,1,1)

		[KeywordEnum(NONE,ALPHAMASK,COLORGRAD,DISSOLVE,ADDITIVE,ALPHABLEND,COLORBLEND,FLOWMAP,DISTORT)] _SecondType("Second Texture Type", float) = 0
		
		[Space]
		_MaskTex("Second Texture", 2D) = "white" {}
		[Enum(R,0,G,1,B,2,A,3)]_MaskChannel("Mask Channel",float) = 0
		_MaskColor("Mask Color", Color) = (1,1,1,1)
		_Strength("Strength", Range(0, 1)) = 1

		[Space]
		_Msk2Tex("Third Texture", 2D) = "white" {}
		[Enum(R,0,G,1,B,2,A,3)]_Msk2Channel("Third Channel",float) = 0
		[MaterialToggle]_ThirdMap("Use Third Texture?",float) = 0
		_Msk2Strength("Third Strength", Range(0, 1)) = 1

		[Space]
		_Cutoff("Cutoff" ,Range(0,1)) = 0
		_HardFade("HardFade" ,Range(0,1)) = 0
		_EdgeColor("Edge Color", Color) = (1,1,1,1)
		_EdgeWidth("Edge Width", Range(0,3)) = 0.15
		
		[Space]
		_Hue("Hue", Range(0,360)) = 0
		_Saturate("Saturate", Range(0,2)) = 1
		_Luminance("Luminance", Range(0,10)) = 1
		_Contrast("Contrast", Range(0,10)) = 1
		
		[Space]
		[KeywordEnum(NONE,SCROLL,ANISPRITES,MULTIBLEND,SCALE,ROTATE)] _ScrollType("Animation Type", float) = 0
		_ScrollMain("Main Count (UV), Speed (UV)", vector) = (1,1,0,0)
		_ScrollMask("Second Count (UV), Speed (UV)", vector) = (1,1,0,0)
		_ScrollMain2("Main Opacity|CutOff|Direction|RollSpeed", vector) = (-1,0,0,0)
		_ScrollMask2("Mask Opacity|CutOff|Direction|RollSpeed", vector) = (-1,0,0,0)
		
		[Space]
		[Toggle]_VertColorAlphaIsLifeTime("Use Vert Color (A) Ctrl?",float) = 0
		[Toggle]_PreserveVertAlpha("Preserve Vert Color (A)?",float) = 0
		
		[Space]
		[MaterialToggle]_Fog("Fog", float) = 1

		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", float) = 0
		[HideInInspector] _Mode("Blend Mode", float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", float) = 0
	}
	
	SubShader{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent-10" "PreviewType" = "Plane" }
		LOD 300

		Stencil
		{
			Ref 25//与地面40 和 非地面遮挡30比较，若非地面开启遮挡则为30，判断不通过，未开启时，Ref为0肯定通过
			Comp LEqual
		}

		Pass {
			NAME "NORMAL"
			Tags { "LightMode" = "Always" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite Off
			ZTest Always
			Cull [_Cull]
			ColorMask RGB
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SCENE_MAP_ON
			#define _FOG_ON
			//#pragma fragmentoption ARB_precision_hint_fastest
			#pragma shader_feature __ _SECONDTYPE_ALPHAMASK _SECONDTYPE_COLORGRAD _SECONDTYPE_DISSOLVE _SECONDTYPE_ADDITIVE _SECONDTYPE_ALPHABLEND _SECONDTYPE_COLORBLEND _SECONDTYPE_FLOWMAP _SECONDTYPE_DISTORT
			#pragma shader_feature __ _SCROLLTYPE_SCROLL _SCROLLTYPE_ANISPRITES _SCROLLTYPE_MULTIBLEND _SCROLLTYPE_SCALE _SCROLLTYPE_ROTATE
			#pragma shader_feature __ _THIRDMAP_ON
			//#pragma shader_feature __ _FOG_ON
			#pragma shader_feature __ _PRE_ON

			#include "../Include/EffectCommon.cginc"

			ENDCG 
		}	
	}

	SubShader{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent-10" "PreviewType" = "Plane" }
		LOD 100

		Stencil
		{
			Ref 25//与地面40 和 非地面遮挡30比较，若非地面开启遮挡则为30，判断不通过，未开启时，Ref为0肯定通过
			Comp LEqual
		}

		Pass {
			NAME "NORMAL"
			Tags { "LightMode" = "Always" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite Off
			ZTest Always
			Cull [_Cull]
			ColorMask RGB
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma shader_feature __ _SECONDTYPE_ALPHAMASK _SECONDTYPE_COLORGRAD _SECONDTYPE_DISSOLVE _SECONDTYPE_ADDITIVE _SECONDTYPE_ALPHABLEND _SECONDTYPE_COLORBLEND _SECONDTYPE_FLOWMAP _SECONDTYPE_DISTORT
			#pragma shader_feature __ _SCROLLTYPE_SCROLL _SCROLLTYPE_ANISPRITES _SCROLLTYPE_MULTIBLEND _SCROLLTYPE_SCALE _SCROLLTYPE_ROTATE
			#pragma shader_feature __ _THIRDMAP_ON
			#pragma shader_feature __ _PRE_ON
			
			#include "../Include/EffectCommon.cginc"

			ENDCG 
		}	
	}

	Fallback Off
	CustomEditor "ROEffectShaderGUI"
}
