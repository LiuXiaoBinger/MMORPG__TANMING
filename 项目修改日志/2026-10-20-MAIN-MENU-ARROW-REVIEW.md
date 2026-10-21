# 2026-10-20 主菜单箭头开关与透明按钮：独立审查

## 审查结论与范围

- 用户要求功能开关使用箭头图标、按钮背景透明。按截图限定到该开关，四个入口与展开组原背景保持；不扩大为整组透明。
- 审查通过，无需要开发返工的问题。正式只改 `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs` 与 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`。
- 审查角色只读取正式文件，独立编译与静态核查输出放本轮 `_temp`；没有调用 Unity MCP，没有修改业务、资源或场景。主 Agent 统一更新四份协作入口。

## 修改内容与设计核查

- `MainView.cs`：原文字字段替换成 `RectTransform` 箭头引用；既有 `SetFunctionMenuVisible` 在收起时置零旋转、展开时设 Z 轴 180 度。沿用容器自身显隐和原开关方法，没有新增状态、业务或管理框架。
- `UIRoot.prefab`：原 Label 组件改成 Arrow Image，引用项目已有 RO `Common.png` 的 `UI_Common_Shouzhanjiantou_03.png`；箭头保持比例显示为 28×20，原开关 108×38 热区不变。原按钮底图仅 Alpha 从非零改成 0，保留可交互与射线接收。
- 箭头子 Image 不接收射线，让父 Button 接收点击；四入口、展开组底板、位置、中文、RO 功能图标和原业务接线全部保持。
- 真实当前文件基线先保存再修改，保留 MainView 既有混合换行，没有按旧历史 SHA 恢复或统一格式化用户文件。

## 独立验证

- 独立编译命令：`python -X utf8 _temp/main-menu-arrow-20261020/review/compile_client.py final`。实际 281 源，0 错误、38 既有警告；对照上一轮完整编译没有新增警告消息签名。编译输入 SHA 与末稿全部一致。
- 独立审计命令：`python -X utf8 _temp/main-menu-arrow-20261020/review/audit.py`。`review/final-audit.json` 共 21 项全通过，冻结 1101 路径内只有 MainView 与 Root 变化，无其他新增、删除。
- Root 5530 对象无增删，5525 原对象块逐字保持。仅五个必要块变化：MainView 字段接线、Label 改名、该子节点尺寸、Text 改 Image、开关背景 Alpha。没有修改其他 RectTransform 或按钮组件。
- 开关 Image 启用、`raycastTarget=True`，Button 仍 `interactable=True`，`targetGraphic` 仍指向透明底图；箭头 `raycastTarget=False`。原 108×38 热区与按钮接线完全保持。
- 实际箭头 Sprite 的 GUID 为 `22781455b3685ea4bae5d7ef77d885ea`、fileID 为 `21300460`，名称与 meta 解析相符。项目 Common 图集 PNG/meta 与 `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/Resources/UI/Atlas/Common.png` 及 meta 逐字一致、SHA 未改。
- 原 Label 转 Image 后，变化对象的本地引用没有悬空；新外部引用均是现有 Common 图集与 Unity 原生 Image 脚本。主界面序列化引用准确切到箭头 RectTransform。
- 原 `ToggleFunctionMenu` 与 `InitView` 默认收起流程保持；`MainCtrl` 未修改，原按钮注册/释放、B/I/L 和设置窗口流程保持。设置窗口原子树 2304 对象逐字保持。
- 新字段、修改方法和既有 `visible` 参数均有具体中文说明；无新增命名空间、三目、`foreach`、`object/dynamic` 载荷或组合布尔判断。
- MainView 末稿 SHA256 为 `0ce28ce80803ab6e0c78a04e59524f0e444a53be8f72e024fc30ceac2b0ae4a4`；Root 末稿为 `71c332e2f2fc6ab9d410926b78f4e05d0f0b5045ea30352860a821d7fc2f80f8`。

## 图像与运行证据边界

- 独立目检开发 `client/preview-collapsed-crop.png`、`client/preview-expanded-crop.png`：收起为向下箭头，展开为向上箭头，原“功能 + / 收起 -”文字和该按钮深色底板已消失，四入口及组底板保持。
- 已读取开发实际 `preview.cs` 和 `preview-result.json`：原生 `GraphicRaycaster` 在两状态各测试按钮中心、左上、右下三个点，六次均命中按钮本身；透明底图仍能点击的结构证据成立。源对象真实缺脚本计数为 0，源默认内容隐藏。
- 开发当时仍在主城 Play，运行程序集没有新箭头字段；预览移除克隆业务组件、直接切克隆显隐并旋转箭头。因此两图与六次射线命中属于隔离原生布局/热区验证，不能称正式新 `SetFunctionMenuVisible`、真实主城菜单点击或包端验收。
- 审查没有启停 Play、刷新运行程序集、保存正式场景或发布。没有已发现阻断；下一步正常加载新代码和 Root 后验开关及四入口，包端按原热更与资源更新流程生效。
- 证据统一保留 `_temp/main-menu-arrow-20261020/review/`；总体使用说明见 `项目分析/MAIN_FUNCTION_MENU_UI.md`，原生开发证据在本轮 `client/`。本轮没有为小改添加新的模拟业务测试或生产防御逻辑。
