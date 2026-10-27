// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*
*      Author: Starking.
*      Version: 18.01.29
*/

Shader "RO/Creature/Player"
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
		_Visibility("可见度",range(0,1)) = 1
		[Space]
		[MaterialToggle]_Alpha_Split("分离Alpha", float) = 0
		[NoScaleOffset]_Tex0("面部",2D) = "white" {}
		[NoScaleOffset]_Tex1("头发",2D) = "white" {}
		[NoScaleOffset]_Tex2("武器1",2D) = "white" {}
		[NoScaleOffset]_Tex3("武器2",2D) = "white" {}
		[NoScaleOffset]_Tex4("身体",2D) = "white" {}
		[NoScaleOffset]_Tex5("身体质感",2D) = "black" {}
		[NoScaleOffset]_Tex6("头发高光颜色",2D) = "black" {}
		_NormalTex ("NormalTex", 2D) = "blue" {}
		//_HairSpecTex("HairSpecTex",2D) = "black" {}
		_HairSpecColor("头发高光颜色", color) = (1,1,1,1)
		_Contrast("对比度", range(0, 2)) = 0.95
		_Brightness("光强", range(0, 2)) = 0.9
		_RimParams("边缘光(xyz=偏向,w=强度)",vector) = (2,1,1,20)
		_EmotionID("眼部表情ID",range(0, 63)) = 0
		_EmotionID2("嘴部表情ID",range(0, 63)) = 0
		_EmotionSize("每个表情宽高占比", range(0.125, 1)) = 0.25
		[HideInInspector] _Falloff("方向衰减(x,y 为区间(-1~1),x<y)", vector) = (-0.1, 0.35, 0, 0)
		_FalloffFace("脸部方向衰减(x,y 为区间(-1~1),x<y)", vector) = (-0.4, 0.35, 0, 0)
		//增加点光源照明2019.05.28
		//[Toggle(ENABLE_POINTLIGHT)] _PointLight("EnablePointLight", Float) = 0
		[HideInInspector]_HairCL("HairCL",color) = (0,0,0,0)
		[HideInInspector]_HairCM("HairCM",color) = (0,0,0,0)
		[HideInInspector]_HairCD("HairCD",color) = (0,0,0,0)
		[HideInInspector]_HairCH("HairCH",vector) = (0,0,0,0)
		[HideInInspector]_EyeCL("EyeCL",color) = (0,0,0,0)
		[HideInInspector]_EyeCM("EyeCM",color) = (0,0,0,0)
		[HideInInspector]_EyeCD("EyeCD",color) = (0,0,0,0)
		[HideInInspector]_EyeCH("EyeCH",vector) = (0,0,0,0)
	}
	
	//======================================================================================
	//
	// 编辑模式(支持身体 normalMap)
	//
	//======================================================================================
	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
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
			#define HAIR_SPEC_MAP
			#define BODY_NORMAL_MAP
			#define CREATURE_SKILLCOLOR
			#define CREATURE_SIXSKIN
			#define CREATURE_WPM
			#define DEPTH_SHADOW_ON
			#define DEPTH_SHADOW_HARD
			#define _LIGHTM_PBR
			#define BLOOM_ON
			#define SCENE_MAP_ON
			#define VERTEXLIGHT_ON
			#define SCENE_MAP_NOCLOUD
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _ALPHA_SPLIT_ON
			//#pragma shader_feature __ _FADE_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			//增加点光源照明2019.05.28
			#pragma shader_feature ENABLE_POINTLIGHT
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
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
			#define HAIR_SPEC_MAP
			#define CREATURE_SKILLCOLOR
			#define CREATURE_SIXSKIN
			#define CREATURE_WPM
			#define _LIGHTM_PBR
			#define DEPTH_SHADOW_ON
			#define DEPTH_SHADOW_HARD
			#define BLOOM_ON
			#define SCENE_MAP_ON
			#define VERTEXLIGHT_ON
			#define SCENE_MAP_NOCLOUD
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _ALPHA_SPLIT_ON
			//#pragma shader_feature __ _FADE_ON
			#pragma multi_compile __ SCENE_MAP_RAIN
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			//增加点光源照明2019.05.28
			#pragma shader_feature ENABLE_POINTLIGHT
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}
	
	SubShader
	{
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
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
			#define CREATURE_SIXSKIN
			#define CREATURE_WPM
			#define _LIGHTM_PBR
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#define VERTEXLIGHT_ON
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _ALPHA_SPLIT_ON
			//#pragma shader_feature __ _FADE_ON
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
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "DepthShadowCaster" = "True" }
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
			#define CREATURE_SIXSKIN
			#define CREATURE_WPM
			#define _LIGHTM_SIMPLE
			#define SCENE_MAP_ON
			#define SCENE_MAP_NOCLOUD
			#pragma shader_feature __ _ALPHA_SPLIT_ON
			//#pragma shader_feature __ _FADE_ON
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
		Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" }
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
			#define CREATURE_SIXSKIN
			#pragma shader_feature __ _ALPHA_SPLIT_ON
			//#pragma shader_feature __ _FADE_ON
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "../Include/CreatureCommon.cginc"

			ENDCG
		}
	}

	Fallback Off
}
