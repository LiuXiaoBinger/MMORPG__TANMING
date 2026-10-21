# 技能特效播放链外观检查

日期：2026-10-13。用户确认异常表现包括颜色或形状不对；本检查负责运行播放链，Shader、材质、变体和包版本另由资源开发职责核验。

## 静态证据

检查 `MArtSkill.PlayRuntimeEffect`、`SkillRuntimeFxLifetime`、`SkillRuntimeFxEvent`、`SkillFxSocketResolver`，并解析八份正式 Runtime 中十二条粒子引用。

十二条事件分别使用六份预制体，引用的 ParticleSystem 均属于预制体根节点，父 Transform fileID 为零。六个根节点缩放均为 `(1,1,1)`。因此当前正式数据没有引用嵌套子组件而漏克隆其他同级粒子的现象；正式实例化完整根对象。

各事件的源粒子子树系统数量依次为 `15,15,15,15,12,15,19,15,16,16,16,13`，合计 182 个系统引用（重复事件各计一次），不是 182 个唯一资源。所有源子粒子的循环标记均为零，运行租约组件把 loop 设为 false 对这组资源未改变取值。

当前播放入口不写材质、Shader、startColor 或 shape 模块，只按导出事件写根节点的位置、旋转、缩放并 `Play(true)`；子节点姿态沿源预制体保留。当前导出规则要求作者 `playbackSpeed=1`、`customLifetime=0`，跟随标记与定位枚举一致；运行定位按这些既有规则执行。

作者预览与运行挂点/前方/绝对世界姿态的求解对应。当前目标头顶模式仍按既有无目标前方兜底；没有新增目标玩法或用本次检查修改配置。

## 验证及边界

只读资产解析结果保存在 `_temp/skill-fx-appearance-20261013/client/particle-source-audit.json`，包括每个引用、根层级、粒子数、循环标记、stopAction 和资产 SHA。

主 Agent 在用户 1071 构建结束、Unity 空闲后已执行 `_temp/skill-fx-appearance-20261013/client/native_playback_probe.cs.txt`。真实当前程序集的 `MArtSkill.PlayRuntimeEffect` 十二条粒子事件全部通过，输出 `PLAYBACK_PROBE_PASS checks=12`，控制台错误零；本开发读取 `native-playback-result.txt` 复核十二个事件与粒子数量。

原生测试通过反射调用正式私有播放入口，生产粒子实例化、定位、播放和生命周期组件均实调。外围使用未注册世界的专用实体/模型与测试 Runtime 副本主键零，避免正式资源租约和网络加载，挂点层级为临时节点；只验证完整子树、子变换、循环、初始颜色模式/值、形状模块类型/缩放、原材质槽引用保持。所有临时对象最终销毁、不保存场景；没有覆盖正式角色姿态、最终 GPU 变体、主城后处理或网络。

目前没有播放链改变这组正式颜色/形状的实证，因此没有修改 `MArtSkill` 或生命周期业务。以上结果不能证明实际 GPU Shader 变体或最终画面正确，用户新建的 1071 包仍需结合实际渲染结果验收。

## 主城图像环境的只读补充

`Scene_MainCity.unity` 的正式主相机 HDR 为 1，PostProcessLayer 已启用、体积层掩码为 64、抗锯齿模式为 1（TAA）；启用的全局体积引用 `Assets/GM_Game/Scenes/MainCity_Res/Data/POST Profile Elven.asset`。该资源 AutoExposure 启用，keyValue=0.87；ColorGrading 启用，自定义 tonemapper=3、postExposure=-0.24、contrast=12。Bloom 标记 active=1，但 enabled.value=0 且 intensity=0，因此不能说正式主城 Bloom 已启用。

项目 `m_ActiveColorSpace=0`（Gamma），Graphics 使用 Built-in 管线。现有 `BlendDistort.shader` 使用 GrabPass 与 `_CameraDepthTexture`。这些上下文会使无后处理、无背景或无深度的隔离预览与主城产生不同的画面，但当前仅为配置事实，没有同环境失败对照，不能据此断言根因或擅自改主城图像设置。

主另生成源资源与 1071 包资源渲染证据于 `_temp/skill-fx-appearance-20261013/review/native-20261013095930/`，最终像素分析由独立测试收口；本开发不将其准备或他人尚未返回的结果计入已通过项。

修改记录：`项目修改日志/2026-10-13-SKILL-FX-PLAYBACK-CHECK-CHANGELOG.md`。
