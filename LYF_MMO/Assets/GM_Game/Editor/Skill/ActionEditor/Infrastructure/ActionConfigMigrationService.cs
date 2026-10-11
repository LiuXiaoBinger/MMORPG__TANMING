using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>编辑配置结构版本迁移与恢复服务；迁移前保存完整备份，保持原资产 GUID。</summary>
    internal static class ActionConfigMigrationService
    {
        /// <summary>当前包支持的配置结构版本号；旧配置只经显式带备份迁移升级。</summary>
        internal const int CurrentSchemaVersion = 1;

        /// <summary>判断非空配置的结构版本是否低于当前编辑器版本，不修改资源。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        internal static bool NeedsMigration(SkillConfigSO config)
        {
            return config != null && config.ActionEditorSchemaVersion < CurrentSchemaVersion;
        }

        /// <summary>先备份磁盘与未保存的内存状态，再更新原配置版本；失败时恢复已更改的原稿。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        /// <param name="backupPath">输出备份资产路径，供迁移失败后的恢复与人工核查。</param>
        /// <param name="error">输出本次操作失败原因；成功时为空，供窗口显示而非运行事件载荷。</param>
        internal static bool MigrateWithBackup(SkillConfigSO config, out string backupPath, out string error)
        {
            backupPath = string.Empty;
            error = string.Empty;
            if (config == null)
            {
                error = "No action config selected.";
                return false;
            }
            if (!NeedsMigration(config)) return true;

            // 原配置在 AssetDatabase 中的持久化路径，用于定点备份与失败重新导入。
            string sourcePath = AssetDatabase.GetAssetPath(config);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.StartsWith("Assets/"))
            {
                error = "Only project Assets can be migrated.";
                return false;
            }

            // 本次迁移是否已经改动原稿；只有 true 时失败分支才恢复原对象。
            bool changedOriginal = false;
            try
            {
            // 备份目录使用的本机年月日时分秒标记，避免覆盖不同批次备份。
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            // 本次配置迁移备份的 Assets 子目录，按时间批次隔离。
            string backupDirectory = $"Assets/ActionEditorBackups/{stamp}";
            EnsureAssetDirectory(backupDirectory);
            backupPath = AssetDatabase.GenerateUniqueAssetPath($"{backupDirectory}/{Path.GetFileName(sourcePath)}");
            if (!AssetDatabase.CopyAsset(sourcePath, backupPath))
            {
                error = $"Failed to back up {sourcePath}.";
                return false;
            }

            // CopyAsset 复制磁盘状态，可能遗漏窗口中尚未保存的编辑。
            // 改动原资产前把完整内存状态复制到备份并定点保存。
            // 已创建的备份资产，继续复制内存中未保存修改，保证恢复点完整。
            var savedBackup = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(backupPath);
            if (savedBackup == null) { error = "Backup could not be loaded."; return false; }
            EditorUtility.CopySerialized(config, savedBackup);
            EditorUtility.SetDirty(savedBackup);
            AssetDatabase.SaveAssetIfDirty(savedBackup);

                Undo.RecordObject(config, "Migrate Action Config");
                changedOriginal = true;
                config.SetActionEditorSchemaVersion(CurrentSchemaVersion);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
                return true;
            }
            catch /* 本次操作捕获的具体异常，记录失败原因；已变更原稿按恢复点回滚。 */ (Exception exception)
            {
                error = exception.Message;
                // 迁移失败时从备份路径重新加载的技能对象，用于恢复原稿。
                var backup = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(backupPath);
                if (changedOriginal && backup != null)
                {
                    EditorUtility.CopySerialized(backup, config);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssetIfDirty(config);
                }
                if (changedOriginal) AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceUpdate);
                return false;
            }
        }

        /// <summary>恢复指定历史备份前先保存当前配置作为恢复点，保留原资产名称和 GUID。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        /// <param name="backup">用户明确指定的历史备份资产，必须与待恢复的原配置不同。</param>
        /// <param name="recoveryPath">输出恢复前当前配置的备份路径，便于撤销本次恢复。</param>
        /// <param name="error">输出本次操作失败原因；成功时为空，供窗口显示而非运行事件载荷。</param>
        internal static bool RestoreWithBackup(SkillConfigSO config, SkillConfigSO backup,
            out string recoveryPath, out string error)
        {
            recoveryPath = string.Empty;
            error = string.Empty;
            // 原配置在 AssetDatabase 中的持久化路径，用于定点备份与失败重新导入。
            string sourcePath = AssetDatabase.GetAssetPath(config);
            // 用户所选历史备份的持久化路径，必须位于原备份目录范围。
            string backupPath = AssetDatabase.GetAssetPath(backup);
            if (config == null || backup == null || config == backup ||
                !sourcePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !backupPath.StartsWith("Assets/ActionEditorBackups/", StringComparison.Ordinal))
            {
                error = "Select a project action and a different asset from Assets/ActionEditorBackups.";
                return false;
            }
            if (backup.ActionEditorSchemaVersion > CurrentSchemaVersion)
            {
                error = "The backup requires a newer action editor.";
                return false;
            }
            // 提交前保存的配置完整副本；失败用于恢复，临时副本最终销毁。
            SkillConfigSO recovery = null;
            try
            {
                // 本次迁移或恢复使用的明确备份目录，不移动其他资产。
                string directory = $"Assets/ActionEditorBackups/{DateTime.Now:yyyyMMdd-HHmmss}";
                EnsureAssetDirectory(directory);
                recoveryPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{Path.GetFileNameWithoutExtension(sourcePath)}-before-restore.asset");
                recovery = ScriptableObject.CreateInstance<SkillConfigSO>();
                EditorUtility.CopySerialized(config, recovery);
                AssetDatabase.CreateAsset(recovery, recoveryPath);
                AssetDatabase.SaveAssetIfDirty(recovery);

                Undo.RegisterCompleteObjectUndo(config, "Restore Action Config");
                // 恢复前原资产名称，CopySerialized 后重新赋回以保持原稿显示身份。
                string name = config.name;
                EditorUtility.CopySerialized(backup, config);
                config.name = name;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
                return true;
            }
            catch /* 本次操作捕获的具体异常，记录失败原因；已变更原稿按恢复点回滚。 */ (Exception exception)
            {
                error = exception.Message;
                if (recovery != null && AssetDatabase.Contains(recovery))
                {
                    EditorUtility.CopySerialized(recovery, config);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssetIfDirty(config);
                }
                return false;
            }
        }

        /// <summary>逐级创建 Assets 内的合法资源目录，拒绝相对路径穿越。</summary>
        /// <param name="directory">Assets 内的目录路径，拒绝空段、非法文件名或相对路径穿越。</param>
        internal static void EnsureAssetDirectory(string directory)
        {
            if (!ActionEditorSettings.TryNormalizeAssetDirectory(directory, out string /* 检查后统一使用斜杠的 Assets 路径，拒绝遍历与非法段后才进入创建流程。 */ normalized))
                throw new ArgumentException("Directory must be inside Assets.", nameof(directory));
            if (AssetDatabase.IsValidFolder(normalized)) return;
            // 规范化资源目录按斜杠切分的路径段，用于逐级创建缺失文件夹。
            string[] parts = normalized.Split('/');
            // 已经检查或创建的父级 Assets 目录路径，逐段向目标目录推进。
            string current = parts[0];
            for (/* 当前目录段的零起始索引，从 1 开始跳过已存在的 Assets 根段。 */ int i = 1; i < parts.Length; i++)
            {
                // 当前待检查的下级目录路径，只在合法 Assets 层次中生成。
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
