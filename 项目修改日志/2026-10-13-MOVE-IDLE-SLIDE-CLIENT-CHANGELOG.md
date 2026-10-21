# 2026-10-13 概率待机平移：客户端公共动画修复

## 修改文件和内容

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`：InState沿RO比较已有_currentState播放目标，避免快速停走把旧过渡源Walk当作已恢复移动。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`：删除未调度回调的后摇等待分支、空killTimer和失效间隔常量，技能动作窗结束后实际提交默认动作，不再只返回状态名。
- `项目分析/MOVE_IDLE_SLIDE_ANIMATION.md`：归档RO对照、两故障事实、修改边界与验证限制。

## 设计思路

仅修已存在公共动作链。播放请求先记目标，去重检查必须使用该目标，不能用尚未结束的原生源状态代替。技能正常结束后直接沿已有默认播放入口切换，不恢复尚未实现的RO后摇定时链、不增加请求队列/确认等待/重试/状态框架。

## 当前验收

- 开发实际280份客户端源Rebuild0错误、38既有警告，编译输入和日志保留在`_temp/move-idle-slide-20261013/client/`。
- 末稿SHA：MAnimator=`3aae49f3c58e670a71d73b0df680f338b5725312a7c8e914d34cb2bb4bf78bf2`；MDefaultAction=`bb7966f721f650c67134b7d004fcccda0d1418230a5ec289076d2d521d7010ac`。末次仅清除第317行尾空格，随后重新编译280源0错38既有警告，双源`git diff --check`退出0。
- 独立before30项已复现普通/战斗、走/跑、两方向快速切换和后摇假播放；final30项通过，包括跳跃/落地保护、持续状态去重、停止和远端战斗跑速。五方法逐字抽取，实体/动画采样和基类播放为薄记录器，不能当作完整原生角色或网络验收。独立280源编译0错39既有警告；末次尾空格后的清单由主与审查统一复核。
- 原生完整角色探针曾因测试对象缺兼容控制器和临时职业表无效失败，随后用户开始构建/退出Play，不记为业务故障或原生PASS。纯原生正式控制器过渡探针随后因Unity资源构建繁忙未取得有效结果，主停止争抢Unity；临时探针已补自主保存结果逻辑，但当前没有成功结果文件，不能宣称原生复现通过。

## 未解决和交接

仍需用户实际刷新/热更后检查双客户端概率滑行；本子任务未停止用户Play、未刷新Unity或发布热更。主Agent统一更新CURRENT_TASK/TEST_REPORT，避免并发覆盖。未改输入、MMoveComponent、状态机、技能主体、服务器、协议、动画资源或场景，不清理临时文件。

前轮双账号互见唯一业务源`MainCityMgr.cs`保持SHA `75a4eb68f8660ca6f933c68e0f532baa1c5bb59bcd85be23d9db9dbd0196dea5`，不重做已验收互见修复。当前正式hotdll已包含该修复，不能继续笼统称为旧代码；本轮两个动画源是否进入用户最终资源版本仍需正式构建结果确认。
