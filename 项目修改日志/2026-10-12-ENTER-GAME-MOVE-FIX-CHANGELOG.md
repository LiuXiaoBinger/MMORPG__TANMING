# 2026-10-12 进入主城后移动和按键无反应

- 用户要求：修复进入游戏后移动不了、按键无反应；只处理本问题，不扩展功能或输入管理机制。
- 当前事实：最新Editor.log已进入主城并绑定本地主角；Player.log仍为早前旧包的场景根错误，不作为本次移动故障证据。PlayerEntity在创角创建并缓存该场景Camera.main，Single卸载后旧相机销毁，移动输入因相机引用为空返回。
- 客户端开发`enter_game_move_develop`和独立测试审查`enter_game_move_review`复现该链，并核主界面先创建、输入模型晚到时的快捷键注册。只修改确认必需的路径，临时证据放`_temp/enter-game-move-20261012/`。
- 正式修改仅两源：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`在缓存相机被销毁时调用原缓存方法获取主城相机；`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`在主角模型/输入创建后调用已有RegisterMainUIKeyHandler补齐技能和界面快捷键。旧MainRoleCtrl保持禁用，不增加轮询或管理机制。
- 第二问题事实：正式Role_MJS输入组件启用、MainRoleCtrl禁用；主界面早于异步模型创建，当时输入单例为空，原快捷键注册返回。旧控制器Start无法执行补注册，测试脚本的额外注册曾掩盖正式登录时序。
- 两源末稿272完整GM_Game源编译零错误、37既有警告，精确差异和主核当前272编译输入SHA一致。
- 独立审查final-audit结论PASS，无需返工；主核27原生输入SHA一致，生产输入完整字节和相机/注册方法逐字一致。PlayerEntity末稿SHA`5174362664d26e27b8eb6a974ee2fc0b8922ed0987f4daa315a8b754f24d3d17`，MainCityMgr末稿SHA`dd36305ed4044b6d81e28c66f53e66f3cc82833b58436dcd0b95ec8410a1002a`。
- 独立原生Unity/InputSystem27项通过：Single真实销毁旧相机，before移动事件零→after新相机朝向/移动事件恢复；输入晚到before缺订阅→原入口补齐技能与界面事件且不重复；真实W/Shift/Space与四方向通过。运行完整真实PlayerInputCtr及生成输入资产，相机/注册为原方法逐字抽取，外围角色/移动桥/UI薄记录；未执行完整正式MMove位移/技能结算/网络/包。MainCityMgr新增原接口调用位置静态核对。
- 首隔离测试无GameView焦点使真实键盘输入被送编辑器，18项核心通过后W断言失败；只调整_temp输入焦点设置后27项通过，无正式输入配置或时序修改，保首失败日志。
- 不发布、不关闭用户进程、不清临时。无本轮已复现剩余阻断；现场仍需重新运行进入主城确认移动及按键，安装包更新配套GM_Game热更DLL后回归。
