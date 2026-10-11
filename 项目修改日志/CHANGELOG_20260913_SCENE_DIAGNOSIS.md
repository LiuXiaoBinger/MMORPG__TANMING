# Unity 场景卡顿与模型偏暗诊断

## 检查范围

- 对比 `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity` 和 `Scene_MainCity.unity`。
- 检查场景序列化配置、运行日志、主城资源规模、Unity 进程和电脑硬件。
- 本次没有修改代码、场景、预制体、贴图导入设置或光照数据。

## 主要发现

1. 两个场景的 `m_LightingDataAsset` 都是空 GUID。场景虽然配置了烘焙 GI，但没有有效 LightingDataAsset，人物偏暗首先应检查光照烘焙和绑定。
2. `New Scene` 运行时会执行 YooAsset 模拟构建；日志出现 Unknown 脚本，并且 `NpcManager.Awake` 早于资源包初始化调用 `LoadPrefabAsync`，抛出 `Package initialize not completed`。
3. 主城资源目录约 964MB，包含约 850MB PNG，多个贴图超过 30MB。进入场景时集中加载、纹理上传和 Shader 处理会造成明显卡顿。
4. 电脑为 i9-14900HX、RTX 4070 Laptop 8GB、约 32GB 内存；从硬件规格和现象看，当前更像场景/资源/初始化问题，不像硬件不足。
5. Play Mode 期间 MCP 失联，Unity 主进程约 3.37GB，AssetImportWorker 各约 1GB；这说明编辑器运行态压力较高，但还不能替代 Profiler 结论。

## 后续验证顺序

1. 由客户端开发修复 `test`、`NpcManager` 的初始化顺序，并清理场景中的缺失脚本。
2. 重新烘焙 `New Scene` 和主城，确认场景文件绑定有效 LightingDataAsset，再比较人物亮度。
3. 在不连接服务器的离线测试场景中采集 Unity Profiler：主线程、渲染线程、GPU、GC Alloc、纹理内存和实例化耗时。
4. 按贴图尺寸和压缩格式优化主城资源，并将 NPC/角色资源改为分批异步加载。

## 验证结果

- 静态检查和硬件检查完成。
- 未进行任何修复，未执行完整 Profiler 采样和真实客户端联调。

## 运行态补充

用户提供的运行时 Statistics 显示 31.5 FPS、CPU Main 31.7ms、Render Thread 8.4ms。由此确认卡顿是运行帧内持续主线程超时，不只是进入场景时的加载卡顿。当前优先排查顺序为：角色和 NPC 的 `Update`、Animator、CharacterController/Physics 检测、阴影准备；其次再处理 1718 Batches、1032 SetPass、903 Shadow Casters 和 61 个可见 Skinned Mesh 带来的提交压力。具体函数必须用 Unity Profiler Timeline 定位。

用户进一步完成了 A/B：关闭 `MainCity_Geometry` 后帧率上升，关闭灯光后帧率也上升。这确认主城几何和实时灯光处理存在直接关联。下一步应保留灯光只关闭阴影做对照；若帧率恢复，优先优化 Directional Light 阴影距离、级联、分辨率、远景投影和几何体的 Cast Shadows，而不是直接关闭灯光。

静态复核发现 `New Scene` 有两个启用的方向光，其中 `Lights/Directional Light` 使用软阴影；场景没有有效 `LightingDataAsset`，也没有 `OcclusionCullingData`。因此当前主城静态几何很可能没有充分使用烘焙光照和遮挡剔除，实时灯光会放大几何体的受光、阴影和可见性提交成本。建议最终方案为单一主方向光 + 烘焙光照/遮挡数据 + 远景环境物件按距离关闭实时投影阴影。

## Occlusion Culling 操作要点

1. 先复制或备份场景。
2. 选中建筑墙体、城门等不透明遮挡物，在 Inspector 的 Static 下启用 `Occluder Static`；选中可能被遮挡的环境物体启用 `Occludee Static`。可先对主城几何子节点设置，避免只标记一个整体大网格。
3. 打开 `Window > Rendering > Occlusion Culling`，在 `Bake` 页点击 `Bake` 生成遮挡数据。
4. 选中运行时使用的 `Main Camera`，确认 Camera 组件的 `Occlusion Culling` 已启用；Cinemachine 最终仍由该 Unity Camera 输出。
5. 从同一位置对比 Statistics 和 Profiler。若 Batches/Shadow Casters 几乎不变，说明对象没有有效遮挡关系或场景网格过于整体化。

遮挡剔除只影响 Renderer，不会自动关闭 MeshCollider、Animator 或脚本 Update；主城还需要拆分视觉与碰撞、减少远景 MeshCollider，并单独处理灯光阴影和 LightingDataAsset。

## Occlusion Culling A/B 结论

用户实测开启遮挡剔除后变化不明显。检查发现 `MainCity_Geometry.prefab` 约含 681 个 Transform，是大型嵌套预制体，内部包含多个实时灯光；遮挡剔除不会减少实时灯光对可见物体产生的额外光照 Pass。因此当前应优先处理：创建并生成有效 LightingDataAsset、把静态环境灯光改为 Baked、减少实时点光源、只保留必要方向光阴影。Occlusion Culling 可以保留，但不应作为本场景的首要优化手段。
