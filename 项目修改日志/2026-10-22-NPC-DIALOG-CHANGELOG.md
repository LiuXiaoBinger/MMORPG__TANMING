# NPC点击无对话：文字绑定修复与现场边界

## 结论与范围

用户反馈游戏点击NPC没有显示对话。实际核对发现TalkWindow.ReFreshUI只有数据转换，没有给预制体已有标题和正文控件赋值。本轮仅修复已确认的文字未绑定缺陷；没有现场Unity/鼠标点击复现，不能报告“面板不弹现象已全部修好”。

分支codex/npc-dialog-fix，从已推送网络接口基线14a0a888850bf5ece67caa96cdf503d447d2f5a0新建，不合main。原9份用户修改及旧未跟踪产物保持；没有启停用户Unity、游戏或服务、保存场景、替GM_Game.dll.bytes、修改网络或导航。

## 最终逐项文件清单

| 操作 | 项目根相对路径 | 用途与具体变化 |
| --- | --- | --- |
| 修改 | LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/TalkWindow.cs | 添加两个私有TMP_Text序列化字段；沿既有NpcTalkViewData刷新NPC名字与对话，下划线分隔短句显示为换行；保留原非法载荷分支及商店事件，补具体中文字段/参数/边界注释。 |
| 修改 | LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab | 仅TalkWindow组件增加两行引用：正文5055306388014330071、标题2887947151108021491。复用原TMP控件、字体和层级，其他YAML块保持。 |
| 修改 | Docs/AI/CURRENT_TASK.md | 记录原反馈、确认缺陷、限定修复、开发身份、验收和现场交接。 |
| 修改 | Docs/AI/TEST_REPORT.md | 记录289源编译、10项薄边界、557编译输入SHA、预制体与原用户文件保护，明确未验现场。 |
| 修改 | 项目分析/项目难点.md | 独立记录点击到显示的分段定位、首轮Collider/文本控件误判和实证纠正、最小方案、验证边界及面试复盘。 |
| 新增 | 项目修改日志/2026-10-22-NPC-DIALOG-CHANGELOG.md | 本文归档逐项变化、开发/返工、真实验收、未解决项及Git回执。 |

无新增业务类型、无移动/删除文件；两业务文件原.meta保持，没有新目录或目录.meta。源码仅+12/-1，UIRoot仅+2/-0。临时任务单、脚本、报告和编译产物统一在_temp/npc-dialog-20261022，不提交。没有修改NPC预制体、TouchObject、导航组件、MainCtrl、View基类、配置表或正式程序集。

## 业务与设计

现有流程保持：PlayerInputCtr鼠标边沿→TouchObject实体筛选→MEvent_Navigation→MNavigationComponent→原10米三维范围→NpcCtrl.OpenTalk→MainCtrl将NpcEntity转NpcTalkViewData→MainView注册窗口→UIBase.ShowMainWindow→TalkWindow.ReFreshUI。

PlayerEntityBuilder.cs:36明确创建MNavigationComponent；NPC根CharacterController已启用，层9可参与DefaultRaycastLayers。实际NPC 4001出生点由配置60.32_19.16_95.36_90覆盖模型预制体初值；不能把预制体64.83/18.84/72.66作为运行目的地。

UIRoot已有正文tex_content与标题Image/Text (TMP)。只添加两引用与两行赋值，不新建窗口、文本控件、碰撞体或生命周期，不引入重试、门禁、队列。正常NpcManager初始化已将Name/Talk置空串后应用非空配置，本轮不增加重复判空。ReFreshUI的object参数是沿用现有WindowBase入口的必要兼容边界，内部立即转换具名NpcTalkViewData，未新增object业务载荷。

## 外部执行与失败纠正

- 豆包官方桌面本项目local环境已核对，先行只读会话38448648846393602/run59391213363817218多次无报告，恢复task_unavailable（目标导航或关闭），同会话open出现trim异常；sessions stop最终cancelled/stopped=true。没有成果，不计开发失败轮，没有改用网页或冒称完成。
- WorkBuddy官方桌面本项目Hub hub-1771073d-0d79-4fd4-89c1-a7e677b75cba，原生会话5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0、runtimeSource=desktop_queue。平台round1/run-8f1f4655-41ce-4569-ad53-7a2dbb6050e6只读预检漏计ClassID143 CharacterController，误报无Collider；也沿用源码过时注释误报无文本控件，Codex回读资产后退回，没有增加重复Collider或UI。
- 平台round2/run-d9bf55d4-ba41-48c9-bb8f-c6223423b25c纠正前述结论，确认空ReFreshUI。Codex独立解析TalkWindow子树并核对两个TMP真实身份，另纠正导航挂载和NPC运行坐标，决定仅两文件绑定。
- 平台round3/run-59f6da0c-e180-4af8-be5d-108b328b244e首次开发增加了无故障依据的引用/字段空值兜底。Codex按NpcManager正常路径证据退回，未为人造未绑定测试保留门禁。
- 平台round4是首次开发后的第1次审查返工，删除三处防御和对应两个人造用例，保留正常流程。前两轮是定位预检，不混称4轮业务失败。最终round4真实运行ID在本轮hub-wait-result.json与hub-accept-result.json；Codex核验后review=accepted/holdsCapacity=false。
- WorkBuddy负责实际业务修改与自检，Codex负责范围设计、实际差异核验、编译复测、记录和Git集成；没有由主Agent代写业务。

## 实际验收

- Codex正式插件路径编译289份客户端源文件，exit0、0错误；编译输入557项SHA与当前文件一致。不是Unity原生编译或Player发布。
- Codex在独立本轮目录复跑WorkBuddy交付夹具，直接编译真实TalkWindow.cs与NpcTalkViewData.cs，PASSED=10 FAILED=0。覆盖4001真实Name/Talk和三行文字、正常切换、重复刷新、原null/异类型载荷分支、原商店事件。Unity/TMP/WindowBase是薄替身，不能当真实UI渲染或鼠标点击。
- 普通dotnet薄边界有两个CS0649序列化字段警告；已核实UIRoot两引用确实存在。没有逐条对比全客户端警告基线，不声称零警告或全部既有。
- 独立预制体逐块比较：去掉新增两引用后与Git基线完全一致，两个引用指向现有启用/活动TMP组件，脚本meta、UIRoot.meta及NPC/输入/导航/控制器保持。
- 原9用户文件SHA保持：热更字节、两字体、客户端目录内两历史协作记录、三个UserSettings/布局、包版本；未混入提交。完整清单和哈希见_temp/npc-dialog-20261022/baseline.json及codex-validation/audit-final.json。
- 当前业务源码SHA97f996abb48d64a5167f9f01f7535a7f80112001d098ffe0f3fd575804c6a3ec；UIRoot SHA28b8b71f879556b2f878688c95a4b024336a82bb0d36893fd4d571538902dd31。最终证据_temp/npc-dialog-20261022/codex-validation/final-verification.json。

## 未解决与现场交接

无Unity MCP工具，进程查询未见Unity.exe或LYF_MMO.exe；Editor.log末次10月21日23:45，仅历史日志，没有本轮真实点击异常。用户尚未确认点击后是否移动、窗口是否弹出及运行环境，因此只能确认并修复文字绑定，面板完全不弹的实际断点仍待现场。

实际运行须重新构建同版GM_Game热更程序集并更新UIRoot资源包。用户当前GM_Game.dll.bytes已有改动，本轮未覆盖；隔离输出不是已安装游戏补丁，未做IL2CPP/HybridCLR发布。

现场验收顺序：加载同版代码与UI资源→点击4001 QuQu→确认角色到达原10米范围（已在范围内则直接开窗）→窗口显示QuQu和三句配置对话→关闭/重开与原商品按钮。如果角色完全不动，记录点击像素、UI阻挡、实际射线/导航状态；如果已到达但未弹窗，检查到达回调与UI引用。没有证据前不推断必须改NavMesh或其他模块。btn_talk原空事件未扩修。

专题复盘：项目分析/项目难点.md。暂存、提交与推送实测回执后续追加，未合main。
