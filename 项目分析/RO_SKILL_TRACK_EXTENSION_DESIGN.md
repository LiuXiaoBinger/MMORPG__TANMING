# RO 技能时间轴轨道补齐方案

日期：2026-10-10。修改记录：项目修改日志/2026-10-10-RO-SKILL-TRACK-EXTENSION-CHANGELOG.md。

## 依据与范围

本次在已融合的 Y 编辑器中补齐制作轨道，依据 RO 的 `config/AutoGen/protobuf/local/common/pbl_skill.proto`、客户端 `MArtSkill.FireEvents` 及技能编辑配置产物。参考项目未找到技能窗口与导出工具源码，因此数据字段和时间语义可以核对，原窗口布局不能确认。详细来源见项目分析/RO_SKILL_EDITOR_REFERENCE.md。

原有动画、特效、音效、位移、朝向控制、碰撞、飞行物、预警、镜头与流程等轨道继续保留。原碰撞/飞行物表达已有制作用途，但不是 RO ResultList/LongAttackData 全字段复制；本次不把已有能力描述为完整 RO 伤害结算。

## 新增数据与界面

| 制作轨道 | 对照 RO 字段及类型 | 时间语义 |
| --- | --- | --- |
| 旋转 | RotateList / MSkillRotateData | At、End 为秒，AngularVelocity 沿原角速度语义 |
| 震屏 | ShakeList / MCameraShakeData | At 为开始秒，Time 为持续秒，保留空间及各轴/FOV参数 |
| 镜头特效 | CameraFxData / MSkillCameraFxData | At 为触发秒，保留可见对象与特效 ID |
| 镜头后处理 | CameraPostEffect / MCameraPostEffectData | 原单对象，At、End 为秒 |
| 模型变色 | ChangeColorList / MSkillChangeColorData | At、End 为秒，保留渐变资源路径 |
| 技能条 | SkillBarList / MSkillBarData | At 为开始秒，Duration 为持续秒 |
| 召唤 | MobUnitList / MSkillMobUnitData | At、LoopCycle、LifeTime 等沿原秒语义 |
| AI 事件 | AIEventList / MSkillAIEventData | At 为触发秒 |
| Buff | BuffList / MSkillBuffData | At 为触发秒，原字段仅 At、Index，不能自行添加 BuffId |

以实际 RO 协议确认 CameraFxData 的集合性质，不将单对象凭空改成列表。新增枚举沿 RO 原值；TrackType 仅在末尾追加，不能改变已有序列化数值。配置内保留 RO 秒字段，窗口按现有帧率投影，编辑拖动后写回秒；不额外储存第二套帧时钟。

震屏运行语义还以 RO `MCameraShakeComponent.initShakeInfo/getShakePos` 核对：Frequency 为持续时间内的震动次数，Time/Frequency 才是间隔；各轴及 FOV 振幅允许负值，校验只要求有限值，不能自行加非负限制。

窗口必须接通轨道注册、创建、选中与属性编辑、删除、复制粘贴、拖动、范围显示、末端帧计算、中英标签、Undo 与保存读取。单对象轨道最多一个。强类型数据和独立同名源文件用于新增领域类型，序列化 UI 边界沿现有 SerializedObject。

## 发布与运行边界

新增内容用于后续制作。本次不改变正式技能动画与粒子调度、不扩展服务器协议或技能结算，也不自动改写七份现有技能资产。新字段默认空时，原资产仍可以按已有导出链发布；新字段有有效内容时，未接入正式执行的内容必须阻止发布并说明原因，不能丢弃内容后报告成功。

包的 RequireEventHandlers 校验保持严格，未接入调度的 RO 事件不能被当成可播放。编辑保存和配置验证须与真实运行验收区分。

## 协作与验收

- 数据开发：原 RO 类型、字段、时间投影、末端和校验、宿主导出默认字段。
- 编辑器开发：原窗口全链编辑操作、轨道枚举注册、双语界面、中文手册及 CHANGELOG。
- 独立审查测试：真实新增 Runtime 与 Editor/宿主程序集编译、序列化和编辑操作边界回归、规范检查、保护基线及返工复验。
- 主 Agent：范围协调、结论验收和记录文档。

临时构建及证据统一放 `_temp/ro-skill-tracks-20261010`。旧表、配置资产、动画、预制体、场景、GUID、预览根姿态修复、Q 的 1/1/2 特效与第三段局部参数都属于保护范围。正式 Unity 界面与保存重开若工具不可用，必须明确尚未实测，不能用隔离编译代替。
