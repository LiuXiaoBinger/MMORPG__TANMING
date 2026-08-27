using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HighlightPlus
{

    [RequireComponent(typeof(HighlightEffect))]
    [DefaultExecutionOrder(100)]
    public class HighlightManager : MonoBehaviour
    {

        private RoleCtrlBase _mainRole;
        private RoleCtrlBase _selectRole;

        [Tooltip("Enables highlight when pointer is over this object.")]
        [SerializeField]
        bool _highlightOnHover = true;

        public bool highlightOnHover
        {
            get { return _highlightOnHover; }
            set
            {
                if (_highlightOnHover != value)
                {
                    _highlightOnHover = value;
                    if (!_highlightOnHover)
                    {
                        if (currentEffect != null)
                        {
                            Highlight(false);
                        }
                    }

                }
            }
        }

        public LayerMask layerMask = -1;
        public Camera raycastCamera;
        public RayCastSource raycastSource = RayCastSource.MousePosition;
        [Tooltip("Minimum distance for target.")]
        public float minDistance;
        [Tooltip("Maximum distance for target. 0 = infinity")]
        public float maxDistance;
        [Tooltip("Blocks interaction if pointer is over an UI element")]
        public bool respectUI = true;

        [Tooltip("If the object will be selected by clicking with mouse or tapping on it.")]
        public bool selectOnClick;
        //[Tooltip("Optional profile for objects selected by clicking on them")]
        //public HighlightProfile selectedProfile;
        //[Tooltip("Profile to use whtn object is selected and highlighted.")]
        //public HighlightProfile selectedAndHighlightedProfile;
        [Tooltip("Automatically deselects other previously selected objects")]
        public bool singleSelection;
        [Tooltip("Toggles selection on/off when clicking object")]
        public bool toggle;
        [Tooltip("Keeps current selection when clicking outside of any selectable object")]
        public bool keepSelection = true;

        HighlightEffect baseEffect, currentEffect;
        Transform currentObject;

        public readonly static List<HighlightEffect> selectedObjects = new List<HighlightEffect>();
        public event OnObjectSelectionEvent OnObjectSelected;
        public event OnObjectSelectionEvent OnObjectUnSelected;
        public event OnObjectHighlightEvent OnObjectHighlightStart;
        public event OnObjectHighlightEvent OnObjectHighlightStay;
        public event OnObjectHighlightEvent OnObjectHighlightEnd;
        public static int lastTriggerFrame;


        static HighlightManager _instance;
        public static HighlightManager instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Misc.FindObjectOfType<HighlightManager>();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void DomainReloadDisabledSupport()
        {
            selectedObjects.Clear();
            lastTriggerFrame = 0;
            _instance = null;
        }

        void OnEnable()
        {
            currentObject = null;
            currentEffect = null;
            if (baseEffect == null)
            {
                baseEffect = GetComponent<HighlightEffect>();

            }
            if (raycastCamera == null)
            {
                raycastCamera = GetCamera();
                if (raycastCamera == null)
                {
                    Debug.LogError("Highlight Manager: no camera found!");
                }
            }

            InputProxy.Init();
        }


        void OnDisable()
        {
            SwitchesObject(null);
            internal_DeselectAll();
        }

        private void Start()
        {
            _mainRole = GetComponent<RoleCtrlBase>();
        }

        void Update()
        {
            if (raycastCamera == null)
                return;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (respectUI) {
                EventSystem es = EventSystem.current;
                if (es == null) {
                    es = CreateEventSystem();
                }
                List<RaycastResult> raycastResults = new List<RaycastResult>();
                PointerEventData eventData = new PointerEventData(es);
                Vector3 cameraPos = raycastCamera.transform.position;
                if (raycastSource == RayCastSource.MousePosition) {
                    eventData.position = InputProxy.mousePosition;
                } else {
                    eventData.position = new Vector2(raycastCamera.pixelWidth * 0.5f, raycastCamera.pixelHeight * 0.5f);
                }
                es.RaycastAll(eventData, raycastResults);
                int hitCount = raycastResults.Count;
                // check UI blocker
                bool blocked = false;
                for (int k = 0; k < hitCount; k++) {
                    RaycastResult rr = raycastResults[k];
                    if (rr.module is UnityEngine.UI.GraphicRaycaster) {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) return;

                // look for our gameobject
                for (int k = 0; k < hitCount; k++) {
                    RaycastResult rr = raycastResults[k];
                    float distance = Vector3.Distance(rr.worldPosition, cameraPos);
                    if (distance < minDistance || (maxDistance > 0 && distance > maxDistance)) continue;

                    GameObject theGameObject = rr.gameObject;
                    if ((layerMask & (1 << rr.gameObject.layer)) == 0) continue;

                    // is this object state controller by Highlight Trigger?
                    HighlightTrigger trigger = theGameObject.GetComponent<HighlightTrigger>();
                    if (trigger != null) return;

                    // Toggles selection
                    Transform t = theGameObject.transform;
                    if (InputProxy.GetMouseButtonDown(0)) {
                        if (selectOnClick) {
                            ToggleSelection(t, !toggle);
                        }
                    } else {
                        // Check if the object has a Highlight Effect
                        if (t != currentObject) {
                            SwitchesObject(t);
                        }
                    }
                    return;
                }
            }
            // if not blocked by UI and no hit found, fallback to raycast (required if no PhysicsRaycaster is present on the camera)
#endif

            Ray ray;
            if (raycastSource == RayCastSource.MousePosition)
            {
#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
                if (!CanInteract())
                {
                    return;
                }
#endif
                ray = raycastCamera.ScreenPointToRay(InputProxy.mousePosition);
            }
            else
            {
                ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
            }

            VerifyHighlightStay();

            if (InputProxy.GetMouseButtonDown(0))
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, maxDistance > 0 ? maxDistance : raycastCamera.farClipPlane, layerMask) && Vector3.Distance(hitInfo.point, ray.origin) >= minDistance)
                {
                    Transform hitTrans = hitInfo.collider.transform;

                    Debug.Log("selectOnClick::" + hitTrans);
                    if (selectOnClick)
                    {

                        if (_selectRole != null) { ShowSelectEffect(false); }

                        //被点击对象
                        _selectRole = hitTrans.GetComponent<RoleCtrlBase>();
                        if (_selectRole != null)
                        {
                            _mainRole._targetRole = _selectRole;
                            if (_selectRole._roleType != RoleType.NPC)
                            {
                                //SelectedBarWidget selectedBarWidget = UIRootMgr.Instance.SelectedBar;
                                //selectedBarWidget.gameObject.Show();
                                //selectedBarWidget.RefreshUI(_selectRole);

                                ShowSelectEffect();
                            }
                            else
                            {
                                NpcCtrl npcCtrl = _selectRole as NpcCtrl;
                                if (npcCtrl != null)
                                {
                                    npcCtrl.OpenTalk( _mainRole);
                                }
                            }

                            ToggleSelection(hitTrans, !toggle);
                        }
                    }
                    return;
                }
                else
                {
                    // Check if the object has a Highlight Effect

                    //if (hitTrans != currentObject) {
                    //    // SwitchesObject(t);
                    //}
                }
                if (selectOnClick && !keepSelection && InputProxy.GetMouseButtonDown(0) && lastTriggerFrame < Time.frameCount)
                {
                    if (_selectRole != null) { ShowSelectEffect(false); }
                    //UIRootMgr.Instance.SelectedBar.gameObject.Show(false);
                    _mainRole._targetRole = null;
                    internal_DeselectAll();
                }
                SwitchesObject(null);

            }

            // no hit
        }


        //private Transform hitTrans;

        public void HitFX(Transform hitTrans)
        {
            Debug.Log("trans:: " + hitTrans);
            HighlightEffect effect = hitTrans.GetComponent<HighlightEffect>();
            if (effect == null) return;
            effect.HitFX(hitTrans.GetComponent<Collider>().bounds.center);

        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EventSystem CreateEventSystem() {
            GameObject eo = new GameObject("Event System created by Highlight Plus", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            return eo.GetComponent<EventSystem>();
        }
#endif
        void VerifyHighlightStay()
        {
            if (currentObject == null || currentEffect == null || !currentEffect.highlighted) return;
            if (OnObjectHighlightStay != null && !OnObjectHighlightStay(currentObject.gameObject))
            {
                SwitchesObject(null);
            }
        }

        public void ShowSelectEffect(bool isShow = true)
        {

            Transform effectSelect = _selectRole.transform.Find("Effect_Select");
            if (effectSelect != null)
            {
                effectSelect.Show(isShow);
            }
        }

        void SwitchesObject(Transform newObject)
        {


            if (currentEffect != null)
            {
                if (highlightOnHover)
                {
                    Highlight(false);
                }
                currentEffect = null;
            }
            currentObject = newObject;
            if (newObject == null) return;
            HighlightTrigger ht = newObject.GetComponent<HighlightTrigger>();
            if (ht != null && ht.enabled)
                return;

            HighlightEffect otherEffect = newObject.GetComponent<HighlightEffect>();
            if (otherEffect == null)
            {
                // Check if there's a parent highlight effect that includes this object
                HighlightEffect parentEffect = newObject.GetComponentInParent<HighlightEffect>();
                if (parentEffect != null && parentEffect.Includes(newObject))
                {
                    currentEffect = parentEffect;
                    if (highlightOnHover)
                    {
                        Highlight(true);
                    }
                    return;
                }
            }
            currentEffect = otherEffect != null ? otherEffect : baseEffect;
            baseEffect.enabled = currentEffect == baseEffect;
            currentEffect.SetTarget(currentObject);

            if (highlightOnHover)
            {
                Highlight(true);
            }
        }

#if !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
        bool CanInteract()
        {
            if (!respectUI) return true;
            EventSystem es = EventSystem.current;
            if (es == null) return true;
            if (Application.isMobilePlatform && InputProxy.touchCount > 0 && es.IsPointerOverGameObject(InputProxy.GetFingerIdFromTouch(0)))
            {
                return false;
            }
            else if (es.IsPointerOverGameObject(-1))
                return false;
            return true;
        }
#endif

        void ToggleSelection(Transform t, bool forceSelection)
        {

            // We need a highlight effect on each selected object
            HighlightEffect hb = t.GetComponent<HighlightEffect>();
            if (hb == null)
            {
                HighlightEffect parentEffect = t.GetComponentInParent<HighlightEffect>();
                if (parentEffect != null && parentEffect.Includes(t))
                {
                    hb = parentEffect;
                    if (hb.previousSettings == null)
                    {
                        hb.previousSettings = ScriptableObject.CreateInstance<HighlightProfile>();
                    }
                    hb.previousSettings.Save(hb);
                }
                else
                {
                    hb = t.gameObject.AddComponent<HighlightEffect>();
                    hb.previousSettings = ScriptableObject.CreateInstance<HighlightProfile>();
                    // copy default highlight effect settings from this manager into this highlight plus component
                    hb.previousSettings.Save(baseEffect);
                    hb.previousSettings.Load(hb);
                }
            }

            bool currentState = hb.isSelected;
            bool newState = forceSelection ? true : !currentState;
            if (newState == currentState) return;

            if (newState)
            {
                if (OnObjectSelected != null && !OnObjectSelected(t.gameObject)) return;
            }
            else
            {
                if (OnObjectUnSelected != null && !OnObjectUnSelected(t.gameObject)) return;
            }

            if (singleSelection)
            {
                internal_DeselectAll();
            }

            currentEffect = hb;
            currentEffect.isSelected = newState;
            baseEffect.enabled = false;

            if (currentEffect.isSelected)
            {
                if (currentEffect.previousSettings == null)
                {
                    currentEffect.previousSettings = ScriptableObject.CreateInstance<HighlightProfile>();
                }
                hb.previousSettings.Save(hb);

                if (!selectedObjects.Contains(currentEffect))
                {
                    selectedObjects.Add(currentEffect);
                }
            }
            else
            {
                if (currentEffect.previousSettings != null)
                {
                    currentEffect.previousSettings.Load(hb);
                }
                if (selectedObjects.Contains(currentEffect))
                {
                    selectedObjects.Remove(currentEffect);
                }
            }

            Highlight(newState);
        }

        void Highlight(bool state)
        {
            if (currentEffect == null) return;

            if (state)
            {
                if (!currentEffect.highlighted)
                {
                    if (OnObjectHighlightStart != null && currentEffect.target != null)
                    {
                        if (!OnObjectHighlightStart(currentEffect.target.gameObject))
                        {
                            currentObject = null; // allows re-checking so it keeps checking with the event
                            return;
                        }
                    }
                }
            }
            else
            {
                if (currentEffect.highlighted)
                {
                    if (OnObjectHighlightEnd != null && currentEffect.target != null)
                    {
                        OnObjectHighlightEnd(currentEffect.target.gameObject);
                    }
                }
            }
            if (selectOnClick || currentEffect.isSelected)
            {
                if (currentEffect.isSelected)
                {

                    currentEffect.previousSettings.Load(currentEffect);

                    if (currentEffect.highlighted && currentEffect.fading != HighlightEffect.FadingState.FadingOut)
                    {
                        currentEffect.UpdateMaterialProperties();
                    }
                    else
                    {
                        currentEffect.SetHighlighted(true);
                    }
                    return;
                }
                else if (!highlightOnHover)
                {
                    currentEffect.SetHighlighted(false);
                    return;
                }
            }
            currentEffect.SetHighlighted(state);
        }



        public static Camera GetCamera()
        {
            Camera raycastCamera = Camera.main;
            if (raycastCamera == null)
            {
                raycastCamera = Misc.FindObjectOfType<Camera>();
            }
            return raycastCamera;
        }

        void internal_DeselectAll()
        {


            foreach (HighlightEffect hb in selectedObjects)
            {
                if (hb != null && hb.gameObject != null)
                {
                    if (OnObjectUnSelected != null)
                    {
                        if (!OnObjectUnSelected(hb.gameObject)) continue;
                    }
                    hb.RestorePreviousHighlightEffectSettings();
                    hb.isSelected = false;
                    hb.SetHighlighted(false);
                }
            }
            selectedObjects.Clear();
        }

        /// <summary>
        /// Deselects any selected object in the scene
        /// </summary>
        public static void DeselectAll()
        {
            if (instance != null)
            {
                _instance.internal_DeselectAll();
                return;
            }

            foreach (HighlightEffect hb in selectedObjects)
            {
                if (hb != null && hb.gameObject != null)
                {
                    hb.RestorePreviousHighlightEffectSettings();
                    hb.isSelected = false;
                    hb.SetHighlighted(false);
                }
            }
            selectedObjects.Clear();
        }

        /// <summary>
        /// Manually causes highlight manager to select an object
        /// </summary>
        public void SelectObject(Transform t)
        {
            ToggleSelection(t, true);
        }

        /// <summary>
        /// Manually causes highlight manager to toggle selection on an object
        /// </summary>
        public void ToggleObject(Transform t)
        {
            ToggleSelection(t, false);
        }

        /// <summary>
        /// Manually causes highlight manager to unselect an object
        /// </summary>
        public void UnselectObject(Transform t)
        {
            if (t == null) return;
            HighlightEffect hb = t.GetComponent<HighlightEffect>();
            if (hb == null) return;

            if (hb.isSelected)
            {
                ToggleSelection(t, false);
            }
        }


    }

}