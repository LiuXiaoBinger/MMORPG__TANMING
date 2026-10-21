# 技能根运动远端同步修复（2026-10-13）

- 用户现象：A施法有根运动，B看到A原地播放技能，结束后才补移。
- 分工：客户端子Agent负责业务修改；独立测试子Agent负责复现、编译和审查；主Agent负责运行现场、验收与协作记录。
- 初步确认：本地PlayerUpdate消费RootMotion，远端Update只消费程序位移；普通影子追赶受技能MoveCasting限制，不能替代技能根位移。
- 拟修改范围：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`。沿用现有根位移采集器、碰撞提交与影子同步字段，不扩协议或框架。
- 临时证据：`_temp/skill-root-sync-20261013/`。正式主城只读采样，不停止用户Play、不保存场景、不发布热更。
- 验证结果、最终文件范围和未解决项待完成后追加；不以定位结论代替画面验收。

## 最终修改与验收

- 业务仅上述MMoveComponent.Update：远端根与程序位移合为一次MoveCharacter；技能位移帧不叠普通追赶；按碰撞后实际点重算已有影子段剩余距离与时钟，精确抵达同步点后收束移动态。没有新增字段、类型或协议。源码SHA `0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE`。
- 开发和独立280源Rebuild均零错误、38/39既有警告；独立修前15项复现/回归、修后19项通过、final-audit PASS。3移动方法及Collector消费方法原文抽取，薄边界不当原生；主569条编译/运行/保护输入SHA全一致，7保护文件保持。仅Update有本轮业务差异。
- 主普通脚本编译发生在用户已退出Play后，末稿loaded Update IL包含Root消费和SetMoving，控制台0错误；主没有停止Play。最初30秒正式只读采样静止，未捕获施法，不计复现。
- 原生隔离正式角色/Animator/Collector/MMove/CharacterController：40帧采集4.999855米，实际4.999854米，误差9.536743E-07米，技能后20帧位移0且移动状态false。Edit模式显式调用真实OnAnimatorMove，目标缓存和施法用反射装配，不代表完整技能链/服务器节拍或联机。
- 原生首两次失败分别为Edit未触发回调、Rebind后实体/原生起点不一致，失败结果保留；仅修临时夹具，末次统一起点后通过。反射首次GetMethod重载歧义仅工具查询失败，改为Update(int)后成功，没有业务异常修复。
- 正式记录更新：Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md。分析：项目分析/SKILL_ROOT_MOTION_SYNC.md及项目分析/SKILL_ROOT_MOTION_REMOTE_SYNC.md；职责日志：项目修改日志/2026-10-13-SKILL-ROOT-SYNC-CLIENT-CHANGELOG.md、项目修改日志/2026-10-13-SKILL-ROOT-SYNC-INDEPENDENT-REVIEW.md。
- 未解决与交接：正式双客户端画面/安装包未验，按原流程重新编译热更DLL并更新资源后验施法。没有发布热更、重启服务、保存正式场景或清理_temp。
- 清理专项补充：原生夹具在Edit调用world.Dispose，间接触发生产Play清理Object.Destroy的1条错误。正式业务保持；仅改临时清理先DestroyImmediate再Dispose。独立真实MGameObject清理验证错误数1→1、新增0，旧消息留存，不能称最终控制台0错误；原生根运动断言结果不受清理日志影响。证据root-preview-cleanup-error.txt、root-preview-cleanup-result.txt。
