using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>用编辑器的实际存储实现编辑隔离副本，核对源表、导出结果及保存失败回滚。</summary>
public static class DungeonBossDropEditorSmoke
{
    /// <summary>测试在首领处配置的三种真实物品。</summary>
    private static readonly HashSet<int> Items = new HashSet<int> { 503, 501, 2001 };

    /// <summary>运行失败时中止，避免把无效的二进制送给后续回环测试。</summary>
    /// <param name="ok">验证结果。</param><param name="name">报告中的测试名称。</param>
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    /// <summary>通过真实编辑和导出入口生成后续游戏服测试所需的多掉落配置。</summary>
    /// <param name="args">第一个参数为含两张工作簿及正式导出脚本的隔离项目根目录。</param>
    public static void Main(string[] args)
    {
        // 测试调用者传入隔离数据，不修改正式平衡表。
        string root = args[0];
        ROBossDropStore store = new ROBossDropStore(root);
        ROBossDropEdit hate = store.Read(4060, 1, 5501, "达纳托斯的憎恨");
        Check(hate.items.Count == 1 && hate.items[0].id == 503 && hate.items[0].count == 1, "原单物品绑定可直接编辑");
        hate.items[0].count = 4;
        hate.items.Add(new ROBossDropEdit.Item { id = 501, count = 2 });
        hate.items.Add(new ROBossDropEdit.Item { id = 2001, count = 3 });
        // 另一难度的绝望改成只记进度，测试多个草稿的一键保存。
        ROBossDropEdit empty = store.Read(4060, 2, 5502, "达纳托斯的绝望");
        empty.items.Clear();
        store.Save(new List<ROBossDropEdit> { hate, empty }, Items);
        // 新建存储对象，防止只检查到上次修改的内存数据。
        store = new ROBossDropStore(root);
        var saved = store.Read(4060, 1, 5501, hate.bossName).items;
        Check(saved.Select(item => item.id).SequenceEqual(new[] {503, 501, 2001}) && saved.Select(item => item.count).SequenceEqual(new[] {4, 2, 3}), "关闭重开后，三种物品顺序和数量仍一致");
        Check(store.Read(4060, 1, 5502, empty.bossName).items.Single().count == 1 && store.Read(4060, 2, 5501, hate.bossName).items.Single().count == 2, "共用旧奖励包的其他首领和难度保持各自配置");
        Check(store.Read(4060, 2, 5502, empty.bossName).items.Count == 0, "空清单可保存");
        // 核对实际生成的 Luban 数据以及已发放角色沿用的稳定键。
        string data = Path.Combine(root, "LYF_Server/Network/LubanManage/Tb");
        string bindingFile = Path.Combine(data, "tbawardtable.bytes");
        var awards = new TbAwardTable(new Luban.ByteBuf(File.ReadAllBytes(bindingFile)));
        var packs = new TbAwardPackTable(new Luban.ByteBuf(File.ReadAllBytes(Path.Combine(data, "tbawardpacktable.bytes"))));
        AwardTable binding = awards.Get(550101);
        Check(binding.PackIds.Length == 3 && packs.Get(binding.PackIds[1]).ItemCount == 2 && awards.Get(550202).PackIds.Length == 0, "Luban 正式导出保留原角色键并正确编码三项和空清单");
        // 修改后的内容会生成新的包行，用不可替换的线上表模拟发布中途失败。
        string src = Path.Combine(root, "Luaban/MiniTemplate/Datas");
        string[] paths = { Path.Combine(src, "#AwardTable.xlsx"), Path.Combine(src, "#AwardPackTable.xlsx"), bindingFile, Path.Combine(data, "tbawardpacktable.bytes") };
        byte[][] before = Array.ConvertAll(paths, File.ReadAllBytes);
        hate.items[0].count = 55;
        bool blocked = false;
        using (FileStream held = File.Open(bindingFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { store.Save(new List<ROBossDropEdit> { hate }, Items); }
            catch (IOException) { blocked = true; }
        }
        Check(blocked && paths.Select((path, index) => File.ReadAllBytes(path).SequenceEqual(before[index])).All(equal => equal), "导出文件被占用时恢复两张源表和两份发布数据");
        // 重复物品与非正件数均不能被写进正式表。
        hate.items[0].count = 0;
        bool invalid = false;
        try { store.Save(new List<ROBossDropEdit> { hate }, Items); }
        catch (InvalidDataException) { invalid = true; }
        Check(invalid && File.ReadAllBytes(paths[0]).SequenceEqual(before[0]), "无效数量保留原文件");
        hate.items[0].count = 4;
        hate.items.Add(new ROBossDropEdit.Item { id = 503 });
        invalid = false;
        try { store.Save(new List<ROBossDropEdit> { hate }, Items); }
        catch (InvalidDataException) { invalid = true; }
        Check(invalid && File.ReadAllBytes(paths[1]).SequenceEqual(before[1]), "重复物品不能消耗同一奖励行两次");
    }
}
