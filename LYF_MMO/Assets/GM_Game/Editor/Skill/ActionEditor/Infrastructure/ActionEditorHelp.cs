using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>定位嵌入包文档并打开本机手册的编辑器菜单，不创建配置或场景对象。</summary>
    public static class ActionEditorHelp
    {
        /// <summary>打开包内中文使用手册，不依赖网络地址。</summary>
        [MenuItem("Tools/ACT Action Editor/Help/User Guide (Chinese)")]
        public static void OpenUserGuide() => Open("Documentation~/UserGuide.zh-CN.md");

        /// <summary>打开原 Y 的入门教程，介绍通用包功能而非本项目运行支持范围。</summary>
        [MenuItem("Tools/ACT Action Editor/Help/Getting Started")]
        public static void OpenTutorial() => Open("Documentation~/index.html");

        /// <summary>打开原 Y 自动化接口说明，不自动执行指南中的操作。</summary>
        [MenuItem("Tools/ACT Action Editor/Help/Agent Guide")]
        public static void OpenAgentGuide() => Open("Documentation~/AgentGuide.md");

        /// <summary>从保留的 Runtime 程序集定位嵌入包目录，检查文档存在后打开本机文件 URI。</summary>
        /// <param name="relativePath">相对于实际包根目录的文档路径，不接受远程下载内容。</param>
        static void Open(string relativePath)
        {
            // Runtime 继续位于嵌入包中；编辑器移入 Assets 后通过运行类型定位包文档。
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SkillConfigSO).Assembly);
            if (package == null) { Debug.LogError("ACT package location could not be resolved."); return; }
            // 包描述中的实际目录加相对文档路径，存在性检查后才交给系统打开。
            string path = Path.Combine(package.resolvedPath, relativePath);
            if (!File.Exists(path)) { Debug.LogError("ACT documentation is missing: " + relativePath); return; }
            Application.OpenURL(new Uri(Path.GetFullPath(path)).AbsoluteUri);
        }
    }
}
