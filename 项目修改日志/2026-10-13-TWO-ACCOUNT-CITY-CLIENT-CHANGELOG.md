# 2026-10-13 双账号主城互见：客户端入场时序修复

## 文件范围

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`：异步本地模型绑定完成后，先通过既有UpdateMove上传出生位置，再发送进入世界请求；删除模型请求后提前入场的块。远端出生读取已有Pos.ToVector3，既有OtherRoleCtrl绑定真实实体后停止旧重力/根运动；本地出生和输入不改。
- `项目分析/TWO_ACCOUNT_CITY_CLIENT_FLOW.md`：归档真实服务端/客户端证据、根因、最小流程和未验证边界。
- `_temp/two-account-city-20261013/client/`：仅开发测试中间产物，不作为正式源码或资源。包含before快照、280源编译模板/日志/输入哈希和远端隔离原生探针。

## 设计依据

原入场时服务端Pos可能为空，按原点进行100米附近过滤；客户端固定主城出生离原点约103.75米，漏发11100后11181只查已有角色，不会补建。改先上报出生坐标再入场，沿现有消息队列处理顺序，不新增协议、确认等待、请求队列、动态AOI或重试。本地运动继续预测，不等服务器。

## 当前验证

- 客户端280真实源Rebuild：0错误、38既有警告；`client/GM_Game-build.log` 与 `compiled-source-hashes.json` 留存。
- 唯一业务源修前SHA：`AA9426922CFC0B0BDE98EE948E13FAC0B01A1C729DAA9E1248815AEBA6C0086F`，时序修后SHA：`AFC71EF53422C7B34AD4A9C66CB042D51807F09320A694139591389F812569BE`。
- 父Agent统一Unity现场，目前无真实远端实体，不把只读输入零值认定本地不能移动；未刷新编译或停止用户Play。
- 主执行远端原生before/bound专项实证旧Update继续写同一模型，绑定后零写入；极端位移仅说明隔离预览场景与主城碰撞查询产生的额外写入，不作为用户真实症状的位移量。
- 三修改末稿重新280源编译0错误38旧警告，SHA `75A4EB68F8660CA6F933C68E0F532BAA1C5BB59BCD85BE23D9DB9DBD0196DEA5`。
- 主实际执行末稿位置/绑定专项四项全部True：远端坐标正确、同实体绑定、旧Update零额外写入、本地出生保持。由临时脚本抽取实际生产修改段在原生克隆上运行；不置换正式Global世界、不刷新当前Play、不冒称完整异步联机流程。结果 `client/remote-final-position-result.txt`。

## 未解决与交接

独立服务端回环已经复现空Pos过滤并验证新消息顺序，客户端修后真实双账号登录及完整安装包仍待验收。主Agent将统一更新 `Docs/AI/CURRENT_TASK.md` 和 `TEST_REPORT.md`；本子任务不并发覆盖协作入口。未更新热更DLL、资源包或发布，未改正式预制体和服务器。
