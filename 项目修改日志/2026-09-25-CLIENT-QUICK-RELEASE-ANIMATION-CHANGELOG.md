# 快速松开移动动画修复

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 修改内容

`stopMove()` 清理方向输入后，如果水平运行标记已经被上一帧或不可移动状态提前清零，原逻辑不会再次触发默认状态机。恢复停止补偿分支，在当前状态为默认动作时主动标记状态机，使默认动作重新读取 `HasDirectionalInput` 并切回 Idle。

`PlayerInputCtr` 同时增加取消闸门：收到 `canceled` 或零值后，固定间隔对账不会使用 Input System 可能残留的非零向量；新的非零 started/performed 回调才会开启下一轮移动。

审查返工进一步校验非零回调发生时 `Movement.IsPressed()` 的实时状态。即使迟到回调时间戳较新，只要按键已释放就立即停止并丢弃该回调。

## 设计思路

输入事件负责清理移动数据，移动组件负责保证表现状态收敛。补偿触发只在“方向输入从有到无且当前为默认状态”时执行，不改变跳跃、技能等非默认状态，避免抢占其他动作。

## 验证结果

- `GM_Game.csproj` 构建成功，0 个错误，38 个项目既有警告。
- 静态规则检查通过：未新增命名空间、`foreach`、三目运算符或业务无约束对象载荷。
- Unity Play Mode 未执行，快速 S 点按、双击和 W/S 反向仍需在可用编辑器中回归。
