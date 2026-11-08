
require("Table/SkillTable")
declareGlobal("BuffTriggerCondition", {})
--Buff触发条件函数
--[[ 不同触发时机, 目前有以下触发时机
        kBuffTTT_Add,               //buff被添加时
        kBuffTTT_Time,              //定时触发
        kBuffTTT_SomeOneEnter,      //有人进入
        kBuffTTT_BeHit,             //被击中时
        kBuffTTT_Attack,            //击中时
        kBuffTTT_AttributeChange,   //属性变化
        kBuffTTT_Delete,            //buff被删除时
        kBuffTTT_Append,            //buff堆叠时

        kBuffTTT_Invalid,//非法的

    class BuffLuaInfo
    {
       //当前buff的总时间及剩余时间
        float total_time_;
        float left_time_;

        //kBuffTTT_SomeOneEnter时进入的entity
        ROEntity* GetLuaEnterEntity(); 
      
       //kBuffTTT_BeHit
        ROEntity* GetLuaFirerEntity();//被击时的攻击者
        DamageResult* behit_damage_result_; //被击时的伤害结果

        //kBuffTTT_Attack
        ROEntity* GetLuaAttackTarget(); //攻击时的攻击目标
        DamageResult* attack_damage_result_; //攻击时的伤害结果

        BuffTriggerTimeType trigger_timing_;//触发时机
    }
]]
--触发条件检查及触发结果的函数接口
--[[
    firer: 产生buff的对象(不是entity时为nil), 比如A给B加了buff, 那么A是firer, B是hoster
    hoster: buff的持有者
    tableParam: 表里填的参数, 例LuaSpecial|INT=0|RANG=5  函数名为LuaSpecial, tableParam:GetParam("INT") 获取到0, tableParam:GetParam("RANG") 获取到5
    args: BuffLuaInfo, 根据不同触发时机, 有一些额外参数, 看上面说明

    return 返回true/false 表示条件成立/失败
]]
--LuaSpecial|INT=0|RANG=5, 用来测试
function BuffTriggerCondition.LuaSpecial(utilTool, firer, hoster, tableParam,args)
    local selfInt = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
    local param = tableParam:GetParam("INT")
    local range = tableParam:GetParam("RANG")
    if (args.trigger_timing_ == ROGameLibs.kBuffTTT_SomeOneEnter) then
        --utilTool:LuaLog("enter entity:"..tostring(enter_entity))
    end
    return true
end

--通用：检测Hoster是否有buff，有则返回true
function BuffTriggerCondition.BuffCheckHas(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("buff")
    if hoster:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--通用：检测Hoster是否有buff，没有则返回true
function BuffTriggerCondition.BuffCheckNoHas(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("buff")
    if hoster:HasBuff(buffID) then
        return false
    else
        return true
    end
end

--通用：检测Firer是否有buff，有则返回true
function BuffTriggerCondition.BuffCheckFirerHas(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("buff")
	
	if firer == nil then
		return false
	end
	
    if firer:HasBuff(buffID) then
		return true
    else
        return false
    end
end

--通用：检测Firer是否有buff，没有则返回true
function BuffTriggerCondition.BuffCheckFirerNoHas(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("buff")
    
	if firer == nil then
		return false
	end
	
	if firer:HasBuff(buffID) then
        return false
    else
        return true
    end
end

--通用：如果受击时，攻击者与buff释放者为同一人，则返回true
function BuffTriggerCondition.SkillBuffSameFirer(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end
    local firer_entity = args:GetLuaFirerEntity()
    if  firer_entity:GetUUID() == firer:GetUUID() then
        return true
    else
        return false
    end
end

--通用：判定攻击是否是暴击
function BuffTriggerCondition.IsCriDam(utilTool, firer, hoster, tableParam, args)
	local firer_result = args.attack_damage_result_
	if firer_result == nil then
		return false
	end
	if firer_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI then
		return true
	else
		return false
	end
end

--通用：判定是否在战斗中
function BuffTriggerCondition.IsInFight(utilTool, firer, hoster, tableParam, args)
	local inFight = hoster:IsFight()
	if inFight ~= nil then
		return inFight
	else
		return false
	end
end


--通用：如果受击时，攻击者与buff持有者为同一人，则返回false
function BuffTriggerCondition.HosterNoCaster(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end
    local firer_entity = args:GetLuaFirerEntity()
    if  firer_entity:GetUUID() == hoster:GetUUID() then
        return false
    else
        return true
    end
end

--通用：判断攻击属性是否为某一属性
function BuffTriggerCondition.CheckDamElement(utilTool, firer,hoster,tableParam,args)
    local damElement = args.attack_type_
	local needElement = tableParam:GetParam("needElement")
	if damElement == needElement then
		return true
	end
	return false
end  

--通用：判断攻击是否为回血，isHeal为0时，伤害小于0返回false，isHeal为1时，伤害小于0返回true
function BuffTriggerCondition.CheckHeal(utilTool, firer,hoster,tableParam,args)
	local isHeal = tableParam:GetParam("isHeal")
	local damageResult = nil
	if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack or args.trigger_timing_  == ROGameLibs.kBuffTTT_AttackBeforeCalculate then
		damageResult = args.attack_damage_result_
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit or args.trigger_timing_  == ROGameLibs.kBuffTTT_BeHitBeforeCalculate then
		damageResult = args.behit_damage_result_
    end
	
    if damageResult == nil then
        return false
    end
	
	if isHeal == 0 and damageResult:GetResult() ~= 0 then
		if damageResult:GetResult() < 0 then
			return false
		else
			return true
		end
	elseif isHeal == 1 and damageResult:GetResult() ~= 0 then
		if damageResult:GetResult() > 0 then
			return false
		else
			return true
		end
	end
	
	return false
end 

--上毒概率
function BuffTriggerCondition.Poison(utilTool, firer, hoster, tableParam,args) 
    if firer == nil or args.attack_damage_result_ == nil then
        return false
    end

    local buffID = tableParam:GetParam("BUFF")
    local buffPro = tableParam:GetParam("Pro")/10000
    local buff = firer:GetBuff(buffID)
    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    
     --utilTool:LuaLog("hoster 1:"..tonumber(buffID))
     --utilTool:LuaLog("hoster 2:"..tonumber(buffPro))
     --utilTool:LuaLog("hoster 3:"..tonumber(skill_num))
     --utilTool:LuaLog("hoster 4:"..tonumber(skill_ID))
    --and skill_ID == skill_num
    
    if buff ~=nil and 
        RORandom:randDouble(0,1) <= buffPro and 
        damageType == ROGameLibs.kEffectDamSkillType_Common then
        return true
    end

    return false
end

--【通用】owner有buff返回true
function BuffTriggerCondition.OwnerHasBuff(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("BuffID")
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--【通用】owner没有buff返回true
function BuffTriggerCondition.OwnerNoHasBuff(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("BuffID")
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner:HasBuff(buffID) then
        return false
    else
        return true
    end
end

--闪避护盾
function BuffTriggerCondition.ShanbiHudun(utilTool, firer, hoster, tableParam,args) 
    if args.behit_damage_result_ == nil then
        return false
    end

    if args.behit_damage_result_:GetType() == ROGameLibs.DAMAGE_TYPE_MISS then
       return true
    end

    return false
end

--隐匿的蓝量检测
function BuffTriggerCondition.YinniSP(utilTool, firer, hoster, tableParam,args) 
    if firer == nil then
        return false
    end

    local SP = firer:GetAttr(AttrType.ATTR_BASIC_SP)
    if SP <= tableParam:GetParam("SP") then
        return true
    end
	return false
end

--加点体验蓝量检测
function BuffTriggerCondition.JiaDianSPTrigger(utilTool, firer, hoster, tableParam,args) 
    local preSp = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
	local maxSp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
	local triggerSpPer = tableParam:GetParam("per")/10000
    if preSp/maxSp <= triggerSpPer then
        return true
    end
	return false
end

--隐匿伏击眩晕
function BuffTriggerCondition.Fuji(utilTool, firer, hoster, tableParam, args)
    local skillID = args.current_skill_id_
    local pro = tableParam:GetParam("Pro")/10000
    local fujiID = tableParam:GetParam("skill")
    if skillID == fujiID and RORandom:randDouble(0,1) <= pro then
       --print("true")
       return true
    end
    --print("false")
    return false
end

--光猎判定
function BuffTriggerCondition.GuangLie(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
    if enter_entity == nil then
        return false
    end

    if enter_entity:GetAttr(AttrType.ATTR_SPECIAL_HIDING) > 0 then
       return true
    end
    return false
end

--光猎判定是否有伤害
function BuffTriggerCondition.GuangLieHitCheck(utilTool, firer, hoster, tableParam, args)
    if hoster:CheckBuffState(3) == true and hoster:HasBuff(999) ~= true then 
        --print("true")
        return true
    else
        --print("false")
        return false
    end
end

--光壁条件判定, 用来判定没有光壁
function BuffTriggerCondition.GuangBi(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
        return false
    end

    local buffID = tableParam:GetParam("BUFF")
    local buff = firer:GetBuff(buffID)
    return buff == nil
end

--光壁效果判定
function BuffTriggerCondition.GuangBiResult(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end

    local damageResult = args.behit_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if damageType == ROGameLibs.kEffectDamSkillType_Physical or damageType == ROGameLibs.kEffectDamSkillType_Common then
        if args.current_skill_ == nil then
            return false
        end

        local dis = args.current_skill_:GetSkillRangeType()
        if dis == 1 then
            return true
        end
    end

    return false
end


--暗壁效果判定
function BuffTriggerCondition.AnBiResult(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end

    local damageResult = args.behit_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if damageType == ROGameLibs.kEffectDamSkillType_Physical or damageType == ROGameLibs.kEffectDamSkillType_Common then
        if args.current_skill_ == nil then
            return false
        end

        local dis = args.current_skill_:GetSkillRangeType()
        if dis == 0 then
            return true
        end
    end

    return false
end

--普攻判定
function BuffTriggerCondition.AtkCommon(utilTool, firer, hoster, tableParam, args)
    if args.attack_damage_result_ == nil then
        return false
    end

    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if damageType == ROGameLibs.kEffectDamSkillType_Common then
       return true
    end

    return false
end

--检查指定武器类型
function BuffTriggerCondition.WeaponType(utilTool, firer, hoster, tableParam, args)
    local wuqiType = args:GetExtraParamByName("wetype")
    local hosterType = hoster:GetAttr(AttrType.ATTR_SPECIAL_WEAPON_TYPE)

    if args.trigger_timing_ ~= ROGameLibs.kBuffTTT_EquipChange then
        return hosterType == wuqiType
    else
        if args.after_weapon_type == args.before_weapon_type then 
            return false
        elseif wuqiType == args.after_weapon_type then
            return true
        elseif wuqiType == args.before_weapon_type then
            return true
        else
            return false
        end
    end
end

--Hit type(0普攻，1物理技能，2魔法技能，3混合伤害）
function BuffTriggerCondition.HitType(utilTool, firer, hoster, tableParam, args)
    local damageResult = nil
    
    if      args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack or args.trigger_timing_  == ROGameLibs.kBuffTTT_AttackBeforeCalculate then
            damageResult = args.attack_damage_result_
    elseif  args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit or args.trigger_timing_  == ROGameLibs.kBuffTTT_BeHitBeforeCalculate then
            damageResult = args.behit_damage_result_
    end

    if damageResult == nil then
        return false
    end
    
    local damageType = damageResult:GetSkillDamageType()
    local hitTypeTable = tableParam:GetParam("Type")
	if damageType == 0 or damageType == 1 then
		if hitTypeTable == 6 then
			return true
		else
			return hitTypeTable == damageType
		end
	else
		return hitTypeTable == damageType
	end 
end

--AttckType(0普攻，1物理技能，2魔法技能，3混合伤害）
function BuffTriggerCondition.AttackType(utilTool, firer, hoster, tableParam, args)
    local damageResult = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack or args.trigger_timing_  == ROGameLibs.kBuffTTT_AttackBeforeCalculate then
        damageResult = args.attack_damage_result_
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit or args.trigger_timing_  == ROGameLibs.kBuffTTT_BeHitBeforeCalculate then
        damageResult = args.behit_damage_result_
    end

    if damageResult == nil then
        return false
    end
    
    local damageType = damageResult:GetSkillDamageType()
    local hitTypeTable = args:GetExtraParamByName("type")
	
	if damageType == 0 or damageType == 1 then
		if hitTypeTable == 6 then
			return true
		else
			return hitTypeTable == damageType
		end
	else
		return hitTypeTable == damageType
	end    
end

--Check Hoster State
function BuffTriggerCondition.CheckHosterState(utilTool, firer, hoster, tableParam, args)
    local entityState = tableParam:GetParam("State")
    return hoster.CheckBuffState(entityState) == true
end

--OwnerBuffCheck(检测召唤兽主人是否拥有buff)
function BuffTriggerCondition.OwnerBuffCheck(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("buff")
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--指定怪物类伤害增加
function BuffTriggerCondition.MonsterDamage(utilTool, firer, hoster, tableParam, args)
    local attack_target_entity = args:GetLuaAttackTarget()
	
    if attack_target_entity == nil then
        return false
    end
    local monster1 = attack_target_entity:GetMonsterIDInEntityTable()
    local monster2 = tableParam:GetParam("monster")
    return monster1 == monster2
end

--对非指定类型伤害增加
function BuffTriggerCondition.TargetType(utilTool, firer, hoster, tableParam, args)
    local attack_target_entity = args:GetLuaAttackTarget()
    if attack_target_entity == nil then
        return false
    end

    local type1 = attack_target_entity:GetUnitTypeLevel()
    local type2 = tableParam:GetParam("type")

    return type1 ~= type2
end

--HitRange检测是单体还是群体
function BuffTriggerCondition.HitRange(utilTool, firer, hoster, tableParam, args)
	if args.current_skill_ == nil then
		return false
	end
    local rangeType = tableParam:GetParam("Type")
    return args.current_skill_:GetSkillRangeType() == rangeType
end

--目标职业
function BuffTriggerCondition.JobType(utilTool, firer, hoster, tableParam, args)
    local job = math.floor(args:GetExtraParamByName("zhiye")/100)
	local jobNow =math.floor(hoster:GetAttr(AttrType.ATTR_SPECIAL_JOB)/100)

    return jobNow == job 	
end

--CheckIsBuffSkill
function BuffTriggerCondition.CheckIsBuffSkill(utilTool, firer, hoster, tableParam, args)
    if args.current_skill_ == nil then
        return false
    end
    return args.current_skill_:IsBuffAddSkill() == false
end

--Check enemy Race
function BuffTriggerCondition.CheckRace(utilTool, firer, hoster, tableParam, args)
    local entityRace = tableParam:GetParam("Type") 
    
    local attack_target_entity = args:GetLuaAttackTarget()
    if attack_target_entity ~= nil and
        attack_target_entity:GetUnitTypeLevel() ~= -1 and 
        attack_target_entity:GetAttr(AttrType.ATTR_SPECIAL_RACE) == entityRace then
        return true
    end

    local firer_entity = args:GetLuaFirerEntity()
    if firer_entity ~= nil and
        firer_entity:GetUnitTypeLevel() ~= -1 and 
        firer_entity:GetAttr(AttrType.ATTR_SPECIAL_RACE) == entityRace then
        return true
    end
    
    return false
end

--Check enemy Race
function BuffTriggerCondition.Race(utilTool, firer, hoster, tableParam, args)
    local entityRace = args:GetExtraParamByName("race")

    local attack_target_entity = args:GetLuaAttackTarget()
    if  attack_target_entity~= nil and 
        attack_target_entity:GetUnitTypeLevel() ~= -1 and 
        attack_target_entity:GetAttr(AttrType.ATTR_SPECIAL_RACE) == entityRace then
        return true
    end

    local firer_entity = args:GetLuaFirerEntity()
    if  firer_entity~= nil and 
        firer_entity:GetUnitTypeLevel() ~= -1 and 
        firer_entity:GetAttr(AttrType.ATTR_SPECIAL_RACE) == entityRace then
        return true
    end     
    
    return false
end

--Check enemy Ele2
function BuffTriggerCondition.EleType(utilTool, firer, hoster, tableParam, args)
    local entityEle = args:GetExtraParamByName("ele")

    if  args.defense_type_ > 100 then
       args.defense_type_ = math.floor(args.defense_type_/100)
    end
    
    --我攻击时,获取被击对象的防御元素属性
    local attack_target_entity = args:GetLuaAttackTarget()
    if attack_target_entity ~= nil and 
        attack_target_entity:GetUnitTypeLevel() ~= -1 and 
        entityEle == args.defense_type_ then
        return true
    end 
    
    --我被击时,获取攻击对象的攻击元素属性
    local firer_entity = args:GetLuaFirerEntity()
    if firer_entity ~= nil and 
        firer_entity:GetUnitTypeLevel() ~= -1 and 
        entityEle == args.attack_type_ then
        return true
    end
    
    return false
end

--Aspersio Check enemy race 撒水祈福确认怪物恶魔种族和不死属性
function BuffTriggerCondition.AspersioType(utilTool, firer, hoster, tableParam, args)
	local entityRace1 = tableParam:GetParam("type1")
	local entityRace2 = tableParam:GetParam("type2")
	
	--攻击时,获取被击对象的种族
	local attack_target_entity = args:GetLuaAttackTarget()
	local target_race = attack_target_entity:GetAttr(AttrType.ATTR_SPECIAL_RACE)
	--攻击时,获取被击对象的防御元素属性
	if target_race ~= nil and entityRace1 ~= nil and entityRace2 ~= nil then
		if target_race == entityRace1 or args.defense_type_== entityRace2 then
			return true
		end
	end
	return false
end

--Check skill
function BuffTriggerCondition.CurSkill(utilTool, firer, hoster, tableParam, args)
    local skillID = args:GetExtraParamByName("skill")
	local skillID2 = tableParam:GetParam("skill2")
	local skillCur
	if skillID == nil or skillID ==0 then 
		skillCur = skillID2
	else
		skillCur = skillID
	end
	
    return args.current_skill_id_ == skillCur
end

--Check skillUuID
function BuffTriggerCondition.CurSkillUuId(utilTool, firer, hoster, tableParam, args)
    local skillID = args:GetExtraParamByName("skill")
	local skillID2 = tableParam:GetParam("skill2")
	local skillCur

	if skillID == nil or skillID ==0 then 
		skillCur = skillID2
	else
		skillCur = skillID
	end
	return skillCur == args:GetCurrSkillUuid()
end

--贝克战斧Check skill
function BuffTriggerCondition.BeiKeCurSkill(utilTool, firer, hoster, tableParam, args)
    local skillID = 600005
	local skillID2 = 610205
	local skillID3 = 600019
	if args.current_skill_id_ == skillID or 
	args.current_skill_id_ == skillID2 or
	args.current_skill_id_ == skillID3 then 
		return true
	else
		return false
	end
end

--Dice
function BuffTriggerCondition.Dice(utilTool, firer, hoster, tableParam, args)
    local pro = tableParam:GetParam("Pro") /10000
    if RORandom:randDouble(0,1) <= pro then
        return true
    end
    return false
end

--HPCondi
function BuffTriggerCondition.HPCondi(utilTool, firer, hoster, tableParam, args)

	local HPCur = 1
	local HPMax = 1
    local per = args:GetExtraParamByName("pro")
    local attack_target_entity = args:GetLuaAttackTarget()
    if  attack_target_entity ~= nil and 
        attack_target_entity:GetUnitTypeLevel() ~= -1 then
		HPCur = attack_target_entity:GetAttr(AttrType.ATTR_BASIC_HP) 
		HPMax = attack_target_entity:GetAttr(AttrType.ATTR_BASIC_MAX_HP) 
    end
	
	if HPCur/HPMax < per/10000 then
		return true
	end
	
	return false
end

--Dice
function BuffTriggerCondition.Prop(utilTool, firer, hoster, tableParam, args)
    local pro = args:GetExtraParamByName("pro")/10000
    if RORandom:randDouble(0,1) <= pro then
        return true
    end
    return false
end

--概率堆叠用
function BuffTriggerCondition.PropSum(utilTool, firer, hoster, tableParam, args)
	local selfBuff = tableParam:GetParam("selfBuff")
	local buff = hoster:GetBuff(selfBuff)
    local pro = tableParam:GetParam("Pro") /10000
	local buffCount
	
	if buff ~= nil then
		buffCount = buff:GetAppendCount()
	else 
		buffCount = 0
	end
	
	pro = math.min(pro*buffCount,1)
    if RORandom:randDouble(0,1) <= pro then
        return true
    end
    return false
end

--BossType
function BuffTriggerCondition.BossType(utilTool, firer, hoster, tableParam, args)
	local monstertype = args:GetExtraParamByName("type")

    local attack_target_entity = args:GetLuaAttackTarget()

	if  attack_target_entity ~= nil then
		local attackArgetEntityTypeLevel = attack_target_entity:GetUnitTypeLevel()
		if attackArgetEntityTypeLevel == 6 or attackArgetEntityTypeLevel == 7 then --魔物驱逐新增的怪物类型，当做普通怪物处理。20200409
			attackArgetEntityTypeLevel = 0
		end
		
        if attackArgetEntityTypeLevel == monstertype then
			return true
		end
    end
	
    return false
end

--受到boss、mvp、mini攻击返回false
function BuffTriggerCondition.NotBossHit(utilTool, firer, hoster, tableParam, args)
    local entityType = args:GetLuaFirerEntity():GetUnitTypeLevel()
    --print("entityType"..tostring(entityType))
    if  entityType == ROGameLibs.kUnitTypeLevel_BOSS or 
        entityType == ROGameLibs.kUnitTypeLevel_MVP or 
        entityType == ROGameLibs.kUnitTypeLevel_Mini then
        return false
    end
    return true
end


--判断boss、mvp和mini返回false
function BuffTriggerCondition.HighTypeFalse(utilTool, firer, hoster, tableParam, args)
	local entityType = hoster:GetUnitTypeLevel()
	if entityType == ROGameLibs.kUnitTypeLevel_BOSS or entityType == ROGameLibs.kUnitTypeLevel_MVP or entityType == ROGameLibs.kUnitTypeLevel_Mini then
		return false
	end
	return true
end

--判断被击是否有伤害(通用)
function BuffTriggerCondition.BeHitDamageCheck(utilTool, firer, hoster, tableParam,args) 
	if args.behit_damage_result_~=nil and
        args.behit_damage_result_:GetResult() > 0 then
       return true
    end
    return false
end

--判断攻击是否有伤害(通用)
function BuffTriggerCondition.HitDamageCheck(utilTool, firer, hoster, tableParam,args) 
	if args.attack_damage_result_~=nil and
        args.attack_damage_result_:GetResult() > 0 then
       return true
    end
    return false
end

--霸邪判断生效次数
function BuffTriggerCondition.BaXieBeHitCheck(utilTool, firer, hoster, tableParam,args) 
	if args.behit_damage_result_~=nil then
        if args.behit_damage_result_:GetType() == ROGameLibs.DAMAGE_TYPE_MISS or args.behit_damage_result_:GetSkillDamageType() == ROGameLibs.kEffectDamSkillType_Magic then
			return false
		end
        return true
    end
    return false
end


--蝙蝠判断周围是否有青蛇
function BuffTriggerCondition.CheckQingShe(utilTool, firer, hoster, tableParam,args)
    local entity = args:GetLuaEnterEntity()
    if entity == nuil then
        return
    end

    local entityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
    if entityID == 5093 then
        utilTool:LuaAddBuff(firer,entity,3014101,true)
    end
end

--青蛇防御特效
function BuffTriggerCondition.CheckAddDef(utilTool, firer, hoster, tableParam,args) 
    local buffID = tableParam:GetParam("Buff")
    return hoster:HasBuff(buffID);
end

--小野猪本体被击取消分身状态
function BuffTriggerCondition.BoarCheckDamage(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    return curHP < maxHP
end

--毒性反弹True
function BuffTriggerCondition.PoisonATKTrue(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end

    local firer_entity = args:GetLuaFirerEntity()
    local attack_target_entity = args:GetLuaAttackTarget()
    if args.attack_type_ == ROGameLibs.kUnitAttrType_Bane and 
        args.behit_damage_result_:GetResult() > 0 and 
        firer_entity ~= attack_target_entity then
        -- utilTool:LuaLog("毒性反弹True"..tostring(args.attack_type_))
        return true
    else
        return false
    end
end

--毒性反弹False
function BuffTriggerCondition.PoisonATKFalse(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end

    local firer_entity = args:GetLuaFirerEntity()
    local attack_target_entity = args:GetLuaAttackTarget()
    if args.attack_type_ ~= ROGameLibs.kUnitAttrType_Bane and 
        args.behit_damage_result_:GetResult() > 0 and 
        firer_entity ~= attack_target_entity then
        -- utilTool:LuaLog("毒性反弹False"..tostring(args.attack_type_))
        return true
    else
        return false
    end
end

--  火柱攻击检测有人进入
function BuffTriggerCondition.HuoZhuEntityCheck(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
	if enter_entity ~= nil then
        if utilTool:LuaCheckFightGroup(hoster, enter_entity) == ROGameLibs.kFightGroupRelationEnemy then
            -- utilTool:LuaLog("entitytrue:"..tostring(enter_entity))
            return true
        end
    end
    return false
end

--隐匿检测
function BuffTriggerCondition.CheckHiding(utilTool, firer, hoster, tableParam, args)
    if hoster:GetAttr(AttrType.ATTR_SPECIAL_BREAK_HIDING) > 0 then
        return true
    else
        return false
    end
end

--狂暴状态血量检测
function BuffTriggerCondition.KuangBaoZhuangTai(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("HP_Trigger_Per")
    local buffID = tableParam:GetParam("Buff_ID")
    local perHP = presentHP / maxHP * 10000
    
    
    --检测血量数值
    if perHP < hpTriggerPer then
        return true
    else
        utilTool:LuaAddBuff(hoster,hoster,buffID,true)
        return false
    end
end

--触发闪电冲击被动
function BuffTriggerCondition.ShanDianChongJi(utilTool, firer, hoster, tableParam, args)
    if firer:GetBuff(4021) == nil then
        return false
    end
    if args.attack_damage_result_ == nil then
        return false
    end
    -- local skill_ID = args.current_skill_id_
    local damageType = args.attack_damage_result_:GetSkillDamageType()
    local hosterLUK = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
    -- print("hosterLUK:"..tostring(hosterLUK))
	
	local wledjyPro = hoster:GetFightTempAttr("EQUIP_WLEDJY_SDCJPRO")/10000
	local wledjyProLukRate = hoster:GetFightTempAttr("EQUIP_WLEDJY_SDCJLUKRATE")/10000
	
    local pro = (tableParam:GetParam("Pro") + 100*hosterLUK/3) / 10000
    local belluz6017 = hoster:GetBuff(60052)
	if belluz6017 ~= nil then
		local belluz6017Level = belluz6017:GetBuffLevel()
		local belluz6017ID = 60052 * 100 + belluz6017Level
		local belluz6017ConfigData = utilTool:LuaGetBuffTableConfigData(belluz6017ID,false)
		local belluz6017Pro = belluz6017ConfigData:GetParam("rate")/10000
		pro = pro + belluz6017Pro
	end
	pro = pro + wledjyPro + wledjyProLukRate * hosterLUK / 100
    -- utilTool:LuaLog("skill_ID:"..tostring(skill_ID))
    -- utilTool:LuaLog("Skill_Type:"..tostring(damageType))
    if damageType == ROGameLibs.kEffectDamSkillType_Common then
        return RORandom:randDouble(0,1) <= pro
    end
    return false
end

--触发箭术专精被动
function BuffTriggerCondition.JianShuZhuanJing(utilTool, firer, hoster, tableParam, args)
    local skillID = args.current_skill_id_
    local skill = tableParam:GetParam("Skill")
    local pro = tableParam:GetParam("Pro")/10000
    -- utilTool:LuaLog("skill_ID:"..tostring(skill_ID))
    -- utilTool:LuaLog("Skill_Type:"..tostring(damageType))
    if skill == skillID then
        if RORandom:randDouble(0,1) <= pro then
            return true
        -- utilTool:LuaLog("1")
        else
            return false
        end
    end
    return false
end

--退魂斩生效检测
function BuffTriggerCondition.TuiHunZhan(utilTool, firer, hoster, tableParam, args)
    local skill = args.current_skill_id_
    if skill == 1000525603 then
        return true
    end
    return false
end

--检测自己是否有坏血病
function BuffTriggerCondition.HuaiXueBingCheck(utilTool, firer, hoster, tableParam, args)
    if hoster:HasBuff(32301) == true then
        return true
    end
    return false
end

--天使之躯攻击
function BuffTriggerCondition.TianShiZhiQuAttack(utilTool, firer, hoster, tableParam, args)
    local enemy = args:GetLuaAttackTarget()
    if enemy == nil then
        -- print("aaa1")
        return false
    end
    local race = enemy:GetAttr(AttrType.ATTR_SPECIAL_RACE)
    -- print("race:"..tostring(race))
    local def_type = enemy:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100
    -- print("def_type:"..tostring(def_type))
        
        
    if race == 2 or def_type == 9 then
        -- print("aaa2")    
        return true
    else
        return false
    end
end

--天使之躯被攻击
function BuffTriggerCondition.TianShiZhiQuBeHit(utilTool, firer, hoster, tableParam, args)
    local enemy = args:GetLuaFirerEntity()
    if enemy == nil then
        -- print("bbb1")
        return false
    end
        
    local race = enemy:GetAttr(AttrType.ATTR_SPECIAL_RACE)
    -- print("race:"..tostring(race))
    local defType = enemy:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100
    -- print("def_type:"..tostring(def_type))
        
        
    if race == 2 or defType == 9 then
        -- print("bbb2")    
        return true
    else
        return false
    end
end

--坐下回血回蓝判断是否在战斗中，负重超过也会失效
function BuffTriggerCondition.SitDownInFight(utilTool, firer, hoster, tableParam, args)
    local inFight = hoster:IsFight()
    if inFight == false and hoster:GetAttr(AttrType.ATTR_SPECIAL_FORBID_NATURAL_RECOVERY) == 0 then
        return true
    else
        return false
    end
end

--商人判断武器值最大化时不自动回血，负重超过也会失效
function BuffTriggerCondition.WeaponMaxPowerIsOn(utilTool, firer, hoster, tableParam, args)
    if hoster:HasBuff(5003) == true or 
        hoster:GetAttr(AttrType.ATTR_SPECIAL_FORBID_NATURAL_RECOVERY) ~= 0 then 
        return false
    else
        return true
    end
end

--禅心和快速恢复判断超重
function BuffTriggerCondition.OverWeight(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("Buff")
    if hoster:HasBuff(buffID) == false then
        return true
    else
        return false
    end
end

--判定目标是否处于特殊状态
function BuffTriggerCondition.BuffState(utilTool, firer, hoster, tableParam, args)
    --print("inhere")
    local buffID = args:GetExtraParamByName("Buff")
    local attack_target_entity = args:GetLuaAttackTarget()
	if attack_target_entity == nil then
        -- print("aaa1")
        return false
    end
    if buffID == 9999 then--对于需要判定全部异常状态时传入值为9999
        if attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Sleep) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Fear) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Immobilize) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Frozen) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Petrochemical) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Paralysis) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Stun) or 
            attack_target_entity:CheckBuffState(ROGameLibs.kBuffStateType_Poisoning) then
                return true
        end
    end

    if attack_target_entity:CheckBuffState(buffID) == true then
        return true
    else
        return false
    end
end

--判定当前箭矢是否为指定箭矢
function BuffTriggerCondition.ArrowType(utilTool, firer, hoster, tableParam, args)
    local idEquip = args:GetExtraParamByName("arrow")
	local idCur = hoster:GetArrowID()
	
	if idCur == nil then
		return false
	else
		if idEquip == idCur then 
			return true
		else
			return false
		end		
	end	
end
		
--商人判断武器值最大化时不自动回血，负重超过也会失效
function BuffTriggerCondition.AutoReJudge(utilTool, firer, hoster, tableParam, args)
    if hoster:GetAttr(AttrType.ATTR_SPECIAL_FORBID_NATURAL_RECOVERY) ~= 0 then 
        return false
    else
        return true
    end
    
 
end

--负重检测
function BuffTriggerCondition.OverWeightJudge(utilTool, firer, hoster, tableParam, args)
    --获取当前负重
    -- local Present_HP = 100
    local Present_Weight = hoster:GetAttr(AttrType.ATTR_BASIC_WIGHT)
    local Max_Weight = hoster:GetAttr(AttrType.ATTR_SPECIAL_WIGHT_MAX)
    local Weight_Trigger_Per = tableParam:GetParam("Weight")
    local Buff_ID = tableParam:GetParam("BuffID")
    local Weight_Per = Present_Weight / Max_Weight * 10000
    
    
    --检测负重数值
    if Weight_Per >= Weight_Trigger_Per then
        return true
    else
        utilTool:LuaAddBuff(hoster,hoster,Buff_ID,true)
        return false
    end
end

--词缀：狂暴检测血量低于50%
function BuffTriggerCondition.KuangBaoCheckHP(utilTool, firer, hoster, tableParam, args)
    local HP_Per = tableParam:GetParam("HP_Per") / 10000
    local HP_Cur = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local HP_Max = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local Current_HP = HP_Cur / HP_Max

    if Current_HP <= HP_Per then
        return true
    else 
        return false
    end
end

--词缀：判断战斗状态
function BuffTriggerCondition.AffixCheckFight(utilTool, firer, hoster, tableParam, args)
    
    local InFight = hoster:IsFight()
    if InFight == false then
        return false
    else
        return true
    end
end

--通用概率触发
function BuffTriggerCondition.TriggerChance(utilTool, firer, hoster, tableParam, args)
    local Chance = tableParam:GetParam("Chance")/10000
    local Random = RORandom:randDouble(0,1)
    if Random <= Chance then
        return true
    else
        return false
    end
end

--狸猫帮buff加技能判断
function BuffTriggerCondition.AddSkillBuffToPlayer(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
    if enter_entity == nil then
        return false
    end
    if enter_entity:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_Other then
        local buffID = tableParam:GetParam("BuffID")
        if enter_entity:HasBuff(buffID) then
            return false
        else
            return true
        end
    end
    return false
end



--新手逗猫自杀事件BUFF判断
function BuffTriggerCondition.AddSkillSelfEventBuff( utilTool, firer, hoster, tableParam, args )
    -- body
    local enter_entity = args:GetLuaEnterEntity()
	if enter_entity == nil then
        return false
    end
	
	if enter_entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID) == 30520 then
		return true
	end
	return false
end

--灵巧修炼毕莱特献祭圈
function BuffTriggerCondition.XianJiCheck(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
    if enter_entity == nil then
        return false
    end
    if enter_entity:HasBuff(32706) then
        return true
    end
    return false
end

--词缀：检测是否进战
function BuffTriggerCondition.AffixCheckFight(utilTool, firer, hoster, tableParam, args)
    local inFight = hoster:IsFight()
    if inFight == false then
        return false
    else
        return true
    end
end

--词缀：检测是自己
function BuffTriggerCondition.AffixGongZhen(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
    if enter_entity == nil then
        return false
    end
    if enter_entity == hoster then
        return false
    else
		return true
	end
    
end

--词缀：检测与firer的距离
function BuffTriggerCondition.AffixCheckDistance(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
        return true
    end
    local firerDest = firer:GetPosition()
    local hosterDest = hoster:GetPosition()
    local distance = ROGameLibs.Vector3:Distance(firerDest, hosterDest)
    local range = tableParam:GetParam("Range")
    local firerHp = firer:GetAttr(AttrType.ATTR_BASIC_HP)
    if distance >= range or firerHp < 1 then
        return true
    end
    return false
end

--检测身处新手区域
function BuffTriggerCondition.AtNovicePlace(utilTool, firer, hoster, tableParam, args)
    local hosterSceneID = hoster:GetCurSceneID()
    if hosterSceneID ~= nil then
        if hosterSceneID == 31 or hosterSceneID == 32 or hosterSceneID == 33 then
            return true
        else
            return false
        end
    end
    return false
end

--勋章：剑士勋章3判断是否CD
function BuffTriggerCondition.Sword3InCD( utilTool, firer, hoster, tableParam, args )
    local buffID = tableParam:GetParam("BuffID")
	local pro = tableParam:GetParam("Pro") / 10000
	local randomNum = RORandom:randDouble(0,1)
	
	
	if hoster:HasBuff(buffID) == false and 
		randomNum <= pro and
		args.behit_damage_result_~=nil and
        args.behit_damage_result_:GetResult() > 0 then
		return true
	else
		return false
    end
end

--勋章：法师勋章2寒冰外套
function BuffTriggerCondition.HanBingWaiTaoCheck(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("HP_Trigger_Per")
    local buffID = tableParam:GetParam("BuffCheck")
    local perHP = presentHP / maxHP * 10000
    local changeAttr = args:GetAttrIncrValue(AttrType.ATTR_BASIC_HP)
    --检测血量数值
    if perHP < hpTriggerPer and hoster:HasBuff(buffID) == false then
        return true
    else
        return false
    end
end

--勋章：弓箭手勋章1判断叠层
function BuffTriggerCondition.Archer1Add( utilTool, firer, hoster, tableParam, args )
    local buff = hoster:GetBuff(8100211)
	local num = tableParam:GetParam("num")
    if buff ~= nil then
        local buffCount = buff:GetAppendCount()
        if buffCount == num then
            utilTool:LuaAddBuff(hoster, hoster, 810021201, true)
            return true
		else
			return false
        end
    end
	return false
end

--勋章：弓箭手勋章1叠层概率
function BuffTriggerCondition.Archer1Pro( utilTool, firer, hoster, tableParam, args )
    local randomNum = RORandom:randDouble(0,1)
	local pro = tableParam:GetParam("pro") / 10000
	local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if randomNum <= pro and hoster:HasBuff(4021) == true and damageType == ROGameLibs.kEffectDamSkillType_Common and hoster:GetSkillInfo(710125) ~= nil then
		return true
	else
		return false
    end
end

--加点镜像副本生命检测
function BuffTriggerCondition.JiaDianEventTrigger(utilTool, firer, hoster, tableParam, args)
    local perHP = tableParam:GetParam("perHP") / 10000
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local currentHP = curHP / maxHP

    if currentHP <= perHP then
        return true
    else 
        return false
    end
end

--黑暗迷宫：血量判断
function BuffTriggerCondition.HeiAnMiGongCheckHP(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local HPPer = tableParam:GetParam("HPPer")
    local perHP = presentHP / maxHP * 10000
    --检测血量数值
    if perHP <= HPPer then
        return true
    else
        return false
    end
end

--装死副本：检测释放的技能ID
function BuffTriggerCondition.CastQuickHeal(utilTool, firer, hoster, tableParam, args)
    local skillID = tableParam:GetParam("skillID")
	local curSkillID = args.current_skill_id_
    if skillID == curSkillID then
        return true
    else
        return false
    end
end

--疾风乱舞被动检测技能
function BuffTriggerCondition.JiFengLuanWu(utilTool, firer, hoster, tableParam, args)
	if args.attack_damage_result_ == nil then
        return false
    end
	local skillID1 = tableParam:GetParam("skillID1")
	-- local power1 = tableParam:GetParam("power1")
	-- local power2 = tableParam:GetParam("power2")
	
	local curSkillID = args.current_skill_id_
    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    
	if damageType == ROGameLibs.kEffectDamSkillType_Common then
		-- args:AddLuaOutParam("power",power1)
		return true
	elseif curSkillID == skillID1 then
		-- args:AddLuaOutParam("power",power2)
		return true
    end
    return false
end

--先锋追击被动检测技能
function BuffTriggerCondition.XianFengZhuiJi(utilTool, firer, hoster, tableParam, args)
	if args.attack_damage_result_ == nil then
        return false
    end
	local skillID1 = tableParam:GetParam("skillID1")
	local pro = tableParam:GetParam("pro") / 10000
	local randomNum = RORandom:randDouble(0,1)
	
	local curSkillID = args.current_skill_id_
    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if randomNum <= pro then
		if damageType == ROGameLibs.kEffectDamSkillType_Common or curSkillID == skillID1 then
			-- args:AddLuaOutParam("power",1)
			return true
		end
	end

    return false
end

--词缀：复仇检测是否进战
function BuffTriggerCondition.FuChouOutWar(utilTool, firer, hoster, tableParam, args)
    local inFight = hoster:IsFight()
    if inFight == false then
        return true
    else
        return false
    end
end

--词缀：炸弹波利召唤概率
function BuffTriggerCondition.ZhaDanBoLiZhaoHuan(utilTool, firer, hoster, tableParam, args)
    local inFight = hoster:IsFight()
	local pro = tableParam:GetParam("pro") / 10000
	local randomNum = RORandom:randDouble(0,1)
    if inFight == false or randomNum >= pro then
        return false
    else
        return true
    end
end

--反击伤害判断
function BuffTriggerCondition.FanJiCheck(utilTool, firer, hoster, tableParam,args) 
	local damageResult = args.behit_damage_result_
	if damageResult~=nil then
	
		
		local damageType = damageResult:GetSkillDamageType()
		local result = damageResult:GetResult()
		
		
	
		if result > 0 and damageType == ROGameLibs.kEffectDamSkillType_Common then
			return true
		end
		
	end
    return false
end

--  检测自己是否是罗伊
function BuffTriggerCondition.LuoYiCheck(utilTool, firer, hoster, tableParam, args)
	local EnterEntity = args:GetLuaEnterEntity()
	if EnterEntity ~= nil then
        if utilTool:LuaCheckFightGroup(hoster, EnterEntity) == ROGameLibs.kFightGroupRelationEnemy and EnterEntity:HasBuff(33409) == true then
            -- utilTool:LuaLog("entitytrue:"..tostring())
            return true
        end
    end
    return false
end

--狮子奋迅判断伤害和概率
function BuffTriggerCondition.ShiZiFenXun(utilTool, firer, hoster, tableParam,args) 
	local pro = tableParam:GetParam("pro") / 10000
	local randomNum = RORandom:randDouble(0,1)
	if args.behit_damage_result_~=nil and
        args.behit_damage_result_:GetResult() > 0 and
		randomNum <= pro then
       return true
    end
    return false
end

--长矛刺击判断
function BuffTriggerCondition.ChangMaoCiJiCheck(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return false
    end
    local firer_entity = args:GetLuaFirerEntity()
    if  firer_entity:GetUUID() == firer:GetUUID() then
        return true
    else
        return false
    end
end

--  黑暗迷宫：黑暗状态判断
function BuffTriggerCondition.DarkMazeDarkCheck(utilTool, firer, hoster, tableParam, args)
	local darkID = tableParam:GetParam("darkID")
	local lightID = tableParam:GetParam("lightID")
	if hoster:HasBuff(darkID) == true or hoster:HasBuff(lightID) == true then
        return false
    end
    return true
end

--  黑暗迷宫：火焰状态判断
function BuffTriggerCondition.DarkMazeFireCheck(utilTool, firer, hoster, tableParam, args)
	local fireID = tableParam:GetParam("fireID")
	if hoster:HasBuff(fireID) == true then
        return true
    end
    return false
end

--  黑暗迷宫：buff锁血
function BuffTriggerCondition.DarkMazeBuffLock(utilTool, firer, hoster, tableParam, args)
	local lightID = tableParam:GetParam("lightID")
	local lightID2 = tableParam:GetParam("lightID2")
	local lightID3 = tableParam:GetParam("lightID3")
	local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = 100
    local buffID = 3313401
    local buffID2 = 3312701
    local perHP = (presentHP / maxHP) * 10000
	if hoster:HasBuff(lightID) == true or hoster:HasBuff(lightID2) == true or hoster:HasBuff(lightID3) == true then
        return false
	elseif perHP <= hpTriggerPer then
		utilTool:LuaAddBuff(hoster,hoster,buffID,true)
		utilTool:LuaAddBuff(hoster,hoster,buffID2,true)
        return false
    end
    return true
end

--  黑暗迷宫：发送事件判断
function BuffTriggerCondition.DarkMazeSendCheck(utilTool, firer, hoster, tableParam, args)
	local lightID = tableParam:GetParam("lightID")
	local sendID = tableParam:GetParam("sendID")
	local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = 100
    local buffID = 3313401
    local buffID2 = 3312701
    local perHP = (presentHP / maxHP) * 10000
	if hoster:HasBuff(lightID) == true or hoster:HasBuff(sendID) == true then
        return false
	elseif perHP <= hpTriggerPer then
		utilTool:LuaAddBuff(hoster,hoster,buffID,true)
		utilTool:LuaAddBuff(hoster,hoster,buffID2,true)
        return false
    end
    return true
end

--狂怒之枪删除逻辑
function BuffTriggerCondition.KuangNuZhiQiangCheck(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("HP_Trigger_Per")
    local buffID = tableParam:GetParam("BuffCheck")
    local perHP = presentHP / maxHP * 10000
    --检测血量数值
    if perHP < hpTriggerPer and hoster:HasBuff(buffID) == true then
        return true
    else
        return false
    end
end

--死神：濒死判断
function BuffTriggerCondition.DeathCheckHP(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() >= curHP then
            return true
        end
    end
    return false
end


--炎热：移动状态判断
function BuffTriggerCondition.AffixCheckMove(utilTool, firer, hoster, tableParam, args)
    if hoster:IsMoving() == true  then
        return true
    end
    return false
end

--非移动状态判断
function BuffTriggerCondition.CheckNotMove(utilTool, firer, hoster, tableParam, args)
    if hoster:IsMoving() == false then
        return true
    end
    return false
end

--上野蛮凶砍Buff概率
function BuffTriggerCondition.YeManXiongKanCondition(utilTool, firer, hoster, tableParam, args)
    local destroyPro = tableParam:GetParam("pro") 
    local randomNum = RORandom:randDouble(0,10000)
    if  randomNum < destroyPro then
        return true 
    end
    return false
end


--双剑格挡触发条件判断
function BuffTriggerCondition.ShuangJianGeDangCondition( utilTool, firer, hoster, tableParam,args )
    local block_pro = tableParam:GetParam("block")/10000
    if RORandom:randDouble(0,1) <= block_pro then
        --BuffTriggerCondition.BeHitDamageCheck(utilTool, firer, hoster, tableParam,args)
        if args.behit_damage_result_~=nil then
            local damage = args.behit_damage_result_:GetResult()
			local damageType = args.behit_damage_result_:GetSkillDamageType()
			if damage > 0 and (damageType == ROGameLibs.kEffectDamSkillType_Common or damageType == ROGameLibs.kEffectDamSkillType_Physical) then
				return true
			end
        end
        return false
    end  
end

--  爆炸木乃伊检测是否为献祭者
function BuffTriggerCondition.BaoZhaMuNaiYi(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")
    local enter_entity = args:GetLuaEnterEntity()
	if enter_entity ~= nil then
        if utilTool:LuaCheckFightGroup(hoster, enter_entity) == ROGameLibs.kFightGroupRelationEnemy then
            if enter_entity:HasBuff(buffID) == true then
				return true
			end
        end
    end
    return false
end

--钢铁意志触发条件
function BuffTriggerCondition.GangTieYiZhiCheck(utilTool, firer, hoster, tableParam, args)
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("HP_Trigger_Per")
    local perHP = presentHP / maxHP * 10000
    local buff = 143
    if args.behit_damage_result_ == nil then
        return false
    end
    if args.behit_damage_result_:GetResult() <= 0 or hoster:HasBuff(buff) then
        return false
    end
    local pro = tableParam:GetParam("Pro")/10000
    local Random = RORandom:randDouble(0,1)
    if Random <= pro and perHP < hpTriggerPer then
        return true
    else
        return false
    end
end

--A7：癫狂buff检测
function BuffTriggerCondition.A7DianKuangCheck(utilTool, firer, hoster, tableParam, args)
    local buff = tableParam:GetParam("BuffID")
    local entity = args:GetLuaEnterEntity()
    if entity ~= nil then
        if entity:HasBuff(buff) == true then
            return true
        end
    end
    return false
end

--A8：梦罗克幼体血量检测
function BuffTriggerCondition.MengLuoKeYouTiHPTrigger(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
	
	local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("hpTrigger")
    local perHP = presentHP / maxHP * 10000
    
    --检测血量数值
    if perHP < hpTriggerPer then
        return true
    else
        return false
    end
end

--天地树——检测陨石召唤是否成功
function BuffTriggerCondition.TianDiShuYunShiZhaoHuan( utilTool, firer, hoster, tableParam,args )
    local pro = tableParam:GetParam("pro")/10000
    if RORandom:randDouble(0,1) <= pro then
        if args.attack_damage_result_~=nil and args.attack_damage_result_:GetResult() > 0 then
			return true
        end
        return false
    end  
end

--天地树——检测神圣之怒是否成功
function BuffTriggerCondition.TianDiShuShenShengZhiNu( utilTool, firer, hoster, tableParam,args )
    -- local pro = tableParam:GetParam("pro")/10000
    if args.attack_damage_result_~=nil and args.attack_damage_result_:GetResult() < 0 then
		return true
    end
    return false
end

--天地树——回魂圣水检测血量
function BuffTriggerCondition.TianDiShuHuiHunShengShui(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
	
	local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpTriggerPer = tableParam:GetParam("HP_Trigger_Per")
    local buffID = tableParam:GetParam("Buff_ID")
    local perHP = presentHP / maxHP * 10000

    
    --检测血量数值
    if perHP < hpTriggerPer then
        return true
    else
        utilTool:LuaAddBuff(hoster,hoster,buffID,true)
        return false
    end
end

--天地树——神树之根移动判断
function BuffTriggerCondition.TianDiShuShenShuZheGen(utilTool, firer, hoster, tableParam, args)
    if hoster:IsMoving() == true  then
        utilTool:LuaAddBuff(hoster,hoster,5154001,true)
		return false
    end
	if hoster:HasBuff(51540) then
		utilTool:LuaDelBuff(hoster, 51540, true)
	end
    return true
end

--天地树——防护之力人数判断
function BuffTriggerCondition.TianDiShuFangHuZhiLi(utilTool, firer, hoster, tableParam, args)
    local enemyList = std.vector_ROGameLibs__ROLuaEntity__:new_local()
    local playerReturn = utilTool:LuaGetEnemy(hoster, 0, 3, enemyList)
    -- 这里不要删除 gc 会进行删除 然后导致 崩溃
    if playerReturn == false then
        -- enemyList:delete()
        return false
    end

    local enemyNum = enemyList:size()
    local pro = tableParam:GetParam("pro")/10000
    if enemyNum >= 5 and RORandom:randDouble(0,1) <= pro then
        if args.behit_damage_result_ ~= nil and args.behit_damage_result_:GetResult() > 0 then
            -- enemyList:delete()
            return true
        end
    end
    -- enemyList:delete()
    return false
                
end

--箭矢修炼判定
function BuffTriggerCondition.JianShiXiuLian(utilTool, firer, hoster, tableParam, args)
    --skill新增接口：是否释放了箭矢，特别地，默认无穷箭矢不算释放
    local useArrow = args.current_skill_:IsFireArrow()
    if useArrow == true then
        --skill新增接口：获取箭矢道具id，如果获取不到或者无穷箭矢，则返回0
       local arrowId = args.current_skill_:GetArrowId()
       --打印箭矢道具id
       -- print(arrowId)
       local rowdata = TableUtil.GetItemTable().GetRowByItemID(arrowId)
        if rowdata ~= nil then
            -- print("找到ItemTable的行")
        else
            -- print("没有找到ItemTable的行")
            return false
        end
        local rowdata2 = TableUtil.GetItemFunctionTable().GetRowByItemId(arrowId)
        if rowdata2 ~= nil then
            -- print("找到ItemFunctionTable的行")
            -- 打印相关信息
            -- print(rowdata2.ItemFunction)
        end
        return true
    end
    return false
    --return args.current_skill_id_ == skillCur
end

--圣之裁决被动判定
function BuffTriggerCondition.ShengZhiCaiJueCondition(utilTool, firer, hoster, tableParam, args)
    if args.attack_damage_result_ == nil then
        return false
    end

    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
	--贝鲁兹核心Buff加概率
	local Belluz4024buff = hoster:GetBuff(65027)
    --utilTool:LuaLog("damageType:"..tostring(damageType))
	
	local skill_pro = tableParam:GetParam("pro")/10000
	
	if Belluz4024buff ~= nil then
		local Belluz4024Lv = Belluz4024buff:GetBuffLevel()
		local belluz4024ID = 65027 * 100 + Belluz4024Lv
		local belluz4024ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4024ID,false)
		local belluz4024Pro = belluz4024ConfigData:GetParam("pro")/10000
		skill_pro = skill_pro + belluz4024Pro
    end
	
    if damageType == ROGameLibs.kEffectDamSkillType_Common then
        local rand = RORandom:randDouble(0,1)
        if rand <= skill_pro then
        return true
        end
    end

    return false
end

--蜗牛王检测触角并杀死触角
function BuffTriggerCondition.WoNiuWangCheck(utilTool, firer, hoster, tableParam, args)
    -- print("1111111")
    if hoster:HasBuff(9457) == true then
        return true
    end
    return false
end

--法师吃球
function BuffTriggerCondition.ChiQiuCheck( utilTool, firer, hoster, tableParam, args )
    local buff = hoster:GetBuff(33042)
	if buff ~= nil then
        local buffCount = buff:GetAppendCount()
        if buffCount >= 5 then
            return true
        end
    end
	return false
end

--贝鲁兹1002
function BuffTriggerCondition.Belluz1002(utilTool, firer, hoster, tableParam,args) 
	local hpRate = tableParam:GetParam("hpRate")/10000
	local hpCur = hoster:GetAttr(AttrType.ATTR_BASIC_HP) 
	local hpMax = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL) 
	if (hpCur/hpMax) >= hpRate then
		return true
	end
    return false
end

--贝鲁兹1003
function BuffTriggerCondition.Belluz1003(utilTool, firer, hoster, tableParam, args)
    --获取当前血量
    -- local Present_HP = 100
    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hpRate = tableParam:GetParam("hpRate")
    local perHP = presentHP / maxHP * 10000
    
    
    --检测血量数值
    if perHP <= hpRate then
        return true
    end
	return false
end

--贝鲁兹1007
function BuffTriggerCondition.Belluz1007(utilTool, firer, hoster, tableParam,args) 
    if args.behit_damage_result_ == nil then
        return false
    end
	local pre = tableParam:GetParam("pre")/10000
	local rate = RORandom:randDouble(0,1)
    if args.behit_damage_result_:GetType() == ROGameLibs.DAMAGE_TYPE_MISS or args.behit_damage_result_:GetType() == ROGameLibs.DAMAGE_TYPE_BLOCK then
		if rate <= pre then
			return true
		end
    end

    return false
end

--贝鲁兹1008
function BuffTriggerCondition.Belluz1008(utilTool, firer, hoster, tableParam,args) 
    
	local damageResult = args.attack_damage_result_
	if damageResult == nil then
        return false
    end
	
	local result = damageResult:GetResult()
	local count = tableParam:GetParam("count")
	local rate = RORandom:randDouble(0,1)
	local damageType = damageResult:GetSkillDamageType()
	local buff = hoster:GetBuff(60009)
    
    --if damageResult:GetType() ~= ROGameLibs.DAMAGE_TYPE_CRI and result > 0 and damageType == ROGameLibs.kEffectDamSkillType_Common then
	if  result > 0 and damageType == ROGameLibs.kEffectDamSkillType_Common then
		utilTool:LuaAddBuff(hoster,hoster,6000901,true)
		if buff ~= nil then
			local buffCount = buff:GetAppendCount()
			if buffCount >= count then
				utilTool:LuaDelBuff(hoster, 60009, true)
				return true
			end
		end
    end

    return false
end

--贝鲁兹1009
function BuffTriggerCondition.Belluz1009(utilTool, firer, hoster, tableParam,args) 
	local hpRate = tableParam:GetParam("hpRate") / 10000
	local enemy = args:GetLuaAttackTarget()
	if enemy ~= nil then
		local HPCur = enemy:GetAttr(AttrType.ATTR_BASIC_HP)
		local HPMax = enemy:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
		if HPCur/HPMax > hpRate then
			return true
		end
    end
    return false
end

--贝鲁兹1010
function BuffTriggerCondition.Belluz1010(utilTool, firer, hoster, tableParam,args) 
	local hpRate = tableParam:GetParam("hpRate") / 10000
	local enemy = args:GetLuaAttackTarget()
	if enemy ~= nil then
		local HPCur = enemy:GetAttr(AttrType.ATTR_BASIC_HP)
		local HPMax = enemy:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
		if HPCur/HPMax < hpRate then
			return true
		end
    end
    return false
end

--贝鲁兹1011
function BuffTriggerCondition.Belluz1011(utilTool, firer, hoster, tableParam, args)
    local skill = args.current_skill_id_
	if skill == nil or args.attack_damage_result_ == nil then
		return false
	end
	local skillTableInfo = Table.SkillTable.GetRowById(skill,true)
	local damage = args.attack_damage_result_:GetResult()
	if skillTableInfo ~= nil and damage > 0 then
		local damageType = args.attack_damage_result_:GetSkillDamageType()
		local skillIsSingle = skillTableInfo.IsAoe
		if skillIsSingle == false and damageType ~= ROGameLibs.kEffectDamSkillType_Common then
			return true
		end
    end
    return false
end

--贝鲁兹7009
function BuffTriggerCondition.Belluz7009(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID and args.attack_damage_result_~=nil then
		local damage = args.attack_damage_result_:GetResult()
		if damage > 0 then
			return true
		end
	end
    return false
end

--贝鲁兹2001
function BuffTriggerCondition.Belluz2001(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	
	-- utilTool:LuaLog("skill"..tonumber(skill))
	-- utilTool:LuaLog("skillID"..tonumber(skillID))
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹2003
function BuffTriggerCondition.Belluz2003(utilTool, firer, hoster, tableParam, args)
	-- local skill = math.floor(args.current_skill_effect_id_/100)
	local skill = math.floor(args.current_skill_effect_id_/100)
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹2013
function BuffTriggerCondition.Belluz2013(utilTool, firer, hoster, tableParam, args)
	-- local skill = math.floor(args.current_skill_effect_id_/100)
	local skill = math.floor(args.current_skill_effect_id_/100)
	local laskSkill = math.floor(hoster:GetLastSkillEffectId()/100)
	if skill == 0 or laskSkill == 0 then
		return false
	end
	
	if skill == laskSkill then
		return true
	end
    return false
end

--贝鲁兹2013删buff
function BuffTriggerCondition.Belluz2013Del(utilTool, firer, hoster, tableParam, args)
	-- local skill = math.floor(args.current_skill_effect_id_/100)
	local skill = math.floor(args.current_skill_effect_id_/100)
	local laskSkill = math.floor(hoster:GetLastSkillEffectId()/100)
	if skill == 0 or laskSkill == 0 then
		return false
	end
	
	if skill ~= laskSkill then
		return true
	end
    return false
end

--贝鲁兹2007
function BuffTriggerCondition.Belluz2007(utilTool, firer, hoster, tableParam, args)
	-- local skill = math.floor(args.current_skill_effect_id_/100)
	local skillInfo = args.trigger_timing_skill_snapshot_
	local skillID = tableParam:GetParam("skillID")
	if skillInfo == nil then
		return false
	end
	
	local skill = skillInfo:GetSkillId()
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹3001 释放技能概率触发
function BuffTriggerCondition.Belluz3001(utilTool, firer, hoster, tableParam, args)
    local skillID = args.current_skill_id_
    local skill = tableParam:GetParam("Skill")
    local pro = tableParam:GetParam("Pro")/10000
    -- utilTool:LuaLog("skill_ID:"..tostring(skill_ID))
    -- utilTool:LuaLog("Skill_Type:"..tostring(damageType))
    if skill == skillID then
        if RORandom:randDouble(0,1) <= pro then
            return true
        -- utilTool:LuaLog("1")
        else
            return false
        end
    end
    return false
end

--贝鲁兹3005 陨石燃烧区域光环添加判断
function BuffTriggerCondition.Belluz3005(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("BuffID")
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner:HasBuff(buffID) then
        return true
    else
        return false
    end
end  

--贝鲁兹3010 暴风雪破冰增伤
function BuffTriggerCondition.Belluz3010up(utilTool, firer,hoster,tableParam,args)
    if firer == nil then
        return false
    end
    local buffID = tableParam:GetParam("BuffID")
    local firer_entity = args:GetLuaFirerEntity()
    if firer_entity:HasBuff(buffID) then
        return true
    else
        return false
    end
end  

--贝鲁兹3011 减技能共CD
function BuffTriggerCondition.Belluz3011(utilTool, firer, hoster, tableParam, args)
    local skillID = args.current_skill_id_
    local skill = tableParam:GetParam("Skill")
    -- utilTool:LuaLog("skill_ID:"..tostring(skill_ID))
    -- utilTool:LuaLog("Skill_Type:"..tostring(damageType))
    if skill == skillID then
            return true
        else
            return false
        end
end

----贝鲁兹3022判断buff
function BuffTriggerCondition.Belluz3022(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")    
    if hoster:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--贝鲁兹4014
function BuffTriggerCondition.Belluz401401(utilTool, firer, hoster, tableParam, args)
	local buff = hoster:GetBuff(65015)
	
	if buff == nil then
		return false
	end
   
	local buffLv = buff:GetBuffLevel()
	if buffLv == 1 then
		return true
	end
end

--贝鲁兹4014
function BuffTriggerCondition.Belluz401402(utilTool, firer, hoster, tableParam, args)
	local buff = hoster:GetBuff(65015)
	
	if buff == nil then
		return false
	end
   
	local buffLv = buff:GetBuffLevel()
	if buffLv == 2 then
		return true
	end
end

--贝鲁兹4014
function BuffTriggerCondition.Belluz401403(utilTool, firer, hoster, tableParam, args)
	local buff = hoster:GetBuff(65015)
	
	if buff == nil then
		return false
	end
   
	local buffLv = buff:GetBuffLevel()
	if buffLv == 3 then
		return true
	end
end

--贝鲁兹4017
function BuffTriggerCondition.Belluz4017(utilTool, firer, hoster, tableParam, args)			   
	local skill = tableParam:GetParam("Skill")
	local currentSkill = args.current_skill_id_
		
	if skill == currentSkill then
		return true
	end
	return false
end

--贝鲁兹4018
function BuffTriggerCondition.Belluz4018(utilTool, firer, hoster, tableParam, args)
	local owner = utilTool:LuaGetMobMonsterOwner(firer, true)
	local Buff = owner:GetBuff(65021)
	
	if Buff ~= nil then
		return true
	end
	return false
end
	  
--贝鲁兹4020
function BuffTriggerCondition.Belluz4020(utilTool, firer, hoster, tableParam, args)
    local damageResult = args.attack_damage_result_
	if damageResult == nil then
		return false
	end
	
	local result = damageResult:GetResult()
	
	--utilTool:LuaLog("result"..tostring(result))
	if result <= 0 then
		return true
	end
end
	
--贝鲁兹5004
function BuffTriggerCondition.Belluz5004(utilTool, firer, hoster, tableParam,args) 
	local damageResult = args.attack_damage_result_
	if damageResult == nil then
		return false
	end
	
	local result = damageResult:GetResult()
	local damageType = damageResult:GetSkillDamageType()
    
    if damageResult:GetType() == ROGameLibs.DAMAGE_TYPE_CRI and result > 0 and damageType == ROGameLibs.kEffectDamSkillType_Common then
		return true
	else
		return false
	end
end

--贝鲁兹5005
function BuffTriggerCondition.Belluz5005(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return false
	end
	local buff = firer:GetBuff(65051)
	if buff == nil then
		return false
	end
	return true
end

--贝鲁兹5005
function BuffTriggerCondition.Belluz500502(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return
	end
	 
	local buff = firer:GetBuff(65051)
	if buff == nil then
		return false
	end
   
	local buffLv = buff:GetBuffLevel()
	if buffLv == 2 then
		return true
	end
end

--贝鲁兹5005
function BuffTriggerCondition.Belluz500503(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return
	end
	 
	local buff = firer:GetBuff(65051)
	if buff == nil then
		return false
	end
   
	local buffLv = buff:GetBuffLevel()
	if buffLv == 3 then
		return true
	end
end

--贝鲁兹5012
function BuffTriggerCondition.Belluz5012(utilTool, firer, hoster, tableParam, args)
	local buff = hoster:GetBuff(65043)
	local pro = RORandom:randDouble(0,1)
	
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")

	if skill == 0 then
		return false
	end
	
	if buff ~= nil and skill == skillID then
		local buffLv = buff:GetBuffLevel()
		local belluz5012ID = 65043 * 100 + buffLv
		local belluz5012ConfigData = utilTool:LuaGetBuffTableConfigData(belluz5012ID,false)
		local belluz5012Pro = belluz5012ConfigData:GetParam("pro")/10000
		if pro <= belluz5012Pro then
			return true
		else
			return false
		end
	end   
end

--贝鲁兹6005
function BuffTriggerCondition.Belluz6005(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
	return false
end

--贝鲁兹6008
function BuffTriggerCondition.Belluz6008(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹7002
function BuffTriggerCondition.Belluz7002(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹7003
function BuffTriggerCondition.Belluz7003(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹7004
function BuffTriggerCondition.Belluz7004(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹7010
function BuffTriggerCondition.Belluz7010(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	local skillID = tableParam:GetParam("skillID")
	if skill == 0 then
		return false
	end
	
	if skill == skillID then
		return true
	end
    return false
end

--贝鲁兹6012
function BuffTriggerCondition.Belluz6012(utilTool, firer, hoster, tableParam, args)
	local skill = args.current_skill_id_
	if skill == 0 then
		return false
	end
	
	if skill == 710101 or skill == 710102 or skill == 710125 or skill == 710126 or skill == 710130 or skill == 710131 or skill == 710132 or skill == 710133 or skill == 710134 or skill == 710135 or skill == 710211 or skill == 710212 or skill == 710213 or skill == 710214 or skill == 710215 or skill == 710216 then
		return true
	end
    return false
end

----贝鲁兹3019判断能量外套等级
function BuffTriggerCondition.Belluz3019(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")  
    if hoster:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--贝鲁兹3008
function BuffTriggerCondition.Belluz3008(utilTool, firer, hoster, tableParam, args)
    local damageResult = args.attack_damage_result_
    if damageResult == nil then
        return false
    end
    local attack_target_entity = args:GetLuaAttackTarget()
    if args.attack_type_ == ROGameLibs.kUnitAttrType_Water and
       args.attack_damage_result_:GetResult() > 0 and 
       firer_entity ~= attack_target_entity then
       return true
    else
       return false
    end
end

----贝鲁兹3010暴风雪几率上冰冻
function BuffTriggerCondition.Belluz3010(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
        return false
    end
    local pro = tableParam:GetParam("Pro")/10000
    local buffID = tableParam:GetParam("BuffID")
    if firer:HasBuff(buffID) then 
    pro = 0.54  
    end
    if RORandom:randDouble(0,1) <= pro then
    --print("pro:"..pro)
        return true
    else
    --print("pro:"..pro)
        return false
    end
 end   
 
 
 --装备效果，站立不动时触发
function BuffTriggerCondition.StandStill(utilTool, firer, hoster, tableParam, args)
    if hoster:IsMoving() == true  then
		return false
    end
    return true
end
 --装备效果，移动时触发
function BuffTriggerCondition.NotStandStill(utilTool, firer, hoster, tableParam, args)
    if hoster:IsMoving() == true  then
		return true
    end
    return false
end

--贝鲁兹3014
function BuffTriggerCondition.Belluz3014(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() >= curHP then
            return true
        else
            return false
        end
    end
end

--骑兵修炼在马上
function BuffTriggerCondition.QiBingXiuLian(utilTool, firer, hoster, tableParam, args)
    local onBattleVehicle = hoster:IsInBattleVehicle()
    local skillInfo = hoster:GetSkillInfo(210107)
    if skillInfo ~= nil and onBattleVehicle == true then
        return true
    else
        return false
    end
end

--俄塞里斯卡判定死亡
function BuffTriggerCondition.BeHitWillDie(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() >= curHP then
            return true
        end
    end
    return false
end

--装备：PTD核心装备判断
function BuffTriggerCondition.PTDEquipedCheck(utilTool, firer,hoster,tableParam,args)
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	if owner ~= nil then
		local tempAttr = owner:GetFightTempAttr("EQUIP_PTD_EQUIPED")
		-- local tempAttr = 1
		if tempAttr >= 1 then
			return true
		end
	end
	return false
end

--装备：PTD核心精炼10判断
function BuffTriggerCondition.PTDRefine10Check(utilTool, firer,hoster,tableParam,args)
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	if owner ~= nil then
		local tempAttr = owner:GetFightTempAttr("EQUIP_PTD_REFINE10")
		-- local tempAttr = 1
		if tempAttr >= 1 then
			return true
		end
	end
	return false
end

--装备：痊愈光辉精炼6判断
function BuffTriggerCondition.QYGHRefine6Check(utilTool, firer,hoster,tableParam,args)
	local tempAttr = hoster:GetFightTempAttr("EQUIP_QYGH_REFINE6")
	-- local tempAttr = 1
	if tempAttr >= 1 then
		return true
	end
	return false
end

--装备：痊愈光辉精炼15判断
function BuffTriggerCondition.QYGHRefine15Check(utilTool, firer,hoster,tableParam,args)
	local tempAttr = hoster:GetFightTempAttr("EQUIP_QYGH_REFINE15")
	-- local tempAttr = 1
	if tempAttr >= 1 then
		return true
	end
	return false
end

--装备：魔神安魂曲蓝量判断
function BuffTriggerCondition.EquipMSAHQ(utilTool, firer,hoster,tableParam,args)
	local spCheck = tableParam:GetParam("spCheck")
	local SP = firer:GetAttr(AttrType.ATTR_BASIC_SP)
	-- local tempAttr = 1
	if SP >= spCheck then
		return true
	end
	return false
end

--沙石风暴孵化蚁卵
function BuffTriggerCondition.YiLuanCheck(utilTool, firer, hoster, tableParam, args)
	local buff = hoster:GetBuff(32529)
	
	if buff ~= nil then
		return true
	else
		return false
	end
end

--撞击的目标是否为书架
function BuffTriggerCondition.BookshelfZhuangJiCheck(utilTool, firer, hoster, tableParam, args)
	local enemy = args:GetLuaAttackTarget()
    if enemy == nil then
        return false
    end
    
	local buff = enemy:GetBuff(37246)
	
	if buff ~= nil then
		return true
	else
		return false
	end
	
end

--被撞击的目标是否为深渊骑士
function BuffTriggerCondition.DarkKnightZhuangJiCheck(utilTool, firer, hoster, tableParam, args)
	local enemy = args:GetLuaFirerEntity()
    if enemy == nil then
        return false
    end
	
	local buff = enemy:GetBuff(37247)
	
	if buff ~= nil then
		return true
	else
		return false
	end
	
end

--撞击的目标是否为柱子/吊灯
function BuffTriggerCondition.PostZhuangJiCheck(utilTool, firer, hoster, tableParam, args)
	local enemy = args:GetLuaAttackTarget()
    if enemy == nil then
        return false
    end
    
	local buff = enemy:GetBuff(37249)
	
	if buff ~= nil then
		return true
	else
		return false
	end
	
end

--撞击的目标是否为玩家
function BuffTriggerCondition.PlayerCheck(utilTool, firer, hoster, tableParam, args)
	local enemy = args:GetLuaAttackTarget()
    if enemy == nil then
        return false
    end
    
	local buff = enemy:GetBuff(37249)
	
	if buff == nil then
		return true
	else
		return false
	end
	
end

--被撞击的目标是否为毕尤
function BuffTriggerCondition.BiyoZhuangJiCheck(utilTool, firer, hoster, tableParam, args)
	local enemy = args:GetLuaFirerEntity()
    if enemy == nil then
        return false
    end
	
	local buff = enemy:GetBuff(37214)
	
	if buff ~= nil then
		return true
	else
		return false
	end
	
end


--卡片检测血量是否低于
function BuffTriggerCondition.Hptest(utilTool, firer, hoster, tableParam, args)

    local presentHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hprate = tableParam:GetParam("hprate")
    local perHP = presentHP / maxHP * 10000
    local testtype = tableParam:GetParam("testtype")
    
    --testtype为1表示判断血量低于xxx时，为0表示判断血量高于xxx时
	
	
    if testtype == 1 and perHP <= hprate then
        return true
   	
	elseif testtype == 0 and perHP >= hprate then
		return true
	end
	
	return false
end
