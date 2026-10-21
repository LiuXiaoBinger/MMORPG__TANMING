// 公共导航面与线共用的Editor预览Shader：保留障碍遮挡，仅纠正Terrain简化平面的有限深度偏差。
Shader "Hidden/LYF/NavigationPreview"
{
    Properties
    {
        // 蓝面、内部边和外轮廓由三个临时材质分别设置颜色与透明度。
        _Color ("预览颜色", Color) = (0, 0.76, 1, 0.5)
        // 蓝面写深度形成导航自身遮挡，线只读深度。
        _ZWrite ("写入深度", Float) = 1
        // 线向相机偏移两厘米消除共面冲突，蓝面为零。
        _ViewOffset ("观察方向偏移（米）", Float) = 0
    }
    SubShader
    {
        Pass
        {
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
            // 向观察相机偏移的米数，只处理共面边线。
            float _ViewOffset;
            // 当前地图存在Terrain高度快照时为1，否则只执行普通深度显示。
            float _TerrainEnabled;
            // 临时RFloat高度图，像素为Terrain世界Y，不是归一化颜色。
            sampler2D _TerrainHeights;
            // 当前Terrain的世界原点，XZ用于定位高度图。
            float4 _TerrainOrigin;
            // XYZ为Terrain世界尺寸，W为高度图边长像素数。
            float4 _TerrainSize;
            // 允许地表高于简化导航面的米数上限，主城实测后限制为1.65米。
            float _TerrainTolerance;
            // 相机生成的真实场景深度，不包含后续指令绘制的导航网格。
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // 原始文件生成的Mesh顶点，不在显示层改变其几何位置。
            struct VertexInput
            {
                // 原始世界网格顶点，经指令矩阵传入。
                float4 vertex : POSITION;
            };
            // 为片元深度判定保留原始导航位置及相机投影信息。
            struct VertexOutput
            {
                // 光栅位置，片元阶段Z为当前平台的深度值。
                float4 vertex : SV_POSITION;
                // 对应真实场景深度纹理的投影UV。
                float4 screen : TEXCOORD0;
                // 原始导航面的世界位置，不包含细线显示偏移。
                float3 world : TEXCOORD1;
                // 原始导航面的视空间位置，用于沿当前像素射线重建遮挡点。
                float3 view : TEXCOORD2;
            };
            // 输出颜色与受限纠正后的深度，继续由LEqual执行真实障碍及导航自身遮挡。
            struct FragmentOutput
            {
                // 当前面或线的透明颜色。
                fixed4 color : SV_Target;
                // 原始或有限贴地纠正后的平台深度。
                float depth : SV_Depth;
            };

            // 将input原顶点投影到相机，线层仅沿视方向偏移，不改Mesh或文件。
            VertexOutput Vert(VertexInput input)
            {
                // 当前顶点及插值所需的完整输出。
                VertexOutput output;
                output.world = mul(unity_ObjectToWorld, input.vertex).xyz;
                // 负Z为相机前方；原值供场景重建，偏移值只用于光栅投影。
                float4 viewPosition = mul(UNITY_MATRIX_MV, input.vertex);
                output.view = viewPosition.xyz;
                viewPosition.z += _ViewOffset;
                output.vertex = mul(UNITY_MATRIX_P, viewPosition);
                output.screen = ComputeScreenPos(output.vertex);
                return output;
            }

            // 对input片元保留普通深度；仅匹配Terrain表面的有限遮挡偏差可修正。
            FragmentOutput Frag(VertexOutput input)
            {
                // 默认输出保持原导航深度，非Terrain或超上限遮挡不走贴地纠正。
                FragmentOutput output;
                output.color = _Color;
                output.depth = input.vertex.z;
                if (_TerrainEnabled < 0.5)
                {
                    return output;
                }
                // 当前像素的真实场景深度和转换后的相机距离，单位米。
                float sceneRawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screen));
                // 真实地表或障碍距离相机的视空间深度。
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth);
                // 原始导航面距离相机的视空间深度。
                float navigationEyeDepth = -input.view.z;
                if (navigationEyeDepth <= sceneEyeDepth)
                {
                    return output;
                }
                // 沿该像素的观察射线重建真实遮挡点；透视相机按距离缩放XY。
                float3 sceneViewPosition = input.view;
                if (unity_OrthoParams.w < 0.5)
                {
                    sceneViewPosition.xy *= sceneEyeDepth / navigationEyeDepth;
                }
                sceneViewPosition.z = -sceneEyeDepth;
                // UNITY_MATRIX_I_V是实际相机视矩阵的逆，随用户旋转Scene镜头实时更新。
                float3 sceneWorldPosition = mul(UNITY_MATRIX_I_V, float4(sceneViewPosition, 1)).xyz;
                // 遮挡点在当前Terrain中的归一化XZ位置。
                float2 terrainUV = (sceneWorldPosition.xz - _TerrainOrigin.xz) / _TerrainSize.xz;
                if (any(terrainUV < 0) || any(terrainUV > 1))
                {
                    return output;
                }
                // 将两端顶点对应到实际像素中心，避免513点高度图的半像素偏移。
                float2 textureUV = (terrainUV * (_TerrainSize.w - 1) + 0.5) / _TerrainSize.w;
                // 当前遮挡点XZ对应的Terrain世界高度。
                float terrainHeight = tex2D(_TerrainHeights, textureUV).r;
                // 仅允许与Terrain相差八厘米内的场景点；树柱岩石高出地表的部分继续遮挡。
                if (abs(sceneWorldPosition.y - terrainHeight) > 0.08)
                {
                    return output;
                }
                // 本次射线中地表与导航面的高度差，限制作用范围，禁止透画任意后方层。
                float terrainGap = sceneWorldPosition.y - input.world.y;
                if (terrainGap < 0 || terrainGap > _TerrainTolerance)
                {
                    return output;
                }
                // 只将匹配地表的片元放到地表之前一个很小的深度偏移，保留原透明颜色与导航自身遮挡。
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
