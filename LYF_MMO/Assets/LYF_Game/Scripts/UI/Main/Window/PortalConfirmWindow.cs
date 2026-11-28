using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地面传送阵的目的地确认窗：只显示目标地图名，并把玩家的选择交给主界面控制器。
/// 窗口挂在主城提示层，由 MainView 创建并登记到 WindowType，自身不处理协议也不切场景。
/// </summary>
public sealed class PortalConfirmWindow : WindowBase
{
   /// <summary>目的地说明文字，显示“要前往『目标地图』吗”，目标名由控制器按对端门写入。</summary>
   [SerializeField, Header("目的地说明")] private TMP_Text _message;

   /// <summary>确认传送按钮，点击后关闭窗口并向控制器发出确认事件。</summary>
   [SerializeField, Header("确认传送按钮")] private Button _confirmButton;

   /// <summary>取消按钮，关闭窗口后角色留在原地。</summary>
   [SerializeField, Header("取消按钮")] private Button _cancelButton;

   /// <summary>右上角关闭按钮，作用与取消一致。</summary>
   [SerializeField, Header("关闭按钮")] private Button _closeButton;

   /// <summary>玩家确认传送时通知控制器；取消和关闭不触发，控制器据此决定是否发门号。</summary>
   public event System.Action Confirmed;

   /// <summary>绑定三个按钮的点击事件；由 MainView 在创建窗口实例时调用一次。</summary>
   public override void InitWindow()
   {
      _confirmButton.onClick.AddListener(OnConfirmClicked);
      _cancelButton.onClick.AddListener(OnCancelClicked);
      _closeButton.onClick.AddListener(OnCancelClicked);
   }

   /// <summary>写入本次目的地名称；同一条走廊通往不同地图时由调用方各自传入。</summary>
   /// <param name="destinationName">对端门所在地图的展示名，为空时显示“未知地点”。</param>
   public void SetDestination(string destinationName)
   {
      // 地图名缺失时仍要给出可读提示，避免出现半句话。
      if (string.IsNullOrEmpty(destinationName))
      {
         _message.text = "要前往『未知地点』吗？";
         return;
      }
      _message.text = "要前往『" + destinationName + "』吗？";
   }

   /// <summary>确认按钮：先收起窗口，再通知控制器发送本次门号，避免连点重复触发。</summary>
   private void OnConfirmClicked()
   {
      CloseWindow();
      Confirmed?.Invoke();
   }

   /// <summary>取消和右上关闭共用：只收起窗口，角色留在原地。</summary>
   private void OnCancelClicked()
   {
      CloseWindow();
   }
}
