# 2026-10-07 YCombatEditor 融合方案

## 修改文件与内容

- `项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md`：新增 YCombatEditor 源码调查、Unity 版本门禁、数据映射、首期白名单、资源生命周期及分阶段验收方案。
- `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`：追加任务交接和独立审查结果。
- `项目修改日志/2026-10-07-YCOMBAT-EDITOR-DESIGN-CHANGELOG.md`：本记录。

## 设计思路与审查

保留当前 Luban 表、服务器权威结算、`skillUuid` 和客户端 `MSkill` 动画入口；YCombatEditor 先作为编辑创作层，只导出可无损承接的完整单段动画和角色根粒子。它要求 Unity 6，而本项目为 Unity 2023.2，因此隔离兼容性验证是实施前提。10070 的目标头顶落剑和 `EffectPos` 蓄力不能直接映射到 Y 通用 FX，继续使用现有事件表现。

源码调查和项目接缝由两个子 Agent 独立完成；审查子 Agent 首轮打回特效定位、音频和动画裁剪遗漏，后续打回运行字段白名单、异步句柄、YooAsset 打包、`skillClip` 回填和默认零位移曲线问题。逐项返工后最终只读复审通过。文档空白检查通过；未修改业务代码或配置，不执行编译。

## 未解决与下一步

Unity 2023 对该 Unity 6 包的编译、编辑器预览和正式资源打包尚未实测。后续先按分析文档阶段 0 在 `_temp` 隔离工程验证，未通过时不在正式项目试装；通过后再委派客户端开发及独立测试审查。原表、代码、场景和资源本轮全部保留。
