using UnityEngine;

/// <summary>RO同名技能输入载荷；仅描述导航到达后要释放的技能，不承担服务器开始广播。</summary>
public sealed class MSkillEventArgs : IEventArg
{
    /// <summary>本项目技能主表ID；对应RO Identify，在导航到达时用于查询已学核心。</summary>
    public int Identify { get; private set; }
    /// <summary>输入选择的世界空间释放点，单位米；当前无目标协议不会上传该坐标。</summary>
    public Vector3 CastPoint { get; private set; }
    /// <summary>输入选择的目标引用，只在本次同步派发期间有效。</summary>
    public MEntity Target { get; private set; }
    /// <summary>事件池回收标记，true表示不可继续使用本次载荷。</summary>
    public bool Deprecated { get; set; }

    /// <summary>事件池创建空指令，具体技能在Create中写入。</summary>
    public MSkillEventArgs() { }

    /// <summary>借出本次导航所需的技能输入指令。</summary>
    /// <param name="identify">已学习技能的主表ID。</param>
    /// <param name="castPoint">世界空间释放点，单位米。</param>
    /// <param name="target">选择目标；无目标技能为空。</param>
    /// <returns>归导航事件拥有并在同步派发结束时回收的指令。</returns>
    public static MSkillEventArgs Create(int identify, Vector3 castPoint, MEntity target)
    {
        // 当前派发独占的池化技能指令，归还后不能被导航组件保存。
        MSkillEventArgs args = MEventPool<MSkillEventArgs>.GetEvent();
        args.Identify = identify;
        args.CastPoint = castPoint;
        args.Target = target;
        return args;
    }

    /// <summary>复制一份独立生命周期的技能输入指令。</summary>
    /// <returns>由复制件持有者负责回收的池化指令。</returns>
    public IEventArg Clone() { return Create(Identify, CastPoint, Target); }

    /// <summary>清除本次输入并归还池，不持有角色或目标引用。</summary>
    public void Recycle()
    {
        Identify = 0;
        CastPoint = Vector3.zero;
        Target = null;
        MEventPool<MSkillEventArgs>.Recycle(this);
    }
}
