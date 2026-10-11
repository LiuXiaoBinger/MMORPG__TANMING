// 宿主加载窗口及五种特效定位参考，保留粒子局部坐标与原角色挂点语义。
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor.Editor;

/// <summary>为宿主编辑资产提供现有窗口入口，以及挂点、前方和目标落点预览。</summary>
public partial class SkillEditorWindow
{
    /// <summary>预览目标只用于 Scene 中位置参照，不写入技能资产或正式场景。</summary>
    public GameObject previewTarget;

    /// <summary>载入宿主明确选择的技能和预览模型，复用原窗口的保存、Undo 与动画采样。</summary>
    /// <param name="action">待编辑的正式或参考 Y 技能资产。</param>
    /// <param name="actor">已有角色预制体，窗口生成自己的隔离预览副本。</param>
    public void LoadAction(SkillConfigSO action, GameObject actor)
    {
        SelectConfig(action);
        previewModel = actor;
        if (action != null)
        {
            ActionEditorUserSettings.instance.SetPreviewModel(action, actor);
        }
        EnsurePreviewInstance();
        UpdateLocalizedTitle();
        Repaint();
    }

    /// <summary>确定生成粒子的世界姿态；目标缺失明确提示兜底，挂点缺失拒绝生成。</summary>
    /// <param name="actor">窗口的隔离角色预览副本。</param>
    /// <param name="effect">粒子事件，位置和旋转由模式解释。</param>
    /// <param name="logError">生成时报告问题；Scene 标记计算保持静默。</param>
    /// <param name="anchor">仅角色局部模式返回可跟随的实际节点。</param>
    /// <param name="position">计算后的世界位置，单位为 Unity 米。</param>
    /// <param name="rotation">计算后的世界旋转。</param>
    /// <returns>位置可准确计算时返回 true；缺角色或挂点时返回 false。</returns>
    bool TryResolvePreviewFxPose(GameObject actor, Global.FxAndSound effect, bool logError,
        out Transform anchor, out Vector3 position, out Quaternion rotation)
    {
        anchor = null;
        position = effect.offset;
        rotation = Quaternion.Euler(effect.rotation);
        // 兼容旧资产 useWorldSpace 与 socketPath，显式宿主模式优先。
        ActionFxPlacementKind placement = effect.placementKind;
        if (placement == ActionFxPlacementKind.ActorRoot && effect.useWorldSpace)
        {
            placement = ActionFxPlacementKind.WorldAbsolute;
        }
        if (placement == ActionFxPlacementKind.WorldAbsolute)
        {
            return true;
        }
        if (actor == null)
        {
            if (logError) Debug.LogError(SkillEditorLocalization.Text("Skill editor effects require a preview actor.", "技能编辑器特效需要预览角色。"));
            return false;
        }
        if (placement == ActionFxPlacementKind.ActorRoot || placement == ActionFxPlacementKind.NamedSocket)
        {
            anchor = ResolvePreviewFxAnchor(actor, effect.socketPath, logError);
            if (anchor == null) return false;
            position = anchor.TransformPoint(effect.offset);
            rotation = anchor.rotation * Quaternion.Euler(effect.rotation);
            return true;
        }
        if (placement == ActionFxPlacementKind.CasterForwardWorld)
        {
            position = actor.transform.position + actor.transform.forward * effect.forwardDistance + effect.offset;
            // 对照旧 skill03_2：世界 X 倾角由事件给出，朝向来自角色当前 Y/Z 角。
            Vector3 actorEuler = actor.transform.eulerAngles;
            rotation = Quaternion.Euler(effect.rotation.x, actorEuler.y, actorEuler.z);
            return true;
        }
        if (placement == ActionFxPlacementKind.TargetTop)
        {
            if (previewTarget == null)
            {
                position = actor.transform.position + actor.transform.forward * effect.forwardDistance + effect.offset;
                if (logError) Debug.LogWarning(SkillEditorLocalization.Text("TargetTop has no preview target: showing the caster-forward fallback only; target-top placement is not verified.", "目标头顶模式未指定预览目标：当前仅显示角色前方兜底，目标头顶尚未验证。"));
                return true;
            }
            // 渲染边界用于编辑器可见模型高度；正式运行读取宿主实体的 Model Height。
            float top = previewTarget.transform.position.y;
            // 预览目标渲染器快照，包围盒仅用于目标头顶定位。
            Renderer[] renderers = previewTarget.GetComponentsInChildren<Renderer>(true);
            // 当前原列表元素的零起始下标。
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].bounds.max.y > top) top = renderers[index].bounds.max.y;
            }
            position = previewTarget.transform.position;
            position.y = top;
            position += effect.offset;
            return true;
        }
        if (logError) Debug.LogError(SkillEditorLocalization.Text("Invalid skill effect placement kind: ", "技能编辑器特效位置模式无效：") + (int)placement);
        return false;
    }
}
