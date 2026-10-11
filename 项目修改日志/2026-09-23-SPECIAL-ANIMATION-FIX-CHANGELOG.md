# Special 动作动画配置修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MActionComponent.cs`

## 修改内容

`MActionComponent.ResolveClipName` 增加 `MAnimator.STATE_SPECIAL` 分支：优先读取 `PresentTable.FreeStyleAnim`，为空时读取 `PresentTable.OpenAnim`，两个字段都为空时使用 `Assets/artres/Resources/Anims/Default/Special.anim` 通用槽位，不借用 Idle。动画路径仍由 `MAnimationMgr` 和 YooAsset 校验。

## 验证结果

- `GM_Game.csproj` 构建 0 个错误，保留项目既有 39 个警告。
- 代码审核通过，未影响 Idle、FIdle、Walk、Run、Jump 分支。
- Unity `New Scene` 清空旧日志后运行约 3 秒，控制台错误 0、警告 0，未再次出现 Special 配置错误。

## 后续排查

如果特定角色仍回退待机，检查对应表现表字段和 `animInfo.txt`/YooAsset 清单中的动画 Location；不要在动作组件中直接复用 Idle。
