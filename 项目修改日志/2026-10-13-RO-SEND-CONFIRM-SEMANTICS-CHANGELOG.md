# RO 施法发送与服务器确认语义核对

- 修改范围：Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md及本记录，仅补充只读核对事实。未修改参考RO或本项目业务源码。
- 核对范围：外部MoonClient/Skill/MNavigationComponent.OnAfterAction、Network/ApplicationLayer/MNetClient.Send及Network/SessionLayer/MSocket.Send。
- 结论：Send返回true仅表示本地发送队列接受请求；预表现在同一调用中立即开始，不等服务器确认。原“避免在无服务器确认下空放”注释将本地提交与服务器确认混淆。
- 建议注释：“施法请求成功提交到本地发送队列后立即启动预表现，不等待服务器确认。”
- 验证：读取Send返回及Enqueue代码、对照OnAfterAction吟唱/瞬发调用顺序；无业务改动不重编。未检查实际网络送达或服务器准许结果，不把本地true等同网络成功。
