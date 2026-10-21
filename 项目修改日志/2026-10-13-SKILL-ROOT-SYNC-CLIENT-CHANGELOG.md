# 2026-10-13 技能根运动远端同步客户端修改

## 需求与范围

用户反馈本地施法有根运动，另一端仅原地播放技能动画，动画结束后才移动。本职责仅修正远端根运动消费及相应影子段接续，不扩展服务器或协议。

业务文件仅 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`，修改限 `Update(int)`。新增专题分析 `项目分析/SKILL_ROOT_MOTION_REMOTE_SYNC.md`。`Docs/AI` 入口由主 Agent 统一更新。

## 内容与设计思路

- 远端也消费 `Model.RootMotion.ConsumeDelta`，与现有可选程序位移合并后只调用一次 `MoveCharacter`；没有程序组件时不再提前退出阻止根位移。
- 本帧存在技能位移时不叠加普通影子移动，维持技能运动与方向输入权限分工。
- 应用技能位移后按实际落点重算已有影子段方向、剩余距离与时钟，避免动画结束继续沿旧方向重复前移；正好抵达停止目标时收束移动态。
- 原程序位移碰撞停止、普通无技能跟随、本地运动及所有前轮修复保持。没有新增业务类型、字段、协议、表、正式场景或资源修改。

## 验证

- 开发最终 280 源 Rebuild：0 错误、38 既有警告；证据 `client/GM_Game-build.log`、`client/compiled-source-hashes.json`。
- 独立实际生产方法专项修前15项复现/回归、末稿19项通过；独立280源Rebuild零错误、39既有警告。`review/final-audit.json`确认方法外完全保持、280编译输入无漂移、四生产方法逐字一致、七保护文件保持；不把薄实体/动画边界当原生联网。
- 修改前快照 `client/MMoveComponent.before.cs.txt`；本轮精确差异 `client/MMoveComponent.delta.diff`；当前源及保护文件 SHA `client/final-business-hashes.json`。
- 仅方法范围内新增中文注释；无 namespace/foreach/三目运算或超过三项并排判断；`git diff --check` 通过。
- 原生夹具首次执行未采得有效根运动，结果保留 `client/native-root-movement-first-attempt.txt`。针对编辑器预览回调手动派发生产采集后取得40帧约5米根运动，但测试模型与CharacterController起点不一致造成109.4979米误差，第二次结果保留 `client/native-root-movement-second-attempt.txt`。仅修临时起点装配后交主最后执行，没有因此修改业务或将失败算通过。
- 第三次原生末稿通过：40帧正式Humanoid动画，生产Collector采集4.999855米、远端实际移动4.999854米，误差`9.536743E-07`米；差量已清空、到目标后移动态为false，退出技能后二十帧追加距离0、移动态false。原生结果 `client/native-root-movement-result.txt`。编辑器非Play预览不自动派发回调，夹具显式调用真实`OnAnimatorMove`；目标缓存由反射装配，未执行网络入口，不算正式双客户端联机。
- 主确认实际已载末稿`MMove.Update(int)` IL含根消费与`SetMoving`，569条编译/运行/保护SHA复核通过。没有用中间已载稿替代最终源码验收。
- 原生40帧行为通过后，控制台另留一条测试清理错误`Destroy may not be called from edit mode`：夹具先`world.Dispose`进入生产Play回收`MGameObject.Release`，再立即销毁临时模型，顺序不适用于Edit预览。仅调整临时夹具为先`DestroyImmediate(model)`再`world.Dispose`，保通过结果及该错误证据，不修改业务或清空控制台；主另做单次Edit清理小验证，不重复40帧行为测试。

## 最终源码 SHA256

- 修改前 MMove：`6C035FABD7B2E857AD06AA7101FAAD2A81F75EABC2A5BC99B33FD748695A293E`。
- 修改后 MMove：`0DB2686CC687D8BDA7616E88646C75449286C5870CA8CB6ADD128E017E70F0EE`。
- 前轮 MAnimator：`3AAE49F3C58E670A71D73B0DF680F338B5725312A7C8E914D34CB2BB4BF78BF2`。
- 前轮 MDefaultActionComponent：`BB7966F721F650C67134B7D004FCCCDA0D1418230A5EC289076D2D521D7010AC`。
- 前轮 MainCityMgr：`75A4EB68F8660CA6F933C68E0F532BAA1C5BB59BCD85BE23D9DB9DBD0196DEA5`。

## 未解决与交接

尚未验收正式两客户端施法画面与新热更包。独立专项和原生隔离结果均须注明边界，不替代正式联机。没有发布热更、重启服务器或保存正式场景。

完整证据根：`_temp/skill-root-sync-20261013`。定位分析：`项目分析/SKILL_ROOT_MOTION_REMOTE_SYNC.md`；主职责汇总：`项目分析/SKILL_ROOT_MOTION_SYNC.md`。
