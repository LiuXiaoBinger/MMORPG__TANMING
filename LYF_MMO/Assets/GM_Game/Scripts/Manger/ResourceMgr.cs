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
    /// <summary>普通 Sprite 的资源句柄，用于维持已缓存资源的生命周期。</summary>
    private Dictionary<string, AssetOperationHandle> _spriteHandleDic = new Dictionary<string, AssetOperationHandle>();
    /// <summary>图集子 Sprite 的资源句柄，用于维持已缓存切片的生命周期。</summary>
    private Dictionary<string, SubAssetsOperationHandle> _spriteAtlasHandleDic = new Dictionary<string, SubAssetsOperationHandle>();
    /// <summary>按原始配置路径缓存已经加载的 Sprite。</summary>
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
            //Assets/artres/Resources/UI/Prefabs/Item/BuyItemPrefab.prefab
            string assetLocation;
            if (path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                assetLocation = path;
            }
            else
            {
                assetLocation = $"{ConstDefine.PrefabPath}{path}";
            }

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
    /// 加载图片资源。优先按普通 Sprite 加载，失败后再按“图集路径/切片名称”加载图集切片。
    /// </summary>
    /// <param name="path">相对于 UISprites 目录的资源路径。</param>
    /// <param name="callback">加载完成回调，加载失败时返回空对象。</param>
    public void LoadSpriteAsync(string path, Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Sprite 资源路径不能为空。");
            callback?.Invoke(null);
            return;
        }

        Sprite cachedSprite;
        if (_spriteDic.TryGetValue(path, out cachedSprite))
        {
            callback?.Invoke(cachedSprite);
            return;
        }

        string assetLocation = $"{ConstDefine.SpritePath}{path}";
        // 先检查清单地址，图集切片的虚拟路径无效时直接走图集加载，避免 YooAsset 输出无效地址错误。
        if (!Global.Instance.YooPackage.CheckLocationValid(assetLocation))
        {
            LoadSpriteFromAtlas(path, callback);
            return;
        }

        Global.Instance.YooPackage.LoadAssetAsync<Sprite>(assetLocation)
            .Completed += (AssetOperationHandle handle) =>
        {
            Sprite sprite = null;
            if (handle.Status == EOperationStatus.Succeed)
            {
                sprite = handle.GetAssetObject<Sprite>();
            }

            if (sprite != null)
            {
                if (!_spriteDic.ContainsKey(path))
                {
                    _spriteDic.Add(path, sprite);
                    _spriteHandleDic.Add(path, handle);
                }
                else
                {
                    // 同一路径并发完成时只保留首个有效句柄，避免重复占用资源。
                    handle.Release();
                }

                callback?.Invoke(sprite);
                return;
            }

            // 普通资源加载失败后释放无效句柄，再尝试解析为图集切片路径。
            handle.Release();
            LoadSpriteFromAtlas(path, callback);
        };
    }

    /// <summary>
    /// 将“图集相对路径/切片名称”解析为图集主资源，并加载指定 Sprite 切片。
    /// </summary>
    /// <param name="path">包含图集目录和切片名称的原始配置路径。</param>
    /// <param name="callback">加载完成回调，加载失败时返回空对象。</param>
    private void LoadSpriteFromAtlas(string path, Action<Sprite> callback)
    {
        int separatorIndex = path.LastIndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= path.Length - 1)
        {
            Debug.LogError($"Sprite 资源加载失败，路径不是有效的图集切片格式：{path}");
            callback?.Invoke(null);
            return;
        }

        string atlasRelativePath = path.Substring(0, separatorIndex) + ".png";
        string spriteName = path.Substring(separatorIndex + 1);
        string atlasLocation = $"{ConstDefine.SpritePath}{atlasRelativePath}";

        // 图集主资源也必须先通过清单校验，避免对不存在的地址发起异步请求。
        if (!Global.Instance.YooPackage.CheckLocationValid(atlasLocation))
        {
            Debug.LogError($"图集主资源地址无效：{atlasLocation}，切片：{spriteName}");
            callback?.Invoke(null);
            return;
        }

        Global.Instance.YooPackage.LoadSubAssetsAsync<Sprite>(atlasLocation)
            .Completed += (SubAssetsOperationHandle atlasHandle) =>
        {
            Sprite sprite = null;
            if (atlasHandle.Status == EOperationStatus.Succeed)
            {
                sprite = atlasHandle.GetSubAssetObject<Sprite>(spriteName);
            }

            if (sprite == null)
            {
                Debug.LogError($"图集切片加载失败，图集：{atlasLocation}，切片：{spriteName}");
                atlasHandle.Release();
                callback?.Invoke(null);
                return;
            }

            if (!_spriteDic.ContainsKey(path))
            {
                _spriteDic.Add(path, sprite);
                _spriteAtlasHandleDic.Add(path, atlasHandle);
            }
            else
            {
                // 同一路径并发完成时只保留首个有效句柄，避免重复占用资源。
                atlasHandle.Release();
            }

            callback?.Invoke(sprite);
        };

    }
}
