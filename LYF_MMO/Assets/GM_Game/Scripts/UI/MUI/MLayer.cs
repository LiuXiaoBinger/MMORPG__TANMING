using UnityEngine;

/// <summary>集中保存项目约定的 Unity 图层名称、编号和位掩码；这里只查询已有图层，不创建图层。</summary>
public class MLayer
    {
        #region Name
        /// <summary>默认对象图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_DEFAULT                      = "Default";
        /// <summary>透明特效图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_TRANSPARENT_FX               = "TransparentFX";
        /// <summary>忽略射线图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_IGNORE_RAYCAST               = "Ignore Raycast";
        /// <summary>水面图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_WATER                        = "Water";
        /// <summary>普通界面图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_UI                           = "UI";
        /// <summary>实体墙图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_REAL_WALL                    = "RealWall";
        /// <summary>地形图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_TERRAIN                      = "Terrain";
        /// <summary>怪物图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_MONSTER                      = "Monster";
        /// <summary>NPC图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_NPC                          = "Npc";
        /// <summary>角色图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_ROLE                         = "Role";
        /// <summary>玩家图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_PLAYER                       = "Player";
        /// <summary>假人图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_DUMMY                        = "Dummy";
        /// <summary>场景特效图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_SCENE_FX                     = "SceneFX";
        /// <summary>场景小物件图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_SCENE_SMALL_OBJECT           = "SceneSmallObject";
        /// <summary>空气墙图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_AIR_WALL                     = "AirWall";
        /// <summary>头像渲染图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_HEAD_PORTRAIT                = "HeadPortrait";
        /// <summary>场景事件图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_SCENE_EVENT                  = "SceneEvent";
        /// <summary>NPC 深度图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_NPC_DEPTH                    = "NpcDepth";
        /// <summary>过场图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_CUTSCENE                     = "CutScene";
        /// <summary>拍照图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_PHOTO                        = "Photo";
        /// <summary>飞行图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_FLYING                       = "Flying";
        /// <summary>波利团图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_ELF                          = "Elf"; //波利团
        /// <summary>RenderTexture 工厂图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_RTFACTORY                    = "RTFactory";
        /// <summary>后处理图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_POST_PROCESS                 = "PostProcess";
        /// <summary>漫游场景图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_ROAM_SCENE                   = "RoamScene";
        /// <summary>不可见对象图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_INVISIBLE                    = "Invisible";
        /// <summary>过场界面图层的精确配置名，须与 Unity 图层设置一致。</summary>
        private static readonly string NAME_CUTSCENEUI                   = "CutSceneUI";
        #endregion

        #region ID
        /// <summary>默认对象图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_DEFAULT                           = LayerMask.NameToLayer(NAME_DEFAULT);
        /// <summary>透明特效图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_TRANSPARENT_FX                    = LayerMask.NameToLayer(NAME_TRANSPARENT_FX);
        /// <summary>过场特效的固定图层编号 3；此项没有按名称查询。</summary>
        public static readonly int ID_CUTSCENE_FX = 3;
        /// <summary>忽略射线图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_IGNORE_RAYCAST                    = LayerMask.NameToLayer(NAME_IGNORE_RAYCAST);
        /// <summary>水面图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_WATER                             = LayerMask.NameToLayer(NAME_WATER);
        /// <summary>普通界面图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_UI                                = LayerMask.NameToLayer(NAME_UI);
        /// <summary>实体墙图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_REAL_WALL                         = LayerMask.NameToLayer(NAME_REAL_WALL);
        /// <summary>地形图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_TERRAIN                           = LayerMask.NameToLayer(NAME_TERRAIN);
        /// <summary>怪物图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_MONSTER                           = LayerMask.NameToLayer(NAME_MONSTER);
        /// <summary>NPC图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_NPC                               = LayerMask.NameToLayer(NAME_NPC);
        /// <summary>角色图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_ROLE                              = LayerMask.NameToLayer(NAME_ROLE);
        /// <summary>玩家图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_PLAYER                            = LayerMask.NameToLayer(NAME_PLAYER);
        /// <summary>假人图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_DUMMY                             = LayerMask.NameToLayer(NAME_DUMMY);
        /// <summary>场景特效图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_SCENE_FX                          = LayerMask.NameToLayer(NAME_SCENE_FX);
        /// <summary>场景小物件图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_SCENE_SMALL_OBJECT                = LayerMask.NameToLayer(NAME_SCENE_SMALL_OBJECT);
        /// <summary>空气墙图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_AIR_WALL                          = LayerMask.NameToLayer(NAME_AIR_WALL);
        /// <summary>头像渲染图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_HEAD_PORTRAIT                     = LayerMask.NameToLayer(NAME_HEAD_PORTRAIT);
        /// <summary>场景事件图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_SCENE_EVENT                       = LayerMask.NameToLayer(NAME_SCENE_EVENT);
        /// <summary>NPC 深度图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_NPC_DEPTH                         = LayerMask.NameToLayer(NAME_NPC_DEPTH);
        /// <summary>过场图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_CUTSCENE                          = LayerMask.NameToLayer(NAME_CUTSCENE);
        /// <summary>拍照图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_PHOTO                             = LayerMask.NameToLayer(NAME_PHOTO);
        /// <summary>飞行图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_FLYING                            = LayerMask.NameToLayer(NAME_FLYING);
        /// <summary>波利团图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_ELF                               = LayerMask.NameToLayer(NAME_ELF);
        /// <summary>RenderTexture 工厂图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_RTFACTORY                         = LayerMask.NameToLayer(NAME_RTFACTORY);
        /// <summary>后处理图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_POST_PROCESS                      = LayerMask.NameToLayer(NAME_POST_PROCESS);
        /// <summary>漫游场景图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_ROAM_SCENE                        = LayerMask.NameToLayer(NAME_ROAM_SCENE);
        /// <summary>不可见对象图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_INVISIBLE                         = LayerMask.NameToLayer(NAME_INVISIBLE);
        /// <summary>过场界面图层编号；对应名称不存在时 NameToLayer 返回 -1。</summary>
        public static readonly int ID_CutSceneUI                        = LayerMask.NameToLayer(NAME_CUTSCENEUI);
        #endregion

        #region Mask
        /// <summary>默认对象图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_DEFAULT                         = 1 << (ID_DEFAULT);
        /// <summary>透明特效图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_TRANSPARENT_FX                  = 1 << (ID_TRANSPARENT_FX);
        /// <summary>过场特效的固定图层编号 3；此项没有按名称查询。</summary>
        public static readonly int MASK_CUTSCENE_FX                     = 1 << (ID_CUTSCENE_FX); // CutScene的特效
        /// <summary>忽略射线图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_IGNORE_RAYCAST                  = 1 << (ID_IGNORE_RAYCAST);
        /// <summary>水面图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_WATER                           = 1 << (ID_WATER);
        /// <summary>普通界面图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_UI                              = 1 << (ID_UI);
        /// <summary>实体墙图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_REAL_WALL                       = 1 << (ID_REAL_WALL);
        /// <summary>地形图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_TERRAIN                         = 1 << (ID_TERRAIN);
        /// <summary>怪物图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_MONSTER                         = 1 << (ID_MONSTER);
        /// <summary>NPC图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_NPC                             = 1 << (ID_NPC);
        /// <summary>角色图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_ROLE                            = 1 << (ID_ROLE);
        /// <summary>玩家图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_PLAYER                          = 1 << (ID_PLAYER);
        /// <summary>假人图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_DUMMY                           = 1 << (ID_DUMMY);
        /// <summary>场景特效图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_SCENE_FX                        = 1 << (ID_SCENE_FX);
        /// <summary>场景小物件图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_SCENE_SMALL_OBJECT              = 1 << (ID_SCENE_SMALL_OBJECT);
        /// <summary>空气墙图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_AIR_WALL                        = 1 << (ID_AIR_WALL);
        /// <summary>头像渲染图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_HEAD_PORTRAIT                   = 1 << (ID_HEAD_PORTRAIT);
        /// <summary>场景事件图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_SCENE_EVENT                     = 1 << (ID_SCENE_EVENT);
        /// <summary>NPC 深度图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_NPC_DEPTH                       = 1 << (ID_NPC_DEPTH);
        /// <summary>拍照图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_PHOTO                           = 1 << (ID_PHOTO);
        /// <summary>飞行图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_FLYING                          = 1 << (ID_FLYING);
        /// <summary>波利团图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_ELF                             = 1 << (ID_ELF);
        /// <summary>RenderTexture 工厂图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_RTFACTOR                        = 1 << (ID_RTFACTORY);
        /// <summary>后处理图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_POST_PROCESS                    = 1 << (ID_POST_PROCESS);
        /// <summary>漫游场景图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_ROAM_SCENE                      = 1 << (ID_ROAM_SCENE);
        /// <summary>不可见对象图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_INVISIBLE                       = 1 << (ID_INVISIBLE);
        /// <summary>过场界面图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_CUTSCENEUI                      = 1 << (ID_CutSceneUI);
        /// <summary>过场图层的单层位掩码，由图层编号左移得到，供相机或射线筛选使用。</summary>
        public static readonly int MASK_CUTSCENE                        = 1 << (ID_CUTSCENE);
        #endregion

    }
