using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>把编辑器中的首领掉落清单映射回正式 Luban 源表，导出成功后一起发布。</summary>
internal sealed class ROBossDropStore
{
    /// <summary>项目根路径，决定源表、Luban 导出入口和发布目录。</summary>
    private readonly string root;
    /// <summary>角色进度键与首领难度的正式绑定。</summary>
    private ROBossDropSheet awards;
    /// <summary>可由多只首领共用的物品与数量组合。</summary>
    private ROBossDropSheet packs;
    /// <summary>两张正式策划表的文件夹。</summary>
    private string Sources { get { return Path.Combine(root, "Luaban/MiniTemplate/Datas"); } }

    /// <summary>打开项目内的两张奖励源表。</summary>
    /// <param name="repository">项目根目录。</param>
    internal ROBossDropStore(string repository)
    {
        root = repository;
        Reload();
    }

    /// <summary>读取最新源表供切换首领时展示。</summary>
    private void Reload()
    {
        awards = new ROBossDropSheet(Path.Combine(Sources, "#AwardTable.xlsx"));
        packs = new ROBossDropSheet(Path.Combine(Sources, "#AwardPackTable.xlsx"));
    }

    /// <summary>按三个业务字段定位绑定，原行 B 列的角色进度键不随改奖变化。</summary>
    /// <param name="sheet">首领绑定表。</param><param name="edit">要定位的副本、难度和怪物模板。</param>
    private static XElement Binding(ROBossDropSheet sheet, ROBossDropEdit edit)
    {
        return sheet.DataRows.SingleOrDefault(row => sheet.Number(row, "C") == edit.dungeon &&
            sheet.Number(row, "D") == edit.degree && sheet.Number(row, "E") == edit.boss);
    }

    /// <summary>将单包或多包行还原成界面的物品清单，未配置过的首领返回空草稿。</summary>
    /// <param name="dungeon">副本配置号。</param><param name="degree">难度配置号。</param><param name="boss">首领模板号。</param><param name="name">首领正式名称。</param>
    internal ROBossDropEdit Read(int dungeon, int degree, int boss, string name)
    {
        // 保留完整选择身份，以便多个未保存的组合分别编辑。
        ROBossDropEdit edit = new ROBossDropEdit { dungeon = dungeon, degree = degree, boss = boss, bossName = name };
        XElement row = Binding(awards, edit);
        if (row == null) return edit;
        // 单个旧数字和竖线分隔的多个包用相同入口读取。
        string[] ids = awards.Text(row, "G").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        XElement[] contents = packs.DataRows;
        for (int index = 0; index < ids.Length; index++)
        {
            // 配置中每个包恰好对应一种物品及其数量。
            int id = int.Parse(ids[index], System.Globalization.CultureInfo.InvariantCulture);
            XElement pack = Array.Find(contents, p => packs.Number(p, "B") == id);
            if (pack == null) throw new InvalidDataException("找不到首领奖励包 " + id);
            edit.items.Add(new ROBossDropEdit.Item { id = packs.Number(pack, "C"), count = packs.Number(pack, "D") });
        }
        return edit;
    }

    /// <summary>在源表副本中更新选中的行，共用物品包只复用或新增，不就地改写。</summary>
    /// <param name="bindings">暂存绑定表。</param><param name="contents">暂存物品包表。</param><param name="edits">窗口内所有待保存的组合。</param><param name="validItems">正式物品与装备的编号集合。</param>
    private static void EditSheets(ROBossDropSheet bindings, ROBossDropSheet contents, List<ROBossDropEdit> edits, HashSet<int> validItems)
    {
        // 相同物品和件数可以复用同一包，其他首领原有引用继续有效。
        var combinations = new Dictionary<(int Id, int Count), int>();
        XElement[] packRows = contents.DataRows;
        int maxPack = 0;
        for (int index = 0; index < packRows.Length; index++)
        {
            // 兼容手工配置的稀疏主键，新增主键始终取表内最大值之后。
            XElement pack = packRows[index];
            int id = contents.Number(pack, "B");
            combinations[(contents.Number(pack, "C"), contents.Number(pack, "D"))] = id;
            maxPack = Math.Max(id, maxPack);
        }
        // 仅发生新增组合时才需要改写包表。
        int oldMaxPack = maxPack;
        for (int index = 0; index < edits.Count; index++)
        {
            // 每只首领和每档难度独立写入自己的绑定行。
            ROBossDropEdit edit = edits[index];
            XElement binding = Binding(bindings, edit);
            if (binding == null)
            {
                // 为新组合分配一次永久进度键，已有组合绝不换号。
                int id = checked(bindings.DataRows.Select(row => bindings.Number(row, "B")).DefaultIfEmpty().Max() + 1);
                binding = bindings.Append("G");
                bindings.Set(binding, "B", id.ToString());
                bindings.Set(binding, "C", edit.dungeon.ToString());
                bindings.Set(binding, "D", edit.degree.ToString());
                bindings.Set(binding, "E", edit.boss.ToString());
                bindings.Set(binding, "F", edit.bossName, false);
            }
            // 每种物品只配一行，空清单保存后仍保留首领进度键。
            var seen = new HashSet<int>();
            int[] ids = new int[edit.items.Count];
            for (int line = 0; line < edit.items.Count; line++)
            {
                // 保存前检验当前草稿中的真实物品及正数量。
                ROBossDropEdit.Item item = edit.items[line];
                if (!validItems.Contains(item.id) || item.count <= 0 || !seen.Add(item.id))
                    throw new InvalidDataException(edit.bossName + "（难度 " + edit.degree + "）第 " + (line + 1) + " 行需要有效且不重复的物品和正数量。");
                if (!combinations.TryGetValue((item.id, item.count), out ids[line]))
                {
                    ids[line] = checked(++maxPack);
                    combinations.Add((item.id, item.count), ids[line]);
                    XElement pack = contents.Append("D");
                    contents.Set(pack, "B", ids[line].ToString());
                    contents.Set(pack, "C", item.id.ToString());
                    contents.Set(pack, "D", item.count.ToString());
                }
            }
            bindings.Set(binding, "G", string.Join("|", ids), false);
        }
        bindings.Write();
        if (maxPack != oldMaxPack) contents.Write();
    }

    /// <summary>使用与命令行一致的 Luban 导出，校验失败时保留草稿和原发布文件。</summary>
    /// <param name="edits">所有脏组合，其他行从点击保存时的最新工作簿读取。</param><param name="validItems">当前项目正式物品和装备的配置编号。</param>
    internal void Save(List<ROBossDropEdit> edits, HashSet<int> validItems)
    {
        if (edits.Count == 0) return;
        // 各次导出独立暂存；失败时可从 generate.log 查到具体单元格错误。
        string stage = Path.Combine(root, "_temp/boss-drop-editor", Guid.NewGuid().ToString("N"));
        string data = Path.Combine(stage, "Datas");
        Directory.CreateDirectory(data);
        string published = Path.Combine(root, "LYF_Server/Network/LubanManage/Tb");
        // 按先包后引用的顺序发布两张源表和两份二进制。
        string[] targets = { Path.Combine(Sources, "#AwardPackTable.xlsx"), Path.Combine(Sources, "#AwardTable.xlsx"),
            Path.Combine(published, "tbawardpacktable.bytes"), Path.Combine(published, "tbawardtable.bytes") };
        string[] prepared = { Path.Combine(data, "#AwardPackTable.xlsx"), Path.Combine(data, "#AwardTable.xlsx"),
            Path.Combine(stage, "Data/tbawardpacktable.bytes"), Path.Combine(stage, "Data/tbawardtable.bytes") };
        // 记录开始导出时的源文件内容，防止覆盖期间从 Excel 或其他窗口保存的修改。
        byte[][] original = new byte[targets.Length][];
        for (int index = 0; index < targets.Length; index++)
        {
            if (File.Exists(targets[index])) original[index] = File.ReadAllBytes(targets[index]);
            if (index < 2) File.WriteAllBytes(prepared[index], original[index]);
        }
        EditSheets(new ROBossDropSheet(prepared[1]), new ROBossDropSheet(prepared[0]), edits, validItems);
        // 调用仓库内现有导出脚本，文件和目录路径均以独立参数边界引用。
        string script = Path.Combine(root, "Luaban/MiniTemplate/gen_dungeon_difficulties.ps1");
        ProcessStartInfo start = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + script + "\" -StagedBossAwards -WorkDirectory \"" + stage + "\"")
            { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = root };
        using (Process process = Process.Start(start))
        {
            process.WaitForExit();
            if (process.ExitCode != 0) throw new IOException("Luban 导出失败，详见 " + Path.Combine(stage, "generate.log"));
        }
        // 已发布文件保留备份；后续某个文件被占用时按逆序恢复本次已替换的文件。
        List<int> replaced = new List<int>();
        try
        {
            for (int index = 0; index < targets.Length; index++)
            {
                if (original[index] != null)
                {
                    if (!File.ReadAllBytes(targets[index]).SequenceEqual(original[index]))
                        throw new IOException("导出期间文件已改变，请重新保存：" + targets[index]);
                    if (original[index].SequenceEqual(File.ReadAllBytes(prepared[index]))) continue;
                    File.Replace(prepared[index], targets[index], Path.Combine(stage, "backup-" + index));
                }
                else File.Move(prepared[index], targets[index]);
                replaced.Add(index);
            }
        }
        catch (Exception)
        {
            for (int index = replaced.Count - 1; index >= 0; index--)
            {
                // 实际被替换或创建的目标下标。
                int file = replaced[index];
                if (original[file] != null) File.Replace(Path.Combine(stage, "backup-" + file), targets[file], null);
                else File.Move(targets[file], prepared[file]);
            }
            throw;
        }
        Reload();
    }
}
