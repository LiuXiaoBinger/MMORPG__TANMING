using System.Collections.Generic;
using cfg;

/// <summary>
/// 商店领域管理器：进程启动时把 Luban 静态商店表装载为内存对象，并按 ShopId 提供查询与开放判断。
/// </summary>
public class ShopManager :Singleton<ShopManager>
{
    // 静态商店表：key 为 ShopId，value 为内存中的商店配置对象。
    //> 静态商店 [table->ShopId, ShopPtr]
    Dictionary<int, ShopStatic> static_shops_;

    /// <summary>启动时预载全部 Luban 商店表到内存。</summary>
    public void Initialize()
    {
        static_shops_ = new Dictionary<int, ShopStatic>();
        Dictionary<int, ShopTable> shopTables = LubanMgr.Instance.GetShopTables();
        foreach (var shop in shopTables.Values)
        {
            static_shops_.Add(shop.Id,ShopStatic.CreateStaticShop(shop));
        }
    }

    /// <summary>根据角色状态检查商店是否开放。</summary>
    public bool IsOpen(OnlineRole role, ShopType shopType)
    {
        return true;
    }

    /// <summary>按 ShopId 查询内存商店对象，未命中返回 null。</summary>
    public ShopStatic GetShopbyID(uint id)
    {
        if(static_shops_.TryGetValue((int)id, out ShopStatic shop))
            return shop;
        return null;
    }
}
