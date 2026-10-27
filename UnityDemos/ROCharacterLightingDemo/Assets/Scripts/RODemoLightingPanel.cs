using UnityEngine;

/// <summary>
/// 演示接入层（不是 RO 原版类型，是 Demo 自己的操作面板）：
/// 只负责"选哪套原版时段配置、用立即还是 2 秒插值去应用"，真正的写全局量逻辑全部在 MEnvorimentSetting 里，
/// 这样 Inspector 上的每个按钮走的都是 RO 原版调用链（ApplyEnvorimentData → InitOrChangeEnviromentSetting → ApplyDayTimeDataLerp）。
/// 编辑态按钮见 Assets/Scripts/Editor/RODemoLightingPanelEditor.cs；Play 模式用数字键 1/2/3、L 键、R 键。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("RO/演示：时段切换面板")]
public class RODemoLightingPanel : MonoBehaviour
{
    /// <summary>场景里的环境设置组件，按钮与快捷键都通过它应用配置；为空时面板不做任何事。</summary>
    public MEnvorimentSetting environment;

    /// <summary>可切换的时段配置列表，顺序约定为：0=白天、1=黄昏、2=夜晚（均来自 RO 原版 dt_*.asset 的数值）。</summary>
    public MDayTimeData[] dayTimes = new MDayTimeData[0];

    /// <summary>当前选中的配置下标，与 dayTimes 对齐；下次"重新应用"用它。</summary>
    public int currentIndex;

    /// <summary>true 表示应用时立即生效（等价源实现 fast=true），false 表示走 2 秒插值。</summary>
    public bool instantApply = true;

    /// <summary>Play 模式下响应数字键/热键，编辑态不处理输入。</summary>
    private void Update()
    {
        // 编辑态不读键盘，避免抢 Unity 快捷键
        if (Application.isPlaying == false)
        {
            return;
        }

        // 数字键 1/2/3 立即切到对应时段
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyIndex(0, true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyIndex(1, true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyIndex(2, true);
        }

        // L 键：插值切到下一套配置，用来看原版 2 秒过渡
        if (Input.GetKeyDown(KeyCode.L))
        {
            ApplyNextWithLerp();
        }

        // R 键：按当前参数重新应用一次（改完资产字段后按一下即可看到效果）
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReapplyCurrent();
        }
    }

    /// <summary>
    /// 切换到指定下标的时段配置。走 RO 原版入口 ApplyEnvorimentData，先 KillLerping 再按 instant 决定立即/插值。
    /// </summary>
    /// <param name="index">dayTimes 下标，越界时忽略。</param>
    /// <param name="instant">true 立即生效，false 走原版 2 秒插值。</param>
    public void ApplyIndex(int index, bool instant)
    {
        if (environment == null)
        {
            return;
        }
        if (dayTimes == null)
        {
            return;
        }
        if (index < 0)
        {
            return;
        }
        if (index >= dayTimes.Length)
        {
            return;
        }
        if (dayTimes[index] == null)
        {
            return;
        }

        currentIndex = index;
        instantApply = instant;

        // 组装源版入参结构：本 Demo 的环境数据组只带时段数据（光照图/天气字段未迁入）
        MEnvorimentData data = new MEnvorimentData();
        data.dayTimeData = dayTimes[index];
        environment.ApplyEnvorimentData(data, instant);
    }

    /// <summary>按当前 instantApply 重新应用当前配置；用户在 Inspector 改了资产字段后用它刷新。</summary>
    public void ReapplyCurrent()
    {
        ApplyIndex(currentIndex, instantApply);
    }

    /// <summary>插值切到下一套配置（循环），用来观察原版 2 秒过渡。</summary>
    public void ApplyNextWithLerp()
    {
        if (dayTimes == null)
        {
            return;
        }
        if (dayTimes.Length == 0)
        {
            return;
        }
        int next = currentIndex + 1;
        if (next >= dayTimes.Length)
        {
            next = 0;
        }
        ApplyIndex(next, false);
    }
}
