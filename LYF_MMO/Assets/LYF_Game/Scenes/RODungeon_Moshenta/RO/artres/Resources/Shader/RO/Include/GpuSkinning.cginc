#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

#include "UnityCG.cginc"

#define USE_MATRIXARRAY (defined(_GPUANIM_MATRIXARRAY))
#define USE_ANIMMAP (defined(_GPUANIM_BONEMAP) || defined(_GPUANIM_VERTEXMAP))
#define USE_BONEMATRIXMAP (defined(_GPUANIM_BONEMAP) && !defined(SHADER_API_D3D11_9X))
#define USE_VERTEXPOSMAP (defined(_GPUANIM_VERTEXMAP) && !defined(SHADER_API_D3D11_9X))
#define USE_WIND (defined(_GPUANIM_WIND))

#define GPUSKIN_ON (USE_MATRIXARRAY || USE_BONEMATRIXMAP || USE_VERTEXPOSMAP || USE_WIND)

#if USE_ANIMMAP

sampler2D _AnimTex;
float4 _AnimTex_TexelSize;
float4 _AnimTex_HDR;
float4 _Offset;//x= pixel offset, y = frame, z = bones, w = fps
UNITY_INSTANCING_BUFFER_START(props)
UNITY_DEFINE_INSTANCED_PROP(float, _FrameShift)
UNITY_INSTANCING_BUFFER_END(props)

#endif

#if USE_MATRIXARRAY

	float4 _BoneMatrix[180];

	#define VERTEXANIM(v) SkinningVertex(v.vertex, v.normal, v.color);

	void SkinningVertex(inout float4 vertex, inout float3 normal, float4 boneWeight)
	{
		int2 index = boneWeight.xy * 3;
		float4x4 B2W1 = float4x4(_BoneMatrix[index.x], _BoneMatrix[index.x + 1], _BoneMatrix[index.x + 2], float4(0, 0, 0, 1));
		float4x4 B2W2 = float4x4(_BoneMatrix[index.y], _BoneMatrix[index.y + 1], _BoneMatrix[index.y + 2], float4(0, 0, 0, 1));

		vertex.xyz = mul(B2W1, vertex).xyz * boneWeight.z + mul(B2W2, vertex).xyz * boneWeight.w;
		normal.xyz = mul(B2W1, normal).xyz * boneWeight.z + mul(B2W2, normal).xyz * boneWeight.w;
	}

#elif USE_BONEMATRIXMAP

	#define VERTEXANIM(v) SkinningVertex(v.vertex, v.normal, v.color);

	void SkinningVertex(inout float4 vertex, inout float3 normal, float4 boneWeight)
	{
		int2 index = boneWeight.xy * 3;
		float2 texelSize = _AnimTex_TexelSize.xy;
		fixed frameStart = _Offset.x;
		fixed frameCount = _Offset.y;
		fixed boneMatrixSize = _Offset.z;
		float timeFactor = _Time.y * _Offset.w;

		float curFrame = round(fmod(timeFactor, (frameCount - 1)));//时间偏移
		curFrame = fmod(curFrame + UNITY_ACCESS_INSTANCED_PROP(props, _FrameShift), frameCount);//限制在帧范围内

		float3 boneOff1 = int3(0, 1, 2) + index.xxx;
		float3 boneOff2 = int3(0, 1, 2) + index.yyy;
		float3 frameOff1X = frameStart + boneMatrixSize * curFrame + boneOff1;
		float3 frameOff2X = frameStart + boneMatrixSize * curFrame + boneOff2;
		float3 frameOff1Y;
		float3 frameOff2Y;

		frameOff1X = modf(frameOff1X * texelSize.x, frameOff1Y) + 0.5 * texelSize.x;
		frameOff2X = modf(frameOff2X * texelSize.x, frameOff2Y) + 0.5 * texelSize.x;
		frameOff1Y = (frameOff1Y + 0.5) * texelSize.y;
		frameOff2Y = (frameOff2Y + 0.5) * texelSize.y;

		float4 mat10 = tex2Dlod(_AnimTex, float4(frameOff1X.x, frameOff1Y.x, 0, 0));
		float4 mat11 = tex2Dlod(_AnimTex, float4(frameOff1X.y, frameOff1Y.y, 0, 0));
		float4 mat12 = tex2Dlod(_AnimTex, float4(frameOff1X.z, frameOff1Y.z, 0, 0));
		float4 mat20 = tex2Dlod(_AnimTex, float4(frameOff2X.x, frameOff2Y.x, 0, 0));
		float4 mat21 = tex2Dlod(_AnimTex, float4(frameOff2X.y, frameOff2Y.y, 0, 0));
		float4 mat22 = tex2Dlod(_AnimTex, float4(frameOff2X.z, frameOff2Y.z, 0, 0));

		float4x4 B2W1 = float4x4(mat10, mat11, mat12, float4(0, 0, 0, 1));
		float4x4 B2W2 = float4x4(mat20, mat21, mat22, float4(0, 0, 0, 1));

		vertex.xyz = mul(B2W1, vertex).xyz * boneWeight.z + mul(B2W2, vertex).xyz * boneWeight.w;
		normal.xyz = mul(B2W1, normal).xyz * boneWeight.z + mul(B2W2, normal).xyz * boneWeight.w;
	}

#elif USE_VERTEXPOSMAP

	#define VERTEXANIM(v) SkinningVertex(v.vertex, vid);

	void SkinningVertex(inout float4 vertex, uint vid)
	{
		fixed vertexCount = _Offset.x;
		fixed frameCount = _Offset.y;
		float timeFactor = _Time.y * _Offset.w;

		float curFrame = round(fmod(timeFactor, (frameCount - 1)));//时间偏移
		curFrame = fmod(curFrame + UNITY_ACCESS_INSTANCED_PROP(props, _FrameShift), frameCount);

		float2 texelSize = _AnimTex_TexelSize.xy;
		float xOff = curFrame * vertexCount + vid;
		float yOff;
		xOff = modf(xOff * texelSize.x, yOff) + 0.5 * texelSize.x;
		yOff = (yOff + 0.5) * texelSize.y;
		float3 vertexMap = tex2Dlod(_AnimTex, float4(xOff, yOff, 0, 0));
		vertex.xyz = vertexMap;
	}

#elif USE_WIND

	half _WaveFactor;
	half4 _ROCWindParams;

	#define VERTEXANIM(v) SkinningVertex(v.vertex, 1 - v.color.a);

	void SkinningVertex(inout float4 vertex, half intensity) {

		half strength = _WaveFactor * intensity;
		half2 timeFactor = fmod(_Time.xy, UNITY_PI * 2);

		half4 wind = half4(_ROCWindParams.xyz, 1);
		wind.xyz += sin(timeFactor.xyx + vertex.xyz) * half3(0.05, 0.08, 0.05);
		wind = mul(wind, unity_ObjectToWorld);

		half3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
		half2 frequence = half2(timeFactor.x, timeFactor.x * 1.618) * _ROCWindParams.ww;
		timeFactor = sin(fmod(frequence, 2 * UNITY_PI) + worldPos.xz) * 0.5 + 0.3;

		//half len = length(vertex.xyz);
		vertex.xyz += (wind * (timeFactor.x + timeFactor.y)) * strength;
		//vertex.xyz = normalize(vertex.xyz) * len;
	}

#endif

#endif