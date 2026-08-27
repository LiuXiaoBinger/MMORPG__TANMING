using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

/**
* Title:
* Descrpiton:
*/

public class SceneMgr : Singleton<SceneMgr>
{
   //当前场景
   public SceneType _currentSceneType;
   private LoadingView _loadingView;
   private IDisposable _obs;

   public void Init(LoadingView loadingView)
   {
      _loadingView = loadingView;
   }
   
   public void LoadSceneMode(SceneType  sceneType,Action callback=null)
   {
      _currentSceneType = sceneType;
      
      //异步加载场景
      SceneOperationHandle handle = Global.Instance.YooPackage.LoadSceneAsync($"{ConstDefine.ScenePath}" +
         $"{sceneType.ToString()}");
      
      if (handle != null && _loadingView != null)
      {
         //设置ui在最前面
         _loadingView.transform.SetAsLastSibling();
         _loadingView.Show();
         
         //设置加载进度
         _obs= Observable.EveryUpdate().Subscribe(_ =>
         {
            _loadingView.RefreshUI(handle.Progress,$"加载场景中::{handle.Progress*100}");
            
            //场景加载完毕
            if (handle.Progress >= 1)
            {
               callback?.Invoke();
               _loadingView.Show(false);
               _obs.Dispose();
            }
         });
      }
   }
}
