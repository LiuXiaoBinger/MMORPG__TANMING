
declareGlobal("BuffTimeCalculator", {})

-----计算Buff持续时间的函数
--[[
	firer: 产生buff的对象(不是entity时为nil), 比如A给B加了buff, 那么A是firer, B是hoster
	hoster: buff的持有者
	buffState: buff的状态
	buff：当前buff， buff:SetBuffTime(Time_result)  -- buff持续时间
								buff:SetBuffTriggerCount(Trigger_result) -- buff触发次数
	tableParam: 表里填的参数, 例CalculatorTime|INT=0|RANG=5  函数名为例CalculatorTime, tableParam:GetParam("INT") 获取到0, tableParam:GetParam("RANG") 获取到5
	]]

--中毒buff持续时间计算
function BuffTimeCalculator.ZhongDu(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local result = 100 - VIT_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--buff最低持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--流血buff持续时间计算
function BuffTimeCalculator.LiuXue(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local result = 100 - AGI_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--buff最低持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--灼烧buff持续时间计算
function BuffTimeCalculator.ZhuoShao(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local result = 100 - AGI_hoster - LUK_hoster/15
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--buff最低持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end
	
--眩晕buff持续时间计算
function BuffTimeCalculator.XuanYun(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local result = 100 - VIT_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--buff最低持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	local lastTime = Time_result/10000
	buff:SetBuffTime(lastTime)
end
	
--冰冻buff持续时间计算
function BuffTimeCalculator.BingDong(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster*5/9
	
	if INT_param < 0 then 
		INT_param = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*INT_param/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end	

--石化buff持续时间计算
function BuffTimeCalculator.ShiHua(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster*5/9
	if INT_param < 0 then 
		INT_param = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*INT_param/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end	
	
--黑暗buff持续时间计算
function BuffTimeCalculator.HeiAn(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	--VIT、LUK属性影响持续时间
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_param = 100 - VIT_hoster - LUK_hoster/5
	if VIT_param < 0 then 
		VIT_param = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*VIT_param/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--诅咒buff持续时间计算
function BuffTimeCalculator.ZuZhou(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local result = 100 - VIT_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--沉默buff持续时间计算
function BuffTimeCalculator.ChenMo(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local VIT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local result = 100 - VIT_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--定身buff持续时间计算
function BuffTimeCalculator.DingShen(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local AGI_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_AGI)
	local result = 100 - AGI_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--恐惧buff持续时间计算
function BuffTimeCalculator.KongJu(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster
	if INT_param < 0 then 
		INT_param = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*INT_param/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--睡眠buff持续时间计算
function BuffTimeCalculator.ShuiMian(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local LUK_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK)
	local DEX_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEX)
	local result = 100 - DEX_hoster - LUK_hoster/5
	if result < 0 then 
		result = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*result/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	buff:SetBuffTime(Time_result)
end

--麻痹buff持续时间计算
function BuffTimeCalculator.MaBi(utilTool, firer, hoster, buffState, buff, tableParam)
	local Time_base = tableParam:GetParam("Time")
	local Time_min = tableParam:GetParam("MinTime")
	local INT_hoster = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	local INT_param = 100 - INT_hoster
	if INT_param < 0 then 
		INT_param = 0
	end
	--计算buff持续时间
	local Time_result = Time_base*INT_param/100
	--最小buff持续时间
	if Time_result < Time_min then 
		Time_result = Time_min
	end
	local lastTime = Time_result/10000
	buff:SetBuffTime(lastTime)
end

--buff护盾血量修正通用逻辑
function BuffTimeCalculator.HuDunXueLiang(utilTool, firer, hoster, buffState, buff, tableParam)
	local HP_per = tableParam:GetParam("Per")--百分比血量
	local is_absorb = tableParam:GetParam("IsAbsorb")--判断是否吸收溢出伤害，0是不吸收溢出伤害
	local HP_Max = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	local hp = HP_Max * HP_per / 10000
	local belluz4012Buff = firer:GetBuff(65009)
	
	if firer:HasBuff(65009) == true then
		local belluz4012Lv = belluz4012Buff:GetBuffLevel()
		local belluz4012ID = 65009 * 100 + belluz4012Lv
		local belluz4012ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4012ID,false)
		local belluz4012TimeUp = belluz4012ConfigData:GetParam("timeUp")
		local belluz4012CountUp = belluz4012ConfigData:GetParam("countUp")
		local Result_times = tableParam:GetParam("Times")
		local Trigger_result = Result_times + belluz4012CountUp
		
		buff:SetBuffTime(10+belluz4012TimeUp)
		buff:SetBuffTriggerCount(Trigger_result)
		buff:SetBuffHp(hp, is_absorb~=0)
		
		
		-- if BuffLv == 1 then
		-- local Trigger_result = Result_times + 5
		-- buff:SetBuffTime(15)
		-- buff:SetBuffTriggerCount(Trigger_result)
		-- buff:SetBuffHp(hp, is_absorb~=0)
		
	    -- elseif BuffLv == 2 then
		-- local Trigger_result = Result_times + 7
	    -- buff:SetBuffTime(17)
		-- buff:SetBuffTriggerCount(Trigger_result)
	    -- buff:SetBuffHp(hp, is_absorb~=0)
		
	    -- else 
		-- local Trigger_result = Result_times + 10
		-- buff:SetBuffTime(20)
		-- buff:SetBuffTriggerCount(Trigger_result)
		-- buff:SetBuffHp(hp, is_absorb~=0)
		-- end
	else
	    buff:SetBuffHp(hp, is_absorb~=0)
    end
end

--速度激发buff时间提升
function BuffTimeCalculator.SuDuJiFaTimeUp(utilTool, firer, hoster, buffState, buff, tableParam)
	local BuffResult = hoster:HasBuff(5006)
	local Time_base = tableParam:GetParam("Time")
	--计算buff持续时间
	local Time_result = Time_base*1.1
	if BuffResult == true then
		buff:SetBuffTime(Time_result)
    else
	    return
	end
end

--凶砍buff时间提升
function BuffTimeCalculator.XiongKanTimeUp(utilTool, firer, hoster, buffState, buff, tableParam)
	local BuffResult = hoster:HasBuff(5006)
	local Time_base = tableParam:GetParam("Time")
	--计算buff持续时间
	local Time_result = Time_base*1.1
	if BuffResult == true then
		buff:SetBuffTime(Time_result)
	end
end

--无视体型攻击buff时间提升
function BuffTimeCalculator.WuShiTiXingGongJiTimeUp(utilTool, firer, hoster, buffState, buff, tableParam)
	local BuffResult = hoster:HasBuff(5006)
	local Time_base = tableParam:GetParam("Time")
	--计算buff持续时间
	local Time_result = Time_base*1.1
	if BuffResult == true then
		buff:SetBuffTime(Time_result)
	end
end

--加速术时间跟随天赐时间
function BuffTimeCalculator.JiaSuShuSkillTimeTime(utilTool, firer, hoster, buffState, buff, tableParam)
	local buffID = tableParam:GetParam("buff")
	local buffTime = tableParam:GetParam("buffTime")
	local buffTimeMax = tableParam:GetParam("buffTimeMax")
	local finalBuffTime = 0
	local buffEntity = hoster:GetBuff(buffID)
	if buffEntity ~= nil then
        local buffLvl = buffEntity:GetBuffLevel()
		if buffLvl < 10 then
			finalBuffTime = buffTime + 20*(buffLvl-1)
		elseif buffLvl == 10 then
			finalBuffTime = buffTimeMax
		end
		--print("finalBuffTime:"..tostring(finalBuffTime))
		buff:SetBuffTime(finalBuffTime)
	end
end

--定身buff持续时间计算
function BuffTimeCalculator.RandomTime(utilTool, firer, hoster, buffState, buff, tableParam)
	local var1 = 0.25
	local var2 = 0.25
	local randomSeed1 = math.random()
	local randomSeed2 = math.random()
	local randomTime1 = var1 * randomSeed1
	local randomTime2 = var2 * randomSeed2
	local randomTime = randomTime1 + randomTime2
	
	if randomTime == 0 then
		randomTime = 0.1
	end
	
	buff:SetBuffTime(randomTime)
end

--霸体受贝鲁兹核心影响
function BuffTimeCalculator.BaTiCal(utilTool, firer, hoster, buffState, buff, tableParam)
	local checkBuff = tableParam:GetParam("checkBuff")
	local basicTime = tableParam:GetParam("basicTime")
	local basicCount = tableParam:GetParam("basicCount")
	local buffEntity = hoster:GetBuff(checkBuff)
	
	if buffEntity == nil then
		return
	end
	
	local buffLevel = buffEntity:GetBuffLevel()
	local buffID = checkBuff * 100 + buffLevel
	local belluz2004ConfigData = utilTool:LuaGetBuffTableConfigData(buffID,false)
	local addTimePer = belluz2004ConfigData:GetParam("addTime")/10000
	local addCount = belluz2004ConfigData:GetParam("addCount")
	
	buff:SetBuffTime(basicTime*(1+addTimePer))
	buff:SetBuffTriggerCount(basicCount+addCount)
end

--火焰之壁击退次数受贝鲁兹3006影响
function BuffTimeCalculator.HuoYanZhiBiDestroy(utilTool, firer, hoster, buffState, buff, tableParam)
	local checkBuff = tableParam:GetParam("checkBuff")
	local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	local basicCount = tableParam:GetParam("basicCount")
	local addCount = 0
    if owner:HasBuff(checkBuff) then
        local buffLv = owner:GetBuff(checkBuff):GetBuffLevel()
		local belluz3006ID = checkBuff * 100 + buffLv
		local belluz3006ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3006ID,false)
		addCount = belluz3006ConfigData:GetParam("count")
    end
    buff:SetBuffTriggerCount(basicCount+addCount)  
end





