using System.Collections.Generic;
using Luban;

/// <summary>将本副本RO属性/状态叶子接入既有Gene；不另建属性叠加或状态引擎。</summary>
internal static class MoshentaGeneBridge
{
    /// <summary>生成原Buff专用内部Gene配置并沿用既有实例管理。</summary>
    /// <param name="runtime">实际宿主的现有Gene容器。</param><param name="buff">原Buff实例。</param><param name="slot">该原Buff内唯一叶子槽。</param><param name="attribute">已核实语义的本项目属性。</param><param name="value">固定点数或百分数。</param><param name="percent">是否按基础值百分比。</param><param name="state">实际动作限制状态。</param>
    internal static void Add(GeneRuntime runtime, MoshentaBuffInstance buff, int slot, GeneAttributeType attribute, float value, bool percent, GeneState state)
    {
        // 负编号仅为服务端内部定义键，不进入玩家装备或技能表。
        int id = -1000000000 - (buff.Id % 10000000) * 16 - slot;
        // GetGeneInfos返回GeneRuntime共同持有的原字典，允许正常有符号int键。
        Dictionary<int, cfg.Gene> definitions = LubanMgr.Instance.GetGeneInfos();
        if (!definitions.ContainsKey(id))
        {
            // cfg.Gene是Luban生成只读类型，此处仅为第三方表反序列化边界。
            ByteBuf buffer = new ByteBuf();
            buffer.WriteInt(id); buffer.WriteString("魔神塔原Buff内部叶子"); buffer.WriteInt(0);
            if (percent) buffer.WriteInt(1); else buffer.WriteInt(0);
            for (int index = 0; index < 5; index++) buffer.WriteInt(0);
            buffer.WriteInt(1); buffer.WriteInt(0);
            if (state != GeneState.None) buffer.WriteInt(4); else buffer.WriteInt(0);
            buffer.WriteInt((int)attribute); buffer.WriteFloat(value); buffer.WriteInt(0); buffer.WriteInt(1);
            buffer.WriteInt((int)state); buffer.WriteInt(0); buffer.WriteInt(0); buffer.WriteInt(10000); buffer.WriteInt(0); buffer.WriteInt(0);
            definitions.Add(id, new cfg.Gene(new ByteBuf(buffer.CopyData())));
        }
        // 原Buff负责计时，GeneTime=0避免第二套计时提前删除叶子。
        ulong sourceId = 0;
        if (buff.Caster != null) sourceId = buff.Caster.UnitId;
        buff.Genes.Add(runtime.AddGene(id, sourceId, null, slot, sourceId));
    }
}
