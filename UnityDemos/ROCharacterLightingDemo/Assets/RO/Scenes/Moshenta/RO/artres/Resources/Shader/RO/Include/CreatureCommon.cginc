#ifndef CREATURECOMMON_INCLUDE
#define CREATURECOMMON_INCLUDE

#include "UnityCG.cginc"
#include "SceneMap.cginc"

#ifdef CREATURE_SIXSKIN
#include "ColorCorrect.cginc"
#endif

//启用角色半透
#if defined(_FADE_ON)

	#define CREATURE_TRANSPARENT 1
	#define CREATURE_MASKCLIP 1

#elif defined(CREATURE_UI)

	#define CREATURE_TRANSPARENT 1
	#define CREATURE_MASKCLIP 0

#elif defined(PREZ_ON)

	#define CREATURE_TRANSPARENT 1
	#define CREATURE_MASKCLIP 0

#else

	#define CREATURE_TRANSPARENT 0
	#define CREATURE_MASKCLIP 0

#endif

#define USE_SCENE_MAP (defined(SCENE_MAP_ON))

#define API_LOW_LEVEL ((SHADER_TARGET < 30) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X))

#define USE_LIGHT_NORMAL (defined(_LIGHTM_SIMPLE) && !API_LOW_LEVEL)
#define USE_LIGHT_BF (defined(_LIGHTM_PBR) && !API_LOW_LEVEL)
#define USE_LIGHT_GGX (defined(_LIGHTM_GGX) && !API_LOW_LEVEL)
#define USE_LIGHT_PBR (USE_LIGHT_BF || USE_LIGHT_GGX)
#define USE_LIGHT (USE_LIGHT_NORMAL || USE_LIGHT_PBR)

//增加使用点光源需要的法线2019.05.28
#define USE_NORMAL (USE_LIGHT || defined(CREATURE_SOUL) || ENABLE_POINTLIGHT)
#define USE_BODY_NORMAL_MAP (USE_NORMAL && defined(BODY_NORMAL_MAP))
#define USE_HAIR_SPEC_MAP (defined(HAIR_SPEC_MAP))
#define USE_WORLDPOS (USE_LIGHT || USE_SCENE_MAP || defined(CREATURE_SOUL) || ENABLE_POINTLIGHT)
#define USE_VIEWDIR (USE_WORLDPOS)

#define USE_PROBELIGHT (USE_LIGHT && !defined(CREATURE_UI))

//#define USE_FACEMASK (defined(_ALPHA_SPLIT_ON))
#define USE_SHADOWMAP ((defined(DEPTH_SHADOW_ON) || defined(DEPTH_SHADOW_UI)) && !API_LOW_LEVEL)

#define USE_SOULFADING (defined(CREATURE_SOUL))

#define USE_SCRPOS (CREATURE_MASKCLIP && API_LOW_LEVEL)

#ifdef USE_SHADOWMAP
#include "DepthShadow.cginc"
#endif

//启用光照
#if USE_LIGHT

	CBUFFER_START(ROCommonLight)

	float3 _ROCLightDir0;
	half4 _ROCLightColor0;

	half4 _ROCBackColor;
	half4 _ROCAmbientColor;
	half4 _ROCRimColor;

	half4 _ROUICLightColor0;
	half4 _ROUICBackColor;
	half4 _ROUICAmbientColor;

	UNITY_DECLARE_TEXCUBE(_ReflectionTex) ;

	CBUFFER_END

	#include "UnityGlobalIllumination.cginc"

	#define ROLIGHTING(i)                                           \
    i.worldNormal = normalize(i.worldNormal);                       \
    half4 colLit = ROLighting(FragInput(i));

	//#define CONTRAST _Contrast

	#if defined(BLOOM_ON)

		#define CONTRAST 0.95
		#define BRIGHTNESS 0.65

	#elif defined(CREATURE_UI)

		#define CONTRAST 0.95
		#define BRIGHTNESS 0.78

	#elif USE_LIGHT_NORMAL

		#define CONTRAST 0.8
		#define BRIGHTNESS 0.8

	#else

		#define CONTRAST 0.8
		#define BRIGHTNESS 0.7

	#endif

#else

	#if defined(CREATURE_SIXSKIN)
	#define ROLIGHTING(i) half4 colLit = PartColor(i, GetPart(i.uv.x));
	#else
		#ifdef _FLOW_ON
			#define ROLIGHTING(i) half4 colLit = ProcessFlowmap(i);
		#else
			#define ROLIGHTING(i) half4 colLit = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
		#endif
	#endif

#endif

static const half3 const_v30 = half3(0, 0, 0);
static const half3 const_v31 = half3(1, 1, 1);
static const half3 const_up = half3(0, 1, 0);
static const half4 const_v40 = half4(0, 0, 0, 0);

#ifdef CREATURE_UI

	static const half3 UICAMPOS = half3(0, 1.5, -3.25);
	static const half3 UILIGHTDIR = half3(-0.6, 0.7, -0.4);
	static const half3 UILIGHTCOL = half3(0.59, 0.58, 0.58);
	static const half3 UILIGHTBAC = half3(0.52, 0.51, 0.51);
	static const half3 UILIGHTSH = half3(.5, .471, .471) * 0.7;

	#ifdef CREATURE_UI_CUSTOM
		half3 _UILIGHTDIR_CUSTOM;
	#endif
	
#endif

uniform half _Focus;
uniform half _FocusX;
uniform half _FocusY;
uniform half _Aperture;

//=============================================================================
// 顶点数据格式与光影公用变量
//=============================================================================

struct appdata 
{
	float4 vertex : POSITION;
	half2 texcoord : TEXCOORD0;
	#if USE_NORMAL
	half3 normal : NORMAL;
	half4 tangent : TANGENT;
	#endif
};

struct v2f
{
	float4 pos : SV_POSITION;
	half2 uv : TEXCOORD0;

	#if USE_WORLDPOS
	float3 worldPos : TEXCOORD1;
	#endif

    #if USE_VIEWDIR
	half3 viewDir : TEXCOORD2;
    #endif

	#if USE_SCENE_MAP
	SCENE_PARAM_COORDS(3, 4)
	#endif

	#if USE_NORMAL
	half3 worldNormal : TEXCOORD5;
	float3 tangentDir : TEXCOORD10;
	float3 bitangentDir : TEXCOORD11;
	#endif

	#if USE_LIGHT

		half4 rim : TEXCOORD6;
		half3 lightSH : TEXCOORD7;
		half3 halfDir : TEXCOORD8;

		#if USE_LIGHT_PBR 
		half3 reflDir : TEXCOORD9;
		#endif

	#endif
	
	#if USE_SOULFADING
	half fade : TEXCOORD7;
	#endif

	#if USE_SCRPOS
	half4 scrPos : TEXCOORD7;
	#endif

//增加使用点光源时的输出颜色2019.05.28
	#if ENABLE_POINTLIGHT
		half3 pointLightColor : COLOR;
	#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

 	half4 _RimParams;

//特殊处理，后面梳理，专门为UICamera特判
uniform half UICharacterSpecial;
#ifndef CREATURE_SIXSKIN
	half _Metallic;
	half _Gloss;
#endif

#if USE_LIGHT
	half2 _Falloff;
	half2 _FalloffFace;
	half _Contrast;
	half _Brightness;
#endif

half _FresnelTense = 0.2;
half _SpecularTense = 0.2;

#ifdef CREATURE_SKILLCOLOR
	half4 _Color;//技能表现Color
	half _SkillRim;
	half4 _SkillRimColor;
	half _SkillRimPower;
#endif

#if CREATURE_TRANSPARENT
	half _Visibility;
#endif

#if USE_SOULFADING
	half4 _FadeParams;
#endif

//=============================================================================
// 纹理采集与对应方法
//=============================================================================

inline half GammaToLinear (half sRGB)
{	
	return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

#ifdef USE_NORMAL
	UNITY_DECLARE_TEX2D(_NormalTex);
#endif

#if defined(CREATURE_SIXSKIN)
	#if USE_HAIR_SPEC_MAP
	UNITY_DECLARE_TEX2D(_Tex6);
	half4 _HairSpecColor;
	#endif
	UNITY_DECLARE_TEX2D(_Tex0);//Head | Head_Low
	UNITY_DECLARE_TEX2D(_Tex4);//Body
	UNITY_DECLARE_TEX2D(_Tex1);//Hair
	UNITY_DECLARE_TEX2D(_Tex2);//Weapon
	UNITY_DECLARE_TEX2D(_Tex3);//WeaponEx

	//#if USE_FACEMASK
	//UNITY_DECLARE_TEX2D_NOSAMPLER(_Tex6);//Head_M
	//#endif

	#if USE_LIGHT
	UNITY_DECLARE_TEX2D_NOSAMPLER(_Tex5);//Body_S
	#endif

	half _EmotionID;
	half _EmotionID2;
	half _EmotionSize;

	//脸部UV转换
	inline half2 FaceUV(half2 uv) 
	{
		half em = uv.y > 0.42 ? _EmotionID : _EmotionID2;//uv对应表情ID 0 ~ 15

		half size = 1.0 / _EmotionSize;
		half2 esk;
		esk.y = floor(em * _EmotionSize);
		esk.x = em - size * esk.y;
		return (uv + esk) * _EmotionSize;
	}

	inline bool4 GetPart(half p)
	{
		return floor(p) == half4(0, 1, 2, 4);//isFace, isHair, isWpon, isBody
	}

	//部件纹理采集
	half4 PartTex(v2f i, in bool4 part)
	{
		half4 col = const_v40;

		half2 uv = i.uv;

		UNITY_BRANCH
		if(part.x)
		{
			uv = FaceUV(uv);
		}

		#if API_LOW_LEVEL
		col = part.w * UNITY_SAMPLE_TEX2D(_Tex4, uv)
			+ part.x * UNITY_SAMPLE_TEX2D(_Tex0, uv)
			+ part.y * UNITY_SAMPLE_TEX2D(_Tex1, uv)
			+ part.z * UNITY_SAMPLE_TEX2D(_Tex2, uv)
			+ (1 - any(part)) * UNITY_SAMPLE_TEX2D(_Tex3, uv);
		#else
		col = part.w ? UNITY_SAMPLE_TEX2D(_Tex4, uv)
			: part.x ? UNITY_SAMPLE_TEX2D(_Tex0, uv)
			: part.y ? UNITY_SAMPLE_TEX2D(_Tex1, uv)
			: part.z ? UNITY_SAMPLE_TEX2D(_Tex2, uv)
			: UNITY_SAMPLE_TEX2D(_Tex3, uv);
		#endif

		return col;
	}

	//无光照处理
	half4 PartColor(v2f i, in bool4 part)
	{
		half4 col = PartTex(i, part);
		
		UNITY_BRANCH
		if (part.x || part.y)
		{
			col.a = part.x ? (1 - col.a) : col.a;
			col.rgb = ColorCorrectPart(col.rgb, col.a, part.y);
			col.a = 1;
		}

		return col;
	}

#else

	UNITY_DECLARE_TEX2D(_MainTex);
	half _EmotionID;
	half4 _EmotionSize;

		#ifdef _MSOE_ON
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MSOETex);
		#endif

	inline half2 FaceUV(half2 uv)
	{
		int2 faceUVRect = step(uv, _EmotionSize.zw);//确定范围内的uv才进行面部处理
		half2 grid = _EmotionSize.xy; //自行取整
		half2 scale = 1 / grid;
		half2 off = _EmotionSize.zw * scale;//每格的长宽

		half2 esk;
		esk.y = floor(_EmotionID * scale.x);//ID自行取整
		esk.x = _EmotionID - grid.x * esk.y;

		return uv + esk * off * (faceUVRect.x * faceUVRect.y).xx;
	}

#endif

#if CREATURE_MASKCLIP
	UNITY_DECLARE_TEX2D(_DitherMaskLOD2D);
#endif

#ifdef _FLOW_ON
	float     _Speed;
	float     _FlowIntensity;
	float     _FlowSignX;
	float     _FlowSignY;
	float     _FlowOffset;
	UNITY_DECLARE_TEX2D(_FlowMap);

	half4 ProcessFlowmap(v2f i) {
		float flow_scale0   = frac( _Speed * _Time[ 1 ] );
		float flow_scale1   = frac( _Speed * _Time[ 1 ] + 0.5f );
		float4 _flowMapTex = UNITY_SAMPLE_TEX2D( _FlowMap, i.uv );
		float internse = _flowMapTex.b;
		float2 flow_raw_dir = _flowMapTex.rg;
		flow_raw_dir        = 2.0 * ( flow_raw_dir.xy - float2( 0.5, 0.5 ) );
		flow_raw_dir        *= float2( _FlowSignX, _FlowSignY );
		float2 flow_dir     = flow_raw_dir * _FlowIntensity;
		float2 flow_offset  = flow_raw_dir * _FlowOffset;
		float2 flow_uv0     = i.uv + flow_dir * flow_scale0 + flow_offset;
		float2 flow_uv1     = i.uv + flow_dir * flow_scale1 + flow_offset;
		float alpha         = abs( 2.0f * ( flow_scale0 - 0.5 ) );

		float4 base_color0  = UNITY_SAMPLE_TEX2D( _MainTex, flow_uv0 );
		float4 base_color1  = UNITY_SAMPLE_TEX2D( _MainTex, flow_uv1 );
		half4 base_color   = lerp( base_color0, base_color1, alpha );
		base_color = base_color * (1+internse);
		return base_color;
	}
#endif

//=============================================================================
// 法线方法
//=============================================================================

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

half AnisoHair(half3 tangent, half3 halfDir, half exponent) {
	half th = dot(tangent, halfDir);
	half sinth = sqrt(1 - th * th);
	half dirAtten = smoothstep(-1, 0, th);
	return saturate(dirAtten * pow(sinth, exponent));
}

//=============================================================================
// 顶点方法
//=============================================================================

v2f vert (appdata v) 
{
	v2f o;
	UNITY_INITIALIZE_OUTPUT(v2f, o);

	float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));

	#if USE_WORLDPOS

		o.worldPos = worldPos.xyz;

	#endif
	
	#if USE_NORMAL
		half3 worldNormal = UnityObjectToWorldNormal(v.normal);
		o.worldNormal = worldNormal;
		#if USE_BODY_NORMAL_MAP
			o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
			o.bitangentDir = normalize(cross(o.worldNormal, o.tangentDir) * v.tangent.w);
		#endif
	#endif

	//=============================================================================
	// 顶点计算好点光源颜色2019.05.28--扩充为4光源06.14
	//=============================================================================
	#if ENABLE_POINTLIGHT
		//half3 lightdir = half3(unity_4LightPosX0.x, unity_4LightPosY0.x, unity_4LightPosZ0.x) - worldPos.xyz;
		//half length = lightdir.x * lightdir.x + lightdir.y * lightdir.y + lightdir.z * lightdir.z;
		//half atten = 1.0 / (1.0 + length * unity_4LightAtten0.x);
		//o.pointLightColor = unity_LightColor[0] * saturate(dot(worldNormal, normalize(lightdir))) * atten;
		o.pointLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, worldPos.xyz, worldNormal);
	#endif

	//=============================================================================
	// 顶点计算好rim
	//=============================================================================

	#if USE_LIGHT
	half4 rim = half4((_ROCRimColor.rgb * 2 - 1) * _ROCRimColor.aaa, _RimParams.w);
	#endif

    #if USE_VIEWDIR

        #ifdef CREATURE_UI
			float3 viewDir = UICAMPOS - worldPos.xyz;
        #else
        	float3 viewDir = _WorldSpaceCameraPos.xyz - worldPos.xyz;
        #endif

		float3 viewDis = viewDir;

		//=========================================================================
		// 顶点计算好反射方向，仅高画质下
		//=========================================================================

		#if USE_LIGHT_PBR
			o.reflDir = reflect(-normalize(viewDir), worldNormal);
		#endif

		//=========================================================================
		// 是否使用技能边缘光表现
		//=========================================================================
		
		#ifdef CREATURE_SKILLCOLOR

			viewDir = normalize(lerp(viewDir * _RimParams.xyz, viewDir, _SkillRim));

			#if USE_LIGHT
			rim = lerp(rim, half4(_SkillRimColor.rgb, _SkillRimPower), _SkillRim);
			#endif

		#else

			viewDir = normalize(viewDir * _RimParams.xyz);

		#endif

        o.viewDir = viewDir;

    #endif

	#if USE_LIGHT

		#if defined(CREATURE_UI)

			o.lightSH = UILIGHTSH;
			o.halfDir = UILIGHTDIR + viewDir;

			#ifdef CREATURE_UI_CUSTOM
				o.halfDir = _UILIGHTDIR_CUSTOM + viewDir;
			#endif

		#else

			//=====================================================================
			// 球偕光照
			//=====================================================================

			half3 sh = const_v30;
				// Approximated illumination from non-important point lights
				#ifdef VERTEXLIGHT_ON
				sh += Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos.xyz, worldNormal);
				#endif

			sh += max(const_v30, ShadeSH9(half4(worldNormal, 1.0)));

				//#ifdef CREATURE_HQ
				//half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				//half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				//half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
				//#endif

			o.lightSH = lerp(sh, _ROCAmbientColor.rgb, _ROCAmbientColor.a);
			o.halfDir = _ROCLightDir0 + viewDir;

		#endif
		
		//=========================================================================
		// 边缘光
		//=========================================================================

		o.rim = rim;

	#endif

	#if USE_SCENE_MAP

		#if !CREATURE_MASKCLIP

			GET_SCENE_PARAM(o)

		#else

			o.sceneMapCoord.x = 1;//不受雾影响

		#endif

		TRANSFER_SCATTER(o, -viewDis, worldPos.y)

	#endif

	#if USE_SOULFADING

		float worldRootY = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).y;
		float refY = worldRootY + _FadeParams.x;//参考点与当前中心距离高度差
		half fade = max(0, worldPos.y - refY);
		fade = 1.0 - exp2(-fade * _FadeParams.y);
		o.fade = fade;

	#endif

	o.pos = mul(UNITY_MATRIX_VP, worldPos);
	
	o.uv = v.texcoord.xy;

	#if USE_SCRPOS
	o.scrPos = ComputeScreenPos(o.pos);
	#endif
	
	

	return o;
}

//=============================================================================
// 片元数据
//=============================================================================

#if USE_LIGHT

	struct fragInputData {
		half3 albedo, ambient, rim, reflDir,pointLightColor;//增加点光源照明2019.05.28
		half NoL, NoV, NoH/*, LoH*/;
		half LoH;
		half hairSpec;
		half smoothness, /*roughness, roughness2, */reflectivity, emission, occlusion, alpha, rimPower;
		bool isHair;
	};

	fragInputData FragInput(v2f i)
	{
		fragInputData f;
		UNITY_INITIALIZE_OUTPUT(fragInputData, f);

		//=============================================================================
		// 光源与向量
		//=============================================================================

		half3 viewDir = i.viewDir;
		half3 halfDir = normalize(i.halfDir);

		#if USE_LIGHT_PBR

			f.reflDir = i.reflDir;//仅反光使用

			#ifdef UNITY_SPECCUBE_BOX_PROJECTION
			f.reflDir = BoxProjectedCubemapDirection(
				f.reflDir,
				i.worldPos,
				unity_SpecCube0_ProbePosition,
				unity_SpecCube0_BoxMin,
				unity_SpecCube0_BoxMax);
			#endif

		#endif

		#if defined(CREATURE_UI)
			half3 lightDir = UILIGHTDIR;//_UILightDir.xyz;
			#ifdef CREATURE_UI_CUSTOM
				lightDir = _UILIGHTDIR_CUSTOM;
			#endif
		#else
			half3 lightDir = _ROCLightDir0;
		#endif

		half3 normalDir = i.worldNormal;


		//=============================================================================
		// 采样与材质变量
		//=============================================================================

		half reflectivity = 0, smoothness = 0.5, occlusion = 1, emission = 0, atten = 0;

		half4 tex = const_v40;
		
		#if defined(CREATURE_SIXSKIN)
			//NoL, NoV, NoH
			half3 cosTheta = mul(half3x3(lightDir, viewDir, halfDir), normalDir);
			half liten = smoothstep(_Falloff.x, _Falloff.y, cosTheta.x);

			//==========================================================================
			// 获得部件纹理
			//==========================================================================

			bool4 part = GetPart(i.uv.x);
			tex = PartTex(i, part);

			f.alpha = tex.a;
			f.isHair = part.y;

			UNITY_BRANCH
			if (part.x)
			{
				//=======================================================================
				// 脸部材质处理
				//=======================================================================
				tex.a = 1 - tex.a;
				cosTheta.x = cosTheta.x * 0.33 + 0.2;
				smoothness *= tex.a;
				liten = smoothstep(_FalloffFace.x, _FalloffFace.y, cosTheta.x);
			}
			else if	(part.y)
			{					
				//=======================================================================
				// 头发材质处理
				//=======================================================================
				smoothness = 0.35;
				#if USE_HAIR_SPEC_MAP
				fixed flag = smoothstep(0,0.5,UNITY_MATRIX_V[2].y) * 0.05;
				half4 specTex = UNITY_SAMPLE_TEX2D(_Tex6, i.uv + fixed2(0,-flag));
				tex.rgb = tex.rgb * (1 - specTex.a) + specTex.a * _HairSpecColor;
				#else
				f.hairSpec = i.worldNormal.y - max(0, viewDir.y);
				#endif
			}

			#if defined(CREATURE_WPM)

				else
				{
					half2 uv = frac(i.uv);

					//=======================================================================
					// 部件材质处理
					//=======================================================================

					half4 msoe;

					UNITY_BRANCH
					if (part.w)
					{
						#if USE_BODY_NORMAL_MAP
							half3 normalTangent =  UnpackNormal(UNITY_SAMPLE_TEX2D(_NormalTex, i.uv));
							float3x3 tangentMatrix =  float3x3( i.tangentDir, i.bitangentDir, i.worldNormal);
							normalDir =  normalize(mul( normalTangent, tangentMatrix ));
							cosTheta = mul(half3x3(lightDir, viewDir, halfDir), normalDir);
							liten = smoothstep(_Falloff.x, _Falloff.y, cosTheta.x);
						#endif
						msoe = UNITY_SAMPLE_TEX2D_SAMPLER(_Tex5, _Tex0, uv);
					}
					else if (part.z)
					{
						msoe = UNITY_SAMPLE_TEX2D(_Tex2, uv * 0.5);
					}
					else
					{
						msoe = UNITY_SAMPLE_TEX2D(_Tex3, uv * 0.5);
					}

					#ifdef UNITY_COLORSPACE_GAMMA
						msoe.y = GammaToLinear(msoe.y);
					#endif

					reflectivity = msoe.x;
					smoothness = msoe.y;
					occlusion = step(1e-3, msoe.z) * (msoe.z - 1) + 1;
					emission = frac(msoe.w);
				}

			#else

				else if (part.w)
				{
					//=======================================================================
					// 身体材质处理
					//=======================================================================
					#if USE_BODY_NORMAL_MAP
						half3 normalTangent =  UnpackNormal(UNITY_SAMPLE_TEX2D(_NormalTex, i.uv));
						float3x3 tangentMatrix =  float3x3( i.tangentDir, i.bitangentDir, i.worldNormal);
						normalDir =  normalize(mul( normalTangent, tangentMatrix ));
						cosTheta = mul(half3x3(lightDir, viewDir, halfDir), normalDir);
						liten = smoothstep(_Falloff.x, _Falloff.y, cosTheta.x);
					#endif
					half4 msoe = UNITY_SAMPLE_TEX2D_SAMPLER(_Tex5, _Tex0, i.uv);

					#ifdef UNITY_COLORSPACE_GAMMA
					//msoe.y = GammaToLinear(msoe.y);
					#endif

					reflectivity = msoe.x;
					smoothness = msoe.y;
					occlusion = step(1e-3, msoe.z) * (msoe.z - 1) + 1;
					emission = frac(msoe.w + 1e-3);
				}

			#endif

			UNITY_BRANCH
			if (part.x + part.y)
			{
				tex.rgb = ColorCorrectPart(tex.rgb, tex.a, part.y);
				f.alpha = 1;
			}

			#if USE_SHADOWMAP

				//========================================================================
				// 自阴影
				//========================================================================

				//UNITY_BRANCH
				//if (!part.x)
				//{
				//	atten = ShadowMapFactor(i.worldPos);			
				//}

			#endif

		#else
			//NoL, NoV, NoH
			#if USE_BODY_NORMAL_MAP
				half3 normalTangent =  UnpackNormal(UNITY_SAMPLE_TEX2D(_NormalTex, i.uv));
				float3x3 tangentMatrix =  float3x3( i.tangentDir, i.bitangentDir, i.worldNormal);
				normalDir =  normalize(mul( normalTangent, tangentMatrix ));
			#endif
			half3 cosTheta = mul(half3x3(lightDir, viewDir, halfDir), normalDir);
			half liten = smoothstep(_Falloff.x, _Falloff.y, cosTheta.x);

			#ifdef _MOTION_ON

				//=======================================================================
				// 怪物表情处理
				//=======================================================================

				i.uv = FaceUV(i.uv);

			#endif

			#ifdef _FLOW_ON
				tex = ProcessFlowmap(i);
			#else
				tex = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
			#endif
			
			f.alpha = tex.a;

			#ifdef _MSOE_ON

				//=======================================================================
				// 怪物材质处理
				//=======================================================================

				half4 msoe = UNITY_SAMPLE_TEX2D_SAMPLER(_MSOETex, _MainTex, i.uv);
				
				#ifdef UNITY_COLORSPACE_GAMMA
					msoe.y = GammaToLinear(msoe.y);
				#endif

				reflectivity = msoe.x;
				smoothness = msoe.y;
				occlusion = step(1e-3, msoe.z) * (msoe.z - 1) + 1;
				emission = frac(msoe.w + 1e-3);

			#else

				reflectivity = _Metallic;
				smoothness = _Gloss;

			#endif

			#if USE_SHADOWMAP

				//=========================================================================
				// 自阴影
				//=========================================================================

				//atten = ShadowMapFactor(i.worldPos);

			#endif

		#endif

		#if USE_SCENE_MAP
			//atten = max(atten, i.sceneMapCoord.y);
		#endif

		atten = 1 - atten;

		//=============================================================================
		// 收缩区间, 放在部件处理完后
		//=============================================================================

		f.NoV = max(0.0001, cosTheta.y);
		f.NoH = saturate(cosTheta.z);
		f.NoL = liten * atten;//selfshadow

		//#if USE_LIGHT_GGX
			f.LoH = saturate(dot(lightDir, halfDir));
		//#endif

		//=============================================================================
		// 计算最终光照用属性
		//=============================================================================

		//f.albedo = tex.rgb * _AlbedoColor; 用于测试固有色的pbr
		f.albedo = tex.rgb;
		f.reflectivity = reflectivity;
		f.smoothness = clamp(smoothness, 0.002h, 0.998h);
		f.occlusion = occlusion;
		f.emission = emission;
		f.rim = i.rim.rgb * saturate(reflectivity + smoothness);
		f.rimPower = i.rim.a; 
		f.ambient = i.lightSH;

		//增加点光源照明2019.05.28
		#if ENABLE_POINTLIGHT
			f.pointLightColor = i.pointLightColor;
		#endif
		return f;
	}

#endif

inline half ROPow5 (half x)
{
    return x*x * x*x * x;
}

//=============================================================================
// 光照计算
//=============================================================================

#if USE_LIGHT
	// Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
	inline half DisneyDiffuseRO(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
	{
	    half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
	    // Two schlick fresnel term
	    half lightScatter   = (1 + (fd90 - 1) * ROPow5(1 - NdotL));
	    half viewScatter    = (1 + (fd90 - 1) * ROPow5(1 - NdotV));

	    return lightScatter * viewScatter;
	}

	// Generic Smith-Schlick visibility term
	inline half SmithVisibilityTermRO (half NdotL, half NdotV, half k)
	{
	    half gL = NdotL * (1-k) + k;
	    half gV = NdotV * (1-k) + k;
	    return 1.0 / (gL * gV + 1e-5f); // This function is not intended to be running on Mobile,
	                                    // therefore epsilon is smaller than can be represented by half
	}

	// Smith-Schlick derived for Beckmann
	inline half SmithBeckmannVisibilityTermRO (half NdotL, half NdotV, half roughness)
	{
	    half c = 0.797884560802865h; // c = sqrt(2 / Pi)
	    half k = roughness * c;
	    return SmithVisibilityTermRO (NdotL, NdotV, k) * 0.25f; // * 0.25 is the 1/4 of the visibility term
	}

	inline half NDFBlinnPhongNormalizedTermRO (half NdotH, half n)
	{
	    // norm = (n+2)/(2*pi)
	    half normTerm = (n + 2.0) * (0.5/UNITY_PI);

	    half specTerm = pow (NdotH, n);
	    return specTerm * normTerm;
	}

	inline half PerceptualRoughnessToSpecPowerRO (half perceptualRoughness)
	{
	    half m = perceptualRoughness * perceptualRoughness;   // m is the true academic roughness.
	    half sq = max(1e-4f, m*m);
	    half n = (2.0 / sq) - 2.0;                          // https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
	    n = max(n, 1e-4f);                                  // prevent possible cases of pow(0,0), which could happen when roughness is 1.0 and NdotH is zero
	    return n;
	}

	inline half3 FresnelTermRO (half3 F0, half cosA)
	{
	    half t = ROPow5 (1 - cosA);   // ala Schlick interpoliation
	    return F0 + (1-F0) * t;
	}

	inline half3 FresnelLerpRO (half3 F0, half3 F90, half cosA)
	{
	    half t = ROPow5 (1 - cosA);   // ala Schlick interpoliation
	    return lerp (F0, F90, t);
	}

	inline half FastGGXBRDF(half NoH, half LoH, half roughness, half roughness2)
	{
		half s = ( NoH * roughness2 - NoH ) * NoH + 1;
		half ds = roughness / max(0.1, s);

		half LoH_2 = max(1, LoH + LoH);
		half vf = LoH_2 * (LoH_2 * roughness + LoH);
		half dvf = 1.0 / vf;

		half DVF = ds * ds * dvf * 0.45;
		return DVF;
	}

	inline half SurfaceReduction(half roughness2)
	{
		return 1.0 / (roughness2 + 1.0);
	}

	inline half3 ReflectionIBL(UNITY_ARGS_TEXCUBE(tex), half4 hdr, half3 reflDir, half mipLevel)
	{
		return DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(tex, reflDir, mipLevel), hdr);
	}


	half4 ROLighting(fragInputData i)
	{
		half3 diffuse, specular, indirectDiffuse, indirectSpecular;
		half3 emission = i.albedo * i.emission;

		half perceptualRoughness = 1 - i.smoothness;
		half roughness = perceptualRoughness * perceptualRoughness;
		half roughness2 = roughness * roughness;
		// Diffuse term
		half diffuseNoV = smoothstep(0,1,(i.NoV + _FresnelTense));
    	half diffuseTerm = DisneyDiffuseRO(saturate(diffuseNoV), saturate(i.NoL), saturate(i.LoH), perceptualRoughness) * saturate(i.NoL);
		#if USE_LIGHT_GGX	
			half specTerm = FastGGXBRDF(i.NoH, i.LoH, roughness, roughness2);
		#else
			half V = SmithBeckmannVisibilityTermRO (i.NoL, diffuseNoV, roughness);
	    	half D = NDFBlinnPhongNormalizedTermRO (i.NoH, PerceptualRoughnessToSpecPowerRO(perceptualRoughness));
			half specTerm = V*D * UNITY_PI ;
			specTerm = max(0, specTerm * i.NoL);
		#endif

		//华为手机对于pow 不支持 2维操作
		//half2 fresnelTerm = pow(1 - i.NoV, half2(4, i.rimPower));
		half2 fresnelTerm = half2(pow(1 - i.NoV,4),pow(1 - i.NoV,i.rimPower));
		half fresnel = max(fresnelTerm.x, fresnelTerm.y);
		half4 ro_ColorSpaceDielectricSpec = half4(0.04, 0.04, 0.04, 1.0 - 0.04);
		half oneMinusReflectivity = ro_ColorSpaceDielectricSpec.a - i.reflectivity * ro_ColorSpaceDielectricSpec.a;

		UNITY_BRANCH
		if (i.isHair) 
		{
			diffuse = i.albedo * 1.15;
			specular = 0.04;

			indirectSpecular = unity_IndirectSpecColor.rgb;
		}
		else
		{
			diffuse = i.albedo * oneMinusReflectivity;
			specular = lerp(ro_ColorSpaceDielectricSpec.rgb,i.albedo,i.reflectivity);

			#if USE_LIGHT_PBR
				half mipLevel = 2.0*perceptualRoughness * (1.7 - perceptualRoughness * 0.7);
				mipLevel *= UNITY_SPECCUBE_LOD_STEPS;

				#if defined(CREATURE_UI)
					indirectSpecular = UNITY_SAMPLE_TEXCUBE_LOD(_ReflectionTex,i.reflDir,mipLevel);
				#else
					indirectSpecular = ReflectionIBL(UNITY_PASS_TEXCUBE(unity_SpecCube0), 
						unity_SpecCube0_HDR, 
						i.reflDir, 
						mipLevel);
				#endif
			#else
				indirectSpecular = 1;

			#endif
		}

		#if USE_LIGHT_PBR
			indirectSpecular *= SurfaceReduction(roughness2);
		#endif
		specTerm *= any(specular) ? 1.0 : 0.0;
		//=============================================================================
		// 主光源衰减
		//=============================================================================

		#if defined(CREATURE_UI)
			half lightInten = _ROUICLightColor0.a;
			half backTint = _ROUICBackColor.a;

			half3 directFront = 2 * lightInten * _ROUICLightColor0.rgb;
			half3 directBack = _ROUICBackColor.rgb;
			half3 direct = directFront * i.NoL;
			directBack = lerp(directBack, max(0, 1 - directFront), i.NoL);
			half3 ambient = lerp(_ROUICAmbientColor.rgb, directBack, backTint);
			half3 lightColor = _ROUICLightColor0.rgb;
		#else

			half lightInten = _ROCLightColor0.a;
			half backTint = _ROCBackColor.a;

			half3 directFront = 2 * lightInten * _ROCLightColor0.rgb;
			half3 directBack = _ROCBackColor.rgb;
			half3 direct = directFront * i.NoL;
			directBack = lerp(directBack, max(0, 1 - directFront), i.NoL);
			half3 ambient = lerp(i.ambient, directBack, backTint);
			half3 lightColor = _ROCLightColor0.rgb;
		#endif

		//Indirect Light
		indirectDiffuse = diffuse * ambient;
		half surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;
		half grazingTerm = saturate(i.smoothness + (1-oneMinusReflectivity));
		half3 occlusion = lerp(ambient, 1.0, i.occlusion) ;
		
		half3 col = const_v30;
		col += diffuse * (diffuseTerm * lightColor);
		col += max(0, specTerm * lightColor * FresnelTermRO(specular,i.LoH) * _SpecularTense);
		col += indirectDiffuse * occlusion;
		col += (indirectSpecular * surfaceReduction * FresnelLerpRO(specular, grazingTerm, diffuseNoV)) * occlusion;
		col	+= emission;
		//增加点光源光照2019.05.28
		#if ENABLE_POINTLIGHT
			col += i.albedo * i.pointLightColor;
		#endif

		#ifdef CREATURE_SKILLCOLOR

			col = lerp(col, _SkillRimColor, _SkillRim * fresnel);
         
		#endif

		return half4(col, i.alpha);
	}
	
#endif

//=============================================================================
// 片元方法
//=============================================================================

inline void ApplySkillColor(inout half3 color, half3 skillColor, half sat) 
{
	half3 skillCol = lerp(color, skillColor, abs(skillColor - 0.5) * 2);
	color = lerp(Luminance(color), skillCol, sat);
}

#ifdef CREATURE_SOUL

	void ApplySoulColor(inout half3 color, inout half alpha, v2f i)
	{
		half3 viewDir = normalize(i.viewDir);
		half3 worldNormal = normalize(i.worldNormal);

		half NoV = saturate(dot(viewDir, worldNormal));
		half rim = pow(1 - NoV, 2) * 0.5;

		color = color * half3(0.3, 0.8, 1) + Luminance(color) * rim;

		alpha *= lerp(i.fade, 0.5 + rim, i.fade);
	}

#endif

half4 preFrag(v2f i)
{
	ROLIGHTING(i);
	half3 col = colLit.rgb ;
	half alpha = colLit.a;

#if defined(SCENE_MAP_ON)
	ApplyShadow(col, 1 - i.sceneMapCoord.y);
	//z通道作为迷雾控制
	ApplyCloudMask(col, i.sceneMapCoord.z);
	APPLY_SCENE_FOG(col, i, const_v31);
#endif

#if defined(SCENE_MAP_ON) && defined(SCENE_MAP_RAIN)
	#if USE_LIGHT
		col += GetLightningColor(i.worldPos, i.worldNormal);
	#else
		col += GetLightningColor(i.worldPos);
	#endif
#endif


	#ifdef CREATURE_SKILLCOLOR
	ApplySkillColor(col, _Color.rgb, _Color.a);
	#endif

	#if CREATURE_TRANSPARENT

		alpha *= _Visibility;

	#elif defined(CREATURE_SOUL)

		ApplySoulColor(col, alpha, i);

	#endif

	#if CREATURE_MASKCLIP

		

	#endif
	

	clip(alpha - 0.01);
	return half4(min(const_v31, col), alpha);
}

half4 frag(v2f i) : SV_Target
{
	return preFrag(i);
}

#endif