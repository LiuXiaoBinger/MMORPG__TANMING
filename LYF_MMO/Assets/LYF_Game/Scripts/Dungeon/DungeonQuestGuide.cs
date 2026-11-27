using System;

/// <summary>副本目标无原表引导坐标时，用编辑场景内已有区域、波次和传送按钮定位。</summary>
[Serializable]
public sealed class DungeonQuestGuide
{
    /// <summary>DungeonTargetTable 的目标编号，同阶段多个目标分别绑定。</summary>
    public int targetId;
    /// <summary>目标区域的 XML 物件编号，负值表示目标使用活跃波次。</summary>
    public int area = -1;
    /// <summary>正式开战前优先交谈的剧情 NPC 波次，零表示无需开战对话。</summary>
    public int talkWave;
    /// <summary>服务端接受剧情选项后开启的场景物件，出现后才引导攻击首领。</summary>
    public int combatObject;
    /// <summary>本目标的剧情人物、敌人或采集波次，依次选择当前存在的对象。</summary>
    public int[] waves;
    /// <summary>到目标区域无法直接步行时可用的前进传送按钮，取可达且已激活的一端。</summary>
    public int[] portals;
}
