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

public class UIRoot : MonoBehaviour
{
   public static UIRoot Instance;
   
   [SerializeField,Header("登录View")] private Loginview _loginview;
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

   private void InitCtrl()
   {
      if(_loginview != null) ;
         LoginViewCtrl = new LoginCtrl(_loginview);
      
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
         
         MainCtrl = new MainCtrl(_mainView);
         
         _mainView.Show();
      }
   }
   /// <summary>
   /// 注册主城ui相关事件
   /// </summary>
   public void RegisterMainUIKeyHandler()
   {
      PlayerInputCtr.Instance.MainUIKeyHandler += MainCtrl.MainUIKeyHandler;
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
