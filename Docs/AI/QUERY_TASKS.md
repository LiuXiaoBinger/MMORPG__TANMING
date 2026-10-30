# 查询索引：仅追加新的持久证据

## [INDEX][协作][QUERY-INDEX] 归档路由

- `项目修改日志/协作记录归档/2026-10-26/INDEX.md` 保留全部原查询标题/编号及准确快照行段。只按主题/编号搜索，不在启动时加载查询历史。历史结论保留原日期，并未针对当前条件重新验证。
- 追加前应用 `.agents/skills/task-log-policy/SKILL.md`。本次未仅为记录整理过程而新建查询记录，参见 CURRENT_TASK 和本任务修改日志。

## [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027] 复制过来的柱子在编辑态为什么看不到烘焙光图

- 这是 `RO-MOSHENTA-MIN-ENV-20261027` 的只读后续查询，不是开发轮次，也没有业务改动。选中的柱子 `sc_dungeon_moshenta_building_002 (1)`，本地坐标 `(72.987, 79.1624, 589.11035)`，预制体实例 `150537529`／源预制体 GUID `e12c4a666ffc9f54dbb6f2ebc6fba78d`，与截图一致。
- 源绑定渲染器 `117451335` 与本项目副本渲染器 `150537531` 都是 index `1`，scaleOffset 约为 `(0.04050754, 0.04050754, 0.31588426, 0.7874021)`；两侧差异只是 float 序列化舍入。
- 主 Agent 独立对比了上述 YAML 记录与实际编辑器：`scaleInLightmap=0.1`、`Receive GI=Lightmaps`、`LightmapSettings.lightmaps=0`、`renderer.lightmapIndex=-1`、组件 `_applied=False`、`Cast Shadows=Off`、`Receive Shadows=False`、场景 `dirty=True`。所以旧烘焙绑定确实在组件数据里，只是当前没有注入编辑态的 `LightmapSettings`；**可编辑的烘焙设置**与**Baked Lightmap 结果显示**是两件不同的事。
- 源美术烘焙场景是同实例 `Cast Shadows=1`、`Receive Shadows=1` 且带 `LightingDataAsset`；源发布场景与本项目副本是同实例 `0/0` 且无 `LightingDataAsset`。截图那套设置与美术烘焙场景一致，但仅凭图片不能证明它当初打开的是哪条路径。
- 现组件只消费 RO 已烘好的数据；重新烘焙并把新的映射采集回来是另一条编辑器工作流，本项目未实现。组件只有显式右键菜单「应用环境配置」这一条入口；**不要**把「停用这个非 `ExecuteAlways` 组件」当作编辑态还原回调来推荐。
- 本次后续查询**没有**执行 Apply／保存／Play／切场景，因为场景里已有用户未保存改动。
- 主 Agent 实测为 **1 盏灯**，否掉了「当前场景 0 盏灯」的说法；也没有断言「持久预览必须靠落盘序列化」，没有断言「未被引用的 EXR 是历史残留」。
- 实际 `requestModelId` 为 `deepseek-v4.1-flash`，在原生会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0` 中独立观察到。原生传输在产物文件写完后断开，核心结论由实际文件加独立核验接受，**没有**成功的最终 RPC 回执。
- 未修改任何业务文件。证据：`_temp/ro-lightmap-inspector-query-codex-accept.json` 与 `_temp/ro-lightmap-inspector-query-review.md`；原报告中被否决的断言以验收记录为准。完整后续说明见本次环境任务修改日志。


## [QUERY][副本组队][DUNGEON-TEAM-20261029] RO 地图绑定与真实队伍 UI 来源

- RO 只读证据位于 F:/UnityPro/ro/Ro_cs422nb/config/Table/CSV/：ThemeDungeonTable.csv 第 9 行，theme 7 -> 剧情 4060 / 普通 4061 / 困难 4062 / TeamTargetID 2007；DungeonsTable.csv 第 404 行将 4060 映射至 SceneID 4060；SceneTable.csv 第 560 行将 4060 映射至 sc_dungeon_moshenta_001/sc_dungeon_moshenta_001，StartPos=132.97=58.89=60.95。本项目加载的场景为 LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity。
- UI 来源位于 clientproj/Assets/artres/Resources/UI/Prefabs/：QuickTeamPanel 是 HUD，Team1 是主窗外框，TeamMember 是其内容。ThemeDungeonTeam 是进本确认窗口，不是建队 UI。复制的 16 个 ROTeam 资源、依赖和精确源/目标清单见完整修改日志中的引用。
- Unity 实际检查纠正了此前推断：内置 UGUI 程序集 GUID f70555f144d8491a825f0804e09c671c 即使 GUIDToAssetPath 为空，仍可解析 Image/Text。有效性以实际 Missing Script 数量为准，不能仅依据 Assets 范围内的 GUID 扫描。MoonClient 组件已剥离；一个旧 TeamInvite 标题组件确实缺失，现已替换。源 Team1 的内容初始为 inactive，移植的组合窗口需显式激活它。
- 调查与失败证据保留在 _temp/dungeon-team-20261029/research.md 和 research-addendum.md；最终事实与边界：项目修改日志/2026-10-29-副本组队与RO界面迁移_CHANGELOG.md。
