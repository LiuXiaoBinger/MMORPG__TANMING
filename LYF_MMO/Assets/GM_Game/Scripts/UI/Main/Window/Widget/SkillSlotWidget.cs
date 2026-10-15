using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快捷技能槽，显示图标并从当前角色技能核心逐帧读取冷却剩余时间。
/// </summary>
public class SkillSlotWidget : MonoBehaviour
{
    /// <summary>技能图标。</summary>
    [SerializeField, Header("技能图标")] private Image _imgIcon;

    /// <summary>固定快捷键文本。</summary>
    [SerializeField, Header("技能绑定按键")] private TMP_Text _texKey;

    /// <summary>技能冷却遮罩。</summary>
    [SerializeField, Header("技能cd mask")] private Image _imgMask;

    /// <summary>技能冷却倒计时文本。</summary>
    [SerializeField, Header("技能cd")] private TMP_Text _texCD;

    /// <summary>中央槽满怒时显示的青蓝细环；普通槽不绑定此节点。</summary>
    [SerializeField, Header("中央满怒外观")] private GameObject _rageReadyVisual;

    /// <summary>技能槽固定快捷键。</summary>
    private string _skillKey;

    /// <summary>当前冷却动画订阅。</summary>
    private IDisposable _cooldownSubscription;

    /// <summary>技能图标异步绑定版本。</summary>
    private int _bindingVersion;

    /// <summary>当前槽位绑定的旧技能主表 ID，零表示没有技能核心可读取。</summary>
    private int _skillId;

    /// <summary>当前技能配置的完整 CD 秒数，作为遮罩填充比例的基准。</summary>
    private float _cooldownDuration;

    /// <summary>组件销毁时释放冷却订阅。</summary>
    private void OnDestroy()
    {
        DisposeCooldown();
    }

    /// <summary>设置技能槽固定快捷键。</summary>
    public void SetKey(string key)
    {
        _skillKey = key;
        if (_texKey != null)
        {
            _texKey.SetText(key);
        }
    }

    /// <summary>使用技能展示数据刷新当前槽位。</summary>
    /// <param name="skill">控制器提供的真实技能图标、配置编号和剩余冷却；空值清除当前绑定。</param>
    public void ReFreshUI(SkillViewData skill)
    {
        Clear();
        // 本次图标请求所属的槽位绑定版本，防止旧图标写入已换技能的槽位。
        int bindingVersion = _bindingVersion;
        if (skill == null)
        {
            return;
        }
        _skillId = skill.SkillId;
        _cooldownDuration = skill.Cooldown;
        if (!string.IsNullOrEmpty(skill.IconPath))
        {
            // sprite 为本次资源加载得到的技能图标，仅按现有满怒外观状态决定是否显示。
            ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
            {
                if (_imgIcon != null && sprite != null && bindingVersion == _bindingVersion)
                {
                    _imgIcon.sprite = sprite;
                    // 中央图标只在外部提供满怒状态后显示，普通槽照常展示真实技能图标。
                    _imgIcon.gameObject.Show(_rageReadyVisual == null || _rageReadyVisual.activeSelf);
                }
            });
        }
        if (skill.RemainingCooldown > 0f)
        {
            StartCooldown(skill.RemainingCooldown);
        }
    }

    /// <summary>切换中央槽的满怒细环及图标，不承担怒气玩法、CD或施法门禁。</summary>
    /// <param name="isFull">窗口按真实当前值和有效上限判断的满怒状态。</param>
    public void SetRageFull(bool isFull)
    {
        if (_rageReadyVisual == null)
        {
            return;
        }
        _rageReadyVisual.SetActive(isFull);
        if (_imgIcon != null)
        {
            _imgIcon.gameObject.Show(isFull && _imgIcon.sprite != null);
        }
    }

    /// <summary>清除技能绑定展示，保留槽位固定快捷键。</summary>
    public void Clear()
    {
        DisposeCooldown();
        _bindingVersion++;
        _skillId = 0;
        _cooldownDuration = 0f;
        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.gameObject.Show(false);
        }
        SetCooldownVisible(false);
    }

    /// <summary>按不受 timeScale 影响的实时时钟播放技能冷却表现。</summary>
    /// <param name="duration">本次刷新时的剩余冷却秒数，非正数表示立即清除遮罩。</param>
    public void StartCooldown(float duration)
    {
        DisposeCooldown();
        if (duration <= 0f)
        {
            SetCooldownVisible(false);
            return;
        }

        // 无技能核心绑定时保留原有独立计时行为，兼容直接调用 StartCooldown 的槽位。
        float cooldownEndRealtime = Time.realtimeSinceStartup + duration;
        // 订阅创建时的技能和角色引用，角色切换或槽位重绑后不得读取新角色的旧 CD。
        int boundSkillId = _skillId;
        PlayerEntity boundRole = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null)
        {
            boundRole = Global.Instance.SceneWorld.LocalRole;
        }
        // 有完整配置时按总 CD 计算填充；直接调用仍沿用传入的剩余秒数。
        float fillDuration = duration;
        if (_cooldownDuration > 0f)
        {
            fillDuration = _cooldownDuration;
        }
        SetCooldownVisible(true);
        if (_texCD != null)
        {
            _texCD.SetText(duration.ToString("F1"));
        }
        if (_imgMask != null)
        {
            _imgMask.fillAmount = Mathf.Clamp01(duration / fillDuration);
        }
        _cooldownSubscription = Observable.EveryUpdate().Subscribe(_ =>
        {
            // 技能槽优先读取核心实时 CD；预测超时回滚后下一帧即可清掉旧遮罩。
            float remainingTime = cooldownEndRealtime - Time.realtimeSinceStartup;
            if (boundSkillId > 0)
            {
                // 绑定角色或其技能数据缺失时，视为冷却失效。
                bool cooldownInvalid = boundRole == null || boundRole.Skill == null;
                // 全局单例缺失时无法读取本地角色冷却。
                if (!cooldownInvalid && Global.Instance == null)
                {
                    cooldownInvalid = true;
                }
                // 场景世界缺失时同样无法读取本地角色冷却。
                if (!cooldownInvalid && Global.Instance.SceneWorld == null)
                {
                    cooldownInvalid = true;
                }
                // 本地角色与绑定角色不一致时，视为冷却失效。
                if (!cooldownInvalid && Global.Instance.SceneWorld.LocalRole != boundRole)
                {
                    cooldownInvalid = true;
                }
                // 当前技能 ID 已被刷新绑走时，视为冷却失效。
                if (!cooldownInvalid && _skillId != boundSkillId)
                {
                    cooldownInvalid = true;
                }
                if (cooldownInvalid)
                {
                    remainingTime = 0f;
                }
                else
                {
                    remainingTime = boundRole.Skill.GetRemainingCooldown(
                        boundSkillId, Time.realtimeSinceStartup);
                }
            }
            if (remainingTime > 0f)
            {
                if (_texCD != null)
                {
                    _texCD.SetText(remainingTime.ToString("F1"));
                }
                if (_imgMask != null)
                {
                    _imgMask.fillAmount = Mathf.Clamp01(remainingTime / fillDuration);
                }
                return;
            }
            SetCooldownVisible(false);
            DisposeCooldown();
        });
    }

    /// <summary>切换冷却遮罩和倒计时显示。</summary>
    private void SetCooldownVisible(bool visible)
    {
        if (_imgMask != null)
        {
            _imgMask.gameObject.Show(visible);
            if (!visible)
            {
                _imgMask.fillAmount = 0f;
            }
        }
        if (_texCD != null && !visible)
        {
            _texCD.text = string.Empty;
        }
    }

    /// <summary>释放旧冷却订阅，避免重绑后重复刷新。</summary>
    private void DisposeCooldown()
    {
        if (_cooldownSubscription != null)
        {
            _cooldownSubscription.Dispose();
            _cooldownSubscription = null;
        }
    }
}
