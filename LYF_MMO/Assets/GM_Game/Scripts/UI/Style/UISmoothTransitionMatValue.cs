using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IndieImpulseAssets {
    public class UISmoothTransitionMatValue : MonoBehaviour {
        public float startValue = -0.28f;
        public float endValue = 0.53f;
        public float duration = 0.78f;
        public bool setMatOnDisable = false;
        public string MatValue = "_CenterMove";
        private Material material;
        private float elapsedTime;

        private void Awake() {
            Image image = GetComponent<Image>();
            if (image != null) {
                material = Instantiate(image.material);  // Clone the material only once
                image.material = material;
            }
        }

        private void OnEnable() {
            if (material == null) {
                Debug.LogWarning("Material was not assigned.");
                enabled = false;
                return;
            }

            material.SetFloat(MatValue, startValue); // Set initial value
            elapsedTime = 0f;                        // Reset elapsed time
            enabled = true;

            GetComponent<Image>().enabled = true; // Enable the texture for transition
        }

        private void Update() {
            if (material == null) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float currentOffset = Mathf.Lerp(startValue, endValue, t);
            material.SetFloat(MatValue, currentOffset);

            // Disable Update when transition completes
            if (t >= 1f) {
                //enabled = false;
                GetComponent<Image>().enabled = false;
            }
        }

        private void OnDisable() {
            if (setMatOnDisable && material != null) {
                material.SetFloat(MatValue, startValue);
            }
        }

        public void SetScaleDirection(float scaleX) {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null) {
                rectTransform.localScale = new Vector3(scaleX, rectTransform.localScale.y, rectTransform.localScale.z);
            }
        }

        private void OnDestroy() {
            if (material != null) {
                Destroy(material); // Clean up the material instance when the object is destroyed
            }
        }


    }
}