using UnityEngine;

/// <summary>
/// 技能输入的入口，把已学技能、释放点和目标交给 MWaitingDataMgr。
/// 后续由导航组件决定何时发送施法请求。
/// </summary>
public static class MSkillClassicsHandler
{
    /// <summary>提交一次技能输入；当前快捷键调用传入角色位置，本方法原样转交参数。</summary>
    /// <param name="originSkill">当前已学技能核心，本次输入不替换连招技能。</param>
    /// <param name="castPoint">输入选择的世界空间释放点，单位米。</param>
    /// <param name="target">当前选择目标；现有无目标协议传空。</param>
    public static void CastSkill(MSkillCore originSkill, Vector3 castPoint, MEntity target)
    {
        MWaitingDataMgr.singleton.
            SetWaitSkill(originSkill, castPoint, target);
    }
}
