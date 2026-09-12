using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/**
* Title:
* Descrpiton:
*/

public class BuyDialog : MonoBehaviour
{
   [SerializeField, Header("商品图标")] private Image _productIcon;
   [SerializeField, Header("商品名称")] private TMP_Text _productName;
   [SerializeField, Header("货币图标")] private Image _currencyIcon;
   [SerializeField, Header("货币名称和总价")] private TMP_Text _currencyText;
   [SerializeField, Header("购买数量输入框")] private TMP_InputField _quantityInput;
   [SerializeField, Header("减少数量按钮")] private Button _decreaseButton;
   [SerializeField, Header("增加数量按钮")] private Button _increaseButton;

   private Action<BuyDialog> _onClosed;
   private Action<int> _onConfirm;
   private int _unitPrice;
   private int _currencyType;
   private int _maxQuantity;
   private int _quantity = 1;
   private string _currencyName;
   private bool _isRefreshingQuantity;

   /// <summary>
   /// 由弹窗管理器登记关闭回调，用于在弹窗销毁时解除重复打开限制。
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

   public void CloseDialog()
    {
       Destroy(gameObject);
    }

   private void OnDestroy()
    {
      UnbindQuantityEvents();
      // 无论是关闭按钮还是其他流程销毁弹窗，都必须通知管理器释放锁定。
      Action<BuyDialog> onClosed = _onClosed;
      _onClosed = null;
      _onConfirm = null;
      onClosed?.Invoke(this);
    }

   private void Awake()
   {
      BindQuantityEvents();
   }

   //点击购买
    public void OnConfirmButtonClick()
    {
      if (_quantity < 1)
      {
         return;
      }

      // 购买是否成功由服务端校验，客户端只提交请求并关闭确认弹窗。
      Action<int> onConfirm = _onConfirm;
      int quantity = _quantity;
      CloseDialog();
      onConfirm?.Invoke(quantity);
   }

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

   private void OnDecreaseButtonClick()
   {
      ApplyQuantity(_quantity - 1);
   }

   private void OnIncreaseButtonClick()
   {
      ApplyQuantity(_quantity + 1);
   }

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

   private void OnQuantityInputEndEdit(string value)
   {
      if (!int.TryParse(value, out int quantity))
      {
         quantity = _quantity;
      }

      ApplyQuantity(quantity);
   }

   private void ApplyQuantity(int quantity)
   {
      _quantity = Mathf.Clamp(quantity, 1, _maxQuantity);
      RefreshQuantityUI();
   }

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

   private void RefreshTotalPrice()
   {
      if (_currencyText != null)
      {
         long totalPrice = (long)_unitPrice * _quantity;
         string category = _currencyType <= 0 ? "未知" : _currencyType.ToString();
         _currencyText.text = $"货币类别 {category} · {_currencyName} {totalPrice}";
      }
   }

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
}
