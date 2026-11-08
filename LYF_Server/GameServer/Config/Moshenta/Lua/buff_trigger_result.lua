require("Table/EntityTable")
declareGlobal("BuffTriggerResult", {})
--Buff特殊触发结果
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

    return 没有返回值
]]
--LuaSpecial|INT=0|RANG=5
function BuffTriggerResult.LuaSpecial(utilTool, firer, hoster, tableParam, args)
    local selfInt = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
    local param = tableParam:GetParam("INT")
    local range = tableParam:GetParam("RANG")
    if (args.trigger_timing_ == ROGameLibs.kBuffTTT_SomeOneEnter) then
        -- utilTool:LuaLog("enter entity:"..tostring(args:GetLuaEnterEntity()))
    end
end

--空函数
function BuffTriggerResult.Nil(utilTool, firer, hoster, tableParam, args)
    
end

--通用：技能减CD
function BuffTriggerResult.SkillCDDown(utilTool, firer, hoster, tableParam, args)
	local skillID = tableParam:GetParam("skillID")
	local cdDown = tableParam:GetParam("cdDown")*1000
	local skillCD = hoster:GetSkillCoolDown(skillID) - cdDown
	hoster:SetSkillCoolDown(skillID,skillCD)
end

--能量外套减伤
function BuffTriggerResult.NengLiangWaiTao(utilTool, firer, hoster, tableParam, args)
    local curSP = firer:GetAttr(AttrType.ATTR_BASIC_SP)
    local maxSP = firer:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local perSP = curSP/maxSP
	
	
    if args.behit_damage_result_ == nil then
        return
    end

    if args.behit_damage_result_:IsNoHurtType() then
        return
    end

    if perSP > 0.51 and perSP <= 1 and args.behit_damage_result_:GetResult() >= 0 then 
        args.behit_damage_result_:SetResult(args.behit_damage_result_:GetResult()*0.85)
    elseif perSP <= 0.51 and args.behit_damage_result_:GetResult() >= 0 then
        args.behit_damage_result_:SetResult(args.behit_damage_result_:GetResult()*0.9)
    end

    if args.behit_damage_result_:GetResult() <=1 and args.behit_damage_result_:GetResult() >=0 then 
        args.behit_damage_result_:SetResult(1) 
    end
end

--强制被击伤害为1
function BuffTriggerResult.BeHitZero(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end

    local damageResult = args.behit_damage_result_
	local damageNum = damageResult:GetResult()
	if damageNum >=1 then
		local hit = damageResult:GetCount()
		damageResult:SetResult(1*hit)
	end
end
 
--禅心
function BuffTriggerResult.Chanxin(utilTool, firer, hoster, tableParam, args)
    local baseSP = 0 
    local perSP = 0
    local addSP = 0
    local skillID = tableParam:GetParam("SkillID")
    
    if hoster:GetSkillInfo(skillID) ~= nil then 
        baseSP = hoster:GetSkillInfo(skillID):GetParam("SP1")
        perSP = hoster:GetSkillInfo(skillID):GetParam("SP2")/10000
    end
    local curSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
    local maxSP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    local weightPer = hoster:GetAttr(AttrType.ATTR_PERCENT_WIGHT)/10000
    
    if weightPer < 0.9 then
       addSP  = baseSP + maxSP * perSP
    end
	
	--贝鲁兹4025
	local Belluz4025buff = hoster:GetBuff(65028)
	if Belluz4025buff ~= nil then
		local Belluz4025Lv = Belluz4025buff:GetBuffLevel()
		local belluz4025ID = 65028 * 100 + Belluz4025Lv
		local belluz4025ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4025ID,false)
		local belluz4025Rate = belluz4025ConfigData:GetParam("rate")/10000
		addSP = addSP * (1+belluz4025Rate)
    end
	
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,addSP,firer,true)
    
    if curSP/maxSP < 1 and addSP ~= 0 then
        utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,addSP)
    end
end

--快速回复
function BuffTriggerResult.KuaiSuHuiFu(utilTool, firer, hoster, tableParam, args)
    local baseHP = 0
    local perHP = 0
    local addHP = 0
    if hoster:GetSkillInfo(200011) ~= nil then
        baseHP = hoster:GetSkillInfo(200011):GetParam("HP1")
        perHP  = hoster:GetSkillInfo(200011):GetParam("HP2")/10000
    end
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP) 
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local weightPer = hoster:GetAttr(AttrType.ATTR_PERCENT_WIGHT)/10000
    if weightPer >= 0.9 then
       addHP = 0
    else
       addHP = baseHP+maxHP*perHP
    end
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,addHP,firer,true)
    
    if curHP/maxHP < 1 and addHP ~= 0 then
        utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,addHP)
    end
end

--生命回复（出生）
function BuffTriggerResult.HPRescue(utilTool, firer, hoster, tableParam, args)
    local addHP = 0
    local reHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP_RESTORE_SPD_FINAL)
    local weightPer = hoster:GetAttr(AttrType.ATTR_PERCENT_WIGHT)/10000
	local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP) 
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    if weightPer < 0.9 then
       addHP = reHP
    end 
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,addHP,firer,true)
	-- if curHP/maxHP < 1 and addHP ~= 0 then
		-- utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,addHP)
	-- end
end

--魔法回复（出生）
function BuffTriggerResult.SPRescue(utilTool, firer, hoster, tableParam, args)
    local addSP = 0
    local reSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP_RESTORE_SPD_FINAL)
    local weightPer = hoster:GetAttr(AttrType.ATTR_PERCENT_WIGHT)/10000
    local curSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
    local maxSP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    if weightPer < 0.9 then
       addSP = reSP
    end 
	
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,addSP,firer,true)
	-- if curSP/maxSP < 1 and addSP ~= 0 then 
		-- utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,addSP)
	-- end
end

--蓝药回复
function BuffTriggerResult.LanpinRe(utilTool, firer, hoster, tableParam, args)
    local sp2 = tableParam:GetParam("SP2")
    local int = hoster:GetAttr(AttrType.ATTR_SPECIAL_INT)
	-- local chanXinLv1 = firer:GetSkillInfo(400011):Level() --法师禅心
	-- local chanXinLv2 = firer:GetSkillInfo(310111):Level() --牧师禅心
	local chanXinLv
	local itemSP = hoster:GetAttr(AttrType.ATTR_PERCENT_ITEM_SP_RESTORE_SPD)/10000
	if firer:GetSkillInfo(400011) == nil and firer:GetSkillInfo(310111) == nil then
		chanXinLv = 0
	elseif firer:GetSkillInfo(400011) == nil and firer:GetSkillInfo(310111) ~= nil then
		chanXinLv = firer:GetSkillInfo(310111):Level()
	else
		chanXinLv = firer:GetSkillInfo(400011):Level()
	end
    local reSP = sp2*(1+int/100+itemSP+0.05*chanXinLv)
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,reSP,firer,true)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,reSP)  
end

--红药回复
function BuffTriggerResult.HongpinRe(utilTool, firer, hoster, tableParam, args)
    local HP2 = tableParam:GetParam("HP2")
    local vit = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	local incHP
	local itemHP = hoster:GetAttr(AttrType.ATTR_PERCENT_ITEM_HP_RESTORE_SPD)/10000
    if firer:GetSkillInfo(200011) == nil then
		incHP = 0
	else
		incHP = firer:GetSkillInfo(200011):Level()
    end
	local reHP = HP2*(1+vit/100+itemHP+0.03*incHP)	
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,reHP,firer,true)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,reHP)  
end

--当前生命值为当前生命上限的百分比（回复料理）
function BuffTriggerResult.Hpmaxper(utilTool, firer, hoster, tableParam, args)

    local HPper = tableParam:GetParam("Hpper")/10000
	local HPnum = tableParam:GetParam("Hpnum")
	local SPnum = tableParam:GetParam("Spnum")
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	local HpRe

	HpRe = HPper*maxHP+HPnum
	
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,HpRe,firer,true)
	hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,SPnum,firer,true)
	
	-- utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,HpRe)  
	-- utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,SPnum) 
end

--天地树果实
function BuffTriggerResult.TianDiShu(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local maxSP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,maxHP,firer,true)
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,maxSP,firer,true)
     
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,maxHP)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,maxSP)     
end

--吸蓝
function BuffTriggerResult.SPAbsorb(utilTool, firer, hoster, tableParam, args)
    local maxSP = nil
    local absorbSp 
	local per = tableParam:GetParam("per")/10000
	local maxa = tableParam:GetParam("maxa")
	local maxb = tableParam:GetParam("maxb")
	local entity = nil
	local damageResult = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        entity = args:GetLuaAttackTarget()
		damageResult = args.attack_damage_result_
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        entity = args:GetLuaFirerEntity()
		damageResult = args.behit_damage_result_
    end
	
	if entity == nil or damageResult == nil then
        return
    end
	
	local damageType = damageResult:GetSkillDamageType()
	maxSP = entity:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
	absorbSp = maxSP*per

	if damageType == 0 then
		absorbSp = math.min(absorbSp,maxa)
	else
		absorbSp = math.min(absorbSp,maxb)
	end
	
	entity:LuaIncAttr(AttrType.ATTR_BASIC_SP,-absorbSp,firer,true) 
	--显示蓝色数字无法区分是回复还是扣除，暂时去掉此效果
	-- if absorbSp > 0 then 
    -- utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,absorbSp)   
	-- else
		-- return
	-- end
	
end


--加属性不返还
function BuffTriggerResult.GeneAttrTo(utilTool, firer, hoster, tableParam, args)
    local id = args:GetExtraParamByName("attr1")
    local value = args:GetExtraParamByName("value1")
    hoster:LuaIncAttr(id,value,firer,true)
	if id == 5 then
		utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,value)
	elseif id == 7 then
		utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,value)
	end	
end 

--根据自身攻击增加属性
function BuffTriggerResult.GetHP(utilTool, firer, hoster, tableParam, args)
    local changePer = args:GetExtraParamByName("value1") / 10000
    local damageResult = nil
    local entity = nil
	local attr = args:GetExtraParamByName("attr1")
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        damageResult = args.attack_damage_result_
        entity = args:GetLuaAttackTarget()
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if entity == nil or damageResult == nil then
        return
    end

    local up = damageResult:GetResult()*changePer
    hoster:LuaIncAttr(attr,up,firer,true) 
	utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,up)
end

--光之障壁
function BuffTriggerResult.GuangZhiZhangBiEffect(utilTool, firer, hoster, tableParam, args)
    local hp = tableParam:GetParam("HP")
    local buffID = tableParam:GetParam("BuffID")

    if args.behit_damage_result_ == nil then
        return
    end

    local damageResult = args.behit_damage_result_
    damageResult:SetType(ROGameLibs.DAMAGE_TYPE_IMMUNE)
    damageResult:SetResult(0)

    if firer ~= nil then
        local maxHP = firer:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
        local finalHP = maxHP*(hp/10000)
        firer:LuaIncAttr(AttrType.ATTR_BASIC_HP, -finalHP, firer, true)
        if firer:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL) <= 0 then
            utilTool:LuaAddBuff(hoster, hoster, buffID, true)
        end
    else
        utilTool:LuaAddBuff(hoster, hoster, buffID, true)
    end
end

--神圣殿堂
function BuffTriggerResult.ShenShengDianTangEffect(utilTool, firer, hoster, tableParam, args)

    if args.behit_damage_result_ == nil then
        return
    end

    local damageResult = args.behit_damage_result_
	if damageResult:GetResult() <= 0 then
		return
	else
		damageResult:SetType(ROGameLibs.DAMAGE_TYPE_IMMUNE)
		damageResult:SetResult(0)
	end
end

----黄金虫卡
function BuffTriggerResult.HuangJinChon(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end

    local damageResult = args.behit_damage_result_
    local damageType = damageResult:GetSkillDamageType()
    if  damageType == 2 then
        damageResult:SetResult(0)
    end
end

----回避伤害
function BuffTriggerResult.ShanDuoDam(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end
    local damageResult = args.behit_damage_result_
    damageResult:SetResult(0)  
end

--中毒伤害计算
function BuffTriggerResult.Zhongdu(utilTool, firer, hoster, tableParam, args)
        local varA1 = tableParam:GetParam("A1") / 10000
        local varA2 = tableParam:GetParam("A2") / 10000
        local varB1 = tableParam:GetParam("B1")
        local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
        local preHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
        local damageResult = maxHP * varA2 + varB1
		local entityType = hoster:GetUnitTypeLevel()
		
		if entityType == ROGameLibs.kUnitTypeLevel_BOSS or entityType == ROGameLibs.kUnitTypeLevel_MVP or entityType == ROGameLibs.kUnitTypeLevel_Mini then
			return
		end
		
        if preHP > maxHP * varA1 then
            hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,-damageResult,firer,true)
            utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_COMMON,damageResult)
        end
end

--双剑格挡判定
function BuffTriggerResult.ShuangJianGeDang(utilTool, firer, hoster, tableParam, args)
   -- local block_pro = tableParam:GetParam("block")/10000
   -- if RORandom:randDouble(0,1) <= block_pro then
      --  if args.behit_damage_result_ == nil then
       --     return
       -- end
	if args.behit_damage_result_ == nil then
        return
    end
    if args.behit_damage_result_:GetType() == ROGameLibs.DAMAGE_TYPE_BLOCK then
		--utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BLOCK,0)
        utilTool:LuaAddBuff(hoster,hoster,16301,true)
    end
end

--黑暗瞬间上Debuff
function BuffTriggerResult.HeiAnRandDebuff(utilTool, firer, hoster, tableParam, args)
    local rand = RORandom:randDouble(0,1)
    if rand <= 1/3 then
        utilTool:LuaAddBuff(firer,hoster,901401,true)
    elseif rand <= 2/3 and rand > 1/3 then
        utilTool:LuaAddBuff(firer,hoster,900501,true)
    else
        utilTool:LuaAddBuff(firer,hoster,900601,true)
    end
end

--反弹
function BuffTriggerResult.DamageReflex(utilTool, firer, hoster, tableParam, args)
    local per = tableParam:GetParam("Per") / 10000
    local minusHP = 0 
    local damageResult = nil
    local entity  = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        damageResult = args.attack_damage_result_
        entity = args:GetLuaAttackTarget()
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if damageResult == nil or entity == nil then
        return
    end
	
-- 判断是否为dummy或者无敌
	local firerEntityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
	
	
	if firerEntityID ~= nil then
		local entityTableInfo = Table.EntityTable.GetRowById(firerEntityID,true)
		if entityTableInfo ~= nil then
			local firerIsDummy = entityTableInfo.IsDummy
			local firerIsWudi = entity:GetAttr(AttrType.ATTR_SPECIAL_NO_ATTACKED)
		
			if firerIsDummy == true or firerIsWudi > 0 then
				return
			end
		end
	end	
	minusHP = damageResult:GetResult() * per 
	entity:LuaIncAttr(AttrType.ATTR_BASIC_HP,-minusHP,firer,true) 
	utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_COMMON,minusHP)
end

--病毒扩散给firer上buff
function BuffTriggerResult.PoisonSpreadTrue(utilTool, firer, hoster, tableParam, args)
    local skill = args.current_skill_id_
    if skill == 500018 then
        utilTool:LuaAddBuff(firer,args:GetLuaFirerEntity(),301201,true)
    end
end

--新手剧情触发无敌回血
function BuffTriggerResult.WuDiHuiXue(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local reHP = maxHP * 0.05
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,reHP,firer,true)
    --utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,Hp_Re) 
end

--新手剧情判断20%血限
function BuffTriggerResult.NovicePlotHPDetc(utilTool, firer, hoster, tableParam,args) 
    if  args.behit_damage_result_ == nil then
        return
    end

    local damage = args.behit_damage_result_:GetResult()
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local changePer = tableParam:GetParam("ChangePer")/10000
    local buffID = tableParam:GetParam("BuffID")
    if curHP <= maxHP * changePer then
        utilTool:LuaAddBuff(firer,hoster,buffID,true)
    end 
    if damage >= curHP then
        args.behit_damage_result_:SetResult(curHP-100)
    end
    
end

--新手剧情怪物减伤
function BuffTriggerResult.GuaiWuJianShang(utilTool, firer, hoster, tableParam,args) 
    if args.behit_damage_result_ == nil then
        return
    end

    local damage = args.behit_damage_result_:GetResult()
    if damage <= 100 then
        args.behit_damage_result_:SetResult(0)
    end
end

--反弹
function BuffTriggerResult.HJCDamageReflex(utilTool, firer, hoster, tableParam, args)
    local per = tableParam:GetParam("Per") / 10000
    local minusHP = 0 
    local damageResult = nil
    local entity = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if damageResult == nil or entity == nil then
        return
    end

    minusHP = damageResult:GetResult() * per 
    if minusHP >= 2500 then
        minusHP = 2500
    end
    entity:LuaIncAttr(AttrType.ATTR_BASIC_HP,-minusHP,firer,true)    
    utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_COMMON,minusHP)
    
end

--卡片反弹
function BuffTriggerResult.CardDamageReflex(utilTool, firer, hoster, tableParam, args)
    local Per = tableParam:GetParam("Per") / 10000
    local PerExtra = tableParam:GetParam("PerExtra") / 10000

	
    local minusHP = 0 
    local damageResult = nil
    local entity  = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        damageResult = args.attack_damage_result_
        entity = args:GetLuaAttackTarget()
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if damageResult == nil or entity == nil then
        return
    end
	
-- 判断是否为dummy或者无敌
	local firerEntityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
	
	
	if firerEntityID ~= nil then
		local entityTableInfo = Table.EntityTable.GetRowById(firerEntityID,true)
		if entityTableInfo ~= nil then
			local firerIsDummy = entityTableInfo.IsDummy
			local firerIsWudi = entity:GetAttr(AttrType.ATTR_SPECIAL_NO_ATTACKED)
		
			if firerIsDummy == true or firerIsWudi > 0 then
				return
			end
		end
	end	
	local preV = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)*PerExtra+Per
	Per = math.min(0.4,preV)
	
	minusHP = damageResult:GetResult() * Per 
	entity:LuaIncAttr(AttrType.ATTR_BASIC_HP,-minusHP,firer,true) 
	utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_COMMON,minusHP)
end




--反弹伤害（装备用）
function BuffTriggerResult.DamageReflexEq(utilTool, firer, hoster, tableParam, args)
    local per = args:GetExtraParamByName("reflex")/ 10000
    local minusHP = 0 
    local damageResult = nil
    local entity  = nil
    
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        damageResult = args.attack_damage_result_
        entity = args:GetLuaAttackTarget()
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if damageResult == nil or entity == nil then
        return
    end
	
	-- 判断是否为dummy或者无敌
	local firerEntityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
	
	if firerEntityID ~= nil then
		local entityTableInfo = Table.EntityTable.GetRowById(firerEntityID,true)
		if entityTableInfo ~= nil then
			local firerIsDummy = entityTableInfo.IsDummy
			local firerIsWudi = entity:GetAttr(AttrType.ATTR_SPECIAL_NO_ATTACKED)
		
			if firerIsDummy == true or firerIsWudi > 0 then
				return
			end
		end
	end
	minusHP  = damageResult:GetResult() * per 
	damageResult:SetResult(0)
	entity:LuaIncAttr(AttrType.ATTR_BASIC_HP,-minusHP,firer,true) 
	utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_COMMON,minusHP)
end

--魔化野猪叠加眩晕
function BuffTriggerResult.ChongFengXuanYun(utilTool, firer, hoster, tableParam, args)
    local buff = hoster:GetBuff(30102)
    if buff ~= nil then
        local BUFF_Count = buff:GetAppendCount()
        if BUFF_Count == 3 then
            utilTool:LuaAddBuff(hoster, hoster, 3010301, true)
            utilTool:LuaAddBuff(hoster, hoster, 3010401, true)
        end
    end
end

--小野猪被击取消分身
function BuffTriggerResult.BoarCheckSelf(utilTool, firer, hoster, tableParam, args)
    local entity = args:GetLuaEnterEntity()
    if entity == nil then
        return
    end

    if entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID) == 5053 then
        local entityHP = entity:GetAttr(AttrType.ATTR_BASIC_MAX_HP)
        entity:LuaIncAttr(AttrType.ATTR_BASIC_HP,-entityHP,firer,true)
        return
    end
end

--自杀
function BuffTriggerResult.Suicide(utilTool, firer, hoster, tableParam, args)
    local HP = hoster:GetAttr(AttrType.ATTR_BASIC_HP) 
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,-HP,firer,true)
end

--根据自身攻击增加属性
function BuffTriggerResult.AbortHP(utilTool, firer, hoster, tableParam, args)
    local changePer = tableParam:GetParam("ChangePer") / 10000
    local damageResult = nil
    local entity = nil
	local attr = tableParam:GetParam("attr1")
    if args.trigger_timing_ == ROGameLibs.kBuffTTT_Attack then
        damageResult = args.attack_damage_result_
        entity = args:GetLuaAttackTarget()
    elseif args.trigger_timing_ == ROGameLibs.kBuffTTT_BeHit then
        damageResult = args.behit_damage_result_
        entity = args:GetLuaFirerEntity()
    end

    if entity == nil or damageResult == nil then
        return
    end

    local up = damageResult:GetResult()*changePer 
    hoster:LuaIncAttr(attr,up,firer,true) 
end

--英灵殿锁血加buff
function BuffTriggerResult.LockHPAddBuff(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")/10000
    local buff = tableParam:GetParam("BuffID")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() > curHP - limitHP * maxHP then
            local damage = curHP - limitHP * maxHP
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
            utilTool:LuaAddBuff(hoster, hoster, buff, true)
        end
    end
end

--英灵殿克制加攻击
function BuffTriggerResult.ValhallaDmgUp(utilTool, firer, hoster, tableParam, args)
    local atkType = args.attack_type_
    local defType = args.defense_type_
    local defTypeResult = defType%100
    local damageUp = 2
    local damageResult = args.attack_damage_result_
    local damage = damageResult:GetResult()
    if (atkType == 4 and defTypeResult == 3) or (atkType == 3 and defTypeResult == 2) or (atkType == 2 and defTypeResult == 1) or (atkType == 1 and defTypeResult == 4) then
        damage = damage * damageUp
        damageResult:SetResult(damage)
        damageResult:SetRealResult(damage)
    end
end

--锁固定数值血量
function BuffTriggerResult.LockRealHP(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() > curHP - limitHP then
            local damage = curHP - limitHP
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
        end
    end
end

--小野猪伤害溢出减免
function BuffTriggerResult.LockHP(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")/10000
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() > curHP - limitHP * maxHP then
            local damage = curHP - limitHP * maxHP
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
        end
    end
end

--锁固定数值血量
function BuffTriggerResult.LockRealHP(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() > curHP - limitHP then
            local damage = curHP - limitHP
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
        end
    end
end

--小野猪分身被击惩罚
function BuffTriggerResult.BoarCheckFake(utilTool, firer, hoster, tableParam, args)
    local firer_entity = args:GetLuaFirerEntity()
    if firer_entity == nil then
        return
    end

    if firer_entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID) == 0 
    	or firer_entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID) == 5055 then
        utilTool:LuaAddBuff(hoster, firer_entity, 3011001, true)
    end
    --print("False")
end

--巨大白幽灵检测幽灵波利
function BuffTriggerResult.CheckGhostring(utilTool, firer, hoster, tableParam, args) 
    local entity = args:GetLuaEnterEntity()
    if entity == nil then
        return
    end

    local entityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
    if entityID == 5081 then
        utilTool:LuaAddBuff(firer,entity,1077001,true)
    end
end

--添加buff
-- function BuffTriggerResult.BuffAdd(utilTool,firer, hoster, tableParam, args)
--     local buffID = args:GetExtraParamByName("buffid")
--     local target = nil

--     if args:GetLuaAttackTarget() ~= nil then
--         target = args:GetLuaAttackTarget()
--     elseif args:GetLuaFirerEntity() ~= nil then
--         target = args:GetLuaFirerEntity()
--     end

--     if target == nil then
--         return
--     end
    
--     utilTool:LuaAddBuff(hoster, target, buffID, true)
-- end

--给目标添加buff
function BuffTriggerResult.BuffAddToTar(utilTool,firer, hoster, tableParam, args)
    local buffID = args:GetExtraParamByName("buffid")
    utilTool:LuaAddBuff(hoster, hoster, buffID, true)
end

--给目标添加多个buff中的一个
function BuffTriggerResult.BuffAddRand(utilTool,firer, hoster, tableParam, args)
    local buffID1 = args:GetExtraParamByName("buffid1")
	local buffID2 = args:GetExtraParamByName("buffid2")
	local buffID3 = args:GetExtraParamByName("buffid3")
	local buffID4 = args:GetExtraParamByName("buffid4")
	local buffID
    local target = nil

    if args:GetLuaAttackTarget() ~= nil then
        target = args:GetLuaAttackTarget()
    elseif args:GetLuaFirerEntity() ~= nil then
        target = args:GetLuaFirerEntity()
    end

    if target == nil then
        return
    end
    
	if RORandom:randDouble(0,1)<0.25 then
		buffID = buffID1
	elseif 0.25<=RORandom:randDouble(0,1) <0.5 then
		buffID = buffID2
	elseif 0.5<=RORandom:randDouble(0,1) <0.75 then
		buffID = buffID3
	else
		buffID = buffID4
	end

    utilTool:LuaAddBuff(hoster, target, buffID, true)
end

--毒性反弹伤害，50%概率上毒
function BuffTriggerResult.PoisonReact(utilTool, firer, hoster, tableParam, args)
    local hitRate = tableParam:GetParam("HitRate")

    local enemy = args:GetLuaFirerEntity()
    if enemy == nil then
        return
    end

    local damage = (hoster:GetAttr(AttrType.ATTR_BASIC_WEAPON_ATK_FINAL) + hoster:GetAttr(AttrType.ATTR_BASIC_BASE_ATK_FINAL))*hitRate
    local buffId = tableParam:GetParam("Buff")

    enemy:LuaIncAttr(AttrType.ATTR_BASIC_HP,-damage,firer,true)
    utilTool:LuaShowDamageResult(hoster, enemy, ROGameLibs.DAMAGE_TYPE_COMMON, damage)
    --utilTool:LuaLog("反弹伤害")
    if RORandom:randDouble(0,1) <= 0.5 then
        utilTool:LuaAddBuff(hoster, enemy, buffId, true)
    end

end

--钝器熟练度
function BuffTriggerResult.WuQiShuLianDu(utilTool, firer, hoster, tableParam, args) 
    local weaponType = hoster:GetAttr(AttrType.ATTR_SPECIAL_WEAPON_TYPE)
    local skillID = tableParam:GetParam("SkillID")
    local buffID = tableParam:GetParam("BuffID")
    local needWeaponType = tableParam:GetParam("WeaponType")
    local deleteBuffID = tableParam:GetParam("DeleteBuffID")
    local buff = hoster:GetSkillInfo(skillID)
	
    if buff ~= nil then
        local skillLv = buff:Level()
        local buffUid = buffID*100+skillLv
        if weaponType == needWeaponType then
            utilTool:LuaAddBuff(hoster,hoster,buffUid,true)
        end
    end
    utilTool:LuaAddBuff(hoster,hoster,deleteBuffID,true)
end

--陷阱伤害计算
function BuffTriggerResult.TrapDamageDown(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end

    local attackDamageType = args.behit_damage_result_:GetSkillDamageType()
    local damage_result = 0

    if attackDamageType == 0 then
        damage_result = 1
    end
    args.behit_damage_result_:SetResult(damage_result)
end

--退魂斩击退判断
function BuffTriggerResult.SoulDownCheck(utilTool, firer, hoster, tableParam, args)
    local enemy = args:GetLuaAttackTarget()
    local enemyPos = enemy:GetPosition()
    local selfPos = hoster:GetPosition()
    local dis = ROGameLibs.Vector3:Distance(enemyPos, selfPos)
    
    if dis <= 1.5 then
        utilTool:LuaAddBuff(hoster, enemy, 3235401, true)
    elseif dis > 1.5 and dis <= 3 then
        utilTool:LuaAddBuff(hoster, enemy, 3235301, true)
    elseif dis > 3 and dis <= 4.5 then
        utilTool:LuaAddBuff(hoster, enemy, 3235201, true)
    elseif dis > 4.5 then
        utilTool:LuaAddBuff(hoster, enemy, 3235101, true)
    end
end

--副本通用脱战回血
function BuffTriggerResult.DungeonBloodRe(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local reHP = maxHP - curHP
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, reHP, firer, true)
    utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, -reHP)
end

--剑攻速增加去掉buff
function BuffTriggerResult.SwordUpDeleteBuff(utilTool, firer, hoster, tableParam, args) 
    local buffID = tableParam:GetParam("BuffID")
    local needWeaponType1 = tableParam:GetParam("WeaponType1")
    local needWeaponType2 = tableParam:GetParam("WeaponType2")

    if args:GetEquipChangeInfo(0) == nil then
        return
    end

    local equipInfo = args:GetEquipChangeInfo(0)

    -- 获取换装备之前的装备id
    local beforeEquip = equipInfo.before_equip_id
    
    -- 获取换装备之后的装备id
    local afterEquip = equipInfo.after_equip_id
    
    -- 判断如果换武器之后没装备武器，换武器之前为所需武器则删buff，换武器之前为非所需武器则return
    if afterEquip == 0 then
        local beforeEquipType = utilTool:LuaGetWeaponType(beforeEquip)
        if beforeEquipType == needWeaponType1 or beforeEquipType == needWeaponType2 then
            utilTool:LuaDelBuff(hoster, buffID, true)
            return
        elseif beforeEquipType ~= needWeaponType1 and beforeEquipType ~= needWeaponType2 then
            return
        end
    end
    
    -- 获取更换武器之前的武器类型
    local beforeEquipType = utilTool:LuaGetWeaponType(beforeEquip)
    
    -- 获取更换武器之后的武器类型
    local afterEquipType = utilTool:LuaGetWeaponType(afterEquip)
    
	-- 判断更换武器前后逻辑   
    if beforeEquipType == needWeaponType1 or beforeEquipType == needWeaponType2 then
		if afterEquipType ~= needWeaponType1 and afterEquipType ~= needWeaponType2 then
        	utilTool:LuaDelBuff(hoster, buffID, true)
		end
    end
end

--坏血病逻辑
function BuffTriggerResult.HuaiXueBingCure(utilTool, firer, hoster, tableParam, args)
    utilTool:LuaAddBuff(hoster, hoster, 3231701, true)
    utilTool:LuaAddBuff(hoster, hoster, 3231801, true)
end

--坏血病区域掉血逻辑
function BuffTriggerResult.BloodAreaEffect(utilTool, firer, hoster, tableParam, args)
    local damagePercent = tableParam:GetParam("Per")/10000
    local entity = args:GetLuaEnterEntity()
	if entity == nil then
		return
	end
    local entityType = entity:GetUnitTypeLevel()
    local maxHp = entity:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local rate = 0
    if entity:HasBuff(32301) then
        rate = entity:GetBuff(32301):GetAppendCount()
    end
    local damageCal = damagePercent*maxHp*(1+rate*0.25)
    if entityType == -1 then
        entity:LuaIncAttr(AttrType.ATTR_BASIC_HP, -damageCal, firer, true)
        utilTool:LuaShowDamageResult(firer, entity, ROGameLibs.DAMAGE_TYPE_COMMON, damageCal)
    end 
end

--白色能量液  根据自身的血量上限和等级 和万分比参数进行计算
function BuffTriggerResult.BaiSeNengLiangYeHuiFuHp(utilTool, firer, hoster, tableParam, args)
    local maxHp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local baseLv = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local para = tableParam:GetParam("Para")
    local levelRate = tableParam:GetParam("LvRate")
    local replyHp = maxHp * para / 10000
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, replyHp, firer, true)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,replyHp)
end


--黄色能量液  根据自身的血量上限和等级 和万分比参数进行计算
function BuffTriggerResult.HuangSeNengLiangYeHuiFuSp(utilTool, firer, hoster, tableParam, args)
    local maxSp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    local baseLv = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local para = tableParam:GetParam("Para")
    local replySp = maxSp * para / 10000
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP, replySp, firer, true)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,replySp)
end

--回复最大生命百分比的当前生命值
function BuffTriggerResult.ReplyCurrentHpByMaxHp( utilTool, firer, hoster, tableParam, args )
    local maxHp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local para = tableParam:GetParam("Para")
    local replyHp = maxHp * para / 10000
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, replyHp, firer, true)
    utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_HP,replyHp)
end

--大门受击提示buff
function BuffTriggerResult.BattleFieldGateBeHit( utilTool, firer, hoster, tableParam, args )
    local firer_entity = args:GetLuaFirerEntity()
	if firer_entity == nil then
		return
	end
    local buff = firer_entity:GetBuff(30201)
    if buff == nil then
        utilTool:LuaShowTips(firer_entity, ROGameLibs.kShowTipType_NormalTips, "BATTLE_WARNING_NO_USE_ATTACKING_GATE")
    end

end

--变身守护者提示buff
function BuffTriggerResult.BattleFieldShouHuZhe( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "BATTLE_WARNING_BEING_A_GUARDIAN")
end

--转职试炼副本30%血限
function BuffTriggerResult.ZhuanZhiPlotHPDetc(utilTool, firer, hoster, tableParam,args) 
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local changePer = tableParam:GetParam("ChangePer")/10000
    local buffid = tableParam:GetParam("BuffID")
    
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() > curHP - changePer * maxHP then
            local damage = curHP - changePer * maxHP
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
            utilTool:LuaAddBuff(firer,hoster,buffid,true)
        end
    end 
end

--转职试炼副本30%蓝限
function BuffTriggerResult.ZhuanZhiPlotSPDetc(utilTool, firer, hoster, tableParam,args) 
    local maxSP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    local curSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
    local changePer = tableParam:GetParam("ChangePer")/10000
    local buffid = tableParam:GetParam("BuffID")

     if curSP <= maxSP * changePer then
        utilTool:LuaAddBuff(hoster,hoster,buffid,true)
    end 
end

--转职试炼男女商人上buff不同
function BuffTriggerResult.SexBuffDif(utilTool, firer, hoster, tableParam,args) 
    local sex = hoster:GetAttr(AttrType.ATTR_SPECIAL_SEX)
    local buffid1 = tableParam:GetParam("BuffID1")
	local buffid2 = tableParam:GetParam("BuffID2")

     if sex == 0  then
        utilTool:LuaAddBuff(hoster,hoster,buffid1,true)
     elseif sex == 1 then
        utilTool:LuaAddBuff(hoster,hoster,buffid2,true)
    end 
end

--暴风雪加冰冻和叠层
function BuffTriggerResult.BaoFengXue(utilTool, firer, hoster, tableParam, args) 
    local buff1 = tableParam:GetParam("Buff1")
    local buff2 = tableParam:GetParam("Buff2")
    local pro = tableParam:GetParam("Pro")/10000
    utilTool:LuaAddBuff(firer,hoster,buff2,true)
    if RORandom:randDouble(0,1) <= pro then
        utilTool:LuaAddBuff(firer,hoster,buff1,true)
    end
end

--暴风雪叠层加冰冻
function BuffTriggerResult.BaoFengXueAdd(utilTool, firer, hoster, tableParam, args) 
    local buff1 = tableParam:GetParam("Buff1")
    local buff2 = tableParam:GetParam("Buff2")
    local buff3 = tableParam:GetParam("Buff3")
    local buff2Entity = hoster:GetBuff(buff2)
    local buff1Entity = hoster:GetBuff(buff1)
    if buff2Entity ~= nil then
        local buff2Count = buff2Entity:GetAppendCount()
        if buff2Count == 3 then
            if buff1Entity == nil then
                utilTool:LuaAddBuff(hoster, hoster, buff1, true)
            else 
                utilTool:LuaAddBuff(hoster, hoster, buff3, true)
            end
        end
    end
end

--挑衅提高仇恨倍率
function BuffTriggerResult.TiaoXinUp(utilTool, firer, hoster, tableParam, args) 
    local power = tableParam:GetParam("Power")/10000
    -- if firer:GetSkillInfo(200006) ~= nil and firer == args.GetLuaAttackTarget() then
    if firer == nil then
		return
	end
	if firer:GetSkillInfo(200006) ~= nil then
        local curSkill = firer:GetSkillInfo(args.current_skill_id_)

        if curSkill == nil then
            return
        end

        local skillOldPower = curSkill:GetNowTriggerHatredToTarget(0)
        local skillPrePower = skillOldPower * (1 + power)

        if args.behit_damage_result_ == nil then
            return
        end

        args.behit_damage_result_:SetBuffHatredToTarget(skillPrePower)
    end
end

--速度激发去掉buff
function BuffTriggerResult.AdrenalineRushDeleteBuff(utilTool, firer, hoster, tableParam, args) 
    local buffID = tableParam:GetParam("BuffID")
    local needWeaponType1 = tableParam:GetParam("WeaponType1")
    local needWeaponType2 = tableParam:GetParam("WeaponType2")
    local needWeaponType3 = tableParam:GetParam("WeaponType3")
	local equipChangeInfo = args:GetEquipChangeInfo(0)
	if equipChangeInfo == nil then
		return
	end
    -- 获取换装备之前的装备id
    local beforeEquip = equipChangeInfo.before_equip_id
    
    -- 获取换装备之后的装备id
    local afterEquip = equipChangeInfo.after_equip_id
    
    -- 判断如果换武器之后没装备武器，换武器之前为所需武器则删buff，换武器之前为非所需武器则return
    if afterEquip == 0 then
        local beforeEquipType = utilTool:LuaGetWeaponType(beforeEquip)
        if beforeEquipType == needWeaponType1 or beforeEquipType == needWeaponType2 or beforeEquipType == needWeaponType3 then
            utilTool:LuaAddBuff(hoster,hoster,buffID,true)
            return
        else
            return
        end
    end
    
    -- 获取更换武器之前的武器类型
    local beforeEquipType = utilTool:LuaGetWeaponType(beforeEquip)
    
    -- 获取更换武器之后的武器类型
    local afterEquipType = utilTool:LuaGetWeaponType(afterEquip)
    
    -- 判断更换武器前后逻辑   
    if (beforeEquipType == needWeaponType1 or beforeEquipType == needWeaponType2 or beforeEquipType == needWeaponType3) and (afterEquipType ~= needWeaponType1 and afterEquipType ~= needWeaponType2 and afterEquipType ~= needWeaponType3) then
		utilTool:LuaAddBuff(hoster,hoster,buffID,true)
    end
end

--加速术加buff
function BuffTriggerResult.JiaSuShuAddBuff(utilTool, firer, hoster, tableParam, args) 
    if firer == nil then
        return
    end

    local buffID = tableParam:GetParam("Buff")*100
    local skillID = tableParam:GetParam("SkillID")

    if firer:GetSkillInfo(skillID) ~= nil then
        local sLv = firer:GetSkillInfo(skillID):Level()
        if sLv == 0 then
            sLv = 10
        end
        buffID = buffID + sLv
        utilTool:LuaAddBuff(firer, hoster, buffID, true)
    end
end

--宴会变身抱抱熊
function BuffTriggerResult.GuildTransTipsBaoBaoXiong( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "GUILD_DINNER_BAOBAOXIONG")
end

--宴会变身变小
function BuffTriggerResult.GuildTransTipsBianXiao( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "GUILD_DINNER_BIANXIAO")
end

--宴会变身变大
function BuffTriggerResult.GuildTransTipsBianDa( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "GUILD_DINNER_BIANDA")
end

--宴会变身石化
function BuffTriggerResult.GuildTransTipsShiHua( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "GUILD_DINNER_SHIHUA")
end

--宴会空
function BuffTriggerResult.GuildTransTipsKong( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "GUILD_DINNER_KONG")
end

--词缀：暴食
function BuffTriggerResult.AffixBaoShi(utilTool, firer, hoster, tableParam, args) 
    local hp_increase = tableParam:GetParam("MaxHp") * 100
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL, hp_increase, firer, true)
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP, 10000, firer, true)
    
end

-- --词缀：复生检测盟友
-- function BuffTriggerResult.AffixFuSheng(utilTool, firer, hoster, tableParam, args) 
--     local entity = args:GetLuaEnterEntity()
--     local buffID = tableParam:GetParam("BuffID")
--     if entity ~= nil then
-- 		if entity ~= hoster then
-- 			utilTool:LuaAddBuff(hoster, entity, buffID, true)
-- 		end
-- 	end
-- end

--词缀：物理/魔法反射循环开启
function BuffTriggerResult.AffixFanShe(utilTool, firer, hoster, tableParam, args) 
    local randomBuff = RORandom:randDouble(0,1)
    if randomBuff < 0.5 then
        utilTool:LuaAddBuff(firer, hoster, 5001001, true)
    else
        utilTool:LuaAddBuff(firer, hoster, 5001101, true)
    end
end

--词缀：反伤通用
function BuffTriggerResult.ReflectDamage(utilTool, firer, hoster, tableParam, args) 
   --local reflexType = tableParam:GetParam("ReflexType")
    local damageResult = args.behit_damage_result_
    local entity = args:GetLuaFirerEntity()
    local owner = utilTool:LuaGetMobMonsterOwner(entity, true)
    local physicalRate = tableParam:GetParam("PhysicalRate")/10000
    local magicRate = tableParam:GetParam("MagicRate")/10000

    if damageResult == nil or entity == nil then
        return
    end

    local damageType = damageResult:GetSkillDamageType()
    local reflexDamage = 0

    --反弹物理伤害和普攻
    if damageType == ROGameLibs.kEffectDamSkillType_Common or damageType == ROGameLibs.kEffectDamSkillType_Physical then
        if physicalRate > 0 then
            reflexDamage = damageResult:GetResult() * physicalRate
        end
    end
    --反弹魔法伤害
    if damageType == ROGameLibs.kEffectDamSkillType_Magic then
        if magicRate > 0 then
            reflexDamage = damageResult:GetResult() * magicRate
        end
    end
    if owner ~= nil then
        entity:LuaIncAttr(AttrType.ATTR_BASIC_HP, -reflexDamage, owner, true)
        utilTool:LuaShowDamageResult(hoster, owner, ROGameLibs.DAMAGE_TYPE_COMMON, reflexDamage)
    else
        entity:LuaIncAttr(AttrType.ATTR_BASIC_HP, -reflexDamage, entity, true)
        utilTool:LuaShowDamageResult(hoster, entity, ROGameLibs.DAMAGE_TYPE_COMMON, reflexDamage)
    end
end

--词缀：咒术击中别人随机给予异常状态
function BuffTriggerResult.AffixZhouShu(utilTool, firer, hoster, tableParam, args) 
    local randomBuff = math.floor(RORandom:randDouble(0,7.99))
	--local randomChance = RORandom:randDouble(0,1)
    local buffID01 = tableParam:GetParam("BuffID01")
    local buffID02 = tableParam:GetParam("BuffID02")
    local buffID03 = tableParam:GetParam("BuffID03")
    local buffID04 = tableParam:GetParam("BuffID04")
    local buffID05 = tableParam:GetParam("BuffID05")
    local buffID06 = tableParam:GetParam("BuffID06")
    local buffID07 = tableParam:GetParam("BuffID07")
    local buffID08 = tableParam:GetParam("BuffID08")

    if randomBuff == 0 then
        utilTool:LuaAddBuff(firer, hoster, buffID01, true)
    elseif randomBuff == 1 then
        utilTool:LuaAddBuff(firer, hoster, buffID02, true)
    elseif randomBuff == 2 then
        utilTool:LuaAddBuff(firer, hoster, buffID03, true)
    elseif randomBuff == 3 then
        utilTool:LuaAddBuff(firer, hoster, buffID04, true)
    elseif randomBuff == 4 then
        utilTool:LuaAddBuff(firer, hoster, buffID05, true)
    elseif randomBuff == 5 then
        utilTool:LuaAddBuff(firer, hoster, buffID06, true)
    elseif randomBuff == 6 then
        utilTool:LuaAddBuff(firer, hoster, buffID07, true)
    elseif randomBuff == 7 then
        utilTool:LuaAddBuff(firer, hoster, buffID08, true)
    end
end

--词缀：腐化——概率给别人上腐化buff
function BuffTriggerResult.AffixFuHua(utilTool, firer, hoster, tableParam, args) 
    local RandomChance = RORandom:randDouble(0,1)
    local Chance = tableParam:GetParam("Chance")/10000
    local entity = args:GetLuaFirerEntity()
    local BuffID = tableParam:GetParam("BuffID")

    if RandomChance <= Chance then
        utilTool:LuaAddBuff(firer, entity, BuffID, true)
    end
end


--词缀：元素使——获取自身属性给自己上对应的buff
function BuffTriggerResult.AffixYuanSu(utilTool, firer, hoster, tableParam, args) 
    local wu = tableParam:GetParam("Wu")
    local shui = tableParam:GetParam("Shui")
    local di = tableParam:GetParam("Di")
    local huo = tableParam:GetParam("Huo")
    local feng = tableParam:GetParam("Feng")
    local du = tableParam:GetParam("Du")
    local sheng = tableParam:GetParam("Sheng")
    local an = tableParam:GetParam("An")
    local nian = tableParam:GetParam("Nian")
    local buSi = tableParam:GetParam("BuSi")

    local eleAttr = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100
    local eleDmgPer = tableParam:GetParam("EleDmgPer")
    
    if eleAttr == 0 then
        utilTool:LuaAddBuff(firer, hoster, wu, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_FORMLESS_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 1 then
        utilTool:LuaAddBuff(firer, hoster, feng, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_WIND_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 2 then
        utilTool:LuaAddBuff(firer, hoster, di, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_EARTH_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 3 then
        utilTool:LuaAddBuff(firer, hoster, huo, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_FIRE_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 4 then
        utilTool:LuaAddBuff(firer, hoster, shui, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_WATER_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 5 then
        utilTool:LuaAddBuff(firer, hoster, du, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_POISON_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 6 then
        utilTool:LuaAddBuff(firer, hoster, sheng, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HOLY_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 7 then
        utilTool:LuaAddBuff(firer, hoster, an, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_DARK_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 8 then
        utilTool:LuaAddBuff(firer, hoster, nian, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_GHOSS_SKILL_ATK, eleDmgPer, firer, true)
    elseif eleAttr == 9 then
        utilTool:LuaAddBuff(firer, hoster, buSi, true)
        hoster:LuaIncAttr(AttrType.ATTR_PERCENT_UNDEAD_SKILL_ATK, eleDmgPer, firer, true)
    end
end

--词缀：元素使——被击恢复自身血量
function BuffTriggerResult.AffixYuanSuRe(utilTool, firer, hoster, tableParam, args) 
    local eleAttr = hoster:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100
    local damageResult = args.behit_damage_result_
	if damageResult == nil then
		return
	end
    local rate = tableParam:GetParam("Rate")
    local damamge = damageResult:GetResult()*rate
	if args.attack_type_ ~= nil then
		if args.attack_type_ == eleAttr then
			damageResult:SetResult(-damamge)
		end
	end
end

--词缀：冰霜10层变成冰冻
function BuffTriggerResult.AffixBingShuang(utilTool, firer, hoster, tableParam, args) 
    local BuffID01 = tableParam:GetParam("BuffID01")
    local BuffID02 = tableParam:GetParam("BuffID02")
	local buff = hoster:GetBuff(BuffID01)
	if buff == nil then
		return
	end
    local BuffAppend = buff:GetAppendCount()
    if BuffAppend == 10 then
        utilTool:LuaAddBuff(firer, hoster, BuffID02, true)
    end
end

--水母冰霜效果
function BuffTriggerResult.ShuiMuBingShuang(utilTool, firer, hoster, tableParam, args)
    local buff = hoster:GetBuff(32602)
    if buff ~= nil then
        local buffCount = buff:GetAppendCount()
        if buffCount == 5 then
            utilTool:LuaAddBuff(hoster, hoster, 3260301, true)
            utilTool:LuaAddBuff(hoster, hoster, 3260401, true)
        end
    end
end

--黑菇变大效果
function BuffTriggerResult.HeiGuBigger(utilTool, firer, hoster, tableParam, args)
    local buff = tableParam:GetParam("Buff")
	local buffLvl = 1
	local buffID = buff*100 + buffLvl
	local buffEntity = hoster:GetBuff(buff)
    if buffEntity == nil then
        utilTool:LuaAddBuff(hoster, hoster, buffID, true)
		return
	else
		buffLvl = buffEntity:GetBuffLevel()
		buffID = buff*100 + buffLvl
		buffLvl = buffLvl + 1
		if buffLvl > 5 then
			buffLvl = 5
		end
		buffID = buff*100 + buffLvl
		utilTool:LuaAddBuff(hoster, hoster, buffID, true)
    end
end

--毕莱特受击提示buff
function BuffTriggerResult.BiLaiTeBeHit( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(args:GetLuaFirerEntity(), ROGameLibs.kShowTipType_NormalTips, "BiLaiTeBeHit")

end

--狸猫帮buff触发添加技能并且显示
function BuffTriggerResult.BuffAttachSkill( utilTool, firer, hoster, tableParam, args )
		local SkillID = tableParam:GetParam("SkillID")
		local SkillLvl = tableParam:GetParam("SkillLvl")
		utilTool:LuaAttachSkillAndSync(hoster, SkillID, SkillLvl)
end

--红色符文
function BuffTriggerResult.RedRune(utilTool, firer, hoster, tableParam, args)
   	local greenBuff = hoster:GetBuff(9221)
   	local blueBuff = hoster:GetBuff(9222)
	local yellowBuff = hoster:GetBuff(9223)
	local purpleBuff = hoster:GetBuff(9224)
	local cyanBuff = hoster:GetBuff(9225)
    if greenBuff == nil and blueBuff == nil then
		if yellowBuff ~= nil or purpleBuff ~= nil or cyanBuff ~= nil then
			utilTool:LuaAddBuff(hoster, hoster, 923201, true)
			return
		end
	elseif greenBuff ~= nil and blueBuff == nil then
		utilTool:LuaAddBuff(hoster, hoster, 922301, true)
	elseif greenBuff == nil and blueBuff ~= nil then
		utilTool:LuaAddBuff(hoster, hoster, 922401, true)
    end
end

--绿色符文
function BuffTriggerResult.GreenRune(utilTool, firer, hoster, tableParam, args)
   	local redBuff = hoster:GetBuff(9220)
   	local blueBuff = hoster:GetBuff(9222)
	local yellowBuff = hoster:GetBuff(9223)
	local purpleBuff = hoster:GetBuff(9224)
	local cyanBuff = hoster:GetBuff(9225)
    if redBuff == nil and blueBuff == nil then
		if yellowBuff ~= nil or purpleBuff ~= nil or cyanBuff ~= nil then
			utilTool:LuaAddBuff(hoster, hoster, 923201, true)
			return
		end
	elseif redBuff ~= nil and blueBuff == nil then
		utilTool:LuaAddBuff(hoster, hoster, 922301, true)
	elseif redBuff == nil and blueBuff ~= nil then
		utilTool:LuaAddBuff(hoster, hoster, 922501, true)
    end
end

--蓝色符文
function BuffTriggerResult.BlueRune(utilTool, firer, hoster, tableParam, args)
   	local redBuff = hoster:GetBuff(9220)
   	local greenBuff = hoster:GetBuff(9221)
	local yellowBuff = hoster:GetBuff(9223)
	local purpleBuff = hoster:GetBuff(9224)
	local cyanBuff = hoster:GetBuff(9225)
    if redBuff == nil and greenBuff == nil then
		if yellowBuff ~= nil or purpleBuff ~= nil or cyanBuff ~= nil then
			utilTool:LuaAddBuff(hoster, hoster, 923201, true)
			return
		end
	elseif redBuff ~= nil and greenBuff == nil then
		utilTool:LuaAddBuff(hoster, hoster, 922401, true)
	elseif redBuff == nil and greenBuff ~= nil then
		utilTool:LuaAddBuff(hoster, hoster, 922501, true)
    end
end

--通用用lua给自己加buff
function BuffTriggerResult.UseLuaAddBuffSelf(utilTool, firer, hoster, tableParam, args)
   	local buffID = tableParam:GetParam("buffID")
	utilTool:LuaAddBuff(hoster, hoster, buffID, true)
end

--武器值最大化SP检测
function BuffTriggerResult.WeaponMaxCheckSP(utilTool, firer, hoster, tableParam, args)
    --获取当前蓝量
    local presentSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
    local buffID = tableParam:GetParam("Buff_ID")
    local skillID = tableParam:GetParam("SkillID")

    --检测蓝量数值
    if presentSP > 0 then
        return true
    else
        utilTool:LuaAddBuff(hoster, hoster, buffID, true)
        utilTool:LuaResetQteSkill(hoster, skillID)
        return false
    end
end

--词缀：不死领域吸血链
function BuffTriggerResult.AffixBloodChain( utilTool, firer, hoster, tableParam, args )
    if firer == nil then
        return
    end
    local firerMaxHp = firer:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local hosterMaxHp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local bossPer = tableParam:GetParam("BossPer")/10000
    local playerPer = tableParam:GetParam("PlayerPer")/10000
    local bossHpRe = bossPer*firerMaxHp
    local playerHpRe = playerPer*hosterMaxHp
    firer:LuaIncAttr(AttrType.ATTR_BASIC_HP, bossHpRe, firer, true)
    utilTool:LuaShowDamageResult(firer, firer, ROGameLibs.DAMAGE_TYPE_BUFF_HP, bossHpRe)
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, -playerHpRe, firer, true)
    utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, playerHpRe)
end

--铁匠铁锤总控
function BuffTriggerResult.DaTieChui(utilTool, firer, hoster, tableParam, args)
    local buff = tableParam:GetParam("Buff")
	local buffLvl = 1
	local buffID = buff*100 + buffLvl
	local buffEntity = hoster:GetBuff(buff)
    if buffEntity == nil then
        utilTool:LuaAddBuff(hoster, hoster, buffID, true)
		return
	else
		buffLvl = buffEntity:GetBuffLevel()
		
		buffID = buff*100 + buffLvl
		buffLvl = buffLvl + 1
		if buffLvl > 4 then
			buffLvl = 4
		end
		buffID = buff*100 + buffLvl
		-- if buffLvl == 4 and hoster:GetBuff(32847) == nil then
			-- utilTool:LuaAddBuff(hoster, hoster, 3284701, true)
		-- end
		utilTool:LuaAddBuff(hoster, hoster, buffID, true)
    end
end

--词缀：中毒
function BuffTriggerResult.AffixPoison( utilTool, firer, hoster, tableParam, args )
    local buff = tableParam:GetParam("Buff")
	local buffAppend = hoster:GetBuff(buff):GetAppendCount()
    local damagePer = tableParam:GetParam("Per")/10000
    local hosterMaxHp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damage = hosterMaxHp*buffAppend*damagePer
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, -damage, hoster, true)
    utilTool:LuaShowDamageResult(hoster, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, damage)
end

--哥布林守护者给boss加护盾
function BuffTriggerResult.GuardianCheckHoster(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("BuffID")
    local entity = args:GetLuaEnterEntity()
    if entity ~= nil then
        local entityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
        if entityID == 6536 or entityID == 6548 or entityID == 6541 then 
            utilTool:LuaAddBuff(hoster, entity, buffID, true)
        end
    end
end


--勋章：盗贼3
function BuffTriggerResult.Thief3DamageUp(utilTool, firer, hoster, tableParam, args)
    local rate = tableParam:GetParam("rate") / 10000
    local buffID = tableParam:GetParam("buffID")
    local damageResult = args.attack_damage_result_
	local entity = args:GetLuaAttackTarget()
	local skillAttr = args.attack_type_ % 100
	local damage = damageResult:GetResult()
	if damageResult == nil or entity == nil then
		return
	end
	
	if entity:HasBuff(buffID) == true and skillAttr == 5 then
		damage = damage * (1 + rate)
		damageResult:SetResult(damage)
	else
		return
	end
end

--勋章：法师1
function BuffTriggerResult.Magician1Burn(utilTool, firer, hoster, tableParam, args)
    local pro = tableParam:GetParam("pro") / 10000
    local buffID = tableParam:GetParam("buffID")
    local buff = math.floor(buffID/100)
	local entity = args:GetLuaAttackTarget()
	local skillAttr = args.attack_type_ % 100
	local randomNum = RORandom:randDouble(0,1)
	if entity == nil then
		return
	end
	if skillAttr == 3 and randomNum <= pro and entity:HasBuff(buff) == false then
		utilTool:LuaAddBuff(hoster, entity, buffID, true)
	elseif entity:HasBuff(buff) == true and skillAttr == 3 then
		utilTool:LuaAddBuff(hoster, entity, 810018201, true)
	else
		return
	end
end

--勋章：法师3
function BuffTriggerResult.Magician3Wind(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")
	local entity = args:GetLuaAttackTarget()
	local skillAttr = args.attack_type_ % 100
	if entity == nil then
		return
	end
	if skillAttr == 1 and entity:CheckBuffState(301) then
		utilTool:LuaAddBuff(hoster, entity, buffID, true)
	else
		return
	end
end

--勋章：牧师1
function BuffTriggerResult.Priest1AddBuff(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
		return
	end
	local oriBuffID = tableParam:GetParam("OriBuff")
	if firer:HasBuff(oriBuffID) == true then
		local proList = {1000,1300,1500,1700,2000}
		local oriBuff = firer:GetBuff(oriBuffID)
		local oriBuffLvl = oriBuff:GetBuffLevel()
		local pro = proList[oriBuffLvl] / 10000
		local randomNum = RORandom:randDouble(0,1)
		local cdBuffID = tableParam:GetParam("CDBuff") * 100 + oriBuffLvl
		local cdBuff = tableParam:GetParam("CDBuff")
		local buffID = tableParam:GetParam("Buff") * 100 + oriBuffLvl
		if firer:HasBuff(cdBuff) == false and randomNum <= pro then
			utilTool:LuaAddBuff(firer, hoster, buffID, true)
			utilTool:LuaAddBuff(firer, firer, buffID, true)
			utilTool:LuaAddBuff(firer, firer, cdBuffID, true)
		end
	end
end

--勋章：商人3
function BuffTriggerResult.Blacksmith3AddBuff(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
		return
	end
	local oriBuffID = tableParam:GetParam("OriBuff")
	if firer:HasBuff(oriBuffID) == true then
		local oriBuff = firer:GetBuff(oriBuffID)
		local oriBuffLvl = oriBuff:GetBuffLevel()
		local buffID = tableParam:GetParam("Buff") * 100 + oriBuffLvl
		utilTool:LuaAddBuff(firer, hoster, buffID, true)
	end
end

--勋章：牧师2
function BuffTriggerResult.Priest2Kill(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")
    local checkBuffID = tableParam:GetParam("CheckBuff")
	local entity = args:GetLuaEnterEntity()
	if entity == nil then
		return
	end
	local entityAttr = entity:GetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)%100
	local race = entity:GetAttr(AttrType.ATTR_SPECIAL_RACE)
	
	if (race == 2 or entityAttr == 9) and firer:HasBuff(checkBuffID) == false then
		utilTool:LuaAddBuff(hoster, hoster, buffID, true)
	else
		return
	end
end

--勋章：牧师2
function BuffTriggerResult.Priest2New(utilTool, firer, hoster, tableParam, args)
    local buff = hoster:GetBuff(8100231)
	local count = tableParam:GetParam("count")
	local addBuff = tableParam:GetParam("buffID")
	local deleteBuff = tableParam:GetParam("deleteBuffID")
    if buff ~= nil then
        local BUFF_Count = buff:GetAppendCount()
        if BUFF_Count == count then
            utilTool:LuaAddBuff(hoster, hoster, addBuff, true)
            utilTool:LuaAddBuff(hoster, hoster, deleteBuff, true)
        end
    end
end

--坐下进战触发
function BuffTriggerResult.SitDownBuff(utilTool, firer, hoster, tableParam, args) 
    local warState = args.change_fight_state_

    local addBuff = tableParam:GetParam("addBuff")
    local delBuff = tableParam:GetParam("delBuff")

    if warState == true then
        utilTool:LuaAddBuff(firer, hoster, delBuff, true)
    elseif warState == false then
        utilTool:LuaAddBuff(firer, hoster, addBuff, true)
    end
end

--法师吃光球副本：吃光球光环
function BuffTriggerResult.ChiGuangQiuGuangHuan(utilTool, firer, hoster, tableParam, args) 
    local entity = args:GetLuaEnterEntity()
    local addBuff = tableParam:GetParam("addBuff")
    local addBuff2 = tableParam:GetParam("addBuff2")
    local selfBuff = tableParam:GetParam("selfBuff")
    if entity ~= nil then
		if entity ~= hoster then
			utilTool:LuaAddBuff(hoster, entity, addBuff, true)
			utilTool:LuaAddBuff(hoster, entity, addBuff2, true)
			utilTool:LuaAddBuff(hoster, hoster, selfBuff, true)
		end
	end
end

--法师吃光球副本：吃光球光环1
function BuffTriggerResult.ChiGuangQiuGuangHuan1(utilTool, firer, hoster, tableParam, args) 
    local entity = args:GetLuaEnterEntity()
    local addBuff = tableParam:GetParam("addBuff")
    local addBuff2 = tableParam:GetParam("addBuff2")
    local addBuff3 = tableParam:GetParam("addBuff3")
    local selfBuff = tableParam:GetParam("selfBuff")
    if entity ~= nil then
		if entity ~= hoster then
			utilTool:LuaAddBuff(hoster, entity, addBuff, true)
			utilTool:LuaAddBuff(hoster, entity, addBuff2, true)
			utilTool:LuaAddBuff(hoster, entity, addBuff3, true)
			utilTool:LuaAddBuff(hoster, hoster, selfBuff, true)
		end
	end
end

--法师吃光球副本：灵魂暗击检测
function BuffTriggerResult.LingHunAnJiCheck(utilTool, firer, hoster, tableParam, args)
	local smallerBuff = tableParam:GetParam("smallerBuff")
	local biggerBuff = tableParam:GetParam("biggerBuff")
	local deleteSmallerBuff = tableParam:GetParam("deleteSmallerBuff")
	local deleteBiggerBuff = tableParam:GetParam("deleteBiggerBuff")
	local smallerBuffInfo = hoster:GetBuff(smallerBuff)
	local biggerBuffInfo = hoster:GetBuff(biggerBuff)
	local newBuffLvl = 0
	local newBuffID = 0
    if smallerBuffInfo == nil and biggerBuffInfo ~= nil then
        local biggerBuffLvl = biggerBuffInfo:GetBuffLevel()
        if biggerBuffLvl >= 2 then
            utilTool:LuaAddBuff(hoster, hoster, deleteBiggerBuff, true)
            newBuffLvl = biggerBuffLvl - 1
            newBuffID = biggerBuff*100 + newBuffLvl
			utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
			return
		elseif biggerBuffLvl <= 1 then
			utilTool:LuaAddBuff(hoster, hoster, deleteBiggerBuff, true)
			return
        end
	elseif smallerBuffInfo ~= nil and biggerBuffInfo == nil then
		local smallerBuffLvl = smallerBuffInfo:GetBuffLevel()
		if smallerBuffLvl <= 4 then
			newBuffLvl = smallerBuffLvl + 1
			newBuffID = smallerBuff*100 + newBuffLvl
			utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
			return
		elseif smallerBuffLvl >= 5 then
			return
		end
	elseif smallerBuffInfo == nil and biggerBuffInfo == nil then
		newBuffID = smallerBuff*100 + 1
		utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
		return
    end
end

--法师吃光球副本：圣灵召唤检测
function BuffTriggerResult.ShengLingZhaoHuanCheck(utilTool, firer, hoster, tableParam, args)
	local smallerBuff = tableParam:GetParam("smallerBuff")
	local biggerBuff = tableParam:GetParam("biggerBuff")
	local deleteSmallerBuff = tableParam:GetParam("deleteSmallerBuff")
	local deleteBiggerBuff = tableParam:GetParam("deleteBiggerBuff")
	local smallerBuffInfo = hoster:GetBuff(smallerBuff)
	local biggerBuffInfo = hoster:GetBuff(biggerBuff)
	local newBuffLvl = 0
	local newBuffID = 0
    if smallerBuffInfo == nil and biggerBuffInfo ~= nil then
        local biggerBuffLvl = biggerBuffInfo:GetBuffLevel()
		if biggerBuffLvl <= 4 then
			newBuffLvl = biggerBuffLvl + 1
			newBuffID = biggerBuff*100 + newBuffLvl
			utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
			return
		elseif biggerBuffLvl >= 5 then
			return
		end
	elseif smallerBuffInfo ~= nil and biggerBuffInfo == nil then
		local smallerBuffLvl = smallerBuffInfo:GetBuffLevel()
        if smallerBuffLvl >= 2 then
            utilTool:LuaAddBuff(hoster, hoster, deleteSmallerBuff, true)
            newBuffLvl = smallerBuffLvl - 1
            newBuffID = smallerBuff*100 + newBuffLvl
			utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
			return
		elseif smallerBuffLvl <= 1 then
			utilTool:LuaAddBuff(hoster, hoster, deleteSmallerBuff, true)
			return
        end
	elseif smallerBuffInfo == nil and biggerBuffInfo == nil then
		newBuffID = biggerBuff*100 + 1
		utilTool:LuaAddBuff(hoster, hoster, newBuffID, true)
		return
    end
end

--按顺序加2个buff
function BuffTriggerResult.Add2Buff(utilTool, firer, hoster, tableParam, args) 
    local addBuff1 = tableParam:GetParam("Buff1")
    local addBuff2 = tableParam:GetParam("Buff2")
	
	utilTool:LuaAddBuff(hoster, hoster, addBuff1, true)
	utilTool:LuaAddBuff(hoster, hoster, addBuff2, true)
end

--按顺序加3个buff
function BuffTriggerResult.Add3Buff(utilTool, firer, hoster, tableParam, args) 
    local addBuff1 = tableParam:GetParam("Buff1")
    local addBuff2 = tableParam:GetParam("Buff2")
	local addBuff3 = tableParam:GetParam("Buff3")
	
	utilTool:LuaAddBuff(hoster, hoster, addBuff1, true)
	utilTool:LuaAddBuff(hoster, hoster, addBuff2, true)
	utilTool:LuaAddBuff(hoster, hoster, addBuff3, true)
end

--黑暗迷宫：删除照亮技能状态
function BuffTriggerResult.EndLightSkill(utilTool, firer, hoster, tableParam, args)
    
    local skillID = tableParam:GetParam("SkillID")

    utilTool:LuaResetQteSkill(hoster, skillID)
end

--波利团：获得精灵奖励
function BuffTriggerResult.GetElfReward(utilTool, firer, hoster, tableParam, args)
    
    local elfId = tableParam:GetParam("elfId")

    hoster:SendElfEvent(elfId)
end

--调皮白幽灵受击缩小
function BuffTriggerResult.BaiYouLingSuoXiao(utilTool, firer, hoster, tableParam, args)
    local buff = hoster:GetBuff(36164)
	local newBuffLv = 0
	if buff ~= nil then
		local buffLv = buff:GetBuffLevel()
		newBuffLv = buffLv + 1
		if buffLv == 7 then
			newBuffLv = 7
		end
	else 
		newBuffLv = 0
		utilTool:LuaAddBuff(hoster, hoster, 3616401, true)
	end
	local newBuffId = 3616400 + newBuffLv
    utilTool:LuaAddBuff(hoster, hoster, newBuffId, true)
end

--疾风乱舞
function BuffTriggerResult.JiFengLuanWu(utilTool, firer, hoster, tableParam, args)
    
	local skillID1 = tableParam:GetParam("skillID1")
	local skillID2 = tableParam:GetParam("skillID2")
	local power1 = tableParam:GetParam("power1")
	local power2 = tableParam:GetParam("power2")
	
	local curSkillID = args.current_skill_id_
    local damageResult = args.attack_damage_result_
    local damageType = damageResult:GetSkillDamageType()
	
	--贝鲁兹6009
	local bulluz6009Buff = hoster:GetBuff(60047)
	if bulluz6009Buff ~= nil then
		local belluz6009Level = bulluz6009Buff:GetBuffLevel()
		local belluz6009ID = 60047 * 100 + belluz6009Level
		local belluz6009ConfigData = utilTool:LuaGetBuffTableConfigData(belluz6009ID,false)
		local belluz6009Rate = belluz6009ConfigData:GetParam("rate")/10000
		power1 = power1 * (1+belluz6009Rate)
		power2 = power2 * (1+belluz6009Rate)
	end
    
	if damageType == ROGameLibs.kEffectDamSkillType_Common then
		hoster:AddSkillPower(skillID2,power1)
	elseif curSkillID == skillID1 then
		hoster:AddSkillPower(skillID2,power2)
		
    end

	
end

--先锋追击
function BuffTriggerResult.XianFengZhuiJi(utilTool, firer, hoster, tableParam, args)
    
	local skillID1 = tableParam:GetParam("skillID1")
	local skillID2 = tableParam:GetParam("skillID2")
	
	hoster:AddSkillPower(skillID2,1)

	
end
--南门黑衣人分身给主身加护盾
function BuffTriggerResult.GuardianMainHoster(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("BuffID")
    local entity = args:GetLuaEnterEntity()
    if entity ~= nil then
        local entityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
        if entityID == 6941 then 
            utilTool:LuaAddBuff(hoster, entity, buffID, true)
        end
    end
end

--波利团：气泡初始
function BuffTriggerResult.BoLiTuanQiPao(utilTool, firer, hoster, tableParam, args)
    
	local buffTag = tableParam:GetParam("buffTag")
	local buffStart = tableParam:GetParam("buffStart")
	local randomNum = RORandom:randDouble(0,6)
	local buffID = 0
	if randomNum >= 0 and randomNum <1 then
		buffID = buffTag*1000 + (buffStart+1)*100 + 1
	elseif randomNum >= 1 and randomNum <2 then
		buffID = buffTag*1000 + (buffStart+2)*100 + 1
	elseif randomNum >= 2 and randomNum <3 then
		buffID = buffTag*1000 + (buffStart+3)*100 + 1
	elseif randomNum >= 3 and randomNum <4 then
		buffID = buffTag*1000 + (buffStart+4)*100 + 1
	elseif randomNum >= 4 and randomNum <5 then
		buffID = buffTag*1000 + (buffStart+5)*100 + 1
	elseif randomNum >= 5 and randomNum <6 then
		buffID = buffTag*1000 + (buffStart+6)*100 + 1
	end
	
	utilTool:LuaAddBuff(hoster, hoster, buffID, true)
	
end

--波利团：气泡计数
function BuffTriggerResult.BoLiTuanQiPaoCount(utilTool, firer, hoster, tableParam, args)
    
	local countNum = tableParam:GetParam("countNum")
	local elfId = tableParam:GetParam("elfId")
	local deleteBuff = tableParam:GetParam("deleteBuff")
	local selfBuff = tableParam:GetParam("selfBuff")
	
	local buff = hoster:GetBuff(selfBuff)
    if buff ~= nil then
        local buffCount = buff:GetAppendCount()
        if buffCount == countNum then
            hoster:SendElfEvent(elfId)
            utilTool:LuaAddBuff(hoster, hoster, deleteBuff, true)
        end
    end
	
end

--波利团：气泡光环
function BuffTriggerResult.BoLiTuanQiPaoGuangHuan(utilTool, firer, hoster, tableParam, args) 
    local entity = args:GetLuaEnterEntity()
    local addBuff = tableParam:GetParam("addBuff")
    local selfBuff = tableParam:GetParam("selfBuff")
    if entity ~= nil then
		if entity ~= hoster then
			utilTool:LuaAddBuff(hoster, entity, addBuff, true)
			utilTool:LuaAddBuff(hoster, hoster, selfBuff, true)
		end
	end
end

--狮子奋迅减cd
function BuffTriggerResult.ShiZiFenXun(utilTool, firer, hoster, tableParam, args)
	local skillID = tableParam:GetParam("skillID")
	local cdDown = tableParam:GetParam("cdDown")*1000
	local skillCD = hoster:GetSkillCoolDown(skillID) - cdDown
	hoster:SetSkillCoolDown(skillID,skillCD)
end

--长矛刺击下次增伤
function BuffTriggerResult.ChangMaoCiJiDamageUp(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end
	local upPre = tableParam:GetParam("upPre")/10000
    args.behit_damage_result_:SetResult(args.behit_damage_result_:GetResult()*(1+upPre))
end

--黑店乱斗给骑士加BUFF
function BuffTriggerResult.GuardianKnight(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("BuffID")
    local entity = args:GetLuaEnterEntity()
    if entity ~= nil then
        local entityID = entity:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
        if entityID == 7061 then 
            utilTool:LuaAddBuff(hoster, entity, buffID, true)
        end
    end
end

--毒性感染检测目标中毒
function BuffTriggerResult.PoisonCheck(utilTool,firer, hoster, tableParam, args)
    local buffID1 = tableParam:GetParam("BuffID1")
	--print("--------------------------------------1698")
	local buffID2 = tableParam:GetParam("BuffID2")
	--print("--------------------------------------1700")
	if hoster then
		--print("hoster not nil")
	else
		--print("hoster nil")
	end
	--print("--------------------------------------1702")
	if hoster ~= nil then
	--print("--------------------------------------1704")
	--print("attrType = "..tostring(attrType))
		local attrType = hoster:CheckBuffState(ROGameLibs.kBuffStateType_Poisoning)
		if attrType == true then
			--print("--------------------------------------1706")
			utilTool:LuaAddBuff(firer, hoster, buffID2, true)
			--print("--------------------------------------1707")
		else 
			--print("--------------------------------------1715")
			utilTool:LuaAddBuff(firer, hoster, buffID1, true)
			--print("--------------------------------------1717")

		end
	end
	
end

--易伤BUFF
function BuffTriggerResult.DamageUp(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end
	local upPre = tableParam:GetParam("upPre")/10000
    args.behit_damage_result_:SetResult(args.behit_damage_result_:GetResult()*(1+upPre))
end

--鬼女镜像副本：摄魂吸蓝
function BuffTriggerResult.DrawSPChain( utilTool, firer, hoster, tableParam, args )
    if firer == nil then
        return
    end
    local firerMaxSp = firer:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    local hosterMaxSp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
    local bossPer = tableParam:GetParam("BossPer")/10000
    local playerPer = tableParam:GetParam("PlayerPer")/10000
    local bossSpRe = bossPer*firerMaxSp
    local playerSpRe = playerPer*hosterMaxSp
    firer:LuaIncAttr(AttrType.ATTR_BASIC_SP, bossSpRe, firer, true)
    utilTool:LuaShowDamageResult(firer, firer, ROGameLibs.DAMAGE_TYPE_BUFF_SP, bossSpRe)
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP, -playerSpRe, firer, true)
    utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, playerSpRe)
end

--守护钱堆还钱
function BuffTriggerResult.HuanQian(utilTool, firer, hoster, tableParam, args)
	local buffEntity = hoster:GetBuff(33433)
		if buffEntity == nil then
		return
		else
	local buffLv = buffEntity:GetBuffLevel()
	local buffbase = 3344000
	local buffid = buffbase + buffLv
    utilTool:LuaAddBuff(hoster, hoster, buffid, true)
	
	end
end

--驱赶波利：回到身边杀波利
function BuffTriggerResult.BoLiBack(utilTool,firer, hoster, tableParam, args)
	utilTool:LuaAddBuff(hoster, hoster, 930401, true)
	utilTool:LuaAddBuff(hoster, hoster, 3270501, true)
	
end

--巧打上Debuff
function BuffTriggerResult.QiaoDaRandom(utilTool, firer, hoster, tableParam, args)
    local rand = math.floor(RORandom:randDouble(0,6))
    if rand == 0 then
        utilTool:LuaAddBuff(firer,hoster,16501,true)
    elseif rand == 1 then
        utilTool:LuaAddBuff(firer,hoster,16601,true)
    elseif rand == 2 then
        utilTool:LuaAddBuff(firer,hoster,16701,true)
    elseif rand == 3 then
        utilTool:LuaAddBuff(firer,hoster,16801,true)
    elseif rand == 4 then
        utilTool:LuaAddBuff(firer,hoster,16901,true)
    else
        utilTool:LuaAddBuff(firer,hoster,17001,true)
    end
end

--吸魂术杀死怪物回蓝
function BuffTriggerResult.XiHunShuResult(utilTool, firer, hoster, tableParam, args)
	local target = args:GetLuaAttackTarget()
	if target == nil then
		return
	end
    local entityType = target:GetUnitTypeLevel()
    if entityType == -1 then
        return
    end   
    local skillID = tableParam:GetParam("SkillID")
    local skillInfo = hoster:GetSkillInfo(skillID)
    local recSpPer = skillInfo:GetParam("RECSPPER")
	local skillSnapshot = args.trigger_timing_skill_snapshot_

    local skillIsAoe = false
    if skillSnapshot ~= nil then
        local skillTableID = skillSnapshot:GetSkillId()

        local rowdata = TableUtil.GetSkillTable().GetRowById(skillTableID)
        if rowdata == nil then
            return
        end

        skillIsAoe = rowdata.IsAoe
    else
        return
    end
    if target ~= nil and skillIsAoe == false then
        local baseLv = target:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
        local recSp = baseLv * recSpPer/10000
        hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,recSp,hoster,true) 
        utilTool:LuaShowDamageResult(hoster,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,recSp)
    end
end

--死神：锁血并加buff
function BuffTriggerResult.AffixDeathAddBuff(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local buff = tableParam:GetParam("BuffID")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() >= curHP then
            local damage = curHP - 1
            damageResult:SetResult(damage)
            damageResult:SetRealResult(damage)
            utilTool:LuaAddBuff(hoster, hoster, buff, true)
        end
    end
end

--角斗场：根据距离判断伤害
function BuffTriggerResult.AffixArena(utilTool, firer, hoster, tableParam, args)
    local dis = tableParam:GetParam("Dis")
    if args.behit_damage_result_ ~= nil then
        local entity = args:GetLuaFirerEntity()
        local entityDest = entity:GetPosition()
        local hosterDest = hoster:GetPosition()
        local distance = ROGameLibs.Vector3:Distance(entityDest, hosterDest)
        local damageResult = args.behit_damage_result_
        --print(tonumber(distance))
        if distance > dis then
            damageResult:SetResult(0)
            damageResult:SetRealResult(0)
        end
    end
end

--词缀：易伤
function BuffTriggerResult.AffixYiShang(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("BuffID")
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner ~= nil then
        utilTool:LuaAddBuff(hoster, owner, buffID, true)
    else
        utilTool:LuaAddBuff(hoster, hoster, buffID, true)
    end
end

--近身搏斗
function BuffTriggerResult.AffixCloseCombat(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ ~= nil then
        local entity = args:GetLuaFirerEntity()
        local entityDest = entity:GetPosition()
        local hosterDest = hoster:GetPosition()
        local distance = ROGameLibs.Vector3:Distance(entityDest, hosterDest)
        local damageResult = args.behit_damage_result_
        local damage = damageResult:GetResult()
        -- utilTool:LuaLog(tostring(damage))
        if distance >= 6 then
            damage = damage * 0.5
        elseif distance < 6 and distance > 3 then
            damage = damage * 0.75
        end
        damageResult:SetResult(damage)
        damageResult:SetRealResult(damage)
    end
end

--毒性外壳：根据层数算伤害
function BuffTriggerResult.AffixPosionArmor(utilTool, firer, hoster, tableParam, args)
    local damagePercent = tableParam:GetParam("Per")
    local buffID = tableParam:GetParam("BuffID")
    local append = 0
    if hoster:HasBuff(buffID) then
        append = hoster:GetBuff(buffID):GetAppendCount()
    end
    local damageCal = append*damagePercent
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP, -damageCal, hoster, true)
    utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, damageCal)
end


--场景词缀：生命偷取
function BuffTriggerResult.DrainLife(utilTool, firer, hoster, tableParam, args)
    local drainPer = tableParam:GetParam("Per")/10000
    local limit = tableParam:GetParam("Limit")/10000
    local maxHp = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP)
    local limitHp = limit * maxHp
    if args.attack_damage_result_ ~= nil then
        local damageResult = args.attack_damage_result_
        local damage = damageResult:GetResult()
        local drainLife = damage * drainPer
        if drainLife > limitHp then
            drainLife = limitHp
        end
        hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, drainLife, hoster, true)
        utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_BUFF_HP, drainLife)
    end
end

--随机加武器禁用buff
function BuffTriggerResult.RandomAddDisableEquipBuff(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
  return
    end
    
    local buffIdList={}
    local paramSize = tableParam:GetParamSize()  --新增接口：获取参数的个数

    if paramSize == 0 then
        --如果没有参数，直接返回
        return
    end

 --生成数组并打乱排序
    for i=1, paramSize, 1 do
        --通过字符串格式来去参数值，Buff1=303001101|Buff2=403001101|Buff3=503001101
        --例如：GetParam("Buff1") 取出来就是303001101
        buffIdList[i] = tableParam:GetParam("Buff" .. tostring(i))
    end

    ROLuaCommonFunction.RandomSort(buffIdList)

    local buffId = buffIdList[1]  --取出第一个buffid
    utilTool:LuaAddBuff(firer, hoster, buffId, true)
end

--金字塔：献祭掉血显示伤害数值
function BuffTriggerResult.XianJiHpDown(utilTool, firer, hoster, tableParam, args)
        local var = tableParam:GetParam("var")/10000
        local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
        local damageResult = maxHP * var
        hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,damageResult,firer,true)
        utilTool:LuaShowDamageResult(firer,hoster,ROGameLibs.DAMAGE_TYPE_COMMON,-damageResult)
end 

--塔防：英灵属性提升
function BuffTriggerResult.TowerDefenceHeroAttrUp(utilTool, firer, hoster, tableParam, args)
    local baseLv = hoster:GetAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local result = 29*(baseLv-39) + 745
	if result < 0 then
		result = 0
	end
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_DEF, result, hoster, true)
end

--塔防：树根禁疗
function BuffTriggerResult.BanCure(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() < 0 then
            damageResult:SetResult(0)
            damageResult:SetRealResult(0)
        end
    end
end

--公会狩猎：怪物出生降低血量
function BuffTriggerResult.GuildHundMonsterDown(utilTool, firer, hoster, tableParam, args)
    local attrDownVar = tableParam:GetParam("var")
    local timeCount = args.host_scene_:GetSealPieceUsedTimesFromDungeons()
    
	if timeCount == nil then
		return
	end
    
	local result = timeCount * attrDownVar

	
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_MAX_HP,-result,hoster,true)
	
end

--公会狩猎：提高玩家双攻
function BuffTriggerResult.GuildHundPlayerUp(utilTool, firer, hoster, tableParam, args)
    local attrUpVar1 = tableParam:GetParam("var1")
    local attrUpVar2 = tableParam:GetParam("var2")
    local attrUpVar3 = tableParam:GetParam("var3")
	local result = 0
	
    local timeCount = args.host_scene_:GetSimpleHuntTimesFromDungeons()
    
	if timeCount == nil then
		return
	end

	if timeCount >= 0 and timeCount < 5 then
		return
	elseif timeCount >= 5 and timeCount < 10 then
		result = attrUpVar1
	elseif timeCount >= 10 and timeCount < 15 then
		result = attrUpVar2
	elseif timeCount >= 15 then
		result = attrUpVar3
	end
	
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_DAM_INCREASE,result,hoster,true)
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE,result,hoster,true)
	
end

--通用百分比血量：hoster
function BuffTriggerResult.CommonIncHpPercent(utilTool, firer, hoster, tableParam, args)
        local var = tableParam:GetParam("var")/10000
        local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
        local damageResult = maxHP * var
        hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, damageResult, firer, true)
        utilTool:LuaShowDamageResult(firer, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, -damageResult)
end

--通用百分比血量：enter_entity
function BuffTriggerResult.HaloIncHpPercent(utilTool, firer, hoster, tableParam, args)
        local var = tableParam:GetParam("var")/10000
        if args:GetLuaEnterEntity() ~= nil then
        	local entity = args:GetLuaEnterEntity()
        	local maxHP = entity:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
        	local damageResult = maxHP * var
        	entity:LuaIncAttr(AttrType.ATTR_BASIC_HP, damageResult, firer, true)
        	utilTool:LuaShowDamageResult(firer, entity, ROGameLibs.DAMAGE_TYPE_COMMON, -damageResult)
        end
end 

--迷雾之森物理伤害提升
function BuffTriggerResult.MiWuZhiSenWuLiShangHai( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "MIWU_WULISHANGHAI")
end

--迷雾之森魔法伤害提升
function BuffTriggerResult.MiWuZhiSenMoFaShangHai( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "MIWU_MOFASHANGHAI")
end

--迷雾之森移动速度提升
function BuffTriggerResult.MiWuZhiSenYiDongSuDu( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "MIWU_YIDONGSUDU")
end

--迷雾之森持续恢复血量和蓝量
function BuffTriggerResult.MiWuZhiSenHuiFu( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "MIWU_HUIFU")
end

--迷雾之森全属性提升
function BuffTriggerResult.MiWuZhiSenShuXingTiSheng( utilTool, firer, hoster, tableParam, args )
    utilTool:LuaShowTips(hoster, ROGameLibs.kShowTipType_NormalTips, "MIWU_QUANSHUXING")
end

--炮台概率加debuff
function BuffTriggerResult.CannonDebuff(utilTool, firer, hoster, tableParam, args)
    local per = tableParam:GetParam("per")/10000
    local buffID1 = tableParam:GetParam("buffID1")
    local buffID2 = tableParam:GetParam("buffID2")
    
	local randomNum1 = RORandom:randDouble(0,1)
	local randomNum2 = RORandom:randDouble(0,1)
	
	if randomNum1 <= per then
		if randomNum2 <= 0.5 then
			utilTool:LuaAddBuff(hoster, hoster, buffID1, true)
		else
			utilTool:LuaAddBuff(hoster, hoster, buffID2, true)
		end
	end
	
end 

--炮台出生添加debuff
function BuffTriggerResult.CannonBirthTrigger(utilTool, firer, hoster, tableParam, args)
    local buffID1 = 5050
	local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	
	if owner == nil then
		return
	end
    
	if owner:HasBuff(5050) == false then
		utilTool:LuaAddBuff(owner, owner, 505001, true)
		return
	end
	
	
	
	if owner:HasBuff(5041) == false then
		return
	end 
	
	local buffInfo = owner:GetBuff(5041)
	local markBuffInfo = owner:GetBuff(5050)
	local markBuffAppend = markBuffInfo:GetAppendCount()
	local buffLevel = buffInfo:GetBuffLevel()
	
	if buffLevel <= 2 then
		if markBuffAppend <= 1 then
			utilTool:LuaAddBuff(owner, owner, 505001, true)
		end
	elseif buffLevel > 2 then
		if markBuffAppend <= 2 then
			utilTool:LuaAddBuff(owner, owner, 505001, true)
		end
	end
	
	markBuffAppend = markBuffInfo:GetAppendCount()
	
	if buffLevel == 1 then
		utilTool:LuaAddBuff(owner, owner, 504401, true)
	elseif buffLevel == 2 then
		utilTool:LuaAddBuff(owner, owner, 504501, true)
	elseif buffLevel == 3 then
		if markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		elseif markBuffAppend == 3 then
			utilTool:LuaAddBuff(owner, owner, 504601, true)
		end
	elseif buffLevel == 4 then
		if markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		elseif markBuffAppend == 3 then
			utilTool:LuaAddBuff(owner, owner, 504701, true)
		end
	elseif buffLevel == 5 then
		if markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		elseif markBuffAppend == 3 then
			utilTool:LuaAddBuff(owner, owner, 504801, true)
		end
	end
	
end

--炮台死亡触发
function BuffTriggerResult.CannonDieTrigger(utilTool, firer, hoster, tableParam, args)
    local buffID1 = 5050
	
	
	local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	
	if owner == nil then
		return
	end
    
	if owner:HasBuff(5050) == false then
		return
	end
	
	local markBuffInfo = owner:GetBuff(5050)
	local markBuffAppend = markBuffInfo:GetAppendCount()
	
	if markBuffAppend == 3 then
		utilTool:LuaAddBuff(owner, owner, 505101, true)
		utilTool:LuaAddBuff(owner, owner, 505001, true)
		utilTool:LuaAddBuff(owner, owner, 505001, true)
	elseif markBuffAppend == 2 then
		utilTool:LuaAddBuff(owner, owner, 505101, true)
		utilTool:LuaAddBuff(owner, owner, 505001, true)
	elseif markBuffAppend == 1 then
		utilTool:LuaAddBuff(owner, owner, 505101, true)
	end
	
	markBuffInfo = owner:GetBuff(5050)
	
	if markBuffInfo == nil then
		return
	end
	
	markBuffAppend = markBuffInfo:GetAppendCount()
	
	
	if owner:HasBuff(5041) == false then
		utilTool:LuaAddBuff(owner, owner, 505201, true)
		return
	end 
	
	local buffInfo = owner:GetBuff(5041)
	local buffLevel = buffInfo:GetBuffLevel()
	
	if buffLevel == 1 then
		utilTool:LuaAddBuff(owner, owner, 505201, true)
	elseif buffLevel == 2 then
		utilTool:LuaAddBuff(owner, owner, 505201, true)
	elseif buffLevel == 3 then
		if markBuffAppend == 1 then
			utilTool:LuaAddBuff(owner, owner, 505201, true)
		elseif markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		end
	elseif buffLevel == 4 then
		if markBuffAppend == 1 then
			utilTool:LuaAddBuff(owner, owner, 505201, true)
		elseif markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		end
	elseif buffLevel == 5 then
		if markBuffAppend == 1 then
			utilTool:LuaAddBuff(owner, owner, 505201, true)
		elseif markBuffAppend == 2 then
			utilTool:LuaAddBuff(owner, owner, 504501, true)
		end
	end
	
end 

--杀死当前攻击的目标
function BuffTriggerResult.KillTarget( utilTool, firer, hoster, tableParam, args )
    -- body
    local HP = args:GetLuaAttackTarget():GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    args:GetLuaAttackTarget():LuaIncAttr(AttrType.ATTR_BASIC_HP,-HP,firer,true)
end

--受伤不跳字
function BuffTriggerResult.NotShowDamageKill(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end
	local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damageResult = args.behit_damage_result_
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP, -maxHP, firer, true)
    damageResult:SetResult(0)

end

--根据进入范围的玩家buff产生不同效果
function BuffTriggerResult.FuWenXiaoChu(utilTool, firer, hoster, tableParam, args)
    local enter_entity = args:GetLuaEnterEntity()
    local buffID = tableParam:GetParam("buffID")
	
	if enter_entity:HasBuff(buffID) == true then
	   utilTool:LuaAddBuff(hoster, hoster, 3355701, true)
	else
       utilTool:LuaAddBuff(hoster, enter_entity, 3355601, true)
	end
end

--天地树天赋——荆棘铠甲
function BuffTriggerResult.JingJiKaiJia(utilTool, firer, hoster, tableParam, args) 
   --local reflexType = tableParam:GetParam("ReflexType")
    local damageResult = args.behit_damage_result_
    local entity = args:GetLuaFirerEntity()
	local reflexRate = tableParam:GetParam("reflexRate") / 10000
	
	local selfV = hoster:GetAttr(AttrType.ATTR_SPECIAL_VIT)
	
	if damageResult == nil or entity == nil then
		return
	end
    local damageType = damageResult:GetSkillDamageType()
    local reflexDamage = 0

    --反弹物理伤害和普攻
    if damageType == ROGameLibs.kEffectDamSkillType_Common then
        reflexDamage = selfV * reflexRate
        entity:LuaIncAttr(AttrType.ATTR_BASIC_HP, -reflexDamage, firer, true)
        utilTool:LuaShowDamageResult(hoster, entity, ROGameLibs.DAMAGE_TYPE_COMMON, reflexDamage)
    end
end

--天地树天赋——圣辉铠甲
function BuffTriggerResult.TianDiShuShengHuiKaiJia(utilTool, firer, hoster, tableParam, args)
    local maxHP = firer:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	
    if args.behit_damage_result_ == nil then
        return
    end

    if args.behit_damage_result_:IsNoHurtType() then
        return
    end

	local damage = args.behit_damage_result_:GetResult()
	local perDamage = damage/maxHP
	
    if perDamage > 0.15 and damage >= 0 then 
        args.behit_damage_result_:SetResult(maxHP*0.15)
    end

end

--天地树天赋——防护之力
function BuffTriggerResult.TianDiShuFangHuZhiLi(utilTool, firer, hoster, tableParam, args)
    if args.behit_damage_result_ == nil then
        return
    end

    if args.behit_damage_result_:IsNoHurtType() then
        return
    end

    args.behit_damage_result_:SetResult(0)
	utilTool:LuaShowDamageResult(hoster, hoster, ROGameLibs.DAMAGE_TYPE_BLOCK, 0)

end

--俄塞里斯卡
function BuffTriggerResult.ESaiLiSiCard(utilTool, firer, hoster, tableParam, args)
	--local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")
    local buff = tableParam:GetParam("BuffID")
    local damageResult = args.behit_damage_result_
    local damage = curHP - limitHP

    hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,-damage,hoster,true)
    utilTool:LuaShowDamageResult(hoster,hoster,ROGameLibs.DAMAGE_TYPE_COMMON,damage)
    damageResult:SetResult(0)
    damageResult:SetRealResult(0)
    utilTool:LuaAddBuff(hoster, hoster, buff, true)
end

--天地树回城检测
function BuffTriggerResult.TdReturnHomeCheck(utilTool, firer, hoster, tableParam, args)
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
		utilTool:LuaDelBuff(hoster, 51578, true)
	end
end

--神圣之怒召唤兽出生检测等级
function BuffTriggerResult.ShenShengZhiNuCheck(utilTool, firer, hoster, tableParam, args)
	local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
	if owner:GetSkillInfo(300008) ~= nil then
		local sLv = owner:GetSkillInfo(300008):Level()
		local buffID = 51543*100 + sLv
		utilTool:LuaAddBuff(hoster, hoster, buffID, true)
	end	
end

--死灵诅咒加buff
function BuffTriggerResult.SiLingZuZhouAddBuff(utilTool, firer, hoster, tableParam, args)
	local rand = RORandom:randDouble(0,1)
    if rand <= 1/3 then
        utilTool:LuaAddBuff(firer,hoster,300310,true)
    elseif rand <= 2/3 and rand > 1/3 then
        utilTool:LuaAddBuff(firer,hoster,900801,true)
    else
        utilTool:LuaAddBuff(firer,hoster,900501,true)
    end
end

--通用大嘴鸟，猎鹰技能检测
function BuffTriggerResult.CommonEquipCheck(utilTool, firer, hoster, tableParam, args)
    
    local skillId = tableParam:GetParam("skillID")
    local buffC = tableParam:GetParam("buffC")
    local buffB = tableParam:GetParam("buffB")
    local buffA = tableParam:GetParam("buffA")
    local buffCOnlyId = buffC * 100 + 1
	
	if hoster:HasBuff(buffB) and hoster:HasBuff(buffA) and hoster:HasBuff(buffC) == false then
		utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
		return
	end

    if args.trigger_timing_ ~= ROGameLibs.kBuffTTT_PassiveSkillLevelChange then
        return
    end
    if skillId ~= args.current_skill_id_ then
        return
    end
    
    local skillLevNow = args.current_skill_level_
    local skillLevBefore = args.before_skill_level_
    if skillLevNow > skillLevBefore then
        utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
    elseif skillLevNow < skillLevBefore then
        utilTool:LuaDelBuff(hoster, buffC, true)
    end

end

--通用大嘴鸟，猎鹰，手推车装备检测
function BuffTriggerResult.CommonSkillCheck(utilTool, firer, hoster, tableParam, args)
    local equipHole = tableParam:GetParam("equipHole")
    local buffC = tableParam:GetParam("buffC")
    local equipID = tableParam:GetParam("equipID")
    local equipChangeInfo = args:GetEquipChangeInfo(0)
	local buffB = tableParam:GetParam("buffB")
    local buffA = tableParam:GetParam("buffA")
    local buffCOnlyId = buffC * 100 + 1
	
	if hoster:HasBuff(buffB) and hoster:HasBuff(buffA) and hoster:HasBuff(buffC) == false then
		utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
		return
	end

    if equipChangeInfo == nil then
        return
    end

    local equipPos = equipChangeInfo.pos
    if equipPos ~= equipHole then
        return
    end
    
    -- 获取换装备之前的装备id
    local beforeVehicle = equipChangeInfo.before_equip_id
    
    -- 获取换装备之后的装备id
    local afterVehicle = equipChangeInfo.after_equip_id

    if beforeVehicle == 0 then
        if afterVehicle == equipID then
           utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
        end
    end

    if afterVehicle == 0 then
        if beforeVehicle == equipID then
           utilTool:LuaDelBuff(hoster, buffC, true)
        end
    end
end
    
--手推车技能检测
function BuffTriggerResult.BarrowEquipCheck(utilTool, firer, hoster, tableParam, args)
    
    local skillId = tableParam:GetParam("skillID")
    local buffLvl = tableParam:GetParam("buffLvl")
    local buffC = tableParam:GetParam("buffC")
    local buffA1 = tableParam:GetParam("buffA1")
    local buffA2 = tableParam:GetParam("buffA2")
    local buffA3 = tableParam:GetParam("buffA3")
    local buffB = tableParam:GetParam("buffB")
    local buffCOnlyId = buffC * 100 + buffLvl
	
	if hoster:HasBuff(buffA1) or hoster:HasBuff(buffA2) or hoster:HasBuff(buffA3) then
		if hoster:HasBuff(buffB) and hoster:HasBuff(buffC) == false then
			utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
			return
		end
	end

    if args.trigger_timing_ ~= ROGameLibs.kBuffTTT_PassiveSkillLevelChange then
        -- utilTool:LuaLog("1111111111111111111:")
        return
    end
    if skillId ~= args.current_skill_id_ then
        -- utilTool:LuaLog("22222222222222222222:")
        return
    end
    
    local skillLevNow = args.current_skill_level_
    local skillLevBefore = args.before_skill_level_
    -- utilTool:LuaLog("333333333333333333333333333:")
    if skillLevNow > skillLevBefore then
        -- utilTool:LuaLog("skillLevNow:"..tostring(skillLevNow))
        utilTool:LuaAddBuff(hoster, hoster, buffCOnlyId, true)
    elseif skillLevNow < skillLevBefore then
        -- utilTool:LuaLog("skillLevBefore:"..tostring(skillLevBefore))
        utilTool:LuaDelBuff(hoster, buffC, true)
    end

end

--痊愈光辉：加buff
function BuffTriggerResult.EquipQYGH(utilTool, firer, hoster, tableParam, args)
    local buffID = tableParam:GetParam("buffID")
    local skillLv = args.current_skill_effect_id_ % 100
	
	local buffOnlyID = buffID * 100 + skillLv
	
	if args:GetLuaAttackTarget() == nil or args.attack_damage_result_ == nil or args:GetLuaAttackTarget():GetUUID() == hoster:GetUUID() then
		return
	end
	utilTool:LuaAddBuff(hoster, args:GetLuaAttackTarget(), buffOnlyID, true)

end


--手推车装备检测
function BuffTriggerResult.BarrowSkillCheck(utilTool, firer, hoster, tableParam, args)
    local equipHole = tableParam:GetParam("equipHole")
    local barrowEquip1 = tableParam:GetParam("barrowEquip1")
    local barrowEquip2 = tableParam:GetParam("barrowEquip2")
    local barrowEquip3 = tableParam:GetParam("barrowEquip3")
	local buffA1 = tableParam:GetParam("buffA1")
    local buffA2 = tableParam:GetParam("buffA2")
    local buffA3 = tableParam:GetParam("buffA3")
    local buffB = tableParam:GetParam("buffB")
	
	if hoster:HasBuff(buffA1) then
		if hoster:HasBuff(buffB) and hoster:HasBuff(5012) == false then
			utilTool:LuaAddBuff(hoster, hoster, 501201, true)
			return
		end
	elseif hoster:HasBuff(buffA2) then
		if hoster:HasBuff(buffB) and hoster:HasBuff(5013) == false then
			utilTool:LuaAddBuff(hoster, hoster, 501301, true)
			return
		end
	elseif hoster:HasBuff(buffA3) then
		if hoster:HasBuff(buffB) and hoster:HasBuff(5014) == false then
			utilTool:LuaAddBuff(hoster, hoster, 501401, true)
			return
		end
	end
    
    local equipChangeInfo = args:GetEquipChangeInfo(0)

    if equipChangeInfo == nil then
        -- utilTool:LuaLog("5555555555555555555555555555555:")
        return
    end

    local equipPos = equipChangeInfo.pos
    if equipPos ~= equipHole then
        -- utilTool:LuaLog("6666666666666666666666666666666:")
        return
    end
    
    -- 获取换装备之前的装备id
    local beforeVehicle = equipChangeInfo.before_equip_id
    
    -- 获取换装备之后的装备id
    local afterVehicle = equipChangeInfo.after_equip_id

    if beforeVehicle == 0 then
        if afterVehicle == barrowEquip1 then
            -- utilTool:LuaLog("afterVehicle:"..tostring(afterVehicle))
            utilTool:LuaAddBuff(hoster, hoster, 501201, true)
            return
        elseif afterVehicle == barrowEquip2 then
            -- utilTool:LuaLog("afterVehicle:"..tostring(afterVehicle))
            utilTool:LuaAddBuff(hoster, hoster, 501301, true)
            return
        elseif afterVehicle == barrowEquip3 then
            -- utilTool:LuaLog("afterVehicle:"..tostring(afterVehicle))
            utilTool:LuaAddBuff(hoster, hoster, 501401, true)
            return
        end
    end

    if afterVehicle == 0 then
        if beforeVehicle == barrowEquip1 or beforeVehicle == barrowEquip2 or beforeVehicle == barrowEquip3 then
            -- utilTool:LuaLog("beforeVehicle:"..tostring(beforeVehicle))
            utilTool:LuaAddBuff(hoster, hoster, 2801, true)
            return
        end
    end
end

----贝鲁兹1002
function BuffTriggerResult.Belluz1002DamDown(utilTool, firer, hoster, tableParam, args)
    local rate = 1 - tableParam:GetParam("rate")/10000
	if args.behit_damage_result_ == nil then
        return
	elseif args.behit_damage_result_:GetResult() <= 0 then
		return
    end
    local damageResult = args.behit_damage_result_
    damageResult:SetResult(args.behit_damage_result_:GetResult() * rate)  
end

----贝鲁兹1009
function BuffTriggerResult.Belluz1009DamUp(utilTool, firer, hoster, tableParam, args)
    local damRate = 1 + tableParam:GetParam("damRate")/10000
	if args.attack_damage_result_ == nil then
        return
    end
    local damageResult = args.attack_damage_result_
    damageResult:SetResult(args.attack_damage_result_:GetResult() * damRate)  
end

----贝鲁兹1013
function BuffTriggerResult.Belluz1013(utilTool, firer, hoster, tableParam, args)
    local successBuff = tableParam:GetParam("successBuff")
    local failBuff = tableParam:GetParam("failBuff")
    local successRate = tableParam:GetParam("successRate")/10000
	local randomNum = RORandom:randDouble(0,1)
	if randomNum <= successRate then
        utilTool:LuaAddBuff(hoster, hoster, successBuff, true)
	else
		utilTool:LuaAddBuff(hoster, hoster, failBuff, true)
    end
end

----贝鲁兹1014
function BuffTriggerResult.Belluz1014(utilTool, firer, hoster, tableParam, args)
    local maxDis = tableParam:GetParam("maxDis")
	local maxRate = tableParam:GetParam("maxRate")
	if args:GetLuaAttackTarget() == nil or args.attack_damage_result_ == nil then
		return
	end
	
	local hosterDest = 0
	
	local targetDest = args:GetLuaAttackTarget():GetPosition()
	local firerEntityID = hoster:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
	if firerEntityID ~= nil then
		local entityTableInfo = Table.EntityTable.GetRowById(firerEntityID,true)
		if entityTableInfo ~= nil then
			local firerPresentID = entityTableInfo.PresentID
			if firerPresentID == 100011 then
				local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
				hosterDest = owner:GetPosition()
			else
				hosterDest = hoster:GetPosition()
			end
		else
			hosterDest = hoster:GetPosition()
		end
	else
		hosterDest = hoster:GetPosition()
	end
	
	local distance = ROGameLibs.Vector3:Distance(targetDest, hosterDest)
	
	local distanceRate = 1 - distance/maxDis
	local increaseResult = math.min(distanceRate*maxRate,maxRate)/10000
	
	if args.attack_damage_result_:GetResult() > 0 then
		local damageResult = args.attack_damage_result_
		damageResult:SetResult(args.attack_damage_result_:GetResult() * (1+increaseResult))
	end
end

----贝鲁兹1015
function BuffTriggerResult.Belluz1015(utilTool, firer, hoster, tableParam, args)
    local maxDis = tableParam:GetParam("maxDis")
	local maxRate = tableParam:GetParam("maxRate")
	if args:GetLuaAttackTarget() == nil or args.attack_damage_result_ == nil then
		return
	end
	
	local hosterDest = 0
	
	local targetDest = args:GetLuaAttackTarget():GetPosition()
	local firerEntityID = hoster:GetAttr(AttrType.ATTR_SPECIAL_ENTITY_TABLE_ID)
	if firerEntityID ~= nil then
		local entityTableInfo = Table.EntityTable.GetRowById(firerEntityID,true)
		if entityTableInfo ~= nil then
			local firerPresentID = entityTableInfo.PresentID
			if firerPresentID == 100011 then
				local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
				hosterDest = owner:GetPosition()
			else
				hosterDest = hoster:GetPosition()
			end
		else
			hosterDest = hoster:GetPosition()
		end
	else
		hosterDest = hoster:GetPosition()
	end
	
	local distance = ROGameLibs.Vector3:Distance(targetDest, hosterDest)
	
	
	local distanceRate = distance/maxDis
	local increaseResult = math.min(distanceRate*maxRate,maxRate)/10000
	
	if args.attack_damage_result_:GetResult() > 0 then
		local damageResult = args.attack_damage_result_
		damageResult:SetResult(args.attack_damage_result_:GetResult() * (1+increaseResult))
	end
end

----贝鲁兹1016
function BuffTriggerResult.Belluz1016(utilTool, firer, hoster, tableParam, args)
    local checkBuff = tableParam:GetParam("checkBuff")
	local addBuff = tableParam:GetParam("addBuff")
	local buffInfo = hoster:GetBuff(checkBuff)
	
	if buffInfo == nil then
		return
	end
	
	local buffLevel = buffInfo:GetBuffLevel()
	local addBuffID = addBuff*100 + buffLevel
	utilTool:LuaAddBuff(hoster, hoster, addBuffID, true)
end

----贝鲁兹2003
function BuffTriggerResult.Belluz2003(utilTool, firer, hoster, tableParam, args)
    local skillID = tableParam:GetParam("skillID")
	local cdDown = tableParam:GetParam("cdDown")*1000
	if hoster:GetSkillCoolDown(skillID) == nil then
		return
	end
	local skillCD = hoster:GetSkillCoolDown(skillID) - cdDown
	hoster:SetSkillCoolDown(skillID,skillCD)
end

--贝鲁兹4008
function BuffTriggerResult.Belluz4008(utilTool, firer, hoster, tableParam, args)
     if firer == nil then
	 return
	 end
	 
    local buffID01 = tableParam:GetParam("BuffID01")
	local buffID02 = tableParam:GetParam("BuffID02")
	local pro = tableParam:GetParam("Pro")
	local buff = firer:GetBuff(buffID01)
	local rand = RORandom:randDouble(0,1)
	
	if firer:HasBuff(buffID01) and rand <= pro/10000 then
	-- if firer:HasBuff(buffID01) and rand <= 1 then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = buffID02 *100 + buffLv
	   utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	else
	   return
	end
end

--贝鲁兹4009
function BuffTriggerResult.Belluz4009(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
		return
	end
	 
    local buffID01 = tableParam:GetParam("BuffID01")
	local buffID02 = tableParam:GetParam("BuffID02")
	local pro = tableParam:GetParam("Pro")
	local buff = firer:GetBuff(buffID01)
	local rand = RORandom:randDouble(0,1)
	
	if buff == nil then
		return
	end
	
	local buffLv = buff:GetBuffLevel()
	local belluz4009ID = buffID01 * 100 + buffLv
	local belluz4009ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4009ID,false)
	local belluz4009Pro = belluz4009ConfigData:GetParam("pro")/10000

	
	if firer:HasBuff(buffID01) and rand <= belluz4009Pro then
	-- if firer:HasBuff(buffID01) and rand <= 1 then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = buffID02 *100 + buffLv
	   utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	else
	   return
	end
end

----贝鲁兹4013
function BuffTriggerResult.Belluz4013(utilTool, firer, hoster, tableParam, args)	
	if args.attack_damage_result_ == nil then
        return
    end

	local upPer = tableParam:GetParam("upPer")/10000
    local damageResult = args.attack_damage_result_
	local damage = damageResult:GetResult() * (1 + upPer) 
	--utilTool:LuaLog("damage"..tonumber(damage))
    damageResult:SetResult(damage)  
end

--贝鲁兹4018
function BuffTriggerResult.Belluz4018(utilTool, firer, hoster, tableParam, args)
	local owner = utilTool:LuaGetMobMonsterOwner(firer, true)
	local buff = owner:GetBuff(65021)
	
	if buff ~= nil then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = 6502200 + buffLv
	   --utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
	   utilTool:LuaAddBuff(owner, hoster, finalBuff, true)
	end
end

--贝鲁兹4019
function BuffTriggerResult.Belluz4019(utilTool, firer, hoster, tableParam, args)
	
	if firer == nil then
		
		return
	end
	
	local buff = firer:GetBuff(65023)
	
	if buff ~= nil then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = 6505300 + buffLv
	   --utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
	   utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	end
end

--贝鲁兹4004加buff
function BuffTriggerResult.Belluz4004AddBuff(utilTool, firer, hoster, tableParam, args)
	
	if firer == nil then
		
		return
	end
	
	local buff = firer:GetBuff(67638)
	
	if buff ~= nil then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = 6007600 + buffLv
	   --utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
	   utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	end
end

--贝鲁兹4003加buff
function BuffTriggerResult.Belluz4003AddBuff(utilTool, firer, hoster, tableParam, args)
	
	if firer == nil then
		
		return
	end
	
	local buff = firer:GetBuff(67637)
	
	if buff ~= nil then
	   local buffLv = buff:GetBuffLevel()
	   local finalBuff = 6007800 + buffLv
	   --utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
	   utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	end
end

--贝鲁兹4020
function BuffTriggerResult.Belluz4020(utilTool, firer, hoster, tableParam, args)
	local target = args:GetLuaAttackTarget()
	local buff = firer:GetBuff(65050)
	
	if target == nil then
		return false
	end
	
	if buff ~= nil then
		local buffLv = buff:GetBuffLevel()
		local finalBuff = 6505200 + buffLv
		--utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
		utilTool:LuaAddBuff(firer, target, finalBuff, true)
	end
end

--贝鲁兹4023
function BuffTriggerResult.Belluz4023(utilTool, firer, hoster, tableParam, args)
	if hoster:HasBuff(65026) == true then 
		local  multi = tableParam:GetParam("Multi")
		local  weightCount = utilTool:LuaGetCurrentSkillProbWeightCount(hoster)
		local  weightPos01 = utilTool:LuaGetCurrentSkillProbWeightPos(hoster, 0)
		--utilTool:LuaLog("weightPos01"..tonumber(weightPos01))
		local  weightPos02 = utilTool:LuaGetCurrentSkillProbWeightPos(hoster, 1)
		local  weightPos03 = utilTool:LuaGetCurrentSkillProbWeightPos(hoster, 2)
		local  luk =  hoster:GetAttr(AttrType.ATTR_SPECIAL_LUK )
		--utilTool:LuaLog("luk"..tonumber(luk))
		local  pro2 = luk* multi/10000+ weightPos02
		local  pro3 = luk* multi/10000+ weightPos03
		local  pro1 = weightPos01- luk* multi/10000 - luk* multi/10000
		--utilTool:LuaLog("pro1"..tonumber(pro1))
		--utilTool:LuaLog("pro2"..tonumber(pro2))
		--utilTool:LuaLog("pro3"..tonumber(pro3))
			--判断几率是否超过100%
			if pro3 >= 100 then
				pro1 = 0
				pro2 = 0
				pro3 = 100
			elseif pro3 + pro2 >= 100 then
				pro1 = 0
				pro2 = 100 - pro3
				pro3 = pro3
			else
				pro1 = pro1
				pro2 = pro2
				pro3 = pro3
			end

		local finalpro1 = pro1
		local finalpro2 = pro2
		local finalpro3 = pro3	
			--utilTool:LuaLog("pro3"..tonumber(pro3))
     		
		utilTool:LuaSetCurrentSkillProbWeightPos(hoster, 0, finalpro1)
		utilTool:LuaSetCurrentSkillProbWeightPos(hoster, 1, finalpro2)
		utilTool:LuaSetCurrentSkillProbWeightPos(hoster, 2, finalpro3)
	end
 end
	   
--贝鲁兹check通用
function BuffTriggerResult.BelluzCheck(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return
	end
	 
	local buffID01 = tableParam:GetParam("BuffID01")
	local buffID02 = tableParam:GetParam("BuffID02")
	local buff = firer:GetBuff(buffID01)
	
	if firer:HasBuff(buffID01) then
		local buffLv = buff:GetBuffLevel()
		local finalBuff = buffID02 *100 + buffLv
		utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	else
		return
	end
end

--贝鲁兹5007
function BuffTriggerResult.Belluz5007(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return
	end
	 
	local buff = tableParam:GetParam("BuffID")
	local hpcure = tableParam:GetParam("HPcure")
	local isbuff = firer:GetBuff(buff)
	
	if firer:HasBuff(buff) then
		local buffLv = isbuff:GetBuffLevel()
		local belluz5007ID = buff * 100 + buffLv
		local belluz5007ConfigData = utilTool:LuaGetBuffTableConfigData(belluz5007ID,false)
		local belluz5007Rate = belluz5007ConfigData:GetParam("rate")
		firer:LuaIncAttr(AttrType.ATTR_PERCENT_HP,belluz5007Rate,hoster,true)
	else
		return
	end
end

--贝鲁兹5008
function BuffTriggerResult.Belluz5008(utilTool, firer, hoster, tableParam, args)
	if firer == nil then
		return
	end
	 
    local buffID01 = tableParam:GetParam("BuffID01")
	local buffID02 = tableParam:GetParam("BuffID02")
	local buff = hoster:GetBuff(buffID01)
	
	if hoster:GetSkillInfo(500011) ~= nil and hoster:HasBuff(buffID01) == true then
		local buffLv = buff:GetBuffLevel()
		local finalBuff = buffID02 *100 + buffLv
		utilTool:LuaAddBuff(firer, hoster, finalBuff, true)
	else
		return
	end
end

----贝鲁兹5004
function BuffTriggerResult.Belluz5004(utilTool, firer, hoster, tableParam, args)	 
    local skillID = tableParam:GetParam("skillID")
	local cdDown = tableParam:GetParam("cdDown")
	if hoster:GetSkillCoolDown(skillID) == nil then
		return
	end
	local skillCD = hoster:GetSkillCoolDown(skillID) - cdDown
	hoster:SetSkillCoolDown(skillID,skillCD)
end

----贝鲁兹3010增伤
function BuffTriggerResult.Belluz3010DamUp(utilTool, firer, hoster, tableParam, args)
    local damRate = 1 + tableParam:GetParam("damRate")/10000
    if args.behit_damage_result_ == nil then
        return
    end
    local damageResult = args.behit_damage_result_
    damageResult:SetResult(args.behit_damage_result_:GetResult() * damRate)  
end

----贝鲁兹3014死亡冰箱效果
function BuffTriggerResult.Belluz3014(utilTool, firer, hoster, tableParam, args)
    local maxHP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local limitHP = tableParam:GetParam("Limit")
    local buff = tableParam:GetParam("BuffID")
    if args.behit_damage_result_ ~= nil then
        local damageResult = args.behit_damage_result_
        if damageResult:GetResult() >= curHP then
            local damage = curHP - limitHP
            hoster:LuaIncAttr(AttrType.ATTR_BASIC_HP,-damage,hoster,true)
            utilTool:LuaShowDamageResult(hoster,hoster,ROGameLibs.DAMAGE_TYPE_COMMON,damage)
            damageResult:SetResult(0)
            damageResult:SetRealResult(0)
            utilTool:LuaAddBuff(hoster, hoster, buff, true)
        end
    end
end

----贝鲁兹3017水球术自然充能
function BuffTriggerResult.Belluz3017(utilTool, firer, hoster, tableParam, args)
    
    local skillID = tableParam:GetParam("skillID")
    local power = tableParam:GetParam("power")   
    hoster:AddSkillPower(skillID,power)   
end


----贝鲁兹6007
function BuffTriggerResult.Belluz6007(utilTool, firer, hoster, tableParam, args)
    local maxDis = tableParam:GetParam("maxDis")
	local maxRate = tableParam:GetParam("maxRate")
	if args:GetLuaAttackTarget() == nil or args.attack_damage_result_ == nil then
		return
	end
	
	local damageResult = args.attack_damage_result_
	local damageType = damageResult:GetSkillDamageType()
    if args.current_skill_ == nil then
        return
    end
    local dis = args.current_skill_:GetSkillRangeType()
	local targetDest = args:GetLuaAttackTarget():GetPosition()
    local hosterDest = hoster:GetPosition()
	local distance = ROGameLibs.Vector3:Distance(targetDest, hosterDest)
	
	local distanceRate = distance/maxDis
	local increaseResult = math.min(distanceRate*maxRate,maxRate)/10000
	
	if args.attack_damage_result_:GetResult() > 0 and (damageType == ROGameLibs.kEffectDamSkillType_Physical or damageType == ROGameLibs.kEffectDamSkillType_Common) and dis == 1 then
		damageResult:SetResult(args.attack_damage_result_:GetResult() * (1+increaseResult))
	end
end

----贝鲁兹4007根据技能等级加对应等级Buff
function BuffTriggerResult.Belluz4007(utilTool, firer, hoster, tableParam, args)
    --local SkillID = math.floor(args.current_skill_effect_id_ - (100*args.current_skill_id_))
    local entity = args:GetLuaAttackTarget()
    local BuffLv = 1
    local BuffID = 67644*100 + BuffLv 
    utilTool:LuaAddBuff(hoster, entity, BuffID, true)
end

----贝鲁兹1004
function BuffTriggerResult.Belluz1004(utilTool, firer, hoster, tableParam, args)
    local damgeDown = tableParam:GetParam("damgeDown") / 10000 
	local currentState = hoster:LuaGetCurrentState()
	if args.behit_damage_result_ ~= nil then
		if args.behit_damage_result_:GetResult() <= 0 then
			return
		elseif currentState == 1 then
			local damageResult = args.behit_damage_result_
			local damage = damageResult:GetResult()
			damageResult:SetResult(damage*(1-damgeDown))
		end
    end
end

--贝鲁兹3006
function BuffTriggerResult.Belluz3006(utilTool, firer, hoster, tableParam, args)
	local checkBuff = tableParam:GetParam("checkBuff")
	local originBuff = tableParam:GetParam("originBuff")
	local newBuff = tableParam:GetParam("newBuff")
	
	local owner = utilTool:LuaGetMobMonsterOwner(firer, true)
	local buff = owner:GetBuff(checkBuff)
	
	if buff ~= nil then
		utilTool:LuaAddBuff(hoster, hoster, newBuff, true)
	else
		utilTool:LuaAddBuff(hoster, hoster, originBuff, true)
	end
end

--贝鲁兹4014
function BuffTriggerResult.Belluz4014(utilTool, firer, hoster, tableParam, args)
    if firer == nil then
		return
	end
	local belluz4014Buff = firer:GetBuff(65015)
	
	if belluz4014Buff == nil then
		return
	end
	
	local belluz4014Lv = belluz4014Buff:GetBuffLevel()
	local belluz4014ID = 65015* 100 + belluz4014Lv
	local belluz4014ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4014ID,false)
	local belluz4014SpCure = belluz4014ConfigData:GetParam("spCure")/10000
    
    local curSP = hoster:GetAttr(AttrType.ATTR_BASIC_SP)
    local maxSP = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_SP_FINAL)
	local addSP = maxSP * belluz4014SpCure
	
    hoster:LuaIncAttr(AttrType.ATTR_BASIC_SP,addSP,hoster,true)
    
    if curSP/maxSP < 1 and addSP ~= 0 then
        utilTool:LuaShowDamageResult(hoster,hoster,ROGameLibs.DAMAGE_TYPE_BUFF_SP,addSP)
    end
end

--贝鲁兹3019
function BuffTriggerResult.Belluz3019(utilTool, firer, hoster, tableParam, args)
    local belluz3019Buff = hoster:GetBuff(67628)
	
	if belluz3019Buff == nil then
		return
	end
	
	local belluz3019Lv = belluz3019Buff:GetBuffLevel()
	local belluz3019ID = 67628* 100 + belluz3019Lv
	local belluz3019ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3019ID,false)
	local belluz3019RateHp = belluz3019ConfigData:GetParam("rateHp")
	local belluz3019RateSp = belluz3019ConfigData:GetParam("rateSp")
	
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP,belluz3019RateHp,hoster,true)
    hoster:LuaIncAttr(AttrType.ATTR_PERCENT_SP,belluz3019RateSp,hoster,true)
    
end

--乌勒尔的决意：加buff用
function BuffTriggerResult.EquipWLEDJYAddBuff(utilTool, firer, hoster, tableParam, args)
	if firer ~= nil then
		local maxCount = firer:GetFightTempAttr("EQUIP_WLEDJY_MAXCOUNT")
		if maxCount == 3 then
			utilTool:LuaAddBuff(firer, hoster, 410104001, true)
		elseif maxCount == 5 then
			utilTool:LuaAddBuff(firer, hoster, 410105001, true)
		end
	end
end

--梦罗克幼体叠层流血
function BuffTriggerResult.DieCengLiuXue(utilTool, firer, hoster, tableParam, args)
    local damagePercent = tableParam:GetParam("per")
    local append = 0
    if hoster:HasBuff(34302) then
        append = hoster:GetBuff(34302):GetAppendCount()
		local damageCal = append*damagePercent
		local damage = hoster:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL) * damageCal / 10000
		hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP, -damageCal, hoster, true)
		utilTool:LuaShowDamageResult(hoster, hoster, ROGameLibs.DAMAGE_TYPE_COMMON, damage)
    end
    
end

--骑兵修炼删移速检测
function BuffTriggerResult.PecoSpeedCheck(utilTool, firer, hoster, tableParam, args)
    
    local skillId = tableParam:GetParam("skillID")
    local buffID = tableParam:GetParam("BuffID")
    if args.trigger_timing_ ~= ROGameLibs.kBuffTTT_PassiveSkillLevelChange then
        return
    end
    if skillId ~= args.current_skill_id_ then
        return
    end
    
    local skillLevNow = args.current_skill_level_

    if skillLevNow  == 0 then
        utilTool:LuaDelBuff(hoster, buffID, true)
    end

end

--贝鲁兹7014
function BuffTriggerResult.Belluz7014(utilTool, firer, hoster, tableParam, args) 
    local maxCount = tableParam:GetParam("maxCount") - 1
	local buff = hoster:GetBuff(60068)
	local belluzBuff = hoster:GetBuff(60067)
	if belluzBuff ~= nil then
		local belluzBuffLv = belluzBuff:GetBuffLevel()
		local buffID = 6006800 + belluzBuffLv
		if buff == nil then
			utilTool:LuaAddBuff(hoster, hoster, buffID, true)
			return
		else
			local buffCount = buff:GetAppendCount()
			utilTool:LuaAddBuff(hoster, hoster, buffID, true)
			if buffCount >= maxCount then
				local increaseBuffID = 6006900 + belluzBuffLv
				utilTool:LuaAddBuff(hoster, hoster, increaseBuffID, true)
				utilTool:LuaDelBuff(hoster, 60068, true)
			end
		end
    end
end

--贝鲁兹7015
function BuffTriggerResult.Belluz7015(utilTool, firer, hoster, tableParam, args) 
	if firer == nil then
		return
	end
	local buff = firer:GetBuff(60072)
	if buff ~= nil then
		local belluzBuffLv = buff:GetBuffLevel()
		local buffID = 6007300 + belluzBuffLv
		utilTool:LuaAddBuff(hoster, hoster, buffID, true)
    end
end


--集中攻击检测霸体等级
function BuffTriggerResult.BaTiCheck(utilTool, firer, hoster, tableParam, args)
    local skillID = tableParam:GetParam("skillID")

    if hoster:GetSkillInfo(skillID) ~= nil then
        local originSkillLevel = hoster:GetSkillInfo(skillID):Level()
        local baTiBuffID = 102 * 100 + originSkillLevel
        utilTool:LuaAddBuff(hoster, hoster, baTiBuffID, true)
    end
    
end

--第二次封印：陨石杀死召唤者
function BuffTriggerResult.YunShiKillOwner(utilTool, firer, hoster, tableParam, args)
    local owner = utilTool:LuaGetMobMonsterOwner(hoster, true)
    if owner ~= nil then
        utilTool:LuaAddBuff(hoster, owner, 3270501, true)
    end
end

--突击炮受到boss和mvp直接或召唤兽伤害立即死亡
function BuffTriggerResult.CannonBehit(utilTool, firer, hoster, tableParam, args)
    local curHP = hoster:GetAttr(AttrType.ATTR_BASIC_HP)
    local damageResult = args.behit_damage_result_
    local entity = args:GetLuaFirerEntity()
    
    if damageResult == nil or entity == nil then
        return
    end
	
-- 判断是否为MVP、BOSS或者为其召唤兽
	local entityType = entity:GetUnitTypeLevel()
		
	if entityType == ROGameLibs.kUnitTypeLevel_BOSS or entityType == ROGameLibs.kUnitTypeLevel_MVP then
		damageResult:SetResult(curHP)
		-- hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP,-10000,hoster,true)
		-- utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_UNKNOW,curHP)
	else
		local entityOwner = utilTool:LuaGetMobMonsterOwner(entity, true)
		local entityOwnerType = entityOwner:GetUnitTypeLevel()
		if entityOwnerType == ROGameLibs.kUnitTypeLevel_BOSS or entityOwnerType == ROGameLibs.kUnitTypeLevel_MVP then
			damageResult:SetResult(curHP)
			-- hoster:LuaIncAttr(AttrType.ATTR_PERCENT_HP,-10000,hoster,true)
			-- utilTool:LuaShowDamageResult(hoster,entity,ROGameLibs.DAMAGE_TYPE_UNKNOW,curHP)
		end
	end
end

--毕尤护盾前检测
function BuffTriggerResult.FrontDefense(utilTool, firer, hoster, tableParam, args)
		local entity = args:GetLuaFirerEntity()
		
		if entity == nil then
			return
		end
		
		if firer == nil then
			return
		end

		if args.behit_damage_result_ == nil then
			return
		end

		local tar_pos = hoster:GetPosition()
		local tar_face = hoster:GetFace()
		tar_face:HorizontalRotate(0, false) --正前方
		local cas_pos = entity:GetPosition()
		local result = utilTool:IsFrontOfTargetWithAngle(tar_pos, tar_face, cas_pos, 150)
		local damageResult = args.behit_damage_result_
		local damageNum = damageResult:GetResult()
		if damageNum >=1 and result == true then
			damageResult:SetResult(0)
		end
end
		
--毕尤护盾右检测
function BuffTriggerResult.RightDefense(utilTool, firer, hoster, tableParam, args)
		local entity = args:GetLuaFirerEntity()
		
		if entity == nil then
			return
		end
		
		if firer == nil then
			return
		end

		if args.behit_damage_result_ == nil then
			return
		end

		local tar_pos = hoster:GetPosition()
		local tar_face = hoster:GetFace()
		tar_face:HorizontalRotate(270, false) --右侧
		local cas_pos = entity:GetPosition()
		local result = utilTool:IsFrontOfTargetWithAngle(tar_pos, tar_face, cas_pos, 150)
		local damageResult = args.behit_damage_result_
		local damageNum = damageResult:GetResult()
		if damageNum >=1 and result == true then
			damageResult:SetResult(0)
		end
end
		
--毕尤护盾后检测
function BuffTriggerResult.BehindDefense(utilTool, firer, hoster, tableParam, args)
		local entity = args:GetLuaFirerEntity()
		
		if entity == nil then
			return
		end
		
		if firer == nil then
			return
		end

		if args.behit_damage_result_ == nil then
			return
		end

		local tar_pos = hoster:GetPosition()
		local tar_face = hoster:GetFace()
		tar_face:HorizontalRotate(180, false) --正后方
		local cas_pos = entity:GetPosition()
		local result = utilTool:IsFrontOfTargetWithAngle(tar_pos, tar_face, cas_pos, 150)
		local damageResult = args.behit_damage_result_
		local damageNum = damageResult:GetResult()
		if damageNum >=1 and result == true then
			damageResult:SetResult(0)
		end
end

--毕尤护盾左检测
function BuffTriggerResult.LeftDefense(utilTool, firer, hoster, tableParam, args)
		local entity = args:GetLuaFirerEntity()
		
		if entity == nil then
			return
		end
		
		if firer == nil then
			return
		end

		if args.behit_damage_result_ == nil then
			return
		end

		local tar_pos = hoster:GetPosition()
		local tar_face = hoster:GetFace()
		tar_face:HorizontalRotate(90, false) --左侧
		local cas_pos = entity:GetPosition()
		local result = utilTool:IsFrontOfTargetWithAngle(tar_pos, tar_face, cas_pos, 150)
		local damageResult = args.behit_damage_result_
		local damageNum = damageResult:GetResult()
		if damageNum >=1 and result == true then
			damageResult:SetResult(0)
		end
end


----伤害改变（目前只有卡片用）
function BuffTriggerResult.DamageDown(utilTool, firer, hoster, tableParam, args)
    local rate = 1 - tableParam:GetParam("rate")/10000
	if args.behit_damage_result_ == nil then
        return
	elseif args.behit_damage_result_:GetResult() <= 0 then
		return
    end
    local damageResult = args.behit_damage_result_
    damageResult:SetResult(args.behit_damage_result_:GetResult() * rate)  
end

