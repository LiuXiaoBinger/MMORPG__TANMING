<#
.SYNOPSIS
仅从 RO 副本主表与目标组表生成对应类型和二进制，保留其他表的生成物。
.PARAMETER WorkDirectory
存放本次隔离输入、Luban 原始输出和日志的临时目录。
#>
param([string]$WorkDirectory)

# Luban 工程目录、项目根目录及本次隔离输出目录。
$templateRoot = $PSScriptRoot
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $templateRoot '../..'))
if ([string]::IsNullOrEmpty($WorkDirectory)) {
    $WorkDirectory = Join-Path $repositoryRoot '_temp/luban-dungeons'
}
$dataRoot = Join-Path $WorkDirectory 'Datas'
$codeRoot = Join-Path $WorkDirectory 'Code'
$binaryRoot = Join-Path $WorkDirectory 'Data'
New-Item -ItemType Directory -Force -Path $dataRoot, $codeRoot, $binaryRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $templateRoot 'Datas/#DungeonsTable.xlsx') -Destination (Join-Path $dataRoot '#DungeonsTable.xlsx')
Copy-Item -LiteralPath (Join-Path $templateRoot 'Datas/#DungeonTargetGroupTable.xlsx') -Destination (Join-Path $dataRoot '#DungeonTargetGroupTable.xlsx')
# 新类型使用全局命名空间；cfg.Tables 保持现有统一加载入口。
$configuration = '{"groups":[{"names":["c"],"default":true},{"names":["s"],"default":true},{"names":["e"],"default":true}],"schemaFiles":[],"dataDir":"Datas","targets":[{"name":"all","manager":"Tables","groups":["c","s","e"],"topModule":""}]}'
# 配置和生成 C# 使用无 BOM UTF-8；脚本自身以 BOM 兼容 Windows PowerShell 5。
$utf8 = New-Object System.Text.UTF8Encoding($false)
$configurationPath = Join-Path $WorkDirectory 'luban.conf'
[System.IO.File]::WriteAllText($configurationPath, $configuration, $utf8)
dotnet (Join-Path $templateRoot 'Luban/Luban.dll') -t all -c cs-bin -d bin --conf $configurationPath -x "outputCodeDir=$codeRoot" -x "outputDataDir=$binaryRoot" *> (Join-Path $WorkDirectory 'generate.log')
if ($LASTEXITCODE -ne 0) {
    throw "Luban 副本主表生成失败，详见 $WorkDirectory/generate.log"
}

# 保留 Luban 字段、编码顺序及原始二进制，只展开数组读取并补充项目要求的中文说明。
# 两张本任务表复用相同的代码适配步骤，不读取或发布其他表的代码。
$tableNames = @('DungeonsTable', 'DungeonTargetGroupTable')
for ($tableIndex = 0; $tableIndex -lt $tableNames.Length; $tableIndex++) {
# 当前处理的生成类型名；目标组暂用主表名匹配同一份 Luban 模板，发布前恢复。
$tableName = $tableNames[$tableIndex]
$beanCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot ($tableName + '.cs'))).Replace($tableName, 'DungeonsTable')
$beanCode = $beanCode.Replace('public sealed partial class DungeonsTable', "/// <summary>RO 副本主表的只读配置行，以 DungeonsID 为稳定主键。</summary>`npublic sealed partial class DungeonsTable")
$beanCode = $beanCode.Replace('    public DungeonsTable(ByteBuf _buf)', "    /// <summary>按 Luban 字段顺序读取副本配置，保留原表集合顺序。</summary>`n    /// <param name=`"_buf`">当前副本记录的二进制读取游标。</param>`n    public DungeonsTable(ByteBuf _buf)")
# 此正则只匹配 Luban 当前模板生成的单行一维或二维数组读取代码。
$arrayPattern = '        \{int __n0 = _buf\.ReadSize\(\); (?<field>\w+) = new (?<type>\w+)\[__n0\](?<nested>\[\])?;[^\r\n]+\}'
$beanCode = [regex]::Replace($beanCode, $arrayPattern, {
    param($match)
    # 当前字段名、基础元素类型及对应的 Luban 标量读取函数。
    $field = $match.Groups['field'].Value
    $elementType = $match.Groups['type'].Value
    $readMethod = 'ReadInt'
    if ($elementType -eq 'float') { $readMethod = 'ReadFloat' }
    if ($elementType -eq 'string') { $readMethod = 'ReadString' }
    # 用独立作用域保留每个数组原有生命周期，避免不同字段的下标互相影响。
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.Add('        {')
    $lines.Add("            // $field 的原表条目数；空单元格编码为空数组。")
    $lines.Add('            int count = _buf.ReadSize();')
    if ($match.Groups['nested'].Success) {
        $lines.Add("            $field = new $elementType[count][];")
    } else {
        $lines.Add("            $field = new $elementType[count];")
    }
    $lines.Add('            // 按原表顺序读取当前条目的下标。')
    $lines.Add('            for (int index = 0; index < count; index++)')
    $lines.Add('            {')
    if ($match.Groups['nested'].Success) {
        $lines.Add('                // 当前组合条目的参数数，分量语义沿用字段说明中的顺序。')
        $lines.Add('                int componentCount = _buf.ReadSize();')
        $lines.Add("                $field[index] = new $elementType[componentCount];")
        $lines.Add('                // 当前条目内的参数下标，不改变原记录的分组边界。')
        $lines.Add('                for (int component = 0; component < componentCount; component++)')
        $lines.Add('                {')
        $lines.Add("                    $field[index][component] = _buf.$readMethod();")
        $lines.Add('                }')
    } else {
        $lines.Add("                $field[index] = _buf.$readMethod();")
    }
    $lines.Add('            }')
    $lines.Add('        }')
    return [string]::Join("`n", $lines)
})
$beanCode = $beanCode.Replace('    public static DungeonsTable DeserializeDungeonsTable(ByteBuf _buf)', "    /// <summary>供副本主表容器反序列化一条配置。</summary>`n    /// <param name=`"_buf`">指向当前记录起点的二进制游标。</param>`n    public static DungeonsTable DeserializeDungeonsTable(ByteBuf _buf)")
$beanCode = $beanCode.Replace('/// 副本ID', '/// RO 原始副本主键，组队进入和副本目标查询均使用此 ID。')
$beanCode = $beanCode.Replace('/// 副本类型', '/// RO 副本分类编码，保留原表整数，不在配置层转换。')
$beanCode = $beanCode.Replace('/// 副本目标' + "`r`n", '/// 完整目标阶段表达式；| 分隔阶段，= 分隔同阶段目标，保持原文。' + "`r`n")
$beanCode = $beanCode.Replace('/// 观战类型', '/// RO 观战模式配置 ID，0 表示未配置。')
$beanCode = $beanCode.Replace('/// 动态数值类型', '/// 动态数值参数组，按原表三个整数分量顺序保存。')
$beanCode = $beanCode.Replace('/// 动态数值功能ID', '/// RO 动态数值功能配置 ID，0 表示未配置。')
$beanCode = $beanCode.Replace('/// tab到敌方范围，默认15', '/// 原 CSV 的选敌距离文本；RO Config 未声明此列，按 EditorTableRead 的 string 保留。')
$beanCode = $beanCode.Replace('/// 自动取消选中范围，默认15', '/// 原 CSV 的取消选中距离文本；RO Config 未声明此列，按 EditorTableRead 的 string 保留。')
$beanCode = $beanCode.Replace('    public const int __ID__', "    /// <summary>Luban 为此配置生成的序列化类型标识。</summary>`n    public const int __ID__")
$beanCode = $beanCode.Replace('    public override int GetTypeId()', "    /// <summary>返回副本配置的 Luban 类型标识。</summary>`n    public override int GetTypeId()")
$beanCode = $beanCode.Replace('    public  void ResolveRef(Tables tables)', "    /// <summary>保留 Luban 引用解析入口，本表按原始 ID 由业务关联其他表。</summary>`n    /// <param name=`"tables`">已加载的共享配置集合，本行不声明自动外键引用。</param>`n    public void ResolveRef(cfg.Tables tables)")
$beanCode = $beanCode.Replace('    public override string ToString()', "    /// <summary>输出副本原始配置字段，供诊断定位具体配置行。</summary>`n    public override string ToString()")

# 容器沿用 Luban 的字典与行序，仅补充注释并转换禁止的三元及 foreach 语法。
$tableCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot ('Tb' + $tableName + '.cs'))).Replace($tableName, 'DungeonsTable')
$tableCode = $tableCode.Replace('public partial class TbDungeonsTable', "/// <summary>按 DungeonsID 建立索引并保留原表行序的副本主表。</summary>`npublic sealed partial class TbDungeonsTable")
$tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.Dictionary', "    /// <summary>原始副本 ID 到只读配置行的运行时索引。</summary>`n    private readonly System.Collections.Generic.Dictionary")
$tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.List', "    /// <summary>按二进制原始行序保存的副本配置列表。</summary>`n    private readonly System.Collections.Generic.List")
$tableCode = $tableCode.Replace('    public TbDungeonsTable(ByteBuf _buf)', "    /// <summary>读取整张副本主表并建立主键索引。</summary>`n    /// <param name=`"_buf`">完整副本主表二进制的读取游标。</param>`n    public TbDungeonsTable(ByteBuf _buf)")
$tableCode = $tableCode.Replace('        int n =', "        // 二进制中保存的副本记录总数。`n        int n =")
$tableCode = $tableCode.Replace('        for(int i', "        // 当前剩余待读取记录数，读取一行后递减。`n        for(int i")
$tableCode = $tableCode.Replace('            DungeonsTable _v;', "            // 列表和主键索引共同引用的当前副本配置实例。`n            DungeonsTable _v;")
$tableCode = $tableCode.Replace('    public System.Collections.Generic.Dictionary', "    /// <summary>按 DungeonsID 索引的集合，调用方不得修改。</summary>`n    public System.Collections.Generic.Dictionary")
$tableCode = $tableCode.Replace('    public System.Collections.Generic.List', "    /// <summary>按原表顺序排列的集合，调用方不得修改。</summary>`n    public System.Collections.Generic.List")
$tableCode = $tableCode.Replace('    public DungeonsTable GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : default;', @'
    /// <summary>按副本 ID 查询，原表未配置时返回 null。</summary>
    /// <param name="key">RO 原始 DungeonsID。</param>
    public DungeonsTable GetOrDefault(int key)
    {
        // 保存主键索引命中的只读配置实例。
        if (_dataMap.TryGetValue(key, out DungeonsTable value))
        {
            return value;
        }
        return null;
    }
'@)
$tableCode = $tableCode.Replace('    public DungeonsTable Get(int key)', "    /// <summary>读取已存在的副本配置，缺失时由字典报告错误。</summary>`n    /// <param name=`"key`">已配置的 RO 副本 ID。</param>`n    public DungeonsTable Get(int key)")
$tableCode = $tableCode.Replace('    public DungeonsTable this[int key]', "    /// <summary>按原始主键访问已配置的副本行。</summary>`n    /// <param name=`"key`">已配置的 RO 副本 ID。</param>`n    public DungeonsTable this[int key]")
$tableCode = $tableCode.Replace('    public void ResolveRef(Tables tables)', "    /// <summary>按原表顺序调用各副本行的引用解析入口。</summary>`n    /// <param name=`"tables`">共享 cfg.Tables 配置集合。</param>`n    public void ResolveRef(cfg.Tables tables)")
$tableCode = $tableCode.Replace('        foreach(var _v in _dataList)', "        // 当前正在解析引用的副本配置下标。`n        for (int index = 0; index < _dataList.Count; index++)")
$tableCode = $tableCode.Replace('            _v.ResolveRef(tables);', '            _dataList[index].ResolveRef(tables);')

# 统一清除生成代码行末空白，重复生成保持确定性。
if ($tableName -eq 'DungeonTargetGroupTable') {
    $beanCode = $beanCode.Replace('DungeonsTable', $tableName).Replace('DungeonsID', 'ID').Replace('副本主表', '副本目标组表').Replace('副本配置', '目标组配置')
    $beanCode = $beanCode.Replace('/// 目标组ID', '/// RO 稳定目标组 ID，由分支副本的 DungeonTarget 表达式引用。').Replace('/// 目标组目标', '/// 本组同时激活的全部目标 ID，全部完成后才能推进；按原表顺序关联 DungeonTargetTable.ID。').Replace('/// 备注', '/// 原表策划备注，保留来源语义，不参与目标结算。')
    $tableCode = $tableCode.Replace('DungeonsTable', $tableName).Replace('DungeonsID', 'ID').Replace('副本主表', '副本目标组表').Replace('副本配置', '目标组配置').Replace('副本 ID', '目标组 ID')
}
$beanCode = [regex]::Replace($beanCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).TrimEnd([char[]]"`r`n") + "`n"
$tableCode = [regex]::Replace($tableCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).TrimEnd([char[]]"`r`n") + "`n"
# 仅发布本表三个生成物，Tables.cs 等接入文件由项目代码单独维护。
$destinationRoot = Join-Path $repositoryRoot 'LYF_Server/Network/LubanManage'
[System.IO.File]::WriteAllText((Join-Path $destinationRoot ('Codes/' + $tableName + '.cs')), $beanCode, $utf8)
[System.IO.File]::WriteAllText((Join-Path $destinationRoot ('Codes/Tb' + $tableName + '.cs')), $tableCode, $utf8)
# Luban 使用小写容器名作为对应二进制文件名。
$binaryName = 'tb' + $tableName.ToLowerInvariant() + '.bytes'
Copy-Item -LiteralPath (Join-Path $binaryRoot $binaryName) -Destination (Join-Path $destinationRoot ('Tb/' + $binaryName))
}
Write-Output '已生成副本主表和目标组表的四个 C# 类型及两个 bytes。'
