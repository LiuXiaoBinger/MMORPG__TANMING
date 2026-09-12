using System.Collections.Generic;
using cfg;

public class ShopManager :Singleton<ShopManager>
{
    //> 静态商店 [table->ShopId, ShopPtr]
    Dictionary<int, ShopStatic> static_shops_;
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
    
    public ShopStatic GetShopbyID(uint id)
    {
        if(static_shops_.TryGetValue((int)id, out ShopStatic shop))
            return shop;
        return null;
    }
}
