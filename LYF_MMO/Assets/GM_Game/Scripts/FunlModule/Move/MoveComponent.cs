using System.Collections;
using System.Collections.Generic;
using UVEC3 = UnityEngine.Vector3;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class MoveComponent : RoleComponentBase
{
    public MoveComponent(ClientRole owner) : base(owner)
    {
    }
    /// <summary>影子跟随的间隔时间（秒）。他机收到移动同步后，从当前点到目标点平滑移动的窗口时长</summary>
    private float delay = 0.15f;//影子跟随的间隔时间
    /// <summary>移动预测标记：服务器下发同步时实体是否仍在持续移动。用于提前到达目标后是“沿朝向外推”还是“停步”</summary>
    private bool keep_moving = false;//移动预测的标记
    /// <summary>最大 Lerp 转向角度（超过该角度则直接转向，不插值）</summary>
    const float MaxRotationAngle = 90;//最大lerp转向角度
    /// <summary>最小 Lerp 转向角度（小于该角度不转向，避免微小抖动反复转向）</summary>
    const float MinRotationAngle = 2;//最小lerp转向角度
    
    public bool LocalSync//客户端自己模拟位置的标记
    {
        set;
        get;
    }
    /// <summary>传送带速度：当前所在所有传送带速度的向量和</summary>
    public UVEC3 WallSpeed { set; get; } = UVEC3.zero;//传送带速度
    /// <summary>最终面朝方向（也用作移动预测/外推的方向）</summary>
    private UVEC3 face = UVEC3.zero;//最终面朝方向

    /// <summary>是否需要水平方向的移动 Update（水平移动激活标记）</summary>
    private bool _isMoving = false;//是否需要水平方向的移动update
    /// <summary>是否需要垂直方向的移动 Update（飞行/攀爬等 Y 轴移动）</summary>
    private bool _isMovingY = false;//是否需要垂直方向的移动update
    /// <summary>目标位置（本机移动目标 / 他机服务器下发的目标点）</summary>
    private UVEC3 _targetPos = UVEC3.zero;//目标位置
    /// <summary>开始位置（本次移动或本次同步的起点）</summary>
    private UVEC3 _startPos = UVEC3.zero;//开始位置
    /// <summary>目标方向向量（水平单位化后的移动方向）</summary>
    private UVEC3 _targetDir = UVEC3.zero;//目标方向向量
    /// <summary>上次移动后离目标点的距离（用于判断是否走过头，防止在目标点附近来回抖动）</summary>
    private float _lastDis = 0.0f;//上次移动后离目标点的距离
    /// <summary>影子跟随已经持续的时间（每帧累加 deltaTime，与 delay 比较判断是否到达目标）</summary>
    private float _server_time = 0;//影子跟随已经持续的时间
    /// <summary>预测的位移向量（预留的预测移动字段）</summary>
    private UVEC3 _predictedPos = UVEC3.zero;//预测的向量
    /// <summary>预测的持续时间（预留的预测移动字段）</summary>
    private float _predictedTime = 0;//预测的持续时间
    /// <summary>当前所站立的传送带 Trigger（用于判断玩家是否还在传送带上）</summary>
   // private MSceneWallTrigger _enterSpeedWall = null;//所站立的传送带
    /// <summary>所在的所有传送带：wallUuid -> 该传送带速度（进入/更新/离开时维护）</summary>
    private Dictionary<ulong, UVEC3> _transWalls = new Dictionary<ulong, UVEC3>();
    
    /// <summary>本次移动总计花费的时间（本机预测累计，用于上行移动上报的 time 字段）</summary>
    public float CostTime { get; set; }//本次移动总计花费的时间
    /// <summary>本次移动是否真的有玩家主动的移动（区别于完全由传送带产生的被动移动）</summary>
    public bool IsMoved { get; set; }//本次移动是否真的有玩家主动的移动（为了区别于全是传送带的移动）
    /// <summary>强制同步位置到服务器的一次性标志（传送带/空气墙等场景置位）</summary>
    private bool force_sync_pos = false;//强制同步位置到服务器
    /// <summary>是否强制同步位置到服务器（读取一次后自动复位，配合上传逻辑使用）</summary>
    public bool ForceSyncPos
    {
        get
        {
            if (force_sync_pos)
            {
                force_sync_pos = false;
                return true;
            }
            return false;
        }
    }
    /// <summary>当前是否处于水平移动状态</summary>
    public bool IsMoving { get { return _isMoving; } }
}
