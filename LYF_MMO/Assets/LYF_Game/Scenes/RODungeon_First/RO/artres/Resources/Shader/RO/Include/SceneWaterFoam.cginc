#ifndef SCENEWATERFOAM_INCLUDE
#define SCENEWATERFOAM_INCLUDE

//================================================================================================
//
// 定义与引用
//
//================================================================================================

#include "UnityCG.cginc"
#include "UnityStandardCore.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "SceneMap.cginc"

#ifdef SC_FORWARDBASE

#ifndef UNITY_PASS_FORWARDBASE
#define UNITY_PASS_FORWARDBASE
#endif

#endif

#ifdef SC_FORWARDADD

#ifndef UNITY_PASS_FORWARDADD
#define UNITY_PASS_FORWARDADD
#endif

#endif

#define API_LOW_LEVEL ((SHADER_TARGET < 30) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X))
#define USE_FORWARDRENDER (defined(SC_FORWARDBASE) || defined(SC_FORWARDADD))
#define USE_DIRLIGHT (USE_FORWARDRENDER && (defined(SC_DIR_LIGHT) || defined(SC_BAKE_LIGHT)))
#define USE_SCENE_MAP ((defined(SCENE_MAP_ON)))
#define USE_VIEWDIR (USE_DIRLIGHT || USE_WATER_FOAM || USE_WATER_WAVE || USE_WATER_REFL || USE_WATER_SPECULAR)
#define USE_WORLDPOS (USE_SCENE_MAP || USE_VIEWDIR)
#define USE_VERTEXANIM_NORMAL (defined(_GPUANIM_MATRIXARRAY) || defined(_GPUANIM_BONEMAP)) && !API_LOW_LEVEL
#define USE_VERTEXANIM_BASE (defined(_GPUANIM_VERTEXMAP) || defined(_GPUANIM_WIND)) && !API_LOW_LEVEL
#define USE_VERTEXANIM (USE_VERTEXANIM_NORMAL || USE_VERTEXANIM_BASE)
#define USE_NORMAL (USE_VERTEXANIM_NORMAL || USE_DIRLIGHT || defined(SC_SHADOWCASTER))
#define USE_WATER_REFL (defined(_WATERMODE_REFLECTIVE))
#define USE_WATER_SPECULAR (defined(_WATERMODE_SPECULAR))
#define USE_SHADOWMAP (defined(DEPTH_SHADOW_ON) && !API_LOW_LEVEL)
#define USE_SCRPOS ((defined(SC_OCCLUDER) && API_LOW_LEVEL)||defined(SC_WATERMIRROR))
#define USE_WATER_FOAM (defined(SC_WATER_FOAM))
#define USE_WATER_WAVE (defined(SC_WATER_WAVE))

static const half3 const_v30 = half3(0, 0, 0);
static const half3 const_v31 = half3(1, 1, 1);
static const half4 const_v4c = half4(0, 0, 0, 1);
static const half4 const_v40 = half4(0, 0, 0, 0);
static const half3 const_up = half3(0, 1, 0);


struct appdata_water
{
	float4 vertex : POSITION;
	float4 tangent : TANGENT;
	float3 normal : NORMAL;
	float4 texcoord : TEXCOORD0;
	float2 texcoordLM : TEXCOORD1;
//	float4 texcoord1 : TEXCOORD1;
//	float4 texcoord2 : TEXCOORD2;
//	float4 texcoord3 : TEXCOORD3;
	fixed4 color : COLOR;
//	UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct v2f
{
	float2 uv_FoamTex : TEXCOORD0;
	float2 uv_NormalTex : TEXCOORD1;
	float4 TW0:TEXCOORD2;
	float4 TW1:TEXCOORD3;
	float4 TW2:TEXCOORD4;
	float4 vertex : SV_POSITION;
	fixed4 color : COLOR;
	half2 ambientOrLightmapUV : TEXCOORD5;
#if USE_SCENE_MAP
	SCENE_PARAM_COORDS(6, 7)
#endif
#if USE_VIEWDIR
		float3 viewDir : TEXCOORD8;
#endif
	//UNITY_VERTEX_INPUT_INSTANCE_ID
};

sampler2D _NormalTex;
float4 _NormalTex_ST;
sampler2D _Gradient;
#if USE_WATER_REFL
samplerCUBE _ReflectionTex;
half _Fresnel;
#endif

fixed4 _WaveParams;
sampler2D _FoamTex;
float4 _FoamTex_ST;
sampler2D _WaveMask;
sampler2D _WaveTex;
half4 _FoamParams;
fixed4 _FoamColor;
half4 _Speed;

half _NormalScale;
half _WaterColorScale;
half _Specular;
fixed _Gloss;
//half4 _SpecColor;
//half4 _LightDir;

half _LMFactor;
half _FogFactor;

#if USE_SCENE_MAP
#define GET_SCEMEMAP(i, worldPos)	GET_WATER_SCEMEMAP(i, worldPos)
#else
#define GET_SCEMEMAP(i, worldPos)
#endif

#if (defined(SCENE_MAP_RAIN) || defined(SCENE_MAP_SNOW))
#define WEATHER_AFFECT GetSnowOrRainFactor(i.sceneMapCoord.w);
#else
#define WEATHER_AFFECT
#endif

v2f vertFoam(appdata_water v)
{
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f, o);

	o.vertex = UnityObjectToClipPos(v.vertex);
#if USE_WATER_FOAM
	o.uv_FoamTex = TRANSFORM_TEX(v.texcoord, _FoamTex);
#endif
	o.uv_NormalTex = TRANSFORM_TEX(v.texcoord, _NormalTex);

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
	fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
	fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
	fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;

	o.TW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
	o.TW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
	o.TW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

	o.color = v.color;

	float3 viewDir = UnityWorldSpaceViewDir(worldPos);
#if USE_VIEWDIR
	o.viewDir = NormalizePerVertexNormal(viewDir);
#endif
#if USE_SCENE_MAP
	TRANSFER_SCATTER(o, -viewDir, worldPos.y)
#endif
#if USE_FORWARDRENDER
	o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
	return o;
}

half4 WaterFoamColor(half3 lmColor, v2f i)
{
	//根据顶点颜色r通道采样海水渐变
	half4 col = _WaterColorScale * (tex2D(_Gradient, float2(i.color.r, 0.5)));
	//GI影响
	col = lerp(col, half4(lmColor, 1), _LMFactor);

	//采样法线贴图
	fixed2 timeOffset = fixed2(_Time.x*_Speed.x, 0);
	fixed4 normalCol = (
		tex2D(_NormalTex, i.uv_NormalTex.xy + timeOffset)
		+ tex2D(_NormalTex, i.uv_NormalTex.yx + timeOffset)
		) / 2;
	half3 tangentNormal = UnpackNormal(normalCol);
	tangentNormal = lerp(half3(0, 0, 1), tangentNormal, _NormalScale);
	half3 worldNormal = normalize(half3(dot(i.TW0.xyz, tangentNormal), dot(i.TW1.xyz, tangentNormal), dot(i.TW2.xyz, tangentNormal)));

	//采样反射天空盒
#if USE_VIEWDIR
	half3 viewDir = i.viewDir;
	half3 reflectionViewDir = reflect(-viewDir, worldNormal);
#endif

#if USE_WATER_REFL && USE_SCENE_MAP
	half vdn = saturate(pow(dot(viewDir, worldNormal), _Fresnel));
	col.rgb = lerp(texCUBE(_ReflectionTex, reflectionViewDir), col.rgb, vdn);
#endif

#if USE_WATER_FOAM
	//泡沫使用法线贴图的rg进行扰动
	half3 foam = tex2D(_FoamTex, i.uv_FoamTex + worldNormal.xy*_WaveParams.w).rgb;
	fixed waveAlpha = tex2D(_WaveMask, float2(i.color.r, 0)).r;
	#if USE_WATER_WAVE
		//计算海浪
		float dt = (_Time.x*_Speed.y + _WaveParams.z*foam.b);
		fixed wave1 = tex2D(_WaveTex, float2(i.color.r + _WaveParams.y + _WaveParams.x*sin(dt), 0)).r;
		fixed wave2 = tex2D(_WaveTex, float2(i.color.r + _WaveParams.y + _WaveParams.x*cos(dt), 0)).r;
		col += (_FoamColor - col)* (wave1 + wave2) * waveAlpha * foam.r * i.color.b;

		//计算岸边泡沫
		fixed sfadein = 1 - saturate((_FoamParams.x - i.color.r) / _FoamParams.x);
		fixed sfadeout = 1 - saturate((i.color.r - _FoamParams.y) / _FoamParams.z);
		col += (_FoamColor - col)* sfadein*sfadeout *_FoamParams.w*foam.g*i.color.g;
	#else
		fixed wave = tex2D(_WaveTex, float2(i.color.r + _WaveParams.y, 0)).r;
		col += (_FoamColor - col) * wave * waveAlpha * foam.r * i.color.b;
	#endif
#else
	col *= 0.75f;
#endif

#if	USE_WATER_SPECULAR && USE_SCENE_MAP
	//计算高光
	fixed3 halfDir = normalize(SafeDir(-_ROCLightDir1) + viewDir);
	half ndh = max(0, dot(worldNormal, halfDir));
	half3 specularCol = _Specular * _SpecColor.rgb * pow(ndh, _Gloss);
	col.rgb += specularCol.rgb;

#endif
	//应用顶点透明度
	col.a *= i.color.a;
	return col;
}

void FinalColor(inout half4 color, v2f i)
{
	half3 col = color.rgb;
	half alpha = color.a;
	//处理环境影响
#if USE_SCENE_MAP
	//处理环境整体色偏
	ApplySceneColor(col);

	//雨雪天模式
	#if defined(SCENE_MAP_SNOW) || defined(SCENE_MAP_RAIN)
		APPLY_SCENE_FOG(col, i, _FogFactor)
		//天气云层压暗
		col *= i.sceneMapCoord.w;
	#elif defined(SCENE_MAP_NOCLOUD)
		APPLY_SCENE_FOG(col, i, _FogFactor)
	#else
		//默认情况或流动模式
		//云层阴影
		ApplyCloudMask(col, i.sceneMapCoord.z);
		APPLY_SCENE_FOG(col, i, _FogFactor)
	#endif

	color.rgb = col;
#endif
	color.a = alpha;
}

fixed4 fragFoam(v2f i) : SV_Target
{
	half4 color = const_v40;
	float3 worldPos = half3(i.TW0.w, i.TW1.w, i.TW2.w);
	#if USE_FORWARDRENDER
		#if USE_VIEWDIR
		i.viewDir = NormalizePerPixelNormal(i.viewDir);
		#endif
	#endif
	//环境影响采样
	GET_SCEMEMAP(i, worldPos)
	#if USE_FORWARDRENDER
		//	环境离线光(烘焙光照+球偕光照)
		half3 bakeGI = const_v30;
		#if defined(LIGHTMAP_ON)
			return half4(0, 1, 0, 0.5);
			bakeGI += DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;
		#elif (defined(LIGHTPROBE_SH) && USE_DIRLIGHT)
			return half4(1, 0, 0, 0.5);
			bakeGI += ShadeSHPerPixel(i.worldNormal, i.ambientOrLightmapUV.rgb, worldPos);
		#endif
		#if defined(DYNAMICLIGHTMAP_ON)
			half3 rtimeGI = DecodeRealtimeLightmap(UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, i.ambientOrLightmapUV.zw)).rgb;
			bakeGI += rtimeGI;
			return half4(0, 0, 1, 0.5);
		#endif
		//	天气处理影响变量
		WEATHER_AFFECT
			//	水光照处理
		//	bakeGI = lerp(const_v31, bakeGI, _LMFactor);
		color = WaterFoamColor(bakeGI, i);
	//	return half4(bakeGI, 1);
		//color.rgb = bakeGI;
		//color.a = 1;
	#endif
	FinalColor(color, i);//叠加环境影响
	return color;
}

#endif