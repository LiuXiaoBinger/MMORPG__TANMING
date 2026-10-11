// 隔离动画、粒子、飞行物及 Scene 参考预览；固定模型根变换不覆盖骨骼姿态，预览不等于游戏运行接入。
using AtkJudge;
using System;
using Ethan.ActionEditor;
using Ethan.ActionEditor.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>技能窗口的隔离角色、动画采样与特效预览实现；原脚本身份仍由 SkillEditorWindow.cs 保留。</summary>
public partial class SkillEditorWindow
{
    /// <summary>当前预览副本的角色根位置，单位为父节点局部米；预制体归零，场景源沿用实例化时局部坐标。</summary>
    Vector3 _previewActorLocalPosition;
    /// <summary>当前预览副本的角色根初始朝向，采样期间只恢复外层变换，不修改骨骼姿态。</summary>
    Quaternion _previewActorLocalRotation;
    /// <summary>当前预览副本的角色根初始缩放，保持源模型的尺寸比例。</summary>
    Vector3 _previewActorLocalScale;
    /// <summary>显式预览采样的 Animator 所在变换；可能是角色根，也可能是嵌套模型节点，随副本销毁清空。</summary>
    Transform _previewAnimatorTransform;
    /// <summary>Animator 节点创建时的局部位置，单位为父节点局部米，防止采样把根位移累计到下一次。</summary>
    Vector3 _previewAnimatorLocalPosition;
    /// <summary>Animator 节点创建时的局部朝向，避免重复采样累计根旋转。</summary>
    Quaternion _previewAnimatorLocalRotation;
    /// <summary>Animator 节点创建时的局部缩放，保留嵌套模型相对角色根的尺寸。</summary>
    Vector3 _previewAnimatorLocalScale;
    /// <summary>只在时间轴固定步采样时允许推进粒子和飞行物，暂停的Editor更新不能改变姿态。</summary>
    bool _isSamplingPreview;
    /// <summary>编辑修改后的预览需从零帧重建；纯重绘同一帧不能累计模拟时间。</summary>
    bool _previewFrameDirty=true;
    /// <summary>显式停止后禁止普通重绘重新生成表现；播放或定位才开启下一次预览。</summary>
    bool _previewStopped;
    /// <summary>本轮固定步正在重建的帧号，供内嵌声音记录其特效真实生成帧。</summary>
    int _samplingPreviewFrame;
    /// <summary>当前配置的预览尾帧缓存，负一需按真实粒子及音频重新估算；绝不写入作者END。</summary>
    int _previewEndFrameCache=-1;
    /// <summary>尾帧缓存对应的原作者资产身份，切配置不能沿用上一个技能的尾长。</summary>
    SkillConfigSO _previewEndConfig;
    /// <summary>初始化副本粒子时钟，停用自动脚本并接管原自动音源；原Prefab完全保留。</summary>
    /// <param name="instance">本窗口创建的隐藏副本，不能传入正式场景或预制体资源。</param>
    /// <returns>包含未激活子节点的粒子快照，由时间轴逐系统模拟。</returns>
    ParticleSystem[] InitializePreviewParticles(GameObject instance){
        // 副本的脚本快照，防止ExecuteAlways在暂停时自行改变姿态。
        MonoBehaviour[] behaviours=instance.GetComponentsInChildren<MonoBehaviour>(true);
        // 脚本组件的零起始下标，仅预览标记不参与原业务。
        for(int index=0;index<behaviours.Length;index++) behaviours[index].enabled=false;
        RegisterEmbeddedPreviewAudio(instance);
        // 根与所有子粒子的固定快照，避免递归重复模拟子系统。
        ParticleSystem[] particles=instance.GetComponentsInChildren<ParticleSystem>(true);
        // 当前粒子组件的数组下标，用稳定种子重建同帧姿态。
        for(int index=0;index<particles.Length;index++){
            particles[index].useAutoRandomSeed=false;
            if(particles[index].randomSeed==0) particles[index].randomSeed=(uint)(index+1);
            particles[index].Simulate(0f,false,true,false);
            particles[index].Pause(false);
        }
        return particles;
    }
    /// <summary>选择Scene预览标签样式，仅用于绘制，不修改预览选择或配置。</summary>
    /// <param name="selected">当前预览事件是否已选中。</param>
    /// <returns>已建立的普通或选中样式。</returns>
    GUIStyle PreviewLabelStyle(bool selected)
    {
        if (selected) return _sceneSelLabelStyle;
        return _sceneLabelStyle;
    }

    /// <summary>将特效挂点路径解析到预览副本；单个名称允许匹配唯一的后代节点。</summary>
    /// <param name="actor">当前预览角色实例，根节点是路径解析起点。</param>
    /// <param name="socketPath">层级路径或唯一挂点名；空值表示角色根节点。</param>
    /// <param name="logError">生成特效时报告缺失或歧义；Scene 视图逐帧绘制时保持静默。</param>
    /// <returns>实际挂点，解析失败时返回空值，调用方不得回退到角色根节点。</returns>
    Transform ResolvePreviewFxAnchor(GameObject actor, string socketPath, bool logError){
        if(actor==null) return null;
        if(string.IsNullOrWhiteSpace(socketPath)) return actor.transform;
        // 层级路径优先使用 Unity 原生查找，避免同名子节点造成歧义。
        string path=socketPath.Trim();
        // 由原层级路径唯一匹配的特效挂点。
        Transform socket=actor.transform.Find(path);
        if(socket!=null) return socket;
        if(path.IndexOf('/')<0){
            // 旧角色预制体的 EffectPos 可能属于嵌套模型；仅唯一匹配才允许使用短名称。
            Transform[] descendants=actor.GetComponentsInChildren<Transform>(true);
            // 当前可索引集合的零起始遍历下标，访问原对象引用。
            for(int i=0;i<descendants.Length;i++){
                // 当前与原挂点名比较的隔离后代节点。
                Transform candidate=descendants[i];
                if(candidate==actor.transform || candidate.name!=path) continue;
                if(socket!=null){
                    if(logError) Debug.LogError(SkillEditorLocalization.Text("Skill effect socket name is ambiguous: ", "技能编辑器特效挂点名称不唯一：")+path,actor);
                    return null;
                }
                socket=candidate;
            }
        }
        if(socket==null && logError) Debug.LogError(SkillEditorLocalization.Text("Skill preview effect socket was not found: ", "技能编辑器预览未找到特效挂点：")+path,actor);
        return socket;
    }

    #region 动画采样与更新
    /// <summary>用统一FPS获取时间轴每秒帧数，兼容原首段剪辑计时与明确计时配置。</summary>
    /// <returns>大于零的时间轴帧率，缺失配置时为三十帧每秒。</returns>
    float GetPreviewFrameRate(){
        if(configFile!=null && configFile.UsesExplicitTiming) return ActionTiming.FrameRate(configFile);
        // 原动画段的零起始下标，旧计时沿用首个有效剪辑FPS。
        for(int index=0;index<animSegments.Count;index++)
            if(animSegments[index].clip!=null && animSegments[index].clip.frameRate>0f) return animSegments[index].clip.frameRate;
        return 30f;
    }
    /// <summary>计算单轮粒子的保守模拟秒上界，包含子发射器最迟死亡触发；实际非零尾粒仍由计数保留。</summary>
    /// <param name="particle">原资源或隔离副本的粒子系统，只读模块数据。</param>
    /// <param name="visited">本次路径访问集合，避免互相引用的子发射器无限递归。</param>
    /// <returns>已经换算系统simulationSpeed的秒数，事件playbackSpeed由调用方统一换算一次。</returns>
    float GetPreviewParticleDuration(ParticleSystem particle,HashSet<ParticleSystem> visited){
        if(particle==null || !visited.Add(particle)) return 0f;
        // 原主模块的延迟、发射持续与最大尾粒寿命，均为粒子模拟秒。
        ParticleSystem.MainModule main=particle.main;
        // 当前系统自身最晚尾粒结束的真实秒数，不含事件倍率。
        float duration=(main.startDelay.constantMax+main.duration+main.startLifetime.constantMax)/Mathf.Max(0.01f,main.simulationSpeed);
        // 子发射器可能在父粒子死亡时才开始，保守追加最长子系统时间窗。
        ParticleSystem.SubEmittersModule subEmitters=particle.subEmitters;
        // 本层子发射器路径的最大额外秒数，不累加同时发生的不同子事件。
        float childDuration=0f;
        if(subEmitters.enabled){
            // 原子发射器列表的零起始下标，仅读原组件引用。
            for(int index=0;index<subEmitters.subEmittersCount;index++){
                // 由当前粒子触发的后续系统，失活子节点不扩展预览范围。
                ParticleSystem child=subEmitters.GetSubEmitterSystem(index);
                if(child==null || !child.gameObject.activeSelf) continue;
                childDuration=Mathf.Max(childDuration,GetPreviewParticleDuration(child,visited));
            }
        }
        visited.Remove(particle);
        return duration+childDuration;
    }
    /// <summary>取得允许触发新事件的正式末帧；尾段只延续已触发表现，不突破显式结束或Complete。</summary>
    /// <returns>零起始正式末帧，未指定配置时沿用原编辑范围。</returns>
    int GetPreviewContentEndFrame(){
        if(configFile!=null && configFile.UsesExplicitTiming) return configFile.RuntimeEndFrame;
        return GetTotalFrames();
    }
    /// <summary>缓存预览末帧，包含正式结束前触发的音频和活跃子粒子延迟、发射及尾粒时间。</summary>
    /// <returns>仅窗口播放与定位使用的末帧，不改变技能状态时长或任何作者事件范围。</returns>
    int GetPreviewEndFrame(){
        if(_previewEndConfig==configFile && _previewEndFrameCache>=0) return _previewEndFrameCache;
        _previewEndConfig=configFile;
        // 正式结束门限，只计算本次确实允许触发的事件。
        int contentEnd=GetPreviewContentEndFrame();
        // 预览尾部的统一每秒帧数，所有资源时长均换算到这个时间轴。
        float fps=GetPreviewFrameRate();
        _previewEndFrameCache=contentEnd;
        // 原效果列表下标，每个资源只在缓存失效时枚举一次子系统。
        for(int index=0;index<fxAndSoundList.Count;index++){
            // 原事件引用，只读资源寿命，不替用户修改制作帧。
            Global.FxAndSound effect=fxAndSoundList[index];
            if(effect==null || effect.keyNumber>contentEnd) continue;
            // 此事件需要保留的时间轴秒数，声音与粒子取最长者。
            float tail=0f;
            if(effect.audioClip!=null) tail=effect.audioClip.length;
            if(effect.particleSystem!=null){
                if(effect.customLifetime>0f) tail=Mathf.Max(tail,effect.customLifetime+0.5f);
                else{
                    // 只读取原层级活跃子粒子；失活节点不能因预览而被自动激活。
                    ParticleSystem[] particles=effect.particleSystem.GetComponentsInChildren<ParticleSystem>();
                    // 单次源层级寿命查询的访问集合，避免子发射器循环引用。
                    HashSet<ParticleSystem> visited=new HashSet<ParticleSystem>();
                    // 活跃粒子组件的零起始下标，最大延迟决定后半段何时出现。
                    for(int particleIndex=0;particleIndex<particles.Length;particleIndex++){
                        // 原资源主模块只读，duration及发射延迟均为粒子模拟秒。
                        // 事件倍率只在最外层换算一次，各系统自身倍率已在统一寿命函数内处理。
                        tail=Mathf.Max(tail,GetPreviewParticleDuration(particles[particleIndex],visited)/Mathf.Max(0.01f,effect.playbackSpeed));
                    }
                }
            }
            _previewEndFrameCache=Mathf.Max(_previewEndFrameCache,effect.keyNumber+Mathf.CeilToInt(tail*fps));
        }
        return _previewEndFrameCache;
    }
    /// <summary>播放与暂停只切换时钟，保留当前角色、粒子和声音采样位置；停止另走原清理入口。</summary>
    /// <param name="playing">真为播放或继续，假为暂停所有预览表现。</param>
    void SetPreviewPlaying(bool playing){
        if(playing && !IsPreviewAllowed) return;
        if(playing && !playFrame){ _previewStopped=false; SampleAnim(); }
        playFrame=playing;
        m_LastEditorTime=EditorApplication.timeSinceStartup;
        SetPreviewAudioPlaying(playing);
        Repaint();
        SceneView.RepaintAll();
    }
    /// <summary>暂停并定位播放头；上一帧、下一帧、输入帧及拖标尺统一使用此入口。</summary>
    /// <param name="frame">要定位的零起始帧，裁剪到包含尾迹的预览导航范围，不扩作者事件编辑范围。</param>
    void SeekPreviewFrame(int frame){
        SetPreviewPlaying(false);
        playTimer=0f;
        _previewStopped=false;
        frameSelectIndex=Mathf.Clamp(frame,0,Mathf.Max(GetTotalFrames(),GetPreviewEndFrame()));
        Repaint();
        SceneView.RepaintAll();
    }
    /// <summary>数据编辑或Undo后冻结预览并使当前帧失效，下次采样反映用户的新声音或特效触发帧。</summary>
    void InvalidatePreviewFrame(){
        SetPreviewPlaying(false);
        _previewFrameDirty=true;
        _previewEndFrameCache=-1;
    }
    /// <summary>按当前播放头重建或推进隔离动画、粒子和声音；暂停重复重绘保持同一姿态。</summary>
    void SampleAnim(){
        if(!IsPreviewAllowed || _previewStopped) return;
        if(last_frameSelectIndex==frameSelectIndex && !_previewFrameDirty) return;
        // 连续播放可沿上一帧推进；暂停定位、回退和修改后都从零帧重建确定的粒子状态。
        bool rebuild=_previewFrameDirty || last_frameSelectIndex<0 || !playFrame || frameSelectIndex<last_frameSelectIndex;
        // 本次需采样的首帧，负一使零帧事件也能被触发。
        int previous=last_frameSelectIndex;
        if(rebuild){ CleanPreviewFx(); ClearPreviewAudioEntries(); previous=-1; }
        // 每个时间轴固定步对应的秒数，不随Editor刷新快慢变化。
        float step=1f/GetPreviewFrameRate();
        _isSamplingPreview=true;
        try{
            // 从事件开始到目标帧逐帧采样挂点位置，世界空间特效也能沿真实发射姿态复现。
            for(int frame=previous+1;frame<=frameSelectIndex;frame++){
                _samplingPreviewFrame=frame;
                if(previewModel!=null){
                    EnsurePreviewInstance();
                    // 当前窗口拥有的动画副本，正式模型不会被采样。
                    GameObject actor=GetEffectivePreviewModel();
                    if(actor!=null) SampleAnimAtFrame(actor,Mathf.Min(frame,GetPreviewContentEndFrame()));
                }
                if(frame>0){ FxUpdate(step); ProjUpdate(step); }
                if(frame<=GetPreviewContentEndFrame()) PlayFxAndSound(frame-1,frame);
            }
            SynchronizePreviewAudio(frameSelectIndex,rebuild);
            last_frameSelectIndex=frameSelectIndex;
            _previewFrameDirty=false;
        }catch(Exception /* 原生采样失败时清理窗口状态并保留异常详情。 */ exception){
            StopPreviewSession(); Debug.LogException(exception);
        }finally{ _isSamplingPreview=false; }
    }
    /// <summary>只在播放时推进时间轴；粒子和飞行物通过采样固定步同步，暂停不读墙钟推进。</summary>
    void OnEditorUpdate(){
        // Editor 启动以来的真实秒数，用于差分预览时间。
        double ct=EditorApplication.timeSinceStartup; 
        // 本轮 Editor 更新的真实秒数，不采用游戏时间缩放。
        float dt=(float)(ct-m_LastEditorTime); 
        m_LastEditorTime=ct;
        if(!IsPreviewAllowed){ if(playFrame) SetPreviewPlaying(false); return; }
        AnimPlayUpdate(dt);
        if(playFrame) SampleAnim();
    }
    /// <param name="dt">本轮 Editor 更新的真实秒数，不采用游戏时间缩放。</param>
    /// <summary>按编辑器真实秒数推进预览帧，循环回零时清理本轮特效，避免残留叠加。</summary>
    void AnimPlayUpdate(float dt){
        if(!IsPreviewAllowed){ SetPreviewPlaying(false); return; }
        if(!playFrame) return;
        // 当前计时帧率，单位帧每秒；缺失剪辑时使用原三十帧默认值。
        float fps=GetPreviewFrameRate();
        frameRate=1f/fps;
        // 预览循环的排除式上限，包含已触发表现的尾段；正式RuntimeEndFrame保持原值。
        int mx=GetPreviewEndFrame()+1;
        if(mx<=0) return;
        playTimer+=dt;
        while (playTimer >= frameRate)
        {
            playTimer-=frameRate; frameSelectIndex++;
            if(frameSelectIndex>=mx){
                // 子发射器等真实存活可能超过静态估算，继续一帧并扩窗口范围，直到尾迹自然清理。
                if(_previewFxInstances.Count>0 || _previewProjInstances.Count>0 || _previewAudioEntries.Count>0){
                    _previewEndFrameCache=frameSelectIndex;
                    mx=_previewEndFrameCache+1;
                }else{
                    frameSelectIndex=0;
                    _previewFrameDirty=true;
                }
            }
            SampleAnim();
            // 静态尾长只用于导航；所有实例满足有限收尾条件并清理后，下一次采样即可循环。
            if(frameSelectIndex>GetPreviewContentEndFrame() && _previewFxInstances.Count==0 && _previewProjInstances.Count==0 && _previewAudioEntries.Count==0){
                frameSelectIndex=0;
                _previewFrameDirty=true;
                SampleAnim();
            }
        }
        Repaint();
    }
    /// <summary>按时间轴固定步模拟粒子；普通单轮按尾粒存活清理，自定义清理延迟另加原半秒缓冲。</summary>
    /// <param name="dt">本步时间轴秒数，正常为一除以当前FPS；粒子模拟单独应用事件播放倍率。</param>
    void FxUpdate(float dt){
        if(Application.isPlaying||(!_isSamplingPreview && !playFrame)||_previewFxInstances.Count==0) return;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=_previewFxInstances.Count-1;i>=0;i--){
            // 当前预览实例是结构体；计时写入后需回存列表。
            var entry=_previewFxInstances[i];
            if(entry.go==null){ _previewFxInstances.RemoveAt(i); continue; }
            // 粒子模拟时间已经乘过事件倍率，单位仍为秒。
            float step=dt*entry.speed;
            // 清理延迟按时间轴秒计，不重复乘除粒子速度；只有实际粒子模拟才应用倍率。
            entry.duration+=dt;
            _previewFxInstances[i]=entry;
            // 派生提示等原事件没有缓存快照，按原行为首次查询粒子。
            ParticleSystem[] particles=entry.particles;
            if(particles==null) particles=entry.go.GetComponentsInChildren<ParticleSystem>(true);
            // 手动Simulate后Pause的IsAlive可能常真，只用真实粒子计数保留可见尾粒。
            bool hasParticles=false;
            // 克隆粒子快照的零起始下标。
            for(int particleIndex=0;particleIndex<particles.Length;particleIndex++){
                // 逐系统独立模拟避免对子系统重复推进，存活包含延迟与已发射尾粒。
                ParticleSystem particle=particles[particleIndex];
                if(particle==null || !particle.gameObject.activeInHierarchy) continue;
                particle.Simulate(step,false,false,false);
                if(particle.particleCount>0) hasParticles=true;
            }
            // 此隔离特效是否满足清理条件。
            bool finished=false;
            if(entry.finitePlayback) finished=!hasParticles && entry.duration>entry.maxLifetime;
            else finished=entry.duration>entry.maxLifetime+0.5f;
            if(finished){
                DestroyImmediate(entry.go);
                _previewFxInstances.RemoveAt(i);
                continue;
            }
        }
        if(_previewFxInstances.Count>0) SceneView.RepaintAll();
    }
    /// <summary>按时间轴固定步移动飞行物及其子粒子，暂停时保持同一世界姿态。</summary>
    /// <param name="dt">本步时间轴秒数，正常为一除以当前FPS；与技能动画和粒子使用相同时钟。</param>
    void ProjUpdate(float dt){
        if(Application.isPlaying||(!_isSamplingPreview && !playFrame)||_previewProjInstances.Count==0) return;
        // 本轮是否移除过无效或已结束的预览实例。
        bool anyRemoved=false;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=_previewProjInstances.Count-1;i>=0;i--){
            // 预览列表中的结构体状态，修改计时后需回写原列表槽。
            var entry=_previewProjInstances[i];
            if(entry.go==null){ _previewProjInstances.RemoveAt(i); anyRemoved=true; continue; }
            entry.elapsed+=dt;
            if(entry.elapsed>entry.lifetime+0.5f){
                DestroyImmediate(entry.go);
                _previewProjInstances.RemoveAt(i);
                anyRemoved=true;
                continue;
            }
            entry.go.transform.position+=entry.velocity*dt;
            // 手动飞行预览的根与子粒子同样只由时间轴推进，暂停时无自然模拟。
            if(entry.particles!=null){
                // 飞行物粒子快照下标，每个系统独立推进一次。
                for(int particleIndex=0;particleIndex<entry.particles.Length;particleIndex++)
                    if(entry.particles[particleIndex]!=null) entry.particles[particleIndex].Simulate(dt,false,false,false);
            }
            _previewProjInstances[i]=entry;
        }
        if(anyRemoved) { /* 清理完毕 */ }
        if(_previewProjInstances.Count>0) SceneView.RepaintAll();
    }

    /// <summary>计算投掷物发射点世界坐标</summary>
    /// <param name="p">当前原飞行物配置，制作资源和飞行参数。</param>
    /// <param name="eff">动画实际采样的隔离模型副本。</param>
    Vector3 GetProjectileFirePosition(Global.Projectile p,GameObject eff){
        if(!string.IsNullOrEmpty(p.firePointPath)){
            // 原飞行物发射点路径匹配的隔离节点。
            Transform fp=eff.transform.Find(p.firePointPath);
            if(fp!=null) return fp.position;
        }
        return eff.transform.TransformPoint(p.spawnOffset);
    }

    /// <summary>在编辑器预览中生成投掷物：Instantiate 预制体并模拟直线飞行</summary>
    /// <param name="p">当前原飞行物配置，制作资源和飞行参数。</param>
    void SpawnPreviewProjectile(Global.Projectile p){
        if(p.prefab==null) return;
        // 本次飞行物预览副本，禁用运行脚本和碰撞体。
        var projGo=Instantiate(p.prefab);
        projGo.hideFlags=HideFlags.DontSave|HideFlags.HideInHierarchy;
        projGo.AddComponent<EditorPreviewFxTag>().hideFlags=HideFlags.DontSave|HideFlags.HideInInspector;

        // ── 禁用运行时脚本（ProjectileBehaviour等），避免编辑器报错 ──
        // 飞行物副本的碰撞体快照，不参与正式物理结算。
        Collider[] colliders=projGo.GetComponentsInChildren<Collider>(true);
        // 当前副本碰撞体的零起始下标。
        for(int index=0;index<colliders.Length;index++) colliders[index].enabled=false;

        // ── 计算发射点和方向 ──
        EnsurePreviewInstance(); 
        // 动画实际采样的隔离模型副本。
        var eff=GetEffectivePreviewModel();
        // 飞行物发射点的世界位置，单位米。
        Vector3 firePos=Vector3.zero;
        // 发射点的世界四元数朝向。
        Quaternion fireRot=Quaternion.identity;
        if(eff!=null){
            firePos=GetProjectileFirePosition(p,eff);
            fireRot=eff.transform.rotation;
        }
        // 沿发射点朝向的世界飞行方向。
        Vector3 fireDir=fireRot*Vector3.forward;

        // ── 应用旋转 ──
        Quaternion finalRot=Quaternion.LookRotation(fireDir)*Quaternion.Euler(p.spawnRotation);
        projGo.transform.position=firePos;
        projGo.transform.rotation=finalRot;

        // ── 应用缩放 ──
        if(p.scale!=Vector3.zero) projGo.transform.localScale=p.scale;

        // ── 计算速度 ──
        float speed=Mathf.Max(p.speed,0.1f);
        // 飞行物预览的世界速度向量，单位米每秒。
        Vector3 velocity=fireDir*speed;

        // ── 散射多发 ──
        if(p.shotCount>1){
            // 总散射角的一半，单位度。
            float halfSpread=p.spreadAngle*0.5f;
            // 当前散射发射序号，零起始。
            for(int s=0;s<p.shotCount;s++){
                // 当前散射发射方向相对正前方的夹角，单位度。
                float angle=Mathf.Lerp(-halfSpread,halfSpread,(float)s/(p.shotCount-1));
                // 本次散射射线旋转后的世界方向。
                Vector3 spreadDir=Quaternion.AngleAxis(angle,Vector3.up)*fireDir;
                // 散射副本生成的世界旋转。
                Quaternion spreadRot=Quaternion.LookRotation(spreadDir)*Quaternion.Euler(p.spawnRotation);
                if(s==0){
                    // 第一发用已创建的实例
                    projGo.transform.rotation=spreadRot;
                    velocity=spreadDir*speed;
                }else{
                    // 额外发射创建副本
                    var extraGo=Instantiate(p.prefab);
                    extraGo.hideFlags=HideFlags.DontSave|HideFlags.HideInHierarchy;
                    extraGo.AddComponent<EditorPreviewFxTag>().hideFlags=HideFlags.DontSave|HideFlags.HideInInspector;
                    // 散射副本碰撞体快照，仅禁用本窗口实例。
                    Collider[] extraColliders=extraGo.GetComponentsInChildren<Collider>(true);
                    // 散射副本的当前碰撞体下标。
                    for(int index=0;index<extraColliders.Length;index++) extraColliders[index].enabled=false;
                    extraGo.transform.position=firePos;
                    extraGo.transform.rotation=spreadRot;
                    if(p.scale!=Vector3.zero) extraGo.transform.localScale=p.scale;
                    _previewProjInstances.Add(new PreviewProjEntry{go=extraGo,velocity=spreadDir*speed,elapsed=0f,lifetime=p.lifetime,particles=InitializePreviewParticles(extraGo)});
                }
            }
        }

        _previewProjInstances.Add(new PreviewProjEntry{go=projGo,velocity=velocity,elapsed=0f,lifetime=p.lifetime,particles=InitializePreviewParticles(projGo)});
    }

    /// <summary>清理所有投掷物预览实例</summary>
    void CleanPreviewProj(){ 
        // 此窗口拥有的飞行物列表下标，倒序销毁不触及正式资源。
        for(int index=_previewProjInstances.Count-1;index>=0;index--)
            if(_previewProjInstances[index].go!=null) DestroyImmediate(_previewProjInstances[index].go);
        _previewProjInstances.Clear();
    }
    #endregion

    #region SceneGUI
    /// <param name="sv">当前 Scene 视图引用，聚焦只改变编辑相机。</param>
    /// <summary>仅在编辑模式且已选择配置时绘制技能参考，不介入正式游戏角色逻辑。</summary>
    void OnSceneGUICallback(SceneView sv){ if(!Application.isPlaying && configFile!=null) OnSceneGUI_Draw(sv); }
    /// <param name="sv">当前 Scene 视图引用，聚焦只改变编辑相机。</param>
    /// <summary>绘制效果、位移及碰撞的 Scene 参考和编辑手柄；不模拟物理结算。</summary>
    void OnSceneGUI_Draw(SceneView sv){
        if(configFile==null || Application.isPlaying) return;
        // 当前隔离角色副本，不能作为正式游戏玩家使用。
        GameObject actor=null;
        if(previewModel!=null){EnsurePreviewInstance();actor=GetEffectivePreviewModel();}
        if(actor!=null && IsPreviewAllowed){
            // 模型到绘图空间的变换矩阵，保留原角色朝向和缩放。
            var matrix=actor.transform.localToWorldMatrix;
            using(new Handles.DrawingScope(Handles.color, Handles.matrix)){
                DrawFxSceneHandles(actor,matrix);
                DrawMoveSegmentSceneHandles(actor,matrix);
            }
        }
        DrawCollisionScene(actor!=null?actor.transform:null);
    }

    // Scene标签样式（延迟初始化）
    static GUIStyle _sceneLabelStyle, _sceneSelLabelStyle;
    /// <summary>_sceneFxLabelStyle：普通粒子 Scene 标签样式懒加载缓存，只用于绘制，不保存进技能配置。；_sceneSelFxLabelStyle：选中粒子 Scene 标签样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    static GUIStyle _sceneFxLabelStyle, _sceneSelFxLabelStyle;

    /// <summary>在 Scene 视图中绘制所有当前帧活跃特效的位置标记和拖拽 Handle</summary>
    /// <param name="eff">动画实际采样的隔离模型副本。</param>
    /// <param name="l2w">模型局部到世界的绘图矩阵。</param>
    void DrawFxSceneHandles(GameObject eff,Matrix4x4 l2w){
        if(fxAndSoundList==null||fxAndSoundList.Count==0) return;
        Handles.matrix=Matrix4x4.identity;
        EnsureSceneLabelStyles();

        // 当前粒子选择的原列表下标，负一表示未选中。
        int selFxIdx=selType==SelType.Fx&&selIdx>=0&&selIdx<fxAndSoundList.Count?selIdx:-1;

        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<fxAndSoundList.Count;i++){
            // 当前原粒子/声音事件引用，预览只实例化其资源。
            var fx=fxAndSoundList[i];
            if(fx.particleSystem==null) continue;
            // 挂点缺失时不绘制错误的角色根节点标记；真正生成时会报告配置错误。
            Transform anchor;
            // 当前 Scene 参考点的世界位置，单位米。
            Vector3 worldPos;
            // 五种定位规则解算的特效世界四元数朝向。
            Quaternion effectRotation;
            if(!TryResolvePreviewFxPose(eff,fx,false,out anchor,out worldPos,out effectRotation)) continue;

            // 判断当前帧是否在特效活跃范围内
            int begin=fx.keyNumber;
            // 当前事件区间的有效末尾帧，用于范围限制。
            int end=fx.endKeyNumber>fx.keyNumber?fx.endKeyNumber:fx.keyNumber;
            // 当前帧是否落在原事件有效区间内。
            bool isActive=frameSelectIndex>=begin&&frameSelectIndex<=end;

            // 计算世界位置

            // 当前 Scene 事件是否选中，决定是否显示编辑手柄。
            bool isSel=i==selFxIdx;

            // 活跃的特效 或 选中的特效 才显示
            if(isActive||isSel){
                // 依据 Scene 缩放估算的世界手柄尺寸。
                float handleSize=HandleUtility.GetHandleSize(worldPos)*0.05f;
                // 当前粒子参考点的选中或普通显示颜色。
                Color dotColor=isSel?new Color(0.3f,1f,0.5f,1f):new Color(0.3f,0.85f,0.45f,0.6f);
                Handles.color=dotColor;
                Handles.SphereHandleCap(0,worldPos,Quaternion.identity,handleSize*2f,EventType.Repaint);

                // 标签
                // 资源原名不翻译，只有空引用占位使用当前语言。
                string label = SkillEditorLocalization.Text($"FX[{i}]", $"特效[{i}]");
                if (fx.particleSystem != null) label = fx.particleSystem.name;
                if(isActive) label=SkillEditorLocalization.Text($" {label} F{begin}", $" {label} 第{begin}帧");
                if(_sceneLabelStyle!=null){
                    // 当前粒子参考标签的选中或普通显示样式。
                    var fxLabelStyle=isSel?_sceneSelFxLabelStyle:_sceneFxLabelStyle;
                    if(fxLabelStyle!=null) Handles.Label(worldPos+Vector3.up*handleSize*4f,label,fxLabelStyle);
                }

                // 选中的特效：完整的 Position + Rotation Handle
                if(isSel){
                    // ── Position Handle ──
                    // 特效位置手柄坐标朝向，挂点模式沿用挂点旋转。
                    Quaternion handleRot=Quaternion.identity;
                    if(anchor!=null) handleRot=anchor.rotation;
                    EditorGUI.BeginChangeCheck();
                    // 位置手柄编辑得到的世界米数。
                    Vector3 newWorldPos=Handles.DoPositionHandle(worldPos,handleRot);
                    if(EditorGUI.EndChangeCheck()){
                        Undo.RecordObject(configFile,SkillEditorLocalization.Text("Move effect", "移动特效"));
                        if(anchor!=null)
                            fx.offset=anchor.InverseTransformPoint(newWorldPos);
                        else
                            fx.offset+=newWorldPos-worldPos;
                    }

                    // ── Rotation Handle ──
                    // 旋转手柄使用的当前世界朝向。
                    Quaternion curRot=effectRotation;
                    EditorGUI.BeginChangeCheck();
                    // 旋转手柄输出的新世界四元数。
                    Quaternion newRot=Handles.DoRotationHandle(curRot,worldPos);
                    if(EditorGUI.EndChangeCheck()){
                        Undo.RecordObject(configFile,SkillEditorLocalization.Text("Rotate effect", "旋转特效"));
                        if(anchor!=null)
                            fx.rotation=(Quaternion.Inverse(anchor.rotation)*newRot).eulerAngles;
                        else
                            fx.rotation=newRot.eulerAngles;
                    }

                    // ── Scale 指示（非交互，仅可视化） ──
                    if(fx.scale!=Vector3.one){
                        // 特效三轴缩放平均倍率，仅用于绘制参考圆。
                        float avgScale=(fx.scale.x+fx.scale.y+fx.scale.z)/3f;
                        Handles.color=new Color(0.3f,1f,0.5f,0.15f);
                        Handles.DrawWireDisc(worldPos,Vector3.up,handleSize*8f*avgScale);
                    }
                }
                else if(fx.rotation!=Vector3.zero){
                    // 非选中但有旋转：小箭头指示朝向
                    Quaternion rot=effectRotation;
                    Handles.color=new Color(0.3f,1f,0.5f,0.4f);
                    Handles.ArrowHandleCap(0,worldPos,rot,handleSize*8f,EventType.Repaint);
                }
            }
        }

        // ── F键聚焦到选中特效 ──
        if(selFxIdx>=0&&Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.F){
            // 当前原粒子/声音事件引用，预览只实例化其资源。
            var fx=fxAndSoundList[selFxIdx];
            // 聚焦粒子时解析到的隔离挂点。
            Transform focusAnchor;
            // 聚焦粒子的世界位置，单位米。
            Vector3 focusPos;
            // 聚焦事件的解算旋转，接口保留完整姿态输出。
            Quaternion focusRotation;
            if(!TryResolvePreviewFxPose(eff,fx,false,out focusAnchor,out focusPos,out focusRotation)) return;
            // 当前 Scene 视图引用，聚焦只改变编辑相机。
            SceneView sv=SceneView.lastActiveSceneView;
            if(sv!=null) sv.LookAt(focusPos,sv.rotation,2f);
            Event.current.Use();
        }
    }

    /// <summary>SceneView 位移段可视化：方向箭头、目标点标记、落点、范围圈、偏移 Handle。</summary>
    /// <param name="eff">动画实际采样的隔离模型副本。</param>
    /// <param name="l2w">模型局部到世界的绘图矩阵。</param>
    void DrawMoveSegmentSceneHandles(GameObject eff,Matrix4x4 l2w){
        if(moveSegmentList==null||moveSegmentList.Count==0) return;
        EnsureSceneLabelStyles();

        // 预览角色根世界位置，单位米。
        Vector3 selfPos=eff.transform.position;
        // 预览角色根世界朝向，不改源对象。
        Quaternion selfRot=eff.transform.rotation;
        // 根据 Scene 视角估算的参考手柄世界尺寸。
        float hs=HandleUtility.GetHandleSize(selfPos);

        // 目标：优先锁定目标，其次最近敌人；编辑器内无运行时索敌，退化为「无目标」提示。
        Transform target=null;
        // 隔离 Actor 上的原目标提供接口，只用于制作参考。
        IActionTargetProvider targetProvider=null;
        // 当前真实动画子资源引用。
        foreach(var c in eff.GetComponents<MonoBehaviour>()) if(c is IActionTargetProvider /* 当前脚本组件实现的强类型目标接口。 */ provider){ targetProvider=provider; break; }
        if(targetProvider!=null) target=targetProvider.ResolveTarget(Global.MoveTargetKind.LockedTarget);
        if(target==null){
            // 尝试从场景找最近的通用动作 Actor。
            target=FindNearestSceneTarget(eff);
        }

        // 当前位移选择的原列表下标，负一表示未选中。
        int selMoveIdx=selType==SelType.Move&&selIdx>=0&&selIdx<moveSegmentList.Count?selIdx:-1;

        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<moveSegmentList.Count;i++){
            // 原位移段引用，仅制作曲线或根运动参数。
            var m=moveSegmentList[i];
            // begin：事件区间起始帧，零起始。；end：当前事件区间的有效末尾帧，用于范围限制。
            int begin=m.keyNumber,end=SegEnd(m.keyNumber,m.endKeyNumber);
            // 当前帧是否落在原事件有效区间内。
            bool isActive=frameSelectIndex>=begin&&frameSelectIndex<=end;
            // 当前 Scene 事件是否选中，决定是否显示编辑手柄。
            bool isSel=i==selMoveIdx;

            if(m.driveMode==Global.MoveDriveMode.Animation){
                if(isSel||isActive){
                    Handles.color=new Color(0.5f,0.8f,1f,isSel?1f:0.5f);
                    Handles.ArrowHandleCap(0,selfPos,selfRot,hs*1.5f,EventType.Repaint);
                    if(_sceneLabelStyle!=null) Handles.Label(selfPos+Vector3.up*hs*0.8f,SkillEditorLocalization.Text($"Root motion x{m.rootMotionScale:F1} F{begin}-{end}", $"根运动 ×{m.rootMotionScale:F1} 第{begin}-{end}帧"),
                        isSel?_sceneSelLabelStyle:_sceneLabelStyle);
                }
                continue;
            }

            // ── 方向解算（复用 MoveSolver） ──
            var ctx=new MoveSolveContext{
                selfPosition=selfPos, selfRotation=selfRot, inputDirection=Vector3.zero,
                hasTarget=target!=null,
                targetPosition=target!=null?target.position:Vector3.zero,
                targetRotation=target!=null?target.rotation:Quaternion.identity };
            // 当前制作参考使用的世界方向向量。
            Vector3 dir=MoveSolver.ResolveDirection(m,ctx);
            // 位移参考的目标落点世界米数。
            Vector3 targetPoint=MoveSolver.ResolveTargetPoint(m,ctx);

            if(!(isSel||isActive)) continue;

            // ── 方向箭头 ──
            Handles.color=isSel?new Color(0.3f,1f,0.5f,1f):new Color(0.3f,1f,0.5f,0.4f);
            Handles.ArrowHandleCap(0,selfPos,Quaternion.LookRotation(dir,Vector3.up),hs*1.2f,EventType.Repaint);

            // ── 目标点标记 + 连线（仅中心为目标时） ──
            if(m.origin==Global.MoveOrigin.Target){
                if(ctx.hasTarget){
                    Handles.color=new Color(1f,0.6f,0.2f,isSel?1f:0.5f);
                    Handles.DrawDottedLine(ctx.targetPosition,targetPoint,4f);
                    Handles.SphereHandleCap(0,targetPoint,Quaternion.identity,hs*0.08f,EventType.Repaint);
                    if(_sceneLabelStyle!=null) Handles.Label(targetPoint+Vector3.up*hs*0.3f,SkillEditorLocalization.Text("Target point", "目标位置"),PreviewLabelStyle(isSel));
                }else if(isSel){
                    if(_sceneLabelStyle!=null) Handles.Label(selfPos+Vector3.up*hs*0.9f,SkillEditorLocalization.Text("No preview target assigned.", "未指定预览目标。"),_sceneLabelStyle);
                }
            }

            // ── 预期落点（按曲线终值与最大距离裁剪推算） ──
            if(m.maxDistance>0f){
                // 位移曲线归一化进度一处的累计值。
                float endVal=m.curve!=null?m.curve.Evaluate(1f):1f;
                // 按曲线终值与最大距离裁剪后的参考米数。
                float finalDist=Mathf.Min(endVal*m.maxDistance,m.maxDistance);
                // 按原曲线估算的世界落点，只有可视化意义。
                Vector3 landing=selfPos+dir*finalDist;
                Handles.color=new Color(0.3f,0.7f,1f,isSel?0.9f:0.4f);
                Handles.DrawWireDisc(landing,Vector3.up,hs*0.12f);
                if(_sceneLabelStyle!=null&&isSel) Handles.Label(landing+Vector3.up*hs*0.2f,SkillEditorLocalization.Text($"Destination {finalDist:F1}m", $"落点 {finalDist:F1}米"),_sceneLabelStyle);
            }

            // ── maxDistance 范围圈（以角色为心） ──
            if(m.maxDistance>0f&&isSel){
                Handles.color=new Color(0.3f,0.7f,1f,0.25f);
                Handles.DrawWireDisc(selfPos,Vector3.up,m.maxDistance);
            }

            // ── minKeepDistance 范围圈（以目标为心） ──
            if(m.minKeepDistance>0f&&ctx.hasTarget&&isSel){
                Handles.color=new Color(1f,0.3f,0.3f,0.3f);
                Handles.DrawWireDisc(ctx.targetPosition,Vector3.up,m.minKeepDistance);
            }

            // ── targetOffset 的 PositionHandle（仅中心为目标且有目标时） ──
            if(isSel&&m.origin==Global.MoveOrigin.Target&&ctx.hasTarget){
                EditorGUI.BeginChangeCheck();
                // 目标偏移手柄输出的新世界落点。
                Vector3 newPoint=Handles.DoPositionHandle(targetPoint,Quaternion.identity);
                if(EditorGUI.EndChangeCheck()){
                    Undo.RecordObject(configFile,SkillEditorLocalization.Text("Move target offset", "移动目标偏移"));
                    // 新落点相对目标的偏移，按原坐标空间转换后保存。
                    Vector3 newOffset=m.offsetSpace==Global.OffsetSpace.TargetLocal
                        ? Quaternion.Inverse(ctx.targetRotation)*(newPoint-ctx.targetPosition)
                        : newPoint-ctx.targetPosition;
                    m.targetOffset=newOffset;
                }
            }

            // ── 标签 ──
            if(_sceneLabelStyle!=null)
                Handles.Label(selfPos+Vector3.up*hs*1.2f,$" {MoveSegLabel(m,begin,end)}",isSel?_sceneSelLabelStyle:_sceneLabelStyle);
        }
    }

    /// <param name="self">当前隔离预览角色，用作最近目标搜索中心。</param>
    /// <summary>在角色二十米范围寻找最近通用 Actor，仅用于位移制作参考，不代替正式索敌。</summary>
    Transform FindNearestSceneTarget(GameObject self){
        // 当前最近目标的距离米数，初始无穷大。
        float closest=float.MaxValue; 
        // 最近通用 Actor 的节点引用，未找到时为空。
        Transform best=null;
        // 二十米范围物理查询命中快照，仅用于 Scene 目标参考。
        var cols=Physics.OverlapSphere(self.transform.position,20f);
        // 二十米查询中的当前碰撞体命中引用。
        foreach(var col in cols){
            if(col.transform.root==self.transform.root) continue;
            // 命中节点或父级上的通用 Actor 接口。
            var d=col.GetComponent<IActionActor>(); if(d==null) d=col.GetComponentInParent<IActionActor>();
            if(d==null) continue;
            // 当前候选目标与预览角色的世界距离，单位米。
            float dist=Vector3.Distance(self.transform.position,col.transform.position);
            if(dist<closest){ closest=dist; best=col.transform; }
        }
        return best;
    }
    /// <summary>延迟初始化 Scene 标签样式，避开编辑器初次加载时 EditorStyles 尚未就绪。</summary>
    static void EnsureSceneLabelStyles(){
        // EditorStyles 在某些时序下（如 OnEnable 首次调用）可能还未初始化，此时 boldLabel 为 null
        try{
            if(EditorStyles.boldLabel==null) return;
        }catch{ return; }
        if(_sceneLabelStyle==null){
            _sceneLabelStyle=new GUIStyle(EditorStyles.boldLabel);
            _sceneLabelStyle.normal.textColor=new Color(1f,.6f,.2f,.8f);
            _sceneLabelStyle.fontSize=10;
        }
        if(_sceneSelLabelStyle==null){
            _sceneSelLabelStyle=new GUIStyle(EditorStyles.boldLabel);
            _sceneSelLabelStyle.normal.textColor=new Color(1f,.2f,.2f,1f);
            _sceneSelLabelStyle.fontSize=11;
        }
        if(_sceneFxLabelStyle==null){
            _sceneFxLabelStyle=new GUIStyle(EditorStyles.boldLabel);
            _sceneFxLabelStyle.normal.textColor=new Color(0.3f,0.85f,0.45f,0.8f);
            _sceneFxLabelStyle.fontSize=10;
        }
        if(_sceneSelFxLabelStyle==null){
            _sceneSelFxLabelStyle=new GUIStyle(EditorStyles.boldLabel);
            _sceneSelFxLabelStyle.normal.textColor=new Color(0.3f,1f,0.5f,1f);
            _sceneSelFxLabelStyle.fontSize=11;
        }
    }
    #endregion

    /// <summary>停动画时钟、清特效并销毁隔离角色副本，下次采样从固定根姿态重建。</summary>
    void StopPreviewSession(){
        playFrame=false; playTimer=0; last_frameSelectIndex=-1;
        _previewStopped=true; _previewFrameDirty=true;
        CleanPreviewFx(); DestroyPreviewInstance(); DestroyPreviewAudioSource();
    }
    /// <summary>从固定预览原点重建指定帧的动画姿态与声明的水平根位移；重复采样不累加位置。</summary>
    /// <param name="target">窗口创建的隔离角色副本，不能传入正式角色或预制体资产。</param>
    /// <param name="frame">显式时间轴的目标帧索引；回退、重复采样与循环均从同一初始根姿态开始。</param>
    void SampleExplicitPreview(GameObject target,int frame){
        // 预览使用的动画组件，允许绑定模型藏在角色包装节点内。
        Animator animator=target.GetComponentInChildren<Animator>();
        if(animator==null) throw new InvalidOperationException(SkillEditorLocalization.Text("Preview requires an Animator.", "预览需要 Animator 组件。"));
        // 仅副本禁用自动根运动；正式角色和动画资产的根运动设置保持原值。
        animator.applyRootMotion=false;
        // 原 Y 的手工动画输出组件只存在于预览副本，保留多段动画的混合接口。
        ActionClipAnimator output=animator.GetComponent<ActionClipAnimator>();
        if(output==null) output=animator.gameObject.AddComponent<ActionClipAnimator>();
        output.enabled=true;
        output.ResetOutput();
        RestorePreviewRootPose();
        try{
            // 显式时间轴的每秒帧数，动画源秒数换算仍交由原输出组件处理。
            float fps=ActionTiming.FrameRate(configFile);
            // 排序快照保留原段对象引用，避免改变编辑稿中的列表顺序。
            List<Global.AnimClipSegment> segments=animSegments.Where(/* 当前采样的原动画段引用，不改剪辑资产。 */ segment=>segment!=null && segment.clip!=null).OrderBy(/* 当前采样的原动画段引用，不改剪辑资产。 */ segment=>segment.startFrame).ToList();
            // 段快照的顺序索引，范围为零至快照数量减一。
            for(int segmentIndex=0;segmentIndex<segments.Count;segmentIndex++){
                // 当前需要重建的动画段；只回放目标帧之前已开始的段。
                Global.AnimClipSegment segment=segments[segmentIndex];
                if(segment.startFrame>frame) break;
                output.Sample(segment.startFrame);
                output.Play(segment,fps);
            }
            output.Sample(frame);
        }finally{
            // Generic 根 Transform 曲线也不能把副本搬走；骨骼采样结果及挂点姿态仍保留。
            RestorePreviewRootPose();
        }
        // 只在成功采样后应用作者声明的动画位移；总从初始姿态积分，保留先前跑远修复。
        Vector3 rootDisplacement=SkillEditorRootMotionPreview.Evaluate(configFile,animator,frame);
        if(_previewInstance!=null) _previewInstance.transform.localPosition=_previewActorLocalPosition+_previewActorLocalRotation*rootDisplacement;
    }
    #region 预览实例
    /// <summary>创建窗口专用角色副本；预制体的历史场景位置不参与预览，场景源沿用原实例化姿态。</summary>
    void EnsurePreviewInstance(){
        if(previewModel==null){ DestroyPreviewInstance(); return; }
        if(!PrefabUtility.IsPartOfPrefabAsset(previewModel) && (configFile==null || !configFile.UsesExplicitTiming)){ DestroyPreviewInstance(); previewAnim=previewModel.GetComponent<Animator>(); return; }
        if(_previewInstance!=null){ previewAnim=_previewInstance.GetComponent<Animator>(); return; }
        if(_previewInstanceRoot==null){ _previewInstanceRoot=new GameObject("SkillEditorPreviewRoot"); _previewInstanceRoot.hideFlags=HideFlags.HideAndDontSave; }
        _previewInstanceRoot.SetActive(false);
        _previewInstance=Instantiate(previewModel,_previewInstanceRoot.transform);
        if(PrefabUtility.IsPartOfPrefabAsset(previewModel)) _previewInstance.transform.localPosition=Vector3.zero;
        // 禁用副本中的正式业务脚本，激活预览不会触发角色移动或联网逻辑。
        MonoBehaviour[] behaviours=_previewInstance.GetComponentsInChildren<MonoBehaviour>(true);
        // 副本业务组件的数组索引，包含未激活节点的组件。
        for(int behaviourIndex=0;behaviourIndex<behaviours.Length;behaviourIndex++) behaviours[behaviourIndex].enabled=false;
        _previewActorLocalPosition=_previewInstance.transform.localPosition;
        _previewActorLocalRotation=_previewInstance.transform.localRotation;
        _previewActorLocalScale=_previewInstance.transform.localScale;
        _previewInstanceRoot.SetActive(true);
        _previewInstance.SetActive(true);
        _previewInstance.transform.SetParent(_previewInstanceRoot.transform); _previewInstance.hideFlags=HideFlags.HideAndDontSave;
        // 同显式采样使用相同的 Animator 选择规则；根运动设置只修改窗口副本。
        Animator animator=_previewInstance.GetComponentInChildren<Animator>();
        if(animator!=null){
            _previewAnimatorTransform=animator.transform;
            _previewAnimatorLocalPosition=_previewAnimatorTransform.localPosition;
            _previewAnimatorLocalRotation=_previewAnimatorTransform.localRotation;
            _previewAnimatorLocalScale=_previewAnimatorTransform.localScale;
            if(configFile!=null && configFile.UsesExplicitTiming) animator.applyRootMotion=false;
        }
        previewAnim=_previewInstance.GetComponent<Animator>();
    }
    /// <summary>恢复隔离副本的角色根与 Animator 根初始变换，不触碰其后代骨骼或当前帧动画姿态。</summary>
    void RestorePreviewRootPose(){
        if(_previewInstance==null) return;
        _previewInstance.transform.localPosition=_previewActorLocalPosition;
        _previewInstance.transform.localRotation=_previewActorLocalRotation;
        _previewInstance.transform.localScale=_previewActorLocalScale;
        if(_previewAnimatorTransform==null) return;
        _previewAnimatorTransform.localPosition=_previewAnimatorLocalPosition;
        _previewAnimatorTransform.localRotation=_previewAnimatorLocalRotation;
        _previewAnimatorTransform.localScale=_previewAnimatorLocalScale;
    }
    /// <summary>销毁窗口隔离副本及其图输出，同时丢弃仅属于该副本的根姿态引用。</summary>
    void DestroyPreviewInstance(){
        if(_previewInstance!=null){ DestroyImmediate(_previewInstance); _previewInstance=null; previewAnim=null; }
        if(_previewInstanceRoot!=null){ DestroyImmediate(_previewInstanceRoot); _previewInstanceRoot=null; }
        _previewAnimatorTransform=null;
        if(previewModel==null) previewAnim=null;
    }
    /// <summary>返回动画实际采样的隔离模型副本；没有副本时沿原接口返回源预览模型。</summary>
    GameObject GetEffectivePreviewModel()=> _previewInstance!=null?_previewInstance:previewModel;
    #endregion

    #region 骨骼采样生成连续判定框
    /// <summary>
    /// 在 Attack 的 keyNumber~endKeyNumber 范围内逐帧采样骨骼位置，
    /// 将结果存入 Attack.boneOffsets 列表。一个 Attack 即可覆盖整段帧范围。
    /// </summary>
    /// <param name="target">要查找跟踪骨骼的隔离预览模型。</param>
    Transform ResolvePreviewBone(GameObject target){
        if(_trackBoneRef==null || target==null) return null;
        if(_trackBoneRef==target.transform || _trackBoneRef.IsChildOf(target.transform)) return _trackBoneRef;
        if(previewModel!=null && (_trackBoneRef==previewModel.transform || _trackBoneRef.IsChildOf(previewModel.transform))){
            // 跟踪骨骼相对源预览模型的层级路径，用于在隔离副本中定位。
            string path=AnimationUtility.CalculateTransformPath(_trackBoneRef,previewModel.transform);
            return string.IsNullOrEmpty(path) ? target.transform : target.transform.Find(path);
        }
        return null;
    }
    /// <param name="a">当前原攻击判定引用，不结算伤害。</param>
    /// <summary>对指定攻击帧区间逐帧采样动画，记录骨骼相对角色的偏移作为判定制作数据。</summary>
    void SampleBoneTrackingOffsets(Global.Attack a){
        if(_trackBoneRef==null||previewModel==null){ Debug.LogWarning(SkillEditorLocalization.Text("Assign a tracking bone and preview actor first.", "请先指定跟踪骨骼和预览角色。")); return; }
        EnsurePreviewInstance(); 
        // 动画实际采样的隔离模型副本。
        var eff=GetEffectivePreviewModel(); if(eff==null) return;

        // 隔离角色中解析得到的骨骼节点，供偏移采样。
        var sampledBone=ResolvePreviewBone(eff); if(sampledBone==null){ Debug.LogWarning(SkillEditorLocalization.Text("The tracking bone must belong to the preview actor.", "跟踪骨骼必须属于预览角色。")); return; }
        // 当前攻击采样区间开始帧，零起始。
        int startF=a.keyNumber;
        // 当前攻击采样区间有效末尾帧。
        int endF=a.endKeyNumber>a.keyNumber?a.endKeyNumber:a.keyNumber;
        // 本次攻击骨骼采样区间包含的帧数量。
        int count=endF-startF+1;

        if(a.boneOffsets==null) a.boneOffsets=new List<Vector3>();
        a.boneOffsets.Clear();

        // 当前零起始时间轴帧，用于采样或区间检测。
        for(int f=startF;f<=endF;f++){
            // 采样动画到指定帧
            SampleAnimAtFrame(eff,f);
            // 获取骨骼在角色局部空间的位置
            Vector3 localPos=eff.transform.InverseTransformPoint(sampledBone.position);
            a.boneOffsets.Add(localPos);
        }

        a.useBoneTracking=true;
        a.isReWrite=true;

        // 将首帧的偏移也写入静态offset（作为回退值）
        if(a.boneOffsets.Count>0) a.offset=a.boneOffsets[0];

        // 恢复当前帧动画
        SampleAnim();
        Debug.Log(SkillEditorLocalization.Text($"Bone sampling completed: {count} offsets, frames {startF}-{endF}.", $"骨骼采样完成：{count}个偏移，第{startF}-{endF}帧。"));
    }

    /// <summary>更新单帧的骨骼偏移</summary>
    /// <param name="a">当前原攻击判定引用，不结算伤害。</param>
    /// <param name="frame">要采样的零起始配置时间轴帧。</param>
    void UpdateSingleFrameBoneOffset(Global.Attack a,int frame){
        if(_trackBoneRef==null||previewModel==null) return;
        EnsurePreviewInstance(); 
        // 动画实际采样的隔离模型副本。
        var eff=GetEffectivePreviewModel(); if(eff==null) return;
        // 隔离角色中解析得到的骨骼节点，供偏移采样。
        var sampledBone=ResolvePreviewBone(eff); if(sampledBone==null) return;
        // 指定骨骼/判定相对角色根的局部米数。
        Vector3 localPos=eff.transform.InverseTransformPoint(sampledBone.position);
        // 当前原事件列表的零起始下标。
        int idx=frame-a.keyNumber;
        if(a.boneOffsets==null) a.boneOffsets=new List<Vector3>();
        // 确保列表足够长
        while(a.boneOffsets.Count<=idx) a.boneOffsets.Add(a.offset);
        if(idx>=0&&idx<a.boneOffsets.Count) a.boneOffsets[idx]=localPos;
    }

    /// <summary>采样动画到指定全局帧</summary>
    /// <param name="target">要临时采样动画的隔离角色副本，不修改正式角色。</param>
    /// <param name="frame">要采样的零起始配置时间轴帧。</param>
    void SampleAnimAtFrame(GameObject target,int frame){
        if(configFile!=null && configFile.UsesExplicitTiming){ SampleExplicitPreview(target,frame); return; }
        if(!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        // 当前原动画段引用，裁剪参数使用源剪辑帧。
        for(int segmentIndex=0;segmentIndex<animSegments.Count;segmentIndex++){
            // 旧计时配置中的原动画段，单位为源剪辑帧。
            Global.AnimClipSegment seg=animSegments[segmentIndex];
            if(seg.clip==null) continue;
            if(frame>=seg.startFrame&&frame<seg.EndFrame){
                // 当前时间轴帧相对段起始的源剪辑帧号。
                int localFrame=frame-seg.startFrame+seg.clipStartFrame;
                // 当前计时帧率，单位帧每秒；缺失剪辑时使用原三十帧默认值。
                float fps=30f;
                if(seg.clip.frameRate>0f) fps=seg.clip.frameRate;
                AnimationMode.SampleAnimationClip(target,seg.clip,localFrame/fps);
                return;
            }
        }
    }

    /// <summary>聚焦SceneView到指定世界坐标位置</summary>
    /// <param name="worldPos">当前 Scene 参考点的世界位置，单位米。</param>
    /// <param name="size">Scene 视图聚焦范围的世界尺寸，默认两米。</param>
    void FocusSceneViewOn(Vector3 worldPos,float size=2f){
        // 当前 Scene 视图引用，聚焦只改变编辑相机。
        var sv=SceneView.lastActiveSceneView;
        if(sv!=null) sv.LookAt(worldPos,sv.rotation,Mathf.Max(size,1f));
    }
    #endregion
}
