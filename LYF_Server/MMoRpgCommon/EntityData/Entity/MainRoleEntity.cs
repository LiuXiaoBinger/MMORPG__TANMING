/// <summary>
/// 玩家主角实体。
/// </summary>
public class MainRoleEntity : EntityBase
{
    /// <summary>角色持久化 ID。</summary>
    public int RoleID { get; set; }

    /// <summary>登录账号 ID。</summary>
    public int AccountID { get; set; }

    /// <summary>职业配置 ID。</summary>
    public int JobID { get; set; }

    /// <summary>角色等级。</summary>
    public int Level { get; set; }

    /// <summary>当前累计经验。</summary>
    public int Exp { get; set; }

    /// <summary>持有游戏货币数量。</summary>
    public int Money { get; set; }

    /// <summary>可分配技能点数。</summary>
    public int SkillUpPoint { get; set; }
}
