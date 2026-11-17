using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>本地玩家持有的目标表现组件，在帧末让 RO 敌方选中圈跟随当前怪物。</summary>
public sealed class MTargetFxComponent
{
    /// <summary>原 RO 敌方脚底预制体在主工程 Resources 中的相对路径。</summary>
    private const string FX_ENEMY_FEET = "Effects/Prefabs/Creature/Common/Fx_Skill_Caution_SuoDi_02_01";

    /// <summary>这一名本地玩家复用的唯一圆环实例，随目标所在场景或玩家离场销毁。</summary>
    private GameObject _fxEnemy_Feet;

    /// <summary>对应 RO 的上一次目标，保存运行时 UID 以区分实体池复用前后，零表示没有标记。</summary>
    private ulong _lastTargetUid;

    /// <summary>同步目标的世界位置、旋转和碰撞尺寸，切换目标时重播收圈动画与粒子。</summary>
    /// <param name="target">玩家通过 Tab、点击或技能自动选择的当前实体，取消选择时为空。</param>
    public void LateUpdate(MEntity target)
    {
        if (!MSkillTargetDefine.IsEnemy(target) || !MSkillTargetDefine.IsValidTarget(target))
        {
            if (_lastTargetUid != 0 && _fxEnemy_Feet != null)
            {
                _fxEnemy_Feet.SetActive(false);
            }
            _lastTargetUid = 0;
            return;
        }

        if (_fxEnemy_Feet == null)
        {
            prepairFx();
        }

        // 圆环独立于怪物模型的缩放，归属目标实际所在场景，卸载副本时一同移除。
        Scene targetScene = target.Model.GameObject.NativeObject.scene;
        if (_fxEnemy_Feet.scene != targetScene)
        {
            SceneManager.MoveGameObjectToScene(_fxEnemy_Feet, targetScene);
        }
        // 原 RO 的 activeFeet 表示这次选择使用的脚圈。
        Transform activeFeet = _fxEnemy_Feet.transform;
        activeFeet.SetPositionAndRotation(target.Position, target.Model.Rotation);
        // 沿用原 RO 的 radius 计算值，此处实际表示碰撞直径，单位为米。
        float radius = target.Radius * 2f;
        activeFeet.localScale = new Vector3(radius / 1.5f, 1f, radius / 1.5f);

        if (_lastTargetUid == target.UID)
        {
            return;
        }
        _lastTargetUid = target.UID;
        _fxEnemy_Feet.SetActive(true);
        // 原 Clip01 的 0.2 秒缩放曲线只在新选中时从头播一次。
        Animation acquire = _fxEnemy_Feet.GetComponent<Animation>();
        acquire.Stop();
        acquire.Play();
        // 清掉上一目标在世界坐标留下的粒子，再从新目标脚下播放。
        ParticleSystem ring = _fxEnemy_Feet.GetComponentInChildren<ParticleSystem>();
        ring.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ring.Play(true);
    }

    /// <summary>本地玩家离场时释放已创建的圆环，并清空对象池下次借用前的选择身份。</summary>
    public void OnDetachFromHost()
    {
        if (_fxEnemy_Feet != null)
        {
            Object.Destroy(_fxEnemy_Feet);
            _fxEnemy_Feet = null;
        }
        _lastTargetUid = 0;
    }

    /// <summary>按 RO 同名方法准备敌方脚圈实例，首次选中或原实例随场景卸载后调用。</summary>
    private void prepairFx()
    {
        _fxEnemy_Feet = Object.Instantiate(Resources.Load<GameObject>(FX_ENEMY_FEET));
        _lastTargetUid = 0;
    }
}
