using UnityEngine;


    /// <summary>角色展示用相机控制：滚轮缩放并调整观察高度，左键拖动转角色，松开后逐渐停转。</summary>
    public class S_CameraController : MonoBehaviour
    {
        /// <summary>被鼠标拖动旋转的展示角色。</summary>
        public GameObject character;

        /// <summary>相机支架，通过局部 Y/Z 位置改变观察高度与距离。</summary>
        public Transform cameraHolder;

        /// <summary>滚轮输入转换为局部 Z 位移的倍率。</summary>
        public float zoomSpeed = 0.5f;

        /// <summary>鼠标水平输入转换为旋转角度的倍率，也用于松开后的减速。</summary>
        public float rotationSpeed = 3f;

        /// <summary>相机位置平滑接近目标的时间参数，单位为秒。</summary>
        public float smoothTime = 0.3F;

        /// <summary>期望的相机支架局部 Z 值，每帧按滚轮累加。</summary>
        private float currentZoom = 0f;

        /// <summary>相机支架局部 Z 的最小允许值。</summary>
        private float zoomMin = 0.5f;

        /// <summary>相机支架局部 Z 的最大允许值。</summary>
        private float zoomMax = 3f;

        /// <summary>SmoothDamp 保存的缩放速度，仅使用 z 分量。</summary>
        private Vector3 zoomVelocity = Vector3.zero;

        /// <summary>SmoothDamp 保存的高度变化速度，仅使用 y 分量。</summary>
        private Vector3 positionVelocity = Vector3.zero;

        /// <summary>遗留相对位置字段，当前逻辑未读取或赋值。</summary>
        private Vector3 initialRelativePosition;

        /// <summary>最近一次水平旋转量；松开鼠标后继续使用并逐渐衰减。</summary>
        private float lastRotateHorizontal;

        /// <summary>显示光标，并以支架现有局部 Z 位置作为初始缩放值。</summary>
        private void Start()
        {
            Cursor.visible = true;
            currentZoom = cameraHolder.localPosition.z;
        }

        /// <summary>每帧处理缩放、角色旋转和按键对应的光标状态。</summary>
        private void Update()
        {
            HandleZoom();
            HandleRotation();
            HandleCursorVisibility();
        }

        /// <summary>滚轮修改目标距离；距离接近最小值时把观察高度抬向头部，并平滑更新支架位置。</summary>
        void HandleZoom()
        {
            // 反向滚轮增量，符号决定目标 Z 增大或减小。
            float scroll = -Input.GetAxis("Mouse ScrollWheel");
            currentZoom += scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, zoomMin, zoomMax);

            // 本帧平滑后的局部 Z，后续作为实际相机距离。
            float targetZ = Mathf.SmoothDamp(cameraHolder.localPosition.z, currentZoom, ref zoomVelocity.z, smoothTime);
            // 目标 Z 位于最小值时为 1，位于最大值时为 0，用来混合观察高度。
            float zoomFactorInverse = 1 - ((currentZoom - zoomMin) / (zoomMax - zoomMin));
            // 缩放到最小 Z 时的局部观察高度。
            float headFocusY = 1.63f;
            // 缩放到最大 Z 时的局部观察高度。
            float defaultCameraY = 1.128f;
            // 先按缩放比例混合高度，再通过 SmoothDamp 平滑。
            float targetY = Mathf.Lerp(defaultCameraY, headFocusY, zoomFactorInverse);

            targetY = Mathf.SmoothDamp(cameraHolder.localPosition.y, targetY, ref positionVelocity.y, smoothTime);

            cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, targetY, targetZ);
        }

        /// <summary>左键按住时读取鼠标水平移动旋转角色；松开后沿上次方向旋转并逐渐减速。</summary>
        void HandleRotation()
        {
            if (Input.GetMouseButton(0))
            {
                lastRotateHorizontal = -Input.GetAxis("Mouse X") * rotationSpeed;
                Quaternion rotation = Quaternion.Euler(0f, lastRotateHorizontal, 0f);
                character.transform.rotation *= rotation;
            }
            else if (!Mathf.Approximately(lastRotateHorizontal, 0))
            {
                Quaternion rotation = Quaternion.Euler(0f, lastRotateHorizontal, 0f);
                character.transform.rotation *= rotation;
                lastRotateHorizontal = Mathf.Lerp(lastRotateHorizontal, 0, Time.deltaTime * rotationSpeed * 3f);
            }
        }

        /// <summary>按住任一鼠标键时隐藏并锁住光标，全部松开后恢复。</summary>
        void HandleCursorVisibility()
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

