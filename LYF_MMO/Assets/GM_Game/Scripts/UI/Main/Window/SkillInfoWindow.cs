using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 技能列表窗口，只负责展示控制器提供的技能数据。
/// </summary>
public class SkillInfoWindow : WindowBase
{
    /// <summary>技能条目加载失败后的最短重试间隔，单位为秒。</summary>
    private const float ItemRetrySeconds = 1f;

    /// <summary>运行时技能条目的父节点，条目按快照顺序排列。</summary>
    [SerializeField, Header("技能列表父组件")] private Transform _content;

    /// <summary>当前职业名称文本。</summary>
    [SerializeField, Header("职业")] private TMP_Text _texJob;

    /// <summary>当前可分配技能点数文本。</summary>
    [SerializeField, Header("技能升级点")] private TMP_Text _texPoint;

    /// <summary>按快照索引保存技能条目对象，空引用表示该项尚未加载成功。</summary>
    private readonly List<GameObject> _skillItems = new List<GameObject>();

    /// <summary>各技能条目当前是否存在未完成的异步预制体请求。</summary>
    private readonly List<bool> _itemLoading = new List<bool>();

    /// <summary>各失败条目的下一次允许重试时间，采用本机不缩放秒数。</summary>
    private readonly List<float> _retryAfterRealtime = new List<float>();

    /// <summary>最近一次完整内容快照；冷却时间变化不需要重建列表。</summary>
    private readonly List<SkillViewData> _displaySkills = new List<SkillViewData>();

    /// <summary>异步条目创建所属的内容版本，仅内容变化时递增。</summary>
    private int _listVersion;

    /// <summary>窗口启用期间定期检查加载失败的技能条目，不依赖后续技能快照刷新。</summary>
    private void Update()
    {
        if (_displaySkills.Count > 0)
        {
            LoadMissingSkillItems();
        }
    }

    /// <summary>刷新技能内容；同一批技能的冷却变化不作废正在加载的条目。</summary>
    /// <param name="obj">技能控制器生成的强类型展示列表。</param>
    public override void ReFreshUI(object obj)
    {
        // WindowBase 的 object 签名属于 UI 框架边界，此处立即转换为强类型技能列表。
        List<SkillViewData> skills = obj as List<SkillViewData>;
        if (skills == null)
        {
            return;
        }
        if (HasSameContent(skills))
        {
            LoadMissingSkillItems();
            return;
        }
        ClearSkillItems();
        _displaySkills.Clear();
        // 新内容快照的技能索引，保持与条目状态列表一一对应。
        for (int index = 0; index < skills.Count; index++)
        {
            _displaySkills.Add(skills[index]);
        }
        _listVersion++;
        // 新内容快照的技能索引，初始化各项异步加载状态。
        for (int index = 0; index < skills.Count; index++)
        {
            _skillItems.Add(null);
            _itemLoading.Add(false);
            _retryAfterRealtime.Add(0f);
        }
        LoadMissingSkillItems();
    }

    /// <summary>比较列表中影响条目外观的稳定字段，忽略逐次变化的剩余冷却。</summary>
    /// <param name="skills">本次控制器生成的技能展示快照。</param>
    /// <returns>条目数量、顺序及展示内容均未变化时返回 true。</returns>
    private bool HasSameContent(List<SkillViewData> skills)
    {
        if (_displaySkills.Count != skills.Count)
        {
            return false;
        }
        // 两次快照的技能索引，用于逐项比较稳定展示字段。
        for (int index = 0; index < skills.Count; index++)
        {
            // 当前新旧条目，空值只在两侧同时为空时视为一致。
            SkillViewData current = skills[index];
            SkillViewData previous = _displaySkills[index];
            if (current == null || previous == null)
            {
                if (current != previous)
                {
                    return false;
                }
                continue;
            }
            // 技能 ID 或等级发生变化即视为快照不一致。
            bool identityChanged = current.SkillId != previous.SkillId || current.Level != previous.Level;
            // 名称或描述发生变化即视为快照不一致。
            bool presentationChanged = current.Name != previous.Name || current.Description != previous.Description;
            // 图标路径发生变化即视为快照不一致。
            bool iconChanged = current.IconPath != previous.IconPath;
            if (identityChanged || presentationChanged || iconChanged)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>更新技能窗口顶部的职业和可用点数。</summary>
    public void SetHeader(string jobName, int availablePoints)
    {
        if (_texJob != null)
        {
            _texJob.text = jobName;
        }
        if (_texPoint != null)
        {
            _texPoint.text = availablePoints.ToString();
        }
    }

    /// <summary>只为尚未成功创建且不在加载中的技能条目发起异步请求。</summary>
    private void LoadMissingSkillItems()
    {
        // 当前技能内容版本，回调用它拒绝已经失效的旧资源请求。
        int listVersion = _listVersion;
        // 本次刷新时的实时时钟，失败条目仅在重试间隔结束后再次加载。
        float realtimeSeconds = Time.realtimeSinceStartup;
        // 当前技能快照索引，也是三个条目状态列表的共同索引。
        for (int index = 0; index < _displaySkills.Count; index++)
        {
            // 无展示数据、已创建条目或正在加载时跳过。
            if (_displaySkills[index] == null || _skillItems[index] != null)
            {
                continue;
            }
            // 正在加载或重试间隔未到时跳过。
            if (_itemLoading[index] || realtimeSeconds < _retryAfterRealtime[index])
            {
                continue;
            }
            AddSkillItem(_displaySkills[index], listVersion, index);
        }
    }

    /// <summary>异步创建指定索引的技能条目；失败后保留该索引供后续刷新重试。</summary>
    /// <param name="skill">本项技能的名称、等级及图标展示数据。</param>
    /// <param name="listVersion">创建请求所属的内容版本。</param>
    /// <param name="index">技能快照和条目状态列表的共同索引。</param>
    private void AddSkillItem(SkillViewData skill, int listVersion, int index)
    {
        _itemLoading[index] = true;
        // 回调参数为当前索引的预制体实例，资源加载失败时为空。
        ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/SkillItemWidget", itemObject =>
        {
            if (listVersion != _listVersion)
            {
                if (itemObject != null)
                {
                    Destroy(itemObject);
                }
                return;
            }
            _itemLoading[index] = false;
            if (itemObject == null || _content == null)
            {
                _retryAfterRealtime[index] = Time.realtimeSinceStartup + ItemRetrySeconds;
                return;
            }
            // 预制体必须包含正式条目组件，损坏资源不应占用成功槽位。
            SkillItemWidget widget = itemObject.GetComponent<SkillItemWidget>();
            if (widget == null)
            {
                Destroy(itemObject);
                _retryAfterRealtime[index] = Time.realtimeSinceStartup + ItemRetrySeconds;
                return;
            }
            itemObject.transform.SetParent(_content, false);
            _skillItems[index] = itemObject;
            OrderSkillItems();
            widget.RefreshUI(skill);
        });
    }

    /// <summary>按技能快照索引重排已创建条目，消除异步回调乱序。</summary>
    private void OrderSkillItems()
    {
        // 当前已加载条目的快照索引，空位保持待重试状态。
        for (int index = 0; index < _skillItems.Count; index++)
        {
            // 对应快照索引的条目对象，存活时移到已创建条目末尾。
            GameObject item = _skillItems[index];
            if (item != null)
            {
                item.transform.SetAsLastSibling();
            }
        }
    }

    /// <summary>销毁上一个技能内容版本创建的全部条目。</summary>
    private void ClearSkillItems()
    {
        // 旧快照中的条目索引，空位代表该条目尚未加载成功。
        for (int index = 0; index < _skillItems.Count; index++)
        {
            GameObject item = _skillItems[index];
            if (item != null)
            {
                Destroy(item);
            }
        }
        _skillItems.Clear();
        _itemLoading.Clear();
        _retryAfterRealtime.Clear();
    }
}
