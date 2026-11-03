namespace MMoRpgCommon.SceneEntity
{
    /// <summary>实体位置与朝向的数据副本；使用浮点值保存，不负责移动计算。</summary>
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
