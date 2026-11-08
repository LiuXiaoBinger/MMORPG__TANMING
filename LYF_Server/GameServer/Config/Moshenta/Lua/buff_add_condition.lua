
declareGlobal("BuffAddCondition", {})
local ShowTipType = ROGameLibs.RONetEventShowTipToRole

-----Buff额外添加条件的函数
--[[
	firer: 产生buff的对象(不是entity时为nil), 比如A给B加了buff, 那么A是firer, B是hoster
	hoster: buff的持有者
	tableParam: 表里填的参数, 例LuaSpecial|INT=0|RANG=5  函数名为LuaSpecial, tableParam:GetParam("INT") 获取到0, tableParam:GetParam("RANG") 获取到5

	return 返回true/false 表示条件成立/失败
	]]

--HPLimitDown|HP=50
function BuffAddCondition.HPLimitDown(utilTool, firer, hoster, tableParam)
	if (true) then
		return true
	end
	local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
	-- utilTool:LuaLog("hoster hp:"..tonumber(curHP))
	-- utilTool:LuaLog("limit hp:"..tableParam:GetParam("HP"))

	return curHP <= tableParam:GetParam("HP")
end

--天使之光BUFF要判定敌方类型
function BuffAddCondition.AngelLight1(utilTool, firer, hoster, tableParam)      
	
	local race = hoster:GetAttr(AttrType.ATTR_SPECIAL_RACE)
	local def_type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100

	if race == tableParam:GetParam("race") or def_type == tableParam:GetParam("element") then
		return true
	else
		return false
	end
end


--偷窃是否能成功判断
function BuffAddCondition.CheckSteal(utilTool, firer, hoster, tableParam)      
	local tiredRatio = 1
	--疲劳值计算
	if firer ~= nil then
		tiredRatio = firer:CalcualteDropAttenuation()
	end
	local pro = tableParam:GetParam("Pro") * tiredRatio
	--处于睡眠状态的目标不能被偷窃
	if RORandom:randDouble(0, 10000) <= pro and hoster:CheckBuffState(ROGameLibs.kBuffStateType_Sleep) ~= true then
		return true
	else
		utilTool:LuaShowTips(firer, ROGameLibs.kShowTipType_FightResultTips, "STEAL_FAILED")
		return false
	end
end

--加速术
function BuffAddCondition.JiaSuShu(utilTool, firer, hoster, tableParam,args) 
	if firer == nil then
		return false
	end
	local skill_id = tableParam:GetParam("BUFF")
	local skill_info = firer:GetSkillInfo(skill_id)--GetBuff(BUFFID)
	return skill_info ~= nil
end

--火焰免疫判定
function BuffAddCondition.FireWallAdd(utilTool, firer, hoster, tableParam)      
	if hoster:GetBuff(2013) ~= nil then
		return false
	else
		return true
	end
end

--狂击眩晕判定
function BuffAddCondition.KuangJiXuanYun(utilTool, firer, hoster, tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_DIZZY_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_DIZZY_ANTIBODY_RATE)/100
	--VIT参数计算
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local VIT_param = 100 - VIT_hoster
	if VIT_param < 0 then
		VIT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*VIT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	
	if firer:GetSkillInfo(200010) ~= nil then
		local chanceSkill = firer:GetSkillInfo(200010):GetParam("PRO")/100
		if RORandom:randDouble(0,100) <= chanceSkill and RORandom:randDouble(0,100) <= chance then
			return true
		else
			return false
		end
	else
		return false
	end
end

--巧打成功率判断
function BuffAddCondition.JointBeat(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	
	--STR参数计算
	local STR_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_STR)
	local STR_param = math.max(100 - STR_hoster,10)

	--上buff成功率判断
	local chance = 50*STR_param/100	
	local entityType = hoster:GetUnitTypeLevel()
	if RORandom:randDouble(0,100) <= chance then
		if entityType == ROGameLibs.kUnitTypeLevel_BOSS or entityType == ROGameLibs.kUnitTypeLevel_MVP then
			return false
		end
		return true
	else 
		return false
	end
end

--上中毒Buff成功率判断
function BuffAddCondition.GetPoison(utilTool, firer, hoster, tableParam)      
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_POISON_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_POISON_ANTIBODY_RATE)/100
	--VIT参数计算
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local VIT_param = 100 - VIT_hoster
	if VIT_param < 0 then
		VIT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*VIT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	--判断防御属性
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if RORandom:randDouble(0,100) <= chance and Enemy_Type%100 ~= 9 then
		return true
	else 
		return false
	end
end

--上病毒散播中毒Buff成功率判断
function BuffAddCondition.BingDuSanBoGetPoison(utilTool, firer, hoster, tableParam)      
	if firer == nil then
		return false
	end
	--基本属性
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_POISON_ANTIBODY_RATE)/100
	if Resistance == 100 then
		return false
	else 
		return true
	end
end

--上流血Buff成功率判断
function BuffAddCondition.GetBlood(utilTool, firer, hoster, tableParam)     
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_BLOOD_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_BLOOD_ANTIBODY_RATE)/100
	--AGI参数计算
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local AGI_param = 100 - AGI_hoster/3
	if AGI_param < 0 then
		AGI_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*AGI_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上灼烧Buff成功率判断
function BuffAddCondition.GetBurn(utilTool, firer, hoster, tableParam)      
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_BURN_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_BURN_ANTIBODY_RATE)/100
	--AGI参数计算
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local AGI_param = 100 - AGI_hoster
	if AGI_param < 0 then
		AGI_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*AGI_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上眩晕Buff成功率判断
function BuffAddCondition.XuanYun(utilTool, firer, hoster, tableParam)     
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_DIZZY_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_DIZZY_ANTIBODY_RATE)/100
	--VIT参数计算
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local VIT_param = 100 - VIT_hoster
	if VIT_param < 0 then
		VIT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*VIT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上冰冻Buff成功率判断
function BuffAddCondition.BingDong(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_FROZEN_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_FROZEN_ANTIBODY_RATE)/100
	--MDEF参数计算
	local MDEF_hoster = hoster:GetAttr(AttrType.ATTR_BASIC_MDEF)
	local MDEF_param = 100 - MDEF_hoster
	if MDEF_param < 0 then
		MDEF_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*MDEF_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	--判断防御属性
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if RORandom:randDouble(0,100) <= chance and Enemy_Type%100 ~= 9 then
		return true
	else 
		return false
	end
end

--上石化Buff成功率判断
function BuffAddCondition.ShiHua(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_FOSSILIZE_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_FOSSILIZE_ANTIBODY_RATE)/100
	--MDEF参数计算
	local MDEF_hoster = hoster:GetAttr(AttrType.ATTR_BASIC_MDEF)
	local MDEF_param = 100 - MDEF_hoster
	if MDEF_param < 0 then
		MDEF_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*MDEF_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	--判断防御属性
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if RORandom:randDouble(0,100) <= chance and Enemy_Type%100 ~= 9 then
		return true
	else 
		return false
	end
end

--上黑暗Buff成功率判断
function BuffAddCondition.HeiAn(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_DARK_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_DARK_ANTIBODY_RATE)/100
	--INT参数计算
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local INT_param = 100 - INT_hoster/3 - VIT_hoster/4 - LUK_hoster/4
	if INT_param < 0 then
		INT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*INT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上诅咒Buff成功率判断
function BuffAddCondition.ZuZhou(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_CURSE_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_CURSE_ANTIBODY_RATE)/100
	--VIT、LUK参数计算
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_param = 100 - VIT_hoster/2 - LUK_hoster/2
	if VIT_param < 0 then
		VIT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*VIT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上沉默Buff成功率判断
function BuffAddCondition.ChenMo(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_SILENT_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_SILENT_ANTIBODY_RATE)/100
	--VIT参数计算
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local VIT_param = 100 - VIT_hoster/2
	if VIT_param < 0 then
		VIT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*VIT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上定身Buff成功率判断
function BuffAddCondition.DingShen(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_STUN_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_STUN_ANTIBODY_RATE)/100
	--AGI参数计算
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local AGI_param = 100 - AGI_hoster
	if AGI_param < 0 then
		AGI_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*AGI_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上恐惧Buff成功率判断
function BuffAddCondition.KongJu(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_FEAR_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_FEAR_ANTIBODY_RATE)/100
	--INT参数计算
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster
	if INT_param < 0 then
		INT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*INT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上睡眠Buff成功率判断
function BuffAddCondition.ShuiMian(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_SLEEP_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_SLEEP_ANTIBODY_RATE)/100
	--INT参数计算
	local DEX_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEX)
	local DEX_param = 100 - DEX_hoster
	if DEX_param < 0 then
		DEX_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*DEX_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--上麻痹Buff成功率判断
function BuffAddCondition.MaBi(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_PARALYTIC_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_PARALYTIC_ANTIBODY_RATE)/100
	--INT参数计算
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster
	if INT_param < 0 then
		INT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*INT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	if RORandom:randDouble(0,100) <= chance then
		return true
	else 
		return false
	end
end

--病毒散播Buff成功率判断
function BuffAddCondition.BingDuSanBo(utilTool, firer,hoster,tableParam)
	--基本属性
	local curHP = hoster:GetAttr(AttrType.ATTR_PERCENT_HP)
	if curHP == 10000 then
		return false
	else
		return true
	end
end

--后退回避隐匿
function BuffAddCondition.HouTuiHuiBiYinNi(utilTool, firer, hoster, tableParam)
	if firer:GetSkillInfo(510105) ~= nil then
		return true
	end
	return false
end

--痊愈术上黑暗Buff成功率判断
function BuffAddCondition.QuanYuShuHeiAn(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_DARK_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_DARK_ANTIBODY_RATE)/100
	--INT参数计算
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local INT_param = 100 - INT_hoster/3 - VIT_hoster/4 - LUK_hoster/4
	if INT_param < 0 then
		INT_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*INT_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if RORandom:randDouble(0,100) <= chance and Enemy_Type == 9 then
		return true
	else 
		return false
	end
end

--无影之牙减速检查怪物类型
function BuffAddCondition.MonsterCheck(utilTool, firer, hoster, tableParam)
	if hoster:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_Normal or hoster:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_ELITE then
		return true
	else
		return false
	end
end

--陨石术眩晕判定
function BuffAddCondition.YunShiShuXuanYun(utilTool, firer, hoster, tableParam)
	if firer == nil then
		return false
	end
	if firer:GetSkillInfo(410113) ~= nil then
		local chance = firer:GetSkillInfo(410113):GetParam("PRO")/100
		--print("chance"..tonumber(chance))
		if RORandom:randDouble(0,100) <= chance then
			return true
		else
			return false
		end
	else
		return false
	end
end

--坏血病检测是否为玩家
function BuffAddCondition.HuaiXueBingCheck(utilTool, firer, hoster, tableParam)
	local entity_type = hoster:GetUnitTypeLevel()
	--utilTool:LuaLog(tostring(entity_type).."entity")
	--print(tostring(entity_type).."entity")
	if entity_type == -1 then
		return true
	end
	return false
end

--不死族免疫异常状态
function BuffAddCondition.BuSiMianYi(utilTool, firer,hoster,tableParam)
	--判断防御属性
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if Enemy_Type%100 ~= 9 then
		return true
	else 
		return false
	end
end

--冰冻术免疫异常状态
function BuffAddCondition.BingDongImmune(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--基本属性
	local chance_base = 100
	local chance_min = tableParam:GetParam("MinChance")/100
	local Success = firer:GetAttr(AttrType.ATTR_SPECIAL_FROZEN_SUCC_RATE)/100
	local Resistance = hoster:GetAttr(AttrType.ATTR_SPECIAL_FROZEN_ANTIBODY_RATE)/100
	--MDEF参数计算
	local MDEF_hoster = hoster:GetAttr(AttrType.ATTR_BASIC_MDEF)
	local MDEF_param = 100 - MDEF_hoster
	if MDEF_param < 0 then
		MDEF_param = 0
	end
	--等级参数计算
	local LV_firer = firer:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local LV_param = LV_firer - LV_hoster
	--上buff成功率判断
	local chance = chance_base*MDEF_param/100 + LV_param*5 + Success - Resistance
	--最小成功率
	if chance < chance_min then
		chance = chance_min
	end
	--本buff判断
	local immuneBuff = tableParam:GetParam("immuneBuff")
	--判断防御属性
	local Enemy_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	if hoster:GetBuff(immuneBuff) == nil then
		if Enemy_Type%100 ~= 9 and RORandom:randDouble(0,100) <= chance then
			return true
		else 
			return false
		end
	end
end

--速度激发上buff条件
function BuffAddCondition.SuDuJiFa(utilTool, firer,hoster,tableParam)
	-- 获取所需的武器类型
	local Need_Weapon_Type = tableParam:GetParam("WeaponType")
	local Need_Weapon_Type2 = tableParam:GetParam("WeaponType2")
	local Need_Weapon_Type3 = tableParam:GetParam("WeaponType3")
	
	local Weapon_Type = hoster:GetAttr(AttrType.ATTR_SPECIAL_WEAPON_TYPE)
	
	if Weapon_Type == Need_Weapon_Type or Weapon_Type == Need_Weapon_Type2 or Weapon_Type == Need_Weapon_Type3 then
		return true
	else
		return false
	end
end

function BuffAddCondition.KongJuZhiYuan(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--判断朝向
	--utilTool:LuaLog("xxxxxx")
	local tar_pos = hoster:GetPosition()
	local tar_face = hoster:GetFace()
	local cas_pos = firer:GetPosition()  
	local result = utilTool:IsFrontOfTarget(tar_pos, tar_face, cas_pos)
    --utilTool:LuaLog(tostring(result))
	return result
end

--词缀：狂暴效果检测自身是否已经狂暴
function BuffAddCondition.CheckBuff(utilTool, firer,hoster,tableParam)
	local BuffID = tableParam:GetParam("BuffID")
	if hoster:HasBuff(BuffID) then
		return false
	else
		return true
	end
end

--检测锤子是否还不够大
function BuffAddCondition.NoBigEnough(utilTool, firer,hoster,tableParam)
	local buff = 9266
	local buffLvl = 1
	local buffEntity = hoster:GetBuff(buff)
    if buffEntity == nil then
		return false
	else
		buffLvl = buffEntity:GetBuffLevel()
		
		if buffLvl == 4 then
			return false
		elseif buffLvl < 4 then
			return true
		end
    end
end

--BossRush：判断是否为兽人婴儿
function BuffAddCondition.IsShouRenYingEr(utilTool, firer,hoster,tableParam)
	local buff = 32877
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

--BossRush：判断是否为boss
function BuffAddCondition.IsBoss(utilTool, firer,hoster,tableParam)
	local buff = 9022
	if hoster:HasBuff(buff) then
		if firer ~= nil then
			utilTool:LuaAddBuff(firer, firer, 3288701, true)
		end
		return true
	else
		return false
	end
end

--BossRush：判断是否为毒液
function BuffAddCondition.IsDuYe(utilTool, firer,hoster,tableParam)
	local buff = 32887
	if hoster:HasBuff(buff) then
		return false
	else
		return true
	end
end

--土波利：检测眩晕
function BuffAddCondition.ToBoLiMianYiCheck(utilTool, firer,hoster,tableParam)
	local buff = 33260
	if hoster:HasBuff(buff) then
		return false
	else
		return true
	end
end

--新料理不叠加判断
function BuffAddCondition.LiaoLiCheck(utilTool, firer,hoster,tableParam)
	local selfBuff = tableParam:GetParam("selfBuff")
	if hoster:HasBuff(selfBuff) then
		return true
	else
		utilTool:LuaAddBuff(hoster, hoster, 203001001, true)
		utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "FOOD_NO_MULTI")
		return true
	end
end

--新药剂不叠加判断
function BuffAddCondition.YaoJiCheck(utilTool, firer,hoster,tableParam)
	local selfBuff = tableParam:GetParam("selfBuff")
	if hoster:HasBuff(selfBuff) then
		return true
	else
		utilTool:LuaAddBuff(hoster, hoster, 203889301, true)
		utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "DRUG_NO_MULTI")
		return true
	end
end

--在condition中添加buff（通用）
function BuffAddCondition.ConditionAddBuff(utilTool, firer,hoster,tableParam)
	local buff = tableParam:GetParam("Buff")
	utilTool:LuaAddBuff(hoster, hoster, buff, true)
	return true
end

--Follow：判断是否为小鸡
function BuffAddCondition.XiaoJiCheck(utilTool, firer,hoster,tableParam)
	local buff = 33407
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

--守护钱堆：判断主角是否有BUFF
function BuffAddCondition.QianDuiCheck(utilTool, firer,hoster,tableParam)
	local buff = 33433
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

--灵魂碎片副本buff检测
function BuffAddCondition.XiQuHunPoCheck(utilTool, firer,hoster,tableParam)
	local buff1 = tableParam:GetParam("buff1")
	local buff2 = tableParam:GetParam("buff2")
	local buff3 = tableParam:GetParam("buff3")
	local buffSelf = tableParam:GetParam("buffSelf")
	if hoster:HasBuff(buff1) or hoster:HasBuff(buff2) or hoster:HasBuff(buff3) then
		if hoster:HasBuff(buffSelf) ~= true then
			return true
		end
	end
	return false
end

--灵巧修炼：判断是否为小蘑菇
function BuffAddCondition.KillSmalMashromCheck(utilTool, firer,hoster,tableParam)
	local buff = 32523
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

--黑暗迷宫：判断是否有亮光buff
function BuffAddCondition.DarkMazeLightCheck(utilTool, firer,hoster,tableParam)
	local lightID = tableParam:GetParam("lightID")
	
	if hoster:HasBuff(lightID) then
		return false
	else
		return true
	end
end

--黑暗迷宫：判断是否是亮的火盆
function BuffAddCondition.DarkMazePenCheck(utilTool, firer,hoster,tableParam)
	local lightID = tableParam:GetParam("lightID")
	local penID = tableParam:GetParam("penID")
	local lightID2 = tableParam:GetParam("lightID2")
	
	if hoster:HasBuff(lightID) == true or hoster:HasBuff(lightID2) == true or hoster:HasBuff(penID) == false then
		return false
	else
		return true
	end
end

--黑暗迷宫：判断是否是蝙蝠
function BuffAddCondition.DarkMazeEnemyCheck(utilTool, firer,hoster,tableParam)
	local tagID = tableParam:GetParam("tagID")
	
	if hoster:HasBuff(tagID) then
		return true
	else
		return false
	end
end

--驱赶波利：判断是波利
function BuffAddCondition.CheckBoLi(utilTool, firer,hoster,tableParam)
	
	if hoster:HasBuff(9302) and hoster:HasBuff(9303) == false then
		return true
	else
		return false
	end
end

--守护钱堆：判断是否有光环
function BuffAddCondition.QianDuiCheck(utilTool, firer,hoster,tableParam)
	local buff = 33433
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

--鬼女：判断是否有回魂效果
function BuffAddCondition.HuiHunCheck(utilTool, firer,hoster,tableParam)
	local BuffID = tableParam:GetParam("BuffID")
	if hoster:HasBuff(BuffID) then
		return true
	else
		return false
	end
end

--加点体验：法师技能判断
function BuffAddCondition.MagicianSkillCheck(utilTool, firer, hoster, tableParam)
	if hoster:GetSkillInfo(400001) ~= nil or hoster:GetSkillInfo(400003) ~= nil or hoster:GetSkillInfo(400005) ~= nil or hoster:GetSkillInfo(400010) ~= nil then
		return false
	else
		return true
	end
end

--加点体验：服事技能判断
function BuffAddCondition.AcolyteSkillCheck(utilTool, firer, hoster, tableParam)
	if hoster:GetSkillInfo(300001) ~= nil then
		return false
	else
		return true
	end
end

--鬼女副本打断大招状态检测
function BuffAddCondition.BreakDaZhao(utilTool, firer,hoster,tableParam)
	local BuffID1 = tableParam:GetParam("BuffID1")
	local BuffID2 = tableParam:GetParam("BuffID2")
    if hoster:HasBuff(BuffID1) or hoster:HasBuff(BuffID2) then
		return false
	else		
	    return true
    end
end

--巴雷蒙德解除中毒状态监测
function BuffAddCondition.BreakZhongDu(utilTool, firer,hoster,tableParam)						
	local BuffID1 = tableParam:GetParam("BuffID1")					
	if hoster:HasBuff(BuffID1) then					
		return false				
	else					
	    return true					
    end						
end						

--弓手二转副本：引爆炸弹波利
function BuffAddCondition.CheckBoom(utilTool, firer,hoster,tableParam)
	local BuffID = tableParam:GetParam("BuffID")
	if hoster:HasBuff(BuffID) then
		return true
	else
		return false
	end
end

--蚁卵孵化
function BuffAddCondition.YiLuanFuHua(utilTool, firer,hoster,tableParam)
	local buff = 32529
	if hoster:HasBuff(buff) then
		return true
	else
		return false
	end
end

function BuffAddCondition.EMengJiangLin(utilTool, firer,hoster,tableParam)
	if firer == nil then
		return false
	end
	--判断朝向
	--utilTool:LuaLog("xxxxxx")
	local tar_pos = hoster:GetPosition()
	local tar_face = hoster:GetFace()
	local cas_pos = firer:GetPosition()  
	local result = utilTool:IsFrontOfTarget(tar_pos, tar_face, cas_pos)
    --utilTool:LuaLog(tostring(result))
	return result
end

--通用：判断是否有buff，var为0则没有buff返回ture，var为1则有buff返回true
function BuffAddCondition.IsHasBuff(utilTool, firer,hoster,tableParam)
	local BuffID = tableParam:GetParam("BuffID")
	local var = tableParam:GetParam("var")
	
	if hoster:HasBuff(BuffID) then
		if var == 0 then
			return false
		else
			return true
		end
	else
		if var == 1 then
			return false
		else
			return true
		end
	end
end


--场景词缀添加条件
function BuffAddCondition.AffixEntityType(utilTool, firer, hoster, tableParam)
    local bossType = tableParam:GetParam("Boss")
    local eliteType = tableParam:GetParam("Elite")
    local normalType = tableParam:GetParam("Normal")
    local entityType = hoster:GetUnitTypeLevel()

    if entityType == ROGameLibs.kUnitTypeLevel_Normal then
        if normalType == 1 then
            return true
        end
    elseif entityType == ROGameLibs.kUnitTypeLevel_ELITE then
        if eliteType == 1 then
            return true
        end
    elseif entityType == ROGameLibs.kUnitTypeLevel_BOSS then
        if bossType == 1 then
            return true
        end
    end
    return false
end

--场景词缀检测玩家是否在战斗状态
function BuffAddCondition.AffixCheckFight(utilTool, firer, hoster, tableParam)
	local inFight = hoster:IsFight()
    if inFight == false then
        return false
    else
        return true
    end
end

--天地树：检测Doodad是否可以添加
function BuffAddCondition.TianDiShuDoodadCheck(utilTool, firer,hoster,tableParam)
	if hoster:HasBuff(51507) or hoster:HasBuff(51508) or hoster:HasBuff(51509) or hoster:HasBuff(51560) or hoster:HasBuff(51512) then
		return true
	else
		return false
	end
end

--天地树回城检测
function BuffAddCondition.TdReturnHomeCheck(utilTool, firer, hoster, tableParam, args)
	local canReturn = hoster:CanAction(ROGameLibs.kStateSinging)
	local DebuffState = false
	
	if hoster:CheckBuffState(ROGameLibs.kBuffStateType_Sleep) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Fear) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Immobilize) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Frozen) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Petrochemical) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Paralysis) or 
		hoster:CheckBuffState(ROGameLibs.kBuffStateType_Stun) then
		
		DebuffState = true
	end
		
	
	if canReturn ~= true or hoster:IsMoving() == true or DebuffState == true then
		return false
	end
	
	return true
end


--同异常状态不能上检测
function BuffAddCondition.SameDebuffFalse(utilTool, firer, hoster, tableParam, args)
	local debuffType = tableParam:GetParam("type")
	
	if hoster:CheckBuffState(debuffType) then 
		return false
	end
		
	return true
end


--Belluz3009暴风雪不再造成冰冻 
function BuffAddCondition.Belluz3009(utilTool, firer,hoster,tableParam,args)
    local buffID = tableParam:GetParam("BuffID")
    if firer == nil then
        return false
    end
    if firer:HasBuff(buffID) then
        return false
    else
        return true
    end
 end
 
 --贝鲁兹3005 陨石燃烧区域光环添加判断
function BuffAddCondition.Belluz3005(utilTool, firer,hoster,tableParam,args)
    local buffID = 67608
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner:HasBuff(buffID) then
        return true
    else
        return false
    end
end

--暴风雪冰冻概率
function BuffAddCondition.BaoFengXueBingDong(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
        return false
    end
    local pro = tableParam:GetParam("Pro")/10000
    local buffID = tableParam:GetParam("BuffID")
        if firer:HasBuff(buffID) then 
           pro = 0.54  
        
        elseif RORandom:randDouble(0,1) <= pro then
           return true
           -- utilTool:LuaLog("1")
        else
           return false
        end
 end   
 
 
   --法老低语检测是否玩家或者普通怪
function BuffAddCondition.FaLaoDiYuCheck(utilTool, firer, hoster, tableParam)
	local entity_type = hoster:GetUnitTypeLevel()
	--utilTool:LuaLog(tostring(entity_type).."entity")
	--print(tostring(entity_type).."entity")
	if entity_type == -1 then
		return true
	end
	--if entity_type == 0 or 9 then
		--return true
	--end
	return false
end

--巫杖魂咬：每层灵巫提升matk加成5%，攻速3%，持续2秒，最多2层，达到最大层数进入【魂咬】状态 
function BuffAddCondition.EquipWZHY(utilTool, firer,hoster,tableParam,args)
    local buff = hoster:GetBuff(4100920)
	local buffCount = 0
	local i
    if buff == nil then
        return true
    else
		local maxCount = hoster:GetFightTempAttr("EQUIP_WZHY_MAXCOUNT")
		buffCount = buff:GetAppendCount()
		if buffCount >= maxCount then
			utilTool:LuaAddBuff(hoster, hoster, 410093001, true)
			-- elseif hunYaoBuff:GetAppendCount() < 2 then
				-- utilTool:LuaAddBuff(hoster, hoster, 410093001, true)
			-- else
				-- utilTool:LuaDelBuff(hoster, 4100930, true)
			utilTool:LuaDelBuff(hoster, 4100920, true)
			i = 1
			for i = 1,maxCount do
				utilTool:LuaAddBuff(hoster, hoster, 410092001, true)
			end
		else
			return true
		end
	end
 end 

 --装备：PTD核心精炼6判断
function BuffAddCondition.PTDRefine6Check(utilTool, firer, hoster, tableParam)
	local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	if owner ~= nil then
		local tempAttr = owner:GetFightTempAttr("EQUIP_PTD_REFINE6")
		-- local tempAttr = 1
		if tempAttr >= 1 then
			return true
		end
	end
	return false
end
 
 --蚁穴惊魂：蚁卵判断
function BuffAddCondition.IsYiLuan(utilTool, firer,hoster,tableParam)
	if hoster:HasBuff(32529) == true then
		return true
	else
		return false
	end
end