using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>保留 Y 编辑器的主线程自动化接口，用于显式资源查询、校验和带撤销的编辑；不驱动本项目角色战斗。</summary>
    public static partial class ActionEditorAgent
    {
        /// <summary>自动化 JSON 请求的强类型结构；资产路径与操作均由调用者显式提供。</summary>
        [Serializable]
        public sealed class Request
        {
            /// <summary>自动化请求或结果格式版本；1 支持基础操作，2 支持快照读取和摘要校验编辑。</summary>
            public int schemaVersion = 1;
            /// <summary>明确的自动化操作名，如 inventory、validate、create_action 或 edit_action。</summary>
            public string operation;
            /// <summary>本次显式处理的 Assets 资源路径；读改克隆操作要求恰好一项，校验要求非空。</summary>
            public string[] assetPaths;
            /// <summary>新建或克隆的 .asset 目标路径；已存在资产或元数据不允许覆盖。</summary>
            public string outputAssetPath;
            /// <summary>来源动画文件路径；模型内子剪辑需由 animationClipName 进一步指定。</summary>
            public string animationAssetPath;
            /// <summary>模型或动画文件内的精确剪辑名称，空值仅允许唯一有效剪辑。</summary>
            public string animationClipName;
            /// <summary>新草稿的用户显示名称；空白时使用目标文件名，不翻译该用户数据。</summary>
            public string displayName;
            /// <summary>新建配置的结束帧；-1 按剪辑推导，0 使用自动范围，正数显式指定。</summary>
            public int exitFrame = -1;
            /// <summary>调用者上次读取的 SHA256 摘要；与当前内容不符时拒绝编辑，防止覆盖新修改。</summary>
            public string expectedDigest;
            /// <summary>只编辑临时副本并返回差异，true 时不提交原资产或创建新文件。</summary>
            public bool dryRun;
            /// <summary>按请求顺序执行的强类型编辑列表；空值代表不修改字段。</summary>
            public Edit[] edits;
        }

        /// <summary>一条配置检查结果，保留资源、轨道、事件位置与稳定诊断码。</summary>
        [Serializable]
        public sealed class Issue
        {
            /// <summary>诊断所属资源路径；请求级错误可能没有资源路径。</summary>
            public string assetPath;
            /// <summary>验证器或自动化层的稳定诊断码，显示语言不改变其身份。</summary>
            public string code;
            /// <summary>原验证器严重程度的名称；Error 会使整个请求失败。</summary>
            public string severity;
            /// <summary>诊断所属原轨道标识，保留英文内部身份以供事件定位。</summary>
            public string track;
            /// <summary>诊断所属列表的零起始索引；-1 代表不关联单个事件。</summary>
            public int eventIndex;
            /// <summary>面向调用者的诊断原因，不参与配置或事件身份匹配。</summary>
            public string message;
        }

        /// <summary>自动化操作结果及资源清单；success 只表示本次编辑或校验成功，不代表游戏表现通过验收。</summary>
        [Serializable]
        public sealed class Result
        {
            /// <summary>自动化请求或结果格式版本；1 支持基础操作，2 支持快照读取和摘要校验编辑。</summary>
            public int schemaVersion = 1;
            /// <summary>本次请求是否没有 Error 诊断；不代表实际角色运行或画面已验证。</summary>
            public bool success;
            /// <summary>明确的自动化操作名，如 inventory、validate、create_action 或 edit_action。</summary>
            public string operation;
            /// <summary>执行请求时的 Unity 版本，供兼容性诊断使用。</summary>
            public string unityVersion;
            /// <summary>从实际加载程序集定位的嵌入包版本；无法定位时记录 unknown。</summary>
            public string packageVersion;
            /// <summary>实际新建或克隆资源的路径；只读操作保持空值。</summary>
            public string createdAssetPath;
            /// <summary>资源清单操作找到的技能编辑配置路径，按路径去重排序。</summary>
            public string[] actionAssets = Array.Empty<string>();
            /// <summary>资源清单操作找到的动画文件路径，不直接代表文件内某个子剪辑。</summary>
            public string[] animationAssets = Array.Empty<string>();
            /// <summary>本次操作累计的强类型诊断项，按产生顺序返回。</summary>
            public List<Issue> issues = new List<Issue>();
            /// <summary>当前返回字段快照的 SHA256 摘要，可作为后续修改的 expectedDigest。</summary>
            public string contentDigest;
            /// <summary>公开配置叶字段的快照列表，按 Unity 序列化迭代顺序保留。</summary>
            public List<Field> fields = new List<Field>();
            /// <summary>临时编辑前后的字段差异列表，dryRun 也会返回。</summary>
            public List<Change> changes = new List<Change>();
        }

        /// <summary>在 Unity 主线程分派显式编辑请求，把预期失败收集为强类型诊断结果。</summary>
        /// <param name="request">强类型自动化请求；所有资源与操作范围由调用者显式指定。</param>
        public static Result Execute(Request request)
        {
            // 当前自动化调用的结果容器，收集资源路径、版本和诊断后返回给调用方。
            var result = NewResult(request?.operation);
            try
            {
                if (request == null || (request.schemaVersion != 1 && request.schemaVersion != 2))
                    throw new ArgumentException("Request schemaVersion must be 1 or 2.");
                result.schemaVersion = request.schemaVersion;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Exit Play Mode before running authoring operations.");
                switch (request.operation)
                {
                    case "inventory":
                        result.actionAssets = FindAssets("t:SkillConfigSO");
                        result.animationAssets = FindAssets("t:AnimationClip");
                        break;
                    case "validate":
                        if (request.assetPaths == null || request.assetPaths.Length == 0)
                            throw new ArgumentException("validate requires explicit assetPaths; zero checked assets is not a pass.");
                        // 本次显式请求中的单个技能资产路径；缺失资产只追加诊断，不扩大搜索范围。
                        foreach (string path in request.assetPaths)
                        {
                            // 当前显式资源路径加载或创建的技能编辑对象，不扩展修改其他配置。
                            var config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
                            if (config == null) AddError(result, "AGENT_ASSET_MISSING", "Action asset not found.", path);
                            else AddIssues(result, path, ActionConfigValidator.Validate(config));
                        }
                        break;
                    case "create_action":
                        CreateAction(request, result);
                        break;
                    case "read_action": case "clone_action": case "edit_action":
                        if (request.schemaVersion != 2) throw new ArgumentException("These operations require schemaVersion 2.");
                        EditAction(request, result);
                        break;
                    default:
                        throw new ArgumentException("Supported operations: inventory, validate, create_action.");
                }
                result.success = result.issues.All(/* 待检查的单条结果诊断，只按严重程度判定当前请求是否失败。 */ i => i.severity != "Error");
            }
            catch /* 本次请求或结果写入的异常，仅报告具体失败原因，不将异常对象传入游戏业务。 */ (Exception exception)
            {
                AddError(result, "AGENT_REQUEST_FAILED", exception.Message);
            }
            return result;
        }

        /// <summary>只读检查原 Y 播放器、动画适配与同对象事件处理器，不执行游戏播放或生成角色。</summary>
        /// <param name="actor">只读检查的原 Y 独立播放对象；不会添加组件或执行角色战斗。</param>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        public static Result InspectActor(GameObject actor, SkillConfigSO config)
        {
            // 当前自动化调用的结果容器，收集资源路径、版本和诊断后返回给调用方。
            var result = NewResult("inspect_actor");
            if (actor == null || config == null)
            {
                AddError(result, "AGENT_ACTOR_INPUT", "Provide an actor and an action config.");
                return result;
            }
            // 检查对象已有的原 Y ActionPlayer 组件，不添加或启动该播放器。
            var player = actor.GetComponent<ActionPlayer>();
            if (player == null) AddError(result, "AGENT_PLAYER_MISSING", "Add ActionPlayer on the actor.");
            // 检查对象上启用且有效的 MonoBehaviour 快照，仅这些组件参与接口发现。
            var behaviours = actor.GetComponents<MonoBehaviour>().Where(/* 同对象组件查询的候选原引用；仅保留非空且活动的组件。 */ x => x != null && x.isActiveAndEnabled).ToArray();
            // 检查对象上第一个实现 IActionAnimator 的活动适配器，用于只读动画能力检查。
            var animator = behaviours.OfType<IActionAnimator>().FirstOrDefault();
            // 通过反射发现的事件处理类型集合，仅位于工具预检查边界，不传递弱类型领域对象。
            var types = new HashSet<Type>();
            // 当前被检查的活动组件原引用，不创建组件副本或修改其启用状态。
            foreach (var component in behaviours)
                // 当前组件声明的接口类型；反射边界只用于识别 IActionEventHandler 的事件类型。
                foreach (var contract in component.GetType().GetInterfaces())
                    if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IActionEventHandler<>))
                        types.Add(contract.GetGenericArguments()[0]);
            // 已有的原 Y CharacterController 位移桥，仅用于声明内置移动能力。
            var bridge = actor.GetComponent<ActionCharacterControllerDisplacement>();
            // 被检查对象已有的 CharacterController，活动状态决定内置位移是否可用。
            var controller = actor.GetComponent<CharacterController>();
            // 原 Y 位移桥与控制器均活动时才视为支持 MoveSegment，不推断本项目运动执行。
            bool builtInMove = bridge != null && bridge.isActiveAndEnabled && controller != null && controller.enabled;
            AddIssues(result, AssetDatabase.GetAssetPath(config), ActionConfigValidator.Validate(config,
                new ActionValidationCapabilities {
                    HasAnimator = animator != null,
                    RequireEventHandlers = true,
                    CanHandleEvent = /* 待验证事件的声明类型；反射预检边界按已发现处理接口或内置位移桥判断支持。 */ type => types.Contains(type) ||
                        (type == typeof(Global.MoveSegment) && builtInMove)
                }));
            if (player != null && !player.isActiveAndEnabled)
                AddError(result, "AGENT_PLAYER_DISABLED", "Enable the actor and ActionPlayer before playback.");
            if (animator != null)
                // 当前原动画片段引用，只读询问已有动画适配器是否可播放。
                foreach (var segment in config.GetEffectiveSegments())
                    if (segment != null && !animator.CanPlay(segment))
                        AddError(result, "AGENT_ANIMATION_UNSUPPORTED", "The attached animation adapter cannot play a segment.");
            result.success = result.issues.All(/* 只读预检查的单条诊断，Error 会令预检查结果失败。 */ i => i.severity != "Error");
            return result;
        }

        /// <summary>从明确的动画资源创建一个新单段草稿；校验失败时销毁临时配置，不覆盖旧文件。</summary>
        /// <param name="request">强类型自动化请求；所有资源与操作范围由调用者显式指定。</param>
        /// <param name="result">本次调用累计的强类型结果，只在编辑器请求生命周期内使用。</param>
        static void CreateAction(Request request, Result result)
        {
            // 调用者请求的新草稿目标路径，须经过 Assets 范围及文件存在性校验。
            string path = request.outputAssetPath;
            if (!ActionEditorSettings.TryNormalizeAssetDirectory(path, out var /* 检查后统一使用斜杠的 Assets 路径，拒绝遍历与非法段后才进入创建流程。 */ normalized) ||
                !normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("outputAssetPath must be a new .asset file below Assets without path traversal.");
            if (File.Exists(normalized) || File.Exists(normalized + ".meta") || Directory.Exists(normalized) ||
                AssetDatabase.LoadMainAssetAtPath(normalized) != null)
                throw new IOException("Destination exists. Choose a new action path; existing assets are never overwritten.");
            // 指定文件内的真实动画剪辑快照，过滤 Unity 自动生成的预览剪辑。
            var clips = AssetDatabase.LoadAllAssetsAtPath(request.animationAssetPath ?? string.Empty)
                .OfType<AnimationClip>().Where(/* 指定动画文件内的候选剪辑原引用，按预览名称过滤或精确剪辑名称匹配。 */ c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            // 调用者指定的唯一来源剪辑；存在多剪辑歧义或名称不匹配时为 null。
            var clip = string.IsNullOrEmpty(request.animationClipName)
                ? (clips.Length == 1 ? clips[0] : null)
                : clips.SingleOrDefault(/* 指定动画文件内的候选剪辑原引用，按预览名称过滤或精确剪辑名称匹配。 */ c => c.name == request.animationClipName);
            if (clip == null || clip.legacy || clip.length <= 0)
                throw new ArgumentException("Provide a non-Legacy, non-empty clip. For a model containing multiple clips, specify animationClipName.");
            if (request.exitFrame < -1) throw new ArgumentException("exitFrame must be -1 (derive), 0, or positive.");
            // 当前显式资源路径加载或创建的技能编辑对象，不扩展修改其他配置。
            var config = ScriptableObject.CreateInstance<SkillConfigSO>();
            // 新配置是否已写为正式资产；false 时 finally 销毁临时 ScriptableObject。
            bool persisted = false;
            try
            {
                config.SetActionEditorSchemaVersion(ActionConfigMigrationService.CurrentSchemaVersion);
                config.InitializeExplicitTiming();
                config.skillName = string.IsNullOrWhiteSpace(request.displayName) ? Path.GetFileNameWithoutExtension(normalized) : request.displayName;
                config.animSegments.Add(new Global.AnimClipSegment { clip = clip, startFrame = 0 });
                config.exitFrame = request.exitFrame == -1 ? Math.Max(1, ActionTiming.Duration(config, config.animSegments[0])) : request.exitFrame;
                AddIssues(result, normalized, ActionConfigValidator.Validate(config));
                if (result.issues.Any(/* 新草稿配置验证的单条诊断，有 Error 时不会创建持久化资源。 */ i => i.severity == "Error")) return;
                ActionConfigMigrationService.EnsureAssetDirectory(Path.GetDirectoryName(normalized).Replace('\\', '/'));
                AssetDatabase.CreateAsset(config, normalized);
                persisted = true;
                AssetDatabase.SaveAssetIfDirty(config);
                result.createdAssetPath = normalized;
            }
            finally { if (!persisted) UnityEngine.Object.DestroyImmediate(config); }
        }

        /// <summary>只查询 Assets 下符合 Unity 过滤器的资源，按路径去重排序后返回。</summary>
        /// <param name="filter">Unity AssetDatabase 类型或名称过滤表达式，只在 Assets 范围查询。</param>
        static string[] FindAssets(string filter) => AssetDatabase.FindAssets(filter, new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(/* 待排序的规范资源路径，按 Ordinal 顺序返回稳定清单。 */ p => p, StringComparer.Ordinal).ToArray();

        /// <summary>记录本次操作与实际 Unity 版本，通过保留的 Runtime 程序集获取嵌入包版本。</summary>
        /// <param name="operation">当前自动化请求的稳定操作标识，写入结果上下文便于调用者匹配。</param>
        static Result NewResult(string operation)
        {
            // Runtime 继续位于嵌入包中；编辑器移入 Assets 后通过运行类型读取包版本。
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SkillConfigSO).Assembly);
            return new Result { operation = operation, unityVersion = Application.unityVersion,
                packageVersion = package == null ? "unknown" : package.version };
        }

        /// <summary>将原验证器的事件定位与严重程度复制到自动化结果，不修改验证对象。</summary>
        /// <param name="result">本次调用累计的强类型结果，只在编辑器请求生命周期内使用。</param>
        /// <param name="path">本条诊断所属的明确资源路径；请求级错误可为空，不用于扩大资源查找。</param>
        /// <param name="issues">原配置验证器产生的诊断序列，将复制轨道与事件定位。</param>
        static void AddIssues(Result result, string path, IEnumerable<ActionValidationIssue> issues)
        {
            // 本次复制的原验证诊断，保留代码、严重程度、轨道与零起始事件索引。
            foreach (var issue in issues)
                result.issues.Add(new Issue { assetPath = path, code = issue.Code, severity = issue.Severity.ToString(),
                    track = issue.Track, eventIndex = issue.EventIndex, message = issue.Message });
        }

        /// <summary>追加一条稳定错误码诊断并设置失败状态；无事件定位时索引为 -1。</summary>
        /// <param name="result">本次调用累计的强类型结果，只在编辑器请求生命周期内使用。</param>
        /// <param name="code">本次失败的稳定错误码，供工具调用方判断失败种类。</param>
        /// <param name="message">具体失败原因，仅用于返回诊断，不写入技能配置。</param>
        /// <param name="path">本条诊断所属的明确资源路径；请求级错误可为空，不用于扩大资源查找。</param>
        static void AddError(Result result, string code, string message, string path = null)
        {
            result.success = false;
            result.issues.Add(new Issue { code = code, severity = "Error", message = message, assetPath = path, eventIndex = -1 });
        }

        /// <summary>保留原 Y 批处理入口；先独占新结果文件再执行请求，避免重复执行产生未报告的编辑。</summary>
        public static void RunFromCommandLine()
        {
            // 当前自动化调用的结果容器，收集资源路径、版本和诊断后返回给调用方。
            Result result;
            // 批处理结果的绝对文件路径，必须为原 Y 日志目录下尚不存在的新文件。
            string output = null;
            try
            {
                // 本次进程命令行参数快照，仅读取调用者显式给出的请求与输出路径。
                var args = Environment.GetCommandLineArgs();
                // 显式命令行请求 JSON 路径，不从场景或网络寻找额外指令。
                string requestPath = Argument(args, "-actionEditorRequest");
                output = Path.GetFullPath(Argument(args, "-actionEditorResult"));
                // 原 Y 批处理结果允许目录的绝对前缀，末尾包含分隔符以排除相似目录。
                string allowed = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ActionEditorAgent")) + Path.DirectorySeparatorChar;
                if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || File.Exists(output))
                    throw new ArgumentException("Result must be a NEW file under this project's Logs/ActionEditorAgent/.");
                // 修改资产前先独占结果文件，避免重复执行创建没有报告的草稿。
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (/* 以 CreateNew 独占创建的结果文件流，先占位再编辑资产，using 结束自动关闭。 */ var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                using (/* 将强类型结果序列化为 JSON 的文本写入器，using 结束刷盘并关闭。 */ var writer = new StreamWriter(stream))
                {
                    try { result = Execute(JsonUtility.FromJson<Request>(File.ReadAllText(requestPath))); }
                    catch /* 本次请求或结果写入的异常，仅报告具体失败原因，不将异常对象传入游戏业务。 */ (Exception exception) { result = NewResult(null); AddError(result, "AGENT_REQUEST_FAILED", exception.Message); }
                    writer.Write(JsonUtility.ToJson(result, true));
                }
            }
            catch /* 本次请求或结果写入的异常，仅报告具体失败原因，不将异常对象传入游戏业务。 */ (Exception exception)
            {
                Debug.LogError("[ACT Agent] " + exception.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            if (Application.isBatchMode) EditorApplication.Exit(result.success ? 0 : 2);
        }

        /// <summary>读取指定命令行开关后的值；开关缺失或末尾无值时报告错误。</summary>
        /// <param name="args">进程命令行参数数组，开关后的下一项才是请求路径或结果路径。</param>
        /// <param name="key">需要查找的命令行开关，如 -actionEditorRequest。</param>
        static string Argument(string[] args, string key)
        {
            // 指定命令行开关在参数数组中的索引；负数或无下一项时抛出缺参错误。
            int index = Array.IndexOf(args, key);
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing argument " + key);
            return args[index + 1];
        }
    }
}
