# 2026-10-20 主菜单开关改RO箭头与透明背景：客户端记录

## 需求及修改范围

用户指向左上“收起-”按钮，要求换箭头图标、背景透明。本次仅改该开关，不改四入口及入口组底板，不扩展设置业务、动画或持久化。

- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/MainView.cs`：Text字段换为箭头RectTransform，SetFunctionMenuVisible沿原显隐流程将箭头置0/180度，移除文字赋值。
- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：原Label节点改为Arrow Image，复用RO现成箭头，原开关Image的alpha改0，保留108×38射线区；箭头显示28×20、保持比例、不拦截射线。
- `项目分析/MAIN_FUNCTION_MENU_UI.md`：更新当前开关外观、接入字段、来源及测试边界；初版文字按钮说明归为历史。

## 设计与来源

采用RO `Common.png` 中的 `UI_Common_Shouzhanjiantou_03.png`，GUID `22781455b3685ea4bae5d7ef77d885ea`、fileID21300460、源20×14。源图朝下，收起时0度，展开时旋转180度朝上；PNG/meta与RO原件逐字一致，不新增图片或图集。

保留原Label对象及组件身份，将UGUI Text改Image；View直接持有其RectTransform，没有新增类型或事件。Ctrl及B/I/L、设置入口逻辑保持。alpha0的原背景Image仍作为按钮targetGraphic接收整个108×38区域的射线，箭头Image不抢点击。

## 开发验证

- 修改前保存MainView、Root完整字节，位于 `_temp/main-menu-arrow-20261020/client/MainView-before.cs`、`UIRoot-before.prefab`；开发末稿SHA在 `client/final-sha.json`。
- 完整281源Rebuild 0错误、38既有警告，日志及输入见 `client/final`，末次输入SHA一致。
- 源只做6项定点文本替换，原CRLF90处、LF125处均保持。Root5530对象均保留，仅5必要块变化、其余5525块逐字保持；四入口图标、底板及组背景保持。
- 原生隔离展开/收起预览通过，局部图为 `client/preview-expanded-crop.png`、`preview-collapsed-crop.png`；未保存任何正式场景或预制体。
- 原生GraphicRaycaster在收起和展开两态分别验证中心、近左上和近右下，共6命中均true；背景alpha0、raycastTarget=true，按钮108×38，箭头不接射线；源默认收起且正式菜单缺脚本0。证据 `client/preview-result.json`。

## 验证边界与交接

本次执行时用户已再次进入主城Play，新箭头字段在已加载程序集为false。没有停止Play、强制刷新脚本或调用旧MainView的新字段；预览仅对正式菜单克隆设置显隐和角度，不能作为正式新方法点击或双端/包端验收。原生主城dirty在预览前后均false；不处理此前Boot内存dirty标记，不保存正式场景，不构建发布。

末态只读结果：主城Play、dirty=false、非编译，旧运行程序集箭头字段false，预览相机/画布残留0，Console0错误/警告；见 `client/final-state-result.json`、`console-final.json`。

独立审查通过，无需返工：281源0错误、38既有警告、无新增警告签名，21项检查全真；仅MainView和Root变化，四入口/组背景及设置2304对象保持，引用闭包与RO箭头同源检查通过。日志见 `项目修改日志/2026-10-20-MAIN-MENU-ARROW-REVIEW.md`。

早期候选`UI_Main_Arrow_Down_01.png`未用于正式资源，候选证据保留为 `client/main-arrow-initial-candidate.json`；实际选择以 `client/arrow-source.json`、`change-details.json`和正式Root为准。协作入口由主Agent统一更新。正常结束当前运行、完成脚本编译并重新加载UIRoot后再从正式入口查看；本轮不主动执行该切换。
