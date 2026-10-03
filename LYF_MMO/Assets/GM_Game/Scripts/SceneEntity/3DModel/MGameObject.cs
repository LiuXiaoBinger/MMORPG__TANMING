using UnityEngine;

/// <summary>
/// Unity GameObject 封装器。
/// 模型对象未加载时缓存变换数据（位置、旋转、缩放、激活状态等），
/// 加载完成后自动同步到真实 Transform，解决异步加载期间属性写入丢失的问题。
/// </summary>
public sealed class MGameObject
{
    /// <summary>绑定的 Unity GameObject 实例。</summary>
    private GameObject _entityObject;
    /// <summary>缓存的 Transform 引用，避免每次访问都 GetComponent。</summary>
    private Transform _transform;
    /// <summary>缓存的世界坐标位置（未绑定 Transform 时使用）。</summary>
    private Vector3 _position;
    /// <summary>缓存的世界旋转（未绑定 Transform 时使用）。</summary>
    private Quaternion _rotation = Quaternion.identity;
    /// <summary>缓存的本地坐标位置（未绑定 Transform 时使用）。</summary>
    private Vector3 _localPosition;
    /// <summary>缓存的本地旋转（未绑定 Transform 时使用）。</summary>
    private Quaternion _localRotation = Quaternion.identity;
    /// <summary>缓存的缩放（未绑定 Transform 时使用）。</summary>
    private Vector3 _scale = Vector3.one;
    /// <summary>缓存的激活状态（未绑定 GameObject 时使用）。</summary>
    private bool _active = true;
    /// <summary>缓存的 Layer（未绑定 GameObject 时使用）。</summary>
    private int _layer;
    /// <summary>缓存的对象名称（未绑定 GameObject 时使用）。</summary>
    private string _name = string.Empty;
    /// <summary>是否有缓存的世界位置数据。</summary>
    private bool _hasWorldPosition;
    /// <summary>是否有缓存的世界旋转数据。</summary>
    private bool _hasWorldRotation;
    /// <summary>是否有缓存的本地位置数据。</summary>
    private bool _hasLocalPosition;
    /// <summary>是否有缓存的本地旋转数据。</summary>
    private bool _hasLocalRotation;
    /// <summary>是否有缓存的缩放数据。</summary>
    private bool _hasScale;

    /// <summary>获取底层 Unity GameObject 对象。</summary>
    public GameObject NativeObject { get { return _entityObject; } }
    /// <summary>获取底层 Transform 组件。</summary>
    public Transform Transform { get { return _transform; } }
    /// <summary>获取是否已绑定有效的 Unity 对象。</summary>
    public bool IsValid { get { return _entityObject != null; } }

    /// <summary>世界坐标位置，未绑定 Transform 时读写缓存值。</summary>
    public Vector3 Position
    {
        get { if (_transform == null) return _position; return _transform.position; }
        set { _position = value; _hasWorldPosition = true; if (_transform != null) _transform.position = value; }
    }

    /// <summary>世界旋转，未绑定 Transform 时读写缓存值。</summary>
    public Quaternion Rotation
    {
        get { if (_transform == null) return _rotation; return _transform.rotation; }
        set { _rotation = value; _hasWorldRotation = true; if (_transform != null) _transform.rotation = value; }
    }

    /// <summary>本地坐标位置，未绑定 Transform 时读写缓存值。</summary>
    public Vector3 LocalPosition
    {
        get { if (_transform == null) return _localPosition; return _transform.localPosition; }
        set { _localPosition = value; _hasLocalPosition = true; if (_transform != null) _transform.localPosition = value; }
    }

    /// <summary>本地旋转，未绑定 Transform 时读写缓存值。</summary>
    public Quaternion LocalRotation
    {
        get { if (_transform == null) return _localRotation; return _transform.localRotation; }
        set { _localRotation = value; _hasLocalRotation = true; if (_transform != null) _transform.localRotation = value; }
    }

    /// <summary>缩放，未绑定 Transform 时读写缓存值。</summary>
    public Vector3 Scale
    {
        get { if (_transform == null) return _scale; return _transform.localScale; }
        set { _scale = value; _hasScale = true; if (_transform != null) _transform.localScale = value; }
    }

    /// <summary>激活状态，未绑定 GameObject 时读写缓存值。</summary>
    public bool IsActive
    {
        get { if (_entityObject == null) return _active; return _entityObject.activeSelf; }
        set { _active = value; if (_entityObject != null) _entityObject.SetActive(value); }
    }

    /// <summary>Layer 层级，未绑定 GameObject 时读写缓存值。</summary>
    public int Layer
    {
        get { if (_entityObject == null) return _layer; return _entityObject.layer; }
        set { _layer = value; if (_entityObject != null) _entityObject.layer = value; }
    }

    /// <summary>对象名称，未绑定 GameObject 时读写缓存值。</summary>
    public string Name
    {
        get { if (_entityObject == null) return _name; return _entityObject.name; }
        set { _name = value; if (_entityObject != null) _entityObject.name = value; }
    }

    /// <summary>
    /// 前向向量：读取为旋转的 forward；设置时通过 LookRotation 改变朝向
    /// </summary>
    public Vector3 Forward
    {
        get { return Rotation * Vector3.forward; }
        set 
        {
            if (value.sqrMagnitude > float.Epsilon)
            {
                Rotation = Quaternion.LookRotation(value);
            }
        }

    }

    /// <summary>
    /// 绑定已实例化的 Unity 对象，并同步之前缓存的变换属性。
    /// 解决异步加载期间写入的位置/旋转/缩放在加载完成后丢失的问题。
    /// </summary>
    /// <param name="nativeObject">要绑定的 Unity GameObject。</param>
    public void SetGameObject(GameObject nativeObject)
    {
        // 仅解除旧原生引用，不清理异步加载期间已经缓存的变换。
        _entityObject = null;
        _transform = null;
        _entityObject = nativeObject;
        if (_entityObject == null)
        {
            return;
        }
        _transform = _entityObject.transform;
        if (!string.IsNullOrEmpty(_name))
        {
            _entityObject.name = _name;
        }
        _entityObject.layer = _layer;
        // 按缓存标记恢复各项变换数据
        if (_hasWorldPosition) _transform.position = _position;
        if (_hasWorldRotation) _transform.rotation = _rotation;
        if (_hasLocalPosition) _transform.localPosition = _localPosition;
        if (_hasLocalRotation) _transform.localRotation = _localRotation;
        if (_hasScale) _transform.localScale = _scale;
        _entityObject.SetActive(_active);
    }

    /// <summary>
    /// 外部实例绑定前保存其当前变换，防止模型包装器把对象重置到原点。
    /// 用于场景中已有预制体直接绑定的场景，保留原始位置和旋转。
    /// </summary>
    /// <param name="nativeObject">要捕获变换的 Unity GameObject。</param>
    public void CaptureNativeTransform(GameObject nativeObject)
    {
        if (nativeObject == null) return;
        Transform nativeTransform = nativeObject.transform;
        _position = nativeTransform.position;
        _rotation = nativeTransform.rotation;
        _localPosition = nativeTransform.localPosition;
        _localRotation = nativeTransform.localRotation;
        _scale = nativeTransform.localScale;
        _hasWorldPosition = true;
        _hasWorldRotation = true;
        _hasLocalPosition = true;
        _hasLocalRotation = true;
        _hasScale = true;
    }

    /// <summary>设置父级节点，保持本地变换不变。</summary>
    /// <param name="parent">父级 GameObject，为 null 时脱离父级。</param>
    public void SetParent(GameObject parent)
    {
        if (_transform != null)
        {
            Transform parentTransform = null;
            if (parent != null)
            {
                parentTransform = parent.transform;
            }
            _transform.SetParent(parentTransform, false);
        }
    }

    /// <summary>
    /// 回收模型对象。
    /// </summary>
    /// <param name="destroyNative">为 true 时销毁场景实例，为 false 仅解除引用。</param>
    public void Release(bool destroyNative)
    {
        if (_entityObject != null && destroyNative)
        {
            Object.Destroy(_entityObject);
        }
        _entityObject = null;
        _transform = null;
        _position = Vector3.zero;
        _rotation = Quaternion.identity;
        _localPosition = Vector3.zero;
        _localRotation = Quaternion.identity;
        _scale = Vector3.one;
        _active = true;
        _layer = 0;
        _name = string.Empty;
        _hasWorldPosition = false;
        _hasWorldRotation = false;
        _hasLocalPosition = false;
        _hasLocalRotation = false;
        _hasScale = false;
    }
}
