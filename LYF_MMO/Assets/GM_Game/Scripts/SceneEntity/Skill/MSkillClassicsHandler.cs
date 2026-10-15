using UnityEngine;

/// <summary>沿RO经典技能释放入口交给技能交接模块，快捷键控制器不直接发送施法协议。</summary>
public static class MSkillClassicsHandler
{
    /// <summary>提交本次经典技能输入；目标选择及拖盘操作尚未接入，当前由快捷键提供角色位置。</summary>
    /// <param name="originSkill">当前已学技能核心，本次输入不替换连招技能。</param>
    /// <param name="castPoint">输入选择的世界空间释放点，单位米。</param>
    /// <param name="target">当前选择目标；现有无目标协议传空。</param>
    public static void CastSkill(MSkillCore originSkill, Vector3 castPoint, MEntity target)
    {
        MWaitingDataMgr.singleton.
            SetWaitSkill(originSkill, castPoint, target);
    }
}
