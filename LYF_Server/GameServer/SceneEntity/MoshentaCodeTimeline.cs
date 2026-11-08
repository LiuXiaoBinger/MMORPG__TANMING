using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>两份原Lua时间轴函数的等价C#路径；计划数据来自逐项校验的原element_plan。</summary>
internal sealed class MoshentaCodeTimeline : MoshentaTimeline
{
    /// <summary>每实体独立的原LuaEnv整数槽。</summary>
    private readonly Dictionary<string, int> environment = new Dictionary<string, int>();
    /// <summary>读取对应原计划并执行InitVariables。</summary><param name="actor">5456或5512宿主。</param>
    internal MoshentaCodeTimeline(MoshentaActor actor) : base(actor)
    {
        // 原65事件只导出数据，函数执行由本类明确实现。
        XElement config = XElement.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "MonsterAI", "TimeAxes.xml"));
        XElement script = config.Elements("script").First(row => (int)row.Attribute("entityId") == actor.EntityId);
        XElement[] lines = script.Elements("axis").ToArray();
        for (int index = 0; index < lines.Length; index++)
        {
            // 每条轴声明的顺序保持原Lua数组顺序。
            List<MoshentaTimePoint> points = new List<MoshentaTimePoint>();
            XElement[] events = lines[index].Elements("event").ToArray();
            for (int item = 0; item < events.Length; item++) points.Add(new MoshentaTimePoint { Delay = (int)events[item].Attribute("delay"), Action = (string)events[item].Attribute("action"), Parameters = events[item].Elements("arg").Select(value => value.Value).ToArray() });
            Plans[(int)lines[index].Attribute("id")] = points;
        }
        if (actor.EntityId == 5456) environment["mark_count"] = 1;
        else
        {
            // 原BossOpt在整局场景共享，第一次才随机六种排列。
            MoshentaSceneVariable option;
            if (!actor.Owner.TrySceneVariable("BossOpt", out option)) actor.Owner.SetSceneInt("BossOpt", actor.Owner.Random.Next(2, 8));
            environment["init_axis_var"] = actor.Owner.GetSceneInt("BossOpt"); environment["axis_seq"] = 1;
        }
    }
    /// <summary>执行Perform_BasicAction和本脚本实际出现的八种函数。</summary><param name="axis">原轴号。</param><param name="number">事件号。</param>
    protected override bool Invoke(int axis, int number)
    {
        // 已校验的原事件及参数文本。
        MoshentaTimePoint point = Plans[axis][number - 1];
        switch (point.Action)
        {
            case "Wait": return true;
            case "SetIntSceneVar": Actor.Owner.SetSceneInt(point.Parameters[0], int.Parse(point.Parameters[1])); return true;
            case "SwitchAxisBySelf": Switch(int.Parse(point.Parameters[0])); return true;
            case "LuaSendTVNotice": Actor.Owner.PublishAiNotice(1, int.Parse(point.Parameters[0]), Actor, 0); return true;
            case "ChooseOption": return Choose();
            case "SetDaNaTuoSiState": Actor.Owner.SetSceneInt("DaNaTuoSiState", environment["axis_seq"] - 1); return true;
            case "DianKuangBuff": return MarkPlayers();
            default: throw new InvalidDataException("未实现的原时间轴函数 " + point.Action);
        }
    }
    /// <summary>按原BossOpt六种排列选择三人格和共战轴。</summary>
    private bool Choose()
    {
        // 原Lua分支顺序逐项保留，不按怪物ID排序替代。
        int[][] options = { new[] { 3, 4, 5, 6 }, new[] { 3, 5, 4, 6 }, new[] { 4, 3, 5, 6 }, new[] { 4, 5, 3, 6 }, new[] { 5, 4, 3, 6 }, new[] { 5, 3, 4, 6 } };
        int sequence = environment["axis_seq"];
        environment["axis_seq"] = sequence + 1;
        Switch(options[environment["init_axis_var"] - 2][sequence - 1]); return true;
    }
    /// <summary>原mark_count从1开始，逐次2/3/4/5标记，达到5后全员第五标记。</summary>
    private bool MarkPlayers()
    {
        // 原LuaForeachPlayers查0至25米内玩家，保留实例顺序。
        List<ulong> players = Actor.Owner.PlayersInRange(Actor.Position, 0, 25);
        if (players.Count == 0) return false;
        int[] buffs = { 3605401, 3605501, 3605601, 3605701, 3605801 };
        int count = environment["mark_count"];
        if (count < 5)
        {
            count++; environment["mark_count"] = count;
            List<int> choices = new List<int>();
            for (int index = 0; index < count; index++) choices.Add(buffs[index]);
            if (players.Count <= choices.Count)
            {
                // 原RandomSort从头到尾随机交换，随机调用次数必须一致。
                for (int index = 0; index < choices.Count; index++) { int other = Actor.Owner.Random.Next(index, choices.Count); int value = choices[index]; choices[index] = choices[other]; choices[other] = value; }
                for (int index = 0; index < players.Count; index++) Actor.Owner.AddBuff(Actor, players[index], choices[index]);
            }
            else
            {
                Actor.Owner.Shuffle(players);
                for (int index = 0; index < players.Count; index++)
                {
                    // 原多出来的玩家随机取已解锁标记，可重复。
                    int selected = index;
                    if (selected >= choices.Count) selected = Actor.Owner.Random.Next(0, choices.Count);
                    Actor.Owner.AddBuff(Actor, players[index], choices[selected]);
                }
            }
        }
        else for (int index = 0; index < players.Count; index++) Actor.Owner.AddBuff(Actor, players[index], buffs[4]);
        return true;
    }
}
