/*
*      Author: Starking.
*      Version: 18.01.29
*/

Shader "RO/Creature/Monster"
{
	Properties
	{ 
		[HideInInspector] _FresnelTense("_FresnelTense",range(0,1)) = 0.2
		[HideInInspector] _SpecularTense("_SpecularTense",range(0,1)) = 0.2
		_Color("技能颜色",Color) = (0.5,0.5,0.5,1)
		_SkillRim("技能外发光", range(0, 1)) = 0
		_SkillRimColor("技能外发光颜色", color) = (0.5,0.75,0.95,1)
		_SkillRimPower("技能外发光强度", range(0.2, 4)) = 0.75
		[MaterialToggle]_Fade("开启半隐", float) = 0
		[HideInInspector]_Visibility("可见度",range(0,1)) = 1	
		[Space]
		_MainTex ("主纹理", 2D) = "white" {}
		_NormalTex ("NormalTex", 2D) = "normalMap"{}
		[MaterialToggle]_MSOE("使用质感纹理", float) = 0
		_MSOETex ("质感(R=金属度，G=光滑度, B=AO, A=自发光)",2D) = "black" {}
		_Metallic("金属度",range(0,1)) = 0
		_Gloss("光滑度",range(0,1)) = 0.5
		_Contrast("对比度", range(0, 2)) = 0.95
		_Brightness("光强", range(0, 2)) = 0.9
		_RimParams("边缘光(xyz=偏向,w=强度)",vector) = (3,1,1,30)
		[MaterialToggle]_Motion("使用表情", float) = 0
		_EmotionID("表情ID",range(0, 8)) = 0
		_EmotionSize("表情区域(xy=格数,zw=纹理范围)", vector) = (1,1,0,0)
		_Falloff("方向衰减(x,y 为区间(-1~1),x<y)", vector) = (-0.25, 0.5, 0, 0)
		[Toggle(ENABLE_POINTLIGHT)] _PointLight("EnablePointLight", Float) = 0
		[Space]
		[MaterialToggle]_Flow("使用FlowMap",float) = 0
		_FlowMap("FlowMap(RG)", 2D) = "white" {}
		_Speed( "Speed", Range( 0.0, 1.0 ) ) = 0.4
		_FlowIntensity( "FlowIntensity", Range( 0.0, 1.0 ) ) = 0.25
		_FlowSignX( "FlowSignX", Range( -1.0, 1.0 ) ) = 1.0
		_FlowSignY( "FlowSignY", Range( -1.0, 1.0 ) ) = -1.0
		_FlowOffset( "FlowOffset", Range( -1.0, 1.0 ) ) = -0.5
	}

	//======================================================================================
	//
	// 编辑模式(支持 normalMap)
	//
	//======================================================================================
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
		LOD 600

		Pass
		{ 
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual Cull Back

			Stencil 
			{
				Ref 1
				Comp Always
				Pass Replace
			}
			
			CGPROGRAM
			#define BODY_NORMAL_MAP
			#define CREATURE_SKILLCOLOR
			#define DEPTH_SHADOW_ON
			#define DEPTH_SHADOW_HARD
			#define _LIGHTM_PBR
			#define BLOOM_ON
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _FLOW_ON
			#pragma shader_feature __ _FADE_ON
			#pragma shader_feature __ _MSOE_ON
			#pragma shader_feature __ _MOTION_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma shader_feature ENABLE_POINTLIGHT
			#pragma exclude_renderers gles
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
		LOD 400

		Pass
		{ 
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual Cull Back

			Stencil 
			{
				Ref 1
				Comp Always
				Pass Replace
			}
			
			CGPROGRAM
			#define CREATURE_SKILLCOLOR
			#define DEPTH_SHADOW_ON
			#define DEPTH_SHADOW_HARD
			#define _LIGHTM_PBR
			#define BLOOM_ON
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _FLOW_ON
			#pragma shader_feature __ _FADE_ON
			#pragma shader_feature __ _MSOE_ON
			#pragma shader_feature __ _MOTION_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma shader_feature ENABLE_POINTLIGHT
			#pragma exclude_renderers gles
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
		LOD 300

		Pass
		{ 
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual Cull Back

			Stencil 
			{
				Ref 1
				Comp Always
				Pass Replace
			}
			
			CGPROGRAM
			#define CREATURE_SKILLCOLOR
			#define _LIGHTM_PBR
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _FLOW_ON
			#pragma shader_feature __ _FADE_ON
			#pragma shader_feature __ _MSOE_ON
			#pragma shader_feature __ _MOTION_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" }
		LOD 200

		Pass
		{ 
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual Cull Back

			Stencil 
			{
				Ref 1
				Comp Always
				Pass Replace
			}
			
			CGPROGRAM
			#define CREATURE_SKILLCOLOR
			#define _LIGHTM_SIMPLE
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#pragma shader_feature __ _FLOW_ON
			#pragma shader_feature __ _FADE_ON
			#pragma shader_feature __ _MSOE_ON
			#pragma shader_feature __ _MOTION_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5

			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" }
		LOD 100

		Pass
		{ 
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual Cull Back

			Stencil 
			{
				Ref 1
				Comp Always
				Pass Replace
			}
			
			CGPROGRAM
			#define CREATURE_SKILLCOLOR
			#pragma shader_feature __ _FADE_ON
			#pragma shader_feature __ _MOTION_ON
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}

	Fallback Off
}
