Shader "RO/UI/TextOutLine"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "OUTLINE"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
			#include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_TexelSize;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float4 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
				fixed4 color : COLOR;
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 tangent : TANGENT;
				float4 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
                fixed4 color : COLOR;
            };

            float4 _ClipRect;

            v2f vert(appdata IN)
            {
                v2f o;

                o.worldPosition = IN.vertex;
                o.vertex = UnityObjectToClipPos(IN.vertex);
                o.tangent = IN.tangent;
                o.texcoord = IN.texcoord;
                o.color = IN.color * _Color;
				o.uv1 = IN.uv1;
				o.uv2 = IN.uv2;
				o.uv3 = IN.uv3;
				o.normal = IN.normal;

                return o;
            }

			fixed IsInRect(float2 pPos, float2 pClipRectMin, float2 pClipRectMax)
			{
				pPos = step(pClipRectMin, pPos) * step(pPos, pClipRectMax);
				return pPos.x * pPos.y;
			}

			fixed SampleAlpha(int pIndex, v2f IN)
			{
                const fixed sinArray[8] = { 0, 0.707, 1, 0.707, 0, -0.707, -1, -0.707};
                const fixed cosArray[8] = { 1, 0.707, 0, -0.707, -1, -0.707, 0, 0.707};
				float2 pos = IN.texcoord + _MainTex_TexelSize.xy * float2(cosArray[pIndex], sinArray[pIndex]) * IN.normal.z;
				return IsInRect(pos, IN.uv1, IN.uv2) * (tex2D(_MainTex, pos) + _TextureSampleAdd).w * IN.tangent.w;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
                //normal.z == 描边的宽度
                //uv3 传参描边的R和G
                //tangent 传参描边的 0 0 B A
				fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				if (IN.normal.z > 0)
				{
					color.w *= IsInRect(IN.texcoord, IN.uv1, IN.uv2);
					half4 val = half4(IN.uv3.x, IN.uv3.y, IN.tangent.z, 0);
					val.w += SampleAlpha(0, IN);
					val.w += SampleAlpha(1, IN);
					val.w += SampleAlpha(2, IN);
					val.w += SampleAlpha(3, IN);
					val.w += SampleAlpha(4, IN);
					val.w += SampleAlpha(5, IN);
					val.w += SampleAlpha(6, IN);
					val.w += SampleAlpha(7, IN);
					color = (val * (1.0 - color.a)) + (color * color.a);

                    color.a *= pow(IN.color.a,6);	//字逐渐隐藏时，描边也要隐藏
				}

                //UIMask 裁减
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
				return color;
			}

            ENDCG
        }
    }
}