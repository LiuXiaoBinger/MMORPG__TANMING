using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/**
* Title:UI和控制器管理类
* Descrpiton:管理所有ui和控制器
*/

/// <summary>
/// UI 总根节点：作为跨场景单例持有各业务视图与对应控制器，负责控制器的创建、重复初始化前释放与销毁，
/// 并处理鼠标点击特效与屏幕坐标到画布本地坐标的换算。
/// </summary>
public class UIRoot : MonoBehaviour
{

   #region 字段
   /// <summary>跨场景 UI 根单例，Awake 赋值并 DontDestroyOnLoad。</summary>
   public static UIRoot Instance;

   /// <summary>登录视图引用。</summary>
   [SerializeField,Header("登录View")] private Loginview _loginview;

   /// <summary>登录控制器。</summary>
   public LoginCtrl LoginViewCtrl;

   /// <summary>创建和选择角色的视图，交给 CreateRoleCtrl 管理交互。</summary>
   [SerializeField,Header("创建角色相关View")] private CreateRoleView _createRoleView;

   /// <summary>当前创建角色控制器，根节点销毁时释放其事件和网络订阅。</summary>
   public CreateRoleCtrl CreateRoleCtrl;

   /// <summary>主城功能界面的 View 引用，主城加载完成后初始化控制器。</summary>
   [SerializeField,Header("主城相关View")] private MainView _mainView;

   /// <summary>当前主城控制器；再次进入主城初始化时先释放旧实例。</summary>
   public MainCtrl MainCtrl;

   /// <summary>提供给 SceneMgr 的场景加载进度界面。</summary>
   [SerializeField,Header("加载界面")] private LoadingView _loadingView;

   /// <summary>鼠标左键点击 UI 时，在画布局部位置播放的粒子特效。</summary>
   [SerializeField, Header("点击特效")] private ParticleSystem _clichFX;

   /// <summary>用于坐标换算和 UI 相机查询的画布，Start 从子节点重新获取。</summary>
   [SerializeField, Header("画布")]  public Canvas _canvas;
   #endregion

   #region 初始化与事件注册
   /// <summary>设置全局入口并保留 UI 根跨场景存在，创建登录和角色选择控制器。</summary>
   private void Awake()
   {
      Instance = this;
      DontDestroyOnLoad(Instance);

      InitCtrl();
   }

   /// <summary>找到画布，并把加载进度 View 交给场景管理器。</summary>
   private void Start()
   {
      _canvas = GetComponentInChildren<Canvas>();
      SceneMgr.Instance.Init(_loadingView);
   }

   /// <summary>只在鼠标左键本帧按下且指针位于 UI 上时播放点击粒子。</summary>
   private void Update()
   {
      //输入系统
      //判断鼠标左键按下
      if (Mouse.current.leftButton.wasPressedThisFrame)
      {
         
         //判断点击的是否是ui
         if (EventSystem.current.IsPointerOverGameObject())
         {
            Debug.Log("鼠标点击");
            _clichFX.transform.localPosition = ScreenPointToviewPoint(Mouse.current.position.ReadValue());
            _clichFX.Play();
         }
      }
   }

   /// <summary>释放本根节点创建的控制器；仅当静态入口仍指向自己时清空入口。</summary>
   private void OnDestroy()
   {
      if (LoginViewCtrl != null)
      {
         LoginViewCtrl.Dispose();
      }
      if (CreateRoleCtrl != null)
      {
         CreateRoleCtrl.Dispose();
      }
      if (MainCtrl != null)
      {
         MainCtrl.Dispose();
      }
      if (Instance == this)
      {
         Instance = null;
      }
   }

   /// <summary>
   /// 让已存在的主城控制器注册快捷键和其他主城输入。
   /// </summary>
   public void RegisterMainUIKeyHandler()
   {
      if (MainCtrl != null)
      {
         MainCtrl.RegisterMainInput();
      }
   }
   #endregion

   /// <summary>返回画布使用的相机；画布尚未取得时返回空。</summary>
   public Camera GetUICamera()
   {
      if(_canvas!=null) return _canvas.worldCamera;
      return null;
   }

   #region 核心逻辑
   /// <summary>为已配置的登录、角色选择 View 创建控制器，主城控制器留到场景加载后创建。</summary>
   private void InitCtrl()
   {
       if (_loginview != null)
       {
          LoginViewCtrl = new LoginCtrl(_loginview);
       }
      
      if(_createRoleView != null) 
         CreateRoleCtrl = new CreateRoleCtrl(_createRoleView);
   }

   /// <summary>
   /// 主城场景加载完成后，替换旧主城控制器、注册输入并显示主城 View。
   /// </summary>
   public void InitMainCtrl()
   {
       if (_mainView != null)
       {
          if (MainCtrl != null)
          {
             // 场景重复初始化前释放旧控制器，避免协议和输入事件重复注册。
             MainCtrl.Dispose();
          }
          MainCtrl = new MainCtrl(_mainView);
          MainCtrl.RegisterMainInput();
          _mainView.Show();
      }
   }

   /// <summary>将屏幕像素坐标 screenPoint 换算为根画布 RectTransform 的局部坐标，供点击特效定位。</summary>
   public Vector2 ScreenPointToviewPoint(Vector2 screenPoint)
   {
      // 换算得到的画布局部坐标，不是世界空间坐标。
      Vector2 pos;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, screenPoint, _canvas.worldCamera, out pos);
      return pos;
   }
   #endregion
}
