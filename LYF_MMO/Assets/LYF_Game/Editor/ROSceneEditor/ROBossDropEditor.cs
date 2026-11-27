using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>RO 副本编辑器内的首领掉落页，支持多组选项保留草稿并一起保存。</summary>
[Serializable]
internal sealed class ROBossDropEditor
{
    /// <summary>当前选中的真实首领模板。</summary>
    [SerializeField] private int boss;
    /// <summary>当前选择的难度表主键，初始为新秀。</summary>
    [SerializeField] private int degree = 1;
    /// <summary>已修改的首领难度组合，窗口切页或脚本域重载后仍可继续编辑。</summary>
    [SerializeField] private List<ROBossDropEdit> edits = new List<ROBossDropEdit>();
    /// <summary>上一次加载波次名单的副本编号。</summary>
    private int shownDungeon;
    /// <summary>当前副本实际出现的首领，按首次出现的波次排序。</summary>
    private ROSceneCreatureEntity[] bosses = Array.Empty<ROSceneCreatureEntity>();
    /// <summary>正式难度表的选项和物品目录在首次打开时载入。</summary>
    private DungeonDifficultyTable[] degrees;
    /// <summary>可按名称或编号搜索的物品、装备名称。</summary>
    private SortedDictionary<int, string> itemNames;
    /// <summary>奖励源表在本窗口的读写入口。</summary>
    private ROBossDropStore store;
    /// <summary>当前行编辑的组合；修改后同一个对象也进入草稿列表。</summary>
    private ROBossDropEdit active;
    /// <summary>保存或数据装载结果反馈。</summary>
    private string status;
    /// <summary>错误信息使用错误样式，正常保存使用信息样式。</summary>
    private bool failed;
    /// <summary>掉落清单在较小窗口中的滚动位置。</summary>
    private Vector2 scroll;
    /// <summary>是否存在尚未写入工作簿的编辑。</summary>
    internal bool Dirty { get { return edits.Count > 0; } }

    /// <summary>提供 Unity 原生可搜索下拉框，按物品编号或名称查找普通物品及装备。</summary>
    private sealed class ItemPicker : AdvancedDropdown
    {
        /// <summary>窗口已加载的正式物品索引。</summary>
        private readonly SortedDictionary<int, string> names;
        /// <summary>向当前正在编辑的行返回所选物品配置编号。</summary>
        private readonly Action<int> selected;

        /// <summary>使用 Unity 内建的搜索和键盘选择。</summary>
        /// <param name="options">正式物品及装备名。</param><param name="onSelect">添加或替换当前物品的回调。</param>
        internal ItemPicker(SortedDictionary<int, string> options, Action<int> onSelect) : base(new AdvancedDropdownState())
        {
            names = options;
            selected = onSelect;
            minimumSize = new Vector2(320f, 360f);
        }

        /// <summary>编号与正式名称共同参与内建搜索。</summary>
        protected override AdvancedDropdownItem BuildRoot()
        {
            // 根项作为搜索框下方的菜单标题，不参与物品选择。
            AdvancedDropdownItem root = new AdvancedDropdownItem("选择掉落物品");
            // 排序索引一次转为数组，避免重复遍历全部物品。
            KeyValuePair<int, string>[] options = names.ToArray();
            for (int index = 0; index < options.Length; index++)
                root.AddChild(new AdvancedDropdownItem(options[index].Key + " · " + options[index].Value) { id = options[index].Key });
            return root;
        }

        /// <summary>提交用户选中的配置编号。</summary><param name="item">已选中的实际物品选项。</param>
        protected override void ItemSelected(AdvancedDropdownItem item) { selected(item.id); }
    }

    /// <summary>首次使用时读正式源表、难度、普通物品与装备；不依赖客户端进入游戏。</summary>
    private void Load()
    {
        if (store != null) return;
        // Unity 的 Assets 向上两级是本仓库根目录。
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        // 从共享配置读取与服务端一致的候选项目。
        string data = Path.Combine(root, "LYF_Server/Network/LubanManage/Tb");
        degrees = new TbDungeonDifficultyTable(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(data, "tbdungeondifficultytable.bytes")))).DataList.ToArray();
        // 游戏服以装备配置覆盖同 ID 的普通物品，编辑器沿用同样的显示顺序。
        var items = new cfg.TbItemInfo(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(data, "tbiteminfo.bytes")))).DataList;
        var equipment = new cfg.TbEquipInfo(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(data, "tbequipinfo.bytes")))).DataList;
        itemNames = new SortedDictionary<int, string>();
        for (int index = 0; index < items.Count; index++) itemNames[items[index].ItemTypeId] = items[index].Name;
        for (int index = 0; index < equipment.Count; index++) itemNames[equipment[index].ItemTypeId] = equipment[index].EquipName;
        store = new ROBossDropStore(root);
    }

    /// <summary>重新打开场景或从节点编辑切回时重读实际波次首领。</summary>
    internal void RefreshScene() { shownDungeon = 0; active = null; }

    /// <summary>取怪物目录首领标记与本副本实际战斗波次的交集。</summary>
    /// <param name="dungeon">选择的副本表行。</param><param name="currentWaves">同地图正在编辑的 XML，可含未保存的波次。</param>
    private void SelectDungeon(DungeonsTable dungeon, XDocument currentWaves)
    {
        shownDungeon = dungeon.DungeonsID;
        active = null;
        bosses = Array.Empty<ROSceneCreatureEntity>();
        // 切换副本时只从该副本的登记和波次获取首领。
        DungeonSceneResource resource = DungeonSceneResource.Load(dungeon.SceneID);
        if (resource == null) return;
        // 波次决定实际出场名单，生物资源提供类型和名字。
        ROSceneCreatureCatalog catalog = ROSceneCreatureCatalog.Load(resource.creatureResource);
        XDocument waves = currentWaves ?? XDocument.Parse(Resources.Load<TextAsset>(resource.flowResource).text);
        bosses = waves.Descendants("MSceneWaveData").Select(wave => (int?)wave.Element("monster_info")?.Element("entity_id") ?? 0)
            .Distinct().Select(id => catalog.FindEntity(id)).Where(entity => entity != null && entity.isBoss && !entity.isNpc).ToArray();
        if (bosses.Length > 0 && !Array.Exists(bosses, value => value.id == boss)) boss = bosses[0].id;
    }

    /// <summary>渲染选择器和掉落行，多个首领可连续编辑后一次导出。</summary>
    /// <param name="dungeon">工具栏当前选中的副本。</param><param name="currentWaves">打开的 XML 与所选地图一致时传入。</param>
    internal void Draw(DungeonsTable dungeon, XDocument currentWaves)
    {
        try
        {
            Load();
            if (dungeon != null && dungeon.DungeonsID != shownDungeon) SelectDungeon(dungeon, currentWaves);
            if (dungeon == null || bosses.Length == 0)
                EditorGUILayout.HelpBox("该副本的正式波次中没有已登记的战斗首领。", MessageType.Info);
            else DrawSelection(dungeon.DungeonsID);
        }
        catch (Exception error) when (!(error is ExitGUIException))
        {
            failed = true;
            status = error.Message;
        }
        using (new EditorGUI.DisabledScope(!Dirty))
            if (GUILayout.Button("保存并导出掉落", GUILayout.Width(160))) Save();
        if (!string.IsNullOrEmpty(status))
        {
            MessageType kind = MessageType.Info;
            if (failed) kind = MessageType.Error;
            EditorGUILayout.HelpBox(status, kind);
        }
    }

    /// <summary>根据真实配置显示首领、难度和可搜索的掉落条目。</summary>
    /// <param name="dungeon">本次编辑所属副本的配置编号。</param>
    private void DrawSelection(int dungeon)
    {
        boss = EditorGUILayout.IntPopup("Boss", boss, Array.ConvertAll(bosses, b => b.displayName + " · " + b.id), Array.ConvertAll(bosses, b => b.id));
        degree = EditorGUILayout.IntPopup("难度", degree, Array.ConvertAll(degrees, d => d.Name), Array.ConvertAll(degrees, d => d.Id));
        if (active == null || active.boss != boss || active.degree != degree)
        {
            // 优先恢复已编辑的组合，没有草稿再从正式源表取原值。
            active = edits.Find(edit => edit.dungeon == dungeon && edit.degree == degree && edit.boss == boss);
            if (active == null) active = store.Read(dungeon, degree, boss, Array.Find(bosses, b => b.id == boss).displayName);
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("掉落物品", "单次入包数量", EditorStyles.boldLabel);
        // 捕获当前草稿本身，搜索菜单回调不依赖后续切换的选择。
        ROBossDropEdit editRow = active;
        for (int index = 0; index < editRow.items.Count; index++)
        {
            // 每行对应本次击杀一起入包的一种物品。
            ROBossDropEdit.Item item = editRow.items[index];
            EditorGUILayout.BeginHorizontal();
            // 在按钮中同时显示物品配置号和策划名。
            itemNames.TryGetValue(item.id, out string name);
            Rect pickerRect = EditorGUILayout.GetControlRect(false);
            if (EditorGUI.DropdownButton(pickerRect, new GUIContent(item.id + " · " + name), FocusType.Keyboard))
                new ItemPicker(itemNames, id => { item.id = id; Changed(editRow); }).Show(pickerRect);
            // 数量在完成输入并保存时统一检查正值。
            int count = EditorGUILayout.IntField(item.count, GUILayout.Width(85));
            if (count != item.count) { item.count = count; Changed(editRow); }
            // 先结束本行布局，再从清单移除当前物品。
            bool remove = GUILayout.Button("移除", GUILayout.Width(48));
            EditorGUILayout.EndHorizontal();
            if (remove) { editRow.items.RemoveAt(index); Changed(editRow); break; }
        }
        // 使用同一位置绘制按钮并定位搜索窗口，鼠标和键盘打开时均紧贴按钮。
        Rect addRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(145));
        if (EditorGUI.DropdownButton(addRect, new GUIContent("添加掉落物品"), FocusType.Keyboard))
            new ItemPicker(itemNames, id => { editRow.items.Add(new ROBossDropEdit.Item { id = id }); Changed(editRow); }).Show(addRect);
        if (editRow.items.Count == 0) EditorGUILayout.HelpBox("空清单只记录击杀进度。", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    /// <summary>标记该首领难度组合需要保存。</summary><param name="edit">刚刚编辑的草稿。</param>
    private void Changed(ROBossDropEdit edit)
    {
        if (!edits.Contains(edit)) edits.Add(edit);
        status = null;
        GUI.changed = true;
    }

    /// <summary>保存所有草稿；失败时保留选择和修改供继续编辑。</summary>
    internal bool Save()
    {
        if (!Dirty) return true;
        try
        {
            Load();
            store.Save(edits, new HashSet<int>(itemNames.Keys));
            status = "已保存并导出 " + edits.Count + " 组首领掉落。";
            failed = false;
            edits.Clear();
            active = null;
            return true;
        }
        catch (Exception error)
        {
            failed = true;
            status = error.Message;
            return false;
        }
    }

    /// <summary>在用户明确放弃窗口修改时释放所有待保存奖励草稿。</summary>
    internal void Discard() { edits.Clear(); active = null; status = null; }
}
