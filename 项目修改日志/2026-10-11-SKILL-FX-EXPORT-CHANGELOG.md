# 特效制作区间导出报错修复

日期：2026-10-11。状态：已完成；fx_export_develop开发、fx_export_review独立验证审核PASS，主核验输入及记录。

## 问题与范围

用户点击校验并导出当前表现，提示“粒子内容只支持单次普通粒子，不支持混合音频、倍率、区间或自定义寿命：1”。当前3007001作者落剑已是F13/end111，最后声音F49；只读源码确认ReadFx的endKeyNumber!=0触发笼统门禁，纯粒子不含混合音频。

实际仅修改LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAssetTools.cs的ReadFx及summary；协作记录和项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md同步。主不直接写业务代码。

## 实现原则

制作区间与现有编辑预览保持一致：开始帧一次生成粒子，结束帧为制作标记，不赋予新截断/重复语义。保合法区间，非法字段和实际未接入参数继续准确拒绝，不直接移除全部校验。

原ActionTiming.ContentEnd对fxList只读取keyNumber，ReadTimelineDuration继续这一已有契约；作者end111不扩实际触发时长，当前最后声音49决定原下限。主初始验收将末标记当制作末帧，读原源码后已纠正，不因此修改包或时长计算。原Y结束帧小于等于开始帧表示单帧，不自创倒序错误。

当前用户配置F13/end111/F49、所有Runtime及原粒子/声音/动画/表格/meta/场景、上一轮时间范围UI和RO正式技能/运动链保护。不自动发布，不保存用户未保存现场，不生成角色或注入按键。

## 验证与交接

before原生真实Validate和Export两入口均复现截图原错误。末稿64独立原生断言通过、worker退出0；当前真实稿只导2条FX，落剑13/30秒一次触发，原TimelineDuration=49/30秒、作者end111及声音49保持。末0/等开始13/非负倒序12/111/10000均沿原单次契约；负帧原Y拒绝，120FPS换算和同帧排序正确，未接入字段与NaN完整门禁保拒，实际字段错误定位通过。

七原作者Validate通过、六旧发布兼容；当前1原发布与最新作者差异先记录，再在隔离Runtime导出后完整反校验与依赖通过，未以修改正式资产迎合。8真实宿主Editor源Rebuild零错/2旧警告；2788相关路径2787字节不变，仅1授权源码改变，无增删或未授权。注释与禁namespace/foreach/三目/弱业务载荷门禁通过；主重核开发1/独立编译8/原生301共310当前SHA零差异。

开发result.json、final-manifest.json、final-diff.txt和host-build.log；独立native-before-repro.txt、native-before.log、native-result.json、native-final.log、Editor-build.log、final-audit.json、两独立报告、independent-protection.json及source-consistency.json存证。主root-manifest-check.json=PASS。

主已读取两独立报告。独立审计夹具首稿将方法summary合法修改误判为范围越界，仅修_temp比较边界至summary前；末稿方法以外与before完整相同，未修改生产迎合夹具，原生和保护证据不受影响。

无本轮未修复已复现阻断。原生发布只在隔离环境，不冒称正式GUI按钮点击/PlayMode画面/听感；正式作者/Runtime/资源保持，不生成角色或保存现场。等待Unity编译后由用户原绑定显式导出当前稿，无需把末111改回0。此次不新增粒子结束截止能力，末标记继续原语义；不扩大测试。

证据_temp/skill-fx-export-20261011/develop与review；临时保留待产品经理确认。协作入口Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md；使用说明项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md。
