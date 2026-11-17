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

    /// <summary>为聊天 UI 提供两种显隐方式：直接启停对象，或保存局部位置后把仍激活的对象移到远处。</summary>
    public static class GameObjectVisibilityExtensions
    {
        /// <summary>按 Unity 实例 ID 保存首次移开前的局部位置；以移出屏幕方式重新显示后移除此记录。</summary>
        private static readonly Dictionary<int, Vector3> SavedLocalPositions = new Dictionary<int, Vector3>();
        /// <summary>每次隐藏时给局部 X、Y 各增加的偏移量，单位为对象局部坐标。</summary>
        private const float OutsideScreenOffset = 100000f;

        /// <summary>
        /// 显示或隐藏对象。使用 MoveOutsideScreen 显示时才会恢复保存的位置；显示和隐藏应传入相同方式。
        /// </summary>
        /// <param name="target">需要控制的对象，null 时不处理。</param>
        /// <param name="visible">true 激活对象，false 按 hideType 隐藏。</param>
        /// <param name="hideType">隐藏方式；显示时也用它决定是否恢复之前保存的位置。</param>
        public static void SetVisible(this GameObject target, bool visible, GameObjectHideType hideType = GameObjectHideType.Deactivate)
        {
            if (target == null) return;

            // 用对象实例 ID 关联原位置，不把 GameObject 本身保存进静态字典。
            int id = target.GetInstanceID();
            if (visible)
            {
                // savedPosition 是首次移开前的局部位置，只有采用相同显隐方式时才读取并清除。
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
            // 重复隐藏会继续累加偏移，但原位置只保存第一次，直到显示时恢复。
            target.transform.localPosition += new Vector3(OutsideScreenOffset, OutsideScreenOffset, 0f);
            target.SetActive(true);
        }
    }
}
