// 编辑器导航预览共用的着色器：给导航面和边线着色，默认按场景深度决定是否可见。
// 只有遮挡点贴近 Terrain、且地表高出导航面不超过允许值时，才把该像素的显示深度稍微提前。
Shader "Hidden/LYF/NavigationPreview"
{
    Properties
    {
        // 蓝面、内部边和外轮廓由三个临时材质分别设置颜色与透明度。
        _Color ("预览颜色", Color) = (0, 0.76, 1, 0.5)
        // 预览器让蓝面写深度，遮住后面的导航层；边线关闭写入，但仍参加深度测试。
        _ZWrite ("写入深度", Float) = 1
        // 沿相机视空间 Z 轴靠近相机的距离；预览器给边线 0.02 米、蓝面 0，减少面与线重叠闪烁。
        _ViewOffset ("观察方向偏移（米）", Float) = 0
    }
    SubShader
    {
        Pass
        {
            // 按源透明度混合颜色，按材质决定是否写深度；双面显示让导航面从上下都能看到。
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            // 当前预览层的颜色和透明度。
            fixed4 _Color;
            // 顶点投影前增加的视空间 Z，单位米；只影响显示位置，不修改原 Mesh。
            float _ViewOffset;
            // 预览器已提供 Terrain 高度图时为 1；小于 0.5 时直接使用普通深度。
            float _TerrainEnabled;
            // Terrain 高度快照，每个像素的 R 分量直接保存世界 Y 坐标。
            sampler2D _TerrainHeights;
            // Terrain 的世界原点，使用 X、Z 计算遮挡点在高度图中的位置。
            float4 _TerrainOrigin;
            // xyz 是 Terrain 的世界尺寸；w 是方形高度图每边的像素数，用于对齐像素中心。
            float4 _TerrainSize;
            // 允许地表高出导航面的最大高度差，单位米；预览器当前传入 1.65，超过时仍被遮挡。
            float _TerrainTolerance;
            // 相机生成的场景深度纹理，用来判断当前像素先遇到地表还是其他遮挡物。
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // 顶点阶段输入：从预览 Mesh 读取位置，原 Mesh 数据保持不变。
            struct VertexInput
            {
                // 输入顶点坐标；通过当前物体矩阵换算到世界空间和相机空间。
                float4 vertex : POSITION;
            };
            // 顶点阶段输出：除了投影位置，还传递原导航位置，供每个像素判断地表遮挡。
            struct VertexOutput
            {
                // 顶点阶段是裁剪空间位置；进入片元阶段后 z 是平台使用的屏幕深度。
                float4 vertex : SV_POSITION;
                // ComputeScreenPos 得到的投影坐标，采样深度纹理时会按 w 做透视除法。
                float4 screen : TEXCOORD0;
                // 原始导航面的世界位置，不包含细线显示偏移。
                float3 world : TEXCOORD1;
                // 原始导航面的视空间位置，用于沿当前像素射线重建遮挡点。
                float3 view : TEXCOORD2;
            };
            // 每个像素同时输出颜色和深度；最终仍按 Pass 的 LEqual 深度测试决定是否绘制。
            struct FragmentOutput
            {
                // 当前面或线的透明颜色。
                fixed4 color : SV_Target;
                // 普通导航深度，或满足 Terrain 条件后稍微提前的深度。
                float depth : SV_Depth;
            };

            // 把 input 顶点转换到相机空间，应用显示偏移后投影；原世界位置和视空间位置另行保留。
            VertexOutput Vert(VertexInput input)
            {
                // 要传给片元阶段的数据，位置与辅助坐标会由光栅化过程插值。
                VertexOutput output;
                output.world = mul(unity_ObjectToWorld, input.vertex).xyz;
                // 相机前方对应负 Z；先保存未偏移位置，再增加 Z，使边线投影稍靠近相机。
                float4 viewPosition = mul(UNITY_MATRIX_MV, input.vertex);
                output.view = viewPosition.xyz;
                viewPosition.z += _ViewOffset;
                output.vertex = mul(UNITY_MATRIX_P, viewPosition);
                output.screen = ComputeScreenPos(output.vertex);
                return output;
            }

            // 用 input 中插值后的导航位置查场景深度；只有被符合高度条件的 Terrain 遮挡时才调整输出深度。
            FragmentOutput Frag(VertexOutput input)
            {
                // 默认返回导航本身的颜色和深度；下面任何条件不满足就保留这个结果。
                FragmentOutput output;
                output.color = _Color;
                output.depth = input.vertex.z;
                if (_TerrainEnabled < 0.5)
                {
                    return output;
                }
                // 当前像素的原始场景深度，取值遵循图形平台规则，这个值本身不是米。
                float sceneRawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screen));
                // 把原始深度换成沿相机前向的距离，单位为世界单位，不是到相机的三维直线距离。
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth);
                // 未应用边线偏移的导航位置到相机的前向距离；导航已在遮挡点之前时无需修正。
                float navigationEyeDepth = -input.view.z;
                if (navigationEyeDepth <= sceneEyeDepth)
                {
                    return output;
                }
                // 用场景深度重建遮挡点：透视相机按前向距离比缩放 xy，正交相机保留 xy。
                float3 sceneViewPosition = input.view;
                if (unity_OrthoParams.w < 0.5)
                {
                    sceneViewPosition.xy *= sceneEyeDepth / navigationEyeDepth;
                }
                sceneViewPosition.z = -sceneEyeDepth;
                // 把重建的遮挡点从相机空间转回世界空间，才能与 Terrain 世界高度比较。
                float3 sceneWorldPosition = mul(UNITY_MATRIX_I_V, float4(sceneViewPosition, 1)).xyz;
                // 遮挡点在当前Terrain中的归一化XZ位置。
                float2 terrainUV = (sceneWorldPosition.xz - _TerrainOrigin.xz) / _TerrainSize.xz;
                if (any(terrainUV < 0) || any(terrainUV > 1))
                {
                    return output;
                }
                // 让 Terrain 两端的采样点落到高度图首尾像素中心，避免边缘采样错开半个像素。
                float2 textureUV = (terrainUV * (_TerrainSize.w - 1) + 0.5) / _TerrainSize.w;
                // 当前遮挡点XZ对应的Terrain世界高度。
                float terrainHeight = tex2D(_TerrainHeights, textureUV).r;
                // 遮挡点与 Terrain 高度相差超过 0.08 米时，不把它当作地表，因此不调整深度。
                if (abs(sceneWorldPosition.y - terrainHeight) > 0.08)
                {
                    return output;
                }
                // 遮挡点比原导航面高出的世界 Y 差；只处理地表在上方且差值不超过允许上限的情况。
                float terrainGap = sceneWorldPosition.y - input.world.y;
                if (terrainGap < 0 || terrainGap > _TerrainTolerance)
                {
                    return output;
                }
                // 通过全部条件后，让输出深度比地表更靠近相机一点；反向 Z 使用加法，普通 Z 使用减法。
                #if defined(UNITY_REVERSED_Z)
                output.depth = sceneRawDepth + 0.000001;
                #else
                output.depth = sceneRawDepth - 0.000001;
                #endif
                return output;
            }
            ENDCG
        }
    }
}
