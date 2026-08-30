/// <summary>
/// 玩家主角实体。
/// </summary>
public class MainRoleEntity : EntityBase
{
    public int RoleID { get; set; }
    public int AccountID { get; set; }
    public int JobID { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Money { get; set; }
    public int SkillUpPoint { get; set; }
}
