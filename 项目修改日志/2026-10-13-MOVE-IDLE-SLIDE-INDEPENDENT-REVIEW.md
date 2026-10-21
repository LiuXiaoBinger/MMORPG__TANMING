# 2026-10-13 移动时偶发待机平移：独立审查与测试记录

## 修改范围与设计

- 用户补充A本地概率待机平移，B看A同样，普通移动键也可出现。本轮不继续扩大双账号入场功能，保前轮MainCity修复。
- 客户端开发仅改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs` 的InState与 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs` 的PlayAnim/移除空killTimer及对应常量。
- InState回归RO已有语义：比较最近Play/CrossFade请求目标 `_currentState`。修前只读Animator当前采样源，Walk→Idle过渡还以Walk为源时，重新按移动会误认为Walk已播放，提前return留下Idle过渡；运动/协议IsMoving仍真而动画为Idle。没有新增播放状态、队列、重试或逐帧强制刷新。
- 默认动作原后摇等待分支只返回状态名，实际Timer已被注释且killTimer空体，技能后摇短进度时会遗漏实际默认播放。按主批准简化为技能动作窗结束后正常切默认，不新接RO后摇计时框架；起跳和落地保护及速度处理保留。
- 独立审查只读业务，在 `_temp/move-idle-slide-20261013/review/` 新建专用临时测试/编译/证据；没有调用Unity MCP、修改正式场景/预制体或发送真实账号请求。

## 核对与验证

- RO原 `clientcode/CSProject/MoonClient/AsyncObj/MAnimator.cs:466` InState为 `_playStateName == stateName`；本项目已有_currentState承担同一播放请求目标职责。正式MMove和PlayerEntity上传IsMoving、远端onSyncMoveEvent仍按原链，没有按动画姿态改运动状态或协议。
- 修前关键生产方法全文抽取：StateName、ShouldPlayMovementAnimation、IsLandingAnimationPlaying、PlayAnim、InState。实体/动画采样/基类播放为薄记录边界，明确分离采样源与过渡目标。本地/远端×Walk/Run×普通/战斗共8种停止过渡中立即恢复移动情境，均复现最终进入Idle；短后摇无Timer不实际提交默认动画也复现。before-runtime.log记录30项，包含正常回归项，不宣传全部都是独立故障。
- 末稿同样5方法逐字抽取，8组合恢复移动都转正确移动目标，技能尾实际提交FIdle，起跳/落地动画不被覆盖，持续同状态不重复播放、正常停止、远端战斗跑步剪辑/速度保持。final-runtime.log记录30项全通过。
- 最终280真实客户端源独立Rebuild 0错误、39既有警告；开发自身工程38既有警告，保留两工程各自实际统计。本轮两源没有新增声明警告。临时抽取测试2条未使用字段警告是夹具自身边界，不计业务警告。
- 精确两源diff已审查，仅上述需求范围。没有新增namespace/foreach/三目/弱类型业务载荷或超过3判断项布尔链；修改注释说明实际用途。未泛改GetCurrentAnimInfo、StateMachine、MMove、服务器或输入。
- `final-audit.json`：280编译输入SHA零漂移，5方法与当前生产源逐字一致；前轮MainCity SHA `75A4EB68F8660CA6F933C68E0F532BAA1C5BB59BCD85BE23D9DB9DBD0196DEA5`保持。
- Default末稿SHA `BB7966F721F650C67134B7D004FCCCDA0D1418230A5EC289076D2D521D7010AC`；MAnimator末稿SHA `3AAE49F3C58E670A71D73B0DF680F338B5725312A7C8E914D34CB2BB4BF78BF2`，与开发交付一致。

## 限制、未解决与交接

- 本独立30项是实际生产关键方法加薄动画边界，不等于真实Animator/异步资源或正式双客户端画面验收。真实Prefab和Animator由主统一执行原生before/final探针，须按实际结果单独记录，不重复计入本独立断言。
- 主旧现场静止时Default想播FIdle而实际残留Skill04，不能直接当成用户移动键触发Idle的同一时刻；GetCurrentAnimInfo原生normalizedTime与信息normalizedTime分开。主读取infoNorm=1不否认此前可能经过漏切分支，但本轮不以这条快照猜测扩改异步加载。
- 技能待释放路径也用InState，现有原生IsInTransition保护保持。本轮未验全部技能资源生命周期或正式热更发布，不以局部默认切换测试覆盖全部技能表现。
- 独立结论：源码审查、末稿编译、关键方法修前复现和修后回归通过，无待返工业务项。后续原正式编译/资源流程更新后，双账号现场验证连续快速点按/松开/重新移动与普通技能结束。
- 主负责将本轮实际验收及原生结果更新 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`；根修改日志与分析按项目要求归档，不清临时或覆盖用户构建。

## 最后纯空白稿核验

- 开发仅移除Default第317行尾空格后，独立重新抽取5方法、运行既有30项并完整Rebuild280源：30通过、0错误39既有警告。没有新增测试范围或修改业务。
- final-audit.json、compiled-source-hashes.json、runtime-inputs.json与两源diff已更新；方法逐字一致、输入无漂移、MainCity保护保持，新Default SHA以本记录BB7966F7开头为准。
- 主报告Unity原生工具busy，未获得本轮有效before/final原生结果；本轮原生专项和正式双客户端画面仍未通过验收，不以薄边界30项替代。
