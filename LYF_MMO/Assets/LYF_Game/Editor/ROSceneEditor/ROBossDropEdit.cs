using System;
using System.Collections.Generic;

/// <summary>编辑器中一个副本、难度和首领的掉落草稿，切换选项时保留到保存或放弃为止。</summary>
[Serializable]
internal sealed class ROBossDropEdit
{
    /// <summary>一行可编辑的正式物品和单次发放数量。</summary>
    [Serializable]
    internal sealed class Item
    {
        /// <summary>物品或装备表的配置编号。</summary>
        public int id;
        /// <summary>本周期首次击杀时一次入包的件数。</summary>
        public int count = 1;
    }

    /// <summary>副本主表配置编号。</summary>
    public int dungeon;
    /// <summary>副本难度表编号，各层共用同一难度。</summary>
    public int degree;
    /// <summary>场景波次中的实际首领模板号。</summary>
    public int boss;
    /// <summary>新建奖励行时写入的正式首领名称。</summary>
    public string bossName;
    /// <summary>按掉落提示顺序排列的物品，空列表表示只记录击杀。</summary>
    public List<Item> items = new List<Item>();
}
