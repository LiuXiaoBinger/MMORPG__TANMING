Shader "RO/UI/Empty"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_StencilComp("Stencil Comparison", Float) = 8
		//https://answers.unity.com/questions/988627/shader-error-material-doesnt-have-stencil-properti.html
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 0
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

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct v2f
			{
				float4 vertex   : SV_POSITION;
			};

			v2f vert(float3 pos : POSITION)
			{
				v2f OUT;
				OUT.vertex = float4(-2,-2,0,1);
				return OUT;
			}
			half4 frag() : SV_TARGET
			{
				return half4(0,0,0,0);
			}
			ENDCG
		}
	}
}
