/// <summary>世界镜头的五种模式，数值同时对应镜头页的排列顺序与本机存档。</summary>
public enum WorldCameraMode
{
    /// <summary>允许自由环绕，在普通战斗和首领战中自动扩大视野。</summary>
    Smart3D,
    /// <summary>沿用切入该模式时玩家选好的俯角。</summary>
    Tuned25D,
    /// <summary>普通战斗保持玩家视野，首领战可自动拉高。</summary>
    Normal3D,
    /// <summary>固定使用项目预设的俯视角。</summary>
    Default25D,
    /// <summary>使用玩家逐项保存的镜头功能组合。</summary>
    Custom
}
