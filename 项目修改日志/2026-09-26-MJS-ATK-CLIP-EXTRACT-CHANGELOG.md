# 2026-09-26 MJS 攻击/技能 FBX 动画剪辑批量提取独立 .anim

## 修改内容

- 新增 Editor 工具脚本 `LYF_MMO/Assets/Editor/MjsAtkClipExtractTool.cs`：
  - 菜单项 `Tools/动画/批量提取MJS攻击技能剪辑`。
  - 第一步：对 `Assets/GM_Game/Animas/Role/MJS/Atk/` 下 6 个 FBX，用 `ModelImporter.clipAnimations` 把第一条自定义剪辑的 `name` 改成与 FBX 同名（`Anim_JX_Atk02/Atk03/Skill01/Skill02/Skill03/Skill04`），只改 `name`，`takeName`、`firstFrame/lastFrame`、`loopTime`、事件、mask 等导入设置全部保留；`Skill04` 的 `avatarSetup=1` 未改动。
  - 第二步：重新导入后用 `AssetDatabase.LoadAllAssetsAtPath` 加载子资产，`for` 循环过滤 `__` 开头的内部/预览剪辑，命中目标 `AnimationClip` 后 `Object.Instantiate` 复制、`clone.name` 设为目标名，`AssetDatabase.CreateAsset` 在同目录生成同名 `.anim`，等同 Ctrl+D。已存在 `.anim` 的跳过。
  - 附带一次性哨兵自动执行：工程内存在 `Assets/Editor/.mjs_extract_pending` 时，`[InitializeOnLoad]` 静态构造函数在域重载后通过 `EditorApplication.delayCall` 自动执行一次并删除哨兵（本次即用该方式触发，因当前会话无法直接点击 Unity 菜单）。

## 新增产物（`Assets/GM_Game/Animas/Role/MJS/Atk/`）

- `Anim_JX_Atk02.anim`（约 873 KB，32902 行，130 个人类肌肉 FloatCurve，3475 关键帧）
- `Anim_JX_Atk03.anim`（约 1.42 MB，53132 行，5722 关键帧）
- `Anim_JX_Skill01.anim`（约 1.42 MB，53249 行，5735 关键帧）
- `Anim_JX_Skill02.anim`（约 1.66 MB，62375 行，6749 关键帧）
- `Anim_JX_Skill03.anim`（约 1.77 MB，66335 行，7189 关键帧）
- `Anim_JX_Skill04.anim`（约 612 KB，23195 行，135 条 FloatCurve，2389 关键帧）
- 每个 `.anim` 均自动生成同名 `.anim.meta`（188 字节，标准 AnimationClip meta）。
- 原有 `Anim_JX_Atk01.anim` 保持不动，未重复生成。

## 设计思路

- 严格复用 `Anim_JX_Atk01` 已验证的两步流程（先规范 FBX 内剪辑名，再复制为独立剪辑），保证产物结构与 Atk01 一致。
- 只改 `clipAnimations[0].name`，通过 `WriteImportSettingsIfDirty + ImportAsset(ForceUpdate)` 写回，避免触碰 `avatarSetup`、人形骨骼映射、帧范围等易错设置。
- 提取时过滤 `__` 前缀剪辑，防止误取 Unity 预览/内部剪辑。
- 脚本放在 `Assets/Editor/`（Editor-only 程序集），不进入业务 asmdef，不污染正式业务脚本。

## 验证结果

- 文件系统：目录下共 7 组 `同名 .FBX + 同名 .anim`（含原有 Atk01），6 个新 `.anim` 与 `.meta` 全部生成。
- 内容抽查：`Atk02/Skill01/Skill04` 文件头 `m_Name` 均正确；每个新 `.anim` 均含 130（Skill04 为 135）个人类肌肉 FloatCurve 块，关键帧数随各剪辑时长不同（2389~7189），证明是各 FBX 实际剪辑数据，非 Atk01 复制。
- FBX.meta：6 个剪辑 `name` 已改为与 FBX 同名，`takeName` 全部原样保留；帧范围与任务表一致（Atk02 1048-1284、Atk03 1700-1999.9999、Skill01 0-93、Skill02 0-412、Skill03 0-380、Skill04 0-34）；`Skill04` avatarSetup=1，其余=2。
- Unity Editor.log：脚本编译通过，无 `error CS` / 异常，日志末尾打印“MJS 攻击/技能剪辑批量提取完成”。

## 未解决问题 / 交接

- 本次因运行环境无法用 computer_use 直接点击菜单，改由哨兵文件 + `[InitializeOnLoad]` 自动执行；菜单 `Tools/动画/批量提取MJS攻击技能剪辑` 仍保留，后续可手动复用。
- 未在 Scene 中实际播放验证动画表现（仅确认资产导入与曲线数据完整）；如需可后续在 Animator/预览窗口核对。
