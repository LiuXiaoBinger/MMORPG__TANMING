namespace Ethan.ActionEditor.Editor
{
    /// <summary>时间轴与属性面板共享的特效内容分类和字段显示规则，兼容原音效数据。</summary>
    public static class EffectAuthoring
    {
        /// <summary>显式内容类型优先；旧事件仅有音频时归音效，其他情况归粒子特效。</summary>
        /// <param name="effect">原粒子/音效事件，旧 Legacy 内容类型按真实引用推断。</param>
        public static Global.EffectContentKind Resolve(Global.FxAndSound effect)
        {
            if (effect.contentKind != Global.EffectContentKind.Legacy) return effect.contentKind;
            return effect.audioClip != null && effect.particleSystem == null
                ? Global.EffectContentKind.Audio : Global.EffectContentKind.VisualEffect;
        }

        /// <summary>按内容类型生成事件显示名，保留原资源名称和旧粒子加音效双引用提示。</summary>
        /// <param name="effect">原粒子/音效事件，旧 Legacy 内容类型按真实引用推断。</param>
        public static string Label(Global.FxAndSound effect)
        {
            if (effect.contentKind == Global.EffectContentKind.Legacy && effect.particleSystem != null && effect.audioClip != null)
                return "VFX + Audio: " + effect.particleSystem.name + " / " + effect.audioClip.name;
            return Resolve(effect) == Global.EffectContentKind.Audio
                ? "Audio: " + (effect.audioClip != null ? effect.audioClip.name : "Assign clip")
                : "VFX: " + (effect.particleSystem != null ? effect.particleSystem.name : "Assign effect");
        }

        /// <summary>根据特效或音效分类及高级显示开关决定字段是否绘制，不删除隐藏的原字段。</summary>
        /// <param name="kind">已解析的粒子特效或音效分类，用于字段显隐判断。</param>
        /// <param name="field">真实字段名；隐藏字段仍保留在原序列化配置内。</param>
        /// <param name="advanced">是否显示原 Y 的结束帧、速度、随机化等高级字段。</param>
        public static bool IsFieldVisible(Global.EffectContentKind kind, string field, bool advanced)
        {
            if (field == "keyNumber" || (advanced && field == "endKeyNumber"))
                return true;
            if (kind == Global.EffectContentKind.Audio)
                return field == "audioClip" || field == "baseVolume" ||
                    (advanced && (field == "audioRandomize" || field == "pitchVariation" || field == "volumeVariation"));
            return field == "particleSystem" || field == "socketPath" || field == "placementKind" || field == "forwardDistance" || field == "offset" || field == "rotation" || field == "scale" ||
                field == "followCharacter" || field == "customLifetime" ||
                (advanced && (field == "endKeyNumber" || field == "playbackSpeed" || field == "useWorldSpace"));
        }
    }
}
