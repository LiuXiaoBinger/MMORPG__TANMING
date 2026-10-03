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
    public void ReFreshUI(SkillViewData skill)
    {
        Clear();
        int bindingVersion = _bindingVersion;
        if (skill == null)
        {
            return;
        }
        _skillId = skill.SkillId;
        _cooldownDuration = skill.Cooldown;
        if (!string.IsNullOrEmpty(skill.IconPath))
        {
            ResourceMgr.Instance.LoadSpriteAsync(skill.IconPath, sprite =>
            {
                if (_imgIcon != null && sprite != null && bindingVersion == _bindingVersion)
                {
                    _imgIcon.gameObject.Show();
                    _imgIcon.sprite = sprite;
                }
            });
        }
        if (skill.RemainingCooldown > 0f)
        {
            StartCooldown(skill.RemainingCooldown);
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
                if (boundRole == null || boundRole.Skill == null ||
                    Global.Instance == null || Global.Instance.SceneWorld == null ||
                    Global.Instance.SceneWorld.LocalRole != boundRole ||
                    _skillId != boundSkillId)
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

    /// <summary>组件销毁时释放冷却订阅。</summary>
    private void OnDestroy()
    {
        DisposeCooldown();
    }
}
