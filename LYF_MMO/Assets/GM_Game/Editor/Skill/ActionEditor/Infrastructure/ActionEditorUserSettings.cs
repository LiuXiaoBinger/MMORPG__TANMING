using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>当前用户的预览模型绑定；保存到 UserSettings，避免改写技能资产的模型引用。</summary>
    [FilePath("UserSettings/ActionEditorUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ActionEditorUserSettings : ScriptableSingleton<ActionEditorUserSettings>
    {
        /// <summary>一项技能资产 GUID 与预览对象全局 ID 的用户偏好关联，不保存临时副本。</summary>
        [Serializable]
        sealed class PreviewBinding
        {
            /// <summary>持久化技能配置 GUID，作为个人预览模型绑定的稳定查询键。</summary>
            public string configGuid;
            /// <summary>源预览对象的 Unity GlobalObjectId 字符串；空值表示解除绑定，不保存临时副本。</summary>
            public string objectId;
        }

        /// <summary>用户已设置的技能到预览源对象关联列表，域重载后从 UserSettings 恢复。</summary>
        [SerializeField] List<PreviewBinding> previewBindings = new List<PreviewBinding>();

        /// <summary>按技能 GUID 查找用户预览绑定并解析对象 ID，失效引用返回空。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        internal GameObject GetPreviewModel(SkillConfigSO config)
        {
            // 当前技能持久化资产 GUID，用于只读查询用户的预览模型关联。
            string guid = GetGuid(config);
            if (string.IsNullOrEmpty(guid)) return null;
            // 当前技能 GUID 对应的个人预览关联项，更新此引用会保留列表中的原对象。
            var binding = previewBindings.Find(/* 个人绑定列表中的原关联项，以配置 GUID 匹配，不复制或更新该项。 */ x => x.configGuid == guid);
            if (binding == null || string.IsNullOrEmpty(binding.objectId)) return null;
            if (!GlobalObjectId.TryParse(binding.objectId, out var /* 解析后的 Unity 全局对象身份，定位源预览模型，不定位临时克隆。 */ objectId)) return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectId) as GameObject;
        }

        /// <summary>新增或更新当前技能的个人预览绑定，空模型解除关联；不标脏技能配置。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        /// <param name="model">源预览模型；空值解除个人绑定，不生成场景角色。</param>
        internal void SetPreviewModel(SkillConfigSO config, GameObject model)
        {
            // 当前技能持久化资产 GUID，用于更新个人预览绑定，不改动技能资产内容。
            string guid = GetGuid(config);
            if (string.IsNullOrEmpty(guid)) return;
            // 当前技能 GUID 对应的个人预览关联项，更新此引用会保留列表中的原对象。
            var binding = previewBindings.Find(/* 个人绑定列表中的原关联项，命中后直接更新原对象的模型全局 ID。 */ x => x.configGuid == guid);
            if (binding == null)
            {
                binding = new PreviewBinding { configGuid = guid };
                previewBindings.Add(binding);
            }
            binding.objectId = model == null ? string.Empty : GlobalObjectId.GetGlobalObjectIdSlow(model).ToString();
            Save(true);
        }

        /// <summary>取得已持久化技能配置的资源 GUID；未保存或空配置返回空字符串。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        static string GetGuid(SkillConfigSO config)
        {
            if (config == null) return string.Empty;
            // 当前技能在 AssetDatabase 中的路径；无路径表示临时或尚未保存的对象。
            string path = AssetDatabase.GetAssetPath(config);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }
    }
}
