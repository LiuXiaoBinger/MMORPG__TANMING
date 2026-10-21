# 2026-10-20 左上功能菜单改用RO图标：客户端记录

## 修改文件与范围

- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：仅改四个入口Icon的Sprite引用，开启保持比例。四个Image块共8字段，其余5526块逐字保持，无增删对象。
- `项目分析/MAIN_FUNCTION_MENU_UI.md`：更新当前RO图标来源，保留首版裁图为历史说明。

没有修改业务源码、布局尺寸、入口中文、事件绑定、默认收起或既有窗口。没有新增PNG/图集，也未删除旧四裁图或原素材。

## 图标来源与方案

直接复用本项目原有 `LYF_MMO/Assets/artres/Resources/UI/Atlas/CommonIcon.png` 四个对应功能图标。其PNG/meta与RO原文件逐字一致；GUID为 `6cc2d0922412f38498c6300249ed9e82`。

| 功能 | Sprite名称 | fileID |
| --- | --- | --- |
| 背包 | UI_Commonicon_Icon_top_Bag.png | 21300682 |
| 角色 | UI_Commonicon_Icon_top_Role.png | 21300664 |
| 技能 | UI_Commonicon_Icon_top_Skil.png | 21300668 |
| 设置 | UI_Commonicon_Icon_top_Setting.png | 21300666 |

已先核RO Main/Lua动态加载规则：最新主界面动态入口使用IconMain，而本轮复用已存在的RO CommonIcon功能图标。保留这一差异，不虚称来自最新IconMain显示链。四图仍在原58×44 Image矩形内保持比例，白色Tint，不改原图颜色。

## 验证

- 修改前冻结Root全文及17路径SHA，见 `_temp/ro-main-menu-icons-20261020/client/UIRoot-before.prefab`、`before-sha.json`。
- 图标名、GUID/fileID、源Sprite尺寸、RO与项目Atlas/meta同源SHA见 `client/icon-sources.json`。正式Root末稿SHA见 `client/root-final-sha.json`。
- Unity只同步导入Root并读取原有Atlas；PreviewScene克隆菜单/底部HUD/小地图，移除克隆业务组件后渲染展开、收起，原图案比例及名称正确。末图 `client/preview-expanded.png`，局部 `client/preview-menu-crop.png`。正式资源没有通过Unity全量保存。
- 原生读到四个准确Sprite名、preserveAspect=True及Tint=RGBA(1,1,1,1)；源正式菜单16 Transform、5 Button、0缺脚本；末Console0错误/警告，PreviewScene已关闭。
- 未改源码，因此不重复执行上轮已通过的281源业务编译；本轮验收是资源差分、源图对应及原生显示。

## 边界与交接

本轮末态已是Boot非Play，MainView/MainCtrl的新方法已存在，未主动切换Play或刷新脚本。Boot当前dirty=True；本轮初始state资源未提供dirty字段，无法确定变脏时间，没有保存或擅自清dirty。17路径基线只有Root变化，主城场景磁盘SHA保持；Boot仅记录末态磁盘SHA `110cf354a0525fdce6048ff1d9b12ba5fc5b98d0a1346c670cf44c38f5b1541f`，不宣称有本轮前后对照。临时预览直接切克隆展示，不作为正式主城业务点击验收；本轮没有构建/发布。

临时首脚本因编码别名`utf8-sig`无法识别而中止于读取冻结清单，未修改正式文件；改为标准`utf-8-sig`后完成。末审计脚本第一次假定Boot位于GM_Game/Scenes导致读取失败，查真实路径 `Assets/Boot/Scene_Boot.unity` 后仅修临时脚本。两项均是临时工具问题，没有因此修改正式业务或场景。

独立审查11项通过：仅四个Image的8属性变化，原5530块均保留、5526逐字保持，其他16冻结路径及RO同源Atlas/meta保持；开发末审计 `client/final-validation.json` 通过。Root末稿SHA为 `acef4917b4a48e432f28c0a80ba75e3c668ecdf502908580a5dd2709ebd0dc4b`。主验收和协作入口由主Agent统一汇总。
