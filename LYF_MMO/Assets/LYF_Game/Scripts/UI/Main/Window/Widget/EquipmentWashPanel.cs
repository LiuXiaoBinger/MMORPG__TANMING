using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>装备工坊的洗练页、萃石选择及候选对比窗口。展示内容按原装备 UID 绑定。</summary>
public sealed class EquipmentWashPanel : MonoBehaviour
{
    /// <summary>选择材料、开始洗练及查看上一次候选的入口。</summary>
    [SerializeField] private Button stoneButton, rollButton, previewButton;
    /// <summary>三档材料选择层、候选对比层和替换确认层。</summary>
    [SerializeField] private GameObject picker, comparison, confirmation;
    /// <summary>各层的取消操作以及对比层继续洗练、请求替换、确认替换按钮。</summary>
    [SerializeField] private Button closePicker, closeComparison, reroll, replace, cancel, confirm;
    /// <summary>从初级到高级的三档材料按钮。</summary>
    [SerializeField] private Button[] choices;
    /// <summary>各档库存和条数上限的文字。</summary>
    [SerializeField] private Text[] choiceLabels;
    /// <summary>当前方案列表、材料库存、费用和对比提示。</summary>
    [SerializeField] private Text activeText, material, price, previousScore, nextScore, repeatCost;
    /// <summary>左右两组各五行，左边为生效方案。</summary>
    [SerializeField] private Text[] rows;
    /// <summary>与十行词条顺序相同的数值进度。</summary>
    [SerializeField] private Image[] bars;
    /// <summary>萃玉选择层、主入口和免用萃玉入口。</summary>
    [SerializeField] private GameObject jadePicker;
    /// <summary>打开或关闭萃玉列表以及取消用玉。</summary>
    [SerializeField] private Button chooseJade, closeJade, noJade;
    /// <summary>四档萃玉及生效的五个锁位。</summary>
    [SerializeField] private Button[] jadeChoices, lockSlots;
    /// <summary>四档萃玉库存及各洗练槽的锁定状态。</summary>
    [SerializeField] private Text[] jadeLabels, lockLabels;
    /// <summary>当前所选萃玉与锁定容量提示。</summary>
    [SerializeField] private Text jadeName;
    /// <summary>本次采用的萃玉编号，零表示不锁词条。</summary>
    private int jade;
    /// <summary>选中要保留的生效槽，低五位按详情中的当前顺序。</summary>
    private int locks;
    /// <summary>连接控制器的现有养成窗口。</summary>
    private EquipmentWorkshopWindow owner;
    /// <summary>服务器同步的当前装备状态。</summary>
    private RoleEquipGeneInfo state;
    /// <summary>本次界面选中的萃石编号。</summary>
    private int stone = EquipmentWash.FirstStone;

    /// <summary>只在养成窗口初始化时绑定正式预制体控件。</summary>
    /// <param name="window">持有装备交易接口的窗口。</param>
    public void Init(EquipmentWorkshopWindow window)
    {
        owner = window;
        chooseJade.onClick.AddListener(() => { Refresh(); jadePicker.SetActive(true); });
        closeJade.onClick.AddListener(() => jadePicker.SetActive(false));
        noJade.onClick.AddListener(() => SelectJade(0));
        for (int index = 0; index < jadeChoices.Length; index++)
        {
            int id = EquipmentWash.FirstJade + index; // 当前行所选的材料编号。
            jadeChoices[index].onClick.AddListener(() => SelectJade(id));
        }
        for (int index = 0; index < lockSlots.Length; index++)
        {
            int slot = index; // 固定绑定这条已生效属性。
            lockSlots[index].onClick.AddListener(() => ToggleLock(slot));
        }
        stoneButton.onClick.AddListener(() => { Refresh(); picker.SetActive(true); });
        closePicker.onClick.AddListener(() => picker.SetActive(false));
        for (int index = 0; index < choices.Length; index++)
        {
            int id = EquipmentWash.FirstStone + index; // 此按钮绑定的实际材料编号。
            choices[index].onClick.AddListener(() => SelectStone(id));
        }
        rollButton.onClick.AddListener(() => Send(EquipOperation.RollWash));
        previewButton.onClick.AddListener(OpenComparison);
        closeComparison.onClick.AddListener(Hide);
        reroll.onClick.AddListener(() => Send(EquipOperation.RollWash));
        replace.onClick.AddListener(() => confirmation.SetActive(true));
        cancel.onClick.AddListener(() => confirmation.SetActive(false));
        confirm.onClick.AddListener(() => Send(EquipOperation.ApplyWash));
    }

    /// <summary>退出页面时关闭临时窗口，候选随装备保存在服务器。</summary>
    public void Hide()
    {
        picker.SetActive(false);
        jadePicker.SetActive(false);
        comparison.SetActive(false);
        confirmation.SetActive(false);
    }

    /// <summary>重新打开工坊时不带入上一次遮罩。</summary>
    private void OnDisable() { Hide(); }

    /// <summary>从材料选择或背包的材料入口指定下一次采用的档位。</summary>
    /// <param name="id">三档萃石之一的物品编号。</param>
    public void SelectStone(int id)
    {
        stone = id;
        if (state != null && !EquipmentWash.CanLock(state, stone, jade, locks)) locks = 0;
        picker.SetActive(false);
        Refresh();
    }

    /// <summary>切换萃玉时保留仍合法的勾选；切换到不使用则清空全部勾选。</summary>
    /// <param name="id">四档萃玉或零。</param>
    public void SelectJade(int id)
    {
        jade = id;
        if (state != null && !EquipmentWash.CanLock(state, stone, jade, locks)) locks = 0;
        jadePicker.SetActive(false);
        Refresh();
    }

    /// <summary>勾选已存在的词条，受所选萃玉及萃石的档位共同限制。</summary>
    /// <param name="slot">玩家点击的当前生效槽。</param>
    private void ToggleLock(int slot)
    {
        if (jade == 0) { jadePicker.SetActive(true); return; }
        int next = locks ^ (1 << slot); // 再次点击已锁项即取消该项。
        if (!EquipmentWash.CanLock(state, stone, jade, next))
        { TipsMgr.Instance.ShowSystemTips("已达当前萃玉或萃石的锁定上限"); return; }
        locks = next;
        Refresh();
    }

    /// <summary>装备列表、物品增量或结算回执更新当前状态。</summary>
    /// <param name="value">与当前所选 UID 相符的生效和候选两组词条。</param>
    public void Bind(RoleEquipGeneInfo value)
    {
        if (state == null || state.ItemUid != value.ItemUid)
        {
            locks = 0;
            Hide();
        }
        else if (state.WashRevision > value.WashRevision) return;
        else if (state.WashRevision != value.WashRevision) confirmation.SetActive(false);
        state = value;
        if (!EquipmentWash.CanLock(state, stone, jade, locks)) locks = 0;
        activeText.text = Describe(value);
        previewButton.gameObject.SetActive(value.WashCandidate.Length > 0);
        if (value.WashCandidate.Length == 0) Hide();
        else if (comparison.activeSelf) PaintComparison();
        Refresh();
    }

    /// <summary>按真实库存刷新材料选择项及继续洗练的价格。</summary>
    public void Refresh()
    {
        if (owner == null || owner.Owned == null) return;
        for (int index = 0; index < jadeChoices.Length; index++)
        {
            int id = EquipmentWash.FirstJade + index; // 以正式背包库存显示“持有/1”。
            jadeLabels[index].text = "<b>" + StoneName(id) + "</b>    " + owner.Owned(id) + "/1\n洗练时可锁定 " + (index + 1) + " 条属性";
        }
        jadeName.text = "选择萃玉";
        if (jade != 0) jadeName.text = StoneName(jade) + "  " + owner.Owned(jade) + "/1";
        bool any = false; // 尚无生效属性时保留原空方案说明。
        for (int slot = 0; slot < lockSlots.Length; slot++)
        {
            int id = 0; // 当前实际可勾选的基因编号。
            if (state != null) id = EquipmentWash.Id(state, slot);
            lockSlots[slot].gameObject.SetActive(id != 0);
            if (id == 0) continue;
            any = true;
            string prefix = "◇  "; // 已勾选会明确显示“已锁”。
            if ((locks & (1 << slot)) != 0) prefix = "已锁  ";
            lockLabels[slot].text = prefix + Line(id, EquipmentWash.Value(state, slot));
        }
        activeText.gameObject.SetActive(!any);
        for (int index = 0; index < choices.Length; index++)
        {
            int id = EquipmentWash.FirstStone + index; // 以配置表中的材料名向玩家展示选择。
            choiceLabels[index].text = StoneName(id) + "    最多 " + EquipmentWash.Limit(id) + " 条    持有 " + owner.Owned(id);
        }
        material.text = StoneName(stone) + "  " + owner.Owned(stone) + "/3";
        price.text = EquipmentWash.Cost(stone).ToString("N0");
        repeatCost.text = StoneName(stone) + " ×3    铜币 " + EquipmentWash.Cost(stone).ToString("N0");
        if (locks != 0) repeatCost.text += "    " + StoneName(jade) + " ×1";
    }

    /// <summary>按正式物品编号读取材料中文名。</summary>
    /// <param name="id">洗练材料编号。</param>
    private static string StoneName(int id) { return LubanMgr.Instance.GetItemInfoById(id).Name; }

    /// <summary>向控制器提交操作，仅带所选档位和当前可见版本。</summary>
    /// <param name="operation">生成候选或确认替换。</param>
    private void Send(EquipOperation operation)
    {
        if (state == null || owner.SelectedItemUid != state.ItemUid) return;
        owner.SendTrade?.Invoke(new EquipOperationReq { ItemUid = state.ItemUid, Operation = operation,
            WashStone = stone, WashJade = jade, WashLocks = locks, WashRevision = state.WashRevision });
    }

    /// <summary>在当前装备和页面仍匹配时才展示回包。</summary>
    /// <param name="reply">游戏服给此次操作的实际结算。</param>
    public void Receive(EquipOperationRet reply)
    {
        if (!gameObject.activeInHierarchy || reply.ItemUid != owner.SelectedItemUid || reply.WashInfo == null) return;
        Bind(reply.WashInfo);
        if (reply.CmdCode != CmdCode.Succeed) { confirmation.SetActive(false); return; }
        if (reply.Operation == EquipOperation.RollWash) OpenComparison();
        else { locks = 0; Hide(); Refresh(); }
    }

    /// <summary>恢复上次候选的材料档位，在原方案旁显示候选。</summary>
    private void OpenComparison()
    {
        if (state == null || state.WashCandidate.Length == 0) return;
        SelectStone(state.WashStone);
        RoleEquipGeneInfo saved = EquipmentWash.Decode(state.WashCandidate); // 继续洗练沿用该候选的原锁位和萃玉档。
        jade = saved.WashJade;
        locks = saved.WashLocks;
        Refresh();
        confirmation.SetActive(false);
        comparison.SetActive(true);
        PaintComparison();
    }

    /// <summary>更新评分、十行文本和每条相对于满值的进度。</summary>
    private void PaintComparison()
    {
        RoleEquipGeneInfo candidate = EquipmentWash.Decode(state.WashCandidate); // 解出右列五槽候选。
        int before = EquipmentWash.Score(state); // 左侧原属性总分。
        int after = EquipmentWash.Score(candidate); // 右侧候选总分。
        previousScore.text = "原属性  ·  " + before;
        nextScore.text = "新属性  ·  " + after;
        if (after > before) nextScore.text += " ↑";
        else if (after < before) nextScore.text += " ↓";
        for (int index = 0; index < rows.Length; index++)
        {
            RoleEquipGeneInfo source = state; // 前五行属于原方案。
            if (index >= EquipmentWash.Slots) source = candidate;
            int slot = index % EquipmentWash.Slots; // 组内槽位编号。
            int id = EquipmentWash.Id(source, slot); // 空槽整行隐藏，首行例外用于空态。
            int value = EquipmentWash.Value(source, slot); // 实际词条点数。
            rows[index].text = string.Empty;
            if (id != 0) rows[index].text = Line(id, value);
            else if (slot == 0) rows[index].text = "暂无属性";
            RectTransform bar = bars[index].rectTransform; // 填充节点相对固定轨道宽度的比例。
            bar.anchorMax = new Vector2(Mathf.Clamp01((float)value / EquipmentWash.Maximum(id)), 1f);
            bar.parent.gameObject.SetActive(id != 0);
        }
    }

    /// <summary>详情页和工坊共用同一套词条名、序号和点数。</summary>
    /// <param name="value">仅传入已确认的词条快照。</param>
    public static string Describe(RoleEquipGeneInfo value)
    {
        StringBuilder lines = new StringBuilder(); // 一条词条占一整行。
        for (int slot = 0; slot < EquipmentWash.Slots; slot++)
        {
            int id = EquipmentWash.Id(value, slot); // 旧装备可有不连续的空槽。
            if (id == 0) continue;
            if (lines.Length > 0) lines.Append('\n');
            lines.Append("◇ ").Append(Line(id, EquipmentWash.Value(value, slot)));
        }
        if (lines.Length == 0) return "暂无洗练属性";
        return lines.ToString();
    }

    /// <summary>生成如“火系穿透：3”的单行装备属性。</summary>
    /// <param name="id">词条基因编号。</param>
    /// <param name="value">装备实例的实得数值。</param>
    private static string Line(int id, int value)
    {
        cfg.Gene gene = LubanMgr.Instance.GetGeneInfoById(id); // 兼容旧存档中不在当前抽取池的基因。
        string name = "属性 " + id;
        if (gene != null) name = KnapsackCtrl.ShortGeneName(gene.EffectDesc).Replace("属性穿透", "系穿透")
            .Replace("属性抵抗", "抗").Replace("属性攻击", "攻").Replace("属性值", "");
        return name + "：" + value;
    }
}
