# 创角场景加载成功后激活误判

日期：2026-10-12。用户报告Scene_CreateRole加载请求在CommitLatestRequest失败，原因为“已加载场景无法设置为Unity活动场景”。

## 原因与证据

SceneMgr使用Additive加载目标场景，在旧场景卸载完成后提交新业务根。YooAsset1.5.7的SceneOperationHandle.ActivateScene实际调用Unity的SceneManager.SetActiveScene。

独立Unity2023.2.20f1c1 PlayMode实验复现：创角场景加载完成后卸载旧登录场景，Unity自动把唯一剩余创角场景设为活动场景；此时再次SetActiveScene返回false。原SceneMgr把false一律当成失败，即使目标已经是活动场景也调用FailRequest，导致业务根未启用。

原生实验同时核对YooAsset provider提前取得的场景值：Additive开始后最后场景确为创角场景，完成时与按资源路径取得的Scene为同一handle。DontDestroyOnLoad场景不进入sceneCount；本次没有错误场景快照证据，因此不重写provider或场景查找。

## 最小修复

唯一业务文件`LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`，仅CommitLatestRequest先比较GetActiveScene与已加载目标快照；已相等直接进入原提交逻辑，否则调用原ActivateScene并保留真正失败处理。沿原旧场景卸载、业务根启用、回调和请求职责，不新增状态、重试或框架。

## 验证与交接

272客户端真实源隔离编译零错误、37条既有警告；主核编译输入SHA全部一致。业务差异为激活条件和对应中文说明，其余文件字节保持；独立原生PlayMode确认自动活动和重复设置false，并完成最小差异审查。

证据`_temp/scene-active-fix-20261012/`，修改日志`项目修改日志/2026-10-12-SCENE-ACTIVE-COMMIT-FIX-CHANGELOG.md`。没有操作用户当前PlayMode、登录服务器或发布新包；正式项目停止当前运行、等待编译后重新登录进入创角作为现场回归。安装包另需重生成热更DLL并打配套资源。
