# 中央真气和气力弧条（2026-10-11）

- 用户说明并确认：中央圆盘左右另外添加真气/气力弧条，底部生命/法力长条保持。主暂定左真气、右气力，不添加文字/数值，保原中央未满暗态/满怒亮环和侧技能槽。
- 只做界面/玩法后续接入沿用既有授权；没有独立真气/气力字段，最小强类型UI接口独立设置0..1填充，无数据默认空轨道，不用MP/运动/怒气冒充，不加协议或积累消耗。
- 拟修改RoleCurrInfoWindow.cs和UIRoot.prefab中央局部，开发skill_hud_develop实施、skill_hud_review独立审查，主记录/协调。从当前未提交稿冻结before，不回退已有成果。
- 待验默认/边界与独立性、原生弧条局部和各比例/数据样例、编译/引用/原输入CD/底HPMP及用户文件保护；证据 `_temp/skill-hud-qi-stamina-20261011/`，分析更新 `项目分析/SKILL_HUD_LAYOUT_AND_STYLE.md`，完成补充末稿与未验范围。

## 完成结果

- 实际四路径：修改 `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleCurrInfoWindow.cs` 和 `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`；新增 `LYF_MMO/Assets/GM_Game/UISprites/Icon/HudResourceArc.png` 与meta。无新业务类型，两个Image字段与UpdateTrueQi/UpdateStamina各自只更新对应三分之一圆弧，max<=0为空，负值/超上限裁剪。
- UI左青蓝真气/右浅金气力，四Image为暗轨/进度各一，紧贴中央外缘，无文字/数值/世界点击拦截。内圈满怒外观、原R/CD、底生命法力长条及侧槽保持，当前无生产调用默认空，不伪造领域来源或玩法。
- 首图集厚环放大成模糊块，开发主动返工为本项目白圆派生透明细环，616px原图/154设计直径/约4单位弧宽；不改原图集、不复制参考素材，Sprite无mipmap/中心透明、GUID唯一，原UISprites收集范围可用。
- 编译开发/独立272正式源各0错37旧；独立24真实接口断言（四源＋Unity/资源薄替身）通过，不套用旧29。6474路径保护6472不变/两授权旧改/两新增无删除越界，原窗口和UIRoot剔新增后字节相同，输入/控制器/其他槽/本机设置/工程保持。
- 主/独立目检空轨、非对称部分、满弧与满怒组合通过，三组各五比例＋三裁图共18PNG，几何无溢出。三原生日志完成/退出0/无编译Shader错；主689项SHA核对全部一致，独立audit/末稿清单通过。正式分析及四入口同步本轮语义和未验边界。
- 交接看本轮develop/hud-arcs-partial-detail.png和hud-arcs-full-rage-detail.png；图片为正式末稿复制品隔离原生样例，不当作领域数据接入或服务器现场。未验真实资源积攒/消耗、正式场景施法或Bundle发布；没有生产调用方默认空，玩法后续接入，无剩余已复现阻断，不清_temp。
