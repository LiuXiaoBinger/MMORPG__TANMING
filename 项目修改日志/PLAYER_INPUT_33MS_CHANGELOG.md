# PlayerInputCtr 33 毫秒移动检查变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`

## 实现内容

- 使用 `MovementCheckIntervalSeconds = 0.033f` 表示约 33 毫秒、约 30Hz 的检查周期。
- `Update` 累加 `Time.unscaledDeltaTime`，未达到周期时返回。
- 达到周期后通过 `while` 扣除周期并检查 `Movement`，保留超出时间，避免低帧率累计误差。
- `OnEnable` 和 `OnDisable` 清零时间累加器，避免跨生命周期携带旧计时。

## 验证结果

- `dotnet build GM_Game.csproj --no-restore --configuration Debug --output _temp/input-time-final`：0 个错误，37 个既有警告。
- 未执行 Unity Play Mode；移动事件发送体仍为原有占位注释。
