using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快捷技能槽，只显示技能图标和控制器下发的冷却表现。
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
        if (_imgIcon != null)
        {
            _imgIcon.sprite = null;
            _imgIcon.gameObject.Show(false);
        }
        SetCooldownVisible(false);
    }

    /// <summary>播放由技能控制器确认后的冷却表现。</summary>
    public void StartCooldown(float duration)
    {
        DisposeCooldown();
        if (duration <= 0f)
        {
            SetCooldownVisible(false);
            return;
        }

        float remainingTime = duration;
        SetCooldownVisible(true);
        _cooldownSubscription = Observable.EveryUpdate().Subscribe(_ =>
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime > 0f)
            {
                if (_texCD != null)
                {
                    _texCD.SetText(remainingTime.ToString("F1"));
                }
                if (_imgMask != null)
                {
                    _imgMask.fillAmount = remainingTime / duration;
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
