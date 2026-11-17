using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IndieImpulseAssets {
    /// <summary>为本 Image 单独复制材质，启用后在指定时长内渐变一个浮点属性，完成后隐藏图片。</summary>
    public class UISmoothTransitionMatValue : MonoBehaviour {
        /// <summary>每次启用时写入材质属性的起始值。</summary>
        public float startValue = -0.28f;

        /// <summary>渐变结束时写入材质属性的目标值。</summary>
        public float endValue = 0.53f;

        /// <summary>完成一次属性渐变所用的秒数。</summary>
        public float duration = 0.78f;

        /// <summary>禁用组件或对象时是否把材质属性恢复到起始值。</summary>
        public bool setMatOnDisable = false;

        /// <summary>需要渐变的 Shader 浮点属性名称。</summary>
        public string MatValue = "_CenterMove";

        /// <summary>此图片独占的材质副本，避免修改共享材质影响其他 UI。</summary>
        private Material material;

        /// <summary>本次启用后累计的缩放时间秒数。</summary>
        private float elapsedTime;

        /// <summary>只在创建时复制一次 Image 材质并绑定给图片。</summary>
        private void Awake() {
            // 同一对象上的目标图片。
            Image image = GetComponent<Image>();
            if (image != null) {
                material = Instantiate(image.material);  // 为本图片创建独立材质副本
                image.material = material;
            }
        }

        /// <summary>重置属性和计时并显示 Image，材质不可用时禁用本组件。</summary>
        private void OnEnable() {
            if (material == null) {
                Debug.LogWarning("Material was not assigned.");
                enabled = false;
                return;
            }

            material.SetFloat(MatValue, startValue); // 恢复起始材质值
            elapsedTime = 0f;                        // 重新开始累计时间
            enabled = true;

            GetComponent<Image>().enabled = true; // 确保本次渐变能看到图片
        }

        /// <summary>按累计时间线性插值材质值；完成后隐藏 Image，组件本身仍保持启用。</summary>
        private void Update() {
            if (material == null) return;

            elapsedTime += Time.deltaTime;
            // 当前渐变进度，限制在 0 到 1。
            float t = Mathf.Clamp01(elapsedTime / duration);
            // 按进度得到的本帧材质属性值。
            float currentOffset = Mathf.Lerp(startValue, endValue, t);
            material.SetFloat(MatValue, currentOffset);

            // 完成后只隐藏 Image；下面禁用组件的语句目前仍被注释
            if (t >= 1f) {
                //enabled = false;
                GetComponent<Image>().enabled = false;
            }
        }

        /// <summary>按配置决定是否把材质属性复位，不销毁材质。</summary>
        private void OnDisable() {
            if (setMatOnDisable && material != null) {
                material.SetFloat(MatValue, startValue);
            }
        }

        /// <summary>销毁组件自己创建的材质副本。</summary>
        private void OnDestroy() {
            if (material != null) {
                Destroy(material); // 归还本组件创建的材质副本
            }
        }

        /// <summary>只设置横向缩放 scaleX，可用负数翻转图片，纵向和深度缩放保持原值。</summary>
        public void SetScaleDirection(float scaleX) {
            // 只调整当前 UI 矩形的横向缩放。
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null) {
                rectTransform.localScale = new Vector3(scaleX, rectTransform.localScale.y, rectTransform.localScale.z);
            }
        }
    }
}