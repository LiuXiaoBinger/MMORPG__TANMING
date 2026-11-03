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
            Require(result.CurrentMp == 800 && start.CastDurationMs == 6300, "费用或整套时长错误");
            Require(combined.CurrentChild.SkillID() == 10040, "首段不是 E");
            Advance(role, 61);
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
            Console.WriteLine("PASS：技能授予与快捷键保留、E→2 顺序、6300ms 占用、单次200MP、主CD、同UUID、自然结束和中断。");
            role.Dispose();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.GetType().Name + ": " + exception.Message);
            return 1;
        }
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
