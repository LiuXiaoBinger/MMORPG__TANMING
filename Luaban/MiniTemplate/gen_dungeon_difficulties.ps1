<#
.SYNOPSIS
隔离导出副本难度、逐怪难度和 Gene，仅同步四个难度类型与三份二进制。
.PARAMETER WorkDirectory
保存隔离输入、原始生成物和日志的临时目录；默认使用项目根目录 _temp/luban-dungeon-difficulties。
.EXAMPLE
powershell -ExecutionPolicy Bypass -File Luaban/MiniTemplate/gen_dungeon_difficulties.ps1
#>
param([string]$WorkDirectory)

# 源表复制等操作失败时立即终止，禁止使用隔离目录中的旧输入继续导出。
$ErrorActionPreference = 'Stop'

# 当前 Luban 工程与共享客户端/服务端的项目根目录。
$templateRoot = $PSScriptRoot
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $templateRoot '../..'))
if ([string]::IsNullOrEmpty($WorkDirectory)) {
    $WorkDirectory = Join-Path $repositoryRoot '_temp/luban-dungeon-difficulties'
}
# 隔离生成配置使用绝对路径，避免从不同工作目录执行时改变输出位置。
$WorkDirectory = [System.IO.Path]::GetFullPath($WorkDirectory)
$dataRoot = Join-Path $WorkDirectory 'Datas'
$codeRoot = Join-Path $WorkDirectory 'Code'
$binaryRoot = Join-Path $WorkDirectory 'Data'
New-Item -ItemType Directory -Force -Path $dataRoot, $codeRoot, $binaryRoot | Out-Null
# 此入口只加载三张相关工作簿，# 前缀由 Luban 自动识别，不重复注册 __tables__。
$workbooks = @('#DungeonDifficultyTable.xlsx', '#DungeonMonsterDifficultyTable.xlsx', '#Gene.xlsx')
for ($index = 0; $index -lt $workbooks.Length; $index++) {
    Copy-Item -LiteralPath (Join-Path $templateRoot ('Datas/' + $workbooks[$index])) -Destination (Join-Path $dataRoot $workbooks[$index])
}
# 新增难度类型沿项目既有副本表使用全局命名空间；共享 cfg.Tables 仍由业务接入维护。
$configuration = '{"groups":[{"names":["c"],"default":true},{"names":["s"],"default":true},{"names":["e"],"default":true}],"schemaFiles":[],"dataDir":"Datas","targets":[{"name":"all","manager":"Tables","groups":["c","s","e"],"topModule":""}]}'
# 生成配置及 C# 使用无 BOM UTF-8；脚本本身保留 BOM 以支持 Windows PowerShell 5。
$utf8 = New-Object System.Text.UTF8Encoding($false)
$configurationPath = Join-Path $WorkDirectory 'luban.conf'
[System.IO.File]::WriteAllText($configurationPath, $configuration, $utf8)
dotnet (Join-Path $templateRoot 'Luban/Luban.dll') -t all -c cs-bin -d bin --conf $configurationPath -x "outputCodeDir=$codeRoot" -x "outputDataDir=$binaryRoot" *> (Join-Path $WorkDirectory 'generate.log')
if ($LASTEXITCODE -ne 0) {
    throw "Luban 副本难度生成失败，详见 $WorkDirectory/generate.log"
}

# 仅适配新难度类型的中文说明、数组读取与循环写法，不改生成字段或二进制编码。
$tableNames = @('DungeonDifficultyTable', 'DungeonMonsterDifficultyTable')
$tableLabels = @('副本难度', '副本逐怪难度')
# 正式生成物位置；不发布隔离目录的 Gene.cs、TbGene.cs 或 Tables.cs。
$destinationRoot = Join-Path $repositoryRoot 'LYF_Server/Network/LubanManage'
for ($tableIndex = 0; $tableIndex -lt $tableNames.Length; $tableIndex++) {
    # 当前生成的类型名、中文职责及使用统一 LF 换行的原始代码。
    $tableName = $tableNames[$tableIndex]
    $label = $tableLabels[$tableIndex]
    $beanCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot ($tableName + '.cs'))).Replace("`r`n", "`n")
    $beanCode = $beanCode.Replace('public sealed partial class ' + $tableName, '/// <summary>' + $label + '的只读配置行，由 Luban 二进制加载。</summary>' + "`npublic sealed partial class " + $tableName)
    $beanCode = $beanCode.Replace('    public ' + $tableName + '(ByteBuf _buf)', '    /// <summary>按表字段顺序读取一条' + $label + '配置。</summary>' + "`n" + '    /// <param name="_buf">定位到当前配置行的二进制读取游标。</param>' + "`n    public " + $tableName + '(ByteBuf _buf)')
    $beanCode = $beanCode.Replace('    public static ' + $tableName + ' Deserialize' + $tableName + '(ByteBuf _buf)', '    /// <summary>由表容器读取当前配置行。</summary>' + "`n" + '    /// <param name="_buf">当前配置行的二进制游标。</param>' + "`n    public static " + $tableName + ' Deserialize' + $tableName + '(ByteBuf _buf)')
    $beanCode = $beanCode.Replace('    public const int __ID__', '    /// <summary>Luban 为该配置类型生成的稳定类型编号。</summary>' + "`n    public const int __ID__")
    $beanCode = $beanCode.Replace('    public override int GetTypeId()', '    /// <summary>返回配置序列化所需的类型编号。</summary>' + "`n    public override int GetTypeId()")
    $beanCode = $beanCode.Replace('    public  void ResolveRef(Tables tables)', '    /// <summary>保留生成器引用解析入口；本表以数值 ID 由业务查询关联。</summary>' + "`n" + '    /// <param name="tables">已加载的共享配置集合。</param>' + "`n    public void ResolveRef(cfg.Tables tables)")
    $beanCode = $beanCode.Replace('    public override string ToString()', '    /// <summary>输出此配置的字段值，供调试定位。</summary>' + "`n    public override string ToString()")

    # 两个配对数组的生成字段名、元素类型、读取函数和中文语义。
    $arrayFields = @('GeneIds', 'GeneValues')
    $arrayTypes = @('int', 'float')
    $arrayReaders = @('ReadInt', 'ReadFloat')
    $arrayLabels = @('基因ID', '基因覆盖值')
    for ($arrayIndex = 0; $arrayIndex -lt $arrayFields.Length; $arrayIndex++) {
        # 当前数组的局部计数名和 Luban 紧凑数组读取片段。
        $field = $arrayFields[$arrayIndex]
        $elementType = $arrayTypes[$arrayIndex]
        $reader = $arrayReaders[$arrayIndex]
        $arrayLabel = $arrayLabels[$arrayIndex]
        $countName = $field.Substring(0, 1).ToLowerInvariant() + $field.Substring(1) + 'Count'
        $pattern = '        \{int __n0 = _buf.ReadSize\(\); ' + $field + ' = new ' + $elementType + '\[__n0\];.*?\}\}'
        # 展开生成器的数组循环，保留相同读取顺序并补足局部变量说明。
        $replacement = @(
            '        // 当前行的' + $arrayLabel + '数量，顺序与配对数组一致。'
            '        int ' + $countName + ' = _buf.ReadSize();'
            '        ' + $field + ' = new ' + $elementType + '[' + $countName + '];'
            '        // 当前' + $arrayLabel + '的零基下标。'
            '        for (int index = 0; index < ' + $countName + '; index++)'
            '        {'
            '            ' + $field + '[index] = _buf.' + $reader + '();'
            '        }'
        ) -join "`n"
        $beanCode = [regex]::Replace($beanCode, $pattern, $replacement)
    }

    # 当前表容器只提供主键索引；副本/难度/实体组合索引由原业务入口维护。
    $tableCode = [System.IO.File]::ReadAllText((Join-Path $codeRoot ('Tb' + $tableName + '.cs'))).Replace("`r`n", "`n")
    $tableCode = $tableCode.Replace('public partial class Tb' + $tableName, '/// <summary>按配置 ID 建立索引并保留原表行序的' + $label + '容器。</summary>' + "`npublic partial class Tb" + $tableName)
    $tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.Dictionary', '    /// <summary>唯一配置 ID 到只读行对象的索引。</summary>' + "`n    private readonly System.Collections.Generic.Dictionary")
    $tableCode = $tableCode.Replace('    private readonly System.Collections.Generic.List', '    /// <summary>按工作簿原始行序保存的配置对象。</summary>' + "`n    private readonly System.Collections.Generic.List")
    $tableCode = $tableCode.Replace('    public Tb' + $tableName + '(ByteBuf _buf)', '    /// <summary>读取整张' + $label + '表并建立配置 ID 索引。</summary>' + "`n" + '    /// <param name="_buf">完整表数据的二进制读取游标。</param>' + "`n    public Tb" + $tableName + '(ByteBuf _buf)')
    $tableCode = $tableCode.Replace('        int n =', '        // 本表序列化记录总数。' + "`n        int n =")
    $tableCode = $tableCode.Replace('        for(int i =', '        // 剩余记录计数，每轮读取一条配置。' + "`n        for(int i =")
    $tableCode = $tableCode.Replace('            ' + $tableName + ' _v;', '            // 当前读取的配置行，由列表与字典共同引用。' + "`n            " + $tableName + ' _v;')
    $tableCode = $tableCode.Replace('    public System.Collections.Generic.Dictionary', '    /// <summary>按配置 ID 查询的集合，调用方不应修改。</summary>' + "`n    public System.Collections.Generic.Dictionary")
    $tableCode = $tableCode.Replace('    public System.Collections.Generic.List', '    /// <summary>按表顺序遍历的集合，调用方不应修改。</summary>' + "`n    public System.Collections.Generic.List")
    # 展开可空查询，不改变缺失记录返回 null 的生成接口语义。
    $lookupCode = @(
        '    /// <summary>按配置 ID 查询，缺失时返回 null。</summary>'
        '    /// <param name="key">配置表唯一 ID。</param>'
        '    public ' + $tableName + ' GetOrDefault(int key)'
        '    {'
        '        // 保存主键匹配到的只读配置实例。'
        '        if (_dataMap.TryGetValue(key, out ' + $tableName + ' value))'
        '        {'
        '            return value;'
        '        }'
        '        return null;'
        '    }'
    ) -join "`n"
    $tableCode = $tableCode.Replace('    public ' + $tableName + ' GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : default;', $lookupCode)
    $tableCode = $tableCode.Replace('    public ' + $tableName + ' Get(int key)', '    /// <summary>读取已存在的配置行，缺失由字典报告错误。</summary>' + "`n" + '    /// <param name="key">已存在的配置 ID。</param>' + "`n    public " + $tableName + ' Get(int key)')
    $tableCode = $tableCode.Replace('    public ' + $tableName + ' this[int key]', '    /// <summary>按唯一 ID 访问配置行。</summary>' + "`n" + '    /// <param name="key">已存在的配置 ID。</param>' + "`n    public " + $tableName + ' this[int key]')
    $tableCode = $tableCode.Replace('    public void ResolveRef(Tables tables)', '    /// <summary>依次执行各行的引用解析入口。</summary>' + "`n" + '    /// <param name="tables">已加载的共享配置集合。</param>' + "`n    public void ResolveRef(cfg.Tables tables)")
    $tableCode = $tableCode.Replace("        foreach(var _v in _dataList)`n        {`n            _v.ResolveRef(tables);`n        }", "        // 当前需要解析的配置行下标。`n        for (int index = 0; index < _dataList.Count; index++)`n        {`n            _dataList[index].ResolveRef(tables);`n        }")

    # 固定行末、CRLF 和文件末尾，保持既有生成物字节并确保重复导出一致。
    $beanCode = [regex]::Replace($beanCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).Trim() + "`n"
    $tableCode = [regex]::Replace($tableCode, '[ \t]+(?=\r?$)', '', [System.Text.RegularExpressions.RegexOptions]::Multiline).Trim() + "`n"
    [System.IO.File]::WriteAllText((Join-Path $destinationRoot ('Codes/' + $tableName + '.cs')), $beanCode.Replace("`n", "`r`n"), $utf8)
    [System.IO.File]::WriteAllText((Join-Path $destinationRoot ('Codes/Tb' + $tableName + '.cs')), $tableCode.Replace("`n", "`r`n"), $utf8)
}

# 仅同步本次三份原始 Luban 二进制；Gene 类型与共享 Tables、LubanMgr、项目文件不覆盖。
$binaryNames = @('tbdungeondifficultytable.bytes', 'tbdungeonmonsterdifficultytable.bytes', 'tbgene.bytes')
for ($binaryIndex = 0; $binaryIndex -lt $binaryNames.Length; $binaryIndex++) {
    Copy-Item -LiteralPath (Join-Path $binaryRoot $binaryNames[$binaryIndex]) -Destination (Join-Path $destinationRoot ('Tb/' + $binaryNames[$binaryIndex]))
}
Write-Output '已生成两张副本难度表的四个类型，以及两份难度表和一份 Gene 二进制；其他生成代码与业务接入未覆盖。'
