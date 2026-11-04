using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>本地玩家持有的目标表现组件，在帧末让 RO 敌方选中圈跟随当前怪物。</summary>
public sealed class MTargetFxComponent
{
    /// <summary>原 RO 敌方脚底预制体在主工程 Resources 中的相对路径。</summary>
    private const string EnemyFeetPath = "Effects/Prefabs/Creature/Common/Fx_Skill_Caution_SuoDi_02_01";

    /// <summary>这一名本地玩家复用的唯一圆环实例，随目标所在场景或玩家离场销毁。</summary>
    private GameObject _enemyFeet;

    /// <summary>当前显示圆环的运行时实体身份，零表示没有选中标记。</summary>
    private ulong _markedUid;

    /// <summary>同步目标的世界位置、旋转和碰撞尺寸，切换目标时重播收圈动画与粒子。</summary>
    /// <param name="target">玩家通过 Tab、点击或技能自动选择的当前实体，取消选择时为空。</param>
    public void LateUpdate(MEntity target)
    {
        if (!MSkillTargetDefine.IsEnemy(target) || !MSkillTargetDefine.IsValidTarget(target))
        {
            if (_markedUid != 0 && _enemyFeet != null)
            {
                _enemyFeet.SetActive(false);
            }
            _markedUid = 0;
            return;
        }

        if (_enemyFeet == null)
        {
            _enemyFeet = Object.Instantiate(Resources.Load<GameObject>(EnemyFeetPath));
            _markedUid = 0;
        }

        // 圆环独立于怪物模型的缩放，归属目标实际所在场景，卸载副本时一同移除。
        Scene targetScene = target.Model.GameObject.NativeObject.scene;
        if (_enemyFeet.scene != targetScene)
        {
            SceneManager.MoveGameObjectToScene(_enemyFeet, targetScene);
        }
        _enemyFeet.transform.SetPositionAndRotation(target.Position, target.Model.Rotation);
        // 原 RO 按碰撞直径除以 1.5 缩放水平面，预制体保持自己的离地高度。
        float scale = target.Radius * 2f / 1.5f;
        _enemyFeet.transform.localScale = new Vector3(scale, 1f, scale);

        if (_markedUid == target.UID)
        {
            return;
        }
        _markedUid = target.UID;
        _enemyFeet.SetActive(true);
        // 原 Clip01 的 0.2 秒缩放曲线只在新选中时从头播一次。
        Animation acquire = _enemyFeet.GetComponent<Animation>();
        acquire.Stop();
        acquire.Play();
        // 清掉上一目标在世界坐标留下的粒子，再从新目标脚下播放。
        ParticleSystem ring = _enemyFeet.GetComponentInChildren<ParticleSystem>();
        ring.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ring.Play(true);
    }

    /// <summary>本地玩家离场时释放已创建的圆环，并清空对象池下次借用前的选择身份。</summary>
    public void OnDetachFromHost()
    {
        if (_enemyFeet != null)
        {
            Object.Destroy(_enemyFeet);
            _enemyFeet = null;
        }
        _markedUid = 0;
    }
}
