using System;
using Google.Protobuf;

/// <summary>在独立进程中验证服务端技能组件的权威成功、拒绝和 RO 式施法 UUID 路径。</summary>
public static class SkillAuthoritySmoke
{
    /// <summary>初始化一名测试角色并执行技能释放规则断言。</summary>
    /// <param name="args">命令行参数；此测试不接受参数。</param>
    /// <returns>全部断言通过时为零，否则为非零错误码。</returns>
    public static int Main(string[] args)
    {
        try
        {
            // 运行目录包含正式 Network 构建复制的全部 Luban 二进制表。
            LubanMgr.Instance.Init();
            OnlineRole role = CreateReadyRole();
            RoleSkillComponent skillComponent;
            if (!role.TryGetComponent(out skillComponent))
            {
                return Fail("角色未注册技能组件");
            }

            // 技能列表尚未装载时必须拒绝，不得修改法力值。
            int initialMp = role.mainRoleInfo.BaseInfo.CurrMp;
            SkillCastRet result;
            SkillCastStartNtf startNotice;
            bool accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 1UL, SkillId = 10010 },
                out result, out startNotice);
            if (accepted || result.CmdCode != CmdCode.SkillStateBlocked ||
                role.mainRoleInfo.BaseInfo.CurrMp != initialMp)
            {
                return Fail("未装载技能列表未被拒绝");
            }

            // 中心服技能回包只装载已学等级，客户端不能提交等级或消耗值。
            RoleSkillInfoRet learnedSkills = new RoleSkillInfoRet { CmdCode = CmdCode.Succeed };
            learnedSkills.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10010, Level = 1 });
            if (!skillComponent.LoadLearnedSkills(learnedSkills))
            {
                return Fail("已学技能列表装载失败");
            }

            // 成功施法扣除权威 MP，并在结果及开始通知中原样沿用请求 UUID。
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 2UL, SkillId = 10010 },
                out result, out startNotice);
            // 未成功、错误码不符或缺少开始通知时直接断言失败。
            if (!accepted || result.CmdCode != CmdCode.Succeed || startNotice == null)
            {
                return Fail("成功施法未原子扣除 MP 或未生成通知");
            }
            // 结果与通知中的 UUID 必须沿用请求值，且 MP 已按 20 点权威扣除。
            if (result.SkillUuid != 2UL || startNotice.SkillUuid != 2UL ||
                result.CurrentMp != initialMp - 20)
            {
                return Fail("成功施法未原子扣除 MP 或未生成通知");
            }

            // 活动施法期间再次请求被技能状态阻断，不依赖历史序号缓存。
            SkillCastRet retryResult;
            SkillCastStartNtf retryNotice;
            bool retryAccepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 2UL, SkillId = 10010 },
                out retryResult, out retryNotice);
            // 重试未被拒绝或错误码不是活动状态阻断时直接断言失败。
            if (retryAccepted || retryResult.CmdCode != CmdCode.SkillStateBlocked)
            {
                return Fail("活动技能未阻断再次施法");
            }
            // MP 必须保持扣除后不变，且不应再产生新的开始通知。
            if (role.mainRoleInfo.BaseInfo.CurrMp != initialMp - 20 || retryNotice != null)
            {
                return Fail("活动技能未阻断再次施法");
            }

            // 结束当前表现后，再次请求应进入单技能 CD 拒绝。
            skillComponent.InterruptActiveCast(true);
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 3UL, SkillId = 10010 },
                out result, out startNotice);
            if (accepted || result.CmdCode != CmdCode.SkillOnCooldown ||
                role.mainRoleInfo.BaseInfo.CurrMp != initialMp - 20)
            {
                return Fail("技能冷却未拒绝或错误扣除 MP");
            }

            // 未学习、非法技能 ID 和死亡状态均不得进入施法提交。
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 4UL, SkillId = 10040 },
                out result, out startNotice);
            if (accepted || result.CmdCode != CmdCode.SkillNotLearned)
            {
                return Fail("未学习技能未被拒绝");
            }
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 5UL, SkillId = 99999 },
                out result, out startNotice);
            if (accepted || (result.CmdCode != CmdCode.SkillNotLearned &&
                result.CmdCode != CmdCode.SkillConfigMissing))
            {
                return Fail("非法技能 ID 未被拒绝");
            }

            // 使用已学习技能构造 MP 不足场景；失败结果必须保持 MP 不变。
            learnedSkills.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10040, Level = 1 });
            skillComponent.LoadLearnedSkills(learnedSkills);
            role.mainRoleInfo.BaseInfo.CurrMp = 0;
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 6UL, SkillId = 10040 },
                out result, out startNotice);
            if (accepted || result.CmdCode != CmdCode.SkillInsufficientMp ||
                role.mainRoleInfo.BaseInfo.CurrMp != 0)
            {
                return Fail("MP 不足未被拒绝");
            }

            role.mainRoleInfo.BaseInfo.CurrHp = 0;
            accepted = skillComponent.TryCast(
                new SkillCastReq { SkillUuid = 7UL, SkillId = 10040 },
                out result, out startNotice);
            if (accepted || result.CmdCode != CmdCode.SkillStateBlocked)
            {
                return Fail("死亡角色未被拒绝");
            }

            Console.WriteLine("技能权威路径通过：未装载、UUID 沿用、活动状态、CD、未学习、非法 ID、MP 不足、死亡均符合规则。");
            return 0;
        }
        catch (Exception exception)
        {
            // 某些服务端依赖的异常 ToString 可能再次访问未初始化对象，测试输出只读取稳定的类型和消息。
            string exceptionType = exception.GetType().FullName;
            string exceptionMessage = exception.Message;
            return Fail("测试异常类型=" + exceptionType + "，消息=" + exceptionMessage);
        }
    }

    /// <summary>创建满足在线和角色数据就绪门槛的最小测试角色。</summary>
    /// <returns>已初始化组件、生命值和法力值的在线角色。</returns>
    private static OnlineRole CreateReadyRole()
    {
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo
        {
            BaseInfo = new RoleBaseInfo
            {
                RoleId = 900001,
                JobId = 1,
                CurrHp = 1000,
                CurrMp = 500,
                MaxHp = 1000,
                MaxMp = 500
            }
        };
        role.InitSerializes();
        role.InitializeSaveVersion(0L);
        role.CompleteRoleDataLoad();
        return role;
    }

    /// <summary>输出失败原因并返回统一失败码。</summary>
    /// <param name="message">失败断言或异常的中文说明。</param>
    /// <returns>固定非零测试失败码。</returns>
    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
