# 登录到主城的简单场景流程

## 需求与故障依据

用户要求恢复原项目的简单场景管理：登录成功后加载创角，进入游戏时加载主城，再执行现有界面回调。删除无业务依据的校验和管理机制。

本次真实安装包 Player.log 报错：`场景请求失败，代次=2，场景=Scene_MainCity，原因=场景 SceneBusinessRoot 未保持默认禁用`。主城业务已经启动，管理器却因默认根状态不符合人为约定而卸载主城；随后小地图访问已销毁的场景相机出现空引用。

正式主城的 SceneBusinessRoot 默认启用，创角同名根默认禁用。这是现有资源状态，不应作为拒绝进入场景的条件。登录 UIRoot 和 Global 使用 DontDestroyOnLoad；创角及主城内置 UIRoot 实例默认禁用。

## 实现范围与流程

仅改 `LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`。以提交 `51ca995` 的原文件为参考，保留 `Init`、`LoadSceneMode` 接口及原进度订阅结构。

1. 使用 YooAsset 的 Single 模式加载目标场景，旧场景卸载和目标激活由 Unity/YooAsset 完成。
2. EveryUpdate 读取实际加载进度；没有加载界面也继续等待加载完成。
3. 使用 IsDone 和 Status 处理实际加载结果。
4. 成功后启用现有 SceneBusinessRoot，并执行成功回调。

删除请求对象、代次、排队、手工激活与卸载、默认根状态门禁、重试和额外生命周期包装。不改登录、创角、主城业务、UIRoot、小地图、正式场景资源或联网热更设置。

## 验证与发布边界

末稿为80行，272真实源编译零错误，独立Unity原生场景验证28项通过；具体证据以 `Docs/AI/TEST_REPORT.md` 和 `项目修改日志/2026-10-12-SCENE-SIMPLE-FLOW-CHANGELOG.md` 为准。验证运行真实SceneMgr与UniRx，Yoo、外围业务/UI为隔离薄边界；不将隔离场景验证当作正式服务器登录或安装包验收。

SceneMgr 属于 GM_Game 热更程序集；现有安装包不会因源码修改自动更新，需要按现有流程生成配套热更 DLL 与资源。本轮不自动发布或替换运行包。
