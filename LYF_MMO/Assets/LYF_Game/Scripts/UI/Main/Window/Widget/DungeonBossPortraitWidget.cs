using UnityEngine;
using UnityEngine.UI;

/// <summary>在首领行中显示一只首领的头像、名字和该角色的周期击杀标记。</summary>
public sealed class DungeonBossPortraitWidget : MonoBehaviour
{
    /// <summary>打开此首领资料抽屉的点击区。</summary>
    [SerializeField] private Button button;
    /// <summary>使用怪物配置对应的原版头像。</summary>
    [SerializeField] private RawImage portrait;
    /// <summary>头像下方的首领名称。</summary>
    [SerializeField] private Text title;
    /// <summary>仅当前已击败的头像显示斩痕。</summary>
    [SerializeField] private GameObject defeatMark;
    /// <summary>打开资料时在对应头像下显示的细金线。</summary>
    [SerializeField] private GameObject selection;

    /// <summary>此首领资料的入口。</summary>
    public Button Button => button;
    /// <summary>资料抽屉复用当前头像的同一份贴图。</summary>
    public Texture Portrait => portrait.texture;

    /// <summary>设置此实例对应的首领及肖像。</summary>
    /// <param name="bossName">怪物配置中的显示名。</param><param name="image">当前首领对应的贴图。</param>
    public void Bind(string bossName, Texture image)
    {
        title.text = bossName;
        portrait.texture = image;
    }

    /// <summary>单独更新此首领的暗色及红斩，无死亡记录的其他头像保持原色。</summary>
    /// <param name="defeated">服务器记录表明该角色本周期已击败此首领。</param><param name="selected">资料抽屉是否选中此项。</param>
    public void SetState(bool defeated, bool selected)
    {
        portrait.color = Color.white;
        if (defeated) portrait.color = new Color(0.40f, 0.39f, 0.40f, 1f);
        defeatMark.SetActive(defeated);
        selection.SetActive(selected);
    }
}
