using System.Collections.Generic;
using System.IO;
using System.Xml;

/// <summary>与 RO GiftTable 对应的本项目赠礼物品清单，客户端筛选和游戏服扣除读取同一份资源。</summary>
public static class GiftTable
{
    /// <summary>可以从背包取出并作为绑定附件寄送的物品配置编号。</summary>
    private static readonly HashSet<int> items = new HashSet<int>();

    /// <summary>首次使用赠礼时读取随 Network 发布的物品清单。</summary>
    static GiftTable()
    {
        // 礼物表随程序集分发，背包和服务端始终使用同一组物品编号。
        using (Stream resource = typeof(GiftTable).Assembly.GetManifestResourceStream("GiftTable.xml"))
        using (XmlReader reader = XmlReader.Create(resource))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Gift")
                    items.Add(int.Parse(reader.GetAttribute("ItemId")));
            }
        }
    }

    /// <summary>检查配置允许赠送；实际绑定、有效期和堆叠数量另由当前背包核实。</summary>
    /// <param name="itemId">物品配置表的稳定编号。</param>
    public static bool Contains(int itemId) { return items.Contains(itemId); }
}
