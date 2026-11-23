using UnityEngine;

/// <summary>在角色现有的手持挂点与背部挂点之间移动同一把主武器。</summary>
public class MRoleWeaponMount : MonoBehaviour
{
    /// <summary>角色预制体中的主武器实例，随战斗状态在两个挂点之间切换。</summary>
    [SerializeField] private Transform _weapon;

    /// <summary>随胸部骨骼运动的背部挂点，位置和朝向保存在预制体中。</summary>
    [SerializeField] private Transform _backSocket;

    /// <summary>实例化时记录的原手持父节点，当前模型存活期间用于恢复握剑。</summary>
    private Transform _handSocket;

    /// <summary>主武器原手持局部坐标，单位米。</summary>
    private Vector3 _handPosition;

    /// <summary>主武器原手持局部旋转。</summary>
    private Quaternion _handRotation;

    /// <summary>当前是否手持，避免静止期间重复设置武器父节点。</summary>
    private bool _isDrawn = true;

    /// <summary>保存预制体中的握剑姿态，首次显示时将主武器放到背部。</summary>
    private void Awake()
    {
        _handSocket = _weapon.parent;
        _handPosition = _weapon.localPosition;
        _handRotation = _weapon.localRotation;
        SetDrawn(false);
    }

    /// <summary>战斗或技能期间恢复原手持姿态，普通状态使用背部挂点。</summary>
    /// <param name="drawn">true 表示手持，false 表示背负。</param>
    public void SetDrawn(bool drawn)
    {
        if (_isDrawn == drawn) return;
        _isDrawn = drawn;
        if (drawn)
        {
            _weapon.SetParent(_handSocket, false);
            _weapon.localPosition = _handPosition;
            _weapon.localRotation = _handRotation;
        }
        else
        {
            _weapon.SetParent(_backSocket, false);
            _weapon.localPosition = Vector3.zero;
            _weapon.localRotation = Quaternion.identity;
        }
    }
}
