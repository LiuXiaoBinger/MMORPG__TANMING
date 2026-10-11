# RO 技能去除额外确认等待（2026-10-11）

- 用户要求：对照真实 RO，删除额外的服务器确认等待管理；本要求替代旧预测确认超时回滚设计。
- 计划范围：MSkillComponent、MSkillCore、MSkill 和 SkillCtrl 的实际施放/回包链，开发冻结后按最小必要范围修改。正式分析见 `项目分析/RO_SKILL_CD_LIFETIME.md`。
- 设计：不保 `_pendingSkillIds`、每笔预测确认超时或确认门禁；客户端依动作/CD 施放并按 RO 生命周期回收。现有服务器结果用于校正 CD/MP、处理对应拒绝，旧回包不得影响新 UUID。
- 开发与审核：ro_cd_lifetime_develop 实施，ro_cd_lifetime_review 独立审查，ro_cd_lifetime_analysis 只读核 RO。主不直接修改业务。
- 证据：`_temp/ro-skill-no-confirm-wait-20261011/`，尚待当前真实源码编译、运行、规范和保护验收；历史结果不作为本轮通过数。
- 保护：不改用户作者配置、Runtime、资源/场景、协议及共享 DLL；不自动导出或清临时。完成后补实际文件、结果和未解决问题。

## 实际实现

- 修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：删除 `_pendingSkillIds` 与每帧确认期限扫描；回包直接按原 UUID 处理实例，最新请求才同步 MP/组 CD；本地已回池施法的迟到开始广播不重播。
- 修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs`：删除逐笔 `_predictions`、HasPendingPrediction、ExpirePrediction 和 CancelPrediction；仅以最近 UUID/已应用结果标记校正和去重，不阻止续放。新施放清结果标记，支持 UUID 回绕重用；新增 RO 同名 `Reload()` 清未收到权威结果的自身预测单技能 CD。
- 修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：Update 使用累计技能时间与 RO 预表现 2 秒/正式 10 秒上限；动作退槽且表现完成即可回池，无需确认或 End。Cease 通过实体当前安装核心及原 UUID 调用 Reload，避免升级前旧核心清新技能 CD。
- 修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：先回收当帧已结束声源，再由基类判断存活，时间线依旧只由 FireEvents/MTimerMgr/OnProcessTimer 登记和触发。
- 无新增/删除业务文件；原 MSkillPrediction 源码/meta/索引保留但没有运行引用。SkillCtrl 原输入和协议回包接口不变，独立运行已链接其真实完整源码验证。
- 制作末触发/完整音尾范围与自然自行回收是本地适配，RO 原 Update 阈值只有 `_data.Time`，不宣称逐字相同；实参单位、UUID 生命周期和具体差分见分析。

## 当前验证

- 开发及独立 273 真实客户端源码 Rebuild 零错误、37 既有警告；独立当前运行 275 项通过，包括真实 SkillCtrl 快捷键/Request 序列化/Result ParseFrom/CD/MP 链。
- 当前断言拆分：92 生命周期、63 运动、34 实体、49 新语义 CD、16 Cease/重入、21 无确认专项。旧确认期限回滚测试改成新语义并重跑；不是复用历史 253 项通过数。
- 独立 final-audit PASS，2801路径2797保持/四授权旧源码改动，无增删越界；本轮保存真实生成工程before，末稿全字节保持。用户作者F13/end111/F49、资源、Runtime和正式场景保持。
- 主核开发4末稿/273编译、独立4末稿/273编译/51运行共605项SHA零差异，`_temp/ro-skill-no-confirm-wait-20261011/root-manifest-check.json` PASS。MSkillPrediction旧定义仍参与编译/链接，但不再有运行业务调用，不声称删除或排除该类。
- 四源逐声明具体中文用途及禁namespace/foreach/三目/弱业务载荷审查通过，新增差分无尾空白。新UUID清结果标记、升级旧核心清新CD风险在开发自查修正后重新编译和独立运行通过。
- 两份独立报告及最终输入均归档本轮review；当前无未修复已复现代码阻断。真实 Unity 画面、设备听感、原生墙体/Animator 精确末帧、联机回包和 Bundle 尚未验；未重建共享DLL或执行原生导出。使用原入口体验，临时资料保留、不自动发布。
