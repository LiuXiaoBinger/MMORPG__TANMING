using UnityEngine;

/* 开发笔记：记录项目中常见 Unity 组件和库的基本用途。本类没有运行逻辑。
     */


/// <summary>仅容纳开发笔记的空类，不会初始化插件或控制游戏行为。</summary>
public class Document {

    #region Camera

    /* Camera 决定从哪里看场景、哪些层可见，以及画面输出到屏幕还是纹理。
     * Clear Flags 与 Background 决定清屏方式和背景色；Culling Mask 筛选可见层。
     * Perspective 是近大远小的透视；Orthographic 不随距离改变物体投影大小。
     * Field of View 控制透视视角，Size 是正交相机视野的竖直半尺寸。
     * Clipping Planes 是可见距离，Viewport Rect 是相机占用的屏幕区域。
     * 视锥剔除和遮挡剔除用于跳过不可见物体；透明物体排序是另外的问题。
     */
    #endregion

    #region Canvas

    /* Canvas 是 UGUI 界面的绘制容器。
     * Screen Space - Overlay 直接覆盖屏幕，不需要相机。
     * Screen Space - Camera 由指定相机绘制，界面位于该相机前方的平面。
     * World Space 将界面放进三维场景，可用于角色头顶文字或场景内面板。
     */

    #endregion

    #region UI适配

    /* 不同分辨率下的界面布局由 Canvas Scaler 与 RectTransform 一起决定。
     * Constant Pixel Size 使用固定像素尺寸，可再乘统一缩放系数。
     * Scale with Screen Size 按参考分辨率缩放，需要选择宽高匹配方式。
     * Constant Physical Size 依赖屏幕 DPI；设备信息不准时物理尺寸也会不准。
     * RectTransform 的锚点决定控件跟随父节点哪一部分，边距决定具体位置。
     */

    #endregion

    #region Prefab

    /* Prefab 是对象及组件配置的模板，同一个资源可以创建多个场景实例。
     * 修改预制体会影响仍继承该属性的实例；实例自己覆盖的属性不会自动变回模板值。
     * 本项目的窗口、角色和特效都通过预制体保存组件及资源引用。
     */

    #endregion

    #region MonoBehaviour的生命周期函数

    /* MonoBehaviour 常用回调：
     * Awake 在实例初始化时执行，用于读取组件和设置自身状态。
     * OnEnable 在组件启用且对象激活时执行；反复启用会反复执行。
     * Start 在首次 Update 前执行一次，要求组件已启用。
     * FixedUpdate 按固定时间步执行，间隔由项目设置决定；Update 每帧执行。
     * LateUpdate 在本帧 Update 后执行，常用于相机跟随。
     * OnDisable 用于停止订阅等工作；OnDestroy 用于销毁清理。
     * OnApplicationQuit 是退出通知，不应据此假定所有平台一定完成存档。
     */

    #endregion

    #region SerializeField

    /* SerializeField 让符合 Unity 序列化规则的私有字段也能保存在场景或资源中，
     * 并显示在 Inspector。它本身不会把运行时进度自动保存成游戏存档。
     */

    #endregion

    #region DoTween

    /* DOTween 用一段时间把位置、大小、颜色等值从起点变化到目标值。
     * Sequence 可组合多个动画，Ease 控制变化速度曲线，Loop 负责循环。
     * 界面销毁或重新使用时，要按持有方的生命周期处理仍在运行的动画。
     */

    #endregion

    #region  Unity中的输入系统

    /* 项目可通过 UnityEngine.Input 读取键鼠状态，也可使用 Input System 的动作配置。
     * 前者常在 Update 中查询按键；后者可将多个设备映射成同一个动作。
     * 是否启用某套输入系统由项目设置和实际控制器决定，不由这份笔记决定。
     */


    #endregion

    #region HybridCLR简介

    /* HybridCLR 用于在 IL2CPP 程序中支持加载和执行部分 C# 程序集。
     * 它把预先编译的代码与解释执行配合使用；是否启用、如何打包要看项目构建配置。
     * 本笔记不表示当前发布包已经完成热更新验证。
     */

    #endregion

    #region YooAsset简介


    /* YooAsset 管理资源包、依赖和加载句柄，项目的 ResourceMgr 在它上面提供加载入口。
     * 异步加载把等待过程交给框架；同步加载会等待结果。
     * 资源能否卸载取决于引用和句柄的释放，不能只因窗口隐藏就认定内存已释放。
     */

    #endregion

    #region UniRx
    /* UniRx 把按键、计时或其他事件表示成可以订阅的数据流。
     * 可用操作符筛选、组合或延迟这些事件。
     * 订阅通常需要随对象生命周期释放，避免对象结束后仍收到回调。
     */

    #endregion

}
