# 2026-10-21 豆包桌面接入项目调查记录

- 用户纠正：要的是类似WorkBuddy GitHub网关、调用豆包桌面客户端的方案，不是网页云电脑实验。已停止网页实验；仅打开LYFMMORGP项目并查看云电脑菜单，没有发送任何任务或生成云端产物。
- GitHub检索并实际读取Fullstop000/doubao-cli的README与package.json：版本0.20.0，明确支持macOS原生Doubao/DoubaoWork桌面后端，提供会话创建/发送/等待/读取/停止、项目与local/cloud运行环境；桌面要求macOS，Linux/macOS另有headless，不可将headless当Windows桌面已接入。
- Windows检索尚未确认现成可用移植。5201213/doubao-free-api虽描述桌面封装，其当前README主路径是网页Cookie加HTTP反代；dorakoo/doubao-studio是Windows自建Electron工作台驱动网页，不等同官方豆包桌面原生会话。
- 本轮仅只读调查，未安装新项目、未改模型/MCP/业务代码，无提交推送。下一步如用户要求Windows接入，先核对官方桌面客户端和本机协议兼容性，再决定最小适配，不能宣称可直接安装跑通。


## 本轮文件

- Docs/AI/CURRENT_TASK.md：记录用户需求纠正，撤销网页云电脑实验方向，保存桌面接入调查结论。
- Docs/AI/TEST_REPORT.md：记录未发送网页任务、未实测Windows桌面CLI以及已读取文档的验证边界。
- 项目修改日志/2026-10-21-DOUBAO-DESKTOP-RESEARCH-CHANGELOG.md：新增本记录。

## 来源

- https://github.com/Fullstop000/doubao-cli
- https://github.com/Fullstop000/doubao-cli/blob/main/package.json
- https://github.com/5201213/doubao-free-api
- https://github.com/dorakoo/doubao-studio

Git：仅协作记录，无业务代码修改，未提交推送，未合main。没有安装、执行上述候选软件，没有上传本地项目或凭据。
