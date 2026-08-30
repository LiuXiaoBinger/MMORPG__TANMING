using UnityEngine;

public class MLayer
    {
        #region Name
        private static readonly string NAME_DEFAULT                      = "Default";
        private static readonly string NAME_TRANSPARENT_FX               = "TransparentFX";
        private static readonly string NAME_IGNORE_RAYCAST               = "Ignore Raycast";
        private static readonly string NAME_WATER                        = "Water";
        private static readonly string NAME_UI                           = "UI";
        private static readonly string NAME_REAL_WALL                    = "RealWall";
        private static readonly string NAME_TERRAIN                      = "Terrain";
        private static readonly string NAME_MONSTER                      = "Monster";
        private static readonly string NAME_NPC                          = "Npc";
        private static readonly string NAME_ROLE                         = "Role";
        private static readonly string NAME_PLAYER                       = "Player";
        private static readonly string NAME_DUMMY                        = "Dummy";
        private static readonly string NAME_SCENE_FX                     = "SceneFX";
        private static readonly string NAME_SCENE_SMALL_OBJECT           = "SceneSmallObject";
        private static readonly string NAME_AIR_WALL                     = "AirWall";
        private static readonly string NAME_HEAD_PORTRAIT                = "HeadPortrait";
        private static readonly string NAME_SCENE_EVENT                  = "SceneEvent";
        private static readonly string NAME_NPC_DEPTH                    = "NpcDepth";
        private static readonly string NAME_CUTSCENE                     = "CutScene";
        private static readonly string NAME_PHOTO                        = "Photo";
        private static readonly string NAME_FLYING                       = "Flying";
        private static readonly string NAME_ELF                          = "Elf"; //波利团
        private static readonly string NAME_RTFACTORY                    = "RTFactory";
        private static readonly string NAME_POST_PROCESS                 = "PostProcess";
        private static readonly string NAME_ROAM_SCENE                   = "RoamScene";
        private static readonly string NAME_INVISIBLE                    = "Invisible";
        private static readonly string NAME_CUTSCENEUI                   = "CutSceneUI";
        #endregion

        #region ID
        public static readonly int ID_DEFAULT                           = LayerMask.NameToLayer(NAME_DEFAULT);
        public static readonly int ID_TRANSPARENT_FX                    = LayerMask.NameToLayer(NAME_TRANSPARENT_FX);
        public static readonly int ID_CUTSCENE_FX = 3;
        public static readonly int ID_IGNORE_RAYCAST                    = LayerMask.NameToLayer(NAME_IGNORE_RAYCAST);
        public static readonly int ID_WATER                             = LayerMask.NameToLayer(NAME_WATER);
        public static readonly int ID_UI                                = LayerMask.NameToLayer(NAME_UI);
        public static readonly int ID_REAL_WALL                         = LayerMask.NameToLayer(NAME_REAL_WALL);
        public static readonly int ID_TERRAIN                           = LayerMask.NameToLayer(NAME_TERRAIN);
        public static readonly int ID_MONSTER                           = LayerMask.NameToLayer(NAME_MONSTER);
        public static readonly int ID_NPC                               = LayerMask.NameToLayer(NAME_NPC);
        public static readonly int ID_ROLE                              = LayerMask.NameToLayer(NAME_ROLE);
        public static readonly int ID_PLAYER                            = LayerMask.NameToLayer(NAME_PLAYER);
        public static readonly int ID_DUMMY                             = LayerMask.NameToLayer(NAME_DUMMY);
        public static readonly int ID_SCENE_FX                          = LayerMask.NameToLayer(NAME_SCENE_FX);
        public static readonly int ID_SCENE_SMALL_OBJECT                = LayerMask.NameToLayer(NAME_SCENE_SMALL_OBJECT);
        public static readonly int ID_AIR_WALL                          = LayerMask.NameToLayer(NAME_AIR_WALL);
        public static readonly int ID_HEAD_PORTRAIT                     = LayerMask.NameToLayer(NAME_HEAD_PORTRAIT);
        public static readonly int ID_SCENE_EVENT                       = LayerMask.NameToLayer(NAME_SCENE_EVENT);
        public static readonly int ID_NPC_DEPTH                         = LayerMask.NameToLayer(NAME_NPC_DEPTH);
        public static readonly int ID_CUTSCENE                          = LayerMask.NameToLayer(NAME_CUTSCENE);
        public static readonly int ID_PHOTO                             = LayerMask.NameToLayer(NAME_PHOTO);
        public static readonly int ID_FLYING                            = LayerMask.NameToLayer(NAME_FLYING);
        public static readonly int ID_ELF                               = LayerMask.NameToLayer(NAME_ELF);
        public static readonly int ID_RTFACTORY                         = LayerMask.NameToLayer(NAME_RTFACTORY);
        public static readonly int ID_POST_PROCESS                      = LayerMask.NameToLayer(NAME_POST_PROCESS);
        public static readonly int ID_ROAM_SCENE                        = LayerMask.NameToLayer(NAME_ROAM_SCENE);
        public static readonly int ID_INVISIBLE                         = LayerMask.NameToLayer(NAME_INVISIBLE);
        public static readonly int ID_CutSceneUI                        = LayerMask.NameToLayer(NAME_CUTSCENEUI);
        #endregion

        #region Mask
        public static readonly int MASK_DEFAULT                         = 1 << (ID_DEFAULT);
        public static readonly int MASK_TRANSPARENT_FX                  = 1 << (ID_TRANSPARENT_FX);
        public static readonly int MASK_CUTSCENE_FX                     = 1 << (ID_CUTSCENE_FX); // CutScene的特效
        public static readonly int MASK_IGNORE_RAYCAST                  = 1 << (ID_IGNORE_RAYCAST);
        public static readonly int MASK_WATER                           = 1 << (ID_WATER);
        public static readonly int MASK_UI                              = 1 << (ID_UI);
        public static readonly int MASK_REAL_WALL                       = 1 << (ID_REAL_WALL);
        public static readonly int MASK_TERRAIN                         = 1 << (ID_TERRAIN);
        public static readonly int MASK_MONSTER                         = 1 << (ID_MONSTER);
        public static readonly int MASK_NPC                             = 1 << (ID_NPC);
        public static readonly int MASK_ROLE                            = 1 << (ID_ROLE);
        public static readonly int MASK_PLAYER                          = 1 << (ID_PLAYER);
        public static readonly int MASK_DUMMY                           = 1 << (ID_DUMMY);
        public static readonly int MASK_SCENE_FX                        = 1 << (ID_SCENE_FX);
        public static readonly int MASK_SCENE_SMALL_OBJECT              = 1 << (ID_SCENE_SMALL_OBJECT);
        public static readonly int MASK_AIR_WALL                        = 1 << (ID_AIR_WALL);
        public static readonly int MASK_HEAD_PORTRAIT                   = 1 << (ID_HEAD_PORTRAIT);
        public static readonly int MASK_SCENE_EVENT                     = 1 << (ID_SCENE_EVENT);
        public static readonly int MASK_NPC_DEPTH                       = 1 << (ID_NPC_DEPTH);
        public static readonly int MASK_PHOTO                           = 1 << (ID_PHOTO);
        public static readonly int MASK_FLYING                          = 1 << (ID_FLYING);
        public static readonly int MASK_ELF                             = 1 << (ID_ELF);
        public static readonly int MASK_RTFACTOR                        = 1 << (ID_RTFACTORY);
        public static readonly int MASK_POST_PROCESS                    = 1 << (ID_POST_PROCESS);
        public static readonly int MASK_ROAM_SCENE                      = 1 << (ID_ROAM_SCENE);
        public static readonly int MASK_INVISIBLE                       = 1 << (ID_INVISIBLE);
        public static readonly int MASK_CUTSCENEUI                      = 1 << (ID_CutSceneUI);
        public static readonly int MASK_CUTSCENE                        = 1 << (ID_CUTSCENE);
        #endregion

    }
