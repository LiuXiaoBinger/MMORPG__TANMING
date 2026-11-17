#ifndef SCENEMAP_INCLUDE
#define SCENEMAP_INCLUDE

#include "UnityCG.cginc"

//================================================================================================
//
// 定义与变量
//
//================================================================================================

#if (defined(SCENE_MAP_ON) || defined(SC_GRASS))//2019.07.02增加草自适应场景阴影功能

	CBUFFER_START(ROCommonEnvoriment)

	half4 _ROCHeightFogParams;//x = 海拔雾高度倒数, y = 海拔雾浓度, z = 散射影响海拔雾, w = (1 = 瑞利抵消 0 = 瑞利不抵消)
	half4 _ROCRadiusFogParams;//x = 瑞利雾起点  y = 米式雾系数, z = 瑞利雾浓度, w = 米式雾浓度
	half4 _ROCMieG;// x =  (1 - G2) / (2 + G2), y = 1 + G2, z = G * 2
	half _ROCWorldParams;//世界单位转为纹理单位

	half4 _ROCMaskColor;//rgb = 云层颜色/环境压暗颜色 a = 雨雪氛围强度

	half4 _ROCShadowColor;
	float3 _ROCLightDir1;
	half3 _ROCSceneColor;

	#ifdef SCENE_MAP_MIE_SCATTER
	half4 _ROCSunColor;
	#endif

	#ifdef SCENE_MAP_RAIN
	half4 _ROCLightningColor;
	float3 _ROCLightningPos;
	#endif

	half4 _ROCFogST;//xy 雾平铺，zw 雾偏移
	half4 _ROCCloudST;//xy 云平铺 zw 云偏移
	half4 _ROCMapFlow;//xy = 雾运动 zw = 云运动

	half4 _ROCFogStartColor; //rgb = 雾色, w = 混合起始距离
	half4 _ROCFogEndColor; //rgb = 雾色, w = 混合结束距离
	#ifndef SHADER_API_MOBILE
	half4 _ROCFogDebug;
	#endif

	CBUFFER_END

	sampler2D _ROCSceneMap;//r = 雾强度 g = 阴影区 b = 云层阴影强度

	#if defined(SCENE_MAP_RAIN) || defined(SCENE_MAP_SNOW)
	sampler2D _ROCWeatherMap;
	#endif

	//#define KR 0.0596831 // 3 / (16 * pi)
	#define KR 0.7161972 // 12 * 3 / (16 * pi)
	#define KM 0.1193662 // 3 / (8 * pi)
	#define WATERSPECULAR 0.255
	#define WATERDIFFUSE 0.745
	#define SNOWDIFFUSE 0.81
	#define SNOWSPECULAR 0.19

	#define SCENE_PARAM_COORDS(idx1, idx2)																	\
		float4 sceneMapCoord : TEXCOORD##idx1;																\
		half4 scatter : TEXCOORD##idx2;

	#define TRANSFER_SCATTER(vi, ray, height)																\
		vi.scatter = Scatter(ray, SafeDir(_ROCLightDir1), height);

	#define APPLY_SCENE_FOG(col, vi, selfFactor)															\
		half4 scatter = half4(vi.scatter.xyz * selfFactor, vi.scatter.w);									\
		scatter.xy *= 1 - vi.sceneMapCoord.x;																\
		ApplySceneFog(col, scatter.x, scatter.y, scatter.z, scatter.w);

	//应用环境纹理，雾流动和云流动

	#define GET_SCENE_PARAM(vi)																				\
		float2 worldUV = vi.worldPos.xz * _ROCWorldParams.xx;												\
		float4 flowUV = vi.worldPos.xzxz * float4(_ROCFogST.xy, _ROCCloudST.xy);							\
		flowUV = frac(flowUV + _Time.xxxx * _ROCMapFlow - float4(_ROCFogST.zw, 0.5 + _ROCCloudST.zw));		\
		vi.sceneMapCoord.xyz = GetSceneMap(worldUV, flowUV);

	#define GET_WATER_SCEMEMAP(vi, worldPos)																	\
		float2 worldUV = worldPos.xz * _ROCWorldParams.xx;													\
		float4 flowUV = worldPos.xzxz * float4(_ROCFogST.xy, _ROCCloudST.xy);								\
		flowUV = frac(flowUV + _Time.xxxx * _ROCMapFlow - float4(_ROCFogST.zw, 0.5 + _ROCCloudST.zw));		\
		vi.sceneMapCoord.xyz = GetSceneMap(worldUV, flowUV);

	#ifdef SCENE_MAP_MIE_SCATTER

		#define SHADOWMIX(shadow, vi) shadow = max(shadow, vi.sceneMapCoord.y); 

	#else

		#define SHADOWMIX(shadow, vi)

	#endif

	//1为环境不压暗， 0为环境压暗
	#define RAIN_SNOW_CLOUD_STRENGTH 0.67 + min(0.33, abs(_ROCMaskColor.a - 0.33));

	//0 = 0,0  0.33 = 1,0,  0.67 = 0,1,  0.99 = 0,0
	#define RAIN_SNOW_STRENGTH 1 - min(1.0, abs(_ROCMaskColor.aa * 3.0 - half2(1.0, 2.0))) 

//================================================================================================
//
// 散射相关
//
//================================================================================================

	half henyeyGreenstein(half cosTheta, half onePlusCosThetaSqr) {
		
		//hg = (1 - g2) * pow(1 + g2 + 2*g*cosTheta, -1.5)
		//r = onePlusCosThetaSqr / (2 + g2)
		//_ROCMieG x = (1 - g2) / (2 + g2) y = 1 + g2 z = g * 2
		return onePlusCosThetaSqr * _ROCMieG.x * pow(_ROCMieG.y - _ROCMieG.z * cosTheta, -1.5);
	}

	half miePhase(half cosTheta, half onePlusCosThetaSqr) {
		return KM * henyeyGreenstein(cosTheta, onePlusCosThetaSqr);
	}

	inline half rayleighPhase(half onePlusCosThetaSqr) {
		return min(1, KR * onePlusCosThetaSqr);
	}

	half4 Scatter(float3 eyeRay, half3 lightDir, half height) {

		float eyeRayLen = length(eyeRay);
		half3 eyeDir = eyeRay / eyeRayLen;
		float pushedDensity = max(0, eyeRayLen - _ROCRadiusFogParams.x);

		float VoL = dot(eyeDir, lightDir);//cosTheta

		float onePlusCosThetaSqr = 1 + VoL * VoL;

		half rayleighPh = rayleighPhase(onePlusCosThetaSqr);

		//x = height desity, y = rayleigh density z = mie density, w = distance
		half4 scatter = half4(0, 0, 0, 0);

		scatter.x = 1 - saturate(height * _ROCHeightFogParams.x);
		scatter.x *= _ROCHeightFogParams.y;

		//启用米式散射
		#ifdef SCENE_MAP_MIE_SCATTER
	
			half miePh = miePhase(VoL, onePlusCosThetaSqr) * _ROCSunColor.a;
			scatter.yz = _ROCRadiusFogParams.zw * pushedDensity;
			scatter = 1 - exp(-scatter);//距离系数
			scatter.x *= _ROCHeightFogParams.z * (max(scatter.y, scatter.z) - 1) + 1;
			scatter.yz *= half2(rayleighPh, miePh);

		//仅启用瑞利散射
		#else

			scatter.y = _ROCRadiusFogParams.z * pushedDensity;
			scatter.xy = 1 - exp(-scatter.xy);//距离系数
			scatter.x *= _ROCHeightFogParams.z * (scatter.y - 1) + 1;
			scatter.y *= rayleighPh;
			scatter.z = 0.0;

		#endif

		//高度雾x值为0时，高度雾不产生效果
		scatter.x *= _ROCHeightFogParams.w;

		scatter.w = eyeRayLen;

		#ifndef SHADER_API_MOBILE
		scatter.xyz *= (1 - _ROCFogDebug.xyz) * (1 - _ROCFogDebug.w);		
		#endif
		
		return scatter;
	}

	inline half3 SafeDir(half3 dir)
	{
		return any(dir) != 0 ? dir : half3(0, 1, 0);
	}

	//使用环境雾影响
	void ApplySceneFog(inout half3 col, half heightScatter, half rayleighScatter, half mieScatter, half distance)
	{
		half3 startColor = _ROCFogStartColor.rgb;
		half startDistance = _ROCFogStartColor.w * _ProjectionParams.w;

		half3 endColor = _ROCFogEndColor.rgb;
		half endDistance = _ROCFogEndColor.w * _ProjectionParams.w;

		half fogFac = min(rayleighScatter + heightScatter, 1.0);
		half3 fogColor = lerp(startColor, endColor, smoothstep(startDistance, endDistance, distance * _ProjectionParams.w));

		#ifdef SCENE_MAP_MIE_SCATTER
			fogColor = lerp(fogColor, GammaToLinearSpace(_ROCSunColor.rgb), saturate(mieScatter));
		#endif

		col = lerp(col, fogColor, fogFac);
	}

//================================================================================================
//
// 天气相关
//
//================================================================================================

	inline half3 GetSceneMap(half2 worldUV, half4 flowUV)
	{
		half3 coord;
		coord.x = tex2Dlod(_ROCSceneMap, half4(flowUV.xy, 0, 2)).x;
		coord.z = tex2Dlod(_ROCSceneMap, half4(flowUV.zw, 0, 2)).z;
		coord.y = tex2Dlod(_ROCSceneMap, half4(worldUV, 0, 0)).y;

		return coord;
	}

	inline half3 GetSceneMap(half2 worldUV)
	{
		half3 coord = half3(0, 0, 0);

		#ifdef SCENE_MAP_MIE_SCATTER

			coord.y = tex2Dlod(_ROCSceneMap, half4(worldUV, 0, 0)).y;

		#endif

		return coord;
	}

	#if defined(SCENE_MAP_RAIN)

		inline half3 GetRainDistMap(half2 bumpUV)
		{
			return tex2Dlod(_ROCWeatherMap, half4(bumpUV, 0, 0)).xyz;
		}

	#elif defined(SCENE_MAP_SNOW)

		inline half GetSnowDensityMap(half2 bumpUV)
		{
			return tex2Dlod(_ROCWeatherMap, half4(bumpUV, 0, 0)).z;
		}

	#endif

	inline void ApplySceneColor(inout half3 color) 
	{
		color *= _ROCSceneColor.rgb * 2;
	}

	//mask为1时
	inline void GetSnowOrRainFactor(out half cloudMask) 
	{
		cloudMask = RAIN_SNOW_CLOUD_STRENGTH;
	}

	void GetRainSmoothness(half zoneMask, half rendererFactor, inout half smoothness)
	{
		#if defined(SCENE_MAP_RAIN)

			half2 inten = RAIN_SNOW_STRENGTH;
			inten.y *= zoneMask * rendererFactor;
			half intensity = saturate(inten.x + inten.y);
			smoothness = intensity * (1 - smoothness) + smoothness;
			
		#endif
	}

	//获得雨雪的覆盖区域和光感变换, 改变原始材质特性
	void GetSnowOrRainFactor(float3 worldPos, half zoneMask, half rendererFactor, half snowFactor,
		inout half3 diff, 
		inout half3 spec, 
		inout half3 normal, 
		inout half smoothness,
		out half cloudMask) 
	{
		//x = 全场强度[_ROCCloudColor.a 0 ~ 0.33]， y = 积雪/水强度[_ROCCloudColor.a 1 ~ 0.67]
		half2 inten = RAIN_SNOW_STRENGTH;
		inten.y *= zoneMask * rendererFactor;
		half intensity = saturate(inten.x + inten.y);

		cloudMask = inten.x * (-0.33) + 1;

		half up = saturate(normal.y * 2);

		half2 bumpUV = worldPos.xz * _ROCWorldParams;
	
		#if defined(SCENE_MAP_RAIN) && defined(SC_REFLECTION)

			smoothness = intensity * (1 - smoothness) + smoothness;

			// half4 distUV = bumpUV.xyxy * half4(18, 18, 24, 24) + _Time.yyyy * half4(-0.05, 0.015, 0.05, -0.025);

			// // 法线扰动
			// half3 d0 = GetRainDistMap(distUV.xy);
			// half3 d1 = GetRainDistMap(distUV.zw);
			// half3 rain = (d0 + d1) - 1;
			// half2 upInten = inten * up;

			// normal = normalize(lerp(normal, rain.xzy, upInten.x));

			spec = lerp(spec, WATERSPECULAR, intensity);
			//diff = lerp(diff, diff * (up - upInten.y), inten.x);

		#elif defined(SCENE_MAP_SNOW)

			half snow = GetSnowDensityMap(bumpUV * 50);

			up *= up;//p2
			up *= up;//p4

			intensity *= up;

			spec = lerp(spec, SNOWSPECULAR, intensity);
			diff = lerp(diff, SNOWDIFFUSE * snow, saturate(intensity * snowFactor));

		#endif
	}

	//使用云影色
	inline void ApplyCloudMask(inout half3 color, half cloudFactor) 
	{
		color = lerp(color, color * _ROCMaskColor.rgb, cloudFactor);
	}

	#ifdef SCENE_MAP_RAIN

		//闪电
		inline half3 GetLightningColor(float3 worldPos)
		{
			half3 lightningDir = _ROCLightningPos - worldPos;
			half dist = length(lightningDir);
			float atten = saturate(7.389 - dist * 0.02) * _ROCLightningColor.a;
			return _ROCLightningColor.rgb * atten;
		}

		//闪电
		inline half3 GetLightningColor(float3 worldPos, half3 worldNormal) 
		{
			half3 lightningDir = _ROCLightningPos - worldPos;
			half dist = length(lightningDir);
			lightningDir = lightningDir / dist;
			half NoL = max(0, dot(worldNormal, lightningDir));
			half atten = saturate(7.389 - dist * 0.02) * _ROCLightningColor.a;
			return _ROCLightningColor.rgb * NoL * atten;
		}

	#endif

	inline void ApplyShadow(inout half3 color, half atten)
	{
		half3 shadowColor = lerp(half3(1.0, 1.0, 1.0), _ROCShadowColor.rgb, _ROCShadowColor.a);
		color.rgb = lerp(color.rgb * shadowColor, color.rgb, atten);
	}

#endif
						
#endif