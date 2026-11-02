using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

/// <summary>
/// 状态互斥表，对应参考工程 config/Table/CSV/StateExclusionTable.csv 与 StateMutexTableManager。
/// 表数据原样取自 RO 正式 CSV，以嵌入资源随 GameServer 编译，不重新编号、不臆造取值。
/// </summary>
public sealed class StateExclusionTable
{

    #region 字段与常量
    /// <summary>状态列之前的固定列数：StateId、State_Name、Concatenate。</summary>
    private const int StateColumnOffset = 3;

    /// <summary>表数据里状态行的实际数量，读取时按表头列数动态确认。</summary>
    private const int DefaultStateCount = 38;

    /// <summary>状态互斥表的唯一实例。</summary>
    private static readonly StateExclusionTable instance_ = new StateExclusionTable();

    /// <summary>互斥矩阵，行是源状态、列是目标状态；未配置格保持 -1。</summary>
    private readonly int[,] values_;

    /// <summary>状态下标到 RO 原始状态名（StateId 列）的映射，仅用于日志与自检。</summary>
    private readonly string[] names_;

    /// <summary>实际读入的状态行数量。</summary>
    private int state_count_;

    /// <summary>是否已完成装载。</summary>
    private bool loaded_;

    #region 构造与属性
    #endregion

    /// <summary>状态互斥表的唯一实例。</summary>
    public static StateExclusionTable Instance
    {
        get { return instance_; }
    }

    /// <summary>是否已完成装载。</summary>
    public bool IsLoaded
    {
        get { return loaded_; }
    }

    /// <summary>实际读入的状态行数量。</summary>
    public int StateCount
    {
        get { return state_count_; }
    }

    /// <summary>构造空表；未装载前所有取值都按未配置处理。</summary>
    private StateExclusionTable()
    {
        // max_count 取 StateTransitionType 的哨兵值 kStateMaxCount，等价 RO 的 EStateTransitionType 总数，
        // 是矩阵的行列宽（当前 36），决定 values_ 与 names_ 的分配长度，构造后不再变化。
        int max_count = (int)StateTransitionType.kStateMaxCount;
        values_ = new int[max_count, max_count];
        names_ = new string[max_count];
        // from 是矩阵行下标（源状态），范围零到 kStateMaxCount 减一。
        for (int from = 0; from < max_count; from++)
        {
            names_[from] = from.ToString(CultureInfo.InvariantCulture);
            // to 是矩阵列下标（目标状态），范围零到 kStateMaxCount 减一。
            for (int to = 0; to < max_count; to++)
            {
                values_[from, to] = (int)StateValueType.kMutexStateNone;
            }

    #region 配置读取与解析
    #endregion
        }
    }

    /// <summary>
    /// 装载互斥表：优先读取嵌入资源，资源缺失时退回运行目录下的 Config/StateExclusionTable.csv。
    /// 重复调用直接返回上一次结果，不重复解析。
    /// </summary>
    /// <returns>装载成功返回 true。</returns>
    public bool Load()
    {
        if (loaded_)
        {
            return true;
        }
        // text 是 CSV 全文（含表头与取值说明两行），来源优先嵌入资源、其次运行目录副本；
        // 为空表示两处都读不到，属于部署缺失而非数据错误。
        string text = ReadResourceText();
        if (text == null)
        {
            LogMsg.Info("状态互斥表读取失败：嵌入资源与 Config/StateExclusionTable.csv 均不可用", LogMsgType.Error);
            return false;
        }
        if (!Parse(text))
        {
            return false;
        }
        loaded_ = true;
        return true;
    }

    /// <summary>读取嵌入资源文本；资源不存在时退回运行目录下的同名文件。</summary>
    /// <returns>CSV 全文；两处都取不到时返回空。</returns>
    private static string ReadResourceText()
    {
        // assembly 是本体所在的 GameServer 程序集：互斥表以嵌入资源随它一起编译，不靠外部路径。
        Assembly assembly = typeof(StateExclusionTable).Assembly;
        // resource_names 是该程序集内全部嵌入资源名，长度即候选数量，可能一个都不匹配。
        string[] resource_names = assembly.GetManifestResourceNames();
        // index 是资源名数组的下标，范围零到 resource_names.Length 减一，用来逐个比对后缀。
        for (int index = 0; index < resource_names.Length; index++)
        {
            if (!resource_names[index].EndsWith("StateExclusionTable.csv", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            // stream 是命中的资源流；取不到说明资源登记异常，跳过继续找下一个同名后缀。
            using (Stream stream = assembly.GetManifestResourceStream(resource_names[index]))
            {
                if (stream == null)
                {
                    continue;
                }
                // reader 按资源流默认编码读取 CSV 全文，读完即随 using 释放。
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        // 嵌入资源缺失时的退路：测试进程直接读运行目录下的副本，行为与嵌入资源一致。
        // path 是运行目录下 Config/StateExclusionTable.csv 的绝对路径。
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            Path.Combine("Config", "StateExclusionTable.csv"));
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return null;
    }

    /// <summary>
    /// 解析 CSV：第一行是表头，第二行是取值说明，第三行起是状态行。
    /// 行序即状态下标，与 RO 的 StateExclusionTable::GetRow(index) 语义一致。
    /// </summary>
    /// <param name="text">CSV 全文。</param>
    /// <returns>解析成功返回 true。</returns>
    private bool Parse(string text)
    {
        // lines 是统一换行符后的 CSV 行数组，长度等于文件物理行数（含末尾空行）；
        // 这里只归一 CRLF/CR，不改动任何单元格内容，供下面的 while 逐行推进。
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        // 当前读取到的行下标，范围为零到行数减一。
        int line_index = 0;
        // 跳过的前两行：表头与取值说明行，都不是状态数据。
        int skipped = 0;
        // column_count 是表头去掉固定三列后的状态列数，同时决定状态行要读多少格。
        int column_count = 0;
        while (line_index < lines.Length)
        {
            // line 是当前待判定的整行原文，空行整行跳过、不进状态行计数。
            string line = lines[line_index];
            line_index++;
            if (line.Trim().Length == 0)
            {
                continue;
            }
            if (skipped == 0)
            {
                // 表头：去掉固定三列后剩下的列数就是状态数量。
                column_count = line.Split(',').Length - StateColumnOffset;
                skipped++;
                continue;
            }
            if (skipped == 1)
            {
                // 取值说明行，跳过。
                skipped++;
                continue;
            }
            if (column_count <= 0)
            {
                break;
            }
            if (state_count_ >= (int)StateTransitionType.kStateMaxCount)
            {
                break;
            }
            ParseRow(line, column_count, state_count_);
            state_count_++;
        }

        if (state_count_ != column_count && column_count > 0)
        {
            LogMsg.Info("状态互斥表行列数不一致：状态行 " + state_count_ + "，状态列 " + column_count,
                LogMsgType.Warn);
        }
        if (state_count_ == 0)
        {
            LogMsg.Info("状态互斥表没有任何状态行", LogMsgType.Error);
            return false;
        }
        // 自检：吟唱必须是第 35 行，否则说明表列序与 StateTransitionType 已经错位。
        if (state_count_ > (int)StateTransitionType.kStateTransitionRequestSinging &&
            names_[(int)StateTransitionType.kStateTransitionRequestSinging] != "State_AJ_StateSinging")
        {
            LogMsg.Info("状态互斥表列序与 StateTransitionType 不一致：第 35 行应为 State_AJ_StateSinging，实际 " +
                names_[(int)StateTransitionType.kStateTransitionRequestSinging], LogMsgType.Error);
            return false;
        }
        return true;
    }

    /// <summary>解析一行状态数据，写入名称与整行互斥取值。</summary>
    /// <param name="line">CSV 行原文。</param>
    /// <param name="column_count">表头确认的状态列数量。</param>
    /// <param name="state_index">该行对应的状态下标。</param>
    private void ParseRow(string line, int column_count, int state_index)
    {
        // fields 是按逗号切开的整行单元格，含前三列固定字段与后面的状态列。
        string[] fields = line.Split(',');
        names_[state_index] = fields[0].Trim();
        // column 是本行内的状态列下标，范围零到 column_count 减一，对应目标状态下标。
        for (int column = 0; column < column_count; column++)
        {
            // field_index 是该状态列在 fields 里的实际位置，要加上固定三列的偏移。
            int field_index = StateColumnOffset + column;
            // value 是解析出的互斥取值；单元格缺失或不是整数时保持“未配置”。
            int value = (int)StateValueType.kMutexStateNone;
            if (field_index < fields.Length)
            {
                // cell 是去空白后的单元格原文，空串与非法文本都按未配置处理。
                string cell = fields[field_index].Trim();
                // parsed 是单元格转成的整数，只在转换成功时写回 value。
                int parsed;
                if (int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    value = parsed;
                }

    #region 业务逻辑：状态查询
    #endregion
            }
            values_[state_index, column] = value;
        }
    }

    /// <summary>查询从 from 状态切换到 to 状态的互斥取值。</summary>
    /// <param name="from">源状态。</param>
    /// <param name="to">目标状态。</param>
    /// <returns>互斥表单元格取值；越界或未装载时按未配置处理。</returns>
    public StateValueType GetStateValue(StateTransitionType from, StateTransitionType to)
    {
        if (!IsInRange(from) || !IsInRange(to))
        {
            return StateValueType.kMutexStateNone;
        }
        return (StateValueType)values_[(int)from, (int)to];
    }

    /// <summary>取状态下标对应的 RO 原始状态名，供日志与自检使用。</summary>
    /// <param name="type">状态标识。</param>
    /// <returns>StateId 列原文；越界时返回下标字符串。</returns>
    public string GetStateName(StateTransitionType type)
    {
        if (!IsInRange(type))
        {
            return ((int)type).ToString(CultureInfo.InvariantCulture);
        }
        return names_[(int)type];
    }
    #endregion
    /// <summary>判断状态下标是否落在互斥矩阵范围内。</summary>
    /// <param name="type">状态标识。</param>
    private static bool IsInRange(StateTransitionType type)
    {
        return (int)type >= 0 && (int)type < (int)StateTransitionType.kStateMaxCount;
    }
}
