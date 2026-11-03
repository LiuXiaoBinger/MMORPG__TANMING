using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/**
* Title:
* Descrpiton:
*/

/// <summary>购买确认弹窗：维护购买数量、总价和加减按钮，确认时提交数量并销毁弹窗，购买结果由外部流程处理。</summary>
public class BuyDialog : MonoBehaviour
{

   #region 字段
   /// <summary>待购买商品图标，由外部加载后设置。</summary>
   [SerializeField, Header("商品图标")] private Image _productIcon;

   /// <summary>本次购买的商品名称。</summary>
   [SerializeField, Header("商品名称")] private TMP_Text _productName;

   /// <summary>支付货币图标，没有可用图标时隐藏。</summary>
   [SerializeField, Header("货币图标")] private Image _currencyIcon;

   /// <summary>合并显示货币类别、名称和按数量计算的总价。</summary>
   [SerializeField, Header("货币名称和总价")] private TMP_Text _currencyText;

   /// <summary>用户直接编辑的购买数量，结束编辑时回填有效范围内的数值。</summary>
   [SerializeField, Header("购买数量输入框")] private TMP_InputField _quantityInput;

   /// <summary>每次减少一件，当前为一件时不可点击。</summary>
   [SerializeField, Header("减少数量按钮")] private Button _decreaseButton;

   /// <summary>每次增加一件，到达数量上限时不可点击。</summary>
   [SerializeField, Header("增加数量按钮")] private Button _increaseButton;

   /// <summary>弹窗销毁通知，管理器据此解除重复打开限制。</summary>
   private Action<BuyDialog> _onClosed;

   /// <summary>确认购买的接收方，参数为本次数量。</summary>
   private Action<int> _onConfirm;

   /// <summary>每件商品的非负单价，计算总价时转为 long。</summary>
   private int _unitPrice;

   /// <summary>支付货币类别编号，不大于零时显示未知。</summary>
   private int _currencyType;

   /// <summary>本次允许选择的最大购买件数，初始化时至少为一。</summary>
   private int _maxQuantity;

   /// <summary>当前用于总价和确认的购买件数，范围为一到上限。</summary>
   private int _quantity = 1;

   /// <summary>支付货币的展示名称，空名称初始化为通用“货币”。</summary>
   private string _currencyName;

   /// <summary>正在由代码回填数量文本，输入变化回调此时不再反向更新。</summary>
   private bool _isRefreshingQuantity;
   #endregion

   #region 生命周期
   /// <summary>绑定数量输入与加减按钮事件。</summary>
   private void Awake()
   {
      BindQuantityEvents();
   }

   /// <summary>注销控件事件并通知关闭；销毁来自任何入口都会释放弹窗占用。</summary>
   private void OnDestroy()
    {
      UnbindQuantityEvents();
      // 无论是关闭按钮还是其他流程销毁弹窗，都必须通知管理器释放锁定。
      // 先保存再清空，保证销毁通知不继续持有控制器引用。
      Action<BuyDialog> onClosed = _onClosed;
      _onClosed = null;
      _onConfirm = null;
      onClosed?.Invoke(this);
    }

   /// <summary>
   /// 绑定关闭和确认回调，保存商品、单价、货币及数量上限，从一件开始刷新显示。
   /// </summary>
   public void Initialize(Action<BuyDialog> onClosed, Action<int> onConfirm,
      string productName, int unitPrice, int currencyType, string currencyName, int maxQuantity)
   {
      _onClosed = onClosed;
      _onConfirm = onConfirm;
      _unitPrice = Mathf.Max(0, unitPrice);
      _currencyType = currencyType;
      _currencyName = string.IsNullOrEmpty(currencyName) ? "货币" : currencyName;
      _maxQuantity = Mathf.Max(1, maxQuantity);
      _quantity = 1;

      if (_productName != null)
      {
         _productName.text = string.IsNullOrEmpty(productName) ? "未命名商品" : productName;
      }

      BindQuantityEvents();
      RefreshQuantityUI();
   }

   /// <summary>
   /// 兼容旧调用方，默认确认购买一件商品。
   /// </summary>
   public void Initialize(Action<BuyDialog> onClosed, Action onConfirm)
   {
      Initialize(onClosed, _ => onConfirm?.Invoke(), "未命名商品", 0, 0, "货币", 1);
   }
   #endregion

   /// <summary>销毁本次弹窗，最终由 OnDestroy 通知管理器关闭。</summary>
   public void CloseDialog()
    {
       Destroy(gameObject);
    }

   #region 界面刷新
   /// <summary>回填合法购买数量，并同步总价和加减按钮状态。</summary>
   private void RefreshQuantityUI()
   {
      if (_quantityInput != null)
      {
         _isRefreshingQuantity = true;
         _quantityInput.SetTextWithoutNotify(_quantity.ToString());
         _isRefreshingQuantity = false;
      }

      RefreshTotalPrice();
      RefreshQuantityButtons();
   }

   /// <summary>按单价乘当前数量计算总价并更新货币说明。</summary>
   private void RefreshTotalPrice()
   {
      if (_currencyText != null)
      {
         // 用 long 相乘，避免两项 int 的乘积先在 int 范围内溢出。
         long totalPrice = (long)_unitPrice * _quantity;
         // 无有效类别编号时只显示未知，不在这里推断货币类型。
         string category = _currencyType <= 0 ? "未知" : _currencyType.ToString();
         _currencyText.text = $"货币类别 {category} · {_currencyName} {totalPrice}";
      }
   }

   /// <summary>按一件下限与本次最大件数设置加减按钮可交互状态。</summary>
   private void RefreshQuantityButtons()
   {
      if (_decreaseButton != null)
      {
         _decreaseButton.interactable = _quantity > 1;
      }

      if (_increaseButton != null)
      {
         _increaseButton.interactable = _quantity < _maxQuantity;
      }
   }
   #endregion

   #region 交互响应
   //点击购买
    /// <summary>保存本次数量和回调，关闭弹窗后通知外部购买；这里不判断服务端购买成功。</summary>
    public void OnConfirmButtonClick()
    {
      if (_quantity < 1)
      {
         return;
      }

      // 购买是否成功由服务端校验，客户端只提交请求并关闭确认弹窗。
      // 销毁会清空字段，因此先保留本次确认接收方。
      Action<int> onConfirm = _onConfirm;
      // 此次确认的数量快照，不受销毁流程影响。
      int quantity = _quantity;
      CloseDialog();
      onConfirm?.Invoke(quantity);
   }

   /// <summary>将当前件数减一，再通过统一入口限制范围并刷新。</summary>
   private void OnDecreaseButtonClick()
   {
      ApplyQuantity(_quantity - 1);
   }

   /// <summary>将当前件数加一，再通过统一入口限制范围并刷新。</summary>
   private void OnIncreaseButtonClick()
   {
      ApplyQuantity(_quantity + 1);
   }

   /// <summary>输入中可解析的 value 立即更新有效数量和总价，不强行改写正在输入的文本。</summary>
   private void OnQuantityInputChanged(string value)
   {
      if (_isRefreshingQuantity || !int.TryParse(value, out int quantity))
      {
         return;
      }

      _quantity = Mathf.Clamp(quantity, 1, _maxQuantity);
      RefreshTotalPrice();
      RefreshQuantityButtons();
   }

   /// <summary>结束编辑后把 value 解析为数量；无法解析时沿用原数量，并回填合法文本。</summary>
   private void OnQuantityInputEndEdit(string value)
   {
      if (!int.TryParse(value, out int quantity))
      {
         quantity = _quantity;
      }

      ApplyQuantity(quantity);
   }
   #endregion

   #region 核心逻辑
   /// <summary>
   /// 设置商品图标；加载失败时保留预制体默认图。
   /// </summary>
   public void SetProductIcon(Sprite sprite)
   {
      if (_productIcon != null && sprite != null)
      {
         _productIcon.sprite = sprite;
         _productIcon.gameObject.SetActive(true);
      }
   }

   /// <summary>
   /// 设置货币图标，没有配置图标时隐藏控件。
   /// </summary>
   public void SetCurrencyIcon(Sprite sprite)
   {
      if (_currencyIcon == null)
      {
         return;
      }

      _currencyIcon.sprite = sprite;
      _currencyIcon.gameObject.SetActive(sprite != null);
   }

   /// <summary>先移除再添加自己的监听，使 Awake 与 Initialize 重复调用时不叠加回调。</summary>
   private void BindQuantityEvents()
   {
      if (_decreaseButton != null)
      {
         _decreaseButton.onClick.RemoveListener(OnDecreaseButtonClick);
         _decreaseButton.onClick.AddListener(OnDecreaseButtonClick);
      }

      if (_increaseButton != null)
      {
         _increaseButton.onClick.RemoveListener(OnIncreaseButtonClick);
         _increaseButton.onClick.AddListener(OnIncreaseButtonClick);
      }

      if (_quantityInput != null)
      {
         _quantityInput.onValueChanged.RemoveListener(OnQuantityInputChanged);
         _quantityInput.onEndEdit.RemoveListener(OnQuantityInputEndEdit);
         _quantityInput.onValueChanged.AddListener(OnQuantityInputChanged);
         _quantityInput.onEndEdit.AddListener(OnQuantityInputEndEdit);
      }
   }

   /// <summary>移除本弹窗添加的数量输入与加减监听。</summary>
   private void UnbindQuantityEvents()
   {
      if (_decreaseButton != null)
      {
         _decreaseButton.onClick.RemoveListener(OnDecreaseButtonClick);
      }

      if (_increaseButton != null)
      {
         _increaseButton.onClick.RemoveListener(OnIncreaseButtonClick);
      }

      if (_quantityInput != null)
      {
         _quantityInput.onValueChanged.RemoveListener(OnQuantityInputChanged);
         _quantityInput.onEndEdit.RemoveListener(OnQuantityInputEndEdit);
      }
   }

   /// <summary>把 quantity 限制到一件至上限，再同步所有数量相关显示。</summary>
   private void ApplyQuantity(int quantity)
   {
      _quantity = Mathf.Clamp(quantity, 1, _maxQuantity);
      RefreshQuantityUI();
   }
   #endregion
}
