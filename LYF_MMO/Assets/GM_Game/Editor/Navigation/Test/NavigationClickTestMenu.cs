using UnityEditor;
using UnityEngine;

/// <summary>导航工具程序集内的专用测试菜单，Play期间临时开关点地输入，不保存场景组件。</summary>
[InitializeOnLoad]
public static class NavigationClickTestMenu
{
    /// <summary>脚本重载后继续监听退出Play，确保DontSave测试宿主被明确销毁。</summary>
    static NavigationClickTestMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>退出Play时销毁临时宿主，不依赖DontSave对象的场景卸载行为。</summary>
    /// <param name="state">编辑器本次Play生命周期变更阶段。</param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
        {
            return;
        }
        // 结束运行时立即移除测试对象，防止编辑态残留不能保存的宿主。
        NavigationClickTest test = FindTest();
        if (test != null)
        {
            Object.DestroyImmediate(test.gameObject);
        }
    }

    /// <summary>创建仅本次Play有效的测试宿主；重复启用保持单个实例。</summary>
    [MenuItem("工具/导航/测试/启用点地导航（仅本次运行）")]
    private static void EnableTest()
    {
        if (FindTest() != null)
        {
            return;
        }
        // DontSave禁止写入场景或构建，退出Play时由生命周期监听销毁临时对象。
        GameObject host = new GameObject("NavigationClickTest（临时点地测试）");
        host.hideFlags = HideFlags.DontSave;
        host.AddComponent<NavigationClickTest>();
        Debug.Log("点地导航测试已启用：在Game窗口点击地面；拖动、UI和实体点击不触发测试导航。退出Play自动移除。");
    }

    /// <summary>仅允许在编辑器Play期间手动启用，不自动进入或退出Play。</summary>
    /// <returns>当前处于Play且未暂停时返回true。</returns>
    [MenuItem("工具/导航/测试/启用点地导航（仅本次运行）", true)]
    private static bool CanEnableTest()
    {
        return EditorApplication.isPlaying && !EditorApplication.isPaused;
    }

    /// <summary>关闭本次临时点地输入，角色后续仍沿原导航规则运行和中断。</summary>
    [MenuItem("工具/导航/测试/关闭点地导航测试")]
    private static void DisableTest()
    {
        // 只删除本菜单创建的专用测试对象，不接触正式实体或场景资源。
        NavigationClickTest test = FindTest();
        if (test != null)
        {
            test.enabled = false;
            Object.Destroy(test.gameObject);
        }
        Debug.Log("点地导航测试已关闭，方向键仍可接管当前角色移动。");
    }

    /// <summary>只有Play中存在临时组件时才允许关闭。</summary>
    /// <returns>运行中已开启测试时返回true。</returns>
    [MenuItem("工具/导航/测试/关闭点地导航测试", true)]
    private static bool CanDisableTest()
    {
        return EditorApplication.isPlaying && FindTest() != null;
    }

    /// <summary>定位DontSave对象上的测试组件，脚本重载后菜单仍能找到同一临时宿主。</summary>
    /// <returns>本次运行已有的临时测试组件；尚未启用时为空。</returns>
    private static NavigationClickTest FindTest()
    {
        // DontSave对象不参与普通Find查询，显式检索已加载的专用类型。
        NavigationClickTest[] tests = Resources.FindObjectsOfTypeAll<NavigationClickTest>();
        if (tests.Length > 0)
        {
            return tests[0];
        }
        return null;
    }
}
