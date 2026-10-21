# 技能 HUD 布局与背包蓝白视觉（2026-10-11）

- 需求：技能HUD结构参考用户提供的一梦江湖武当截图，样式使用本项目背包的二游蓝白、圆角格子、半透明背景；复用本项目资源，不复制参考游戏素材。
- 拟修改文件：RoleCurrInfoWindow.cs、UIRoot.prefab角色HUD子树、SkillSlotWidget.prefab，具体以开发末稿为准。主只协调与记录，客户端由skill_hud_develop实施，skill_hud_review独立审核。
- 设计：左右两组技能槽、中央放大R、下方生命/法力；保持Q/E/R/F/1..6绑定与真实数据，空位只作视觉占位。背包样式作为资源来源，不改背包、服务器、技能生命周期或CD业务。
- 验证准备：保存before，编译与引用检查、点击/键/CD显示、多分辨率和Unity隔离渲染；不在正式场景挂测试组件，不覆盖用户场景。不采用历史测试作为本轮证据。
- 临时证据 `_temp/skill-hud-layout-20261011/`；分析放 `项目分析/`，协作入口记录Docs/AI/CURRENT_TASK.md与TEST_REPORT.md。当前进行中，实际文件/验收/未解决问题结束追加。

## 完成结果

- 修改源码 `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleCurrInfoWindow.cs`：只对R选择中央样式、将实例挂到同名按键锚点。修改 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` 的角色HUD子树和 `SkillSlotWidget.prefab` 的蓝白圆角/内缩图标/键/CD视觉；新增同目录 `SkillCoreWidget.prefab` 与meta，仍同原技能槽组件，不新增业务类型或管理层。
- 布局为左Q/E/F/1/2、中央原R、右3/4/5/6＋空格，下沿真实HP/MP、上方原头像昵称职业。沿用背包Common1与原字体，装饰和槽Graphic不拦世界点击，血蓝Slider只显示。背包和其他UIRoot窗口块完整字节保持，原技能输入/绑定/CD/血蓝更新源、本机设置及服务器/表保持。
- 正式分析新增 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md`，四协作入口已同步本轮范围、验收和交接。主协调/验收/记录，开发由skill_hud_develop、独立审核由skill_hud_review实施。
- 验收：开发/独立各272真实源Rebuild0错/37旧，两报告/audit/manifest PASS。主568项SHA全一致（560正式输入/合同＋8视觉图）；6469前置路径保护6466保持、仅3授权旧文件变化，两授权新资产，无删除越界。十锚点/四序列化字段/GUID/fileID检查通过，HUD外块相同，工程before字节保持。
- 五比例1920×1080、1280×720、1920×1200、2560×1080、3440×1440，独立几何无溢出并目检开发原生Unity渲染；主普通与CD裁图目检通过。Q/R临时半填遮罩及2.5样例数字可辨，参考截图局部效果见本轮develop/hud-detail.png。没有将已有298技能断言称为本轮行为测试。
- 首稿素材误选暗菱形已返工为Common1圆片21300192＋原生Mask；隔离漏TMP Settings导致无文字，补原资源恢复，正式字体未改。临时包测试依赖/重复GUID与独立数字名称解析只修隔离工程/夹具，失败日志保留，不迎合临时夹具改业务。
- 边界/交接：原生截图只验证末稿样式与样例CD外观，业务脚本仅在临时复制品移除，未真实登录服务器/现场施法或打Bundle。编辑器默认EditorSimulateMode支持现资源；安装包按原流程同步热更代码与资源。无剩余已复现阻断，不覆盖正式场景、不挂测试组件、不清_temp。
