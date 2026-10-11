# 2026-09-27 RO 施法实例列表生命周期返工

## 需求与首次审查

用户指出 RO 的 `IsAlreadyCasting(args.UUID)`、`OnSkillAutoEnd()` 和 `_skills`：动作状态结束不等于施法生命周期结束，弹道、脱手及其他仍在运行的旧实例必须留存。独立审查确认之前本项目仅为多段连击保留旧实例，单段继续占当前槽且旧列表不逐帧更新，判定未通过。

RO 对照：`clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs:88-101` 查当前及旧实例去重；`:448-467` 移交和回收；`:612-619` 更新旧实例；`:751-756` 任意技能退出动作状态后移交；`:940-947` 新施法先去重再移交旧实例。

## 修改文件与实现

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：已学技能集合改为 `_learnedSkillIds`，`_skills` 改为 `List<MSkill>`；所有活跃施法退出动作槽都调用 `OnSkillAutoEnd` 移入列表，解除动画租约并建立新的当前实例。列表逐帧更新，按 UUID 查找、确认、结束、超时及清空；删除因旧实例仍活跃而阻断新技能的额外条件。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：动画槽释放后记录 `_animationReleased`，禁止旧实例逐帧更新时重新安装动画，仍保留其 UUID、计时和确认生命周期。
- 首次返工复审再次未通过：重复当前或旧 UUID 的开始事件会先进入状态自转，再在 `OnStart` 查重，可能结束新的当前技能。第二次返工将 UUID 查重放到 `OnSkillStartedEvent`，命中当前或列表实例后直接返回，未命中才转发原动作回调。没有修改共享动作基类。
- 保留旧技能表、协议、Q 三段和 700 毫秒状态窗、模拟角色学习包及非目标代码。协作记录同步 `Docs/AI/CURRENT_TASK.md` 和 `Docs/AI/TEST_REPORT.md`。

## 验证及边界

- 客户端子 Agent 构建初版 0 错误；第二次返工修正本项目 `MEventHandler` 包装类适配后构建 0 错误、38 条警告。主 Agent 对最终源码将输出和中间文件全部定向 `_temp/ro-skill-lifecycle-final/` 重建，0 错误、42 条警告。
- 首次独立审查确实打回重复 UUID 的状态自转问题；最终只读复审核对前置查重、列表留存更新和 UUID 精确结束。独立只读进程编译因 Windows SDK 路径访问被拒，未进入业务编译；当前授权环境构建已通过。
- 未运行 Unity Game View 真实按键、异步动画画面或四服联调。当前 `MSkill.Update` 尚没有 RO 的实际弹体运行逻辑；本次提供正确的实例留存更新生命周期，不宣称已实现弹道、伤害或完整 RO 表现时间轴。
