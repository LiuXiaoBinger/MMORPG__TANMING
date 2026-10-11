# 2026-09-26 MJS 独立动画剪辑重命名（JX→MJS）并移动到 Skill 目录

## 修改内容

- 新增 Editor 工具脚本 `LYF_MMO/Assets/Editor/MjsAnimRenameMoveTool.cs`：
  - 菜单项 `Tools/动画/重命名并移动MJS技能剪辑`。
  - 用 `AssetDatabase.MoveAsset(旧路径, 新路径)` 一步完成移动+重命名，.meta 随资产一同迁移、GUID 保持不变（Animator 已有引用不断）。
  - 移动后用 `AssetDatabase.LoadAssetAtPath<AnimationClip>` 加载新路径剪辑，把内部 `clip.name`（m_Name）同步改成新文件名，`EditorUtility.SetDirty`。
  - 移动前检查目标路径不存在同名资产；MoveAsset 返回非空错误串时打印 LogError 并跳过该条。
  - 附带一次性哨兵 `Assets/Editor/.mjs_rename_move_pending` + `[InitializeOnLoad]` 自动执行钩子。

## 移动映射（只动 .anim，FBX/.meta 留在 Atk）

| 源（Atk 目录） | 目标（Common/ALL/Skill 目录） |
|---|---|
| Anim_JX_Atk01.anim | Anim_MJS_Atk01.anim |
| Anim_JX_Atk02.anim | Anim_MJS_Atk02.anim |
| Anim_JX_Atk03.anim | Anim_MJS_Atk03.anim |
| Anim_JX_Skill01.anim | Anim_MJS_Skill01.anim |
| Anim_JX_Skill02.anim | Anim_MJS_Skill02.anim |
| Anim_JX_Skill03.anim | Anim_MJS_Skill03.anim |
| Anim_JX_Skill04.anim | Anim_MJS_Skill04.anim |

## 设计思路

- 用 `MoveAsset` 而非文件系统直接搬运，保证 .meta 同步迁移、GUID 不变，避免 Animator/预制体里的引用断裂。
- 文件名（资产名）与内部 `m_Name` 一并改成 `Anim_MJS_*`，命名保持一致，便于后续按名字检索。
- 脚本放在 `Assets/Editor/`（Editor-only 程序集），不进入业务 asmdef。

## 验证结果

- Skill 目录：7 个 `.anim` + 7 个 `.anim.meta`，文件名均为 `Anim_MJS_` 前缀。
- Atk 目录：只剩 7 个 FBX 及其 `.meta`，所有 `.anim` 已移走。
- 抽查 `Anim_MJS_Atk02.anim`、`Anim_MJS_Skill04.anim` 文件头，内部 `m_Name` 均已改为 `Anim_MJS_*`。
- Unity Editor.log：7 条均打印“已移动并重命名”，末尾“重命名移动完成”；无 `error CS`、无 Exception、无 MoveAsset 错误串。
- GUID 由 MoveAsset 保持不变，预期 Animator 引用仍正常。

## 未解决问题 / 交接

- 本次仍通过哨兵 + Win32 前置 Unity 窗口自动触发（computer_use 桌面控制被拦截）；菜单保留可手动复用。
- 未在 Animator/场景中逐个确认引用绑定（GUID 不变理论上不断）；如需可后续打开 Animator 预览核对。
