# RO 技能更新循环精简与参考移植约束（2026-10-11）

- 用户要求：去掉技能更新的 MSkillUpdateEntry/快照/代次包装，照 RO 简单写；存活列表不一概视为脱手。加入持久约束，参考项目时主要沿用原代码，不擅自扩展需求外机制。
- 约束文件：AGENTS.md、AIconfig/workflow.md，同步 Docs/AI/DECISIONS.md；参考一致性与简洁性优先，仅做必要工程接入，不为人造极端测试增加包装。
- 实际业务范围：MSkillComponent.cs、MSkill.cs（仅删除快照专用内部属性）、删除此前 Agent 新增的 MSkillUpdateEntry.cs/meta、生成 GM_Game.csproj 移除该索引，共五路径；没有新增类型或辅助管理层。
- 参考：RO 常驻 Update 倒序推进 `_skills`，当前 `_skill` 在动作 OnUpdate 更新/结束；列表还有独立技能分支，不只脱手。保本地 Entity 的 Skill 先 Machine 调度和 MTimerMgr 到期职责，不扩 CD 时钟、网络或资源。
- 分工：ro_cd_lifetime_develop 开发，ro_cd_lifetime_review 独立审查，主协调与约束/协作记录。详细分析见 `项目分析/RO_SKILL_CD_LIFETIME.md`。
- 待验：当前/列表各帧一次、Cease 自删除不漏、Q 切段、自然尾/声音/冷 GO/Timer、无确认和迟到结果、实体运动末帧；完整源码编译、规范及源码/meta/工程删除范围保护。旧假设重入断言移除，不复用历史275成果。
- 证据 `_temp/ro-skill-update-simple-20261011/`，独立终验已通过；不改作者 F13/end111/F49、Runtime、资源/场景、服务器/协议/共享 DLL，不自动导出、不清临时。

## 实现与当前验证

- 常驻 Update 删除快照池借还、try/finally、Contains、代次比较和当前槽特别更新，直接沿 RO `for` 倒序取技能/Update/Cease。当前技能恢复在 OnUpdate 推进/结束，原动作退出移交保持。
- `_skills` 注释改为存活技能而非全部脱手，注释保持简短中文语义。删除包装源/meta及唯一 Compile；保所有其他工程字节，MSkill 只删快照专用属性，真实异步保护与 MTimerMgr 到期回调保持。
- 独立当前 274 项运行通过：92 生命周期、63 运动、34 实体、49 CD、15 普通列表/Cease、21 无确认；272 真实客户端源编译零错误/37 旧警告。删除三项无实际生产依据的虚 Update 删兄弟/重Fire断言，增加两项普通倒序自删除回归，历史275不计本轮通过。
- 首次运行 `_temp` 状态机薄边界缺原 Stop 入口，仅补测试边界后通过；生产没有为夹具改变。独立两报告/final-audit均PASS，主已读取并完成复核。

## 终验与交接

- 2801保护2796保持，三授权修改/两授权删除，无新增或越界；CD块字节保持。独立审计发现工程BOM额外丢失已返工恢复，最终精确只删89字节包装Compile，其余全字节保持，不隐藏配置返工。
- 主核开发3末稿/272编译、独立3末稿/272编译/50运行共600项SHA全部一致，另核两删除before SHA与正式路径已不存在，root-manifest-check PASS；修改中文注释/禁止语法及新增差分空白审查通过。
- 除五业务路径外，修改AGENTS.md、AIconfig/workflow.md、Docs/AI四入口与项目分析/RO_SKILL_CD_LIFETIME.md，新增本日志。主不修改客户端业务，开发与独立审查均留痕。
- 当前无未解决已复现代码阻断；真实PlayMode/画面/听感、TCP/服务器、原生墙体/Animator精确末帧/Bundle未验，算法测试保薄边界限定。使用原入口体验，临时保留，不自动发布、重建共享DLL或清理。
