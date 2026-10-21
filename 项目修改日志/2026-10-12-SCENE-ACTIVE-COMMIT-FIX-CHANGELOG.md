# 创角场景活动状态误判修复

日期：2026-10-12。状态：代码修复与独立验证完成。

修改文件：`LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`；协作入口`Docs/AI/CURRENT_TASK.md`、`TEST_REPORT.md`、`PROJECT_CONTEXT.md`、`DECISIONS.md`；分析`项目分析/SCENE_ACTIVE_COMMIT_FIX.md`及本记录。

修改内容：CommitLatestRequest先判断目标场景是否已经为Unity活动场景，只有尚未活动才调用YooAsset.ActivateScene，保留原真实失败处理。开发由scene_active_fix执行，scene_active_review独立审查；主只协调、验收与记录。

设计依据：Unity自动把卸载旧登录场景后的唯一剩余创角场景设为活动；重复SetActiveScene返回false。原代码把已经成功的活动状态误判失败，阻止业务根启用。实际YooAsset接口是SetActiveScene包装，无需重写加载挂起、快照、旧场景清理或增加重试。

验证：独立Unity PlayMode复现自动活动和重复设置false；核provider提前场景值与加载后路径场景一致，否定错快照假设。开发272真实源编译零错/37旧警告，主独立272编译输入SHA一致，精准差异仅指定激活条件适配，其余业务字节保持。首次Edit实验未保存旧空场景导致拒Additive，只修临时实验场景后重验，保首日志不算通过。

证据`_temp/scene-active-fix-20261012/develop`和`review`。未修改正式场景或发布包，未运行用户正式服务器登录链；后续在项目停止当前运行、编译完成后重新登录进入创角验证。安装包需同步新GM_Game热更DLL及对应资源包。本轮无已复现剩余代码阻断，不清临时证据。

独立审查已封存review-report.md，结论PASS；fixed实验在真实PlayMode验证提取的相同条件两分支，未直接调用真实SceneMgr提交方法。原生复现、条件验证与编译均通过，完整正式场景调度仍以上述现场回归为准。
