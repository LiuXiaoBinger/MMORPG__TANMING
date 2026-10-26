// RODungeon_First 专用最简贴图着色器（只被本目录 10 个材质引用）
//
// 为什么需要它：
//   1) 场景复制自 RO 客户端，原材质用 RO/Scene/* 着色器，依赖 RO 全局参数与烘焙光。
//      迁移时烘焙光没有随场景带过来（lightmaps 为空），直接用原着色器会全黑。
//   2) Unity 内置里没有同时满足「贴图 + _Color 乘法 + 不依赖灯光」的着色器：
//      Unlit/Texture 只有 _MainTex；Unlit/Color 只有 _Color；
//      Legacy Shaders/Diffuse、Mobile/Diffuse 都需要实时光源，而本场景 0 盏灯。
//   3) Shader.Find("Standard") 在本工程会命中
//      Assets/artres/Resources/Shader/RO/Replace/Standard.shader 这个同名替身，
//      它把顶点色乘进 albedo；而 RO 网格顶点色是遗留数据通道（含洋红标记与全黑遮罩），
//      会渲染出异常颜色，所以不能直接用。
//
// 实现：只有「贴图采样 × _Color」，不采样顶点色、不做法线调色、不做光照计算。
// 按 RO 原场景着色器（ROScene/ROTerrain）的原意，顶点色不参与着色。
// 文件名沿用第 3 轮引入时的命名，避免 meta guid 变动导致 10 个材质引用断裂。

Shader "LYF/Dungeon/ROVertexColorSafe"
{
	Properties
	{
		// 颜色乘数：沿用 RO 材质原来的 _Diffuse 0.5，保持迁移前的明暗语义
		_Color ("Color", Color) = (1, 1, 1, 1)
		// 主贴图：沿用 RO 材质原本的 albedo 贴图，UV 与平铺参数保持不变
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		// 不透明几何，走默认不透明队列
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// 顶点输入：只声明位置与第一套 UV。
			// 刻意不声明 COLOR / NORMAL，避免把 RO 遗留的顶点色与法线数据带进着色。
			struct appdata
			{
				float4 vertex : POSITION;   // 模型空间顶点位置
				float2 uv : TEXCOORD0;      // 第一套 UV，用于采样主贴图
			};

			// 顶点到片元传递的数据
			struct v2f
			{
				float4 vertex : SV_POSITION; // 裁剪空间位置
				float2 uv : TEXCOORD0;       // 已套用平铺/偏移的 UV
			};

			sampler2D _MainTex;      // 主贴图采样器
			float4 _MainTex_ST;      // 主贴图的平铺(tiling)与偏移(offset)
			fixed4 _Color;           // 颜色乘数

			// 顶点着色器：模型空间转裁剪空间，并套用贴图平铺偏移
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			// 片元着色器：贴图颜色 × 颜色乘数，直接输出
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				return col;
			}
			ENDCG
		}
	}
}
