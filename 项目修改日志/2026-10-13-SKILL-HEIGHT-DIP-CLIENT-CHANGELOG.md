# 2026-10-13 技能退出瞬间下沉客户端修复

## 需求与事实

用户说明两端全部技能有下沉后立即恢复，后补充一次施法后无法继续。客户端职责不扩大到动画整体重制或改数值。

正式10080施法采样证实：实体Y18.73892不变，退出动画途中真实Slider_F被释放、空Skill占位进入混合，脚Y降到18.32197后随Idle恢复。真实现场证据 `_temp/skill-height-dip-20261013/root-live-height-skill10080.txt`。只读18组已就绪真实剪辑采样不含此异常，故不能只用已安装剪辑验证否认用户故障。

## 修改文件与思路

仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs` 的 `TryReleasePendingSkillAnim`。释放判定由最新请求`InState`改为原生当前采样`GetCurrentAnimatorStateInfo(0).IsName(STATE_SKILL)`，保留现有过渡保护，防止Idle请求已记录但原生尚未切出时回退空槽。

前轮 `InState`快速停走修复与远端根运动成果均保持。没有新增字段、队列、状态重置、协议、表、资源修改或冷加载行为扩展。

新增分析：`项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md`。Docs/AI入口由主职责维护。

## 编译与范围验证

- 开发280源Rebuild：零错误、38既有警告；证据 `client/GM_Game-build.log`、`client/compiled-source-hashes.json`。
- 独立280源Rebuild零错误、39既有警告，警告签名与前轮一致；实际`TryReleasePendingSkillAnim`及`InState`抽取修前10项复现/回归、末稿10项通过。独立`review/final-audit.json`为PASS，释放方法外逐字保持、InState未变、19保护SHA保持、280编译输入无漂移。
- 用户自然退出Play后，Boot域重载已确认实际IL为末稿，主执行原生退出夹具PASS：最新请求FIdle、原生当前Skill为true且尚未进入过渡的缝隙仍保留Slider_F；12帧原生过渡全部保留真实剪辑，第12帧实际退出Skill后释放；脚相对根最低0.1100968米，未下沉。证据 `client/native-exit-release-result.txt`，此次控制台零错误警告。
- 原生夹具使用正式Humanoid、AnimatorOverrideController及生产MAnimator释放方法，但ClipInfo与待释放调用为独立测试装配，不走正式网络、完整技能生命周期或双客户端画面；此边界不因PASS扩大。没有停止用户Play、改源资产或正式场景。
- 方法外逐字保持；20冻结输入仅本源变化，280编译输入无漂移：`client/scope-audit.json`为PASS。
- 修改前快照 `client/MAnimator.before.cs.txt`，本轮精确差异 `client/MAnimator.delta.diff`；新增关键注释为中文，没有新namespace/foreach/三目或超过三项并排判断，`git diff --check`退出0。
- 修改前SHA：`3AAE49F3C58E670A71D73B0DF680F338B5725312A7C8E914D34CB2BB4BF78BF2`。
- 修改后SHA：`D7FDACE77C8F26003B4DAC59354291DA9211E1B0CECC1DAF3BD35F8CEF37639D`。

## 施法门禁调查

现场角色MP=0、Default、CurrentSkill空、计时器不暂停，所有技能CD0/CanSkillTrue；普通技能因法力不足不能启动，F成本0可发正常请求。没有依据修改生命周期或移除MP门禁。权威MP来源交后端职责核查。

## 未解决与交接

独立生产方法专项及原生退出缝隙已通过；正式双客户端所有技能表现与新热更包仍待验收。本职责不停止用户Play、不发布热更、不重启服务器、不保存正式场景。

完整证据根：`_temp/skill-height-dip-20261013`。分析：`项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md`。
