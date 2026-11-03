using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Editor 中检查大状态互斥规则的专用测试，读取正式表和源码，不创建角色或发送协议。
/// 表会加载到单例内存，判决用例也会临时清空、填入单例状态列表；“只读”仅指不写入资产文件。
/// 部分检查直接对源码全文搜索字符串，注释也会参与匹配，不能把这类检查当作编译或运行验证。
/// </summary>
public static class StateExclusionContractTests
{

    #region 字段与常量
    /// <summary>本轮通过的断言数，每次 Run 从零开始。</summary>
    private static int _passed;

    /// <summary>本轮失败的断言数，非零时整体判定失败。</summary>
    private static int _failed;

    /// <summary>互斥管理器的私有判决方法名，测试通过反射直接喂入当前状态列表。</summary>
    private const string JudgeMethodName = "JudgeCurrentStates";

    /// <summary>客户端施法门口所在源码的工程相对路径。</summary>
    private const string NavigationSourcePath =
        "Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs";

    /// <summary>互斥管理器源码的工程相对路径，用于校验状态接入与门口顺序。</summary>
    private const string ManagerSourcePath =
        "Assets/GM_Game/Scripts/StateExclusion/StateExclusionManager.cs";
    #endregion

    #region 测试入口
    /// <summary>依次校验表尺寸、矩阵取值、判决语义、状态接入与施法门口顺序，最后输出汇总。</summary>
    public static void Run()
    {
        _passed = 0;
        _failed = 0;
        TestTableShape();
        TestMatrixValues();
        TestJudgeSemantics();
        TestDefaultAllow();
        TestConnectedStates();
        TestGateOrder();
        if (_failed == 0)
        {
            Debug.Log("STATE_EXCLUSION_CONTRACT_PASS assertions=" + _passed);
        }
        else
        {
            Debug.LogError("STATE_EXCLUSION_CONTRACT_FAIL passed=" + _passed + " failed=" + _failed);
        }
    }
    #endregion

    #region 表结构校验
    /// <summary>校验 RO 原表副本能整份装载，行数与列数对齐枚举，且原表自带的状态名按原样保留。</summary>
    private static void TestTableShape()
    {
        if (!LoadOfficialTable())
        {
            Fail("正式互斥表装载失败");
            return;
        }
        // 状态总数即矩阵行列数，也是整表应有的行数。
        int stateCount = (int)MExlusionStates.State_Last;
        Check(StateExclusionTable.singleton.Size == stateCount,
            "表行数等于状态总数 " + stateCount + "，实际 " + StateExclusionTable.singleton.Size);
        // 逐行确认矩阵列数完整；行名不做与枚举的相等校验，RO 按行下标消费。
        bool allRowsValid = true;
        for (int index = 0; index < StateExclusionTable.singleton.Size; index++)
        {
            // 当前下标的矩阵行。
            StateExclusionTable.RowData rowData = StateExclusionTable.singleton[index];
            if (rowData == null)
            {
                allRowsValid = false;
                continue;
            }
            if (rowData.Concatenate == null || rowData.Concatenate.Length != stateCount)
            {
                allRowsValid = false;
            }
        }
        Check(allRowsValid, "38 行每行 38 列");
        // 表头与说明行被跳过，第 0 行必须是首条数据。
        Check(StateExclusionTable.singleton[0].StateId == "State_A_InitiativeTransfigured",
            "第 0 行是首条数据而不是表头");
        // RO 原表 24 / 35 行自带的状态名与枚举名拼写不同，属于原表现状，必须原样保留。
        Check(StateExclusionTable.singleton[24].StateId == "State_Y_CoupleActionContinue",
            "第 24 行保留 RO 原名，实际 " + StateExclusionTable.singleton[24].StateId);
        Check(StateExclusionTable.singleton[35].StateId == "State_AJ_StateSinging",
            "第 35 行保留 RO 原名，实际 " + StateExclusionTable.singleton[35].StateId);
    }
    #endregion

    #region 矩阵取值校验
    /// <summary>核对 RO 原表中与本次接入状态相关的关键单元格，确认未改写任何矩阵值。</summary>
    private static void TestMatrixValues()
    {
        if (!LoadOfficialTable())
        {
            Fail("正式互斥表装载失败，跳过矩阵取值校验");
            return;
        }
        // 采集中不允许切吟唱。
        CheckCell(MExlusionStates.State_H_Collection, MExlusionStates.State_AJ_Singing, 0);
        // 攀爬中不允许切吟唱。
        CheckCell(MExlusionStates.State_G_Climb, MExlusionStates.State_AJ_Singing, 0);
        // 钓鱼中不允许切吟唱。
        CheckCell(MExlusionStates.State_S_FishingAction, MExlusionStates.State_AJ_Singing, 0);
        // 跳舞中不允许切吟唱。
        CheckCell(MExlusionStates.State_AH_Dance, MExlusionStates.State_AJ_Singing, 0);
        // 单人动作可以被施法直接打断。
        CheckCell(MExlusionStates.State_F_SingleAction, MExlusionStates.State_AJ_Singing, 1);
        // 已在吟唱中再请求吟唱属于未配置路径。
        CheckCell(MExlusionStates.State_AJ_Singing, MExlusionStates.State_AJ_Singing, -1);
        // 自动采集中切吟唱需要先退出采集。
        CheckCell(MExlusionStates.State_AQ_AutoCollect, MExlusionStates.State_AJ_Singing, 2);
        // 战斗载具到自身是纯前端可直接切换。
        CheckCell(MExlusionStates.State_Q_RidingFight, MExlusionStates.State_Q_RidingFight, 3);
    }

    /// <summary>核对单个矩阵单元格的取值。</summary>
    /// <param name="currentState">作为矩阵行的当前状态。</param>
    /// <param name="targetState">作为矩阵列的目标状态。</param>
    /// <param name="expectedCode">RO 原表该格的预期取值。</param>
    private static void CheckCell(MExlusionStates currentState, MExlusionStates targetState,
        int expectedCode)
    {
        // 当前状态对应的矩阵行，目标状态用于索引它的列。
        StateExclusionTable.RowData rowData = StateExclusionTable.singleton[(int)currentState];
        if (rowData == null)
        {
            Fail("缺少矩阵行：" + currentState);
            return;
        }
        // 该格的实际取值。
        int actualCode = rowData.Concatenate[(int)targetState];
        Check(actualCode == expectedCode, currentState + " → " + targetState + " 期望 " + expectedCode +
            "，实际 " + actualCode);
    }
    #endregion

    #region 判决语义校验
    /// <summary>
    /// 用反射直接调用私有判决方法并喂入构造的当前状态列表，
    /// 覆盖 0/-1 拒绝、1/2/3 放行、忽略“先离开再进入”以及多来源任一拒绝。
    /// 反射调用正式私有实现，但测试会修改单例里的当前状态列表；不改生产接口或资产。
    /// </summary>
    private static void TestJudgeSemantics()
    {
        if (!LoadOfficialTable())
        {
            Fail("正式互斥表装载失败，跳过判决语义校验");
            return;
        }
        // 反射取得的私有判决方法；缺失时说明实现被改动，直接判失败。
        MethodInfo judgeMethod = typeof(StateExclusionManager).GetMethod(
            JudgeMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (judgeMethod == null)
        {
            Fail("未找到私有判决方法 " + JudgeMethodName);
            return;
        }
        // 采集中切吟唱：单元格 0，必须拒绝。
        Check(Judge(judgeMethod, MExlusionStates.State_AJ_Singing,
                MExlusionStates.State_H_Collection, false) == false,
            "采集(0) 拒绝切吟唱");
        // 吟唱中切吟唱：单元格 -1，必须拒绝。
        Check(Judge(judgeMethod, MExlusionStates.State_AJ_Singing,
                MExlusionStates.State_AJ_Singing, false) == false,
            "吟唱(-1) 拒绝再切吟唱");
        // 单人动作切吟唱：单元格 1，允许。
        Check(Judge(judgeMethod, MExlusionStates.State_AJ_Singing,
                MExlusionStates.State_F_SingleAction, false) == true,
            "单人动作(1) 允许切吟唱");
        // 自动采集切吟唱：单元格 2，默认允许。
        Check(Judge(judgeMethod, MExlusionStates.State_AJ_Singing,
                MExlusionStates.State_AQ_AutoCollect, false) == true,
            "自动采集(2) 默认允许切吟唱");
        // 同一格 2 在忽略“先离开再进入”时按不可切换处理。
        Check(Judge(judgeMethod, MExlusionStates.State_AJ_Singing,
                MExlusionStates.State_AQ_AutoCollect, true) == false,
            "自动采集(2) 在忽略先离开再进入时拒绝");
        // 战斗载具到自身：单元格 3，纯前端允许。
        Check(Judge(judgeMethod, MExlusionStates.State_Q_RidingFight,
                MExlusionStates.State_Q_RidingFight, false) == true,
            "战斗载具(3) 允许前端切换");
        // 多来源任一禁止：单人动作允许、采集禁止，整体必须拒绝。
        StateExclusionManager.singleton.PlayerExclusionStateList.Clear();
        StateExclusionManager.singleton.AddStateToList(MExlusionStates.State_F_SingleAction);
        StateExclusionManager.singleton.AddStateToList(MExlusionStates.State_H_Collection);
        // 多来源整体判决结果。
        object multiResult = judgeMethod.Invoke(StateExclusionManager.singleton,
            new object[] { MExlusionStates.State_AJ_Singing, false, false });
        Check(multiResult is bool && (bool)multiResult == false, "多个来源中任一禁止即整体拒绝");
        StateExclusionManager.singleton.PlayerExclusionStateList.Clear();
    }

    /// <summary>把单个当前状态放进管理器列表后调用私有判决方法。</summary>
    /// <param name="judgeMethod">反射取得的私有判决方法。</param>
    /// <param name="targetState">目标状态。</param>
    /// <param name="currentState">唯一一个当前状态。</param>
    /// <param name="ignoreOneLeaveOneEnter">是否忽略“先离开再进入”。</param>
    /// <returns>正式实现给出的判决结果。</returns>
    private static bool Judge(MethodInfo judgeMethod, MExlusionStates targetState,
        MExlusionStates currentState, bool ignoreOneLeaveOneEnter)
    {
        StateExclusionManager.singleton.PlayerExclusionStateList.Clear();
        StateExclusionManager.singleton.AddStateToList(currentState);
        // 正式实现返回的装箱布尔值。
        object result = judgeMethod.Invoke(StateExclusionManager.singleton,
            new object[] { targetState, false, ignoreOneLeaveOneEnter });
        return result is bool && (bool)result;
    }
    #endregion

    #region 默认态校验
    /// <summary>
    /// 编辑器内没有本地主角，等于“未持有任何大状态”，必须先收集到空列表再直接放行；
    /// 目标为默认态时同样因为不查表而放行，不依赖任何专门的默认态分支。
    /// </summary>
    private static void TestDefaultAllow()
    {
        if (!LoadOfficialTable())
        {
            Fail("正式互斥表装载失败，跳过默认态校验");
            return;
        }
        Check(StateExclusionManager.singleton.GetIsCanChangeState(MExlusionStates.State_AJ_Singing, false),
            "无大状态时默认放行切吟唱");
        Check(StateExclusionManager.singleton.GetIsCanChangeState(MExlusionStates.State_Default_State, false),
            "无大状态时目标默认态同样放行");
    }
    #endregion

    #region 状态接入校验
    /// <summary>
    /// 校验已接入的五个真实状态：StateDefine 成员必须存在，且管理器源码中确实按该状态收集；
    /// 同时确认通用特殊动作没有被冒充成单人动作。这是静态源码契约，不依赖运行时实体。
    /// </summary>
    private static void TestConnectedStates()
    {
        // 已接入的动作状态与对应的大状态，顺序与 GetPlayerCurrentState 一致。
        StateDefine[] connectedStates =
        {
            StateDefine.kStateCollect,
            StateDefine.kStateClimbing,
            StateDefine.kStateSinging,
            StateDefine.kStateFishing,
            StateDefine.kStateDance,
        };
        // 与之对应的互斥状态，用于拼接源码断言片段。
        MExlusionStates[] connectedExclusionStates =
        {
            MExlusionStates.State_H_Collection,
            MExlusionStates.State_G_Climb,
            MExlusionStates.State_AJ_Singing,
            MExlusionStates.State_S_FishingAction,
            MExlusionStates.State_AH_Dance,
        };
        // 管理器源码文本；读不到时判失败，避免静默通过。
        string managerSource = ReadProjectText(ManagerSourcePath);
        if (string.IsNullOrEmpty(managerSource))
        {
            Fail("读取互斥管理器源码失败");
            return;
        }
        // 逐个确认枚举成员存在且源码中存在“判断该状态→收集对应大状态”的片段。
        for (int index = 0; index < connectedStates.Length; index++)
        {
            // 当前待验证的已接入动作枚举名称。
            string stateName = connectedStates[index].ToString();
            Check(Array.IndexOf(Enum.GetNames(typeof(StateDefine)), stateName) >= 0,
                "StateDefine 存在成员 " + stateName);
            // 源码中用于识别状态判断的固定文本。
            string collectSnippet = "currentState == StateDefine." + stateName;
            // 源码中用于识别收集互斥状态的固定文本。
            string addSnippet = "AddStateToList(MExlusionStates." + connectedExclusionStates[index] + ")";
            // 首次出现判断文本的位置，搜索包括注释。
            int collectIndex = managerSource.IndexOf(collectSnippet, StringComparison.Ordinal);
            // 首次出现收集文本的位置，需位于判断文本之后。
            int addIndex = managerSource.IndexOf(addSnippet, StringComparison.Ordinal);
            Check(collectIndex >= 0 && addIndex > collectIndex,
                "管理器按 " + stateName + " 收集 " + connectedExclusionStates[index]);
        }
        // 通用特殊动作缺少可证明为单人动作的子类型信息，不得再被硬映射。
        Check(managerSource.IndexOf("kStateSpecial", StringComparison.Ordinal) < 0,
            "管理器未把通用特殊动作冒充单人动作");
        Check(managerSource.IndexOf("targetState == MExlusionStates.State_Default_State",
                StringComparison.Ordinal) < 0,
            "GetIsCanChangeState 没有默认态提前放行分支");
    }
    #endregion

    #region 门口顺序校验
    /// <summary>
    /// 校验施法门口的调用顺序：状态互斥必须在 CanStart、发包与预表现之前，拒绝时才能做到不发包不预表现。
    /// </summary>
    private static void TestGateOrder()
    {
        // 施法门口源码文本；读不到时判失败。
        string navigationSource = ReadProjectText(NavigationSourcePath);
        if (string.IsNullOrEmpty(navigationSource))
        {
            Fail("读取导航组件源码失败");
            return;
        }
        // 互斥判定的调用位置。
        int gateIndex = navigationSource.IndexOf(
            "GetIsCanChangeState(MExlusionStates.State_AJ_Singing, false)", StringComparison.Ordinal);
        if (gateIndex < 0)
        {
            Fail("导航组件缺少施法前状态互斥判定");
            return;
        }
        // 原技能准入、发包与本地预表现的调用位置。
        int canStartIndex = navigationSource.IndexOf("CanStart(skillArgs.Identify", StringComparison.Ordinal);
        // 技能发包协议常量在源码全文第一次出现的位置。
        int sendIndex = navigationSource.IndexOf("CMD_SkillCastCode", StringComparison.Ordinal);
        // 本地预测调用文本第一次出现的位置。
        int predictIndex = navigationSource.IndexOf(".BeginPrediction(", StringComparison.Ordinal);
        Check(canStartIndex > gateIndex, "互斥判定在 CanStart 之前");
        Check(sendIndex > gateIndex, "互斥判定在技能发包之前");
        Check(predictIndex > gateIndex, "互斥判定在本地预表现之前");
    }
    #endregion

    #region 测试辅助
    /// <summary>装载正式 CSV 并整份覆盖表实例，保证断言基于当前仓库数据。</summary>
    /// <returns>装载成功返回 true。</returns>
    private static bool LoadOfficialTable()
    {
        // Resources 下的正式互斥表文本。
        TextAsset asset = Resources.Load<TextAsset>("StateExclusionTable");
        if (asset == null)
        {
            return false;
        }
        // 本次装载是否通过行数、行序与列数校验。
        bool isLoaded = StateExclusionTable.singleton.LoadFromCsv(asset.text);
        Resources.UnloadAsset(asset);
        return isLoaded;
    }

    /// <summary>按工程相对路径读取源码文本，供静态契约断言使用。</summary>
    /// <param name="projectRelativePath">以 Assets/ 开头的工程相对路径。</param>
    /// <returns>文件全文；文件不存在时返回空串，其他 I/O 异常仍向外抛出。</returns>
    private static string ReadProjectText(string projectRelativePath)
    {
        // 工程根目录，源自 Assets 的上一级。
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        // 目标源文件绝对路径。
        string fullPath = Path.Combine(projectRoot, projectRelativePath);
        if (!File.Exists(fullPath))
        {
            return string.Empty;
        }
        return File.ReadAllText(fullPath);
    }

    /// <summary>记录一条通过断言。</summary>
    /// <param name="condition">断言条件。</param>
    /// <param name="description">断言描述，失败时输出。</param>
    private static void Check(bool condition, string description)
    {
        if (condition)
        {
            _passed++;
            return;
        }
        Fail(description);
    }

    /// <summary>记录一条失败断言并输出原因。</summary>
    /// <param name="description">失败描述。</param>
    private static void Fail(string description)
    {
        _failed++;
        Debug.LogError("状态互斥契约失败：" + description);
    }
    #endregion
}
