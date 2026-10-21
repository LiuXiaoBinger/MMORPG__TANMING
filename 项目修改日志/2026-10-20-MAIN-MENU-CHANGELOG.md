# 左上可收缩功能菜单修改记录

- 用户需求：将设置按钮纳入类似一梦江湖的可收缩菜单，明确位置为左上，菜单含背包、设置等入口。
- 本轮范围：默认收起，展开显示已有背包、角色、技能、设置四入口；使用现有匹配图标，沿现有UI模式接入；不实现设置效果或新增业务页面。
- 角色：main_menu_client负责正式客户端代码和资源，main_menu_review负责独立审查/编译；主Agent负责协调、验收与协作记录。
- 文件范围：MainView.cs、MainCtrl.cs、UIRoot.prefab及UISprites/MainMenu目录meta/4裁图/4meta共12正式路径，冻结清单`_temp/main-menu-integration-20261020/client/final-sha.json`。四入口、展开状态和显示职责按项目现有View/控制器分工处理，不另建UI框架。
- 设计取舍：左上实际位置应避开角色HUD；原背包快捷键对应行为（含镜头）复用原流程，设置窗口保留。用户截图未呈现可辨识按钮位置，已文字确认“左上”。
- 验证：开发及独立281源均0错38既有警告，独立24项审计通过；引用、范围、中文注释与布局目检通过。主冻结10项无关输入保持，中间产物统一保存在`_temp/main-menu-integration-20261020`。
- 未解决事项：正式Play/包端测试按实际条件记录，不能将隔离预览称正式运行通过；不启动或停止用户Play，不发布或覆盖正式DLL。

## 实际实现与取舍

- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`：序列化开关/入口容器/提示与四按钮；容器状态即显示状态，不增加重复状态或管理层。初始化收起、展开提示“收起 -”，收起提示“功能 +”。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：五个按钮事件成对注册/注销；背包/角色/技能复用原B/I/L处理，保背包和角色镜头调用；设置沿ShowMainWindow，点击入口先收起菜单。
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：距左上24参考像素，108×38开关，下方344×92四入口；原角色HUD实际位于底部，所以无需移动HUD。原设置入口对象迁入展开组，原设置窗口保持。
- `LYF_MMO/Assets/GM_Game/UISprites/MainMenu`：从项目现成ExtractedMenuUi裁出背包/角色/双剑/齿轮，中文由现有字体绘制，不混用原图不同业务名。仅清新裁图3红/22黄边缘像素，原图和GUID保持；四裁图/meta与目录meta共9路径。
- 图标原图/裁切/新图SHA和GUID见`_temp/main-menu-integration-20261020/client/icon-sources.json`；专题`项目分析/MAIN_FUNCTION_MENU_UI.md`说明坐标、适配、职责和验证边界。

## 独立验收与运行边界

- 1085路径审查基线仅3既有路径变化、新增9资源路径。旧5472对象全保留、5465原块逐字保持；7必要旧对象/33字段变化、58新对象，设置正文2304对象逐字保持；无关10输入与4原图保持。
- 独立编译281源0错38警告，新增警告签名0；独立24项PASS。五按钮监听成对、原B/I/L方法不变；新增代码无namespace/三目/foreach/超长条件，无业务泛化或新设置效果。
- 主独立原生读取正式菜单：默认内容false、5按钮/4对应sprite、0缺脚本；当前主城Play/dirty=false/非编译、无本轮预览根残留，末Console0错误警告。原会话404后仅在本轮_temp重新标准初始化，成功读取，无服务配置修改。
- 隔离同框两图已主/审查目检：左上菜单与底部HUD、右上小地图不重叠。预览移除克隆业务组件且手动切显隐/文字，只能作为布局证据。首预览脚本missing变量初值0不当计数，后由正式源对象真实GameObjectUtility计数与主复核替代。
- 当前运行MainView/MainCtrl没有本轮新方法；未停Play/刷新脚本/保存场景。代码及资源已完成，正式点击待正常结束运行、编译并重载UIRoot后验。包端需沿既有流程配套更新GM_Game和UIRoot资源，本轮无构建发布。
- 独立角色日志`项目修改日志/2026-10-20-MAIN-MENU-CLIENT.md`、`项目修改日志/2026-10-20-MAIN-MENU-REVIEW.md`，主汇总`_temp/main-menu-integration-20261020/root-final-validation.json`。这是常规UI整合，本轮无需新增面试难点案例。
- 主末次汇总15项全通过：12正式路径、两方281编译源SHA、10保护输入、原生源菜单与Console等证据匹配，限定源码差分空白检查通过。协作四入口、项目分析/README.md、专题及根日志已收尾；同时将`项目分析/RO_SETTINGS_UI_INTEGRATION.md`的查看入口更新为左上“功能 + → 设置”，避免旧说明仍指向右上。
