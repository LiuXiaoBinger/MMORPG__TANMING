#ifndef EFFECTCOMMON_INCLUDE
#define EFFECTCOMMON_INCLUDE

//================================================================================================
//
// 定义与引用
//
//================================================================================================

#include "UnityCG.cginc"
#include "../Include/SceneMap.cginc"

#define USE_HDR (defined(_HDR_ON))
#define USE_THIRDTEX (defined(_THIRDMAP_ON))
#define USE_CUSTOMDATA (defined(_CUSTOMDATA_ON))
#define USE_SECONDCOLOR (defined(_SECONDCOLOR_ON))
#define USE_BLACKLESS (defined(_BLACKLESS_ON))
#define USE_NORMMAP (defined(_SECONDTYPE_DISTORT) || defined(_SECONDTYPE_FLOWMAP))
#define USE_DISSOLVE (defined(_SECONDTYPE_DISSOLVE)) || (USE_NORMMAP && USE_THIRDTEX)
#define USE_MASK (USE_DISSOLVE || defined(_SECONDTYPE_ALPHAMASK) || defined(_SECONDTYPE_ADDITIVE) || defined(_SECONDTYPE_ALPHABLEND) || defined(_SECONDTYPE_COLORBLEND)) 
#define USE_GRAD (defined(_SECONDTYPE_COLORGRAD))
#define USE_NONORMMAP (USE_MASK || USE_GRAD)
#define USE_SECONDTEX (USE_NORMMAP || USE_NONORMMAP)

#define USE_ANIMUV (defined(_SCROLLTYPE_SCROLL) || defined(_SCROLLTYPE_ANISPRITES) || defined(_SCROLLTYPE_MULTIBLEND) || defined(_SCROLLTYPE_SCALE) || defined(_SCROLLTYPE_ROTATE) || defined(_SCROLLTYPE_RANDOM))
#define USE_NORMAL (defined(_RIM_ON))
#define USE_TNORMAL (defined(EF_DISTORTION) && USE_NORMAL)
#define USE_PROJCOORD (defined(EF_PROJECTOR) || defined(EF_DISTORTION) || defined(_WORLDUV_ON))

#define USE_SCENE_MAP ((defined(SCENE_MAP_ON)) && defined(_FOG_ON))
#define USE_VIEWDIR (USE_NORMAL || USE_SCENE_MAP)

#if defined(USE_ANIMUV)
	#define real float
	#define real2 float2
	#define real3 float3
	#define real4 float4
#else
	#define real half
	#define real2 half2
	#define real3 half3
	#define real4 half4
#endif

static const half3 const_v31 = half3(1, 1, 1);
static const half4 const_v40 = half4(0, 0, 0, 0);

//================================================================================================
//
// 结构与变量
//
//================================================================================================

struct a2v {
	float4 vertex : POSITION;
	half4 color : COLOR;
	real4 texcoord : TEXCOORD0;
	
	#if USE_CUSTOMDATA
	real4 uv1 : TEXCOORD1;
	#endif

	#if USE_NORMAL
	half3 normal : NORMAL;
	#endif

	#if USE_TNORMAL
	half4 tangent: TANGENT;
	#endif
};

struct v2f {
	float4 pos : SV_POSITION;
	half4 color : COLOR;

	#if USE_THIRDTEX || defined(_SCROLLTYPE_RANDOM)
	real4 uvMain : TEXCOORD0;
	#else
	real2 uvMain : TEXCOORD0;
	#endif

	#if USE_CUSTOMDATA
		real4 uv1 : TEXCOORD1;
	#endif

	#if USE_SCENE_MAP
	float3 worldPos : TEXCOORD3;
	SCENE_PARAM_COORDS(4, 5)
	#endif

	#if USE_SECONDTEX
	real3 uvMask : TEXCOORD6;
	#endif

	#ifdef _RIM_ON
	half3 normal : TEXCOORD7;
	half3 viewDir : TEXCOORD8;
	#endif
   
	#if USE_PROJCOORD
	half4 projPos : TEXCOORD9;
	#endif

	#if USE_TNORMAL
	half3 tangent : TEXCOORD10;
	half3 binormal : TEXCOORD11;
	#endif
};

#if USE_SECONDCOLOR
	half4 _ColorA;
	half4 _ColorB;
	half _Multiple;
	half _DstBlend;
#endif

#ifndef EF_UI
	half4 _TintColor;
#endif

sampler2D _MainTex;	float4 _MainTex_ST;
half4 _MainColor;

half _Mode;

#if USE_SECONDTEX
	half _MaskType;
	half _MaskChannel;
	sampler2D _MaskTex;	float4 _MaskTex_ST;
	half4 _MaskColor;
	half _Strength;
	half _VertColorAlphaIsLifeTime;
	half _PreserveVertAlpha;
#endif

#if USE_THIRDTEX
	sampler2D _Msk2Tex;	float4 _Msk2Tex_ST;
	half _Msk2Channel;
	half _Msk2Strength;
#endif

#if USE_DISSOLVE
	half _HardFade;
	half _Cutoff;
	half _EdgeWidth;
	half4 _EdgeColor;
#elif USE_GRAD
	half _Hue;
	half _Saturate;
	half _Luminance;
	half _Contrast;
#endif

#if USE_ANIMUV
    half _ScrollType;
    half4 _ScrollMain;
    half4 _ScrollMask;
	#ifdef _SCROLLTYPE_MULTIBLEND
	half4 _ScrollMain2;
	half4 _ScrollMask2;
	#endif
    #define MAINUVST _ScrollMain
    #define MASKUVST _ScrollMask
	#ifdef _SCROLLTYPE_RANDOM
	half4 _ScrollMain2;
	half _RandomNum; // 随机滚动
	#endif
#else
    #define MAINUVST _MainTex_ST
    #define MASKUVST _MaskTex_ST
#endif

#ifdef _RIM_ON
	half4 _RimColor;
	half _RimPower;
	half _RimScatter;
#endif

#if defined(EF_DISTORTION)
	sampler2D _GrabTexture;
	half _BumpStrength;
#elif defined(EF_PROJECTOR)
	float4x4 unity_Projector;
	float4x4 unity_ProjectorClip;
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
// UV动画
//
//================================================================================================

//uv animation
inline real2 UVOffset(real2 uv, float4 param) {

	#if defined(_SCROLLTYPE_SCROLL)
    
		uv = uv * param.xy + _Time.xx * param.zw;
    
	#elif defined(_SCROLLTYPE_ANISPRITES)

		//序列图动画
		float speed = _Time.y * param.z;    
		//从行列数转为单位格占用uv宽度   
		param.xy = 1 / param.xy;
		//取当前格的xid，yid，由于序列图一般是从左上往右下做，而采样从左下往右上处理，因此 y 的取值调整为 1 - y
		param.zw = float2(floor(speed) * param.x, 1 - floor(speed * param.x) * param.y);
		uv = uv * param.xy + param.zw;//二次UV变换

	#elif defined(_SCROLLTYPE_SCALE)
    
		uv = (uv - param.zw) / param.xy + param.zw;
    
    #elif defined(_SCROLLTYPE_ROTATE)

		half2 dir;
		sincos((param.x + param.y * _Time.y), dir.x, dir.y);
		uv = mul(half2x2(dir.y, -dir.x, dir.x, dir.y), uv - param.zw) + param.zw;

	#elif defined(_SCROLLTYPE_RANDOM)
		//序列图动画
		float speed = _Time.y * param.z;    
		//speed *= _RandomNum;
		//从行列数转为单位格占用uv宽度   
		param.xy = 1 / param.xy;
		
		//取当前格的xid，yid，由于序列图一般是从左上往右下做，而采样从左下往右上处理，因此 y 的取值调整为 1 - y
		param.zw = float2(floor(speed) * param.x, 1 - floor(speed * param.x) * param.y) ;
		param.zw *= _RandomNum;
		uv = uv * param.xy + param.zw;//二次UV变换

	#else

		uv = uv * param.xy + param.zw;

	#endif
       
	return uv;
}


half4 MultiBlend(sampler2D tex, real2 uv, half4 param1, half4 param2, half4 param3)
{
	half4 col = const_v40;

	int count = min(8, param1.x);//layer
	half interval = param1.y;// per layer interval
	half duration = param1.z;// per layer duration
	real2 centerUV = param3.zw + 0.5;// texture center offset

	half2 startScale = param3.xy;// scale
	half2 finalScale = startScale * param1.w;// final scale
	half fadeSpeed = param2.x;// fadein 1, fadeout -1
	half startOpac = param2.y;// base opacity
	half startDir = param2.z;// base direction
	float rotateSpeed = param2.w * _Time.y;// rotateSpeed

	float timeFactor;
	half4 tCol;
	real2 tUV;
	half2 tDir;

	UNITY_LOOP
	for (int layer = 0; layer < count; layer++) 
	{
		timeFactor = fmod(_Time.y + interval * layer, duration) / duration;// layer time shift

		tUV = uv - centerUV;// transform to texture center
		tUV /= (lerp(startScale, finalScale, timeFactor) + 1.0e-5);// scale

		sincos(startDir * layer + rotateSpeed, tDir.x, tDir.y); // get angle
		tUV = mul(half2x2(tDir.y, -tDir.x, tDir.x, tDir.y), tUV);// rotate

		tUV += centerUV;// back to sample uv

		tCol = tex2D(tex, tUV);

		tCol.a *= lerp(startOpac, saturate(startOpac + fadeSpeed), saturate(timeFactor * abs(fadeSpeed)));// alpha change

		col = max(col, tCol);
	}

	return col;
}

//================================================================================================
//
// 纹理与通道控制
//
//================================================================================================

inline half GetMaskChannel(half4 mask, half maskChannel)
{
	int id = (int)maskChannel;
	return mask[id];
}

#if USE_DISSOLVE

	half4 Dissolve(half4 col, half level, half strength) 
	{
		half dissolve = 1 - strength * (1 - _Cutoff);
		half softDis = max(0, level - dissolve);
		half hardDis = ((level - 1) * dissolve + 1) * step(1e-3, softDis);
		half finDis = lerp(softDis, hardDis, _HardFade);
		col.a *= finDis;
		clip(col.a - 0.001);
		half edge = smoothstep(level, dissolve, softDis / _EdgeWidth);
		col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge * col.a);
		return col;
	}

#elif USE_GRAD

	#include "../Include/ColorCorrect.cginc"
	#define KMIDGREY 0.5

	//色彩变化
	void ColorGrad(inout half3 tex, half strength) 
	{
		half3 col = tex;

		#ifdef UNITY_COLORSPACE_GAMMA
		col = GammaToLinearSpace(col);
		#endif

		col = RGB2HSV(col);
		col.x = fmod(col.x + _Hue / 360, 1);
		col.yz *= half2(_Saturate, _Luminance);
		col = HSV2RGB(col);
		col = (col - KMIDGREY) * _Contrast + KMIDGREY;

		#ifdef UNITY_COLORSPACE_GAMMA
		col = LinearToGammaSpace(col);
		#endif

		tex = lerp(tex, col, strength);
	}

#endif

//二层纹理混合
inline void TextureBlend(inout half4 tex, half4 mask, half strength) 
{
	#if defined(_SECONDTYPE_ALPHAMASK)

		tex.a = lerp(tex.a, tex.a * GetMaskChannel(mask, _MaskChannel), strength);

	#elif defined(_SECONDTYPE_ADDITIVE)

		tex.rgb += mask.rgb * mask.a * strength;

	#elif defined(_SECONDTYPE_ALPHABLEND)

		tex = lerp(tex, mask, mask.a * strength);

	#elif defined(_SECONDTYPE_COLORBLEND)

		tex = lerp(tex, mask, strength);

    #elif defined(_SECONDTYPE_DISSOLVE)

		tex = Dissolve(tex, GetMaskChannel(mask, _MaskChannel), strength);

	#endif
}

#if USE_THIRDTEX

	//三层纹理混合
	inline void ThirdBlend(inout half4 tex, half4 mask, half strength)
	{
		#if USE_NORMMAP

			tex = Dissolve(tex, GetMaskChannel(mask, _Msk2Channel), strength);

		#else

			tex.a = lerp(tex.a, tex.a * GetMaskChannel(mask, _Msk2Channel), strength);

		#endif
	}
	
#endif

#ifdef _RIM_ON

	inline void AddRim(inout half4 color, half3 normal, half3 viewDir)
	{
		half nv = saturate(dot(normal, viewDir));
		color = lerp(color, _RimColor, pow(1 - nv, _RimPower));

		color.a *= pow(saturate(nv),_RimScatter);
	}

#endif

//================================================================================================
//
// 顶点与片段
//
//================================================================================================

v2f vert(a2v v) {
	
	v2f o;	
	UNITY_INITIALIZE_OUTPUT(v2f, o);

	o.color = v.color;
   	
   	#if USE_CUSTOMDATA
	o.uv1 = v.uv1;
	#endif

	float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

	o.pos = mul(UNITY_MATRIX_VP, worldPos);

    //背景扭曲，使用屏幕空间坐标
	#if defined(EF_DISTORTION)
    
	    o.projPos = ComputeGrabScreenPos(o.pos);

    //投影器，使用投影空间坐标
    #elif defined(EF_PROJECTOR)

        o.projPos = mul(unity_Projector, v.vertex);

    //使用屏幕空间坐标
    #elif defined(_WORLDUV_ON)

    	o.projPos = ComputeScreenPos (o.pos);
    	COMPUTE_EYEDEPTH(o.projPos.z);
    #endif

	real2 uvCoord;

	#if defined(EF_PROJECTOR)

		uvCoord = o.projPos.xy / o.projPos.w;

	#else

		#if USE_CUSTOMDATA
		uvCoord = v.texcoord.xy + v.texcoord.zw;
		#else
		uvCoord = v.texcoord.xy;
		#endif

	#endif

	#if defined(_SCROLLTYPE_MULTIBLEND) && !defined(_SECONDTYPE_FLOWMAP)

		o.uvMain.xy = uvCoord;
		#if USE_CUSTOMDATA
		o.uvMain.xy +=  v.uv1.xy;
		#endif
		#if USE_MASK || USE_NORMMAP
		o.uvMask.xy = uvCoord;
		#endif

	#else
		#if defined(_SCROLLTYPE_RANDOM)
			o.uvMain.xy = UVOffset(uvCoord, MAINUVST);
			//序列图动画
			float speed = _Time.y * _ScrollMain2.z;    
			//从行列数转为单位格占用uv宽度   
			_ScrollMain2.xy = 1 / _ScrollMain2.xy;
			//取当前格的xid，yid，由于序列图一般是从左上往右下做，而采样从左下往右上处理，因此 y 的取值调整为 1 - y
			_ScrollMain2.zw = float2(floor(speed) * _ScrollMain2.x, 1 - floor(speed * _ScrollMain2.x) * _ScrollMain2.y);
			o.uvMain.zw = uvCoord * _ScrollMain2.xy + _ScrollMain2.zw;//二次UV变换
		#else
			o.uvMain.xy = UVOffset(uvCoord, MAINUVST);
		#endif
		#if USE_MASK || USE_NORMMAP
		o.uvMask.xy = UVOffset(uvCoord, MASKUVST);
		#endif

	#endif

	#if USE_THIRDTEX
	
		o.uvMain.zw = uvCoord * _Msk2Tex_ST.xy + _Msk2Tex_ST.zw;

	#endif

	#if USE_SECONDTEX

		half useVA = _VertColorAlphaIsLifeTime;
		half dropVA = 1 - _PreserveVertAlpha;

		half2 str = half2(_Strength, o.color.a);

		//使用顶点色强度时，_Strength由顶点色替代， 不保留alpha的情况下顶点色透明强制为1
		str = lerp(str, half2(str.y,  1) ,half2(useVA, dropVA));

		o.uvMask.z = str.x;
		o.color.a = lerp(o.color.a, str.y, useVA);

	#endif

	#if USE_VIEWDIR
	half3 viewDir = _WorldSpaceCameraPos.xyz - worldPos.xyz;
	#endif

    //启用边缘光
	#ifdef _RIM_ON

	    half3 normal = UnityObjectToWorldNormal(v.normal);

		#if USE_TNORMAL
		half3 tangent = UnityObjectToWorldDir(v.tangent.xyz);
		half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
		half3 binormal = cross(normal, tangent) * tangentSign;
		o.tangent = tangent;
		o.binormal = binormal;
		#endif

	    o.normal = normal;
	    o.viewDir = NormalizePerVertexNormal(viewDir);

	#endif

	#if USE_SCENE_MAP

		o.worldPos = worldPos.xyz;
		GET_SCENE_PARAM(o)
		TRANSFER_SCATTER(o, -viewDir, worldPos.y)

	#endif

	//UNITY_TRANSFER_FOG(o, o.vertex);
	return o;
}

half4 frag(v2f i) : SV_Target 
{
    //背景扭曲，主纹理用作扭曲通道
	#if defined(EF_DISTORTION)

        half4 bumpMap = tex2D(_MainTex, i.uvMain.xy);
    	half3 bump = UnpackNormal(bumpMap);

        half4 projPos = UNITY_PROJ_COORD(i.projPos);
    	projPos.xy += bump.xy * _BumpStrength;
        
    	half4 tex = tex2Dproj(_GrabTexture, projPos);
    	tex.a *= bumpMap.a;
    #elif defined(_WORLDUV_ON)
    	float2 sceneUVs = (i.projPos.xy / i.projPos.w);
    	float4 tex = tex2D(_MainTex,TRANSFORM_TEX((sceneUVs * 2 - 1).rg, _MainTex));
	#else

		#ifdef EF_PROJECTOR

			half2 projWXY = i.projPos.xy / i.projPos.w;
			int2 clipS = step(1.0e-3, projWXY) * step(projWXY, 1.0);
			int cl = clipS.x * clipS.y;
			
		#endif

			//纹理扭曲模式
		#if defined(_SECONDTYPE_DISTORT)

			half4 bumpMap = tex2D(_MaskTex, i.uvMask.xy);
			half3 bumpMask = UnpackNormal(bumpMap);
			i.uvMain.xy += bumpMask.xy * bumpMap.a * i.uvMask.z;

		#elif defined(_SECONDTYPE_FLOWMAP)

			half4 bumpMap = tex2D(_MaskTex, i.uvMask.xy);
			half2 dir = UnpackNormal(bumpMap).xy * i.uvMask.z;

			float2 phase = fmod(_Time.yy * 0.5 + float2(0.5, 1.0), 1);
			float flowLerp = abs((0.5 - phase.x) / 0.5);

		#endif

		#if defined(_SECONDTYPE_FLOWMAP)

			half4 tex0 = tex2D(_MainTex, i.uvMain.xy - dir * phase.x);
			half4 tex1 = tex2D(_MainTex, i.uvMain.xy - dir * phase.y);
			half4 tex = lerp(tex0, tex1, flowLerp);
			tex.a *= bumpMap.a;

		#elif defined(_SCROLLTYPE_MULTIBLEND)

			half4 tex = MultiBlend(_MainTex, i.uvMain.xy, _ScrollMain, _ScrollMain2, _MainTex_ST);
			#elif defined(_SCROLLTYPE_RANDOM)

				//msk *= (tex2D(_MainTex, i.uvMain.xy) + tex2D(_MainTex, i.uvMain.zw));
			    half4 tex =  (tex2D(_MainTex, i.uvMain.xy) +tex2D(_MainTex, i.uvMain.zw));
			#else
			half4 tex = tex2D(_MainTex, i.uvMain.xy);

		#endif

		#ifdef EF_PROJECTOR

			tex *= cl;

		#endif

		#ifndef EF_UI

			tex *= _MainColor;

    	#endif

	#endif
 
	#if USE_NONORMMAP

        #if USE_GRAD

            ColorGrad(/* inout */tex.rgb, i.uvMask.z);

        #else

			half4 msk = _MaskColor;
			
            #if defined(_SCROLLTYPE_MULTIBLEND)

				msk *= MultiBlend(_MaskTex, i.uvMask.xy, _ScrollMask, _ScrollMask2, _MaskTex_ST);

            
			
			#else
				msk *= tex2D(_MaskTex, i.uvMask.xy);

            #endif

            #ifdef EF_PROJECTOR

				msk *= cl;

            #endif

            TextureBlend(/* inout */tex, msk, i.uvMask.z);

        #endif
	
	#endif

	//扭曲模式启用消散，其余模式使用alphamask
	#if USE_THIRDTEX

		ThirdBlend(tex, tex2D(_Msk2Tex, i.uvMain.zw), _Msk2Strength);
	
	#endif

	#if USE_SECONDCOLOR
			//去色
			half desaturate = dot(tex.rgb,float3(0.3,0.59,0.11));
			half lerpParam = saturate(saturate(_ColorB.a - tex.a) / (_ColorB.a - _ColorA.a));
		#if USE_BLACKLESS
			lerpParam = saturate(saturate(_ColorB.a - desaturate) / (_ColorB.a - _ColorA.a));
			tex.a = desaturate;
		#endif
		half3 colorR = lerp(_ColorA.rgb,_ColorB.rgb,lerpParam);
		tex.rgb = colorR * _Multiple * tex.a;
	#endif

	half4 col = i.color * tex;

	#ifdef _PRE_ON
	col *= i.color.a;
	#endif
    //边缘光
	#ifdef _RIM_ON

		#if USE_TNORMAL
		half3 normal = normalize(i.tangent * bump.x + i.binormal * bump.y + i.normal * (1 - dot(bump.xy, bump.xy)));
		#else
		half3 normal = normalize(i.normal);
		#endif

		AddRim(col, normal, NormalizePerPixelNormal(i.viewDir));
    
	#endif

	#ifndef EF_UI
		col *= _TintColor * 2.0;
	#endif

	#if USE_SCENE_MAP

		half fogFactor = step(_Mode, 2.5);
		APPLY_SCENE_FOG(col.rgb, i, fogFactor.xxx)

	#endif

	//UNITY_APPLY_FOG_COLOR(i.fogCoord, col, (1 - step(2.5, _Mode)) * unity_FogColor);
	#if USE_HDR
	return col;
	#else
	return saturate(col);
	#endif
	
}
	
#endif