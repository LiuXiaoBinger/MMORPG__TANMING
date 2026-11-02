using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// RO <c>StateExclusionTable</c> 的客户端只读镜像：按状态下标提供互斥矩阵行。
/// 数据来源是 RO <c>config/Table/CSV/StateExclusionTable.csv</c> 的逐字节副本，
/// 位于本项目 <c>Data/StateExclusion/Resources/StateExclusionTable.csv</c>。
/// RO 按行下标消费该表，因此行下标直接等于 <see cref="MExlusionStates"/> 的枚举值：
/// 38 行固定对应 State_A 至 State_AQ，行内 <c>StateId</c> 文本只用于日志与提示，
/// 与原表保持一字不改（其中两行名称与枚举名拼写不同，属于 RO 原表现状）。
/// </summary>
public sealed class StateExclusionTable
{

    #region 字段与属性
    /// <summary>RO 命名风格的单例入口；表数据在客户端进程内只装载一次。</summary>
    public static StateExclusionTable singleton
    {
        get { return _singleton; }
    }

    /// <summary>进程内唯一表实例，冷启动阶段由 <see cref="StateExclusionManager"/> 装载数据。</summary>
    private static readonly StateExclusionTable _singleton = new StateExclusionTable();

    /// <summary>未装载数据时的空行集合，避免调用方对空引用做额外判断。</summary>
    private static readonly RowData[] EmptyRows = new RowData[0];

    /// <summary>按状态下标索引的矩阵行；装载失败时保持空数组。</summary>
    private RowData[] _rows = EmptyRows;

    /// <summary>矩阵行数，正常等于 <see cref="MExlusionStates.State_Last"/> 的数值。</summary>
    public int Size
    {
        get { return _rows.Length; }
    }

    /// <summary>是否已经成功装载整张表；为 false 时所有行查询都会返回空。</summary>
    public bool IsLoaded
    {
        get { return _rows.Length > 0; }
    }

    /// <summary>按状态下标取矩阵行，越界或未装载时返回空。</summary>
    /// <param name="index">状态下标，取自 <see cref="MExlusionStates"/> 的枚举值。</param>
    /// <returns>该状态作为“当前状态”时的整行互斥配置。</returns>
    public RowData this[int index]
    {
        get
        {
            if (index < 0 || index >= _rows.Length)
            {
                return null;
            }
            return _rows[index];
        }
    }
    #endregion

    #region 配置读取与解析
    /// <summary>
    /// RO 原表在前两行放的是表头与说明文字，真正数据从第 3 个物理行开始。
    /// 说明行里带引号包住的中文逗号分段，不参与解析，按固定行数跳过最稳妥。
    /// </summary>
    private const int HeaderLineCount = 2;

    /// <summary>
    /// 用 CSV 文本整体替换当前矩阵。表结构与 RO 原表完全一致：
    /// 第 1 行表头、第 2 行状态码说明，其后每行一个状态；
    /// 第 1 列 StateId、第 2 列中文名、第 3 列 Concatenate（竖线分隔的矩阵值）。
    /// 只消费 RO 自己标注为“实际使用的配置”的第 3 列，右侧 38 个逐状态列保留但不读。
    /// 行数或列数与枚举不匹配时整份拒绝，避免半张错位矩阵参与施法准入。
    /// </summary>
    /// <param name="csvText">来自 Resources 的整份 CSV 文本，允许 CRLF 或 LF 换行。</param>
    /// <returns>整张表通过行数、列数与数值校验时返回 true。</returns>
    public bool LoadFromCsv(string csvText)
    {
        if (string.IsNullOrEmpty(csvText))
        {
            Debug.LogError("状态互斥表内容为空，保留空表。");
            return false;
        }
        // 统一换行后再切行，兼容 Windows 导出的 CRLF 与仓库内的 LF。
        string[] lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        // 状态总数即矩阵行数，同时决定每行 Concatenate 的列数。
        int expectedRowCount = (int)MExlusionStates.State_Last;
        // 候选行集：按状态下标预置的行槽位，只有整表校验通过后才会替换正式矩阵。
        RowData[] parsedRows = new RowData[expectedRowCount];
        // 已解析的状态行数，同时充当下一行的状态下标。
        int parsedCount = 0;
        // 前 HeaderLineCount 行是表头与说明文字，从其后开始逐行读取状态。
        for (int lineIndex = HeaderLineCount; lineIndex < lines.Length; lineIndex++)
        {
            // 当前物理行去掉首尾空白后为空则跳过（文件末尾换行、空行）。
            string line = lines[lineIndex].Trim();
            if (line.Length == 0)
            {
                continue;
            }
            if (parsedCount >= expectedRowCount)
            {
                Debug.LogError("状态互斥表行数超过枚举状态总数 " + expectedRowCount + "，整份表拒绝装载。");
                _rows = EmptyRows;
                return false;
            }
            // 三列固定顺序：状态名、中文名、矩阵值串；值串内部只有竖线分隔的整数。
            string[] cells = line.Split(',');
            if (cells.Length < 3)
            {
                Debug.LogError("状态互斥表第 " + (lineIndex + 1) + " 行列数不足，整份表拒绝装载。");
                _rows = EmptyRows;
                return false;
            }
            // 该行自带的 RO 原始状态名，只用于行数据与日志，不参与下标校验。
            string stateId = cells[0].Trim();
            // 该行到各个目标状态的矩阵值，长度必须等于状态总数。
            int[] concatenate = ParseConcatenate(cells[2]);
            if (concatenate == null || concatenate.Length != expectedRowCount)
            {
                Debug.LogError("状态互斥表 " + stateId + " 的矩阵值数量与状态总数 " + expectedRowCount +
                    " 不一致，整份表拒绝装载。");
                _rows = EmptyRows;
                return false;
            }
            parsedRows[parsedCount] = new RowData(stateId, cells[1].Trim(), concatenate);
            parsedCount++;
        }
        if (parsedCount != expectedRowCount)
        {
            Debug.LogError("状态互斥表只有 " + parsedCount + " 行，枚举要求 " + expectedRowCount + " 行，整份表拒绝装载。");
            _rows = EmptyRows;
            return false;
        }
        _rows = parsedRows;
        return true;
    }

    /// <summary>把竖线分隔的整数串解析为整型数组，非法数字返回空。</summary>
    /// <param name="concatenateText">形如 <c>2|0|0|1|-1</c> 的矩阵值串。</param>
    /// <returns>解析成功的值数组；任一格不是整数时返回空。</returns>
    private static int[] ParseConcatenate(string concatenateText)
    {
        // 逐列切分后的原始文本，列数由状态总数决定。
        string[] valueCells = concatenateText.Split('|');
        // 解析结果数组：下标为目标状态枚举值，元素为该列读到的矩阵值。
        int[] values = new int[valueCells.Length];
        // 当前正在解析的列下标。
        for (int index = 0; index < valueCells.Length; index++)
        {
            // 当前单元格值：单格矩阵文本去掉首尾空白后的整数结果，取值含义见 MExlusionStatesCode。
            string cell = valueCells[index].Trim();
            int value;
            if (!int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                Debug.LogError("状态互斥表矩阵值不是整数：" + cell);
                return null;
            }
            values[index] = value;
        }
        return values;
    }
    #endregion

    #region 表行数据
    /// <summary>
    /// 单个状态作为“当前状态”时的一整行互斥配置，
    /// 字段语义与 RO <c>StateExclusionTable.RowData</c> 一致。
    /// </summary>
    public sealed class RowData
    {
        /// <summary>RO 原表里的状态名，例如 <c>State_H_Collection</c>，用于日志与提示定位。</summary>
        public string StateId { get; private set; }

        /// <summary>状态中文名，例如“采集_7”，用于界面提示。</summary>
        public string StateName { get; private set; }

        /// <summary>到各目标状态的矩阵值，下标为目标状态枚举值，取值见 <see cref="MExlusionStatesCode"/>。</summary>
        public int[] Concatenate { get; private set; }

        /// <summary>按 CSV 行构造一行只读矩阵数据。</summary>
        /// <param name="stateId">状态枚举名。</param>
        /// <param name="stateName">状态中文名。</param>
        /// <param name="concatenate">到各目标状态的矩阵值，长度等于状态总数。</param>
        public RowData(string stateId, string stateName, int[] concatenate)
        {
            StateId = stateId;
            StateName = stateName;
            Concatenate = concatenate;
        }
    }
    #endregion
}
