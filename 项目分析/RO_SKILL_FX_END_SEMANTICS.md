# RO技能特效结束时间语义核对

日期：2026-10-11。范围：只读参考源码，未改客户端业务或任何配置资源。

## 真实RO字段及执行链

参考根F:/UnityPro/ro/Ro_cs422nb。

- config/AutoGen/protobuf/local/common/pbl_skill.proto，MSkillFxData定义At/End为float、DestroyDelay为float，单位为秒，并有LoopCount/LoopCycle。
- clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs，fxListEvents按(At+LoopCycle*j)*_timeScale登记每次特效触发，不等待前实例结束，LoopCount最少一次。
- 同类playFx约714行将fxData.playTime设为(DestroyDelay+End-At)*_timeScale，由MFxMgr管理该实例实际存活。
- clientcode/CSProject/MoonClient/Effect/Fx/MFx.cs，InitTime将非正时长转DEFAULT_TIME；Init将时长写_orgTime/_destroyTime。loadFinish遇默认时长可读取Prefab上的FxTimer.FxTime。
- clientcode/CSProject/MoonClient/Effect/Fx/MFxMgr.cs，CreateFx等入口在默认playTime时可取特效表PlayTime；Update约532行对正DestroyTime扣帧秒，归零加入移除列表，最终回收实例。没有把End仅用作编辑标记。

## 与当前项目差异

当前Y作者endKeyNumber保存帧号，但普通特效预览按开始帧一次Spawn；宿主SkillRuntimeFxEvent无End/DestroyDelay，本项目普通粒子以自然尾迹收尾。上一轮ReadFx修复仅兼容合法作者区间与准确错误，不构成RO结束时长执行器。

结论：RO有真实End结束时间，并参与实例播放/销毁时长；本项目当前特效结束帧只有制作显示语义，未对齐这部分RO能力。后续实现需专门委派开发与独立审核，不能将只读核对说成已接入。当前用户只询问参考事实，本轮不修改业务或发布配置。

记录：项目修改日志/2026-10-11-RO-FX-END-ANALYSIS-CHANGELOG.md；协作入口Docs/AI/CURRENT_TASK.md和Docs/AI/TEST_REPORT.md。
