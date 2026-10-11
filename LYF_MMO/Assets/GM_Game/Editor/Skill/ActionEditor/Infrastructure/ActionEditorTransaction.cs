using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>将一次拖拽的多次修改合并成一个原生 Undo 操作；手势结束后释放目标引用。</summary>
    internal sealed class ActionEditorGesture
    {
        /// <summary>一次拖拽中正在修改的 Unity 编辑对象引用，手势结束后清空。</summary>
        Object target;
        /// <summary>该拖拽在 Unity Undo 中的分组号；-1 表示尚未开始或已经完成。</summary>
        int group = -1;

        /// <summary>在修改前记录目标的原生 Undo；手势版本将连续修改归入同一个撤销组。</summary>
        /// <param name="value">本次拖拽实际修改的 Unity 对象原引用；同一手势内连续记录该目标。</param>
        /// <param name="operation">本次 Unity Undo 的编辑说明，用于撤销历史显示，不是自动化命令标识。</param>
        internal void Record(Object value, string operation)
        {
            if (value == null) return;
            if (target != value || group < 0)
            {
                Complete();
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(operation);
                target = value;
            }
            Undo.RecordObject(value, operation);
        }

        /// <summary>提交手势内待记录快照并折叠撤销组，随后清空目标与组号，允许下一次拖拽。</summary>
        internal void Complete()
        {
            if (group < 0) return;
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            target = null;
            group = -1;
        }
    }

    /// <summary>对技能编辑资产统一记录撤销、设置脏标记和定点保存，不执行全项目保存。</summary>
    internal static class ActionEditorTransaction
    {
        /// <summary>在明确的编辑目标修改前保存原生 Undo 快照；不创建拖拽手势分组。</summary>
        /// <param name="target">将记录、标脏或保存的明确 Unity 编辑对象，不操作其他资源。</param>
        /// <param name="operation">本次 Unity Undo 的编辑说明，用于撤销历史显示，不是自动化命令标识。</param>
        internal static void Record(Object target, string operation)
        {
            if (target == null) return;
            Undo.RecordObject(target, operation);
        }

        /// <summary>将非空编辑目标标记为待保存，不立即写磁盘。</summary>
        /// <param name="target">将记录、标脏或保存的明确 Unity 编辑对象，不操作其他资源。</param>
        internal static void MarkChanged(Object target)
        {
            if (target == null) return;
            EditorUtility.SetDirty(target);
        }

        /// <summary>仅保存当前已经标脏的目标，避免保存其他用户未完成的资产。</summary>
        /// <param name="target">将记录、标脏或保存的明确 Unity 编辑对象，不操作其他资源。</param>
        internal static void SaveIfDirty(Object target)
        {
            if (target == null || !EditorUtility.IsDirty(target)) return;
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}
