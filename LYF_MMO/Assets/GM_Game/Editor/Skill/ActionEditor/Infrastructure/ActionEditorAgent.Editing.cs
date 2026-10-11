using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ethan.ActionEditor.Editor
{
    /// <summary>保留 Y 编辑器的主线程自动化接口，用于显式资源查询、校验和带撤销的编辑；不驱动本项目角色战斗。</summary>
    public static partial class ActionEditorAgent
    {
        /// <summary>Unity 序列化叶字段的强类型传输描述；按 kind 选择有效值槽，引用以 GUID 与局部文件 ID 表达。</summary>
        [Serializable] public sealed class Field
        {
            /// <summary>path 为真实序列化路径；kind 为 Unity 属性类型；text 存字符串或枚举名；referenceGuid 为持久化资源 GUID。</summary>
            public string path, kind, text, referenceGuid;
            /// <summary>integer 保留整数字段或枚举值的 64 位容器；referenceLocalId 区分同资源文件内的子资产。</summary>
            public long integer, referenceLocalId;
            /// <summary>浮点字段快照的双精度容器；编辑输入必须为有限数。</summary>
            public double number;
            /// <summary>布尔叶字段的实际值，不用整型或字符串代替。</summary>
            public bool boolean;
            /// <summary>向量、颜色及四元数的四分量容器，按属性类型选择有效分量。</summary>
            public Vector4 vector;
            /// <summary>Unity 动画曲线叶字段的完整快照，保留关键点和切线。</summary>
            public AnimationCurve curve;
        }
        /// <summary>一条显式字段设置或根轨道列表结构编辑；不会把任意反射对象传入业务接口。</summary>
        [Serializable] public sealed class Edit
        {
            /// <summary>叶字段设置为 set；根列表结构编辑支持 insert、remove 或 move，按请求顺序执行。</summary>
            public string operation = "set";
            /// <summary>需要修改的真实序列化路径；根列表操作不允许含点号的嵌套路径。</summary>
            public string path;
            /// <summary>index 是根列表的零起始源索引；destinationIndex 是 move 操作移除后插入的目标索引。</summary>
            public int index, destinationIndex;
            /// <summary>set 操作使用的强类型叶字段值，kind 必须与目标 Unity 属性类型相同。</summary>
            public Field value;
        }
        /// <summary>单个序列化路径编辑前后的 JSON 摘要，用于报告草稿与原稿差异。</summary>
        [Serializable] public sealed class Change { /* path 为差异字段路径；before 与 after 分别为该路径修改前后的字段 JSON，可为空表示新增或移除。 */ public string path, before, after; }
        /// <summary>为字段快照生成稳定 JSON 的包装结构，仅在编辑器序列化摘要边界使用。</summary>
        [Serializable] sealed class FieldSet { /* 公开配置叶字段的快照列表，按 Unity 序列化迭代顺序保留。 */ public List<Field> fields; }

        // 只允许公开配置字段；Unity 内部字段和计时迁移字段不属于普通自动化编辑范围。
        /// <summary>限制自动化只能访问配置公开字段；写入时排除计时版本和帧率迁移字段。</summary>
        /// <param name="path">待访问的真实序列化字段路径；首段必须是 SkillConfigSO 公开字段。</param>
        /// <param name="writing">true 时采用写入白名单，禁止借叶字段编辑改变计时版本或帧率。</param>
        static bool Allowed(string path, bool writing = false)
        {
            if (string.IsNullOrEmpty(path)) return false;
            // 序列化路径的首字段名，反射只允许 SkillConfigSO 声明的公开字段。
            string root = path.Split('.')[0];
            return typeof(SkillConfigSO).GetField(root) != null &&
                (!writing || (root != "timingVersion" && root != "timelineFrameRate"));
        }

        /// <summary>通过 SerializedObject 读取公开配置的叶字段快照；资产引用转为持久化身份，避免弱类型载荷。</summary>
        /// <param name="config">明确指定的技能编辑资产，迁移或编辑只针对该对象，不扫描其他配置。</param>
        static List<Field> ReadFields(SkillConfigSO config)
        {
            // 按序列化游标顺序累计的叶字段快照，不携带原 Unity 对象实例。
            var fields = new List<Field>();
            using (/* 只在当前 using 作用域持有的 Unity 序列化访问器，操作对象为明确配置或副本。 */ var serialized = new SerializedObject(config))
            {
                // 当前 Unity 叶字段游标，类型决定使用哪个强类型值槽。
                var p = serialized.GetIterator();
                // 是否进入当前 Generic 子属性；不允许字段的子树会被整段跳过。
                bool descend = true;
                while (p.Next(descend))
                {
                    descend = p.propertyType == SerializedPropertyType.Generic;
                    if (!Allowed(p.propertyPath)) { descend = false; continue; }
                    if (p.propertyType == SerializedPropertyType.Generic) continue;
                    // 当前叶字段的强类型快照，保存原路径、类型与有效值，不向业务层透传 object。
                    var f = new Field { path = p.propertyPath, kind = p.propertyType.ToString() };
                    switch (p.propertyType)
                    {
                        case SerializedPropertyType.Integer: case SerializedPropertyType.ArraySize: f.integer = p.longValue; break;
                        case SerializedPropertyType.Enum: f.integer = p.intValue; f.text = p.enumValueIndex >= 0 ? p.enumNames[p.enumValueIndex] : null; break;
                        case SerializedPropertyType.Float: f.number = p.doubleValue; break;
                        case SerializedPropertyType.Boolean: f.boolean = p.boolValue; break;
                        case SerializedPropertyType.String: f.text = p.stringValue; break;
                        case SerializedPropertyType.Vector2: f.vector = p.vector2Value; break;
                        case SerializedPropertyType.Vector3: f.vector = p.vector3Value; break;
                        case SerializedPropertyType.Vector4: f.vector = p.vector4Value; break;
                        case SerializedPropertyType.Quaternion: /* 从 Unity 字段读取的原四元数，按 x、y、z、w 分量保存，避免改用欧拉角。 */ var q = p.quaternionValue; f.vector = new Vector4(q.x,q.y,q.z,q.w); break;
                        case SerializedPropertyType.Color: f.vector = p.colorValue; break;
                        case SerializedPropertyType.AnimationCurve: f.curve = p.animationCurveValue; break;
                        case SerializedPropertyType.ObjectReference:
                            if (p.objectReferenceValue != null && !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(p.objectReferenceValue, out f.referenceGuid, out f.referenceLocalId))
                                throw new ArgumentException("Only persistent asset references are supported: " + p.propertyPath);
                            break;
                        default: f.text = p.type; break;
                    }
                    fields.Add(f);
                }
            }
            return fields;
        }
        /// <summary>对字段快照的 UTF-8 JSON 计算 SHA256 十六进制摘要，用于拒绝过期编辑请求。</summary>
        /// <param name="fields">按 Unity 原顺序读取的叶字段快照列表，用于稳定 JSON 和内容摘要。</param>
        static string Digest(List<Field> fields)
        {
            using (/* 本次快照摘要使用的 SHA256 实例，using 结束释放加密资源。 */ var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new FieldSet { fields = fields })))).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>在临时副本上修改并校验；摘要一致且非 dryRun 时才提交，异常撤销并恢复原稿。</summary>
        /// <param name="request">强类型自动化请求；所有资源与操作范围由调用者显式指定。</param>
        /// <param name="result">本次调用累计的强类型结果，只在编辑器请求生命周期内使用。</param>
        static void EditAction(Request request, Result result)
        {
            if (request.assetPaths == null || request.assetPaths.Length != 1) throw new ArgumentException("Provide exactly one source assetPath.");
            // 此次读改克隆操作唯一的源 .asset 路径，必须是规范 Assets 内路径。
            string path = request.assetPaths[0];
            if (!ActionEditorSettings.TryNormalizeAssetDirectory(path, out var /* 检查后统一使用斜杠的 Assets 路径，拒绝遍历与非法段后才进入创建流程。 */ normalized) || normalized != path || !path.EndsWith(".asset", StringComparison.Ordinal))
                throw new ArgumentException("Source must be a canonical .asset path below Assets.");
            // 调用者明确指定的原技能配置资产，编辑先克隆，不直接写入未校验值。
            var source = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
            if (source == null) throw new ArgumentException("Action asset not found.");
            result.fields = ReadFields(source);
            result.contentDigest = Digest(result.fields);
            if (request.operation == "read_action") { AddIssues(result,path,ActionConfigValidator.Validate(source)); return; }
            if (request.expectedDigest != result.contentDigest) throw new InvalidOperationException("Stale or missing expectedDigest; read the action again.");
            if (request.operation == "clone_action")
            {
                if (!ActionEditorSettings.TryNormalizeAssetDirectory(request.outputAssetPath, out var /* 克隆操作的合法新 .asset 路径，原文件及其 .meta 必须都不存在。 */ destination) || !destination.EndsWith(".asset", StringComparison.Ordinal))
                    throw new ArgumentException("Destination must be a new .asset path below Assets.");
                if (File.Exists(destination) || File.Exists(destination + ".meta") || Directory.Exists(destination) || AssetDatabase.LoadMainAssetAtPath(destination) != null)
                    throw new IOException("Destination exists.");
                normalized = destination;
            }
            // 完整配置临时副本，所有字段修改和验证在提交前执行，未持久化时最终销毁。
            var draft = Object.Instantiate(source);
            draft.name = source.name;
            try
            {
                // 本次按顺序执行的强类型编辑指令，仅作用于临时技能副本。
                foreach (var edit in request.edits ?? Array.Empty<Edit>()) ApplyEdit(draft, edit);
                // 编辑后按相同序列化顺序读取的字段快照，校验通过后才更新返回摘要。
                var after = ReadFields(draft);
                // 编辑前字段路径到字段 JSON 的查找表，仅用于构建准确的差异报告。
                var beforeByPath = result.fields.ToDictionary(/* 当前快照的单个强类型叶字段，分别读取路径或序列化为差异 JSON。 */ f => f.path, /* 当前快照的单个强类型叶字段，分别读取路径或序列化为差异 JSON。 */ f => JsonUtility.ToJson(f));
                // 编辑后字段路径到字段 JSON 的查找表，与原快照按路径并集合比较。
                var afterByPath = after.ToDictionary(/* 当前快照的单个强类型叶字段，分别读取路径或序列化为差异 JSON。 */ f => f.path, /* 当前快照的单个强类型叶字段，分别读取路径或序列化为差异 JSON。 */ f => JsonUtility.ToJson(f));
                // 本轮比较的序列化路径，来自编辑前后字段集合的去重排序并集。
                foreach (var key in beforeByPath.Keys.Union(afterByPath.Keys).OrderBy(/* 编辑前后并集中的序列化路径，按 Ordinal 排序保证差异顺序稳定。 */ k => k, StringComparer.Ordinal))
                {
                    beforeByPath.TryGetValue(key, out var /* 当前路径编辑前的字段 JSON，路径不存在时为空以表示新增。 */ beforeValue); afterByPath.TryGetValue(key, out var /* 当前路径编辑后的字段 JSON，路径不存在时为空以表示移除。 */ afterValue);
                    if (beforeValue != afterValue) result.changes.Add(new Change { path = key, before = beforeValue, after = afterValue });
                }
                AddIssues(result, path, ActionConfigValidator.Validate(draft));
                if (result.issues.Any(/* 临时编辑副本的单条验证诊断，Error 会阻止修改原稿。 */ i => i.severity == "Error")) return;
                result.fields = after; result.contentDigest = Digest(after);
                if (request.dryRun) return;
                Undo.IncrementCurrentGroup(); /* 本次原子自动化提交的 Undo 分组号；异常仅撤销该组及其内部修改。 */ int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Agent Edit Action");
                // 提交前保存的配置完整副本；失败用于恢复，临时副本最终销毁。
                var recovery = Object.Instantiate(source); recovery.name = source.name;
                try
                {
                    if (request.operation == "clone_action")
                    {
                        ActionConfigMigrationService.EnsureAssetDirectory(Path.GetDirectoryName(normalized).Replace('\\','/'));
                        AssetDatabase.CreateAsset(draft, normalized);
                        Undo.RegisterCreatedObjectUndo(draft, "Agent Clone Action");
                        AssetDatabase.SaveAssetIfDirty(draft);
                        result.createdAssetPath = normalized;
                        draft = null;
                    }
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(source, "Agent Edit Action");
                        EditorUtility.CopySerialized(draft, source);
                        EditorUtility.SetDirty(source); AssetDatabase.SaveAssetIfDirty(source);
                    }
                    Undo.CollapseUndoOperations(group);
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group);
                    if (request.operation == "edit_action") { EditorUtility.CopySerialized(recovery, source); EditorUtility.SetDirty(source); AssetDatabase.SaveAssetIfDirty(source); }
                    else if (AssetDatabase.Contains(draft)) AssetDatabase.DeleteAsset(normalized);
                    throw;
                }
                finally { Object.DestroyImmediate(recovery); }
            }
            finally { if (draft != null && !AssetDatabase.Contains(draft)) Object.DestroyImmediate(draft); }
        }

        /// <summary>执行一条受限编辑；结构列表操作留在反射适配边界，叶字段使用 Unity 序列化类型校验。</summary>
        /// <param name="draft">尚未提交的技能副本，所有字段编辑与校验先在该副本执行。</param>
        /// <param name="edit">单个强类型 set、insert、remove 或 move 操作，索引与值须通过校验。</param>
        static void ApplyEdit(SkillConfigSO draft, Edit edit)
        {
            if (edit == null || !Allowed(edit.path, true)) throw new ArgumentException("Unsupported or protected field path.");
            if (edit.operation != "set")
            {
                if (edit.path.Contains(".")) throw new ArgumentException("Structural edits require a root track list.");
                // 反射获取的公开根列表声明；只在序列化适配边界访问，不作为领域参数透传。
                var field = typeof(SkillConfigSO).GetField(edit.path);
                if (!(field.GetValue(draft) is IList /* 反射取得的原根轨道 IList 视图，仅用于通用结构插入、删除、移动的序列化适配边界。 */ list) || !field.FieldType.IsGenericType) throw new ArgumentException("Not a track list.");
                if (edit.index < 0 || edit.index > list.Count) throw new ArgumentOutOfRangeException("index");
                if (edit.operation == "insert") list.Insert(edit.index, Activator.CreateInstance(field.FieldType.GetGenericArguments()[0]));
                else if (edit.operation == "remove" && edit.index < list.Count) list.RemoveAt(edit.index);
                else if (edit.operation == "move" && edit.index < list.Count && edit.destinationIndex >= 0 && edit.destinationIndex < list.Count)
                { /* 从原轨道列表取出的元素引用；反射/序列化边界暂存后在目标索引插回，不作为业务载荷。 */ var item = list[edit.index]; list.RemoveAt(edit.index); list.Insert(edit.destinationIndex,item); }
                else throw new ArgumentException("Invalid structural edit.");
                return;
            }
            using (/* 只在当前 using 作用域持有的 Unity 序列化访问器，操作对象为明确配置或副本。 */ var serialized = new SerializedObject(draft))
            {
                // 当前 Unity 叶字段游标，类型决定使用哪个强类型值槽。
                var p = serialized.FindProperty(edit.path);
                // 调用者传入的强类型叶字段值，kind 与目标属性相符且数值有限才允许写入。
                var v = edit.value;
                if (p == null || v == null || p.propertyType.ToString() != v.kind) throw new ArgumentException("Field is missing or value kind mismatches.");
                if (double.IsNaN(v.number) || double.IsInfinity(v.number)) throw new ArgumentException("Numbers must be finite.");
                for(/* 待校验向量的分量索引，范围 0 至 3，拒绝 NaN 或无穷大。 */ int component=0;component<4;component++)
                    if(float.IsNaN(v.vector[component]) || float.IsInfinity(v.vector[component])) throw new ArgumentException("Vector components must be finite.");
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if(p.type == "int" && (v.integer < int.MinValue || v.integer > int.MaxValue)) throw new ArgumentException("Integer is outside the field range.");
                        p.longValue = v.integer; break;
                    case SerializedPropertyType.Enum:
                        if (!p.enumNames.Contains(v.text)) throw new ArgumentException("Set enum using its exact name in value.text.");
                        p.enumValueIndex = Array.IndexOf(p.enumNames,v.text); break;
                    case SerializedPropertyType.Float: p.doubleValue = v.number; break;
                    case SerializedPropertyType.Boolean: p.boolValue = v.boolean; break;
                    case SerializedPropertyType.String: p.stringValue = v.text ?? ""; break;
                    case SerializedPropertyType.Vector2: p.vector2Value = v.vector; break;
                    case SerializedPropertyType.Vector3: p.vector3Value = v.vector; break;
                    case SerializedPropertyType.Vector4: p.vector4Value = v.vector; break;
                    case SerializedPropertyType.Color: p.colorValue = v.vector; break;
                    case SerializedPropertyType.Quaternion: p.quaternionValue = new Quaternion(v.vector.x,v.vector.y,v.vector.z,v.vector.w); break;
                    case SerializedPropertyType.AnimationCurve: p.animationCurveValue = v.curve; break;
                    case SerializedPropertyType.ObjectReference:
                        // 当前 GUID 与局部文件 ID 解析的 Unity 资源引用，仅保留在序列化适配边界。
                        Object reference = null;
                        if (!string.IsNullOrEmpty(v.referenceGuid))
                        {
                            reference = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(v.referenceGuid)).FirstOrDefault(/* 请求 GUID 文件内的候选 Unity 子资产，仅在序列化边界按 GUID 与局部 ID 精确匹配。 */ o =>
                                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string /* 当前候选子资产的真实资源 GUID，与请求引用身份逐项匹配。 */ guid,out long /* 当前候选子资产在同文件内的局部 ID，与 GUID 一起唯一定位资源引用。 */ local) && guid == v.referenceGuid && local == v.referenceLocalId);
                            if (reference == null) throw new ArgumentException("Asset GUID/local file ID does not resolve.");
                            // 沿序列化路径反射解析的声明类型，用于拒绝不匹配的 Unity 资产引用。
                            Type expected=typeof(SkillConfigSO);
                            // 当前原序列化字段路径段；Array 与 data[n] 只改变类型导航，不改写配置字段名。
                            foreach(string part in edit.path.Split('.'))
                            {
                                if(part=="Array") continue;
                                if(part.StartsWith("data[",StringComparison.Ordinal)) expected=expected.IsArray ? expected.GetElementType() : expected.GetGenericArguments()[0];
                                else expected=expected.GetField(part)?.FieldType ?? throw new ArgumentException("Cannot resolve reference field type.");
                            }
                            if(!expected.IsInstanceOfType(reference)) throw new ArgumentException("Reference type does not match the field.");
                        }
                        p.objectReferenceValue = reference;
                        if (p.objectReferenceValue != reference) throw new ArgumentException("Reference type does not match the field.");
                        break;
                    default: throw new ArgumentException("Use a supported leaf field or a structural track edit.");
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
