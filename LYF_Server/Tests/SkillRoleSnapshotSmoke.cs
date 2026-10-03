using System;
using Google.Protobuf;

/// <summary>在独立进程中验证中心服技能列表不能跨同一复合会话的角色切换串用。</summary>
public static class SkillRoleSnapshotSmoke
{
    /// <summary>模拟角色 B 当前在线时，依次处理迟到的 A 列表和 B 的有效列表。</summary>
    /// <param name="args">命令行参数；此测试不接收参数。</param>
    /// <returns>跨角色列表被拒且本角色列表成功装载时返回零。</returns>
    public static int Main(string[] args)
    {
        try
        {
            // 新增字段必须保留协议约定的编号，避免生成程序集与 proto 源不一致。
            Google.Protobuf.Reflection.FieldDescriptor roleIdField =
                RoleSkillInfoRet.Descriptor.FindFieldByNumber(3);
            if (roleIdField == null || roleIdField.Name != "role_id")
            {
                return Fail("11080 生成协议缺少编号为 3 的 role_id 字段");
            }
            // 当前复合会话绑定的角色 B，角色 ID 故意与迟到回包 A 不同。
            OnlineRole currentRole = new OnlineRole
            {
                GateSessionId = 7301,
                UnitySessionId = 7302,
                mainRoleInfo = new MainRoleInfo
                {
                    BaseInfo = new RoleBaseInfo
                    {
                        RoleId = 990002,
                        JobId = 1,
                        CurrHp = 100,
                        CurrMp = 100
                    }
                }
            };
            RoleManager.Instance.AddOnlineRole(990002, currentRole);
            // 与处理器实际使用的复合会话索引相同，避免仅测试协议字段本身。
            OnlineRole indexedRole = RoleManager.Instance.GetOnlineRoleBySession(7301, 7302);
            if (!ReferenceEquals(indexedRole, currentRole))
            {
                return Fail("角色 B 未进入当前复合会话索引");
            }
            // 当前在线角色的权威技能组件，初始不得含任何已学列表。
            RoleSkillComponent skillComponent;
            if (!currentRole.TryGetComponent(out skillComponent) || skillComponent.SkillsLoaded)
            {
                return Fail("角色 B 技能组件初始状态无效");
            }

            // A 的中心服回包复用相同 Gate/Unity 会话，但持久角色 ID 仍必须是 A。
            RoleSkillInfoRet staleResponse = new RoleSkillInfoRet
            {
                CmdCode = CmdCode.Succeed,
                RoleId = 990001
            };
            staleResponse.RoleSkillInfoList.Add(new RoleSkillInfo
            {
                SkillId = 10010,
                Level = 1,
                BindKey = "Q"
            });
            if (RoleSkillInfoHandler.IsMatchingRole(currentRole, staleResponse))
            {
                return Fail("角色 B 被判定为迟到角色 A 列表的所有者");
            }
            // 处理器必须在装载列表之前拒绝旧角色身份。
            RoleSkillInfoHandler handler = new RoleSkillInfoHandler();
            handler.Handle(CreateContext(staleResponse));
            if (skillComponent.SkillsLoaded)
            {
                return Fail("迟到的角色 A 技能列表被装载给角色 B");
            }

            // B 的有效回包使用同一复合会话，验证拒绝旧包未阻断后续正确同步。
            RoleSkillInfoRet currentResponse = new RoleSkillInfoRet
            {
                CmdCode = CmdCode.Succeed,
                RoleId = 990002
            };
            currentResponse.RoleSkillInfoList.Add(new RoleSkillInfo
            {
                SkillId = 10040,
                Level = 1,
                BindKey = "E"
            });
            if (!RoleSkillInfoHandler.IsMatchingRole(currentRole, currentResponse))
            {
                return Fail("角色 B 的有效列表未通过角色归属判定");
            }
            // 实际网关会话由四服联调覆盖，此处只验证有效快照能进入 B 的权威组件。
            if (!skillComponent.LoadLearnedSkills(currentResponse) || !skillComponent.SkillsLoaded)
            {
                return Fail("角色 B 的有效技能列表未装载");
            }

            Console.WriteLine("技能角色列表隔离通过：A 迟到回包拒绝，B 有效回包装载。");
            return 0;
        }
        catch (Exception exception)
        {
            return Fail("测试异常=" + exception.GetType().FullName + "，消息=" + exception.Message);
        }
    }

    /// <summary>为一条中心服 11080 回包构造无外部网络依赖的逻辑线程上下文。</summary>
    /// <param name="response">携带查询角色 ID 与已学技能列表的中心服回包。</param>
    /// <returns>复用当前 Gate/Unity 会话标识的不可变命令上下文。</returns>
    private static GameMessageHandlerContext CreateContext(RoleSkillInfoRet response)
    {
        // 原始回包仅在测试内构造，传给 GameCommand 后按生产路径复制为快照。
        BasePackage package = new BasePackage
        {
            ProtoCode = NetDefine.CMD_RoleSkillInfoCode,
            GateSessionId = 7301,
            UnitySessionId = 7302,
            Data = response.ToByteString()
        };
        // 无实际 CenterServer 连接；处理器只需检查协议身份和角色索引。
        GameCommand command = GameCommand.From(GameCommandSource.CenterServer, null, package);
        return new GameMessageHandlerContext(command);
    }

    /// <summary>向标准错误流报告失败断言。</summary>
    /// <param name="message">描述未满足的角色隔离行为。</param>
    /// <returns>固定非零退出码。</returns>
    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
