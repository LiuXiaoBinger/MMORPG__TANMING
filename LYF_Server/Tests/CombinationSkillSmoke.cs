using System;
using System.IO;
using System.Reflection;

/// <summary>使用真实技能表、角色组件和固定 Tick 验证 E→2 组合顺序、结算与中断。</summary>
public static class CombinationSkillSmoke
{
    /// <summary>测试专用时钟入口，仅在独立进程推进服务器逻辑时间。</summary>
    private static readonly FieldInfo TickField = typeof(GameGlobal).GetField("<LogicTick>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>运行组合权威路径，任一断言失败返回非零。</summary>
    /// <param name="args">此测试不接收业务参数。</param>
    public static int Main(string[] args)
    {
        try
        {
            LubanMgr.Instance.Init();
            VerifyRoleGrant();
            // 最小在线角色不连接数据库或网关，仅验证正式组件链。
            OnlineRole role = new OnlineRole();
            role.mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo { RoleId = 930101, JobId = 1, CurrHp = 1000, MaxHp = 1000, CurrMp = 1000, MaxMp = 1000 } };
            role.InitSerializes();
            role.InitializeSaveVersion(0L);
            role.CompleteRoleDataLoad();
            // 本次角色实际注册的技能组件，测试不替换其实现。
            RoleSkillComponent component;
            role.TryGetComponent(out component);
            // 模拟中心服下发的学习列表，组合子技能由配置装配，无需额外独立请求。
            RoleSkillInfoRet learned = new RoleSkillInfoRet { CmdCode = CmdCode.Succeed };
            learned.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10100, Level = 1 });
            component.LoadLearnedSkills(learned);
            // 成功回包和开始通知应只生成一套主技能身份。
            SkillCastRet result;
            // 开始通知沿用主 UUID 并携带整个组合时长。
            SkillCastStartNtf start;
            Require(component.TryCast(new SkillCastReq { SkillId = 10100, SkillUuid = 201UL }, out result, out start), "组合请求未成功");
            // 当前实例必须是组合运行类型，不能只播放第一个普通技能。
            ROCombineSkill combined = component.CurrentSkill() as ROCombineSkill;
            Require(combined != null, "未创建 ROCombineSkill");
            Require(result.CurrentMp == 800 && start.CastDurationMs == 4700, "费用或整套时长错误");
            Require(combined.CurrentChild.SkillID() == 10040, "首段不是 E");
            Advance(role, 29);
            Require(combined.CurrentChild.SkillID() == 10040, "E 尚未结束就接段");
            Advance(role, 1);
            Require(combined.CurrentChild.SkillID() == 10060, "未按时自动接 2");
            Require(combined.CurrentChild.GetSkillUUID() == 201UL && role.mainRoleInfo.BaseInfo.CurrMp == 800, "子段更换 UUID 或重复扣蓝");
            Require(component.GetSkillCoolDown(10040) == 0 && component.GetSkillCoolDown(10060) == 0, "组合错误启动子技能冷却");
            Advance(role, 63);
            Require(component.IsCasting(), "末段提前结束");
            Advance(role, 1);
            Require(!component.IsCasting(), "组合到期仍占用动作");
            Require(component.GetSkillCoolDown(10100) > 0, "组合主冷却丢失");
            component.SetSkillCoolDown(10100, 0);
            Require(component.TryCast(new SkillCastReq { SkillId = 10100, SkillUuid = 202UL }, out result, out start), "二次组合请求失败");
            combined = (ROCombineSkill)component.CurrentSkill();
            component.InterruptActiveCast(true);
            Advance(role, 126);
            Require(!combined.Casting() && combined.CurrentChild == null, "中断后仍执行后续段");
            VerifyCancellation(role);
            Console.WriteLine("PASS：技能授予、E→2 在1500ms接段、4700ms占用、单次200MP、主CD、同UUID、全部技能滑步/跳跃取消、无后续段及拒绝滑步不误取消。");
            role.Dispose();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.GetType().Name + ": " + exception.Message);
            return 1;
        }
    }

    /// <summary>沿正式施法事件和移动上报验证全部现有技能的主动取消，保留已经提交的费用与冷却。</summary>
    /// <param name="role">当前独立进程已初始化的测试角色，不加入在线索引。</param>
    private static void VerifyCancellation(OnlineRole role)
    {
        // 所有正式技能均应允许被滑步或跳跃取消，滑步自身也允许被跳跃取消。
        int[] skillIds = { 10010, 10040, 10050, 10060, 10070, 10080, 10100 };
        // 正式技能组件负责费用与冷却，测试只补齐学习等级。
        RoleSkillComponent skills = role.GetComponent<RoleSkillComponent>();
        // 现有移动上报入口负责在新跳跃时切出技能状态。
        ROMoveComponent move = role.GetComponent<ROMoveComponent>();
        // 学习快照为每个真实技能提供一级配置。
        RoleSkillInfoRet learned = new RoleSkillInfoRet { CmdCode = CmdCode.Succeed };
        // 当前待登记的技能下标。
        for (int index = 0; index < skillIds.Length; index++)
            learned.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = skillIds[index], Level = 1 });
        skills.LoadLearnedSkills(learned);
        role.mainRoleInfo.BaseInfo.MaxMp = 10000;
        role.mainRoleInfo.BaseInfo.CurrMp = 10000;
        // 每种技能分别验证滑步接管和跳跃取消。
        for (int index = 0; index < skillIds.Length; index++)
        {
            Advance(role, 300);
            RequestCast(role, skillIds[index], (ulong)(400 + index * 3));
            // 取消前的真实动作实例，取消后仍可检查 Interrupted。
            ROSkill oldSkill = skills.CurrentSkill();
            Require(oldSkill != null && oldSkill.SkillID() == skillIds[index], "正式技能入口未开始动作");
            if (skillIds[index] != 10080)
            {
                RequestCast(role, 10080, (ulong)(401 + index * 3));
                Require(oldSkill.Interrupted && !oldSkill.Casting(), "滑步未取消原技能 " + skillIds[index]);
                Require(skills.CurrentSkill().SkillID() == 10080, "滑步未接管动作");
            }
            move.OnMoveStart(new MoveOperationReq { JumpCount = 1 });
            Require(!skills.IsCasting(), "跳跃未取消滑步");
            move.OnMoveStart(new MoveOperationReq { JumpCount = 0 });
            Advance(role, 300);
            RequestCast(role, skillIds[index], (ulong)(402 + index * 3));
            oldSkill = skills.CurrentSkill();
            Require(oldSkill != null, "跳跃测试未开始原技能 " + skillIds[index]);
            // 跳跃前的权威蓝量及单技能冷却，取消不退还二者。
            int mp = role.mainRoleInfo.BaseInfo.CurrMp;
            // 冷却使用服务器逻辑 Tick，移动上报本身不推进时间。
            long cooldown = skills.GetSkillCoolDown(skillIds[index]);
            move.OnMoveStart(new MoveOperationReq { JumpCount = 1 });
            Require(oldSkill.Interrupted && !skills.IsCasting(), "跳跃未取消技能 " + skillIds[index]);
            Require(role.mainRoleInfo.BaseInfo.CurrMp == mp && skills.GetSkillCoolDown(skillIds[index]) == cooldown, "取消退还了MP或CD");
            // 组合取消必须同时终止当前子段和尚未启动的下一段。
            ROCombineSkill combination = oldSkill as ROCombineSkill;
            if (combination != null)
            {
                Advance(role, 100);
                Require(combination.CurrentChild == null && !combination.Casting(), "跳跃后组合继续接段");
            }
            move.OnMoveStart(new MoveOperationReq { JumpCount = 0 });
        }
        Advance(role, 300);
        RequestCast(role, 10100, 500UL);
        // 滑步处于冷却时请求应被拒绝，原组合应继续运行。
        ROSkill retained = skills.CurrentSkill();
        skills.SetSkillCoolDown(10080, 1000);
        RequestCast(role, 10080, 501UL);
        Require(ReferenceEquals(retained, skills.CurrentSkill()) && retained.Casting(), "失败滑步错误取消原技能");
        move.OnMoveStart(new MoveOperationReq { JumpCount = 1 });
        skills.SetSkillCoolDown(10100, 0);
        RequestCast(role, 10100, 502UL);
        retained = skills.CurrentSkill();
        move.OnMoveStart(new MoveOperationReq { JumpCount = 1 });
        Require(ReferenceEquals(retained, skills.CurrentSkill()) && retained.Casting(), "重复空中同步错误取消技能");
        move.OnMoveStart(new MoveOperationReq { JumpCount = 2 });
        Require(!skills.IsCasting(), "新一段跳跃未取消技能");
    }

    /// <summary>按正式零吟唱事件链请求一次技能，不绕过状态机的替换权限。</summary>
    /// <param name="role">发起技能的测试角色。</param>
    /// <param name="skillId">已学习的技能主键。</param>
    /// <param name="uuid">本次请求唯一标识。</param>
    private static void RequestCast(OnlineRole role, int skillId, ulong uuid)
    {
        // 池化事件仅在当前同步调用栈内使用，分发完成归还。
        ROClientFireSkillArgs args = ROEventPool<ROClientFireSkillArgs>.Instance.Get();
        try
        {
            args.skill_id_ = skillId;
            args.skill_uuid_ = uuid;
            args.target_ = 0UL;
            args.cast_point_ = new ROVector3(0f, 0f, 0f);
            role.GetEventDispatcher().DispatcherEvent(args);
        }
        finally { args.Release(); }
    }

    /// <summary>通过中心服真实的技能列表补充函数验证授予条件，不连接数据库。</summary>
    private static void VerifyRoleGrant()
    {
        // 反射仅用于访问中心服私有装配边界，避免为测试公开业务方法。
        MethodInfo grant = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CenterServer.exe"))
            .GetType("CentRoleModel").GetMethod("AddCombinationSkill", BindingFlags.Static | BindingFlags.NonPublic);
        // 第一份角色数据只学 E，随后补齐 2 并重复调用以验证不产生重复记录。
        RoleSkillInfoRet response = new RoleSkillInfoRet();
        response.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10040, Level = 1, BindKey = "e" });
        // object[] 是 CLR 反射 Invoke 的参数边界，实际业务载荷仍为 RoleSkillInfoRet。
        grant.Invoke(null, new object[] { response });
        Require(response.RoleSkillInfoList.Count == 1, "只学一段时不应授予组合");
        response.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10060, Level = 1, BindKey = "2" });
        grant.Invoke(null, new object[] { response });
        Require(response.RoleSkillInfoList.Count == 3 && response.RoleSkillInfoList[2].SkillId == 10100, "两段已学仍未授予组合");
        Require(response.RoleSkillInfoList[2].BindKey == "3", "空闲 3 键未绑定组合");
        grant.Invoke(null, new object[] { response });
        Require(response.RoleSkillInfoList.Count == 3, "组合被重复授予");
        response.RoleSkillInfoList.RemoveAt(2);
        response.RoleSkillInfoList[0].BindKey = "3";
        grant.Invoke(null, new object[] { response });
        Require(response.RoleSkillInfoList[0].BindKey == "3" && response.RoleSkillInfoList[2].BindKey == string.Empty, "覆盖了已有 3 键绑定");
    }

    /// <summary>按正式服务器顺序推进逻辑 Tick，再更新角色组件。</summary>
    /// <param name="role">本测试创建的角色。</param>
    /// <param name="frames">需要推进的 50ms 帧数。</param>
    private static void Advance(OnlineRole role, int frames)
    {
        // 已推进的帧索引，反射仅限测试计时边界。
        for (int index = 0; index < frames; index++)
        {
            TickField.SetValue(GameGlobal.Instance, GameGlobal.Instance.LogicTick + 1L);
            role.Update(50);
        }
    }

    /// <summary>记录真实失败，不能将测试继续运行冒充通过。</summary>
    /// <param name="condition">需要成立的业务结果。</param>
    /// <param name="message">失败时指出的具体行为。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
