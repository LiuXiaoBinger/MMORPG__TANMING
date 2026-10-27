/*
*      Author: Starking.
*      Version: 18.01.29
*/

Shader "RO/Scene/Terrain" {
	Properties {
        [KeywordEnum(FOUR, THREE, TWO, ONE)]_Layer("纹理层数", float) = 0
		_Splat0("纹理 1", 2D) = "white" {}
		_Splat1("纹理 2", 2D) = "white" {}
		_Splat2("纹理 3", 2D) = "white" {}
		_Splat3("纹理 4", 2D) = "white" {}

		[Space]
		[MaterialToggle]_NORMALMAP("NORMALMAP", float) = 0
		_BumpMap ("纹理 1 Normal Map", 2D) = "blue" {}
		_SpecTex ("纹理 1 Spec Tex", 2D) = "black" {}
		_BumpIntense("法线强度",Range(0,10)) = 1
		_SpecIntense("高光强度",Range(1,20)) = 1
		_Diffuse("Diffuse", Color) = (0.5,0.5,0.5,1)

		_UVTiling01("纹理 s1 (x,y) 纹理 2 (x,y)",vector) = (1,1,1,1)
		_UVTiling23("纹理 3 (x,y) 纹理 4 (x,y)",vector) = (1,1,1,1)
		_Control("通道强度 (RGBA)", 2D) = "white" {}
		[HideInInspector]_MainTex("Never Used", 2D) = "black" {}
		_LMFactor("烘培影响", Range(0,2)) = 1
		_FogFactor("雾影响", Range(0,1)) = 1
		_LightFactor("场景光影响",range(0, 1)) = 0
		_RainFactor("雨雪影响", Range(0,10)) = 5
	}
		
	//======================================================================================
	//
	// 编辑模式(使用系统光照)
	//
	//======================================================================================
	Subshader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Back
		LOD 600

		Stencil
		{
			Ref 40//地表写40
			Comp Always
			Pass Replace
		}

		Pass{
			Tags{ "LightMode" = "ForwardBase" }
			Blend One Zero
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDBASE
			#define SC_TERRAIN
			#define SC_BAKE_LIGHT
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#define DEPTH_SHADOW_ON
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile_fwdbase nodirlightmap nodynlightmap
			//增加点光源照明2019.06.14
			#pragma shader_feature ENABLE_POINTLIGHT//
			#pragma shader_feature __ _NORMALMAP_ON
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDADD
			#define SC_TERRAIN
            #define SC_BAKE_LIGHT		
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#define DEPTH_SHADOW_ON
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile_fwdadd nodirlightmap nodynlightmap 	
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_S
			#pragma target 4.6
			#pragma exclude_renderers gles
			#define SC_TERRAIN
            #define SC_SHADOWCASTER
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile_shadowcaster
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
	// 极致模式(支持方向光和天气质感)
	//
	//======================================================================================
	
	Subshader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Back
		LOD 400

		Stencil
		{
			Ref 40//地表写40
			Comp Always
			Pass Replace
		}

		Pass{
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vertTerrain
			#pragma fragment fragTerrain
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDBASE
			#define SC_TERRAIN
			#define SC_DIR_LIGHT
			#define SC_REFLECTION
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
			#define DEPTH_SHADOW_ON
			#pragma shader_feature __ _NORMALMAP_ON
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			//增加点光源照明2019.06.14
			#pragma shader_feature ENABLE_POINTLIGHT
			#include "../Include/SceneCommon.cginc"

            float3 _ROPlayerPosition;
            float4 _ROPlayerLightColor;
            float _ROPlayerLightRadius;
			
			v2f vertTerrain(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VERTEXOFFSET

				VertInit(o, v);

				#if defined(_MSE_ON)
					half3 mse = half3(0, 0, _Emission);
				#else
					half3 mse = MAINTEX_MGE;
				#endif

				half reflectivity = mse.x;
				half smoothness = mse.y;
				smoothness = clamp(smoothness, 0.002h, 0.998h);

				WEATHER_SMOOTHNESS

				float3 lightDir = _ROCLightDir0;
				lightDir *= rsqrt(max(0.001, dot(lightDir, lightDir)));
				o.litTerm = LitAtten(o.worldNormal, normalize(o.viewDir), lightDir, smoothness, reflectivity, _LightFactor);

				o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				//=============================================================================
				// 扩充为4光源2019.06.14
				//=============================================================================
				#if ENABLE_POINTLIGHT
					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
					half3 worldNormal = UnityObjectToWorldNormal(v.normal);
					o.worldNormal = worldNormal;
					o.pointLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, worldPos.xyz, worldNormal);
				#endif
				return o;
			}

			half4 fragTerrain(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				float3 worldPos = i.worldPos;
				i.worldNormal = normalize(i.worldNormal);
				i.viewDir = NormalizePerPixelNormal(i.viewDir);

				//====================================================================================================
				//	环境影响采样
				//====================================================================================================
				GET_SCEMEMAP(i)

				//====================================================================================================
				//	基础色采样
				//====================================================================================================
				MAINTEX_COLOR(color, i)
				//基礎采樣更換綫性空間
				color.rgb = GammaToLinearSpace(color.rgb);       
	
				//================================================================================================
				//	环境离线光(烘焙光照+球偕光照)
				//================================================================================================
				half3 bakeGI = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;
				//更換線性空間//針對綫性空間色彩補償
				float fixedSaturation = min(0.5 * max(bakeGI.r, max(bakeGI.g, bakeGI.b)), 1.0);
				bakeGI = lerp(bakeGI.rgb, GammaToLinearSpace(bakeGI.rgb), fixedSaturation);
			
				#if ENABLE_POINTLIGHT
					bakeGI += i.pointLightColor;
				#endif

				//============================================================================================
				//	方向光源
				//============================================================================================
				half3 lightColor = _ROCLightColor0.rgb;
				float3 lightDir = _ROCLightDir0;
				half atten = _LightFactor;

				//SaveNormalize
				lightDir *= rsqrt(max(0.001, dot(lightDir, lightDir)));

				half3 mse = MAINTEX_MGE;
				half reflectivity = mse.x;
				half smoothness = mse.y;
				half3 diffColor = color.rgb * max(0.04, 1 - reflectivity);
				half3 specColor = color.rgb * reflectivity;	

				half3 colLit = const_v30;

				//================================================================================================
				//	天气处理影响变量
				//================================================================================================
				WEATHER_AFFECT

				//================================================================================================
				//	光源计算量 x= 主光方向强度 y=高光强度 z=菲涅尔强度 w=掠入光强度
				//================================================================================================
				smoothness = clamp(smoothness, 0.002h, 0.998h);
				half4 litTerm = i.litTerm;

				//================================================================================================
				//	叠加方向光
				//================================================================================================
				colLit += DirLight(diffColor, specColor, lightColor, litTerm);

				//============================================================================================
				//	叠加反光
				//============================================================================================
				colLit += Reflaction(specColor, smoothness, i.worldNormal, i.viewDir, litTerm);

				//============================================================================================
				//	叠加烘焙光或球偕光
				//============================================================================================
				colLit += diffColor * BakeColor(bakeGI);
				color.rgb = min(const_v31, colLit);

            	float3 sphereDisp = i.worldPos.xyz - (_ROPlayerPosition + float3(0, 1, 0));
                float pointLightLen = length(sphereDisp);
                float3 pointLightDir = sphereDisp / pointLightLen;
                color.rgb += _ROPlayerLightColor * saturate(dot(i.worldNormal, -pointLightDir)) * (1 - saturate(pointLightLen / _ROPlayerLightRadius));

				//========================================================================================================
				//	叠加环境影响
				//========================================================================================================
				FinalColor(color, i);

				//模拟线性空间
				color.rgb = LinearToGammaSpace(color.rgb);
				return color;
			}
			ENDCG
		}

		Pass{
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_S
			#pragma target 3.5
			#define SC_TERRAIN
            #define SC_SHADOWCASTER
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile_shadowcaster
			#include "../Include/SceneCommon.cginc"
			ENDCG
		}
	}

	//======================================================================================
	//
	// 高级模式(支持光雾和角色投影)
	//
	//======================================================================================

	Subshader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Back
		LOD 300
			
		Stencil
		{
			Ref 40//地表写40
			Comp Always
			Pass Replace
		}

		Pass{
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vertTerrain
			#pragma fragment fragTerrain
            #pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#pragma exclude_renderers gles
			#define SC_FORWARDBASE
			#define SC_TERRAIN
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			#include "../Include/SceneCommon.cginc"

			v2f vertTerrain(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VERTEXOFFSET

				VertInit(o, v);

				o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				return o;
			}

			half4 fragTerrain(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				//====================================================================================================
				//	环境影响采样
				//====================================================================================================
				GET_SCEMEMAP(i)

				//====================================================================================================
				//	基础色采样
				//====================================================================================================
				MAINTEX_COLOR(color, i)

				//基礎采樣更換綫性空間
				color.rgb = GammaToLinearSpace(color.rgb);

				//================================================================================================
				//	环境离线光(烘焙光照+球偕光照)
				//================================================================================================
				half3 bakeGI = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;

				//更換線性空間//針對綫性空間色彩補償
				float fixedSaturation = min(0.5 * max(bakeGI.r, max(bakeGI.g, bakeGI.b)), 1.0);
				bakeGI = lerp(bakeGI.rgb, GammaToLinearSpace(bakeGI.rgb), fixedSaturation);

				//================================================================================================
				//	天气处理影响变量
				//================================================================================================
				WEATHER_AFFECT

				//================================================================================================
				//	叠加烘焙光
				//================================================================================================
				color.rgb *= BakeColor(bakeGI);

				//========================================================================================================
				//	叠加环境影响
				//========================================================================================================
				FinalColor(color, i);

				//模拟线性空间
				color.rgb = LinearToGammaSpace(color.rgb);
				return color;
			}
			ENDCG
		}
	}

	//======================================================================================
	//
	// 中级模式(支持大气雾)
	//
	//======================================================================================

	Subshader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Back
		LOD 200

		Stencil
		{
			Ref 40//地表写40
			Comp Always
			Pass Replace
		}

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vertTerrain
			#pragma fragment fragTerrain
            #pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.5
			#define SC_FORWARDBASE
			#define SC_TERRAIN
			#define SCENE_MAP_ON
			#define SCENE_MAP_MIE_SCATTER
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#pragma multi_compile __ SCENE_MAP_RAIN SCENE_MAP_SNOW
			#include "../Include/SceneCommon.cginc"

			v2f vertTerrain(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VERTEXOFFSET

				VertInit(o, v);

				o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				return o;
			}

			half4 fragTerrain(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				//====================================================================================================
				//	环境影响采样
				//====================================================================================================
				GET_SCEMEMAP(i)

				//====================================================================================================
				//	基础色采样
				//====================================================================================================
				MAINTEX_COLOR(color, i)

				//基礎采樣更換綫性空間
				color.rgb = GammaToLinearSpace(color.rgb);

				//================================================================================================
				//	环境离线光(烘焙光照+球偕光照)
				//================================================================================================
				half3 bakeGI = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;

				//更換線性空間//針對綫性空間色彩補償
				float fixedSaturation = min(0.5 * max(bakeGI.r, max(bakeGI.g, bakeGI.b)), 1.0);
				bakeGI = lerp(bakeGI.rgb, GammaToLinearSpace(bakeGI.rgb), fixedSaturation);

				//================================================================================================
				//	天气处理影响变量
				//================================================================================================
				WEATHER_AFFECT

				//================================================================================================
				//	叠加烘焙光
				//================================================================================================
				color.rgb *= BakeColor(bakeGI);

				//========================================================================================================
				//	叠加环境影响
				//========================================================================================================
				FinalColor(color, i);

				//模拟线性空间
				color.rgb = LinearToGammaSpace(color.rgb);
				return color;
			}
			ENDCG
		}
	}

	//======================================================================================
	//
	// 低级模式(只有纹理色和烘焙光照)
	//
	//======================================================================================
	
	Subshader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Back
		LOD 100

		Stencil
		{
			Ref 40//地表写40
			Comp Always
			Pass Replace
		}

		Pass {
			Tags{ "LightMode" = "ForwardBase" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vertTerrain
			#pragma fragment fragTerrain
			#pragma fragmentoption ARB_precision_hint_fastest
			#define SC_FORWARDBASE
			#define SC_TERRAIN
            #pragma shader_feature __ _LAYER_THREE _LAYER_TWO _LAYER_ONE
			#include "../Include/SceneCommon.cginc"

			v2f vertTerrain(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VERTEXOFFSET

				VertInit(o, v);

				o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				return o;
			}

			half4 fragTerrain(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				//====================================================================================================
				//	环境影响采样
				//====================================================================================================
				GET_SCEMEMAP(i)

				//====================================================================================================
				//	基础色采样
				//====================================================================================================
				MAINTEX_COLOR(color, i)

				//基礎采樣更換綫性空間
				color.rgb = GammaToLinearSpace(color.rgb);

				//================================================================================================
				//	环境离线光(烘焙光照+球偕光照)
				//================================================================================================
				half3 bakeGI = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;

				//更換線性空間//針對綫性空間色彩補償
				float fixedSaturation = min(0.5 * max(bakeGI.r, max(bakeGI.g, bakeGI.b)), 1.0);
				bakeGI = lerp(bakeGI.rgb, GammaToLinearSpace(bakeGI.rgb), fixedSaturation);

				//================================================================================================
				//	天气处理影响变量
				//================================================================================================
				WEATHER_AFFECT

				//================================================================================================
				//	叠加烘焙光
				//================================================================================================
				color.rgb *= BakeColor(bakeGI);

				//========================================================================================================
				//	叠加环境影响
				//========================================================================================================
				FinalColor(color, i);

				//模拟线性空间
				color.rgb = LinearToGammaSpace(color.rgb);
				return color;
			}
			ENDCG
		}
	}

	Fallback Off		
}
