# PlayerInputCtr 30 帧移动检查变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`

## 实现内容

- 增加移动检查间隔常量，设为 30 个 Unity `Update` 渲染帧。
- 增加帧计数器；每帧累加，达到 30 帧后执行一次移动输入检查并清零。
- 未改变 `Movement` 的实时读取，也未改变 Input System 的按键和移动动作回调。

## 验证结果

- `dotnet build GM_Game.csproj --no-restore --configuration Debug --output LYF_MMO/_temp/input-30-frame-build`：0 个错误，41 个既有警告。
- 未执行 Unity Play Mode；移动事件发送代码仍是原有占位注释。
