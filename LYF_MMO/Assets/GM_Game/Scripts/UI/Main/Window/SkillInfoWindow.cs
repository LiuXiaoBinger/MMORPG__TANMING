using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 技能列表窗口，只负责展示控制器提供的技能数据。
/// </summary>
public class SkillInfoWindow : WindowBase
{
    [SerializeField, Header("技能列表父组件")] private Transform _content;
    [SerializeField, Header("职业")] private TMP_Text _texJob;
    [SerializeField, Header("技能升级点")] private TMP_Text _texPoint;

    private readonly List<GameObject> _skillItems = new List<GameObject>();
    private int _listVersion;

    public override void ReFreshUI(object obj)
    {
        List<SkillViewData> skills = obj as List<SkillViewData>;
        if (skills == null)
        {
            return;
        }
        ClearSkillItems();
        _listVersion++;
        for (int index = 0; index < skills.Count; index++)
        {
            AddSkillItem(skills[index], _listVersion);
        }
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

    private void AddSkillItem(SkillViewData skill, int listVersion)
    {
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
            if (itemObject == null || _content == null)
            {
                return;
            }
            itemObject.transform.SetParent(_content, false);
            _skillItems.Add(itemObject);
            SkillItemWidget widget = itemObject.GetComponent<SkillItemWidget>();
            if (widget != null)
            {
                widget.RefreshUI(skill);
            }
        });
    }

    private void ClearSkillItems()
    {
        for (int index = 0; index < _skillItems.Count; index++)
        {
            GameObject item = _skillItems[index];
            if (item != null)
            {
                Destroy(item);
            }
        }
        _skillItems.Clear();
    }
}
