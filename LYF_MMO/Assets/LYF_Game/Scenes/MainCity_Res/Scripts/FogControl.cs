using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 当带 Player 标签的碰撞体进入或离开区域时，在水下与环境配置之间改变整个场景的雾密度和颜色。
/// 本组件只写 RenderSettings 的全局雾参数，雾的开关和模式由场景另行配置。
/// </summary>
public class FogControl : MonoBehaviour
{

    [Header("Underwater Fog")]
    // 玩家进入区域后的目标雾颜色，由 Inspector 配置。
    public Color fogColorUnderwater;
    // 玩家进入区域后的目标雾密度系数，写入全场景的 RenderSettings.fogDensity。
    public float fogDensityUnderwater = 0.125f;
    // 进入区域时的渐变时长，单位为受 timeScale 影响的游戏秒；虽然名称含 Speed，实际作为持续时间使用。
    public float transitionSpeedUnder = 2f;


    [Header("Environment Fog")]
    // 离开区域时恢复的雾颜色，由 Inspector 配置；Awake 不会自动读取场景当前的雾颜色。
    public Color fogColorEnvironment;
    // 离开区域时恢复的雾密度系数；Awake 会用场景当时的雾密度覆盖这里的初值。
    public float fogDensityEnvironment = 0.015f;
    // 离开区域时的渐变时长，单位为受 timeScale 影响的游戏秒，密度和颜色使用同一时长。
    public float transitionSpeedAbove = 0.15f;

    // 最近一次进入区域时创建的密度协程，用于在离开区域时停止它。
    private IEnumerator enterCoroutine;
    // 最近一次离开区域时创建的密度协程，用于在再次进入区域时停止它。
    private IEnumerator exitCoroutine;

    // 最近一次进入区域时创建的颜色协程，和密度协程分别保存、分别停止。
    private IEnumerator enterCoroutine2;
    // 最近一次离开区域时创建的颜色协程，用于在再次进入区域时停止颜色恢复过程。
    private IEnumerator exitCoroutine2;


    /// <summary>
    /// 记录组件初始化时的全局雾密度，作为之后离开区域时恢复的目标值。
    /// </summary>
    void Awake ()
    {
        fogDensityEnvironment = RenderSettings.fogDensity;
    }

    /// <summary>
    /// 玩家进入时停止离开方向的渐变，分别启动密度和颜色协程，切换到水下配置。
    /// </summary>
    /// <param name="other">进入当前触发区域的碰撞体；只处理其所在对象带 Player 标签的情况。</param>
    void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enter!");

            if (exitCoroutine != null)

                StopCoroutine(exitCoroutine);
            // 始终从保存的环境密度开始插值，起点不会改成进入瞬间的实际雾密度。
            enterCoroutine = (DoTheFade(fogDensityEnvironment, fogDensityUnderwater, transitionSpeedUnder, fogColorUnderwater));
            StartCoroutine(enterCoroutine);
            

            if (exitCoroutine2 != null)
             
                StopCoroutine(exitCoroutine2);
                // 这里的 if 只控制上面的 StopCoroutine；每次玩家进入都会新建并启动颜色渐变。
                enterCoroutine2 = (ChangeColor(fogColorEnvironment, fogColorUnderwater, transitionSpeedUnder));
                StartCoroutine(enterCoroutine2);
            

        }
            
    }

    /// <summary>
    /// 玩家离开时停止进入方向的渐变，按配置的水下起点恢复环境密度和颜色。
    /// </summary>
    /// <param name="other">离开当前触发区域的碰撞体；使用碰撞体所在对象的 Player 标签筛选。</param>
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag ("Player"))
        {
            Debug.Log("Exit!");

            if (enterCoroutine != null)

                StopCoroutine(enterCoroutine);
            // 使用配置的水下密度作为起点，即使进入方向的渐变尚未完成也按这个起点恢复。
            exitCoroutine = (DoTheFade(fogDensityUnderwater, fogDensityEnvironment, transitionSpeedAbove, fogColorEnvironment));
            StartCoroutine(exitCoroutine);

            if (enterCoroutine2 != null)
                StopCoroutine(enterCoroutine2);
            exitCoroutine2 = (ChangeColor(fogColorUnderwater, fogColorEnvironment, transitionSpeedAbove));
            StartCoroutine(exitCoroutine2);
        }
            
    }

    /// <summary>
    /// 每帧按时间插值全局雾密度，同时把全局雾颜色直接写成指定颜色。
    /// 起终密度不同时，当前循环不会在达到目标后自行结束，需要外部停止协程。
    /// </summary>
    /// <param name="current">密度的固定插值起点，由进入或离开事件传入配置值。</param>
    /// <param name="goal">密度的插值终点。</param>
    /// <param name="time">计划的渐变时长，单位为游戏秒；计算进度时直接用它作除数。</param>
    /// <param name="color">本协程每帧直接写入的雾颜色；并行的 ChangeColor 也会写同一个全局颜色。</param>
    /// <returns>逐帧执行密度渐变的迭代器，由触发事件启动和停止。</returns>
    private IEnumerator DoTheFade(float current, float goal, float time, Color color)
    {
        // 计划达到目标密度的游戏时间点，单位秒，与 Time.time 使用同一时间基准。
        float endTime = Time.time + time;
        // 固定保存起点，循环内没有更新；因此起终值相同时不执行循环，不同时持续执行。
        var value = current;
        while (value != goal)
        {
            // 根据剩余秒数换算本帧进度；超过结束时间后可大于 1，Lerp 会把进度限制到 0～1。
            float t = 1f - (endTime - Time.time) / time;
            // 本帧要应用到场景的雾密度；它没有写回上面的循环判断变量 value。
            var newVal = Mathf.Lerp(current, goal, t);
            RenderSettings.fogDensity = newVal;
            RenderSettings.fogColor = color;
            yield return null;

        }
    }

    /// <summary>
    /// 每帧把全局雾颜色从指定起点插值到目标颜色，和密度协程同时运行。
    /// 起终颜色不同时，当前循环达到目标后仍继续写入，直到协程被外部停止。
    /// </summary>
    /// <param name="color1">颜色的固定插值起点，由进入或离开事件传入配置值。</param>
    /// <param name="color2">颜色的插值终点。</param>
    /// <param name="time2">计划的渐变时长，单位为游戏秒；计算进度时直接用它作除数。</param>
    /// <returns>逐帧执行颜色渐变的迭代器，由触发事件启动和停止。</returns>
    private IEnumerator ChangeColor(Color color1, Color color2, float time2)
    {
        // 计划达到目标颜色的游戏时间点，单位秒，受 Time.time 的时间缩放影响。
        float endTime = Time.time + time2;
        // 只保存初始颜色，循环内不更新；起终颜色相同时不执行，不同时无法据此自然结束。
        var value = color1;
        while (value != color2)
        {
            // 本帧的时间进度；达到或超过 1 后，Color.Lerp 持续返回目标颜色。
            float t = 1f - (endTime - Time.time) / time2;
            // 当前帧按进度得到的雾颜色，写入整个场景共用的 RenderSettings。
            var colorA = Color.Lerp(color1, color2, t);
            // 这里只更新颜色，密度由 DoTheFade 处理；两个协程都会写全局雾颜色。
            RenderSettings.fogColor = colorA;
            yield return null;

        }
    }
}

