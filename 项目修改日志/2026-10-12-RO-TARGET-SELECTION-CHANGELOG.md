# RO 三种目标选择接入

- 需求：点击敌人、瞄准切换、技能自动选敌；沿RO类/成员/方法名，仅做当前工程必要适配并简化复杂依赖。
- 分工：target_client客户端开发、target_protocol服务端边界与必要接入、target_review独立测试审查、主Agent只协调验收及记录。
- 修改准备：Docs/AI/CURRENT_TASK.md登记范围与验收；现有用户改动作为基线，正式源码改前备份，编译/验证产物只写_temp/ro-target-selection-20261012。
- 参考项目分析/RO_TARGET_SELECTION_FLOW.md，实施项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md，后端边界项目分析/RO_TARGET_SELECTION_SERVER_BOUNDARY.md。

## 实际修改与设计

- 13客户端路径：TouchObject/MSkillTargetMgr/MSkillTargetDefine三新源及meta，PlayerEntity/SceneEntityWorld/SceneWorldSystem/PlayerInputCtr/SkillCtrl/MNavigationComponent/RoleCurrInfoWindow七旧源。完整根目录相对路径/逐文件内容见项目修改日志/2026-10-12-RO-TARGET-SELECTION-CLIENT-CHANGELOG.md，改前备份与末稿差分保留，不覆盖用户稿。
- RO同名接口接现输入/实体世界：左键同对象按下松开选中，瞄准按钮/Tab在30米内前四敌循环，攻击优先当前敌人否则找最近；模型层级识别碰撞体不改名字，UI不点穿/目标文字同步，死亡/隐藏/离场/超距清目标。
- 怪物体系为敌、其他玩家友；六技能全无目标，选敌仅辅助水平朝向，无敌保原释放，提交后再转向与预测，失败不转不预测，Type3/轻功不参与。保F/跳跃/MP/CD/root/移动与原离线语义，不加重试、代次、等待或框架。
- 后端只读核表/二进制6×18逐值一致与正式技能/主城链，无后端/协议/DLL/表/场景/预制体改动；职责日志项目修改日志/2026-10-12-RO-TARGET-SELECTION-SERVER-BOUNDARY-CHANGELOG.md。
- 主更新Docs/AI四入口和本汇总；开发/后端新增两分析与两职责日志。全部工具、夹具、构建、备份/清单/截图在_temp/ro-target-selection-20261012，未清临时。

## 验收与失败留存

- 独立280源Rebuild 0错38警告（35旧＋3处既有MonsterEntity重名引用CS0436，实际绑定客户端并有原生证明）；中文注释/强类型/一类一文件/禁namespace、foreach、三目/弱载荷审查通过，无业务返工。
- 原生完整真实DLL/world/输入/射线/UI57＋72生产源薄边界技能/移动回归36，共93项通过。HUD中文可读、无重叠/F最右；4控件方法逐字等价。独立报告_temp/ro-target-selection-20261012/review/review-report.md和final-audit.json；日志native-target-final.log/regression-final.log、图hud-target-idle-1920x1080.png。
- 主root_verify.py核11清单3792去重文件SHA、原生DLL/4方法/3运行标记一致；2067关键保护仅7授权既有源变。root-manifest-check.json PASS，保护不等于全工作区。
- 首次临时依赖/别名/渲染帧/回归边界/预览目录缓存/审计拼写问题均留失败日志，只修_temp。主首核HUD注释导致编译SHA不同，独立末稿重编后通过，首失败root-first-manifest-check.json保留；未扩大业务或复制旧PASS。

## 未解决边界与下一步

- 正式主城无怪物生成链，不插Mock；专用世界工厂怪物验收不等于正式战斗。无敌瞄准未选中，攻击保原无目标施法；本轮不含刷怪/目标协议/伤害/PvP。
- 原生模型为专用GO/Collider，Global正式网络/包初始化未执行；回归薄边界、HUD子树隔离预览。未验正式联机/异步模型/安装包/Bundle，未部署/重启/发布。下一步正式重新运行查提示与入口，用户明确怪物/目标结算需求后再扩。
- 临时证据保留待产品经理确认清理。
