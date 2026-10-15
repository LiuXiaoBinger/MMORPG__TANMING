namespace MMoRpgCommon.SceneEntity
{
    /// <summary>实体同步使用的确定性位置与朝向快照。</summary>
    public sealed class EntityTransformSnapshot
    {
        /// <summary>世界坐标 X。</summary>
        public float X { get; set; }
        /// <summary>世界坐标 Y（高度）。</summary>
        public float Y { get; set; }
        /// <summary>世界坐标 Z。</summary>
        public float Z { get; set; }
        /// <summary>绕 Y 轴朝向角（弧度）。</summary>
        public float Yaw { get; set; }
    }
}
