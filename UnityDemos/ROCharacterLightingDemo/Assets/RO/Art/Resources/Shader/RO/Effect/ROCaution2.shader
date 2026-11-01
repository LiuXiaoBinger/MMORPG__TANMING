Shader "RO/Caution/Caution2"
{
	Properties
	{
		[KeywordEnum(Rect,Round,Fan,Round2)]_Shape("模式?", float) = 0
		[MaterialToggle]_BaseToggle("开启底色?", float) = 0
		_BaseColor("底色色调",Color) = (1,1,1,1)
		_BaseTex("底色", 2D) = "black"{}
		_Decal("纹路", 2D) = "white"{}
		_Color("色调", Color) = (0,0.8,1,0.25)
		_Border("边缘色", Color) = (0,1,1,1)
		_Params("(外半径|前长, 内半径|后长, 夹角|宽, 边宽)",vector) = (2,0,1,0.02)
		[Space]
		[KeywordEnum(None,Manual,Auto)]_Flow("流光?", float) = 0
		_CauColor("警示区颜色", color) = (1,1,0,0)
		_Progress("进度 (手动模式)", range(0, 1)) = 0
		_Progress2("进度范围", range(0, 1)) = 0
		_ParamsFlow("(流光速度, 流光锐度, 流光层数, 流光强度)", vector) = (0.5, 5, 1, 1)
		[Space]
		_Opacity("起点消隐", range(0,1)) = 1
		[MaterialToggle]_Blinn("闪烁?", float) = 0
		_ParamsEx("(纹理幅宽, 旋转, 闪烁速度, 闪烁强度)",vector) = (1, 0, 4, 0.5)
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("混合方式(1=add,10=blend)",float) = 10

        [Space]
        _CenterOffset("中心偏移",Range(0,1))=0.5
        _FadeCo("_FadeCo",Float)=2.0

        [Space]
        _LimitFadeRange("alpha 约束",Range(0,0.5)) = 0
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent+300" }
		LOD 100
		Blend SrcAlpha [_DstBlend]
		ZWrite Off ZTest Always Cull Off Lighting Off
		Offset -1, -1

		Stencil
		{
			Ref 25//与地面40 和 非地面遮挡30比较，若非地面开启遮挡则为30，判断不通过，未开启时，Ref为0肯定通过
			Comp LEqual
		}

		Pass
		{	
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _SHAPE_RECT _SHAPE_ROUND _SHAPE_FAN _SHAPE_ROUND2
			#pragma shader_feature __ _BLINN_ON
			#pragma shader_feature __ _BASETOGGLE_ON
			#pragma shader_feature __ _FLOW_MANUAL _FLOW_AUTO
            #define _Caution2
			#include "../include/CautionCommon.cginc"
			
			ENDCG
		}
	}
}
