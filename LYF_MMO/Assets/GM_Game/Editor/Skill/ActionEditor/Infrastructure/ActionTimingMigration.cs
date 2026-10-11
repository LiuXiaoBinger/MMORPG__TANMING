using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>旧剪辑帧尺到显式时间轴帧率的手动迁移；保留事件帧号，混合帧率须显式确认。</summary>
    public static class ActionTimingMigration
    {
        /// <summary>保存恢复点后把旧时间尺改为显式计时；失败恢复原稿，最终销毁未持久化副本。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        /// <param name="acknowledgeMixedRates">用户是否已确认采用第一剪辑帧尺；混合来源帧率时必须为 true 才迁移。</param>
        /// <param name="backupPath">输出备份资产路径，供迁移失败后的恢复与人工核查。</param>
        /// <param name="error">输出本次操作失败原因；成功时为空，供窗口显示而非运行事件载荷。</param>
        public static bool Migrate(SkillConfigSO config, bool acknowledgeMixedRates, out string backupPath, out string error)
        {
            backupPath = null; error = null;
            if (config == null || EditorApplication.isPlayingOrWillChangePlaymode) { error = "Select an action in Edit Mode."; return false; }
            if (config.UsesExplicitTiming) return true;
            // 此次旧计时迁移原资产的明确路径，须为 Assets 内已持久化对象。
            string path = AssetDatabase.GetAssetPath(config);
            if (!path.StartsWith("Assets/",StringComparison.Ordinal)) { error = "Only project Assets can be migrated."; return false; }
            // 迁移沿用的旧第一剪辑帧尺，单位帧/秒；事件坐标仍保留原帧号。
            float fps = ActionTiming.FrameRate(config);
            if (!acknowledgeMixedRates && config.GetEffectiveSegments().Any(/* 旧计时配置的动画片段原引用，用来源帧率判断是否混合帧尺。 */ s => s?.clip != null && !Mathf.Approximately(ActionTiming.SourceRate(s),fps)))
            { error = "Mixed source FPS: confirm the first-clip timeline ruler, then review clip durations, overlap and all event positions."; return false; }
            // 完整配置临时副本，所有字段修改和验证在提交前执行，未持久化时最终销毁。
            var draft = UnityEngine.Object.Instantiate(config);
            // 提交前保存的配置完整副本；失败用于恢复，临时副本最终销毁。
            var recovery = UnityEngine.Object.Instantiate(config);
            draft.name = recovery.name = config.name;
            // 原配置是否已经提交了迁移值；失败且 true 时用恢复点复制回原稿。
            bool changed = false;
            try
            {
                draft.InitializeExplicitTiming(fps);
                // 迁移后临时副本的验证诊断，有 Error 时不会更改原资产。
                var issues = ActionConfigValidator.Validate(draft);
                if (ActionConfigValidator.HasErrors(issues)) { error = string.Join("\n", issues.Where(/* 迁移草稿验证的单条诊断，仅 Error 项组成阻断迁移的原因文本。 */ i=>i.Severity==ActionValidationSeverity.Error).Select(/* 迁移草稿验证的单条诊断，仅 Error 项组成阻断迁移的原因文本。 */ i=>i.Message)); return false; }
                // 本次迁移或恢复使用的明确备份目录，不移动其他资产。
                string directory = "Assets/ActionEditorBackups/Timing";
                ActionConfigMigrationService.EnsureAssetDirectory(directory);
                backupPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + Path.GetFileName(path));
                AssetDatabase.CreateAsset(recovery,backupPath);
                AssetDatabase.SaveAssetIfDirty(recovery);
                Undo.RegisterCompleteObjectUndo(config,"Migrate Action Timing");
                changed=true;
                EditorUtility.CopySerialized(draft,config);
                EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
                return true;
            }
            catch/* 本次操作捕获的具体异常，记录失败原因；已变更原稿按恢复点回滚。 */ (Exception exception)
            {
                error=exception.Message;
                if(changed) { EditorUtility.CopySerialized(recovery,config); EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config); }
                return false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(draft);
                if(!AssetDatabase.Contains(recovery)) UnityEngine.Object.DestroyImmediate(recovery);
            }
        }
    }
}
