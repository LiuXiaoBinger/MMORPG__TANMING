# HighlightManager 空引用与 MainRoleCtrl 兼容恢复

- 日期：2026-09-24。
- 需求：修复 `HighlightManager.Update()` 空引用，并保留 `MainRoleCtrl` 的其他功能；旧控制器不得重新接管新场景实体的移动和跳跃。
- 修改文件：`LYF_MMO/Assets/GM_Game/Plugins/HighlightPlus/Scripts/HighlightManager.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`。
- 根因：本地主角预制体有 `HighlightManager`，没有 `RoleCtrlBase/MainRoleCtrl`；高亮管理器直接写空的 `_mainRole._targetRole`。
- 修复：高亮管理器统一解析当前对象或全局主角控制器，并在选中、NPC 对话、取消选择前判空；主角模型绑定时补建或复用 `MainRoleCtrl`，初始化角色基础数据并绑定 `PlayerEntity`。
- 职责边界：`MainRoleCtrl` 继续承担技能、选中和界面兼容；活动 `PlayerEntity` 下，`RoleCtrlBase.Update/OnAnimatorMove` 的既有早退禁止旧移动、重力和根运动，跳跃仍由新实体组件负责。

## 独立审查

- 空引用链复核通过：角色选择、NPC 对话和取消选择均不再直接解引用可能为空的 `_mainRole`；`NpcCtrl.OpenTalk()` 对未就绪主角也有空值保护。
- 兼容职责复核通过：动态补齐的 `MainRoleCtrl` 保留 `Awake/Start/UseSkill`、UI 快捷键、选中及相机相关入口，并绑定活动 `PlayerEntity`。
- 运动权威复核通过：活动实体下旧 `Update` 和 `OnAnimatorMove` 均在位移前返回，未恢复旧移动、重力、根运动或跳跃入口。
- 独立构建命令：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/highlight-mainrole-review-build --verbosity quiet`；结果为 0 个错误、38 个项目既有警告。
- 修改范围约束检查与 `git diff --check` 通过。
- 未执行 Unity Play Mode：当前 Unity MCP 不可用，Windows 桌面连接因 Codex 认证错误无法取得 Unity 窗口，因此本次不能记为运行验收通过。
- 验证：客户端构建成功，0 错误、38 个项目既有警告；目标文件空白检查通过。
- 未解决：当前未执行 Unity Play Mode；需要点击角色、NPC 和空白区域确认高亮选择与控制台无异常。
- 相关记录：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。
