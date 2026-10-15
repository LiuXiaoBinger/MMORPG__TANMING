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
   /// <summary>跨场景 UI 根单例，Awake 赋值并 DontDestroyOnLoad。</summary>
   public static UIRoot Instance;

   /// <summary>登录视图引用。</summary>
   [SerializeField,Header("登录View")] private Loginview _loginview;

   /// <summary>登录控制器。</summary>
   public LoginCtrl LoginViewCtrl;

   [SerializeField,Header("创建角色相关View")] private CreateRoleView _createRoleView;

   public CreateRoleCtrl CreateRoleCtrl;

   [SerializeField,Header("主城相关View")] private MainView _mainView;

   public MainCtrl MainCtrl;

   [SerializeField,Header("加载界面")] private LoadingView _loadingView;

   [SerializeField, Header("点击特效")] private ParticleSystem _clichFX;

   [SerializeField, Header("画布")]  public Canvas _canvas;

   private void Awake()
   {
      Instance = this;
      DontDestroyOnLoad(Instance);

      InitCtrl();
   }

   private void Start()
   {
      _canvas = GetComponentInChildren<Canvas>();
      SceneMgr.Instance.Init(_loadingView);
   }

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
   /// 加载完主城场景再调用
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

   /// <summary>
   /// 注册主城ui相关事件
   /// </summary>
   public void RegisterMainUIKeyHandler()
   {
      if (MainCtrl != null)
      {
         MainCtrl.RegisterMainInput();
      }
   }

   public Camera GetUICamera()
   {
      if(_canvas!=null) return _canvas.worldCamera;
      return null;
   }

   public Vector2 ScreenPointToviewPoint(Vector2 screenPoint)
   {
      Vector2 pos;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, screenPoint, _canvas.worldCamera, out pos);
      return pos;
   }
}
