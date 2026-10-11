namespace Ethan.ActionEditor.Editor
{
    /// <summary>原 Y 内置轨道名称与序列化列表的映射；隐藏界面不移除既有事件数据。</summary>
    internal static class BuiltinTrackRegistry
    {
        /// <summary>将验证器的原轨道标识映射到配置列表字段名，未知扩展返回空。</summary>
        /// <param name="track">原验证轨道标识，用于查找配置内的列表属性名。</param>
        internal static string GetValidationListProperty(string track)
        {
            switch (track)
            {
                case "ChangeColorList": return "ChangeColorList";
                case "SkillBarList": return "SkillBarList";
                case "MobUnitList": return "MobUnitList";
                case "AIEventList": return "AIEventList";
                case "BuffList": return "BuffList";
                case "ShakeList": return "ShakeList";
                case "CameraPostEffect": return "CameraPostEffect";
                case "CameraFxData": return "CameraFxData";
                case "RotateList": return "RotateList";
                case "Flow": return "flowNodes";
                case "Camera": return "cameraCues";
                case "Animation": return "animSegments";
                case "Attack": return "attackList";
                case "Phase2Attack": return "phase2AttackList";
                case "FxAndSound": return "fxList";
                case "Phase2Fx": return "phase2FxList";
                case "Jump": return "jumpList";
                case "CancelPoint": return "cancelList";
                case "Projectile": return "projectileList";
                case "WarningCue": return "warningCueList";
                case "HitFx": return "hitFxList";
                case "SuperArmor": return "superArmorList";
                case "AdjustMotion": return "adjustMotionList";
                case "Trail": return "trailToggleList";
                case "Move": return "moveSegmentList";
                case "Interaction": return "interactionWindows";
                default: return null;
            }
        }

        // 未指定资源的空事件仍须保持可选，允许用户随后绑定粒子或音效。
        /// <summary>判断事件是否出现在特效轨道；空事件仍可选中以便指定粒子引用。</summary>
        /// <param name="item">原粒子/音效事件引用；空引用不显示，空资源占位事件仍保持可编辑。</param>
        internal static bool IsVisibleInFxTrack(Global.FxAndSound item)
        {
            if (item == null) return false;
            if (item.contentKind != Global.EffectContentKind.Legacy)
                return item.contentKind == Global.EffectContentKind.VisualEffect;
            return item.particleSystem != null || item.audioClip == null;
        }
        /// <summary>判断事件是否出现在音效轨道；空事件仍可选中以便指定音频引用。</summary>
        /// <param name="item">原粒子/音效事件引用；空引用不显示，空资源占位事件仍保持可编辑。</param>
        internal static bool IsVisibleInSoundTrack(Global.FxAndSound item)
        {
            if (item == null) return false;
            if (item.contentKind != Global.EffectContentKind.Legacy)
                return item.contentKind == Global.EffectContentKind.Audio;
            return item.audioClip != null || item.particleSystem == null;
        }

        /// <summary>折叠旧跳转、取消、预警及朝向别名；音效保持独立身份，不与粒子抢占显示行。</summary>
        /// <param name="type">原轨道类型枚举；规范化只影响界面分组，不迁移数据。</param>
        internal static Global.TrackType Canonical(Global.TrackType type)
        {
            if (type == Global.TrackType.Jump || type == Global.TrackType.Cancel) return Global.TrackType.Flow;
            if (type == Global.TrackType.Warning) return Global.TrackType.Fx;
            if (type == Global.TrackType.AdjustMotion) return Global.TrackType.Move;
            return type;
        }

        /// <summary>按规范轨道返回表现或机制分组标识，仅用于编辑界面布局。</summary>
        /// <param name="type">原轨道类型枚举；规范化只影响界面分组，不迁移数据。</param>
        internal static string GetCategory(Global.TrackType type)
        {
            switch (Canonical(type))
            {
                case Global.TrackType.ChangeColor:
                case Global.TrackType.SkillBar:
                case Global.TrackType.Shake:
                case Global.TrackType.CameraPostEffect:
                case Global.TrackType.CameraFx:
                case Global.TrackType.Animation:
                case Global.TrackType.Fx:
                case Global.TrackType.Sound:
                case Global.TrackType.Camera:
                case Global.TrackType.HitFx:
                case Global.TrackType.Warning:
                case Global.TrackType.Trail: return "Presentation";
                default: return "Mechanics";
            }
        }
    }
}
