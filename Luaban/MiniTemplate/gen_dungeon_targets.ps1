<#
.SYNOPSIS
仅从副本目标工作簿生成并更新该表的两个类型和二进制，不覆盖其它 Luban 表。
.PARAMETER WorkDirectory
隔离配置、原始生成物与日志的临时目录；默认位于项目根目录的 _temp 下。
#>
param([string]$WorkDirectory)

# 本脚本所在的 Luban 工程目录。
$templateRoot = $PSScriptRoot
# 共享客户端和服务端的项目根目录。
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $templateRoot '../..'))
if ([string]::IsNullOrEmpty($WorkDirectory)) {
    $WorkDirectory = Join-Path $repositoryRoot '_temp/luban-dungeon-targets'
}
# 本次只包含一张表的输入、代码和数据输出目录。
$dataRoot = Join-Path $WorkDirectory 'Datas'
$codeRoot = Join-Path $WorkDirectory 'Code'
$binaryRoot = Join-Path $WorkDirectory 'Data'
New-Item -ItemType Directory -Force -Path $dataRoot, $codeRoot, $binaryRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $templateRoot 'Datas/#DungeonTargetTable.xlsx') -Destination (Join-Path $dataRoot '#DungeonTargetTable.xlsx')
# 空 topModule 让新增类型位于全局命名空间；原有 cfg.Tables 仅作为引用解析参数。
$configuration = @'
{"groups":[{"names":["c"],"default":true},{"names":["s"],"default":true},{"names":["e"],"default":true}],"schemaFiles":[],"dataDir":"Datas","targets":[{"name":"all","manager":"Tables","groups":["c","s","e"],"topModule":""}]}
'@
# UTF-8 无 BOM 用于生成器配置与适配后的 C# 源文件。
$utf8 = New-Object System.Text.UTF8Encoding($false)
# 隔离配置的实际路径，所有相对数据引用均相对该配置解析。
$configurationPath = Join-Path $WorkDirectory 'luban.conf'
[System.IO.File]::WriteAllText($configurationPath, $configuration, $utf8)
dotnet (Join-Path $templateRoot 'Luban/Luban.dll') -t all -c cs-bin -d bin --conf $configurationPath -x "outputCodeDir=$codeRoot" -x "outputDataDir=$binaryRoot" *> (Join-Path $WorkDirectory 'generate.log')
if ($LASTEXITCODE -ne 0) {
    throw "Luban 副本目标表生成失败，详见 $WorkDirectory/generate.log"
}

# 以下仅将 Luban 原始 C# 输出适配为本项目注释、for 循环及 cfg.Tables 签名。
# 二进制完全使用 Luban 输出，字段顺序和数值编码不在脚本中重写。
$beanCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot 'DungeonTargetTable.cs'))
$beanCode = $beanCode.Replace('public sealed partial class DungeonTargetTable', '/// <summary>RO 副本目标的一条只读配置，ID 与原表保持一致。</summary>' + "`n" + 'public sealed partial class DungeonTargetTable')
$beanCode = $beanCode.Replace('    public DungeonTargetTable(ByteBuf _buf)', '    /// <summary>按 Luban 字段顺序读取一条副本目标配置。</summary>' + "`n" + '    /// <param name="_buf">当前表二进制读取游标。</param>' + "`n" + '    public DungeonTargetTable(ByteBuf _buf)')
$beanCode = $beanCode.Replace('        {int __n0 = _buf.ReadSize(); Target = new double[__n0];for(var __index0 = 0 ; __index0 < __n0 ; __index0++) { double __e0;__e0 = _buf.ReadDouble(); Target[__index0] = __e0;}}', @'
        // 目标参数数量由 Luban 数组编码指定，现有原表使用两个或三个分量。
        int targetCount = _buf.ReadSize();
        Target = new double[targetCount];
        // 当前读取的目标参数下标，依次为类型和该类型的业务参数。
        for (int index = 0; index < targetCount; index++)
        {
            Target[index] = _buf.ReadDouble();
        }
'@)
$beanCode = $beanCode.Replace('        {int __n0 = _buf.ReadSize(); GuidePos = new float[__n0];for(var __index0 = 0 ; __index0 < __n0 ; __index0++) { float __e0;__e0 = _buf.ReadFloat(); GuidePos[__index0] = __e0;}}', @'
        // 引导坐标为空数组或世界坐标的 x、y、z 三个米数分量。
        int positionCount = _buf.ReadSize();
        GuidePos = new float[positionCount];
        // 当前读取的坐标分量下标。
        for (int index = 0; index < positionCount; index++)
        {
            GuidePos[index] = _buf.ReadFloat();
        }
'@)
$beanCode = $beanCode.Replace('    public static DungeonTargetTable DeserializeDungeonTargetTable(ByteBuf _buf)', '    /// <summary>供生成表容器创建一条配置记录。</summary>' + "`n" + '    /// <param name="_buf">指向本条记录起点的二进制游标。</param>' + "`n" + '    public static DungeonTargetTable DeserializeDungeonTargetTable(ByteBuf _buf)')
$beanCode = $beanCode.Replace('/// 目标ID', '/// 原表稳定目标 ID；副本有序目标列表和完成事件均引用此值。')
$beanCode = $beanCode.Replace('/// 副本目标（1，对话；2，打怪，击杀特定怪物；3：采集；4，获得特定物品；5，进入区域；6：使用道具；7：特殊事件', '/// 原始目标参数；首项为类型，类型 7 的第三项为完成次数，第二项保留原值。')
$beanCode = $beanCode.Replace('/// 引导坐标（不配置不会在小地图显示引导位置，配置则会显示引导位置）', '/// 可选世界引导坐标，按 x、y、z 米数排列；空数组表示不配置导航点。')
$beanCode = $beanCode.Replace('/// 目标描述', '/// 展示给玩家的原表目标描述，保留原文。')
$beanCode = $beanCode.Replace('/// 备注', '/// 原表策划备注，保留来源信息，不参与目标结算。')
$beanCode = $beanCode.Replace('    public const int __ID__', '    /// <summary>Luban 生成的类型标识，用于序列化类型识别。</summary>' + "`n" + '    public const int __ID__')
$beanCode = $beanCode.Replace('    public override int GetTypeId()', '    /// <summary>返回本配置类型的 Luban 标识。</summary>' + "`n" + '    public override int GetTypeId()')
$beanCode = $beanCode.Replace('    public  void ResolveRef(Tables tables)', '    /// <summary>保留 Luban 引用解析入口；本表不声明跨表引用。</summary>' + "`n" + '    /// <param name="tables">共享配置集合，本表不需要读取其中其它表。</param>' + "`n" + '    public void ResolveRef(cfg.Tables tables)')
$beanCode = $beanCode.Replace('    public override string ToString()', '    /// <summary>输出当前行的原始配置值，供配置诊断使用。</summary>' + "`n" + '    public override string ToString()')

# 容器保留 Luban 的索引与顺序列表，仅替换被项目规则禁止的语法。
$tableCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot 'TbDungeonTargetTable.cs'))
$tableCode = $tableCode.Replace('public partial class TbDungeonTargetTable', '/// <summary>按稳定目标 ID 索引、并保留原表行序的副本目标容器。</summary>' + "`n" + 'public sealed partial class TbDungeonTargetTable')
$tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.Dictionary', '    /// <summary>目标 ID 到只读配置行的索引，加载后供业务查询。</summary>' + "`n" + '    private readonly System.Collections.Generic.Dictionary')
$tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.List', '    /// <summary>按 Luban 二进制行序保存的配置记录。</summary>' + "`n" + '    private readonly System.Collections.Generic.List')
$tableCode = $tableCode.Replace('    public TbDungeonTargetTable(ByteBuf _buf)', '    /// <summary>读取整张目标表并建立 ID 索引。</summary>' + "`n" + '    /// <param name="_buf">完整目标表二进制的读取游标。</param>' + "`n" + '    public TbDungeonTargetTable(ByteBuf _buf)')
$tableCode = $tableCode.Replace('        int n =', '        // 当前二进制中需要读取的目标记录总数。' + "`n" + '        int n =')
$tableCode = $tableCode.Replace('        for(int i', '        // 剩余待读取记录数，每次读取一条后递减。' + "`n" + '        for(int i')
$tableCode = $tableCode.Replace('            DungeonTargetTable _v;', '            // 当前反序列化的原表记录，列表和索引持有相同实例。' + "`n" + '            DungeonTargetTable _v;')
$tableCode = $tableCode.Replace('    public System.Collections.Generic.Dictionary', '    /// <summary>按原目标 ID 查询的集合，调用方不得修改。</summary>' + "`n" + '    public System.Collections.Generic.Dictionary')
$tableCode = $tableCode.Replace('    public System.Collections.Generic.List', '    /// <summary>保留原表行序的集合，调用方不得修改。</summary>' + "`n" + '    public System.Collections.Generic.List')
$tableCode = $tableCode.Replace('    public DungeonTargetTable GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : default;', @'
    /// <summary>按目标 ID 查询；未配置时返回 null。</summary>
    /// <param name="key">RO 原表稳定目标 ID。</param>
    public DungeonTargetTable GetOrDefault(int key)
    {
        // 保存索引命中的目标配置；未命中时返回空引用。
        if (_dataMap.TryGetValue(key, out DungeonTargetTable value))
        {
            return value;
        }
        return null;
    }
'@)
$tableCode = $tableCode.Replace('    public DungeonTargetTable Get(int key)', '    /// <summary>按必定存在的目标 ID 读取配置。</summary>' + "`n" + '    /// <param name="key">已存在于原表的稳定目标 ID。</param>' + "`n" + '    public DungeonTargetTable Get(int key)')
$tableCode = $tableCode.Replace('    public DungeonTargetTable this[int key]', '    /// <summary>使用稳定目标 ID 访问对应配置记录。</summary>' + "`n" + '    /// <param name="key">已存在于原表的稳定目标 ID。</param>' + "`n" + '    public DungeonTargetTable this[int key]')
$tableCode = $tableCode.Replace('    public void ResolveRef(Tables tables)', '    /// <summary>按原表顺序执行生成行的引用解析入口。</summary>' + "`n" + '    /// <param name="tables">运行时统一加载的 cfg.Tables 集合。</param>' + "`n" + '    public void ResolveRef(cfg.Tables tables)')
$tableCode = $tableCode.Replace('        foreach(var _v in _dataList)', '        // 当前参与引用解析的配置行下标。' + "`n" + '        for (int index = 0; index < _dataList.Count; index++)')
$tableCode = $tableCode.Replace('            _v.ResolveRef(tables);', '            _dataList[index].ResolveRef(tables);')

# 移除生成行的行尾空白，并让两个 C# 文件仅以一个换行结束。
$beanCode = [regex]::Replace($beanCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).TrimEnd([char[]]"`r`n") + "`n"
$tableCode = [regex]::Replace($tableCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).TrimEnd([char[]]"`r`n") + "`n"

# 只写这张表的两个源文件；共享 Tables.cs 的接入由项目代码维护。
$destinationRoot = Join-Path $repositoryRoot 'LYF_Server/Network/LubanManage'
[System.IO.File]::WriteAllText((Join-Path $destinationRoot 'Codes/DungeonTargetTable.cs'), $beanCode, $utf8)
[System.IO.File]::WriteAllText((Join-Path $destinationRoot 'Codes/TbDungeonTargetTable.cs'), $tableCode, $utf8)
Copy-Item -LiteralPath (Join-Path $binaryRoot 'tbdungeontargettable.bytes') -Destination (Join-Path $destinationRoot 'Tb/tbdungeontargettable.bytes')
Write-Output '已生成副本目标表：DungeonTargetTable.cs、TbDungeonTargetTable.cs、tbdungeontargettable.bytes。'
