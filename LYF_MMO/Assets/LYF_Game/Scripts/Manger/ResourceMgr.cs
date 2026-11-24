using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/**
 * Title:
 * Description:
 */


/// <summary>
/// YooAsset 资源加载管理器：按路径缓存 Prefab、特效和 Sprite 的异步句柄，
/// 已缓存资源直接实例化，未缓存资源走异步加载并在回调前保持句柄引用。
/// </summary>
public class ResourceMgr : Singleton<ResourceMgr>
{


    /// <summary>按路径缓存的 Prefab 资源句柄；命中后直接 InstantiateSync 出新实例，句柄长期持有。</summary>
    private Dictionary<string, AssetOperationHandle> prefabDic = new Dictionary<string, AssetOperationHandle>();
    /// <summary>按路径缓存的特效资源句柄；与 prefabDic 同样长期持有以避免特效反复加载。</summary>
    private Dictionary<string, AssetOperationHandle> effectDic = new Dictionary<string, AssetOperationHandle>();
    /// <summary>普通 Sprite 的资源句柄，用于维持已缓存资源的生命周期。</summary>
    private Dictionary<string, AssetOperationHandle> _spriteHandleDic = new Dictionary<string, AssetOperationHandle>();
    /// <summary>按图集地址持有已加载的子资源句柄，同页切片共享其生命周期。</summary>
    private Dictionary<string, SubAssetsOperationHandle> _spriteAtlasHandleDic = new Dictionary<string, SubAssetsOperationHandle>();
    /// <summary>按原始配置路径缓存已经加载的 Sprite。</summary>
    private Dictionary<string, Sprite> _spriteDic = new Dictionary<string, Sprite>();
    /// <summary>
    /// 加载并实例化预制体；已缓存时立即创建新对象，否则加载完成后回调。
    /// </summary>
    /// <param name="path">预制体可传 Assets/ 开头的完整资源地址，或传预制体目录下相对路径；特效入口只接收特效目录下相对路径。</param>
    /// <param name="callback">接收本次新建对象的回调；调用方负责对象使用和销毁。</param>
    public void LoadPrefabAsync(string path, Action<GameObject> callback)
    {

        if (prefabDic.ContainsKey(path))
        {
            callback?.Invoke(prefabDic[path].InstantiateSync());
        }
        else
        {
            //Assets/LYF_Game/Prefabs/Resources/UI/Prefabs/Item/BuyItemPrefab.prefab
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
    /// 从特效目录加载资源并创建新对象；缓存句柄供下一次播放复用。
    /// </summary>
    /// <param name="path">预制体可传 Assets/ 开头的完整资源地址，或传预制体目录下相对路径；特效入口只接收特效目录下相对路径。</param>
    /// <param name="callback">接收本次新建对象的回调；调用方负责对象使用和销毁。</param>
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
        // 最后一个斜线把图集路径与切片名称分开。
        int separatorIndex = path.LastIndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= path.Length - 1)
        {
            Debug.LogError($"Sprite 资源加载失败，路径不是有效的图集切片格式：{path}");
            callback?.Invoke(null);
            return;
        }

        // 约定图集主资源为 PNG，去掉切片名后补扩展名。
        string atlasRelativePath = path.Substring(0, separatorIndex) + ".png";
        // 从图集中取出的 Sprite 名称。
        string spriteName = path.Substring(separatorIndex + 1);
        string atlasLocation = $"{ConstDefine.SpritePath}{atlasRelativePath}";

        // 图集主资源也必须先通过清单校验，避免对不存在的地址发起异步请求。
        if (!Global.Instance.YooPackage.CheckLocationValid(atlasLocation))
        {
            Debug.LogError($"图集主资源地址无效：{atlasLocation}，切片：{spriteName}");
            callback?.Invoke(null);
            return;
        }

        // 已完成的同一图集可以直接查找另一张切片。
        if (_spriteAtlasHandleDic.TryGetValue(atlasLocation, out SubAssetsOperationHandle sharedAtlas))
        {
            CacheAtlasSprite(sharedAtlas, atlasLocation, spriteName, path, callback);
            return;
        }

        Global.Instance.YooPackage.LoadSubAssetsAsync<Sprite>(atlasLocation)
            .Completed += (SubAssetsOperationHandle loadedAtlas) =>
        {
            // 失败的临时句柄及时归还，下次请求仍可重新加载。
            if (loadedAtlas.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"图集加载失败：{atlasLocation}");
                loadedAtlas.Release();
                callback?.Invoke(null);
                return;
            }

            // 同帧请求可由同一 YooAsset 提供者完成，管理器只保留一个有效句柄。
            if (!_spriteAtlasHandleDic.TryGetValue(atlasLocation, out SubAssetsOperationHandle retainedAtlas))
            {
                retainedAtlas = loadedAtlas;
                _spriteAtlasHandleDic.Add(atlasLocation, retainedAtlas);
            }
            else
            {
                loadedAtlas.Release();
            }
            CacheAtlasSprite(retainedAtlas, atlasLocation, spriteName, path, callback);
        };

    }
    /// <summary>从共享图集取出指定切片，沿用调用方原始路径缓存成功结果。</summary>
    /// <param name="atlas">持有整页切片的已完成图集句柄。</param>
    /// <param name="atlasLocation">报错时标识缺少切片的图集地址。</param>
    /// <param name="spriteName">配置路径末段对应的切片名称。</param>
    /// <param name="path">供后续相同请求直接命中的完整配置路径。</param>
    /// <param name="callback">把真实切片或缺失时的空值返回本次调用方。</param>
    private void CacheAtlasSprite(SubAssetsOperationHandle atlas, string atlasLocation, string spriteName, string path, Action<Sprite> callback)
    {
        // 并发请求完成时也可能已由前一回调缓存这张切片。
        if (!_spriteDic.TryGetValue(path, out Sprite sprite))
        {
            sprite = atlas.GetSubAssetObject<Sprite>(spriteName);
            if (sprite != null)
            {
                _spriteDic.Add(path, sprite);
            }
            else
            {
                Debug.LogError($"图集切片加载失败，图集：{atlasLocation}，切片：{spriteName}");
            }
        }
        callback?.Invoke(sprite);
    }
}
