using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>管理江湖聊天的设置及表情、玩具弹窗中的本地分页。</summary>
public sealed class YimengChatPages : MonoBehaviour
{
    /// <summary>各页选中时显示的浅色底纹，数组顺序与内容页一致。</summary>
    [SerializeField] private Image[] _selection;
    /// <summary>分页文字，通过颜色区分普通和选中态。</summary>
    [SerializeField] private TMP_Text[] _captions;
    /// <summary>分页各自的 UI 容器，同时只显示一个。</summary>
    [SerializeField] private GameObject[] _pages;
    /// <summary>当前选中页，从零计数。</summary>
    private int _current;

    /// <summary>切换分页及选中状态，供预制体按钮调用。</summary>
    /// <param name="index">目标页下标，从零计数。</param>
    public void SelectPage(int index)
    {
        _current = index;
        // 遍历预制体中同序的内容页、文字和选中底纹。
        for (int i = 0; i < _pages.Length; i++)
        {
            // 此页是否与刚选中的下标相同。
            bool selected = i == index;
            _pages[i].SetActive(selected);
            _selection[i].enabled = selected;
            if (selected)
                _captions[i].color = new Color32(14, 22, 23, 255);
            else
                _captions[i].color = new Color32(172, 185, 182, 255);
        }
    }

    /// <summary>翻到同一页组的上一页，首尾循环。</summary>
    public void PreviousPage()
    {
        SelectPage((_current + _pages.Length - 1) % _pages.Length);
    }

    /// <summary>翻到同一页组的下一页，首尾循环。</summary>
    public void NextPage()
    {
        SelectPage((_current + 1) % _pages.Length);
    }
}
