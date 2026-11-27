using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

/// <summary>只读写两张 Luban 奖励工作簿的首张数据页，保留其余页、样式及共享字符串。</summary>
internal sealed class ROBossDropSheet
{
    /// <summary>标准工作表及共享文本所用的 Open XML 命名空间。</summary>
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    /// <summary>当前源工作簿或暂存副本的完整路径。</summary>
    private readonly string path;
    /// <summary>工作簿内第一张工作表，以 XML 保留没有编辑的行与列。</summary>
    private readonly XDocument page;
    /// <summary>Excel 手工保存时可能通过下标引用文本。</summary>
    private readonly string[] shared;
    /// <summary>本页的实际数据行容器。</summary>
    private XElement Rows { get { return page.Root.Element(Sheet + "sheetData"); } }
    /// <summary>仅枚举拥有数值主键的业务行，排除表头和空行。</summary>
    internal XElement[] DataRows { get { return Rows.Elements(Sheet + "row").Where(row => Number(row, "B") > 0).ToArray(); } }

    /// <summary>载入正式源表或其暂存副本，不占用 Excel 文件。</summary>
    /// <param name="file">奖励工作簿路径。</param>
    internal ROBossDropSheet(string file)
    {
        path = file;
        using (ZipArchive zip = ZipFile.OpenRead(file))
        {
            using (Stream stream = zip.GetEntry("xl/worksheets/sheet1.xml").Open()) page = XDocument.Load(stream);
            // 支持 Office 的共享文本以及 Luban 源表中的直接文本。
            ZipArchiveEntry strings = zip.GetEntry("xl/sharedStrings.xml");
            shared = Array.Empty<string>();
            if (strings != null)
                using (Stream stream = strings.Open())
                    shared = XDocument.Load(stream).Root.Elements(Sheet + "si").Select(si => string.Concat(si.Descendants(Sheet + "t").Select(text => text.Value))).ToArray();
        }
    }

    /// <summary>按单元格类型读取完整文本，同时支持数值、公式缓存和行内文本。</summary>
    /// <param name="row">工作表数据行。</param><param name="column">Excel 列字母。</param>
    internal string Text(XElement row, string column)
    {
        // 单元格地址与 XML 行号一致，稀疏行不依赖节点顺序。
        string address = column + (string)row.Attribute("r");
        XElement cell = row.Elements(Sheet + "c").FirstOrDefault(c => (string)c.Attribute("r") == address);
        if (cell == null) return string.Empty;
        // Excel 三种文本表示都转换为同一种编辑值。
        string type = (string)cell.Attribute("t");
        string value = (string)cell.Element(Sheet + "v") ?? string.Empty;
        if (type == "s") return shared[int.Parse(value, CultureInfo.InvariantCulture)];
        if (type == "inlineStr") return string.Concat(cell.Descendants(Sheet + "t").Select(t => t.Value));
        return value;
    }

    /// <summary>读取主键或数值列，表头和空单元格返回零。</summary>
    /// <param name="row">工作表行。</param><param name="column">列字母。</param>
    internal int Number(XElement row, string column)
    {
        int.TryParse(Text(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
        return value;
    }

    /// <summary>仅改指定单元格的值，保留该格已有样式。</summary>
    /// <param name="row">待写业务行。</param><param name="column">列字母。</param><param name="value">文本或整数的十进制表示。</param><param name="numeric">是否写为 Excel 数值。</param>
    internal void Set(XElement row, string column, string value, bool numeric = true)
    {
        // 已存在的稀疏单元格优先复用。
        string address = column + (string)row.Attribute("r");
        XElement cell = row.Elements(Sheet + "c").FirstOrDefault(c => (string)c.Attribute("r") == address);
        if (cell == null)
        {
            cell = new XElement(Sheet + "c", new XAttribute("r", address));
            row.Add(cell);
        }
        cell.RemoveNodes();
        if (numeric) cell.SetAttributeValue("t", null);
        else cell.SetAttributeValue("t", "str");
        cell.Add(new XElement(Sheet + "v", value));
    }

    /// <summary>追加一条具有新主键的业务行，同时扩展 Office 表格的有效区域。</summary>
    /// <param name="lastColumn">本表末列字母。</param>
    internal XElement Append(string lastColumn)
    {
        // 当前最大 XML 行号含三个表头。
        int next = Rows.Elements(Sheet + "row").Max(row => (int)row.Attribute("r")) + 1;
        XElement result = new XElement(Sheet + "row", new XAttribute("r", next));
        Rows.Add(result);
        page.Root.Element(Sheet + "dimension")?.SetAttributeValue("ref", "A1:" + lastColumn + next);
        return result;
    }

    /// <summary>仅替换暂存工作簿的数据页，其他压缩条目原样保留。</summary>
    internal void Write()
    {
        using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            zip.GetEntry("xl/worksheets/sheet1.xml").Delete();
            using (Stream stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open()) page.Save(stream, SaveOptions.DisableFormatting);
        }
    }
}
