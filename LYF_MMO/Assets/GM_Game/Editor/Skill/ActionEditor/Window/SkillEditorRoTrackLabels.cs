using System;
using UnityEditor;
using UnityEngine;

/// <summary>RO轨道属性的双语显示映射；字段身份及枚举数值不随语言改变。</summary>
internal static class SkillEditorRoTrackLabels
{
    /// <summary>为原RO字段构建当前语言标签，未知扩展仍显示原字段名便于定位。</summary>
    /// <param name="property">原序列化属性，只读取字段名和原工具提示，不修改值。</param>
    /// <returns>界面显示内容，不作为序列化数据或业务载荷。</returns>
    internal static GUIContent Content(SerializedProperty property)
    {
        // 未覆盖字段显示Unity原名，保留第三方扩展的可读边界。
        string label = property.displayName;
        switch (property.name)
        {
            case "At": label = SkillEditorLocalization.Text("Start (s)", "开始时刻（秒）"); break;
            case "End": label = SkillEditorLocalization.Text("End (s)", "结束时刻（秒）"); break;
            case "Time": label = SkillEditorLocalization.Text("Shake duration (s)", "震屏时长（秒）"); break;
            case "Duration": label = SkillEditorLocalization.Text("Bar duration (s)", "进度条时长（秒）"); break;
            case "Index": label = SkillEditorLocalization.Text("RO event index", "RO事件索引"); break;
            case "colorGradientPath": label = SkillEditorLocalization.Text("Color gradient path", "颜色渐变资源路径"); break;
            case "Event": label = SkillEditorLocalization.Text("AI event key", "AI事件键"); break;
            case "TemplateID": label = SkillEditorLocalization.Text("Unit template ID", "召唤单位模板ID"); break;
            case "Loop": label = SkillEditorLocalization.Text("Repeat summon", "循环召唤"); break;
            case "LoopCount": label = SkillEditorLocalization.Text("Repeat count", "循环次数"); break;
            case "LoopCycle": label = SkillEditorLocalization.Text("Repeat interval (s)", "循环间隔（秒）"); break;
            case "LifeTime": label = SkillEditorLocalization.Text("Unit lifetime (s)", "召唤寿命（秒）"); break;
            case "CastPointType": label = SkillEditorLocalization.Text("Cast point source", "召唤原点来源"); break;
            case "MobPositionType": label = SkillEditorLocalization.Text("Spawn position rule", "召唤点规则"); break;
            case "MobLifeType": label = SkillEditorLocalization.Text("Lifetime rule", "寿命绑定规则"); break;
            case "MobMonsterType": label = SkillEditorLocalization.Text("Summoned unit type", "召唤对象类别"); break;
            case "MobFirerType": label = SkillEditorLocalization.Text("Owner source", "召唤归属来源"); break;
            case "MobAttrType": label = SkillEditorLocalization.Text("Attribute source", "召唤属性来源"); break;
            case "IsCopyHated": label = SkillEditorLocalization.Text("Copy threat", "复制仇恨"); break;
            case "IsCopyAttr": label = SkillEditorLocalization.Text("Copy attributes", "复制属性"); break;
            case "OffsetAtX": label = SkillEditorLocalization.Text("Spawn offset X (m)", "召唤横向偏移（米）"); break;
            case "OffsetAtY": label = SkillEditorLocalization.Text("Spawn offset Y (m)", "召唤高度偏移（米）"); break;
            case "OffsetAtZ": label = SkillEditorLocalization.Text("Spawn offset Z (m)", "召唤纵向偏移（米）"); break;
            case "MinRadius": label = SkillEditorLocalization.Text("Inner radius (m)", "内半径（米）"); break;
            case "MaxRadius": label = SkillEditorLocalization.Text("Outer radius (m)", "外半径（米）"); break;
            case "SceneCastKey": label = SkillEditorLocalization.Text("Scene spawn point key", "场景召唤点键"); break;
            case "MaxDistanceWithOwner": label = SkillEditorLocalization.Text("Owner distance limit (m)", "离拥有者距离上限（米）"); break;
            case "CanBeAffected": label = SkillEditorLocalization.Text("Can be affected by skills", "可被技能影响"); break;
            case "Coordinate": label = SkillEditorLocalization.Text("Shake coordinate space", "震屏坐标空间"); break;
            case "FovAmp": label = SkillEditorLocalization.Text("FOV amplitude (deg)", "视野角振幅（度）"); break;
            case "Frequency": label = SkillEditorLocalization.Text("Shake count within duration", "持续时间内震动次数"); break;
            case "AmplitudeX": label = SkillEditorLocalization.Text("X amplitude (m)", "X振幅（米）"); break;
            case "AmplitudeY": label = SkillEditorLocalization.Text("Y amplitude (m)", "Y振幅（米）"); break;
            case "AmplitudeZ": label = SkillEditorLocalization.Text("Z amplitude (m)", "Z振幅（米）"); break;
            case "Random": label = SkillEditorLocalization.Text("Random shake", "随机震动"); break;
            case "WithResult": label = SkillEditorLocalization.Text("Trigger with hit result", "随命中结果触发"); break;
            case "ShakeEveryOne": label = SkillEditorLocalization.Text("Shake for all viewers", "所有观看者震屏"); break;
            case "Curve": label = SkillEditorLocalization.Text("Shake curve path", "震屏曲线路径"); break;
            case "UseCameraEffect": label = SkillEditorLocalization.Text("Enable post effect", "启用后处理"); break;
            case "EveryOne": label = SkillEditorLocalization.Text("Apply for all viewers", "影响所有观看者"); break;
            case "Range": label = SkillEditorLocalization.Text("Viewer range (m)", "观看者范围（米）"); break;
            case "EffectType": label = SkillEditorLocalization.Text("Post effect type", "后处理类型"); break;
            case "Args0": label = SkillEditorLocalization.Text("Effect argument 0", "效果参数0"); break;
            case "Args1": label = SkillEditorLocalization.Text("Effect argument 1", "效果参数1"); break;
            case "FxShowTo": label = SkillEditorLocalization.Text("Visible to", "显示对象范围"); break;
            case "CameraFxId": label = SkillEditorLocalization.Text("Camera effect ID", "镜头特效ID"); break;
            case "AngularVelocity": label = SkillEditorLocalization.Text("Angular velocity (deg/s)", "旋转角速度（度/秒）"); break;
        }
        return new GUIContent(label, property.tooltip);
    }

    /// <summary>显示RO字段及其原枚举选项的双语文字，枚举下标仍交Unity序列化保存。</summary>
    /// <param name="property">当前RO事件的原子字段序列化游标。</param>
    /// <param name="applyEdit">原属性事务提交回调，不作为业务事件。</param>
    internal static void Draw(SerializedProperty property, Action applyEdit)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            SkillEditorPropertyLabels.DrawLeaf(property, Content(property), applyEdit);
            return;
        }
        // 原枚举选项的名称和数量由Unity实际字段类型提供。
        string[] names = property.enumNames;
        // 显示选项与原枚举索引一一对应，翻译不改变其数值。
        GUIContent[] labels = new GUIContent[names.Length];
        // 原枚举选项的零起始下标。
        for (int index = 0; index < names.Length; index++) labels[index] = new GUIContent(EnumLabel(names[index]));
        property.enumValueIndex = EditorGUILayout.Popup(Content(property), property.enumValueIndex, labels);
    }

    /// <summary>翻译RO原枚举成员的显示语义，未知新增成员保留原名以便识别。</summary>
    /// <param name="name">原枚举成员标识，不回写资产。</param>
    /// <returns>当前语言的显示文字。</returns>
    private static string EnumLabel(string name)
    {
        switch (name)
        {
            case "ESkillCastPointType_FirerBase": return SkillEditorLocalization.Text("FirerBase", "以施法者为原点");
            case "ESkillCastPointType_TargetBase": return SkillEditorLocalization.Text("TargetBase", "以技能目标为原点");
            case "ESkillCastPointType_SceneBase": return SkillEditorLocalization.Text("SceneBase", "以SceneCastKey对应的场景点为原点");
            case "ESkillMobPositionType_FIXED_OFFSET": return SkillEditorLocalization.Text("FIXED OFFSET", "按OffsetAtX/Y/Z生成固定偏移点");
            case "ESkillMobPositionType_RANDOM_CIRCLE": return SkillEditorLocalization.Text("RANDOM CIRCLE", "在MinRadius至MaxRadius环形区域内取点");
            case "ESkillMobLifeType_ESMLT_Normal": return SkillEditorLocalization.Text("ESMLT Normal", "由普通寿命规则管理");
            case "ESkillMobLifeType_ESMLT_WithinSkill": return SkillEditorLocalization.Text("ESMLT WithinSkill", "与所属技能生命周期绑定");
            case "ESkillMobLifeType_ESMLT_WithinSummoner": return SkillEditorLocalization.Text("ESMLT WithinSummoner", "与召唤者生命周期绑定");
            case "ESkillMobLifeType_ESMLT_WithinBuff": return SkillEditorLocalization.Text("ESMLT WithinBuff", "与关联Buff生命周期绑定");
            case "ESkillMobLifeType_ESMLT_WithinOwner": return SkillEditorLocalization.Text("ESMLT WithinOwner", "与拥有者生命周期绑定");
            case "ESkillMobMonsterType_NormalMonster": return SkillEditorLocalization.Text("NormalMonster", "普通怪物单位");
            case "ESkillMobMonsterType_Dummy": return SkillEditorLocalization.Text("Dummy", "用于技能表现或判定的替身单位");
            case "ESkillMobMonsterType_Trap": return SkillEditorLocalization.Text("Trap", "陷阱单位");
            case "ESkillMobMonsterType_Block": return SkillEditorLocalization.Text("Block", "阻挡单位");
            case "ESkillMobMonsterType_LifeForm": return SkillEditorLocalization.Text("LifeForm", "生命体单位");
            case "kMobFirerType_Firer": return SkillEditorLocalization.Text("Caster owner", "归属于施法者");
            case "kMobFirerType_Target": return SkillEditorLocalization.Text("Target owner", "归属于技能目标");
            case "kMobFirerType_BuffHoster": return SkillEditorLocalization.Text("Buff host owner", "归属于Buff宿主");
            case "kMobAttrType_Config": return SkillEditorLocalization.Text("Template attributes", "从召唤模板配置读取属性");
            case "kMobAttrType_FirerSnapShot": return SkillEditorLocalization.Text("Caster attribute snapshot", "使用施法者属性快照");
            case "kMobAttrType_Target": return SkillEditorLocalization.Text("Current target attributes", "使用目标当前属性");
            case "kMobAttrType_BuffHoster": return SkillEditorLocalization.Text("Current Buff host attributes", "使用Buff宿主当前属性");
            case "kMobAttrType_BuffHosterSnapShot": return SkillEditorLocalization.Text("Buff host attribute snapshot", "使用Buff宿主属性快照");
            case "ShowToSkillFirer": return SkillEditorLocalization.Text("Skill caster only", "只向技能施法者显示");
            case "ShowToSkillTarget": return SkillEditorLocalization.Text("Skill target only", "只向技能目标显示");
            case "ShowToEveryoneInSight": return SkillEditorLocalization.Text("Everyone in sight", "向视野内所有观察者显示");
            case "ShowToSkillTargetTeammerber": return SkillEditorLocalization.Text("Target team members", "向技能目标队友显示");
            case "CameraMotionSpace_World": return SkillEditorLocalization.Text("World", "世界坐标轴");
            case "CameraMotionSpace_Self": return SkillEditorLocalization.Text("Self", "角色自身坐标轴");
            case "CameraMotionSpace_Camera": return SkillEditorLocalization.Text("Camera", "镜头坐标轴");
            case "CameraEffectType_RadialBlur": return SkillEditorLocalization.Text("RadialBlur", "径向模糊");
            case "CameraEffectType_WaveDistort": return SkillEditorLocalization.Text("WaveDistort", "波形扭曲");
            default: return name;
        }
    }

}
