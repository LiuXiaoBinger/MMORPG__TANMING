#ifndef SCENECOMMON_INCLUDE
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members noBlurPos)
#pragma exclude_renderers d3d11
#define SCENECOMMON_INCLUDE

//================================================================================================
//
// 定义与引用
//
//================================================================================================

#include "UnityCG.cginc"
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

//增加地表高光法线
#define USE_NORMAL_MAP defined(_NORMALMAP_ON)

#define API_LOW_LEVEL ((SHADER_TARGET < 30) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X))
#define USE_FORWARDRENDER (defined(SC_FORWARDBASE) || defined(SC_FORWARDADD))
#define USE_DIRLIGHT (USE_FORWARDRENDER && (defined(SC_DIR_LIGHT) || defined(SC_BAKE_LIGHT)))
#define USE_SCENE_MAP ((defined(SCENE_MAP_ON)))
#define USE_VIEWDIR (USE_DIRLIGHT)
//增加使用点光源需要的法线2019.06.14//2019.07.02增加草自适应场景阴影功能
#define USE_WORLDPOS (USE_SCENE_MAP || USE_VIEWDIR || defined(ENABLE_POINTLIGHT) || defined(SC_GRASS))
#define USE_VERTEXANIM_NORMAL (defined(_GPUANIM_MATRIXARRAY) || defined(_GPUANIM_BONEMAP)) && !API_LOW_LEVEL
#define USE_VERTEXANIM_BASE (defined(_GPUANIM_VERTEXMAP) || defined(_GPUANIM_WIND)) && !API_LOW_LEVEL
#define USE_VERTEXANIM (USE_VERTEXANIM_NORMAL || USE_VERTEXANIM_BASE)
//增加使用点光源需要的法线2019.06.14
#define USE_NORMAL (USE_VERTEXANIM_NORMAL || USE_DIRLIGHT || defined(SC_SHADOWCASTER)|| defined(ENABLE_POINTLIGHT))
#define USE_WATER_REFL (defined(_WATERMODE_REFLECTIVE) || defined(_WATERMODE_SPECULAR))
#define USE_SHADOWMAP (defined(DEPTH_SHADOW_ON) && !API_LOW_LEVEL)
#define USE_SCRPOS ((defined(SC_OCCLUDER) && API_LOW_LEVEL)) || defined(_OCCLUDER_NEW_ON)

#if USE_SHADOWMAP
#include "DepthShadow.cginc"
#endif

#if USE_DIRLIGHT

    CBUFFER_START(ROCommonLight)

        float3 _ROCLightDir0;
        half4 _ROCLightColor0;

    CBUFFER_END

    #include "UnityGlobalIllumination.cginc"
    half _Metallic;
    half _Smoothness;
    half _LightFactor;

#endif

static const half3 const_v30 = half3(0, 0, 0);
static const half3 const_v31 = half3(1, 1, 1);
static const half4 const_v4c = half4(0, 0, 0, 1);
static const half4 const_v40 = half4(0, 0, 0, 0);
static const half3 const_up = half3(0, 1, 0);

#ifdef SC_PLAYER_LIGHT
float3 _ROPlayerPosition;
float4 _ROPlayerLightColor;
float _ROPlayerLightRadius;
#endif

CBUFFER_START(ROGrass)
    half4 _ROGrassEnvorimentColor;
    half4 _ROGrassEnvorimentDarkColor;
    float3 _ROGrassInteractPos;
    half4 _ROTreeEnvorimentColor;
CBUFFER_END

UNITY_DECLARE_TEX2D(_BumpMap);
UNITY_DECLARE_TEX2D(_SpecTex);
float _BumpIntense;
float _SpecIntense;
float4 _Diffuse;

void LightResult(float3 tangentDir,float3 bitangentDir,float3 worldNormal,float3 viewDir,float3 lightDir,float3 lightColor,float2 texCoord, out half3 diffuse, out half3 spec)
{
    half3 specTex = UNITY_SAMPLE_TEX2D(_SpecTex, texCoord).rgb;
    half3 tangentNormal = UnpackNormal(UNITY_SAMPLE_TEX2D(_BumpMap, texCoord)).rgb;
    tangentNormal = normalize(tangentNormal * float3(1, _BumpIntense, 1));
    float3x3 tangentMatrix = float3x3(tangentDir, bitangentDir, worldNormal);
    half3 normal = normalize(mul(tangentNormal, tangentMatrix));
    diffuse = saturate(dot(normal, lightDir)) * _Diffuse.rgb + half3(0.5, 0.5, 0.5);
    spec = pow(saturate(dot(normalize(viewDir), reflect(-lightDir, normal))), _SpecIntense) * specTex.rgb;
}

#ifdef SC_TERRAIN
    UNITY_DECLARE_TEX2D(_Control); half4 _Control_ST;
    UNITY_DECLARE_TEX2D(_Splat0);
    
    half4 _UVTiling01;
    half4 _UVTiling23;

    #if USE_FORWARDRENDER

        #define MAINTEX_MGE 0
        
        #if (defined(SCENE_MAP_RAIN) || defined(SCENE_MAP_SNOW))
            
            #if USE_DIRLIGHT
            #define WEATHER_AFFECT GetSnowOrRainFactor(worldPos, i.sceneMapCoord.z, 1, 1, diffColor, specColor, i.worldNormal, smoothness, i.sceneMapCoord.w);
            #define WEATHER_SMOOTHNESS GetRainSmoothness(o.sceneMapCoord.z, 1, smoothness);
            #else
            #define WEATHER_AFFECT GetSnowOrRainFactor(i.sceneMapCoord.w);
            #define WEATHER_SMOOTHNESS
            #endif

        #else

            #define WEATHER_AFFECT
            #define WEATHER_SMOOTHNESS

        #endif

    #else

        #define MAINTEX_MGE 0
        #define WEATHER_AFFECT
        #define WEATHER_SMOOTHNESS

    #endif                                                                  
    
    //https://zhuanlan.zhihu.com/p/26383778?utm_source=weibo&utm_medium=social&utm_oi=757356763560288256
    half4 Blend(half4 high, half4 control)
    {
        half4 blend = half4(.0,.0,.0,.0);
        //该参数可暴露调整
        half weight = 0.3;

        blend.r = high.r * control.r;
        blend.g = high.g * control.g;
        blend.b = high.b * control.b;
        blend.a = high.a * control.a;

        half ma = max(blend.r,max(blend.g,max(blend.b,blend.a)));
        blend = max(blend - ma + weight,0) * control;
        return blend / (blend.r + blend.g + blend.b + blend.a);
    }

    #if defined(_LAYER_ONE)

        #define SPLATUV(idx1, idx2) half2 uv0 : TEXCOORD##idx1;

        #define MAINTEX_UV(o, texcoord)                                                     \
        o.uv = texcoord.xy * _Control_ST.xy + _Control_ST.zw;                               \
        o.uv0 = texcoord.xy * _UVTiling01.xy;   

        #if USE_NORMAL && USE_VIEWDIR && USE_NORMAL_MAP
            #define MAINTEX_COLOR(color, i)                                                           \
            half3 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv0).rgb;                                      \
            half3 diffuse, spec;                                                                      \
            LightResult(i.tangentDir,i.bitangentDir,i.worldNormal,i.viewDir,_ROCLightDir0,_ROCLightColor0,i.uv0,diffuse,spec);                                     \
            half4 color = saturate(half4(lay1 * diffuse + spec, 1));
        #else
            #define MAINTEX_COLOR(color, i)                                                           \
            half3 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv0).rgb;                                      \
            half4 color = saturate(half4(lay1, 1));
        #endif

        

    #elif defined(_LAYER_TWO)
            
        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat1);

        #define SPLATUV(idx1, idx2) half4 uv01 : TEXCOORD##idx1;

        #define MAINTEX_UV(o, texcoord)                                                     \
        o.uv = texcoord.xy * _Control_ST.xy + _Control_ST.zw;                               \
        o.uv01 = texcoord.xyxy * _UVTiling01; 

        #if USE_NORMAL && USE_VIEWDIR && USE_NORMAL_MAP
            #define MAINTEX_COLOR(color, i)                                                     \
            half2 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rg;                                  \
            half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
            half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
            half4 color = const_v4c;                                                            \
            half4 high = half4(lay1.a,lay2.a,.0,.0);                                            \
            half4 control = half4(ctr,.0,.0);                                                   \
            half4 blend = Blend(high,control);                                                  \
            half3 diffuse, spec;                                                                \
            LightResult(i.tangentDir,i.bitangentDir,i.worldNormal,i.viewDir,_ROCLightDir0,_ROCLightColor0,i.uv01,diffuse,spec);                                     \
            color.rgb = lay1.rgb * diffuse * blend.r + lay2.rgb * blend.g ;                               \
            color.rgb = saturate(color.rgb + spec * blend.r);
        #else
             #define MAINTEX_COLOR(color, i)                                                     \
              half2 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rg;                                  \
              half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
              half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
              half4 color = const_v4c;                                                            \
              half4 high = half4(lay1.a,lay2.a,.0,.0);                                            \
              half4 control = half4(ctr,.0,.0);                                                   \
              half4 blend = Blend(high,control);                                                  \
              color.rgb = lay1.rgb * blend.r + lay2.rgb * blend.g ;                               \
              color.rgb = saturate(color.rgb); 
        #endif
    #elif defined(_LAYER_THREE)

        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat1);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat2);

        #define SPLATUV(idx1, idx2) half4 uv01 : TEXCOORD##idx1; half2 uv2 : TEXCOORD##idx2;

        #define MAINTEX_UV(o, texcoord)                                                     \
        o.uv = texcoord.xy * _Control_ST.xy + _Control_ST.zw;                               \
        o.uv01 = texcoord.xyxy * _UVTiling01;                                               \
        o.uv2 = texcoord.xy * _UVTiling23.xy;   

        #if USE_NORMAL && USE_VIEWDIR && USE_NORMAL_MAP
            #define MAINTEX_COLOR(color, i)                                                     \
            half3 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rgb;                                 \
            half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
            half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
            half4 lay3 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat2, _Splat0, i.uv2).rgba;              \
            half4 color = const_v4c;                                                            \
            half4 high = half4(lay1.a,lay2.a,lay3.a,.0);                                        \
            half4 control = half4(ctr,.0);                                                      \
            half4 blend = Blend(high,control);                                                  \
            half3 diffuse, spec;                                                                \
            LightResult(i.tangentDir,i.bitangentDir,i.worldNormal,i.viewDir,_ROCLightDir0,_ROCLightColor0,i.uv01,diffuse,spec);                                     \
            color.rgb = lay1.rgb * diffuse * blend.r + lay2.rgb * blend.g + lay3.rgb * blend.b ;          \
            color.rgb = saturate(color.rgb + spec * blend.r);
        #else
            #define MAINTEX_COLOR(color, i)                                                     \
            half3 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rgb;                                 \
            half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
            half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
            half4 lay3 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat2, _Splat0, i.uv2).rgba;              \
            half4 color = const_v4c;                                                            \
            half4 high = half4(lay1.a,lay2.a,lay3.a,.0);                                        \
            half4 control = half4(ctr,.0);                                                      \
            half4 blend = Blend(high,control);                                                  \
            color.rgb = lay1.rgb * blend.r + lay2.rgb * blend.g + lay3.rgb * blend.b ;          \
            color.rgb = saturate(color.rgb);
        #endif
    #else

        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat1);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat2);
        UNITY_DECLARE_TEX2D_NOSAMPLER(_Splat3);

        #define SPLATUV(idx1, idx2) half4 uv01 : TEXCOORD##idx1; half4 uv23 : TEXCOORD##idx2;
        
        #define MAINTEX_UV(o, texcoord)                                                     \
        o.uv = texcoord.xy * _Control_ST.xy + _Control_ST.zw;                               \
        o.uv01 = texcoord.xyxy * _UVTiling01;                                               \
        o.uv23 = texcoord.xyxy * _UVTiling23;

        #if USE_NORMAL && USE_VIEWDIR && USE_NORMAL_MAP
            #define MAINTEX_COLOR(color, i)                                                     \
            half4 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rgba;                                \
            half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
            half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
            half4 lay3 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat2, _Splat0, i.uv23.xy).rgba;          \
            half4 lay4 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat3, _Splat0, i.uv23.zw).rgba;          \
            half4 color = const_v4c;                                                            \
            half4 high = half4(lay1.a,lay2.a,lay3.a,lay4.a);                                    \
            half4 blend = Blend(high,ctr);                                                      \
            half3 diffuse, spec;                                                                \
            LightResult(i.tangentDir,i.bitangentDir,i.worldNormal,i.viewDir,_ROCLightDir0,_ROCLightColor0,i.uv01,diffuse,spec);                                     \
            color.rgb = lay1.rgb * diffuse * blend.r + lay2.rgb * blend.g + lay3.rgb * blend.b + lay4.rgb * blend.a;      \
            color.rgb = saturate(color.rgb + spec * blend.r);
        #else
            #define MAINTEX_COLOR(color, i)                                                     \
            half4 ctr = UNITY_SAMPLE_TEX2D(_Control, i.uv).rgba;                                \
            half4 lay1 = UNITY_SAMPLE_TEX2D(_Splat0, i.uv01.xy).rgba;                           \
            half4 lay2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat1, _Splat0, i.uv01.zw).rgba;          \
            half4 lay3 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat2, _Splat0, i.uv23.xy).rgba;          \
            half4 lay4 = UNITY_SAMPLE_TEX2D_SAMPLER(_Splat3, _Splat0, i.uv23.zw).rgba;          \
            half4 color = const_v4c;                                                            \
            half4 high = half4(lay1.a,lay2.a,lay3.a,lay4.a);                                    \
            half4 blend = Blend(high,ctr);                                                      \
            color.rgb = lay1.rgb * blend.r + lay2.rgb * blend.g + lay3.rgb * blend.b + lay4.rgb * blend.a;      \
            color.rgb = saturate(color.rgb);
        #endif

        
        
    #endif

#else

    UNITY_DECLARE_TEX2D(_MainTex); half4 _MainTex_ST;
    half _Emission;
    half _Opacity;
    half _SnowFactor;

    #if USE_FORWARDRENDER

        #if defined(_MSE_ON)
            UNITY_DECLARE_TEX2D_NOSAMPLER(_MSETex);
            #define MAINTEX_MGE UNITY_SAMPLE_TEX2D_SAMPLER(_MSETex, _MainTex, i.uv).rgb 
        #elif USE_DIRLIGHT
            #define MAINTEX_MGE half3(_Metallic, _Smoothness, _Emission)
        #else
            #define MAINTEX_MGE half3(0, 0, _Emission)
        #endif

        #if (defined(SCENE_MAP_RAIN) || defined(SCENE_MAP_SNOW))

            #if USE_DIRLIGHT
            #define WEATHER_AFFECT GetSnowOrRainFactor(worldPos, i.sceneMapCoord.z, 0, _SnowFactor, diffColor, specColor, i.worldNormal, smoothness, i.sceneMapCoord.w);
            #else
            #define WEATHER_AFFECT GetSnowOrRainFactor(i.sceneMapCoord.w);
            #endif

            #if defined(SC_DIR_LIGHT)
            #define WEATHER_SMOOTHNESS GetRainSmoothness(o.sceneMapCoord.z, 0, smoothness);
            #else
            #define WEATHER_SMOOTHNESS
            #endif

        #else
            #define WEATHER_AFFECT
            #define WEATHER_SMOOTHNESS
        #endif

    #else

        #define MAINTEX_MGE 0
        #define WEATHER_AFFECT
        #define WEATHER_SMOOTHNESS

    #endif

    #define MAINTEX_UV(o, texcoord) o.uv = texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;   
                                                
    #if USE_NORMAL && USE_VIEWDIR && USE_NORMAL_MAP
        #define MAINTEX_COLOR(color, i)                                                 \
        half4 color = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);                               \
        half3 diffuse, spec;                                                            \
        LightResult(i.tangentDir,i.bitangentDir,i.worldNormal,i.viewDir,_ROCLightDir0,_ROCLightColor0,i.uv,diffuse,spec);                                     \
        color.rgb = color.rgb * diffuse + spec;
    #else
        #define MAINTEX_COLOR(color, i) half4 color = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
    #endif
    

#endif

#if USE_SCENE_MAP
    #define GET_SCEMEMAP(i) GET_SCENE_PARAM(i)
#else
    #define GET_SCEMEMAP(i)
#endif

#if USE_VERTEXANIM && defined(SC_OBJECT)
    #include "GpuSkinning.cginc"    
    #define VERTEXOFFSET VERTEXANIM(v)
#else
    #define VERTEXOFFSET
#endif

#ifdef  SC_GRASS
    fixed _ShadowFactor;
#endif

#ifdef _OCCLUDER_NEW_ON
    UNITY_DECLARE_TEX2D(_DitherMaskLOD2D);
    void ditherAlphaClip(float2 pos,float alpha)
    {
        pos /= 4;
        pos.y = min(frac(pos.y) * 0.0625 /* 1/16 */ + alpha, 0.9375);
        clip(UNITY_SAMPLE_TEX2D(_DitherMaskLOD2D, pos).a - 0.5);
    }
    float _DissolveDistance;
    float _DissolveDistanceFactor;
#endif

    uniform half _Focus;
            uniform half _FocusX;
            uniform half _FocusY;
            uniform half _Aperture;

//================================================================================================
//
// 结构与变量
//
//================================================================================================

struct appdata 
{
    float4 vertex : POSITION;
    half2 texcoord : TEXCOORD0;
    half2 texcoordLM : TEXCOORD1;

    #if defined(DYNAMICLIGHTMAP_ON)
        half2 texcoordDLM : TEXCOORD2;
    #endif

    #if defined(SC_OBJECT)
        half4 color : COLOR;
    #endif

    #if USE_NORMAL
        half3 normal : NORMAL;
        float4 tangent : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct v2f 
{
    #ifdef SC_SHADOWCASTER
        V2F_SHADOW_CASTER_NOPOS
    #else
        float4 pos : SV_POSITION;

        #if USE_DIRLIGHT
            half4 ambientOrLightmapUV : TEXCOORD0;
        #else
            half2 ambientOrLightmapUV : TEXCOORD0;
        #endif

    #endif

    half2 uv : TEXCOORD1;

    #if USE_WORLDPOS
    float3 worldPos : TEXCOORD2;
    #endif

    #if USE_SCENE_MAP
    SCENE_PARAM_COORDS(3, 4)
    #endif

    #if USE_VIEWDIR
    float3 viewDir : TEXCOORD5;
    #endif

    #if defined(SC_TERRAIN)
    SPLATUV(6, 7)

    #elif USE_VERTEXANIM
    half4 color : TEXCOORD6;
    #endif

    #if USE_SCRPOS
    half4 scrPos : TEXCOORD12;
    #endif

    #if USE_NORMAL
        half3 worldNormal : TEXCOORD8;
        float3 tangentDir : TEXCOORD11;
        float3 bitangentDir : TEXCOORD13;
    #endif
    
    #if USE_FORWARDRENDER && defined(SC_BAKE_LIGHT)
        UNITY_SHADOW_COORDS(9)
    #elif defined(SC_DIR_LIGHT)
        half4 litTerm : TEXCOORD9;
    #endif

    //增加使用点光源时的输出颜色2019.06.14
    #ifdef ENABLE_POINTLIGHT
        half3 pointLightColor : COLOR;
    #endif

    #ifdef _OCCLUDER_NEW_ON
        float3 playerPosView : TEXCOORD14;
        float3 posView : TEXCOORD15;
    #endif
    float4 debug : TEXCOORD30;
            
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

half _Cutoff;
uniform half4 _ROTerrainLightColor;
uniform half4 _ROTerrainDarkColor;
uniform half _ROTerrainThreshold;
half _LMFactor;
half _FogFactor;

#ifdef SC_BAKE_LIGHT
    half _VISLM;
#endif

//================================================================================================
//
// API适配
//
//================================================================================================

inline half3 NormalizePerVertexNormal(float3 n) // takes float to avoid overflow
{
    #if (SHADER_TARGET < 30)
    return normalize(n);
    #else
    return n; // will normalize per-pixel instead
    #endif
}

inline half3 NormalizePerPixelNormal(half3 n)
{
    #if (SHADER_TARGET < 30)
    return n;
    #else
    return normalize(n);
    #endif
}

//================================================================================================
//
// 草地交互
//
//================================================================================================

#ifdef  SC_GRASS
    float4 InteractGrass(float4 worldPos)
    {
        float3 sphereDisp = worldPos.xyz - _ROGrassInteractPos;
        worldPos.xz += sphereDisp.xz * (1 - saturate(length(sphereDisp)));
        return worldPos;
    }
#endif


//================================================================================================
//
// 顶点预处理
//
//================================================================================================

void VertInit(inout v2f o, appdata v)
{
    #ifndef SC_SHADOWCASTER
        float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

        #ifdef  SC_GRASS
            worldPos = InteractGrass(worldPos);
        #endif

        #if USE_WORLDPOS
            o.worldPos = worldPos.xyz;
        #endif

        o.pos = mul(UNITY_MATRIX_VP, worldPos);

        #if USE_SCRPOS
            o.scrPos = ComputeScreenPos(o.pos);
        #endif

        #ifdef _OCCLUDER_NEW_ON
            o.playerPosView = mul(UNITY_MATRIX_V, float4(_ROGrassInteractPos, 1)).xyz;
            o.posView = mul(UNITY_MATRIX_V, worldPos).xyz;
        #endif
    #endif
    
    MAINTEX_UV(o, v.texcoord)

    #if USE_WORLDPOS
    float3 viewDir = _WorldSpaceCameraPos.xyz - worldPos.xyz;
    #endif

    #if USE_FORWARDRENDER

        #if USE_NORMAL
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
        o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
        o.bitangentDir = normalize(cross(o.worldNormal, o.tangentDir) * v.tangent.w);
        #endif

        #if USE_VIEWDIR
        o.viewDir = viewDir;
        #endif

    #endif

    #if USE_SCENE_MAP

    TRANSFER_SCATTER(o, -viewDir, worldPos.y)

    #endif
}

half4 VertGI(
    half2 texcoordLM, 
    #ifdef DYNAMICLIGHTMAP_ON
    half2 texcoordDLM,
    #endif
    float3 worldPos, half3 worldNormal) 
{
    half4 ambientOrLightmapUV = 0;

    #ifdef LIGHTMAP_ON

    ambientOrLightmapUV.xy = texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    ambientOrLightmapUV.zw = 0;

    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif defined(LIGHTPROBE_SH)

        #ifdef VERTEXLIGHT_ON
        // Approximated illumination from non-important point lights
        ambientOrLightmapUV.rgb += Shade4PointLights(
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, worldPos, worldNormal);
        #endif

        ambientOrLightmapUV.rgb += ShadeSHPerVertex(worldNormal, ambientOrLightmapUV.rgb);
    #endif
    
    #ifdef DYNAMICLIGHTMAP_ON
    ambientOrLightmapUV.zw = texcoordDLM.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    return ambientOrLightmapUV;
}

//================================================================================================
//
// 像素光照
//
//================================================================================================

#if USE_DIRLIGHT

    inline half FastGGXDGF(half NoH, half LoH, half roughness, half roughness2)
    {
        half d = NoH * NoH * (roughness2 - 1.0) + 1.00001h;
        half LoH2 = LoH * LoH;
        half normalizationTerm = roughness * 4.0 + 2.0;
        half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * normalizationTerm);

        #if defined (SHADER_API_MOBILE)
            specularTerm = specularTerm - 1e-4;
            specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
        #endif

        return specularTerm;
    }

    //LambertTerm, SpecularTerm, FresnelTerm, GrazingTerm
    inline half4 LitAtten(half3 worldNormal, half3 viewDir, float3 lightDir, half smoothness, half reflectivity, half atten)
    {
        viewDir = normalize(viewDir);
        half3 halfDir = normalize(lightDir + viewDir);

        half3 cosTheta = mul(half3x3(lightDir, viewDir, halfDir), worldNormal);
        cosTheta = saturate(cosTheta);

        half NoL = cosTheta.x;
        half NoV = cosTheta.y;
        half NoH = cosTheta.z;

        #ifdef SC_BAKE_LIGHT

            half LoH = saturate(dot(lightDir, halfDir));
            half roughness = 1 - smoothness;
            roughness *= roughness;
            roughness = max(roughness, 0.002);
            half specularTerm = FastGGXDGF(NoH, LoH, roughness, roughness * roughness);

        #else

            half specularTerm = pow(NoH, lerp(1, 128, smoothness));

        #endif

        half fresnelTerm = 1 - NoV;
        fresnelTerm = fresnelTerm * fresnelTerm;    //p2
        fresnelTerm = fresnelTerm * fresnelTerm;    //p4

        half gazingTerm = saturate(smoothness + reflectivity);

        return half4(NoL * atten, specularTerm, fresnelTerm, gazingTerm);
    }

    inline half3 DirLight(half3 diffColor, half3 specColor, half3 litColor, half4 litTerm)
    {
        //x = LambertTerm, y = SpecularTerm
        return (diffColor + specColor * litTerm.y) * litColor * litTerm.x;
    }

    inline half3 ReflectionIBL(UNITY_ARGS_TEXCUBE(tex), half4 hdr, half3 reflDir, half mipLevel)
    {
        return DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(tex, reflDir, mipLevel), hdr);
    }

    inline half3 Reflaction(half3 specColor, half smoothness, half3 worldNormal, half3 viewDir, half4 litTerm)
    {
        half3 col = const_v30;
        viewDir = normalize(viewDir);
        #if (defined(SCENE_MAP_RAIN) && defined(SC_REFLECTION)) || defined(SC_BAKE_LIGHT)

            half perceptualRoughness = 1 - smoothness;
            half roughness =  perceptualRoughness * perceptualRoughness;
            half mipLevel = perceptualRoughness * 1.7 - roughness * 0.7;
            mipLevel *= UNITY_SPECCUBE_LOD_STEPS; 

            col = ReflectionIBL(UNITY_PASS_TEXCUBE(unity_SpecCube0), 
                unity_SpecCube0_HDR, 
                reflect(-viewDir, worldNormal), 
                mipLevel);

            #if defined(SC_BAKE_LIGHT)
            col *= 1.0 / (roughness * roughness + 1.0);
            #elif defined(SC_DIR_LIGHT)
            col *= 0.28;
            #endif

            //z = FresnelTerm, w = GrazingTerm 
            col *= lerp(specColor, litTerm.w, litTerm.z);

        #endif

        return col;
    }

#endif

//================================================================================================
//
// 颜色处理
//
//================================================================================================

void FinalColor(inout half4 color, v2f i) 
{
    half shadowAtten = 1;

    //============================================================================================
    //  处理玩家投影
    //============================================================================================

    #if USE_SHADOWMAP

        #if defined(SC_TERRAIN) || (defined(SC_OBJECT) && !(defined(SCENE_MAP_SNOW) || defined(SCENE_MAP_RAIN)))

            shadowAtten = 1 - ShadowMapFactor(i.worldPos);

        #endif

        #if USE_NORMAL

            half shadowClip = (dot(_SMLightFwd,i.worldNormal)>0);

            shadowAtten=lerp(shadowAtten,1,shadowClip);

        #endif

        #if USE_SCENE_MAP
                
            SHADOWMIX(shadowAtten, i)

        #endif
        
    #endif

    half3 col = color.rgb;
    half alpha = color.a;

    #if USE_SCENE_MAP

        //========================================================================================
        //  处理环境整体色偏
        //========================================================================================

        ApplySceneColor(col);

        //========================================================================================
        //  处理环境影响
        //========================================================================================

        //雪天模式
        #if defined(SCENE_MAP_SNOW) || defined(SCENE_MAP_RAIN)

            //阴影不会太深
            ApplyShadow(col, max(shadowAtten, 0.5));

            APPLY_SCENE_FOG(col, i, _FogFactor)

            //天气云层压暗
            col *= i.sceneMapCoord.w;

            #if defined(SCENE_MAP_RAIN)

                #if defined(SC_DIR_LIGHT)
                col += GetLightningColor(i.worldPos, i.worldNormal);
                #else
                col += GetLightningColor(i.worldPos);
                #endif

            #endif
        #elif defined(SCENE_MAP_NOCLOUD)

            //阴影不会太深
            ApplyShadow(col, max(shadowAtten, 0.5));

            APPLY_SCENE_FOG(col, i, _FogFactor)

        //默认情况或流动模式
        #else

            //叠加角色阴影
            ApplyShadow(col, shadowAtten);

            //云层阴影
            ApplyCloudMask(col, i.sceneMapCoord.z);

            APPLY_SCENE_FOG(col, i, _FogFactor)

        #endif

        color.rgb = col;

    #endif

    //============================================================================================
    //  处理半透
    //============================================================================================

    #ifdef SC_OBJECT
    alpha *= _Opacity;
    #endif

    #ifdef SC_GRASS
    half dist = distance(i.worldPos, _WorldSpaceCameraPos);
    alpha *= 1 - saturate((dist - 40) / 10);
    #endif

    //============================================================================================
    //  处理透明剔除
    //============================================================================================

    #if defined(SC_OCCLUDER)

        #if USE_SCRPOS
        float2 vpos = i.scrPos.xy / i.scrPos.w * _ScreenParams.xy;
        #else
        float2 vpos = i.pos.xy;
        #endif

        half d = length((vpos * 2 - _ScreenParams.xy) / _ScreenParams.xx);
        d = smoothstep(0.25, 0.75, d);
        d *= d;

        alpha = _Opacity * d;

        // vpos /= 4; // the dither mask texture is 4x4
        // vpos.y = min(frac(vpos.y) * 0.0625 /* 1/16 */ + _Opacity * d, 0.9375); // quantized lod fade by 16 levels
        // clip(UNITY_SAMPLE_TEX2D(_DitherMaskLOD2D, vpos).a - 0.5);
        // alpha = 1;

    #endif

    #ifdef _ALPHAMODE_CUTOUT

        clip(alpha - _Cutoff);
        alpha = 1;

    #endif

    color.a = alpha;
}

half3 BakeColor(half3 bakeGI)
{
    return lerp(const_v31, bakeGI * lerp(_ROTerrainDarkColor, _ROTerrainLightColor, smoothstep(_ROTerrainThreshold, 1.0, bakeGI.r)), _LMFactor);
}

//================================================================================================
//
// 顶点与片段
//
//================================================================================================

v2f vert(appdata v
#ifdef SC_SHADOWCASTER
    , out float4 opos : SV_POSITION
#endif
#ifdef _GPUANIM_VERTEXMAP
    , uint vid : SV_VertexID
#endif
) {
    UNITY_SETUP_INSTANCE_ID(v);                                                 
    v2f o;                                                                          
    UNITY_INITIALIZE_OUTPUT(v2f, o);       
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    VERTEXOFFSET

    VertInit(o, v);

    #if USE_FORWARDRENDER

        #if defined(SC_BAKE_LIGHT)
            UNITY_TRANSFER_SHADOW(o, v.texcoordLM.xy);
        #elif defined(SC_DIR_LIGHT)

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
            o.litTerm = LitAtten(o.worldNormal, o.viewDir, lightDir, smoothness, reflectivity, _LightFactor);

        #endif

        #if USE_DIRLIGHT

            #ifdef DYNAMICLIGHTMAP_ON
            o.ambientOrLightmapUV = VertGI(v.texcoordLM.xy, v.texcoordDLM.xy, o.worldPos, o.worldNormal);
            #else
            o.ambientOrLightmapUV = VertGI(v.texcoordLM.xy, o.worldPos, o.worldNormal);
            #endif

        #else
        o.ambientOrLightmapUV.xy = v.texcoordLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        #endif

    #elif defined(SC_SHADOWCASTER)

        TRANSFER_SHADOW_CASTER_NOPOS(o, opos)

    #endif

    //=============================================================================
    // 扩充为4光源2019.06.14
    //=============================================================================

    #if USE_WORLDPOS
        float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
    #endif

    #ifdef ENABLE_POINTLIGHT
        //half3 lightdir = half3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x) - worldPos.xyz;
        //half length = lightdir.x * lightdir.x + lightdir.y * lightdir.y + lightdir.z * lightdir.z;
        //half atten = 1.0 / (1.0 + length * unity_4LightAtten0.x);
        //o.pointLightColor = unity_LightColor[0] * saturate(dot(worldNormal, normalize(lightdir))) * atten;
        o.pointLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
            unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
            unity_4LightAtten0, worldPos.xyz, o.worldNormal);
    #endif

    return o;
}

half4 frag(v2f i) : SV_Target
{    
    UNITY_SETUP_INSTANCE_ID(i);

    #if USE_FORWARDRENDER

        #if USE_WORLDPOS
            float3 worldPos = i.worldPos;
        #endif

        #if USE_NORMAL
            i.worldNormal = normalize(i.worldNormal);
        #endif

       

    #endif

    //====================================================================================================
    //  环境影响采样
    //====================================================================================================

    GET_SCEMEMAP(i)

    //====================================================================================================
    //  基础色采样
    //====================================================================================================
    MAINTEX_COLOR(color, i)
    
    color.rgb = GammaToLinearSpace(color.rgb);       //基礎采樣更換綫性空間

    #if USE_FORWARDRENDER

        //================================================================================================
        //  环境离线光(烘焙光照+球偕光照)
        //================================================================================================

        half3 bakeGI = const_v30;

        #if defined(LIGHTMAP_ON)
            
            bakeGI += DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb;
            //更換線性空間//針對綫性空間色彩補償
            float fixedSaturation = min(0.5 * max(bakeGI.r,max(bakeGI.g, bakeGI.b)),1.0);
            bakeGI = lerp(bakeGI.rgb, GammaToLinearSpace(bakeGI.rgb), fixedSaturation);
            //bakeGI += GammaToLinearSpace(DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.ambientOrLightmapUV.xy)).rgb);
            ////End更換線性空間

        #elif (defined(LIGHTPROBE_SH) && USE_DIRLIGHT)
            bakeGI += ShadeSHPerPixel(i.worldNormal, i.ambientOrLightmapUV.rgb, worldPos);
        #endif

        #if defined(DYNAMICLIGHTMAP_ON)
        ////更換線性空間
        half3 rtimeGI = DecodeRealtimeLightmap(UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, i.ambientOrLightmapUV.zw)).rgb;        
       //half3 rtimeGI = GammaToLinearSpace(DecodeLightmap(DecodeRealtimeLightmap(UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, i.ambientOrLightmapUV.zw)).rgb);

        bakeGI += rtimeGI;

        #endif

        #ifdef ENABLE_POINTLIGHT
            bakeGI += i.pointLightColor;
        #endif 

        //================================================================================================
        //  分特性组合光照
        //================================================================================================

        #if USE_DIRLIGHT

            //============================================================================================
            //  方向光源
            //============================================================================================

            #ifdef SC_BAKE_LIGHT
                ////更換線性空間
                half3 lightColor = _LightColor0.rgb;
                //half3 lightColor = GammaToLinearSpace(_LightColor0.rgb);

                UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

                #if defined(SC_FORWARDBASE)

                    float3 lightDir = _WorldSpaceLightPos0.xyz;

                #elif defined(SC_FORWARDADD)

                    lightColor *= atten;

                    float3 lightDir = _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;

                    #ifndef USING_DIRECTIONAL_LIGHT
                        lightDir = NormalizePerVertexNormal(lightDir);
                    #endif

                #endif

            #else
                half3 lightColor = _ROCLightColor0.rgb;
                float3 lightDir = _ROCLightDir0;
                half atten = _LightFactor;

            #endif
            
            //SaveNormalize
            lightDir *= rsqrt(max(0.001, dot(lightDir, lightDir)));

            half3 mse = MAINTEX_MGE;
            half reflectivity = mse.x;
            half smoothness = mse.y;
            half3 diffColor = color.rgb * max(0.04, 1 - reflectivity);
            half3 specColor = color.rgb * reflectivity;

            half3 colLit = const_v30;           

            //================================================================================================
            //  天气处理影响变量
            //================================================================================================

            WEATHER_AFFECT

            //================================================================================================
            //  自发光
            //================================================================================================

            #ifdef SC_OBJECT
                //emission
                colLit += color.rgb * mse.zzz;
            #endif

            //================================================================================================
            //  光源计算量 x= 主光方向强度 y=高光强度 z=菲涅尔强度 w=掠入光强度
            //================================================================================================

            smoothness = clamp(smoothness, 0.002h, 0.998h);

            #if defined(SC_BAKE_LIGHT) 

                half4 litTerm = LitAtten(i.worldNormal, i.viewDir, lightDir, smoothness, reflectivity, atten);

            #elif defined(SC_DIR_LIGHT)

                half4 litTerm = i.litTerm;

            #else

                half4 litTerm = const_v40;

            #endif

            //================================================================================================
            //  叠加方向光
            //================================================================================================

            colLit += DirLight(diffColor, specColor, lightColor, litTerm);

            #ifndef SC_FORWARDADD

                //============================================================================================
                //  叠加反光
                //============================================================================================

                colLit += Reflaction(specColor, smoothness, i.worldNormal, i.viewDir, litTerm);

                //============================================================================================
                //  叠加烘焙光或球偕光
                //============================================================================================

                bakeGI = BakeColor(bakeGI);
                colLit += diffColor * bakeGI;

                #ifdef SC_BAKE_LIGHT
                colLit = lerp(colLit, bakeGI, _VISLM);
                #endif

            #endif      

            color.rgb = min(const_v31, colLit);

        #else

            //================================================================================================
            //  天气处理影响变量
            //================================================================================================

            WEATHER_AFFECT

            half3 colLit = const_v30;

            //================================================================================================
            //  自发光
            //================================================================================================

            #ifdef SC_OBJECT
            ////更換線性空間
            colLit += color.rgb * _Emission;
            //colLit += color.rgb *  GammaToLinearSpace(_Emission);
            #endif

            //================================================================================================
            //  叠加烘焙光
            //================================================================================================

            bakeGI = BakeColor(bakeGI);
            colLit += color.rgb * bakeGI;

            color.rgb = colLit;

        #endif

    #endif

    //========================================================================================================
    //  叠加环境影响
    //========================================================================================================
    
    #ifdef SC_GRASS
    //2019.07.02增加草自适应场景阴影功能
    color.rgb *= lerp(_ROGrassEnvorimentColor.rgb, _ROGrassEnvorimentDarkColor.rgb * (1 - _ShadowFactor), smoothstep(0.0, 0.25, GetSceneMap(i.worldPos.xz * _ROCWorldParams.xx).y));
    #endif

    #ifdef SC_BILLBOARD_TREE
    color.rgb *= _ROTreeEnvorimentColor.rgb;
    #endif

    #ifdef SC_PLAYER_LIGHT
	float3 sphereDisp = i.worldPos.xyz - (_ROPlayerPosition + float3(0, 1, 0));
    float pointLightLen = length(sphereDisp);
    float3 pointLightDir = sphereDisp / pointLightLen;
    color.rgb += _ROPlayerLightColor * saturate(dot(i.worldNormal, -pointLightDir)) * (1 - saturate(pointLightLen / _ROPlayerLightRadius));
    #endif

    FinalColor(color, i);
    #if USE_SCRPOS
    #ifdef _OCCLUDER_NEW_ON
    //遮挡马赛克
    //float2 screenDstPos = float2(0.5,0.5);
    //float distance = 1;
    //float2 screenPos = i.scrPos.xy / i.scrPos.w ;
    //float2 dir = (screenDstPos - screenPos) * _ScreenParams.xy/_ScreenParams.yy;
    //float screenSpaceDistance = distance - sqrt(dir.x * dir.x + dir.y * dir.y);
    //float zFlag = 1 - step(0, i.playerPosView.z - i.posView.z);
    //float xFlag = 1 - step(1,abs(i.playerPosView.x - i.posView.x));
    //if(step(0,screenSpaceDistance) * zFlag * xFlag + step(0,(7 - length(i.viewDir))))
    //{
    //    float param = lerp(0.1,1,(1 - screenSpaceDistance/distance));
    //    ditherAlphaClip(i.pos.xy,param);
    //}
    #endif
    #endif

    ////更換線性空間
    color.rgb = LinearToGammaSpace(color.rgb);
    return color;
}

half4 frag_S(v2f i) : SV_Target
{
    #if defined(_ALPHAMODE_CUTOUT)
        MAINTEX_COLOR(color, i)
        clip(color.a - _Cutoff);
        return color;
    #else
        return 0;
    #endif
}

#endif