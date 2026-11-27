using System;
using System.Collections.Generic;
using System.Xml.Linq;

/// <summary>一档终绝周箱的楼层门槛、固定物品和等权随机池，界面与游戏服读取同一份配置。</summary>
public sealed class DungeonFinalRewardTier
{
    /// <summary>面向玩家显示的阶位名称。</summary>
    public string Name;
    /// <summary>计入本阶所需的最低通关层数。</summary>
    public int Floor;
    /// <summary>本档随机池有放回的抽取次数。</summary>
    public int Draws;
    /// <summary>领取时全部发给角色的物品及其数量。</summary>
    public MailItem[] Fixed;
    /// <summary>每次随机抽取一种的正式物品和数量。</summary>
    public MailItem[] Pool;

    /// <summary>按配置顺序读取七档周箱。</summary><param name="xml">客户端资源或服务器内嵌的同一份 XML。</param>
    public static DungeonFinalRewardTier[] Read(string xml)
    {
        // 按门槛从低到高排列，保留配置给出的显示顺序。
        List<DungeonFinalRewardTier> tiers = new List<DungeonFinalRewardTier>();
        // 每个阶位有一个固定物品组和一个随机物品组。
        List<XElement> rows = new List<XElement>(XDocument.Parse(xml).Root.Elements("tier"));
        for (int index = 0; index < rows.Count; index++)
        {
            // 当前阶位的配置行。
            XElement row = rows[index];
            tiers.Add(new DungeonFinalRewardTier { Name = (string)row.Attribute("name"), Floor = (int)row.Attribute("floor"),
                Draws = (int)row.Attribute("draws"), Fixed = Items(row.Element("fixed")), Pool = Items(row.Element("pool")) });
        }
        return tiers.ToArray();
    }

    /// <summary>读取物品表编号与一次发放的件数，和背包已有的物品载荷共用类型。</summary><param name="group">当前阶的必得组或随机组。</param>
    private static MailItem[] Items(XElement group)
    {
        // 配置中的一项代表一种材料及其发放件数。
        List<XElement> nodes = new List<XElement>(group.Elements("item"));
        MailItem[] items = new MailItem[nodes.Count];
        for (int index = 0; index < nodes.Count; index++)
            items[index] = new MailItem { ItemId = (int)nodes[index].Attribute("id"), Count = (int)nodes[index].Attribute("count") };
        return items;
    }

    /// <summary>按本周最高成功楼层取唯一一档周箱，零层没有宝箱。</summary>
    /// <param name="tiers">门槛升序的周箱配置。</param><param name="floor">角色已通关的最高层。</param>
    public static DungeonFinalRewardTier ForFloor(DungeonFinalRewardTier[] tiers, int floor)
    {
        for (int index = tiers.Length - 1; index >= 0; index--)
            if (floor >= tiers[index].Floor) return tiers[index];
        return null;
    }
}
