using System.Collections.Generic;
using UnityEngine;

namespace ChatTest.UI
{
    /// <summary>隐藏对象时采用的方式。</summary>
    public enum GameObjectHideType
    {
        // 直接禁用 GameObject。
        Deactivate,
        // 保持激活，但移动到屏幕外。
        MoveOutsideScreen
    }

    /// <summary>GameObject 显示和隐藏扩展方法。</summary>
    public static class GameObjectVisibilityExtensions
    {
        private static readonly Dictionary<int, Vector3> SavedLocalPositions = new Dictionary<int, Vector3>();
        private const float OutsideScreenOffset = 100000f;

        /// <summary>
        /// 显示或隐藏对象。hideType 只在隐藏时决定采用失活还是移出屏幕。
        /// </summary>
        public static void SetVisible(this GameObject target, bool visible, GameObjectHideType hideType = GameObjectHideType.Deactivate)
        {
            if (target == null) return;

            int id = target.GetInstanceID();
            if (visible)
            {
                if (hideType == GameObjectHideType.MoveOutsideScreen && SavedLocalPositions.TryGetValue(id, out Vector3 savedPosition))
                {
                    target.transform.localPosition = savedPosition;
                    SavedLocalPositions.Remove(id);
                }
                target.SetActive(true);
                return;
            }

            if (hideType == GameObjectHideType.Deactivate)
            {
                target.SetActive(false);
                return;
            }

            if (!SavedLocalPositions.ContainsKey(id))
            {
                SavedLocalPositions[id] = target.transform.localPosition;
            }
            target.transform.localPosition += new Vector3(OutsideScreenOffset, OutsideScreenOffset, 0f);
            target.SetActive(true);
        }
    }
}
