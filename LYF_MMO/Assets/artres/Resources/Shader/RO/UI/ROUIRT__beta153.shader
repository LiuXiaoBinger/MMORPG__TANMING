Shader "RO/UI/Render Texture"
{
	Properties
	{
		[PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
		_OutlineTex("Outline Texture", 2D) = "black" {}
		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", float) = 5
		[MaterialToggle]_Outline("Enable Outline?", float) = 0
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend [_SrcBlend] [_DstBlend]
		//Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			//#pragma multi_compile __ OUTLINE_HALF OUTLINE_RG OUTLINE_RGBA
			//#pragma shader_feature __ _OUTLINE_ON
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#define USE_OUTLINE ((defined(OUTLINE_HALF) || defined(OUTLINE_RG) || defined(OUTLINE_RGBA)) && _OUTLINE_ON)

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};

			fixed4 _BlendColor;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1,1) * OUT.vertex.w;
				#endif

				OUT.color = IN.color;
				return OUT;
			}

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;

			#ifdef _OUTLINE_ON

			#if defined(OUTLINE_RGBA)
			#define DCLTEX sampler2D
			#define DECTEX(t) DecodeFloatRGBA(t)
			#elif defined(OUTLINE_RG)
			#define DCLTEX sampler2D
			#define DECTEX(t) DecodeFloatRG(t)
			#else
			#define DCLTEX sampler2D_half
			#define DECTEX(t) t
			#endif

			#define S smoothstep

			DCLTEX _OutlineTex;
			half4 _OutlineTex_TexelSize;

			half3 outline(half2 uv, half3 c)
			{
				half ch = c.r + c.g + c.b;
				half2 off = _OutlineTex_TexelSize.xy * 0.35;

				//robert
				half4 fd;
				fd.x = DECTEX(tex2D(_OutlineTex, uv - off));//x-0.5 y-0.5
				fd.y = DECTEX(tex2D(_OutlineTex, uv + off));//x+0.5 y+0.5
				fd.z = DECTEX(tex2D(_OutlineTex, uv + half2(-off.x, off.y)));//x-0.5 y+0.5
				fd.w = DECTEX(tex2D(_OutlineTex, uv + half2(off.x, -off.y)));//x+0.5 y-0.5

				half d = fd.x + fd.y;

				half gd = abs(fd.x - fd.y) + abs(fd.z - fd.w);
				gd = exp2(-gd * 200);
				gd = S(0.15, 0.75, gd);
				gd = ch > 2.85 ? 1 : gd;

				half3 oc = saturate(c - (1 - gd) * 0.33);
				return oc;
			}

			#endif

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, IN.texcoord); 
				#if USE_OUTLINE
					color.rgb = outline(IN.texcoord, color.rgb);
				#endif

				color += _TextureSampleAdd;

				#ifdef UNITY_UI_CLIP_RECT
				IN.color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				color.rgb *= IN.color.a;
				color.a = lerp(1, color.a, IN.color.a);
				
				return color;
			}

			ENDCG
		}
	}
}
