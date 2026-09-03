using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/**
 * Title:
 * Description:
 */


public class ResourceMgr : Singleton<ResourceMgr>
{


    private Dictionary<string, AssetOperationHandle> prefabDic = new Dictionary<string, AssetOperationHandle>();
    private Dictionary<string, AssetOperationHandle> effectDic = new Dictionary<string, AssetOperationHandle>();
    private Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();
    /// <summary>
    /// 加载Prefab
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    public void LoadPrefabAsync(string path, Action<GameObject> callback)
    {

        if (prefabDic.ContainsKey(path))
        {
            callback?.Invoke(prefabDic[path].InstantiateSync());
        }
        else
        {
            string assetLocation = path.StartsWith("Assets/", StringComparison.Ordinal)
                ? path
                : $"{ConstDefine.PrefabPath}{path}";

            Global.Instance.YooPackage.LoadAssetAsync(assetLocation)
                .Completed += (AssetOperationHandle handle) =>
            {
                GameObject go = handle.InstantiateSync();

                if (!prefabDic.ContainsKey(path))
                {
                    prefabDic.Add(path, handle);
                }

                callback?.Invoke(go);
            };
        }
        
    }
    /// <summary>
    /// 加载特效资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    public void LoadEffetAsync(string path, Action<GameObject> callback)
    {

        if (effectDic.ContainsKey(path))
        {
            callback?.Invoke(effectDic[path].InstantiateSync());
        }
        else
        {
            Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.EffectPath}{path}")
                .Completed += (AssetOperationHandle handle) =>
            {
                GameObject go = handle.InstantiateSync();

                if (!effectDic.ContainsKey(path))
                {
                    effectDic.Add(path, handle);
                }

                callback?.Invoke(go);
            };
        }

    }
    
    /// <summary>
    /// 加载图片资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    public void LoadSpriteAsync(string path, Action<Sprite> callback)
    {

        if (effectDic.ContainsKey(path))
        {
            callback?.Invoke(_spriteDic[path]);
        }
        else
        {
            Global.Instance.YooPackage.LoadAssetAsync<Sprite>($"{ConstDefine.SpritePath}{path}")
                .Completed += (AssetOperationHandle handle) =>
            {
                Sprite go = handle.GetAssetObject<Sprite>();

                if (!_spriteDic.ContainsKey(path))
                {
                    _spriteDic.Add(path, go);
                }

                callback?.Invoke(go);
            };
        }

    }
}
