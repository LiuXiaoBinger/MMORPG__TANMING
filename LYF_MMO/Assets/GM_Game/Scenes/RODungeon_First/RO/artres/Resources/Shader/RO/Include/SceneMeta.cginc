#ifndef SCENE_META_INCLUDE
#define SCENE_META_INCLUDE

#define UNITY_PASS_META 1

#include "UnityCG.cginc"
//#include "UnityStandardInput.cginc"
#include "UnityMetaPass.cginc"
#include "UnityStandardUtils.cginc"
//#include "UnityStandardCore.cginc"

struct v2f_meta
{
    float4 uv       : TEXCOORD0;
    float4 pos      : SV_POSITION;
};

#ifdef _MSE_ON
sampler2D _MSETex;
#else
half _Metallic;
half _Smoothness;
#endif

sampler2D _MainTex; half4 _MainTex_ST;
half _Emission;

v2f_meta vert_meta (appdata_full v)
{
    v2f_meta o;
    UNITY_INITIALIZE_OUTPUT(v2f_meta, o);
    o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
    o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
    return o;
}

// Albedo for lightmapping should basically be diffuse color.
// But rough metals (black diffuse) still scatter quite a lot of light around, so
// we want to take some of that into account too.
half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half smoothness)
{
    half roughness = 1 - smoothness;
    roughness *= roughness;
    half3 res = diffuse;
    res += specular * roughness * 0.5;
    return res;
}

float4 frag_meta (v2f_meta i) : SV_Target
{
    #ifdef _MSE_ON

    half4 mse = tex2D(_MSETex, i.uv.xy);
    half metallic = mse.x;
    half smoothness = mse.y; // this is 1 minus the square root of real roughness m.

    #else

    half metallic = _Metallic;
    half smoothness = _Smoothness;

    #endif

    half3 albedo = tex2D(_MainTex, i.uv.xy);

    half oneMinusReflectivity;
    half3 specColor;
    half3 diffColor = DiffuseAndSpecularFromMetallic(albedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

    #if defined(EDITOR_VISUALIZATION)
    o.Albedo = diffColor;
    #else
    o.Albedo = UnityLightmappingAlbedo (diffColor, specColor, smoothness);
    #endif

    o.SpecularColor = specColor;
    o.Emission = albedo * _Emission;

    return UnityMetaFragment(o);
}

#endif