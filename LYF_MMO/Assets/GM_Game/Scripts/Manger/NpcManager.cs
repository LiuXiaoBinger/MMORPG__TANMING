using System.Collections.Generic;
using MMoRpgCommon;
using UnityEngine;

/// <summary>
/// 根据 Luban NPC 配置动态创建场景 NPC。
/// </summary>
public class NpcManager : MonoBehaviour
{
    [SerializeField, Header("NPC 实例父节点，可为空")] private Transform _npcRoot;

    private readonly Dictionary<int, NpcEntity> _npcEntities = new Dictionary<int, NpcEntity>();
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Dictionary<int, NpcParseInfo> npcInfos = LubanMgr.Instance.GetNpcInfos();
        if (npcInfos == null || npcInfos.Count == 0)
        {
            Debug.LogWarning("没有可创建的 NPC 配置。", this);
            return;
        }

        foreach (KeyValuePair<int, NpcParseInfo> pair in npcInfos)
        {
            CreateNpc(pair.Value);
        }
    }

    public NpcEntity GetNpc(int npcId)
    {
        _npcEntities.TryGetValue(npcId, out NpcEntity entity);
        return entity;
    }

    private void CreateNpc(NpcParseInfo info)
    {
        if (info == null || _npcEntities.ContainsKey(info.ID))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(info.PrefabPath))
        {
            Debug.LogWarning($"NPC {info.ID} 没有配置预制体路径。", this);
            return;
        }

        ResourceMgr.Instance.LoadPrefabAsync(info.PrefabPath, npcObject =>
        {
            if (npcObject == null)
            {
                Debug.LogError($"NPC {info.ID} 预制体加载失败：{info.PrefabPath}", this);
                return;
            }

            if (_npcRoot != null)
            {
                npcObject.transform.SetParent(_npcRoot, true);
            }

            Vector3 spawnPosition = new Vector3(info.Position.X, info.Position.Y, info.Position.Z);
            if (spawnPosition == Vector3.zero)
            {
                // 坐标未配置时使用管理器所在位置，避免 NPC 在场景原点掉出地图。
                spawnPosition = transform.position;
            }

            npcObject.transform.position = spawnPosition;
            npcObject.name = string.IsNullOrEmpty(info.Name) ? $"NPC_{info.ID}" : info.Name;

            NpcEntity entity = CreateEntity(info);
            NpcCtrl npcCtrl = npcObject.GetComponent<NpcCtrl>();
            if (npcCtrl == null)
            {
                npcCtrl = npcObject.AddComponent<NpcCtrl>();
            }

            npcCtrl.Initialize(entity);
            _npcEntities[info.ID] = entity;
        });
    }

    private static NpcEntity CreateEntity(NpcParseInfo info)
    {
        NpcEntity entity = new NpcEntity
        {
            EntityID = info.ID,
            NpcID = info.ID,
            NpcType = info.Type,
            Name = info.Name ?? string.Empty,
            MapID = info.MapID,
            PrefabPath = info.PrefabPath ?? string.Empty,
            Think = info.Think ?? string.Empty,
            Talk = info.Talk ?? string.Empty,
            Position = info.Position.ToString(),
        };

        if (info.ShopItemList != null)
        {
            foreach (NpcShopItem shopItem in info.ShopItemList)
            {
                if (shopItem == null)
                {
                    continue;
                }

                entity.ShopItemList.Add(new NpcShopData
                {
                    ShopID = shopItem.ShopID,
                    LimitType = (int)shopItem.LimitType,
                    LimitCount = shopItem.LimitCount,
                });
            }
        }

        return entity;
    }
}
