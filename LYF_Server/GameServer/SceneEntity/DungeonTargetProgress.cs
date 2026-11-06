using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>沿用RO的Section顺序与分支图；每个副本实例只统计当前阶段的正式目标事件。</summary>
public sealed class DungeonTargetProgress
{
    /// <summary>沿用RO无效选支请求的随机后继回退，仅在显式错误选择时使用。</summary>
    private static readonly Random branchRandom = new Random();
    /// <summary>阶段号到原目标行号；线性模式从1编号，分支模式使用原组表编号。</summary>
    private readonly SortedDictionary<int, int[]> sections = new SortedDictionary<int, int[]>();
    /// <summary>阶段后继；多个后继等待原NPC脚本selecttargetbranch选择。</summary>
    private readonly Dictionary<int, int[]> children = new Dictionary<int, int[]>();
    /// <summary>当前阶段编号，无目标时为零。</summary>
    private int currentSection;
    /// <summary>当前阶段各目标累计次数，与目标行顺序一一对应。</summary>
    private int[] counts = new int[0];
    /// <summary>类型8各目标尚未完成的原波次号，进入新阶段时重新建立。</summary>
    private HashSet<int>[] remainingWaves = new HashSet<int>[0];
    /// <summary>当前阶段全部目标完成后为真；分支等待和末阶段保留完成快照。</summary>
    public bool SectionFinished { get; private set; }

    /// <summary>直接读取所选副本的目标模式与原始字符串，不按副本编号分支。</summary>
    /// <param name="dungeon">本实例所选DungeonsTable行。</param>
    public DungeonTargetProgress(DungeonsTable dungeon)
    {
        if (string.IsNullOrEmpty(dungeon.DungeonTarget)) return;
        // 原字符串分隔出的Section或有向边，始终按RO表定义解释。
        string[] parts;
        if (dungeon.DungeonTargetType == 1)
        {
            parts = dungeon.DungeonTarget.Split('|');
            // 线性阶段顺序是原字符串顺序，同阶段用等号连接多个目标。
            for (int index = 0; index < parts.Length; index++)
            {
                sections.Add(index + 1, ParseIds(parts[index], '='));
                if (index + 1 < parts.Length) children.Add(index + 1, new[] { index + 2 });
            }
        }
        else if (dungeon.DungeonTargetType == 2)
        {
            parts = dungeon.DungeonTarget.Split(',');
            // 分支字符串的数字引用DungeonTargetGroupTable，不能当作目标行号。
            for (int index = 0; index < parts.Length; index++)
            {
                // 每条边首项为父组，其余项按配置顺序给出可选子组。
                int[] edge = ParseIds(parts[index], '=', '|');
                children.Add(edge[0], edge.Skip(1).ToArray());
                for (int node = 0; node < edge.Length; node++)
                    if (!sections.ContainsKey(edge[node])) sections.Add(edge[node], LubanMgr.Instance.GetDungeonTargetGroupTableById(edge[node]).Targets.ToArray());
            }
        }
        else throw new NotSupportedException("未定义的DungeonTargetType: " + dungeon.DungeonTargetType);
        Activate(sections.Keys.First());
    }

    /// <summary>按原表分隔符解析纯整数目标或组引用。</summary>
    /// <param name="value">原配置中一个阶段或一条边。</param>
    /// <param name="separators">该模式在RO中使用的分隔字符。</param>
    private static int[] ParseIds(string value, params char[] separators)
    {
        return value.Split(separators).Select(item => int.Parse(item, CultureInfo.InvariantCulture)).ToArray();
    }

    /// <summary>为新进入的Section建立独立次数和待完成波次。</summary>
    /// <param name="sectionId">目标图中已存在的阶段号。</param>
    private void Activate(int sectionId)
    {
        currentSection = sectionId;
        SectionFinished = false;
        counts = new int[sections[sectionId].Length];
        remainingWaves = new HashSet<int>[counts.Length];
        // 每个原目标有自己的计数，不能把同阶段多个目标合成一个数字。
        for (int index = 0; index < counts.Length; index++)
        {
            // 目标定义与Luban快照保持同一来源。
            DungeonTargetTable row = LubanMgr.Instance.GetDungeonTargetTableById(sections[sectionId][index]);
            remainingWaves[index] = new HashSet<int>();
            if ((int)row.Target[0] != 8) continue;
            for (int wave = 1; wave < row.Target.Length; wave++) remainingWaves[index].Add((int)row.Target[wave]);
        }
    }

    /// <summary>取原处理器的完成次数；对话、区域、全波完成都只需完成一次。</summary>
    /// <param name="row">当前目标的原表行。</param>
    private static int Total(DungeonTargetTable row)
    {
        // RO的1、5、8类型使用基类默认总数1；第三项不是通用总次数。
        int type = (int)row.Target[0];
        if (type == 1 || type == 5 || type == 8) return 1;
        return (int)row.Target[2];
    }

    /// <summary>只让当前Section订阅匹配事件；一条事件不会重放给新激活的Section。</summary>
    /// <param name="type">原目标类型：1对话、2击杀、3采集、5区域、7AI事件、8清波。</param>
    /// <param name="subjectId">实体、采集、区域、目标或波次的原配置号。</param>
    /// <param name="sceneId">仅区域目标使用的原场景号。</param>
    private bool Consume(int type, int subjectId, int sceneId = 0)
    {
        if (currentSection == 0 || SectionFinished) return false;
        // 本条事件是否实际改变当前目标，供所属场景决定可靠广播。
        bool changed = false;
        // 仅遍历事件开始时的当前Section，在循环结束后才转移阶段。
        for (int index = 0; index < counts.Length; index++)
        {
            // 原行的类型和各参数定义均由对应RO处理器决定。
            DungeonTargetTable row = LubanMgr.Instance.GetDungeonTargetTableById(sections[currentSection][index]);
            if ((int)row.Target[0] != type || counts[index] >= Total(row)) continue;
            // 大多数处理器按第二项匹配配置号；特殊事件按行号匹配int_arg。
            bool matches = (int)row.Target[1] == subjectId;
            if (type == 7) matches = row.ID == subjectId;
            if (type == 5) matches = (int)row.Target[1] == sceneId && (int)row.Target[2] == subjectId;
            if (type == 8)
            {
                remainingWaves[index].Remove(subjectId);
                matches = remainingWaves[index].Count == 0;
            }
            if (!matches) continue;
            counts[index]++;
            changed = true;
        }
        if (!changed) return false;
        SectionFinished = true;
        for (int index = 0; index < counts.Length; index++)
            if (counts[index] < Total(LubanMgr.Instance.GetDungeonTargetTableById(sections[currentSection][index]))) SectionFinished = false;
        // 唯一后继自动进入，多后继保留完成Section并等待NPC选择。
        int[] next;
        if (SectionFinished && children.TryGetValue(currentSection, out next))
            if (next.Length == 1) Activate(next[0]);
        return true;
    }

    /// <summary>消费权威怪物从存活变为死亡的单次事件。</summary>
    /// <param name="entityId">死亡实体的原EntityTable配置号。</param>
    public bool MonsterDied(int entityId) { return Consume(2, entityId); }

    /// <summary>消费正式采集成功事件，次数按成功采集数增加。</summary>
    /// <param name="collectionId">原采集表配置号。</param>
    public bool Collected(int collectionId) { return Consume(3, collectionId); }

    /// <summary>消费原场景AI特殊事件，不接受其他同名载荷格式。</summary>
    /// <param name="name">原事件名dungeonfinishtarget。</param>
    /// <param name="targetId">事件int_arg中的目标行号。</param>
    public bool SceneEvent(string name, int targetId)
    {
        if (name != "dungeonfinishtarget") return false;
        return Consume(7, targetId);
    }

    /// <summary>消费原场景区域进入事件，按场景号和墙号联合匹配。</summary>
    /// <param name="sceneId">进入区域所在的SceneTable编号。</param>
    /// <param name="wallId">原区域或墙编号。</param>
    public bool EnterTrigger(int sceneId, int wallId) { return Consume(5, wallId, sceneId); }

    /// <summary>消费已激活且真实完成的波次，不预先标记后续Section。</summary>
    /// <param name="waveId">原场景波次编号。</param>
    public bool WaveFinished(int waveId) { return Consume(8, waveId); }

    /// <summary>保留RO的NPC脚本边界；由实际脚本传入对话或选支事件，客户端不能直接写进度。</summary>
    /// <param name="name">dungeontargetnpctalk或selecttargetbranch。</param>
    /// <param name="npcId">实际完成对话的NPC表编号。</param>
    /// <param name="branchId">selecttargetbranch选择的原组表编号。</param>
    public bool NpcScript(string name, int npcId, int branchId)
    {
        if (name == "dungeontargetnpctalk") return Consume(1, npcId);
        if (name != "selecttargetbranch" || !SectionFinished) return false;
        // 无选择请求时继续等待；显式无效选择沿RO随机回退并记录配置错误。
        int[] next;
        if (!children.TryGetValue(currentSection, out next) || next.Length <= 1) return false;
        if (!next.Contains(branchId))
        {
            LogMsg.Info("副本目标分支配置错误，组号=" + branchId + "，按RO随机选择当前真实后继");
            branchId = next[branchRandom.Next(next.Length)];
        }
        Activate(branchId);
        return true;
    }

    /// <summary>追加当前Section的完整目标快照，供入场、晚加入和可靠更新使用。</summary>
    /// <param name="state">所属副本实例的协议快照。</param>
    public void FillSnapshot(DungeonFlowState state)
    {
        if (currentSection == 0) return;
        // 同阶段所有目标全部发送，保留已完成项直到整个阶段完成。
        for (int index = 0; index < counts.Length; index++)
        {
            // 文案、总数和目标身份始终来自当前Luban行。
            DungeonTargetTable row = LubanMgr.Instance.GetDungeonTargetTableById(sections[currentSection][index]);
            state.Objectives.Add(new DungeonObjectiveData { Id = row.ID, Description = row.TargetDes,
                Current = counts[index], Total = Total(row), Completed = counts[index] >= Total(row) });
        }
    }
}
