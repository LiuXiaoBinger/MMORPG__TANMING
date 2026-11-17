using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>创建独立技能编辑草稿的资源菜单；生成的数据还需宿主绑定与导出校验才能用于本项目。</summary>
    public static class ActionAssetCreation
    {
        /// <summary>创建当前结构版本的技能草稿并交给 Project 窗口命名；不覆盖已有资产。</summary>
        [MenuItem("Assets/Create/Combat/Skill Config")]
        public static void Create()
        {
            // 新建的 SkillConfigSO 草稿，初始化显式计时后交给 Project 窗口命名和落盘。
            var config=ScriptableObject.CreateInstance<SkillConfigSO>();
            config.InitializeExplicitTiming();
            config.SetActionEditorSchemaVersion(ActionConfigMigrationService.CurrentSchemaVersion);
            ProjectWindowUtil.CreateAsset(config,"NewSkillConfig.asset");
        }
    }
}
