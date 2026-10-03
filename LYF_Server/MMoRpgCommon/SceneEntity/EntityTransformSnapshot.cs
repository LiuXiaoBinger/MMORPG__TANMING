namespace MMoRpgCommon.SceneEntity
{
    /// <summary>实体同步使用的确定性位置与朝向快照。</summary>
    public sealed class EntityTransformSnapshot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
    }
}
