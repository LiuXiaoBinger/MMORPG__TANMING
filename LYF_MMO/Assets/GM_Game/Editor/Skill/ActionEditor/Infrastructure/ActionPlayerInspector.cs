using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>原 Y 独立播放器的运行诊断面板；本项目正式角色不依赖该播放器。</summary>
    [CustomEditor(typeof(ActionPlayer))]
    public sealed class ActionPlayerInspector : UnityEditor.Editor
    {
        /// <summary>绘制原 Y 播放器字段和运行交互诊断，Edit Mode 不读取运行评估。</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if(!Application.isPlaying) return;
            // 检查对象已有的原 Y ActionPlayer 组件，不添加或启动该播放器。
            var player=(ActionPlayer)target;
            player.InteractionDiagnosticsEnabled=EditorGUILayout.Toggle("Interaction diagnostics",player.InteractionDiagnosticsEnabled);
            EditorGUILayout.LabelField("Action / frame",(player.CurrentConfig==null ? "Idle" : player.CurrentConfig.name)+" / "+player.CurrentFrame);
            if(!string.IsNullOrEmpty(player.LastInteractionRejection)) EditorGUILayout.HelpBox(player.LastInteractionRejection,MessageType.None);
            // 原 Y 独立播放器最近一次交互候选评估，显示优先级与选中结果。
            foreach(var trace in player.LastInteractionEvaluation)
            {
                EditorGUILayout.LabelField($"[{trace.index}] {trace.id} @ {trace.frame}",trace.selected ? "Selected" : trace.accepted ? "Lower priority" : "Rejected");
                if(!string.IsNullOrEmpty(trace.reason)) EditorGUILayout.LabelField(trace.reason,EditorStyles.wordWrappedMiniLabel);
                // 当前候选的条件诊断，包含路径、是否通过及具体拒绝原因。
                foreach(var condition in trace.conditions) EditorGUILayout.LabelField(condition.path+": "+condition.accepted+" "+condition.reason,EditorStyles.wordWrappedMiniLabel);
            }
            Repaint();
        }
    }
}
