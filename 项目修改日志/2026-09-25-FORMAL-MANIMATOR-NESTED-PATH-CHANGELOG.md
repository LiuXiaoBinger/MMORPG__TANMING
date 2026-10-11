# 正式 MAnimator 嵌套状态路径修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`

## 修改内容

- 确认主城角色由 `MainCityMgr` 绑定 `Assets/artres/Resources/Controller/MAnimator.controller`。
- 该控制器使用 `Base Layer.Idle.*`、`Base Layer.Move.*`、`Base Layer.Jump.*` 嵌套状态路径，不使用旧的 `Movement/Action + Blend Tree` 作为主路径。
- 补充 Idle、FIdle、四种 Walk/Run 和三段 Jump 状态的正式嵌套路径解析。
- 动画剪辑异步准备期间先切换到当前 Controller 的原生状态，避免目标剪辑未就绪时保留上一段 Walk。
- 旧参数兼容逻辑仅在 Animator 声明 `Movement` 和 `Action` 参数时才有机会执行。

## 设计思路

逻辑状态名和 Unity 状态机完整路径分离。`MAnimator` 负责把逻辑状态转换为当前正式 Controller 的完整路径，并在覆盖剪辑加载完成后续播真实剪辑。这样 Idle 请求不会因为找不到短名称状态而落入无效兼容分支，也不会继续显示上一段移动动画。

## 验证结果

- `GM_Game.csproj` 构建通过：0 个错误，39 个项目既有警告。
- 目标文件静态检查通过：未新增 `namespace`、`foreach`、三目运算符、业务 `object` 或 `dynamic`。
- Unity Play Mode 尚未执行，仍需在编辑器中复测短按方向、快速双击和快速反向。
