using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RO 客户端大状态互斥管理器：在发包前按 <see cref="StateExclusionTable"/> 判定
/// “当前持有的多个大状态”能否切换到目标状态，矩阵语义与 RO
/// <c>MoonClient/StateExclusion/StateExclusionManager.cs</c> 一致。
/// 只做客户端准入判定，真正退旧状态与结果仍由服务端权威处理，不修改任何协议字段。
/// </summary>
public sealed class StateExclusionManager
{

    #region 字段与常量
    /// <summary>Resources 下的表名；正式 CSV 位于 Data/StateExclusion/Resources 目录。</summary>
    private const string TableResourceName = "StateExclusionTable";

    /// <summary>
    /// 客户端开关的固定取值，对应 RO <c>config/Table/CSV/GlobalTableSystem.csv</c> 第 579 行
    /// <c>StateExclusionClientCheck=1</c>。本项目没有 RO 整张全局配置表，这里按已验证的开启值固化，
    /// 置 0 即整体关闭客户端互斥判定。
    /// </summary>
    private const int StateExclusionClientCheck = 1;

    /// <summary>RO 命名风格的单例入口。</summary>
    public static StateExclusionManager singleton
    {
        get { return _singleton; }
    }

    /// <summary>进程内唯一管理器，冷启动阶段由 <see cref="Global"/> 初始化。</summary>
    private static readonly StateExclusionManager _singleton = new StateExclusionManager();

    /// <summary>当前角色持有的大状态列表，每次查询原地清空复用，避免施法热路径反复分配集合。</summary>
    private readonly List<MExlusionStates> _playerExclusionStateList = new List<MExlusionStates>();

    /// <summary>本次进程是否已完成装载尝试，包括缺表或表非法，装载失败不会被后续查询反复重试。</summary>
    private bool _isInitialized;

    /// <summary>客户端互斥开关是否开启；关闭时所有判定直接放行。</summary>
    private bool _isClientCheckEnabled;

    /// <summary>
    /// 当前角色持有的大状态列表，由 <see cref="GetPlayerCurrentState"/> 原地清空后重新填充。
    /// 这里暴露的是同一个复用实例，不是只读快照：调用方读取即可，写入会污染下一次判定。
    /// </summary>
    public List<MExlusionStates> PlayerExclusionStateList
    {
        get { return _playerExclusionStateList; }
    }
    #endregion

    #region 配置读取与解析
    /// <summary>
    /// 同步装载互斥表并锁定开关。Global 在角色开放前调用；重复调用只装载一次。
    /// 缺表或表非法时保留空表并记录错误，此时只有处于已接入大状态的角色会被判定拒绝。
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;
        _isClientCheckEnabled = StateExclusionClientCheck == 1;
        // Resources 同步装载文本资产，解析完成后立即释放句柄，不长期占用 Unity 资源。
        TextAsset asset = Resources.Load<TextAsset>(TableResourceName);
        if (asset == null)
        {
            Debug.LogError("状态互斥表未找到，客户端互斥判定将缺少矩阵数据。");
            return;
        }
        try
        {
            StateExclusionTable.singleton.LoadFromCsv(asset.text);
        }
        finally
        {
            Resources.UnloadAsset(asset);
        }
    }
    #endregion

    #region 业务逻辑：当前状态收集
    /// <summary>
    /// 按 RO 的顺序收集本地主角当前持有的全部大状态。本项目尚无载具、变身、自拍、
    /// 观战、跑商等玩法来源，这些状态不会进入列表；列表为空表示处于默认态，可直接切换。
    /// </summary>
    /// <returns>复用同一个列表实例的当前状态集合，返回值在下一次调用前有效。</returns>
    public List<MExlusionStates> GetPlayerCurrentState()
    {
        _playerExclusionStateList.Clear();
        // 未创建世界或本地主角缺失（登录中、切场景）时按默认态处理，不伪造状态。
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return _playerExclusionStateList;
        }
        // 本地主角实体；服务端尚未下发角色时为空。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null || localRole.Machine == null)
        {
            return _playerExclusionStateList;
        }
        // 当前动作状态机的唯一状态，生活动作与战斗动作互斥来源。
        StateDefine currentState = localRole.CurrentState;
        // 采集：对应 RO kStateCollect / State_H_Collection。
        if (currentState == StateDefine.kStateCollect)
        {
            AddStateToList(MExlusionStates.State_H_Collection);
        }
        // 攀爬：对应 RO IsClimb / State_G_Climb。
        if (currentState == StateDefine.kStateClimbing)
        {
            AddStateToList(MExlusionStates.State_G_Climb);
        }
        // 吟唱：对应 RO kStateSinging / State_AJ_Singing。
        if (currentState == StateDefine.kStateSinging)
        {
            AddStateToList(MExlusionStates.State_AJ_Singing);
        }
        // 钓鱼：对应 RO IsFishing / State_S_FishingAction。
        if (currentState == StateDefine.kStateFishing)
        {
            AddStateToList(MExlusionStates.State_S_FishingAction);
        }
        // 跳舞：对应 RO IsDancing / State_AH_Dance。
        if (currentState == StateDefine.kStateDance)
        {
            AddStateToList(MExlusionStates.State_AH_Dance);
        }
        return _playerExclusionStateList;
    }

    /// <summary>向当前状态列表追加一个状态，重复状态不重复加入。</summary>
    /// <param name="addState">需要追加的大状态。</param>
    public void AddStateToList(MExlusionStates addState)
    {
        if (!_playerExclusionStateList.Contains(addState))
        {
            _playerExclusionStateList.Add(addState);
        }
    }
    #endregion

    #region 业务逻辑：状态切换准入
    /// <summary>
    /// 判断当前持有的全部大状态能否切换到目标状态，语义与 RO
    /// <c>GetIsCanChangeState(MExlusionStates, bool, bool)</c> 一致：
    /// 当前状态列表为空（处于默认态）时放行；任一当前状态在矩阵中返回 0 或 -1 即拒绝；
    /// 2 需要先退旧状态；3 为纯前端可切；1 可直接切换。
    /// </summary>
    /// <param name="targetState">准备切入的目标状态。</param>
    /// <param name="showErrorTips">被拒绝时是否用系统提示告知玩家；施法入口传 false，不打断输入。</param>
    /// <param name="ingnoreOneLeaveOneEnter">为 true 时把“先离开再进入”也视为不可切换，沿 RO 参数原拼写。</param>
    /// <returns>允许切换时返回 true；被任一当前状态拒绝时返回 false。</returns>
    public bool GetIsCanChangeState(MExlusionStates targetState, bool showErrorTips = true,
        bool ingnoreOneLeaveOneEnter = false)
    {
        Initialize();
        if (!_isClientCheckEnabled)
        {
            return true;
        }
        GetPlayerCurrentState();
        if (_playerExclusionStateList.Count == 0)
        {
            return true;
        }
        return JudgeCurrentStates(targetState, showErrorTips, ingnoreOneLeaveOneEnter);
    }

    /// <summary>
    /// 按当前状态列表逐个查矩阵判定是否允许切换，任一当前状态不允许即整体拒绝。
    /// 判定顺序与 RO 的循环一致：先判当前状态行是否存在，再取目标列并解释单元格语义。
    /// </summary>
    /// <param name="targetState">准备切入的目标状态，作为矩阵列下标。</param>
    /// <param name="showErrorTips">被拒绝时是否用系统提示告知玩家。</param>
    /// <param name="ingnoreOneLeaveOneEnter">为 true 时把“先离开再进入”视为不可切换。</param>
    /// <returns>全部当前状态都允许时返回 true。</returns>
    private bool JudgeCurrentStates(MExlusionStates targetState, bool showErrorTips,
        bool ingnoreOneLeaveOneEnter)
    {
        // 逐个当前状态查矩阵，任一不允许即整体拒绝，与 RO 的短路顺序一致。
        bool returnState = false;
        for (int index = 0; index < _playerExclusionStateList.Count; index++)
        {
            // 当前遍历到的大状态，作为矩阵的行。
            MExlusionStates currentState = _playerExclusionStateList[index];
            if (currentState == MExlusionStates.State_Default_State)
            {
                continue;
            }
            // 当前状态对应的矩阵行；未装载或缺行时视为配置错误。
            StateExclusionTable.RowData rowData = GetDataByState(currentState);
            if (rowData == null)
            {
                Debug.LogError("状态互斥表缺少当前状态配置：" + currentState);
                returnState = false;
            }
            else
            {
                // 目标状态作为列下标，必须先确认没有越界再取值。
                if ((int)targetState < 0 || (int)targetState >= rowData.Concatenate.Length)
                {
                    Debug.LogError("状态互斥表目标状态越界：" + targetState);
                    return false;
                }
                // 该单元格的切换许可值，取值含义见 MExlusionStatesCode。
                int stateCode = rowData.Concatenate[(int)targetState];
                if (stateCode == (int)MExlusionStatesCode.NoExist)
                {
                    returnState = false;
                }
                else if (stateCode == (int)MExlusionStatesCode.CanNotTrans)
                {
                    returnState = false;
                }
                else if (stateCode == (int)MExlusionStatesCode.NormalPss)
                {
                    returnState = true;
                }
                else if (stateCode == (int)MExlusionStatesCode.OneLeaveOneEnter)
                {
                    // 需要先退旧状态；调用方明确忽略该语义时按不可切换处理。
                    if (ingnoreOneLeaveOneEnter)
                    {
                        returnState = false;
                    }
                    else
                    {
                        returnState = true;
                    }
                }
                else if (stateCode == (int)MExlusionStatesCode.ClientState)
                {
                    returnState = true;
                }
            }
            if (!returnState)
            {
                if (showErrorTips)
                {
                    ShowMsgByTwoState(targetState, currentState);
                }
                return false;
            }
        }
        return returnState;
    }

    /// <summary>按状态枚举值取矩阵行，越界或未装载时返回空。</summary>
    /// <param name="mExlusionStates">要查询的状态。</param>
    /// <returns>该状态的矩阵行；表中没有对应行时为空。</returns>
    private static StateExclusionTable.RowData GetDataByState(MExlusionStates mExlusionStates)
    {
        return StateExclusionTable.singleton[(int)mExlusionStates];
    }

    /// <summary>
    /// 用系统提示告知“当前状态无法切到目标状态”。RO 使用专门的错误提示表按
    /// <c>ErrA状态ToB状态</c> 取文案，本项目暂无该表，按 RO 最终兜底分支用两个状态中文名拼接。
    /// </summary>
    /// <param name="targetState">准备切入的目标状态。</param>
    /// <param name="currentState">当前持有的状态。</param>
    private static void ShowMsgByTwoState(MExlusionStates targetState, MExlusionStates currentState)
    {
        // 目标状态的中文名，缺失时退回枚举名。
        string targetName = GetStateName(targetState);
        // 当前状态的中文名，缺失时退回枚举名。
        string currentName = GetStateName(currentState);
        TipsMgr.Instance.ShowSystemTips("当前处于" + currentName + "，无法切换到" + targetName);
    }

    /// <summary>取状态中文名；表中没有该行时退回枚举名，保证提示可读。</summary>
    /// <param name="state">要显示的状态。</param>
    /// <returns>状态中文名或枚举名。</returns>
    private static string GetStateName(MExlusionStates state)
    {
        // 该状态在互斥表中的行，用来读取中文名。
        StateExclusionTable.RowData rowData = GetDataByState(state);
        if (rowData == null)
        {
            return state.ToString();
        }
        return rowData.StateName;
    }
    #endregion
}
