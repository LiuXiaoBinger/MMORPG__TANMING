# 2026-10-20 左上功能菜单改用 RO 图标：独立资源审查

## 结论与范围

- 用户纠正图标来源，要求使用 RO 游戏的图标。独立审查结论：通过，没有需要开发返工的问题。
- 正式资源仅修改 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`。四个入口分别替换 Sprite，并开启原比例显示；菜单位置、尺寸、显隐逻辑、文字及业务入口保持。
- 本轮没有代码修改，不重复编译上一轮 281 源，不新增模拟业务测试。审查没有调用 Unity MCP，没有修改正式源码、资源或场景。

## RO 来源与功能映射

直接复用项目已有 `LYF_MMO/Assets/artres/Resources/UI/Atlas/CommonIcon.png`，其 PNG 和 `.meta` 均与 `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/Resources/UI/Atlas/CommonIcon.png` 及 `.meta` 逐字一致。共享图集 GUID 为 `6cc2d0922412f38498c6300249ed9e82`。

| 菜单入口 | RO Sprite 名 | fileID |
| --- | --- | --- |
| 背包 | `UI_Commonicon_Icon_top_Bag.png` | 21300682 |
| 角色 | `UI_Commonicon_Icon_top_Role.png` | 21300664 |
| 技能 | `UI_Commonicon_Icon_top_Skil.png` | 21300668 |
| 设置 | `UI_Commonicon_Icon_top_Setting.png` | 21300666 |

- 上述名称、内部 ID 和裁切区域直接从原图集 meta 解析，并按正式 Image 所属按钮核对，不仅按图片文件名推断。
- 原 RO `RoleAttr.prefab` 与 `SkillLearning.prefab` 也分别引用表中角色、技能图标，作为功能语义的补充证据。
- 不复制新图集，不修改原 RO 图集，不删除上一轮四份裁图或其 meta。

## 独立验证

- 审查开始冻结 17 路径，其中包括原 Root、`MainView/MainCtrl`、原四裁图及其 meta、设置关键源和主城场景；另记录 Scripts/UISprites/UIPrefabs 共 1094 路径基线。
- `python -X utf8 _temp/ro-main-menu-icons-20261020/review/audit.py`：11 项检查全部通过。当前 Root SHA256 为 `acef4917b4a48e432f28c0a80ba75e3c668ecdf502908580a5dd2709ebd0dc4b`。
- Root 共 5530 对象，无增加、删除；仅四个 Image 的 `m_Sprite`、`m_PreserveAspect` 共八个顶层属性变化，其他 5526 对象块逐字保持。没有修改任何 RectTransform、按钮、标签或 MainView 序列化接线。
- 四份 Sprite 引用的源图集、GUID、fileID、名称与对应按钮全部匹配现存子资源，原图比例开启；图集 PNG/meta SHA 保持原值。
- 1094 路径只有 Root 变化，无其他新增、删除；其余 16 冻结输入全部保持，包括原四裁图/meta、业务源码及场景。原设置窗口子树 2304 对象逐字保持。
- 菜单默认内容收起、左上布局和四入口静态接线保持上轮末稿；本轮没有改变已审查的展开收起方法、B/I/L 和设置窗口流程。
- 独立目检 `client/preview-menu-crop.png`：RO 背包、角色头盔、技能星球、设置齿轮与中文分别对应，显示正常，没有混入上轮图标。

## 边界与交接

- 图片属于开发原生隔离克隆的布局预览，不新增正式主城点击或包端验证结论。源码本轮不变，上一轮“新运行程序集生效后再验菜单交互”的边界继续保留。
- 无资源层阻断事项；由主 Agent 汇总四份协作入口。后续正常加载本轮 Root 后查看 RO 图标，正式包按既有资源更新流程生效，本轮不构建、不发布。
- 证据统一保留在 `_temp/ro-main-menu-icons-20261020/review/`，包括 `before-sha.json`、`scope-before-sha.json`、`ro-atlas-baseline.json`、`final-audit.json`；总体使用说明见 `项目分析/MAIN_FUNCTION_MENU_UI.md`。
