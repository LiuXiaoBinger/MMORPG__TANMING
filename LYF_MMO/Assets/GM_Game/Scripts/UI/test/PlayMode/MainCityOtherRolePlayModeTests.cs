using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 主城其他玩家协议模拟的 PlayMode 验证。
/// 测试程序集直接使用 Unity Test Framework 的运行时引用，不依赖 Editor 程序集。
/// </summary>
public sealed class MainCityOtherRolePlayModeTests
{
    /// <summary>专用测试场景的构建路径；该场景必须由测试环境加入可加载场景列表。</summary>
    private const string TestScenePath = "Assets/GM_Game/Scenes/New Scene.unity";

    /// <summary>已有 MainCityOtherRoleMock 自动投递的其他玩家持久化角色 ID。</summary>
    private const int ExpectedRoleId = 900001;

    /// <summary>等待 YooAsset、Global 和正式角色同步链路完成的最长秒数，超过后判定失败。</summary>
    private const float RoleCreateTimeoutSeconds = 30f;

    /// <summary>
    /// 在测试开始前以运行时场景 API 加载专用 New Scene。
    /// PlayMode 测试不引用 UnityEditor，避免被归类到 Editor 测试程序集。
    /// </summary>
    /// <returns>等待场景异步加载完成的 Unity Test Framework 协程。</returns>
    [UnitySetUp]
    public IEnumerator LoadNewScene()
    {
        // 可加载性用于提前给出场景未进入测试构建列表的明确错误，不隐式修改 Build Settings。
        Assert.IsTrue(Application.CanStreamedLevelBeLoaded(TestScenePath),
            "New Scene 未加入可加载场景列表，无法执行 PlayMode 协议模拟测试。");

        // 运行时异步加载操作；加载完成前每帧让回 Unity 主循环。
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(TestScenePath,
            LoadSceneMode.Single);
        Assert.IsNotNull(loadOperation, "New Scene 加载请求创建失败。");
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // 加载完成后验证当前激活场景，确保后续断言观察的是专用测试场景。
        Scene activeScene = SceneManager.GetActiveScene();
        Assert.AreEqual(TestScenePath, activeScene.path,
            "PlayMode 测试必须在 New Scene 中执行。");
    }

    /// <summary>
    /// 验证自动投递 CMD_SyncotherOnlineCode 后，正式场景实体世界包含目标其他玩家。
    /// </summary>
    /// <returns>等待协议处理完成或超时断言失败的 Unity Test Framework 协程。</returns>
    [UnityTest]
    public IEnumerator AutoSyncProtocolCreatesOtherRole()
    {
        // 以真实时间计算等待上限，避免 Time.timeScale 变化导致测试无限等待。
        float deadlineSeconds = Time.realtimeSinceStartup + RoleCreateTimeoutSeconds;
        while (Time.realtimeSinceStartup < deadlineSeconds)
        {
            // 当前 Global 实例为空时说明 YooAsset 或场景初始化尚未完成，继续等待下一帧。
            Global currentGlobal = Global.Instance;
            if (currentGlobal != null && currentGlobal.SceneWorld != null)
            {
                // 正式场景实体世界按角色持久化 ID 查询其他玩家，测试不参与任何创建逻辑。
                RoleEntity otherRole;
                if (currentGlobal.SceneWorld.TryGetRole(ExpectedRoleId, out otherRole) &&
                    otherRole != null)
                {
                    Assert.AreEqual(ExpectedRoleId, otherRole.GetID(),
                        "正式主城同步链写入的角色 ID 必须与 CMD_SyncotherOnlineCode 载荷一致。");
                    yield break;
                }
            }

            // 将轮询让回 Unity 主循环，以便异步资源、协程和 SocketDispatcher 同步处理继续推进。
            yield return null;
        }

        Assert.Fail("等待 30 秒后，Global.SceneWorld 仍未找到角色 ID 900001；请检查 YooAsset、MainCityMgr 注册和 CMD_SyncotherOnlineCode 自动投递日志。");
    }
}
