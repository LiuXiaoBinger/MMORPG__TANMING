using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>按表现行 ID 管理编辑器导出资产的异步加载和粒子实例共享租约。</summary>
public static class SkillRuntimePresentationCache
{
    /// <summary>单条表现资产的地址前缀，后接十进制表现行 ID。</summary>
    private const string LocationPrefix =
        "Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_";

    /// <summary>所有仍由技能核心或粒子实例持有的表现资产加载记录。</summary>
    private static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>();

    /// <summary>一次表现资产加载的状态和引用计数，仅在 Unity 主线程访问。</summary>
    private sealed class Entry
    {
        /// <summary>关联的表现行主键，正整数。</summary>
        public int PresentationId;

        /// <summary>核心与已生成粒子合计持有数，非负整数。</summary>
        public int OwnerCount;

        /// <summary>当前异步加载的 YooAsset 句柄；最终持有者释放时归还。</summary>
        public AssetOperationHandle Handle;

        /// <summary>已加载并通过资产身份检查的只读表现资产。</summary>
        public SkillRuntimePresentation Asset;

        /// <summary>本条记录是否已经发起加载，防止并发重复请求。</summary>
        public bool LoadStarted;

        /// <summary>异步句柄是否已完成；未完成时由回调负责释放失效记录。</summary>
        public bool LoadCompleted;

        /// <summary>尚未收到加载终态的一次性监听；技能取消时主动解除，防止缓存持有已回池实例。</summary>
        public readonly List<Action<SkillRuntimePresentation>> ReadyListeners =
            new List<Action<SkillRuntimePresentation>>();
    }

    /// <summary>为技能核心或粒子实例持有一个表现资产引用，并按需开始异步加载。</summary>
    /// <param name="presentationId">Luban 表现行的正数主键。</param>
    public static void Retain(int presentationId)
    {
        if (presentationId <= 0)
        {
            return;
        }
        // 当前表现行的共享加载记录；相同技能的不同实体共用此句柄。
        Entry entry;
        if (!Entries.TryGetValue(presentationId, out entry))
        {
            entry = new Entry();
            entry.PresentationId = presentationId;
            Entries.Add(presentationId, entry);
        }
        entry.OwnerCount++;
        BeginLoad(entry);
    }

    /// <summary>取得已完全加载的表现资产；尚未就绪时由本次施法等待原表现行。</summary>
    /// <param name="presentationId">已由技能核心持有的表现行主键。</param>
    /// <returns>加载成功且身份正确的资产，否则为空。</returns>
    public static SkillRuntimePresentation GetReady(int presentationId)
    {
        // 查询只返回缓存资产；施法等待通过可取消的一次性就绪监听完成。
        Entry entry;
        if (presentationId <= 0 || !Entries.TryGetValue(presentationId, out entry))
        {
            return null;
        }
        BeginLoad(entry);
        return entry.Asset;
    }

    /// <summary>登记一次资源就绪通知；已有终态时同步交付，失败以空资产通知调用方停止等待。</summary>
    /// <param name="presentationId">本次施法已锁定且由核心持有的正数表现行主键。</param>
    /// <param name="listener">一次性强类型回调；调用方负责捕获施法身份并在取消时解除。</param>
    public static void AddReadyListener(int presentationId, Action<SkillRuntimePresentation> listener)
    {
        // 核心所持有的共享记录；订阅本身不增加资源租约。
        Entry entry;
        if (listener == null || !Entries.TryGetValue(presentationId, out entry))
        {
            return;
        }
        if (entry.LoadCompleted)
        {
            listener(entry.Asset);
            return;
        }
        entry.ReadyListeners.Add(listener);
        BeginLoad(entry);
    }

    /// <summary>取消尚未交付的资源通知，使 Reset 后缓存不再引用旧池对象。</summary>
    /// <param name="presentationId">登记时锁定的表现行主键。</param>
    /// <param name="listener">登记时保存的同一委托引用。</param>
    public static void RemoveReadyListener(int presentationId, Action<SkillRuntimePresentation> listener)
    {
        // 取消只作用于当前行；旧记录已释放时无需再次处理。
        Entry entry;
        if (listener != null && Entries.TryGetValue(presentationId, out entry))
        {
            entry.ReadyListeners.Remove(listener);
        }
    }

    /// <summary>Global 安装资源包后补启动此前持有的记录；生命周期通知不依赖逐帧技能轮询。</summary>
    public static void NotifyResourcePackageReady()
    {
        // 加载完成可能同步释放或新增记录，按原对象快照遍历保护共享容器。
        List<Entry> entries = new List<Entry>(Entries.Values);
        // index 覆盖安装资源包时已存在的记录，不扫描配置中的技能事件。
        for (int index = 0; index < entries.Count; index++)
        {
            // 前一记录的同步完成通知可能释放其他行，只启动仍由当前字典持有的原记录。
            Entry current;
            if (Entries.TryGetValue(entries[index].PresentationId, out current) &&
                ReferenceEquals(current, entries[index]))
            {
                BeginLoad(current);
            }
        }
    }

    /// <summary>从当前记录移除一次性监听后交付终态；单个调用方异常不影响其他施法。</summary>
    /// <param name="entry">已经完成加载或确定失败的共享记录。</param>
    private static void NotifyReady(Entry entry)
    {
        // 委托快照在回调前脱离缓存，回调取消或重新施法不会修改本轮遍历。
        List<Action<SkillRuntimePresentation>> listeners =
            new List<Action<SkillRuntimePresentation>>(entry.ReadyListeners);
        entry.ReadyListeners.Clear();
        // 本轮终态资产快照；首个回调释放记录后其他回调仍收到原始终态并自行校验身份。
        SkillRuntimePresentation asset = entry.Asset;
        // index 按登记顺序交付每个独立施法，过期闭包由 UUID/代次门控拒绝。
        for (int index = 0; index < listeners.Count; index++)
        {
            try
            {
                listeners[index](asset);
            }
            catch (Exception exception)
            {
                // exception 是订阅方抛出的异常；只记录本次失败，不中断同一资产的其他使用者。
                Debug.LogException(exception);
            }
        }
    }

    /// <summary>解除一个核心或粒子实例的持有，最后持有者归还 YooAsset 句柄。</summary>
    /// <param name="presentationId">此前已 Retain 的表现行主键。</param>
    public static void Release(int presentationId)
    {
        // 当前记录可能正等待加载完成；回调会为已失效的记录释放句柄。
        Entry entry;
        if (presentationId <= 0 || !Entries.TryGetValue(presentationId, out entry))
        {
            return;
        }
        entry.OwnerCount--;
        if (entry.OwnerCount > 0)
        {
            return;
        }
        Entries.Remove(presentationId);
        entry.ReadyListeners.Clear();
        entry.Asset = null;
        if (entry.Handle != null && entry.LoadCompleted)
        {
            entry.Handle.Release();
            entry.Handle = null;
        }
    }

    /// <summary>首次可访问 YooAsset 时加载导出资产，失败时记录错误且不启动其他特效来源。</summary>
    /// <param name="entry">仍被至少一个持有者引用的共享记录。</param>
    private static void BeginLoad(Entry entry)
    {
        if (entry.LoadStarted || Global.Instance == null ||
            Global.Instance.YooPackage == null)
        {
            return;
        }
        // 导出器与运行端共用固定 Location 规则，资源收集器必须包含该路径。
        string location = LocationPrefix + entry.PresentationId + ".asset";
        if (!Global.Instance.YooPackage.CheckLocationValid(location))
        {
            entry.LoadStarted = true;
            entry.LoadCompleted = true;
            Debug.LogWarning("技能表现资产未进入资源清单：" + location);
            NotifyReady(entry);
            return;
        }
        entry.LoadStarted = true;
        entry.Handle = Global.Instance.YooPackage.LoadAssetAsync<SkillRuntimePresentation>(
            location);
        entry.Handle.Completed += handle =>
        {
            // handle 是本记录发起的 YooAsset 句柄，过期记录只能归还该句柄，不触及新记录。
            entry.LoadCompleted = true;
            // 核心可能已被回收并为同一 ID 建立新记录，旧完成回调只清理旧句柄。
            Entry current;
            if (!Entries.TryGetValue(entry.PresentationId, out current) ||
                !ReferenceEquals(current, entry))
            {
                handle.Release();
                return;
            }
            if (handle.Status != EOperationStatus.Succeed)
            {
                Debug.LogWarning("技能表现资产加载失败：" + location);
                NotifyReady(entry);
                return;
            }
            // 只有 ID 相符的资产能进入本表现行，技能和剪辑由施法实例取得时校验。
            SkillRuntimePresentation asset =
                handle.GetAssetObject<SkillRuntimePresentation>();
            if (asset == null || asset.PresentationId != entry.PresentationId)
            {
                Debug.LogError("技能表现资产 ID 不匹配：" + location);
                NotifyReady(entry);
                return;
            }
            entry.Asset = asset;
            NotifyReady(entry);
        };
    }
}
