Shader "RO/Scene/AllInOneUnderWater" {
	Properties {
		_MainTex("基础色 (RGB)", 2D) = "white" {}
		[MaterialToggle]_MSE("使用质感纹理", float) = 0
		_MSETex("质感 (金属=X，光滑=Y，自发光=Z)", 2D) = "black" {}
		_Metallic("金属", range(0, 1)) = 0
		_Smoothness("金属", range(0, 1)) = 0
		_Emission("发光", range(0, 1)) = 0
		[Enum(Off,0,Cutoff,1,blend,2)]_AlphaMode("透明处理", float) = 0
		_Opacity("透明",range(0,1)) = 1
		_Cutoff("透明剔除阈值",Range(0,1)) = 0.5
		_LMFactor("烘培影响", Range(0,1)) = 1
		_FogFactor("雾影响", Range(0,1)) = 1
		_SnowFactor("覆雪强度", Range(0,2)) = 1
		_LightFactor("场景光强",range(0, 1)) = 0
		_WaveFactor("摆动幅度",Range(0,1)) = 1
		[KeywordEnum(OFF, MATRIXARRAY, BONEMAP, VERTEXMAP, WIND)] _GpuAnim("蒙皮动画", float) = 0
		_AnimTex("蒙皮纹理",2D) = "Black" {}
		_Offset("偏移参数(x=像素偏移，y=帧数，z=骨骼数，w=FPS)",vector) = (0, 1, 1, 30)
		_FrameShift("错帧",float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("剔除", float) = 2
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("基色混合", float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("背景混合", float) = 0
		[Toggle]_ZWrite("写入深度", float) = 1
		[Enum(Mask, 0, Pass, 2)]_Stencil("地面特效遮挡", float) = 0
		//增加点光源照明2019.06.14
		//[Toggle(ENABLE_POINTLIGHT)] _PointLight("EnablePointLight", Float) = 0

		[Space]
		[MaterialToggle]_NORMALMAP("NORMALMAP", float) = 0
		_BumpMap ("纹理 1 Normal Map", 2D) = "blue" {}
		_SpecTex ("纹理 1 Spec Tex", 2D) = "black" {}
		_BumpIntense("法线强度",Range(0,10)) = 1
		_SpecIntense("高光强度",Range(1,20)) = 1
		_Diffuse("Diffuse", Color) = (0.5,0.5,0.5,1)
	}

	//======================================================================================
	//
	// 编辑模式(使用系统光照)
	//
	//======================================================================================
	
	Subshader {
		Tags { "RenderType" = "Opaque" }
		Cull [_Cull]
		LOD 600
		
		Stencil
		{
			Ref 30//非地表写30
			Comp Always
			Pass [_Stencil]
		}

		Pass{
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDBASE
			#define SC_OBJECT
			#define SC_BAKE_LIGHT
			//#define SCENE_MAP_ON
			//#define SCENE_MAP_MIE_SCATTER	
			//#define DEPTH_SHADOW_ON
			//#define DEPTH_SHADOW_HARD
			#pragma shader_feature __ _MSE_ON	
			#pragma shader_feature __ _ALPHAMODE_CUTOUT	
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			#pragma multi_compile_fwdbase nodirlightmap
			#pragma shader_feature __ _NORMALMAP_ON
			//#pragma multi_compile_instancing
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			ZWrite Off ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDADD
			#define SC_OBJECT
			#define SC_BAKE_LIGHT
			//#define SCENE_MAP_ON
			//#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _MSE_ON
			#pragma shader_feature __ _ALPHAMODE_CUTOUT	
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			#pragma multi_compile_fwdadd nodirlightmap 
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_S
			#pragma target 3.5
			#pragma exclude_renderers gles
            #define SC_SHADOWCASTER
			#pragma shader_feature __ _ALPHAMODE_CUTOUT
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			#pragma multi_compile_shadowcaster
			//#pragma multi_compile_instancing
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}
		
        Pass
        {
            Tags { "LightMode"="Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
			#pragma exclude_renderers gles
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "../Include/SceneMeta.cginc"
            ENDCG
        }
	}

	//======================================================================================
	//
	// 极致模式(支持方向光，角色投影和天气质感)
	//
	//======================================================================================

	Subshader {
		Tags { "RenderType" = "Opaque" }
		Cull [_Cull]
		LOD 400

		Stencil
		{
			Ref 30//非地表写30
			Comp Always
			Pass [_Stencil]
		}

		Pass{
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
            #define SC_PLAYER_LIGHT
			#define SC_FORWARDBASE
			#define SC_OBJECT
			#define SC_DIR_LIGHT
			#define SC_REFLECTION
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#define DEPTH_SHADOW_ON
			#define DEPTH_SHADOW_HARD
			#pragma shader_feature __ _MSE_ON
			#pragma shader_feature __ _ALPHAMODE_CUTOUT	
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			//#pragma multi_compile_instancing
			#pragma multi_compile __ LIGHTMAP_ON
			#pragma shader_feature __ _NORMALMAP_ON
			//增加点光源照明2019.06.14
			#pragma shader_feature ENABLE_POINTLIGHT//
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_S
			#pragma target 3.5
            #define SC_SHADOWCASTER
			#pragma shader_feature __ _ALPHAMODE_CUTOUT
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			//#pragma multi_compile_shadowcaster
			//#pragma multi_compile_instancing
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}
	}

	//======================================================================================
	//
	// 高级模式(支持光雾)
	//
	//======================================================================================
	
	Subshader {
		Tags { "RenderType" = "Opaque" }
		Cull [_Cull]
		LOD 300

		Stencil
		{
			Ref 30//非地表写30
			Comp Always
			Pass [_Stencil]
		}

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDBASE
			#define SC_OBJECT
			//#define SC_DIR_LIGHT
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _ALPHAMODE_CUTOUT	
			#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			//#pragma multi_compile_instancing
			#pragma multi_compile __ LIGHTMAP_ON
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

	// 	// Pass{
	// 	// 	Name "ShadowCaster"
	// 	// 	Tags{ "LightMode" = "ShadowCaster" }
	// 	// 	ZWrite On ZTest LEqual
			
	// 	// 	CGPROGRAM
	// 	// 	#pragma vertex vert
	// 	// 	#pragma fragment frag_S
	// 	// 	#pragma target 3.5
    //     //     #define SC_SHADOWCASTER
	// 	// 	#pragma shader_feature __ _ALPHAMODE_CUTOUT
	// 	// 	#pragma shader_feature __ _GPUANIM_WIND _GPUANIM_MATRIXARRAY _GPUANIM_BONEMAP _GPUANIM_VERTEXMAP
	// 	// 	//#pragma multi_compile_shadowcaster
	// 	// 	//#pragma multi_compile_instancing
	// 	// 	#include "../Include/SceneCommon.cginc"
	// 	// 	ENDCG
	// 	// }
	}

	//======================================================================================
	//
	// 中级模式(支持大气雾)
	//
	//======================================================================================

	Subshader {
		Tags { "RenderType" = "Opaque" }
		Cull [_Cull]
		LOD 200

		Stencil
		{
			Ref 30//非地表写30
			Comp Always
			Pass [_Stencil]
		}

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#define SC_FORWARDBASE
			#define SC_OBJECT
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#pragma shader_feature __ _ALPHAMODE_CUTOUT
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			//#pragma multi_compile_instancing
			#pragma multi_compile __ LIGHTMAP_ON
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}
	}

	//======================================================================================
	//
	// 低级模式(只有纹理色和烘焙光照)
	//
	//======================================================================================
	
	Subshader {
		Tags { "RenderType" = "Opaque" }
		Cull [_Cull]
		LOD 100

		Stencil
		{
			Ref 30//非地表写30
			Comp Always
			Pass [_Stencil]
		}
		
		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite] ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#define SC_FORWARDBASE
			#define SC_OBJECT
			#pragma shader_feature __ _ALPHAMODE_CUTOUT
			#pragma multi_compile __ LIGHTMAP_ON
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}
	}
	
	Fallback Off
	CustomEditor "ROSceneShaderGUI"
}
