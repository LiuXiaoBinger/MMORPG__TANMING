using UnityEngine;

/// <summary>设置窗口视图，保留RO的四类页面；本轮仅负责页面显隐，不修改游戏设置。</summary>
public sealed class SettingPanel : WindowBase
{
    /// <summary>依次为系统、个人、挂机、推送的页面根节点。</summary>
    [SerializeField] private GameObject[] _pages;

    /// <summary>各页签被选中时显示的原RO蓝色页签节点，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOn;

    /// <summary>各页签未选中时显示的原RO浅色页签节点，与页面下标一一对应。</summary>
    [SerializeField] private GameObject[] _tabOff;

    /// <summary>每次打开设置窗口时展示系统设置页。</summary>
    private void OnEnable()
    {
        SelectOneHandler(0);
    }

    /// <summary>沿RO设置页的选择入口切换页面，仅处理窗口内的显示状态。</summary>
    /// <param name="index">页面下标，0系统、1个人、2挂机、3推送，由预制体页签按钮传入。</param>
    public void SelectOneHandler(int index)
    {
        // 按页面顺序同步内容与页签外观，保证同一时刻仅显示一个页面。
        for (int pageIndex = 0; pageIndex < _pages.Length; pageIndex++)
        {
            _pages[pageIndex].SetActive(pageIndex == index);
            _tabOn[pageIndex].SetActive(pageIndex == index);
            _tabOff[pageIndex].SetActive(pageIndex != index);
        }
    }
}
