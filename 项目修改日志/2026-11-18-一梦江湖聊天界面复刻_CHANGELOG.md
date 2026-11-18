# 2026-11-18 江湖聊天频道管理面板修正

任务编号 `YIMENG-CHAT-20261118`。本记录范围为获批的频道管理修正。

## 背景与实现

用户截图中，「管理频道」在「接收消息」右侧打开并列列表。此前实际按钮跳到「常用频道」设置页。现由接收消息页展示独立滚动区，管理行与新聊天的现有左栏条目一一对应。18 个条目包含声演坊、常用、当前、互联世界、世界、队伍等。每行有眼睛和四向手柄，右下是固定的「应用」按钮。

眼睛和拖动先编辑面板草稿。「应用」才更新新版左栏顺序与可见性，状态保留在本次 MainView 会话。切换设置页、关闭设置或收起聊天会关闭管理面板。再次打开从左栏读取已应用状态。应用后若当前频道已隐藏，切到排序后的首个可见页签。

主 Agent 隔离复验发现，只保留「招募」后应用，再收起和展开聊天会失去设置入口。第二轮在现有「应用」可用条件中要求至少保留一个带设置入口的可见频道。保留普通页或系统页时，可应用并通过该页恢复被关掉的频道。常用八项发言菜单、独立页固定发言目标以及原文字和语音链路通过回归。

## 文件清单

| 状态 | 路径 | 变更 |
|---|---|---|
| 修改 | `LYF_MMO/Assets/LYF_Game/Prefabs/Resources/UI/Prefabs/YimengChat.prefab` | 保存频道管理面板、18 行、独立滚动容器、应用底栏和按钮引用。 |
| 修改 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatWindow.cs` | 刷新应用后的当前页选择和频道控件，管理应用时保留接收消息设置。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatChannelManagement.cs` | 读取新版左栏、编辑草稿并应用显隐顺序，确保可重新进入设置。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatChannelManagement.cs.meta` | 保存频道管理组件的 Unity 资源标识。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatChannelManagementRow.cs` | 绑定单行控件，通过 Unity `IDragHandler` 接收手柄拖动，并更新眼睛图标。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatChannelManagementRow.cs.meta` | 保存频道管理行组件的 Unity 资源标识。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/UISprites/YimengChat/ChannelManagementIcons.png` | 保存睁眼、划线眼睛和四向箭头素材。 |
| 新增 | `LYF_MMO/Assets/LYF_Game/UISprites/YimengChat/ChannelManagementIcons.png.meta` | 保存图集导入配置与精灵切片。 |

## 验收与边界

- Codex 原生开发子 Agent 为 `/root/yimeng_chat_dev`，原始会话 `01a1ea95-307b-74c3-8af2-d5fb6a06191a`，本轮实际开发模型 `gpt-6.1-sol`，首轮实现加一轮返修。主 Agent 执行复现、复验和 Git 集成。
- 第一轮隔离 Play Mode 通过 154 项自检。主 Agent 补测只保留招募时复现恢复入口丢失，证据为 `_temp/yimeng-chat-channel-management-20261118/main-only-recruit-repro.log`。
- 最终使用 Unity `2023.2.20f1c1` 在隔离项目运行 `-batchmode -force-d3d11 -executeMethod YimengChatPlayVerification.Start`，164 项独立复验通过。证据为 `_temp/yimeng-chat-channel-management-20261118/main-acceptance-channel-management.log`，包括两个画幅、分别滚动、草稿撤销、应用、恢复、三频道和旧版回归。
- Editor 与 Windows Player 分别编译 394 个源文件并通过。52 个受保护路径的 SHA-256 匹配。旧代码、旧资源及共享入口在该保护范围内。样式截图与范围汇总保存在 `_temp/yimeng-chat-channel-management-20261118/`。
- 验收使用本地协议出口和合成音频。真实联网、物理麦克风和用户当时运行的场景待实测。
- 本轮业务改动已通过 `ec6b032fef5ac155de99aa9317454ddd600c2ffc` 推送到 `origin/codex/feature-development`，父提交为 `82a78c7`，提交确认包含上述 8 个路径。证据为 `_temp/yimeng-chat-channel-management-20261118/git-receipt.txt`。
- 通过 `token-monitor` 运行 `collect` 和 `context`。两轮开发分别记录 5,924,369 与 2,097,167 tokens，包含缓存输入。原生日志汇总与统计窗口保存在 `_temp/yimeng-chat-channel-management-20261118/turn-usage.json`，当前对话压缩计数保存在同目录的 `context-compactions.json`。

## 后续补充 本人消息右排与内置大笑表情（2026-11-18）

原消息行已通过 ChatMessage.IsLocalPlayer 区分气泡颜色，布局始终在左。新版沿用这项身份标记，把本人头像、时间、昵称和气泡移到右侧，其他玩家保持左侧。正文按消息行实际宽度测量并换行，语音播放与系统消息通栏沿用现有入口。

Unity Play Mode 中通过 `Tools/Chat/新版聊天测试` →「模拟他人消息」，向本地历史插入一条标明测试来源的世界消息，可在「常用」或「世界」查看。测试入口处于 Editor 目录，通过既有 ReceiveMessage 写入会话历史。

底栏笑脸 → 上方「表情」 → 「大笑」走同一套世界、当前场景和队伍的文本发言链路，原输入草稿保留。整条 `【大笑表情】` 由正式广播驱动一次回显。新版识别为图片，旧版展示可读标记。项目 EmojiOne 图集导入为普通纹理，隔离验证确认没有可直接引用的 Sprite 子资产，因此预制体中的 RawImage 直接读取同一图集的 `1f601` 区域。协议仍提供文字和语音两类媒介。

| 状态 | 路径 | 变更 |
|---|---|---|
| 修改 | `LYF_MMO/Assets/LYF_Game/Prefabs/Resources/UI/Prefabs/YimengChat.prefab` | 局部绑定消息行头像、独立气泡底图、表情按钮及 EmojiOne 区域。 |
| 修改 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatMessageRow.cs` | 按作者靠左或靠右排版，随可用宽度换行，识别整条内置表情标记。 |
| 修改 | `LYF_MMO/Assets/LYF_Game/Scripts/UI/Main/YimengChat/YimengChatWindow.cs` | 使大笑按钮跟随当前频道的发言权限并使用原发送回调。 |
| 新增 | `LYF_MMO/Assets/Editor/YimengChatTestWindow.cs` | 提供 Unity 编辑器专用窗口和本地模拟他人消息按钮。 |
| 新增 | `LYF_MMO/Assets/Editor/YimengChatTestWindow.cs.meta` | 保存编辑器测试窗口源码的 Unity 资源标识。 |

- 原 Codex 开发 Agent `/root/yimeng_chat_dev` 使用 `gpt-6.1-sol`，会话 `01a1ea95-307b-74c3-8af2-d5fb6a06191a`。首次连接断流，尚无正式文件落盘。同一开发轮续接后交付，由主 Agent 独立复验。
- Unity `2023.2.20f1c1` 隔离 Play Mode 36 项通过。覆盖本人、对方、语音、系统、窄宽中英文、编辑器本地入口、三频道表情发包、等待广播单次回显和原界面切换。主 Agent 执行 `-batchmode -force-d3d11 -executeMethod YimengChatPlayVerification.Start`。证据为 `_temp/yimeng-chat-own-rows-sticker-20261118/main-acceptance-own-rows-sticker.log`，截图位于同目录 `proof-final/`。
- Editor 与 Windows Player 各编译 394 个源文件，新增 Editor 专用窗口按项目 Editor 引用编译通过。60 个保护路径的 SHA-256 匹配。网络验收捕获本地请求并注入服务广播，语音使用合成音频。真实服务器、物理麦克风和用户当前运行场景待实测。
- 功能提交 `69c92fd7bbd281ccd396f78265b014d5c6228ce1` 已推送到 `origin/codex/feature-development`，父提交 `da6df38`。验证的提交范围为上表 5 个路径，证据为 `_temp/yimeng-chat-own-rows-sticker-20261118/git-receipt.txt`。
- `token-monitor` 已运行 `collect` 和 `context`。已收到原生用量回执的开发续接轮记录 9,296,548 tokens，含缓存输入。中断尝试的独立用量不可测。汇总和统计窗口为 `_temp/yimeng-chat-own-rows-sticker-20261118/turn-usage.json`，对话压缩计数见同目录 `context-compactions.json`。
