using Cinemachine;
using DG.Tweening;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * Title:
 * Description:
 */


public class CameraMgr : MonoBehaviour
{
    [SerializeField] private float _yAxisSpeed = 0.1f;
    [SerializeField] private float _xAxisSpeed = 10f;
    [SerializeField] private float _delayTimer = 0.6f;
    private RoleCtrlBase  _roleMainCtrl;
    public static CameraMgr Instance;
    private CinemachineFreeLook _cinemachine;

    private Mouse _mouse;

    private void Awake()
    {
        Instance = this;
        _cinemachine = GetComponent<CinemachineFreeLook>();
        _mouse = Mouse.current;
        // SetOrbites(12);
    }

    public void InitCamera( RoleCtrlBase roleCtrl )
    {
        if (roleCtrl != null)
        {
            _cinemachine.Follow = roleCtrl.transform;
            _cinemachine.LookAt = roleCtrl.LookAt;
            _roleMainCtrl = roleCtrl;
        }
        SetOrbites(12);
    }

    private void Update()
    {
        if(_isOpenRoleAtttWindow ==true)return;

        //摄像机缩放功能
        //1.获取到鼠标滚轮事件 
        if (_mouse.scroll.y.ReadValue() != 0)
        {
            SetOrbites(_cinemachine.m_Orbits[0].m_Height - _mouse.scroll.y.ReadValue()*2 );
        }


        //摄像机旋转功能 替代Mouse Y
        CameraRotation();

    }

    /// <summary>
    /// 摄像机旋转功能
    /// </summary>
    private void CameraRotation()
    {

        //鼠标右键按下时
        if (_mouse.rightButton.isPressed)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (_mouse.delta.ReadValue() != Vector2.zero)
            {
                _cinemachine.m_YAxis.Value -= _mouse.delta.ReadValue().y * Time.deltaTime * _yAxisSpeed;
                _cinemachine.m_XAxis.Value += _mouse.delta.ReadValue().x * Time.deltaTime * _xAxisSpeed;
            }
        }
        //鼠标抬起时
        if (_mouse.rightButton.wasReleasedThisFrame)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

    }


 

    /// <summary>
    /// 设置轨道数据
    /// </summary>
    /// <param name="offset"></param>
    private void SetOrbites(float offset)
    {

        //限制缩放值
        offset = Mathf.Clamp(offset, 3f, 50f);

        //Top Rig
        DOTween.To(() => _cinemachine.m_Orbits[0].m_Height, x => _cinemachine.m_Orbits[0].m_Height = x, offset, _delayTimer);
        DOTween.To(() => _cinemachine.m_Orbits[0].m_Radius, x => _cinemachine.m_Orbits[0].m_Radius = x, offset * 0.25f, _delayTimer);


        DOTween.To(() => _cinemachine.m_Orbits[1].m_Height, x => _cinemachine.m_Orbits[1].m_Height = x, offset * 0.5f, _delayTimer);
        DOTween.To(() => _cinemachine.m_Orbits[1].m_Radius, x => _cinemachine.m_Orbits[1].m_Radius = x, offset * 0.7f, _delayTimer);


        DOTween.To(() => _cinemachine.m_Orbits[2].m_Radius, x => _cinemachine.m_Orbits[2].m_Radius = x, offset * 0.15f, _delayTimer);

    }

    #region 设置角色属性window视角

    private float _oldOrbitesOffet;
    private float _oldeulerAnglesY;
    private float _oldcimeYaxis;
    private RectTransform _roleAtttWindowRectTransform;
    
    [HideInInspector]public bool _isOpenRoleAtttWindow;
    //是否打开了背包背包window
    private bool isOpenKnapsackWindow;
    /// <summary>
    /// 设置角色属性window视角
    /// </summary>
    /// <param name="roleAtttWindow"></param>
    public void RoleAttrWindowAngle(WindowBase roleAtttWindow)
    {
        if (roleAtttWindow.gameObject.activeSelf)
        {
            _isOpenRoleAtttWindow = true;
            _roleAtttWindowRectTransform =roleAtttWindow.transform as RectTransform;
            _oldOrbitesOffet = _cinemachine.m_Orbits[0].m_Height;
            _oldeulerAnglesY = Camera.main.transform.eulerAngles.y;
            _oldcimeYaxis = _cinemachine.m_YAxis.Value;
            if (isOpenKnapsackWindow)
                
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(0,0.85f),460);
            }
            else
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(1.1f,0.85f),800);
            }
            
        }
        else
        {
            RecoverAngle();

        }
    }

    /// <summary>
    /// 恢复视角
    /// </summary>
    public void RecoverAngle()
    {
        if (_isOpenRoleAtttWindow)
        {
            _isOpenRoleAtttWindow = false;
            ToggleAngle(_oldOrbitesOffet, _oldeulerAnglesY, _oldcimeYaxis,new Vector3(0,1.17f),460);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cinemOffset">轨道</param>
    /// <param name="roleRotaY"></param>
    /// <param name="cinemYAxis"></param>
    private void ToggleAngle(float cinemOffset, float roleRotaY, float cinemYAxis,Vector3 lookatpos,float roleAttrWindowOffset)
    {
        //设置轨道
        SetOrbites(cinemOffset);
        //设置角色选择，让角色正对摄像机 Camera.main.transform.eulerAngles.y
        _roleMainCtrl.transform.eulerAngles = new Vector3(0, roleRotaY, 0);
        //设置相机旋转x轴旋转
        _cinemachine.m_YAxis.Value = cinemYAxis;
        
        //设置Lookat点属性
        _cinemachine.LookAt.localPosition = lookatpos;

        _roleAtttWindowRectTransform.DOAnchorPos(new Vector2(roleAttrWindowOffset, 0), _delayTimer);
    }

    public void KnapsackWindowAngle(WindowBase window)
    {
        isOpenKnapsackWindow = window.gameObject.activeSelf;
        if (_isOpenRoleAtttWindow)//如果角色属性wind已经打开
        {
            if (isOpenKnapsackWindow) //在打开背包
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(0,0.85f),460);
            }
            else
            {
                ToggleAngle(8, Camera.main.transform.eulerAngles.y+180, 0.2f,new Vector3(1.1f,0.85f),800);
            }
        }
    }
    
    #endregion
    
}
