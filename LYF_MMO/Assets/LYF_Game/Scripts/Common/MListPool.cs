using System.Collections.Generic;

/// <summary>复用临时 List；每次取出或放回都会清空内容，减少反复创建列表。</summary>
/// <typeparam name="T">列表中保存的元素类型。</typeparam>
public static class MListPool<T>
{
    /// <summary>该元素类型的临时列表池，借出与归还时均清空列表。</summary>
    private static readonly MObjectPool<List<T>> s_pool =
        new MObjectPool<List<T>>(Create, ClearList, ClearList);

    /// <summary>空闲列表耗尽时创建一个新列表。</summary>
    /// <returns>尚未借出的空列表实例。</returns>
    private static List<T> Create()
    {
        return new List<T>();
    }

    /// <summary>清空借出或归还的列表，解除对各元素的引用。</summary>
    /// <param name="items">本池借出的可修改列表。</param>
    private static void ClearList(List<T> items)
    {
        items.Clear();
    }

    /// <summary>借出一个空列表。</summary>
    /// <returns>由调用方独占至 Release 的列表。</returns>
    public static List<T> Get()
    {
        return s_pool.Get();
    }

    /// <summary>归还临时列表并解除其中元素引用。</summary>
    /// <param name="items">此前由本池借出的列表。</param>
    public static void Release(List<T> items)
    {
#if DEBUG
        if (items == null)
        {
            // 调试版本报告错误；下面仍按原流程调用对象池归还。
            UnityEngine.Debug.LogError("归还的临时列表为空。");
        }
#endif
        s_pool.Release(items);
    }

    /// <summary>清除当前空闲的列表实例。</summary>
    public static void Clear()
    {
        s_pool.Clear();
    }
}
