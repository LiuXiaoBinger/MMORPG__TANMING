# 动画索引资源与 MAnimationMgr 加载变更

## 修改文件

- `LYF_MMO/Assets/GM_Game/Resources/ResInfo/animInfo.txt`
- `LYF_MMO/Assets/GM_Game/Resources.meta`
- `LYF_MMO/Assets/GM_Game/Resources/ResInfo.meta`
- `LYF_MMO/Assets/GM_Game/Resources/ResInfo/animInfo.txt.meta`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimationMgr.cs`
- `LYF_MMO/GM_Game.csproj`

## 实现内容

- 新增 RO 同款二维 JSON 动画索引，覆盖 `Animas/Role/MJS/Common/ALL` 下 12 个动作。每行按 `[YooAsset Location, 时长秒, 循环标记]` 保存，时长和循环标记直接来自对应 `.anim` 的 `m_StopTime`、`m_LoopTime`。
- `MAnimationMgr.Preload(PreloadCallback)` 使用 YooAsset 异步加载 `Assets/GM_Game/Resources/ResInfo/animInfo.txt` 的 `TextAsset`，解析成功后建立 `clipName -> Location` 和 `Location -> ClipInfo` 映射；未使用 `Resources.Load`。
- `ClipInfo` 的引用归零后加入 LRU 空闲队列，不立即释放资源；空闲缓存沿用 RO 的 64 容量、32 条批量淘汰、60 秒周期语义。`UpdateCache()` 可由主循环显式驱动，保留/释放剪辑时也会惰性检查。
- `GM_Game.csproj` 已登记新增文本资源；Unity 文件夹和文本资源均已提供 `.meta`。

## 验证结果

- 已逐条比对 12 个 `.anim` 源文件与 `animInfo.txt`：Location、`m_StopTime`、`m_LoopTime` 全部一致。
- 已执行 `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/animinfo-resource-build`，结果为 0 个错误、43 条工程既有警告。
- 已静态扫描 `MAnimationMgr.cs`：未新增 `namespace`、`foreach`、三目运算符或弱类型业务载荷。

## 未解决问题

- 尚未在 Unity Play Mode 中验证 YooAsset 清单是否已收录新增的 `animInfo.txt` 和 12 个 `.anim` Location；需要由启动流程在资源包清单更新完成后调用 `MAnimationMgr.singleton.Preload(...)`，再执行首次动作覆盖验证。
- `Docs/AI/CURRENT_TASK.md` 与 `Docs/AI/TEST_REPORT.md` 含非 UTF-8 历史字节，`apply_patch` 分别在字节偏移 32 和 315001 处拒绝读取。为避免整文件转码破坏既有记录，本次未直接改写这两个文件；本变更说明保留了应写入的任务、验证和交接信息。

## 2026-09-16 索引复核

- 已再次按 RO 二维 JSON 约束验证：索引包含 12 条记录，每条严格为 `[YooAsset Location, 时长秒, 循环标记]` 三列。
- 已逐条验证资源路径存在，索引时长与对应 `.anim` 的 `m_StopTime` 一致，索引循环标记与 `m_LoopTime` 一致；未改写已有动画索引数据。
