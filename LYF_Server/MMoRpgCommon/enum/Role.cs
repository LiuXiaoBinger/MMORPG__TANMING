/// <summary>
/// 角色计数类型，用于区分商城、每日副本和永久副本等次数。
/// </summary>
public enum RoleCountType
{
    /// <summary>商城商品购买次数，具体周期由 PurchaseLimitType 区分。</summary>
    Shop = 1,

    /// <summary>副本购买或进入次数。</summary>
    Dungeon = 2
}


public enum RoleModuleType {
  kRoleModuleTypeNone = 0,
  kRoleModuleTypeBrief = 1,
  kRoleModuleTypeExtraInfo = 2,
  kRoleModuleTypeSkill = 3,
  kRoleModuleTypeQualityPoint = 4,
  kRoleModuleTypeBag = 5,
  kRoleModuleTypeGameRole = 6,
  kRoleModuleTypeTask = 7,
  kRoleModuleTypeFashion = 8,
  kRoleModuleTypeEquip = 9,
  kRoleModuleTypeSticker = 10,
  kRoleModuleTypeVehicle = 11,
  kRoleModuleTypeOpenSystem = 12,
  kRoleModuleTypeShop = 13,
  kRoleModuleTypeDungeon = 14,
  kRoleModuleTypeCount = 15,
  kRoleModuleTypeGuarantee = 16,
  kRoleModuleTypeScript = 17,
  kRoleModuleTypeSevenLogin = 18,
  kRoleModuleTypeLifeSkill = 19,
  kRoleModuleTypeLifeEquip = 20,
  kRoleModuleTypeTempRecord = 21,
  kRoleModuleTypeAchievement = 22,
  kRoleModuleTypeIllustration = 23,
  kRoleModuleTypeHealth = 24,
  kRoleModuleTypeWorldEvent = 25,
  kRoleModuleTypeCatTrade = 26,
  kRoleModuleTypeMedal = 27,
  kRoleModuleTypeTutorialMark = 28,
  kRoleModuleTypePay = 29,
  kRoleModuleTypePostcard = 30,
  kRoleModuleTypeDelegate = 31,
  kRoleModuleTypeThirtySign = 32,
  kRoleModuleTypeSuit = 33,
  kRoleModuleTypeMerchant = 34,
  kRoleModuleTypeLuaActivity = 35,
  kRoleModuleTypeLimitedTimeOffer = 36,
  kRoleModuleTypeMall = 37,
  kRoleModuleTypeGuildAuctionRecordPersonal = 38,
  kRoleModuleTypeDungeonWatch = 39,
  kRoleModuleTypeMercenary = 40,
  kRoleModuleTypeVitalData = 41,
  kRoleModuleTypeCommondata = 42,
  kRoleModuleTypeItemComponent = 43,
  kRoleModuleTypeTag = 44,
  kRoleModuleTypeMonthCard = 45,
  kRoleModuleTypeLuckyPoint = 46,
  kRoleModuleTypeSurprise = 47,
  kRoleModuleTypeCommonAward = 48,
  kRoleModuleTypeChatTag = 49,
  kRoleModuleTypeExtraCardDrop = 50,
  kRoleModuleTypeJifen = 51,
  kRoleModuleTypeCovenant = 52,
  kRoleModuleTypeActivityDraw = 53,
  kRoleModuleTypeSignin = 54,
  kRoleModuleTypeGVG = 55,
  kRoleModuleTypeCallRegress = 56,
  kRoleModuleTypePassCheck = 57,
  kRoleModuleTypeTotalRecharge = 58,
  //RoleModuleType_INT_MIN_SENTINEL_DO_NOT_USE_ = google.protobuf.kint32min,
  //RoleModuleType_INT_MAX_SENTINEL_DO_NOT_USE_ = google.protobuf.kint32max
};

public enum RoleStateFlag
{
    RSF_ISReconnecting,                 // [客户端连接已断开 -> 发起重连]
    RSF_ISConnected,                    // ![离开Gs -> 进入Gs]
    RSF_ISWaitLogin,                    // [Init -> OnLogin]
    RSF_ISAccountBriefChanged,
    RSF_ISRoleDataNeedSave,
    RSF_ISSceneSwitch_Verifying,		//场景切换检查中
    RSF_ISFirstInit,
    RSF_ISLoginReconnect,				// 发起了登录重连
    RSF_ISLoginReconnectFinally,		// 登录重连结束
    RSF_ISLoadingScene,                 // [EnterSceneNtf -> DoEnterScene]
    RSP_ISFirstSaveGameSerialize,		// 第一次登陆保存game_role
    RSP_ISCache,                        // 切场景缓存
    RSP_ISReconnectReadAccount,         // 重连读取account
};