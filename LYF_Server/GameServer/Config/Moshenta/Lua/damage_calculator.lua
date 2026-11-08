declareGlobal("DamageCalculator", {})
require("skill.skill_trigger_moudle")
-- BUFF_SNAPSHOT_ATTR_TYPE_LEVEL 等级
-- BUFF_SNAPSHOT_ATTR_TYPE_APPEND_COUNT 层数
--[[
    enum DAMAGE_TYPE
    {
        DAMAGE_TYPE_UNKNOW = 0, //未知
        DAMAGE_TYPE_COMMON = 1, //普通
        DAMAGE_TYPE_CRI = 2, //暴击
        DAMAGE_TYPE_MISS = 3, //miss
        DAMAGE_TYPE_IMMUNE = 4, //免疫
    };

    firer: 攻击者
    target: 受击者
    damage_info:表格参数  例: 函数名|参数名=value damage_info:GetParam("参数名")
        int GetParam(string) // 获取表格中参数
    args:程序传参
        int result_index_  //技能打击点index, 从0开始
        int target_index_  //当前目标是这次打击第几个目标, 从0开始
    damage_result: 伤害计算结果
        DAMAGE_TYPE GetType() //伤害类型
        void SetType(int)     //设置伤害类型, 看上面枚举
        int GetResult()       //伤害数值
        SetResult(int)        //设置伤害数值, 为负数则是加血
]]
--[[
//临时战斗属性相关
        void SetFightTempAttr(std::string& key, int value);
        void IncrFightTempAttr(std::string& key, int value);
        int GetFightTempAttr(std::string& key);
]]
--元素克制属性修正(1到9依次对应风，地，火，水，毒，圣，暗，念，不死，强制无属性, 横向也是一样)
    DamageCalculator.element_dam = {
        [0]  ={[0]=1    ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=1    ,[6]=1    ,[7]=1    ,[8]=0.7  ,[9]=1    ,[10]=1   },
        [1]  ={[0]=1    ,[1]=0.25 ,[2]=0.8  ,[3]=1    ,[4]=1.75 ,[5]=1    ,[6]=0.75 ,[7]=1    ,[8]=1    ,[9]=1    ,[10]=1   },
        [2]  ={[0]=1    ,[1]=1.75 ,[2]=0.25 ,[3]=0.8  ,[4]=1    ,[5]=1    ,[6]=0.75 ,[7]=1    ,[8]=1    ,[9]=1    ,[10]=1   },
        [3]  ={[0]=1    ,[1]=1    ,[2]=1.75 ,[3]=0.25 ,[4]=0.8  ,[5]=1    ,[6]=0.75 ,[7]=1    ,[8]=1    ,[9]=1.5  ,[10]=1   },
        [4]  ={[0]=1    ,[1]=0.9  ,[2]=1    ,[3]=1.75 ,[4]=0.25 ,[5]=1    ,[6]=0.75 ,[7]=1    ,[8]=1    ,[9]=1    ,[10]=1   },
        [5]  ={[0]=1    ,[1]=1.25 ,[2]=1.25 ,[3]=1.25 ,[4]=1.25 ,[5]=0.25 ,[6]=0.5  ,[7]=0.5  ,[8]=1    ,[9]=0.25 ,[10]=1   },
        [6]  ={[0]=1    ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=1    ,[6]=0.25 ,[7]=1.50 ,[8]=1    ,[9]=1.75 ,[10]=1   },
        [7]  ={[0]=1    ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=0.5  ,[6]=1.5  ,[7]=0.25 ,[8]=1    ,[9]=0.25 ,[10]=1   },
        [8]  ={[0]=0.7  ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=1    ,[6]=0.75 ,[7]=0.75 ,[8]=1.5  ,[9]=1.25 ,[10]=1   },
        [9]  ={[0]=1    ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=0.5  ,[6]=1.25 ,[7]=0.25 ,[8]=1    ,[9]=0.25 ,[10]=1   },
        [10] ={[0]=1    ,[1]=1    ,[2]=1    ,[3]=1    ,[4]=1    ,[5]=1    ,[6]=1    ,[7]=1    ,[8]=1    ,[9]=1    ,[10]=1   },

    }

--体型修正(1短剑，2单手剑，3双手剑，4单手矛，5双手矛，6单手斧，7双手斧，8钝器，9单手仗，10双手仗，11弓，12拳套，13乐器，14鞭子，15书，16拳刃，17盾牌)
--2,0,1分别对应小，中，大体型；
    DamageCalculator.body_dam = {
        [0]=    {[2]=1    ,[0]=1    ,[1]=1    },
        [1]=    {[2]=1    ,[0]=0.75 ,[1]=0.5  },
        [2]=    {[2]=0.75 ,[0]=1    ,[1]=0.75 },
        [3]=    {[2]=0.75 ,[0]=0.75 ,[1]=1    },
        [4]=    {[2]=0.75 ,[0]=0.75 ,[1]=1    },
        [5]=    {[2]=0.75 ,[0]=0.75 ,[1]=1    },
        [6]=    {[2]=0.5  ,[0]=0.75 ,[1]=1    },
        [7]=    {[2]=0.5  ,[0]=0.75 ,[1]=1    },
        [8]=    {[2]=0.75 ,[0]=1    ,[1]=1    },
        [9]=    {[2]=1    ,[0]=1    ,[1]=1    },
        [10]=   {[2]=1    ,[0]=1    ,[1]=1    },
        [11]=   {[2]=1    ,[0]=1    ,[1]=0.75 },
        [12]=   {[2]=1    ,[0]=0.75 ,[1]=0.5  },
        [13]=   {[2]=0.75 ,[0]=1    ,[1]=0.75 },
        [14]=   {[2]=0.75 ,[0]=1    ,[1]=0.75 },
        [15]=   {[2]=1    ,[0]=1    ,[1]=0.5  },
        [16]=   {[2]=0.75 ,[0]=1    ,[1]=0.75 },
        [17]=   {[2]=0.75 ,[0]=0.75 ,[1]=1    },
		[101]=   {[2]=1    ,[0]=0.75 ,[1]=0.5    },
		[102]=   {[2]=0.75 ,[0]=1    ,[1]=0.75   },
		[103]=   {[2]=0.75 ,[0]=0.75 ,[1]=0.5    },
		[104]=   {[2]=0.75 ,[0]=0.75 ,[1]=0.5    },
    }
    
    --种族修正，0到9对应动物，人形，恶魔，植物，不死，无形，鱼贝，天使，昆虫，龙族，机械
    DamageCalculator.race_dam = {
        [0] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [1] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [2] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [3] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [4] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [5] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [6] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [7] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [8] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
        [9] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=0},
		[10] 	={[0]=1,[1]=1,[2]=1,[3]=1,[4]=1,[5]=1,[6]=1,[7]=1,[8]=1,[9]=1,[10]=1},
    }
    
--Other技能：伤害计算公式流程Base, 类型为 ROGameLibs.DAMAGE_TYPE_COMMON
function DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,damage_result_type)
    --Skill Apply
    SkillEffectIDMoudle.OnSkillAttackBeforeCalculate(caster, caster_snapshot, target, args.skill_:SkillID(), args.skill_:SkillTableUUID(), args.skill_:SkillEffectID(), damage_result, args)
    --Buff Apply
    ROGameLibs.DamageCalculatorUtil:ApplyBuffEffectBeforeCalculate(caster, caster_snapshot, target, args, damage_result)
    damage_result:SetType(damage_result_type)
    args:CalculatorProcess(caster_snapshot, caster, target, damage_result, nil);
	--伤害被格挡了
	local IsNotBlock = DamageCalculator.IsDamageNotBlock( caster, caster_snapshot,target, args, damage_result )
	if IsNotBlock == false then
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_BLOCK)
		return false
	end
	return true
end

--不再判定格挡的基础伤害流程base,用于普攻和物理伤害计算
function DamageCalculator.DamageCalcBaseNoBlock(caster, caster_snapshot, target, args,damage_result,damage_result_type)
    --Skill Apply
    SkillEffectIDMoudle.OnSkillAttackBeforeCalculate(caster, caster_snapshot, target, args.skill_:SkillID(), args.skill_:SkillTableUUID(), args.skill_:SkillEffectID(), damage_result, args)
    --Buff Apply
    ROGameLibs.DamageCalculatorUtil:ApplyBuffEffectBeforeCalculate(caster, caster_snapshot, target, args, damage_result)
    damage_result:SetType(damage_result_type)
    args:CalculatorProcess(caster_snapshot, caster, target, damage_result, nil);
end

--物理伤害
function DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local skill = args.skill_;
    if caster == nil or target == nil or skill == nil then
        return false
    end
	local IsNotBlock = DamageCalculator.IsDamageNotBlock( caster, caster_snapshot,target, args, damage_result )

    --Skill Trigger
    SkillEffectIDMoudle.OnSkillAttackBeforeJudge(caster, caster_snapshot, target, args.skill_:SkillID(), args.skill_:SkillTableUUID(), args.skill_:SkillEffectID(), damage_result, args)
    --Buff Apply
    ROGameLibs.DamageCalculatorUtil:ApplyBuffEffectBeforeJudge(caster, caster_snapshot, target, args, damage_result)
	--判断miss，之后造成伤害
	if false == DamageCalculator.IsPhysicalSkillNotMiss(utilTool, caster,caster_snapshot, target, damage_info, args) then
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_MISS)
		return false
	--判断格挡，之后造成伤害
	elseif IsNotBlock == false then
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_BLOCK)
		return false
	end
	
	DamageCalculator.DamageCalcBaseNoBlock(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
	return true		
end

--普攻伤害流程
function DamageCalculator.CalculateCommonSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local skill = args.skill_;

    if caster == nil or target == nil or skill == nil then
        return false
    end
	local IsNotBlock = DamageCalculator.IsDamageNotBlock( caster, caster_snapshot,target, args, damage_result )
    --Skill Trigger
    SkillEffectIDMoudle.OnSkillAttackBeforeJudge(caster, caster_snapshot, target, args.skill_:SkillID(), args.skill_:SkillTableUUID(), args.skill_:SkillEffectID(), damage_result, args)
    --Buff Apply
    ROGameLibs.DamageCalculatorUtil:ApplyBuffEffectBeforeJudge(caster, caster_snapshot, target, args, damage_result)
    
    --暴击判定
	if DamageCalculator.IsCommonSkillCrit(utilTool, caster, caster_snapshot, target, damage_info, args) and IsNotBlock then
		DamageCalculator.DamageCalcBaseNoBlock(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_CRI)
		return true
	--判断miss，之后造成伤害
	elseif false == DamageCalculator.IsCommonSkillNotMiss(utilTool, caster,caster_snapshot, target, damage_info, args) then
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_MISS)
		return false
	--判断格挡，之后造成伤害
	elseif IsNotBlock == false then
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_BLOCK)
		return false
	end

	DamageCalculator.DamageCalcBaseNoBlock(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
	return true
end

--暴击流程
function DamageCalculator.CalculateCriSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local skill = args.skill_;
    if caster == nil or target == nil or skill == nil then
        return false
    end
    --Skill Trigger
    SkillEffectIDMoudle.OnSkillAttackBeforeJudge(caster, caster_snapshot, target, args.skill_:SkillID(), args.skill_:SkillTableUUID(), args.skill_:SkillEffectID(), damage_result, args)
    --Buff Apply
    ROGameLibs.DamageCalculatorUtil:ApplyBuffEffectBeforeJudge(caster, caster_snapshot, target, args, damage_result)
    
    --暴击判定
    if DamageCalculator.IsCommonSkillCrit(utilTool, caster, caster_snapshot, target, damage_info, args) then
        DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_CRI)
        return true
    end 
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
end
  
--普通攻击是否暴击，暴击返回true
function DamageCalculator.IsCommonSkillCrit(utilTool, caster, caster_snapshot,target, damage_info, args)
	args:CalculatorSkillCrit(caster_snapshot, target)
	
--取值
    --玩家
    -- local pla = {
                    -- cri             = args:GetCasterAttr(AttrType.ATTR_BASIC_CRI),
    -- }
    --目标
    -- local tar = {
                    -- cri_res         = args:GetTargetAttr(AttrType.ATTR_BASIC_CRI_RES),
    -- }
	local Id = args.skill_:SkillEffectID()
    local cri_final=(args.skill_crit_attr_.caster_cri - args.skill_crit_attr_.target_cri_res)/100
	
    --目标处于睡眠状态则进行的第一次物理攻击，暴击值翻倍；
    if	target:CheckBuffState(ROGameLibs.kBuffStateType_Sleep)       == true then
		cri_final = (args.skill_crit_attr_.caster_cri - args.skill_crit_attr_.target_cri_res)/100*2
	elseif  71020300 < Id and Id < 71020311 then	--锐利射击
		cri_final = (args.skill_crit_attr_.caster_cri - args.skill_crit_attr_.target_cri_res+20)/100
	elseif  31011400 < Id and Id < 31011411 or 31011600 < Id and Id < 31011611 then	--圣之裁决
		if caster:HasBuff(65025) == true then --贝鲁兹 裁决增幅 效果
			local belluz4022Buff = caster:GetBuff(65025)
			local belluz4022Lv = belluz4022Buff:GetBuffLevel()
			local belluz4022ID = 65025 * 100 + belluz4022Lv
			local belluz4022ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4022ID,false)
			local belluz4022Rate = belluz4022ConfigData:GetParam("rate")/10000
			cri_final = (args.skill_crit_attr_.caster_cri - args.skill_crit_attr_.target_cri_res)/100 * belluz4022Rate
	    else
			cri_final = 0
        end
    end
	-- print("cri_final"..cri_final)
    -- utilTool:LuaLog("hoster cri_final:"..tonumber(cri_final))
    ---------这里要注意拳刃的判断条件，后续补上；
	   cri_final = math.max(math.min(cri_final,1),0)
    --RORandom:randDouble(0,1)  程序提供的随机数接口
    return  RORandom:randDouble(0,1) <= cri_final 	
end

--DamageCalculator.common_skill_not_miss = true
--普通攻击是否命中，命中返回true
function DamageCalculator.IsCommonSkillNotMiss(utilTool, caster,caster_snapshot, target, damage_info, args)
	args:CalculatorSkillNotMissAttr(caster_snapshot, target);
--取值
    --玩家
    -- local pla = {
                    -- hit_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_HIT),                     
    -- }
    --目标
    -- local tar = {
                    -- flee_base       = args:GetTargetAttr(AttrType.ATTR_BASIC_FLEE),                   
    -- }	
    local Flee_Num 	  = target:GetAttackerCount()  --围攻数量
    local hit_base = args.skill_not_miss_attr_.caster_hit_base
    local flee_base = args.skill_not_miss_attr_.target_flee_base
		  Flee_Num    = math.max(math.min(Flee_Num,7),2)
    local hit_final   =(hit_base - flee_base* (1-(Flee_Num-2)*0.3))/100

		  
	--PVP中处理闪避命中
	
	if caster:InPvPScene() == true then
		local hit_dex = caster:GetAttr(AttrType.ATTR_BASIC_DEX)
		local hit_temp = hit_base*1.05-flee_base+hit_dex*0.5
		if hit_temp > -100 and hit_temp < 0 then
			hit_final= ((hit_temp+100)/100*5+15)/100
		elseif hit_temp >=0 and hit_temp < 100 then
			hit_final= ((hit_temp)/100*80+20)/100
		end
	end 
		  hit_final   = math.max(math.min(hit_final,1),0.05)


    --RORandom:randDouble(0,1)  程序提供的随机数接口
	-- print("hit_final"..hit_final)
    --判定敌方在睡眠和麻痹眩晕状态下必定命中
    if  RORandom:randDouble(0,1) <= hit_final                               or 
        target:CheckBuffState(ROGameLibs.kBuffStateType_Sleep)      == true or 
        target:CheckBuffState(ROGameLibs.kBuffStateType_Paralysis)  == true or
        target:CheckBuffState(ROGameLibs.kBuffStateType_Stun)       == true then
        return true
    else
        return false
    end
end

--物理技能是否命中，命中返回true
function DamageCalculator.IsPhysicalSkillNotMiss(utilTool, caster, caster_snapshot,target, damage_info, args)
	args:CalculatorSkillNotMissAttr(caster_snapshot, target);
--取值

    local Id = args.skill_:SkillEffectID()
    local hit_rate = 0
    --玩家
    -- local pla = {
                    -- hit_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_HIT),        
    -- }
    --目标
    -- local tar = {
                    -- flee_base       = args:GetTargetAttr(AttrType.ATTR_BASIC_FLEE),                     
    -- }
    local hit_base = args.skill_not_miss_attr_.caster_hit_base
	local hit_dex = caster:GetAttr(AttrType.ATTR_BASIC_DEX)
	--PVP中的命中补正
		if caster:InPvPScene() == true then
			hit_base = hit_base*1.05+hit_dex*0.5
		end
    local flee_base = args.skill_not_miss_attr_.target_flee_base
		
    local Flee_Num 	  = target:GetAttackerCount() --围攻数量
		  Flee_Num    = math.max(math.min(Flee_Num,7),2)
    local hit_final   =(hit_base-flee_base*(1-(Flee_Num-2)*0.3))/100
	
    --狂击和怒爆的命中加成
    if     Id  == 20000401 then hit_final   =   ((hit_base+3)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000402 then hit_final   =   ((hit_base+3)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000403 then hit_final   =   ((hit_base+5)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000404 then hit_final   =   ((hit_base+5)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000405 then hit_final   =   ((hit_base+8)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000406 then hit_final   =   ((hit_base+8)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000407 then hit_final   =   ((hit_base+10)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000408 then hit_final   =   ((hit_base+10)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000409 then hit_final   =   ((hit_base+15)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000410 then hit_final   =   ((hit_base+15)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000501 then hit_final   =   ((hit_base+5)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000502 then hit_final   =   ((hit_base+8)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000503 then hit_final   =   ((hit_base+10)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000504 then hit_final   =   ((hit_base+13)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000505 then hit_final   =   ((hit_base+15)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000506 then hit_final   =   ((hit_base+18)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000507 then hit_final   =   ((hit_base+20)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000508 then hit_final   =   ((hit_base+23)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000509 then hit_final   =   ((hit_base+25)-flee_base*(1-(Flee_Num-2)*0.3))/100
    elseif Id  == 20000510 then hit_final   =   ((hit_base+28)-flee_base*(1-(Flee_Num-2)*0.3))/100
    else  hit_final   =((hit_base)-flee_base*(1-(Flee_Num-2)*0.3))/100
    end
	--音速投掷加速带来50%命中加成
	if 51010201 <= Id and Id <= 51010210 and caster:GetSkillInfo(510104) ~= nil then
		hit_rate = caster:GetSkillInfo(510104):GetParam("HitRate")/10000 
		hit_final = ((hit_base)-flee_base*(1-(Flee_Num-2)*0.3))/100*(1+hit_rate/10000)
	end
	--处理PVP命中
	if caster:InPvPScene() == true then
		if  hit_final < 0 then
			hit_final= ((hit_final*100+100)/100*5+15)/100
		elseif hit_final >=0 and hit_final < 1 then
			hit_final= ((hit_final*100)/100*80+20)/100
		end
	end 
		hit_final   = math.max(math.min(hit_final,1),0.05)
    --RORandom:randDouble(0,1)  程序提供的随机数接口
    --判定敌方在睡眠和麻痹眩晕状态下必定命中,伏击（已废弃）必定命中、闪电冲击、心灵震波必定命中
    if  RORandom:randDouble(0,1) <= hit_final   or  (50001200 < Id and  Id< 50001211)   or
		Id== 71010101	or	Id==71012601	or  (51020400 < Id and  Id< 51020411)		or
        target:CheckBuffState(ROGameLibs.kBuffStateType_Sleep)     == true              or
        target:CheckBuffState(ROGameLibs.kBuffStateType_Paralysis) == true              or
        target:CheckBuffState(ROGameLibs.kBuffStateType_Stun)      == true              then
        return true
    else
        return false
    end
end

--伤害是否被格挡
function DamageCalculator.IsDamageNotBlock( caster, caster_snapshot,target, args, damage_result )
	--双剑格挡
	local BlockPro = target:GetFightTempAttr("SKILL_SJGD_BLOCK")/10000
	local DamageType = damage_result:GetSkillDamageType()
	if RORandom:randDouble(0,1) <= BlockPro and (DamageType == 0 or DamageType == 1)then
		return false
	end
	return true
end


--属性和伤害计算过程
function DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
--取值
--[[
    --玩家
    -- local pla = {
            --取基础属性
            -- job             = args:GetCasterAttr(AttrType.ATTR_SPECIAL_JOB),
            -- base_lv         = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV),
            -- size            = args:GetCasterAttr(AttrType.ATTR_SPECIAL_SIZE),
            -- race            = args:GetCasterAttr(AttrType.ATTR_SPECIAL_RACE),
            -- str_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_STR),
            -- str_equip       = args:GetCasterAttr(AttrType.ATTR_BASIC_STR_EQUIP),
            -- int_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_INT),
            -- int_equip       = args:GetCasterAttr(AttrType.ATTR_BASIC_INT_EQUIP),
            -- dex_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_DEX),
            -- dex_equip       = args:GetCasterAttr(AttrType.ATTR_BASIC_DEX_EQUIP),
            -- luk_base        = args:GetCasterAttr(AttrType.ATTR_BASIC_LUK),
            -- luk_equip       = args:GetCasterAttr(AttrType.ATTR_BASIC_LUK_EQUIP),
            -- atk             = args:GetCasterAttr(AttrType.ATTR_BASIC_BASE_ATK_FINAL),
            -- matk            = args:GetCasterAttr(AttrType.ATTR_BASIC_BASE_MATK_FINAL),
            -- cur_hp          = args:GetCasterAttr(AttrType.ATTR_BASIC_HP),
            -- max_hp          = args:GetCasterAttr(AttrType.ATTR_BASIC_MAX_HP),
                                
            --取装备属性
            -- weapon_atk      = args:GetCasterAttr(AttrType.ATTR_BASIC_WEAPON_ATK_FINAL),
            -- weapon_matk     = args:GetCasterAttr(AttrType.ATTR_BASIC_WEAPON_MATK_FINAL),
			-- weapon_atk_per  = args:GetCasterAttr(AttrType.ATTR_PERCENT_WEAPON_ATK),
			-- weapon_matk_per = args:GetCasterAttr(AttrType.ATTR_PERCENT_WEAPON_MATK),
            -- jinglian_atk    = args:GetCasterAttr(AttrType.ATTR_BASIC_REFINE),
            -- jinglian_matk   = args:GetCasterAttr(AttrType.ATTR_BASIC_MREFINE),
            -- skill_atk       = args:GetCasterAttr(AttrType.ATTR_BASIC_SKILL_ATK),
            -- skill_atk_per   = args:GetCasterAttr(AttrType.ATTR_PERCENT_SKILL_ATK),
            -- skill_matk      = args:GetCasterAttr(AttrType.ATTR_BASIC_SKILL_MATK),
            -- skill_matk_per  = args:GetCasterAttr(AttrType.ATTR_PERCENT_SKILL_MATK),
            -- ignore_def      = args:GetCasterAttr(AttrType.ATTR_BASIC_IGNORE_DEF),
            -- ignore_def_per  = args:GetCasterAttr(AttrType.ATTR_PERCENT_IGNORE_DEF),
            -- ignore_mdef     = args:GetCasterAttr(AttrType.ATTR_BASIC_IGNORE_MDEF),
            -- ignore_mdef_per = args:GetCasterAttr(AttrType.ATTR_PERCENT_IGNORE_MDEF),
			-- ignore_def_shei = args:GetCasterAttr(AttrType.ATTR_BASIC_SHIELD_DEF_IGNORE),
            -- atk_up          = args:GetCasterAttr(AttrType.ATTR_PERCENT_DAM_INCREASE),
            -- matk_up         = args:GetCasterAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE),

            -- atk_type        = args.attack_type_,
			-- dam_float	   = args:GetCasterAttr(AttrType.ATTR_BASIC_FLOATING_DAMAGE_DOWN),
			-- dam_float_per   = args:GetCasterAttr(AttrType.ATTR_PERCENT_FLOATING_DAMAGE_DOWN),
            -- small_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_SMALL_DAM),
            -- mid_dam         = args:GetCasterAttr(AttrType.ATTR_PERCENT_MID_DAM),
            -- big_dam         = args:GetCasterAttr(AttrType.ATTR_PERCENT_BIG_DAM),
            -- boss_dam        = args:GetCasterAttr(AttrType.ATTR_PERCENT_BOSS_DAM),
            
            -- brute		   = args:GetCasterAttr(AttrType.ATTR_BASIC_BRUTE_DAM),
			-- brute_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_BRUTE_DAM),
            -- human       	   = args:GetCasterAttr(AttrType.ATTR_BASIC_DEMI_HUMAN_DAM),
			-- human_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_DEMI_HUMAN_DAM),
            -- demon      	   = args:GetCasterAttr(AttrType.ATTR_BASIC_DEMON_DAM),
			-- demon_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_DEMON_DAM),
            -- plant       	   = args:GetCasterAttr(AttrType.ATTR_BASIC_PLANT_DAM),
			-- plant_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_PLANT_DAM),
            -- undead      	   = args:GetCasterAttr(AttrType.ATTR_BASIC_UNDEAD_DAM),
			-- undead_dam      = args:GetCasterAttr(AttrType.ATTR_PERCENT_UNDEAD_DAM),
            -- formless    	   = args:GetCasterAttr(AttrType.ATTR_BASIC_FORMLESS_DAM),
			-- formless_dam    = args:GetCasterAttr(AttrType.ATTR_PERCENT_FORMLESS_DAM),
            -- fish        	   = args:GetCasterAttr(AttrType.ATTR_BASIC_FISH_DAM),
			-- fish_dam        = args:GetCasterAttr(AttrType.ATTR_PERCENT_FISH_DAM),
            -- angel       	   = args:GetCasterAttr(AttrType.ATTR_BASIC_ANGEL_DAM),
			-- angel_dam       = args:GetCasterAttr(AttrType.ATTR_PERCENT_ANGEL_DAM),
            -- insect      	   = args:GetCasterAttr(AttrType.ATTR_BASIC_INSECT_DAM),
			-- insect_dam      = args:GetCasterAttr(AttrType.ATTR_PERCENT_INSECT_DAM),
            -- dragon      	   = args:GetCasterAttr(AttrType.ATTR_BASIC_DRAGON_DAM),
			-- dragon_dam      = args:GetCasterAttr(AttrType.ATTR_PERCENT_DRAGON_DAM),
            
            -- wind            = args:GetCasterAttr(AttrType.ATTR_BASIC_WIND_SKILL_ATK),  
            -- wind_per        = args:GetCasterAttr(AttrType.ATTR_PERCENT_WIND_SKILL_ATK), 
            -- earth           = args:GetCasterAttr(AttrType.ATTR_BASIC_EARTH_SKILL_ATK),  
            -- earth_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_EARTH_SKILL_ATK), 
            -- fire            = args:GetCasterAttr(AttrType.ATTR_BASIC_FIRE_SKILL_ATK),  
            -- fire_per        = args:GetCasterAttr(AttrType.ATTR_PERCENT_FIRE_SKILL_ATK), 
            -- water           = args:GetCasterAttr(AttrType.ATTR_BASIC_WATER_SKILL_ATK),  
            -- water_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_WATER_SKILL_ATK), 
            -- formless        = args:GetCasterAttr(AttrType.ATTR_BASIC_FORMLESS_SKILL_ATK),  
            -- formless_per    = args:GetCasterAttr(AttrType.ATTR_PERCENT_FORMLESS_SKILL_ATK), 
            -- holy            = args:GetCasterAttr(AttrType.ATTR_BASIC_HOLY_SKILL_ATK),  
            -- holy_per        = args:GetCasterAttr(AttrType.ATTR_PERCENT_HOLY_SKILL_ATK), 
            -- dark            = args:GetCasterAttr(AttrType.ATTR_BASIC_DARK_SKILL_ATK),  
            -- dark_per        = args:GetCasterAttr(AttrType.ATTR_PERCENT_DARK_SKILL_ATK), 
            -- ghoss           = args:GetCasterAttr(AttrType.ATTR_BASIC_GHOSS_SKILL_ATK),  
            -- ghoss_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_GHOSS_SKILL_ATK), 
            -- undead          = args:GetCasterAttr(AttrType.ATTR_BASIC_UNDEAD_SKILL_ATK),  
            -- undead_per      = args:GetCasterAttr(AttrType.ATTR_PERCENT_UNDEAD_SKILL_ATK), 
            -- poison          = args:GetCasterAttr(AttrType.ATTR_BASIC_POISON_SKILL_ATK),  
            -- poison_per      = args:GetCasterAttr(AttrType.ATTR_PERCENT_POISON_SKILL_ATK), 
            
            -- wind_npc        = args:GetCasterAttr(AttrType.ATTR_PERCENT_WIND_NPC_DAM),
            -- earth_npc       = args:GetCasterAttr(AttrType.ATTR_PERCENT_EARTH_NPC_DAM), 
            -- fire_npc        = args:GetCasterAttr(AttrType.ATTR_PERCENT_FIRE_NPC_DAM),
            -- water_npc       = args:GetCasterAttr(AttrType.ATTR_PERCENT_WATER_NPC_DAM), 
            -- formless_npc    = args:GetCasterAttr(AttrType.ATTR_PERCENT_FORMLESS_NPC_DAM), 
            -- holy_npc        = args:GetCasterAttr(AttrType.ATTR_PERCENT_HOLY_NPC_DAM), 
            -- dark_npc        = args:GetCasterAttr(AttrType.ATTR_PERCENT_DARK_NPC_DAM),
            -- ghoss_npc       = args:GetCasterAttr(AttrType.ATTR_PERCENT_GHOSS_NPC_DAM),
            -- undead_npc      = args:GetCasterAttr(AttrType.ATTR_PERCENT_UNDEAD_NPC_DAM), 
            -- poison_npc      = args:GetCasterAttr(AttrType.ATTR_PERCENT_POISON_NPC_DAM), 
    -- }
    --目标
    -- local tar = {
            --取基础属性
            -- job             = args:GetTargetAttr(AttrType.ATTR_SPECIAL_JOB),
            -- base_lv         = args:GetTargetAttr(AttrType.ATTR_SPECIAL_BASE_LV),
            -- size            = args:GetTargetAttr(AttrType.ATTR_SPECIAL_SIZE),
            -- race            = args:GetTargetAttr(AttrType.ATTR_SPECIAL_RACE),
            -- int_base        = args:GetTargetAttr(AttrType.ATTR_BASIC_INT),
            -- int_equip       = args:GetTargetAttr(AttrType.ATTR_BASIC_INT_EQUIP),
            -- vit_base        = args:GetTargetAttr(AttrType.ATTR_BASIC_VIT), 
            -- vit_equip       = args:GetTargetAttr(AttrType.ATTR_BASIC_VIT_EQUIP),
            -- t_def           = args:GetTargetAttr(AttrType.ATTR_BASIC_BASE_DEF),
            -- t_mdef          = args:GetTargetAttr(AttrType.ATTR_BASIC_BASE_MDEF),
    
            --取装备属怿
            -- equip_def       = args:GetTargetAttr(AttrType.ATTR_BASIC_DEF),
            -- equip_def_per   = args:GetTargetAttr(AttrType.ATTR_PERCENT_DEF),
            -- equip_mdef      = args:GetTargetAttr(AttrType.ATTR_BASIC_MDEF), 
            -- equip_mdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_MDEF),
			-- equip_def_shei  = args:GetTargetAttr(AttrType.ATTR_BASIC_SHIELD_DEF),
            -- jinglian_def    = args:GetTargetAttr(AttrType.ATTR_BASIC_REFINE_REDUC),
            -- jinglian_mdef   = args:GetTargetAttr(AttrType.ATTR_BASIC_MREFINE_REDUC),
            -- atk_down        = args:GetTargetAttr(AttrType.ATTR_PERCENT_DAM_REDUC),
            -- matk_down       = args:GetTargetAttr(AttrType.ATTR_PERCENT_MDAM_REDUC),
            
            -- def_type        = args.defense_type_,
    
            -- small_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_SMALL_RES), 
            -- mid_dam         = args:GetTargetAttr(AttrType.ATTR_PERCENT_MID_RES),
            -- big_dam         = args:GetTargetAttr(AttrType.ATTR_PERCENT_BIG_RES),
            -- boss_dam        = args:GetTargetAttr(AttrType.ATTR_PERCENT_BOSS_RES),

            -- hp_max          = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP),
            -- hp_max_per      = args:GetTargetAttr(AttrType.ATTR_PERCENT_MAX_HP),
            
            -- brute_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_BRUTE_RES),
            -- human_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_DEMI_HUMAN_RES),
            -- demon_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_DEMON_RES),
            -- plant_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_PLANT_RES),
            -- undead_dam      = args:GetTargetAttr(AttrType.ATTR_PERCENT_UNDEAD_RES),
            -- formless_dam    = args:GetTargetAttr(AttrType.ATTR_PERCENT_FORMLESS_RES),
            -- fish_dam        = args:GetTargetAttr(AttrType.ATTR_PERCENT_FISH_RES),
            -- angel_dam       = args:GetTargetAttr(AttrType.ATTR_PERCENT_ANGEL_RES),
            -- insect_dam      = args:GetTargetAttr(AttrType.ATTR_PERCENT_INSECT_RES),
            -- dragon_dam      = args:GetTargetAttr(AttrType.ATTR_PERCENT_DRAGON_RES),
			    
            -- wind            = args:GetTargetAttr(AttrType.ATTR_BASIC_WIND_SKILL_DEF),  
            -- wind_per        = args:GetTargetAttr(AttrType.ATTR_PERCENT_WIND_SKILL_DEF),
            -- earth           = args:GetTargetAttr(AttrType.ATTR_BASIC_EARTH_SKILL_DEF),  
            -- earth_per       = args:GetTargetAttr(AttrType.ATTR_PERCENT_EARTH_SKILL_DEF), 
            -- fire            = args:GetTargetAttr(AttrType.ATTR_BASIC_FIRE_SKILL_DEF),  
            -- fire_per        = args:GetTargetAttr(AttrType.ATTR_PERCENT_FIRE_SKILL_DEF), 
            -- water           = args:GetTargetAttr(AttrType.ATTR_BASIC_WATER_SKILL_DEF),  
            -- water_per       = args:GetTargetAttr(AttrType.ATTR_PERCENT_WATER_SKILL_DEF), 
            -- formless        = args:GetTargetAttr(AttrType.ATTR_BASIC_FORMLESS_SKILL_DEF),  
            -- formless_per    = args:GetTargetAttr(AttrType.ATTR_PERCENT_FORMLESS_SKILL_DEF), 
            -- holy            = args:GetTargetAttr(AttrType.ATTR_BASIC_HOLY_SKILL_DEF),  
            -- holy_per        = args:GetTargetAttr(AttrType.ATTR_PERCENT_HOLY_SKILL_DEF), 
            -- dark            = args:GetTargetAttr(AttrType.ATTR_BASIC_DARK_SKILL_DEF),  
            -- dark_per        = args:GetTargetAttr(AttrType.ATTR_PERCENT_DARK_SKILL_DEF), 
            -- ghoss           = args:GetTargetAttr(AttrType.ATTR_BASIC_GHOSS_SKILL_DEF),  
            -- ghoss_per       = args:GetTargetAttr(AttrType.ATTR_PERCENT_GHOSS_SKILL_DEF), 
            -- undead          = args:GetTargetAttr(AttrType.ATTR_BASIC_UNDEAD_SKILL_DEF),  
            -- undead_per      = args:GetTargetAttr(AttrType.ATTR_PERCENT_UNDEAD_SKILL_DEF), 
            -- poison          = args:GetTargetAttr(AttrType.ATTR_BASIC_POISON_SKILL_DEF),  
            -- poison_per      = args:GetTargetAttr(AttrType.ATTR_PERCENT_POISON_SKILL_DEF),
    -- }
	]]
    --取特殊值系数
        --体型修正系数
        --先判断手上武器
    local Weapon_type    = args.calculate_data_.caster_weapon_type;-- args:GetCasterAttr(AttrType.ATTR_SPECIAL_WEAPON_TYPE)
	local size_kezhi
	--海盗之王卡片和无视体型判定
		if 	caster:HasBuff(5100149) == true then
			size_kezhi	   = 1 
		--2020CBT2武器值最大化随等级修改体型修正值
		elseif caster:HasBuff(5005) == true then
			local wuqizhidahualv=caster:GetBuff(5005):GetBuffLevel()
			size_kezhi= ( 1-DamageCalculator.body_dam[Weapon_type][args.calculate_data_.target_size] ) * wuqizhidahualv/5 + DamageCalculator.body_dam[Weapon_type][args.calculate_data_.target_size]
		else
			size_kezhi     = DamageCalculator.body_dam[Weapon_type][args.calculate_data_.target_size]
		end
		
		--[[
    -- local size_dam_per 	= 0
	-- local size_res_per   = 0 
        -- if          tar["size"]         == 2 then
                    -- size_dam_per        = pla["small_dam"]
        -- elseif      tar["size"]         == 0 then
                    -- size_dam_per        = pla["mid_dam"]
        -- elseif      tar["size"]         == 1 then
                    -- size_dam_per        = pla["big_dam"] 
        -- end
        
        -- if          pla["size"]         == 2 then
                    -- size_res_per        = tar["small_dam"]
        -- elseif      pla["size"]         == 0 then
                    -- size_res_per        = tar["mid_dam"]
        -- elseif      pla["size"]         == 1 then
                    -- size_res_per        = tar["big_dam"] 
        -- end
        ]]
    local size_dam  = size_kezhi
	local size_up	= args.calculate_data_.size_dam_per/10000-args.calculate_data_.size_res_per/10000
	--[[
    -- local yuansu_dam 		= 0
	-- local yuansu_dam_per 	= 0
	-- local yuansu_def 		= 0 
	-- local yuansu_def_per 	= 0
				
        -- 元素属性伤害修正
        -- if          pla["atk_type"] == 1 then 
                    -- yuansu_dam = pla["wind"]        yuansu_dam_per = pla["wind_per"]
                    -- yuansu_def = tar["wind"]        yuansu_def_per = tar["wind_per"]
        -- elseif      pla["atk_type"] == 2 then 
                    -- yuansu_dam = pla["earth"]       yuansu_dam_per = pla["earth_per"]
                    -- yuansu_def = tar["earth"]       yuansu_def_per = tar["earth_per"]
        -- elseif      pla["atk_type"] == 3 then 
                    -- yuansu_dam = pla["fire"]        yuansu_dam_per = pla["fire_per"]
                    -- yuansu_def = tar["fire"]        yuansu_def_per = tar["fire_per"]
        -- elseif      pla["atk_type"] == 4 then 
                    -- yuansu_dam = pla["water"]       yuansu_dam_per = pla["water_per"]
                    -- yuansu_def = tar["water"]       yuansu_def_per = tar["water_per"]
        -- elseif      pla["atk_type"] == 0 then 
                    -- yuansu_dam = pla["formless"]    yuansu_dam_per = pla["formless_per"]
                    -- yuansu_def = tar["formless"]    yuansu_def_per = tar["formless_per"]
        -- elseif      pla["atk_type"] == 6 then 
                    -- yuansu_dam = pla["holy"]        yuansu_dam_per = pla["holy_per"]
                    -- yuansu_def = tar["holy"]        yuansu_def_per = tar["holy_per"]
        -- elseif      pla["atk_type"] == 7 then 
                    -- yuansu_dam = pla["dark"]        yuansu_dam_per = pla["dark_per"]
                    -- yuansu_def = tar["dark"]        yuansu_def_per = tar["dark_per"]
        -- elseif      pla["atk_type"] == 8 then 
                    -- yuansu_dam = pla["ghoss"]       yuansu_dam_per = pla["ghoss_per"]
                    -- yuansu_def = tar["ghoss"]       yuansu_def_per = tar["ghoss_per"]
        -- elseif      pla["atk_type"] == 9 then 
                    -- yuansu_dam = pla["undead"]      yuansu_dam_per = pla["undead_per"]
                    -- yuansu_def = tar["undead"]      yuansu_def_per = tar["undead_per"]
        -- elseif      pla["atk_type"] == 5 then 
                    -- yuansu_dam = pla["poison"]      yuansu_dam_per = pla["poison_per"]
                    -- yuansu_def = tar["poison"]      yuansu_def_per = tar["poison_per"]
        -- else
                    -- yuansu_dam = pla["formless"]    yuansu_dam_per = pla["formless_per"]
                    -- yuansu_def = tar["formless"]    yuansu_def_per = tar["formless_per"]
        -- end
       ]]
    local yuansu 		= args.calculate_data_.yuansu -- (1+yuansu_dam_per/10000-yuansu_def_per/10000) 
	--[[
	-- local race_dam_per = 0
	-- local race_res_per = 0
	
        -- 种族修正系数
        -- if          tar["race"] == 0 then
					-- race_dam = pla["brute"]
                    -- race_dam_per = pla["brute_dam"]
        -- elseif      tar["race"] == 1 then
					-- race_dam = pla["human"]
                    -- race_dam_per = pla["human_dam"]
        -- elseif      tar["race"] == 2 then 
					-- race_dam = pla["demon"]
                    -- race_dam_per = pla["demon_dam"]
        -- elseif      tar["race"] == 3 then 
					-- race_dam = pla["plant"]
                    -- race_dam_per = pla["plant_dam"]
        -- elseif      tar["race"] == 4 then 
                    -- race_dam = pla["undead"]
					-- race_dam_per = pla["undead_dam"]
        -- elseif      tar["race"] == 5 then 
                    -- race_dam = pla["formless"]
					-- race_dam_per = pla["formless_dam"]
        -- elseif      tar["race"] == 6 then 
                    -- race_dam = pla["fish"]
					-- race_dam_per = pla["fish_dam"]
        -- elseif      tar["race"] == 7 then 
                    -- race_dam = pla["angel"]
					-- race_dam_per = pla["angel_dam"]
        -- elseif      tar["race"] == 8 then 
                    -- race_dam = pla["insect"]
					-- race_dam_per = pla["insect_dam"]
        -- elseif      tar["race"] == 9 then 
                    -- race_dam = pla["dragon"]
					-- race_dam_per = pla["dragon_dam"]
        -- end
        
        -- if          pla["race"] == 0 then 
                    -- race_res_per = tar["brute_dam"]
        -- elseif      pla["race"] == 1 then 
                    -- race_res_per = tar["human_dam"]
        -- elseif      pla["race"] == 2 then 
                    -- race_res_per = tar["demon_dam"]
        -- elseif      pla["race"] == 3 then 
                    -- race_res_per = tar["plant_dam"]
        -- elseif      pla["race"] == 4 then 
                    -- race_res_per = tar["undead_dam"]
        -- elseif      pla["race"] == 5 then 
                    -- race_res_per = tar["formless_dam"]
        -- elseif      pla["race"] == 6 then 
                    -- race_res_per = tar["fish_dam"]
        -- elseif      pla["race"] == 7 then 
                    -- race_res_per = tar["angel_dam"]
        -- elseif      pla["race"] == 8 then 
                    -- race_res_per = tar["insect_dam"]
        -- elseif      pla["race"] == 9 then 
                    -- race_res_per = tar["dragon_dam"]
        -- end
	]]
    local race_dam_final	= args.calculate_data_.race_dam_per/10000-args.calculate_data_.race_res_per/10000  --(race_dam_per/10000-race_res_per/10000)
         
        --元素克制系数

    local element	= DamageCalculator.element_dam[args.attack_type_][args.defense_type_]  -- DamageCalculator.element_dam[pla["atk_type"]][tar["def_type"]]
	local YuanSuGanZhiBuff_Para=caster:GetBuff(2035)
	local belluz5016 = caster:GetBuff(60042)--贝鲁兹 腐蚀毒素 效果
	--贝鲁兹4019
	local belluz4019buff01 = caster:GetBuff(65053) --贝鲁兹 神圣元素亲和 效果
	local belluz4019buff02 = caster:GetBuff(1058)
	
	if YuanSuGanZhiBuff_Para == nil then
	else
			local elementpara = caster:GetBuff(2035):GetBuffLevel()
			if elementpara <=5 then
				if  element>1 and ( args.attack_type_== 1 or  args.attack_type_== 2 or args.attack_type_== 3 or args.attack_type_== 4 or args.attack_type_== 8 ) then
				element=element+0.02*elementpara
				end
			else
				if  element>1 and ( args.attack_type_== 1 or  args.attack_type_== 2 or args.attack_type_== 3 or args.attack_type_== 4 or args.attack_type_== 8 ) then
				element=element+0.03*(elementpara-5)+0.1
				end
			end
	
	
	end
	
	
	if belluz5016 == nil then	--贝鲁兹腐蚀毒素效果
	else
		local belluz5016Level = belluz5016:GetBuffLevel()
		local belluz5016ID = 60042 * 100 + belluz5016Level
		local belluz5016ConfigData = utilTool:LuaGetBuffTableConfigData(belluz5016ID,false)
		local belluz5016Para = belluz5016ConfigData:GetParam("rate")/10000
			
		if  element>1 and args.attack_type_ == 5 and ( args.defense_type_== 1 or  args.defense_type_== 2 or args.defense_type_== 3 or args.defense_type_== 4 ) then
			element=element+belluz5016Para
		end
	end
	
	if belluz4019buff01 ~= nil and belluz4019buff02 ~= nil then	--贝鲁兹神圣元素亲和效果
		local belluz4019Lv = belluz4019buff01:GetBuffLevel()
		local belluz4019ID = 65023 * 100 + belluz4019Lv
		local belluz4019ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4019ID,false)
		local belluz4019Rate = belluz4019ConfigData:GetParam("rate")/10000
			
		if  element>1 and args.attack_type_ == 6 and ( args.defense_type_== 9 or args.defense_type_== 7 ) then
			element=element+belluz4019Rate
		end
	end
	
	--火属性克制伤害增加
	local fireEleAdd = args:GetFightTempAttr("EQUIP_COMMON_FIREDAM_SNAPSHOT")/10000
	if  fireEleAdd~=nil and element>1 and args.attack_type_ == 3 and ( args.defense_type_== 9 or args.defense_type_== 2 ) then
		element=element+fireEleAdd
	end
	
	--毒属性克制伤害增加
	local poisonEleAdd = args:GetFightTempAttr("EQUIP_COMMON_POISONDAM_SNAPSHOT")/10000
	if  poisonEleAdd~= nil and element>1 and args.attack_type_ == 5 and ( args.defense_type_== 1 or  args.defense_type_== 2 or args.defense_type_== 3 or args.defense_type_== 4 ) then
		element=element+poisonEleAdd
	end
	
	--[[
    -- local npc_dam 		= 0  
        -- 对指定属性魔物的伤害系数
        -- if          tar["def_type"] == 1 then 
                    -- npc_dam = pla["wind_npc"]
        -- elseif      tar["def_type"] == 2 then 
                    -- npc_dam = pla["earth_npc"]
        -- elseif      tar["def_type"] == 3 then 
                    -- npc_dam = pla["fire_npc"]
        -- elseif      tar["def_type"] == 4 then 
                    -- npc_dam = pla["water_npc"]
        -- elseif      tar["def_type"] == 0 then 
                    -- npc_dam = pla["formless_npc"]
        -- elseif      tar["def_type"] == 6 then 
                    -- npc_dam = pla["holy_npc"]
        -- elseif      tar["def_type"] == 7 then 
                    -- npc_dam = pla["dark_npc"]
        -- elseif      tar["def_type"] == 8 then 
                    -- npc_dam = pla["ghoss_npc"]
        -- elseif      tar["def_type"] == 9 then 
                    -- npc_dam = pla["undead_npc"]
        -- elseif      tar["def_type"] == 5 then 
                    -- npc_dam = pla["poison_npc"]
        -- else        
                    -- npc_dam = pla["formless_npc"]
        -- end
]]
    local npc_dam_final	= args.calculate_data_.npc_dam_final-- (1+npc_dam/10000)

	--判定对指定属性目标属性对玩家无效
	-- if  target:GetUnitTypeLevel() == -1 then
		-- npc_dam_final  		= 1
	-- end    
    local wuqi_atk          = args.calculate_data_.wuqi_atk -- pla["weapon_atk"]
    local wuqi_matk         = args.calculate_data_.wuqi_matk  -- pla["weapon_matk"]
    local wuqi_atk_per		= args.calculate_data_.wuqi_atk_per/10000  -- pla["weapon_atk_per"]
	local wuqi_matk_per		= args.calculate_data_.wuqi_matk_per/10000  -- pla["weapon_matk_per"]
	--[[
	--远程物理伤害判定
	-- local atk_range		= 0
	-- local atk_range2 	= 0
	-- atk_range 	= args:GetCasterAttr(AttrType.ATTR_SPECIAL_REMOTE_PHYSICAL_DAMAGE_RATE)
	-- atk_range2	= args:GetTargetAttr(AttrType.ATTR_SPECIAL_REMOTE_PHYSICAL_DAMAGE_RELIEF)
	-- atk_range3	= args:GetCasterAttr(AttrType.ATTR_SPECIAL_PHYSICAL_DAMAGE_RATE)
	-- atk_range4	= args:GetTargetAttr(AttrType.ATTR_SPECIAL_PHYSICAL_DAMAGE_RELIEF)
	-- local attack_range 	= args.skill_:GetSkillRangeType()	--取攻击是远程还是近战
	
	-- 非boss增伤属性加入
	-- local atkup		= 0
	-- local atkdown 	= 0
	
	-- 判定目标为非boss
	-- if	target:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_BOSS	or 	
		-- target:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_Mini	or
		-- target:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_MVP	then
		-- atkup		= args:GetCasterAttr(AttrType.ATTR_PERCENT_BOSS_DAM)
		-- atkdown		= args:GetTargetAttr(AttrType.ATTR_PERCENT_BOSS_RES)
	-- elseif target:GetUnitTypeLevel() == -1 then
		-- atkup		= args:GetCasterAttr(AttrType.ATTR_PERCENT_PLAYER_RES)
		-- atkdown		= args:GetTargetAttr(AttrType.ATTR_PERCENT_PLAYER_DAM)
	--else
		-- atkup		= args:GetCasterAttr(AttrType.ATTR_PERCENT_MONSTER_DAM)
		-- atkdown		= args:GetTargetAttr(AttrType.ATTR_PERCENT_MONSTER_RES)
	-- end
	
	
	--要判定物理攻击和普攻，还要注意暴击的情况	
	-- if 	damage_result:GetType() == 0 and attack_range == 1 then
		-- pla["atk_up"] = pla["atk_up"]+atk_range-atk_range2+atkup-atkdown
	-- elseif
		-- damage_result:GetType() == 1 and attack_range == 1 then
		-- pla["atk_up"] = pla["atk_up"]+atk_range-atk_range2+atkup-atkdown
	-- elseif
		-- damage_result:GetType() == 0 and attack_range == 0 then
		-- pla["atk_up"] = pla["atk_up"]+atk_range3-atk_range4+atkup-atkdown
	--elseif
		-- damage_result:GetType() == 1 and attack_range == 0 then
		-- pla["atk_up"] = pla["atk_up"]+atk_range3-atk_range4+atkup-atkdown
	--else
		-- pla["atk_up"] = pla["atk_up"]+atkup-atkdown
	-- end
	]]
    --防御和减伤
    local def_suzhi         = args.calculate_data_.def_suzhi-- tar["t_def"]
    local mdef_suzhi        = args.calculate_data_.mdef_suzhi--tar["t_mdef"]
    --[[
	--防御值
    -- local total_equip_def   = (math.max((tar["equip_def"]-pla["ignore_def"])*(1+tar["equip_def_per"]/10000),0)+math.max((tar["equip_def_shei"]-pla["ignore_def_shei"])*(1+tar["equip_def_per"]/10000),0))*(1-pla["ignore_def_per"])
    -- local total_equip_mdef  = (math.max((tar["equip_mdef"]-pla["ignore_mdef"])*(1+tar["equip_mdef_per"]/10000),0))*(1-pla["ignore_mdef_per"])
	
    -- local def_equip       =tar["jinglian_def"]
    -- local mdef_equip      =tar["jinglian_mdef"]
	
	--新的防御计算，根据pvp场景和pve场景
	-- local def_jianshang
	-- local mdef_jianshang
    -- local scene = caster:GetCurSceneType()
	-- caster:InPvPScene() == true (判定场景是否是pvp场景)
	-- if tar["job"] == -1 then
		-- def_jianshang	= math.max((total_equip_def+4000)/(total_equip_def*10+4000),0.05)
		-- mdef_jianshang	= math.max((total_equip_mdef+1000)/(total_equip_mdef*10+1000),0.05)
	-- else
	--  if scene == 8 then
		 -- def_jianshang	= math.max(1-(total_equip_def+def_equip)/((total_equip_def+def_equip)+5*tar["base_lv"]),0.05)
		 -- mdef_jianshang	= math.max(1-(total_equip_mdef+mdef_equip)/((total_equip_mdef+mdef_equip)+5*tar["base_lv"]),0.05)
	--  else
		 -- def_jianshang	= math.max((total_equip_def+4000)/(total_equip_def*10+4000),0.1)*(1-math.max(def_equip/(def_equip+300),0.05))
		 -- mdef_jianshang	= math.max((total_equip_mdef+1000)/(total_equip_mdef*10+1000),0.1)*(1-math.max(mdef_equip/(mdef_equip+150),0.05))
	 -- end
    -- end
	]]

	--print("pvpdam"..pvpdam)
    local def_jianshang     = args.calculate_data_.def_jianshang
    local mdef_jianshang    = args.calculate_data_.mdef_jianshang
   
    --取技能倍率
	local 	racekezhi		= DamageCalculator.race_dam[args.calculate_data_.caster_race][args.calculate_data_.target_race]
    local   skill_num       = args.calculate_data_.skill_num -- args.skill_:StaticDam(args.result_index_)
    local   skill_per       = args.calculate_data_.skill_per	-- args.skill_:DamageRadio(args.result_index_)/10000*(1+args:GetCasterAttr(AttrType.ATTR_PERCENT_SKILL_ATK)/10000)
	local skillCorrection = args:GetFightTempAttr("SKILL_CANNON_ATTRDOWN")/10000
	local skillPerAdd = args:GetFightTempAttr("SKILL_SKILLPER_ADD_SNAPSHOT")/10000
	local skillNumAdd = args:GetFightTempAttr("SKILL_SKILLNUM_ADD_SNAPSHOT")
	if skillCorrection ~= nil or skillPerAdd~=nil or skillNumAdd~=nil then
		if skillCorrection ~= 0 or skillPerAdd~=0 or skillNumAdd~=0 then
			skill_num	=	(skill_num+skillNumAdd)*math.max(0,1-skillCorrection)
			skill_per	=	(skill_per+skillPerAdd)*math.max(0,1-skillCorrection)
		end
	end
    local	Dam_float		= racekezhi*args.calculate_data_.dam_float -- RORandom:randDouble((9700+pla["dam_float_per"])/10000,1.03) 乘以种族克制是为了服务战场的大门和水晶伤害
	local	Additional_ATK	= racekezhi*args:GetCasterAttr(AttrType.ATTR_BASIC_ADDITIONAL_ATK_DAM)
	local	Additional_MATK	= racekezhi*args:GetCasterAttr(AttrType.ATTR_BASIC_ADDITIONAL_MATK_DAM )--乘以种族克制是为了服务战场的大门和水晶伤害
    --[[
    -- utilTool:LuaLog("skill_num"..skill_num)
    -- utilTool:LuaLog("skill_per"..skill_per)
    -- utilTool:LuaLog("a"..args.calculate_data_.atk) -- pla["atk"])
    -- utilTool:LuaLog("b"..args.calculate_data_.matk)-- pla["matk"])
    -- utilTool:LuaLog("wuqi_atk"..wuqi_atk)
    -- utilTool:LuaLog("wuqi_matk"..wuqi_matk)
    -- utilTool:LuaLog("size_dam"..size_dam)
    -- utilTool:LuaLog("yuansu"..yuansu)
    -- utilTool:LuaLog("race_dam_final"..race_dam_final)
    -- utilTool:LuaLog("element"..element)
    -- utilTool:LuaLog("c"..args.calculate_data_.jinglian_atk)-- pla["jinglian_atk"])
    -- utilTool:LuaLog("d"..args.calculate_data_.jinglian_matk)  --pla["jinglian_matk"])
    -- utilTool:LuaLog("def_jianshang"..def_jianshang)
    -- utilTool:LuaLog("mdef_jianshang"..mdef_jianshang)
    -- utilTool:LuaLog("def_jianshang"..def_jianshang)
    -- utilTool:LuaLog("npc_dam_final"..npc_dam_final)
    -- utilTool:LuaLog("def_suzhi"..def_suzhi)
    -- utilTool:LuaLog("mdef_suzhi"..mdef_suzhi)
    
    -- print("skill_num"..skill_num)
    -- print("skill_per"..skill_per)
    -- print("a"..args.calculate_data_.atk) -- pla["atk"])
    -- print("b"..args.calculate_data_.matk)-- pla["matk"])
    -- print("wuqi_atk"..wuqi_atk)
    -- print("wuqi_matk"..wuqi_matk)
    -- print("size_dam"..size_dam)
    -- print("yuansu"..yuansu)
    -- print("race_dam_final"..race_dam_final)
    -- print("element"..element)
    -- print("c"..args.calculate_data_.jinglian_atk)-- pla["jinglian_atk"])
    -- print("d"..args.calculate_data_.jinglian_matk)  --pla["jinglian_matk"])
    -- print("def_jianshang"..def_jianshang)
    -- print("mdef_jianshang"..mdef_jianshang)
    -- print("npc_dam_final"..npc_dam_final)
    -- print("def_suzhi"..def_suzhi)
    -- print("mdef_suzhi"..mdef_suzhi)
   ]]
   --PVP中对攻击属性放大缩小
	local jinglian_atk = args.calculate_data_.jinglian_atk
	local jinglian_matk = args.calculate_data_.jinglian_matk
	local atk =args.calculate_data_.atk
	local matk =args.calculate_data_.matk
	if caster:InPvPScene() == true then
		local pvpWeaponAtkPara={
		[0]=    1,
		[1]=    0.6,
		[2]=    0.75,
		[3]=    1.3,
		[4]=    0.75,
		[5]=    1.2,
		[6]=    0.66,
		[7]=    1,
		[8]=    0.7,
		[9]=    0.58,
		[10]=   0.88,
		[11]=   0.8,
		[12]=   1,
		[13]=   1,
		[14]=   1,
		[15]=   1,
		[16]=   0.9,
		[17]=   1,
		[101]=   0.8,
		[102]=   1.2,
		[103]=   1,
		[104]=   1
		}
		local BaseLv = math.max(args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV) - 29 ,1)
		--物理攻击
		local atkLvPara = math.min(wuqi_atk,((3*BaseLv*BaseLv+107*BaseLv+600)/28+100)*pvpWeaponAtkPara[Weapon_type])
		local matkLvPara = math.min(wuqi_matk,((3*BaseLv*BaseLv+107*BaseLv+600)/25+100)*pvpWeaponAtkPara[Weapon_type])

		wuqi_atk = (wuqi_atk + 2*atkLvPara)/3
		atk = atk * 2/3

		wuqi_matk = (wuqi_matk + matkLvPara)/2
		matk = matk * 2/3
	end
   
    return skill_num, skill_per, atk , matk, wuqi_atk, wuqi_matk, wuqi_atk_per, wuqi_matk_per, size_dam, size_up, yuansu, race_dam_final, element,jinglian_atk, jinglian_matk, def_jianshang, mdef_jianshang, npc_dam_final, def_suzhi, mdef_suzhi,args.calculate_data_.caster_atk_up, args.calculate_data_.caster_matk_up, args.calculate_data_.target_atk_down, args.calculate_data_.target_matk_down, Dam_float ,Additional_ATK ,Additional_MATK  
    end
	
function DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
		--返回PVP伤害系数
	local pvpdam
	if caster:InPvPScene() == true then--(判定场景是否是pvp场景)
		if IsCommonSkill == true then
			pvpdam=0.7--普攻伤害系数
		else
			if IsMagicSkill == true then
				pvpdam=0.3--魔法伤害系数
			else
				pvpdam=0.6--物理伤害系数
			end
		end
	else
		pvpdam= 1
	end
	return pvpdam
end
--普通攻击计算
function DamageCalculator.CommonSkillCritCalc(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculateCommonSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    
	local IsCommonSkill=true
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	--魔刀吞神效果
	local TempAddDam = args:GetFightTempAttr("EQUIP_MDTS_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--巫杖魂咬判定
	 TempAddDam = TempAddDam + args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000

	--圣锤十字星判定
	TempAddDam = TempAddDam + args:GetFightTempAttr("EQUIP_SCSZX_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000

	--魔锤安魂曲判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_MSAHQ_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)
	
	
	local damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+Additional_ATK+skill_num*Dam_float)*pvpdam
    local damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+Additional_ATK+skill_num*Dam_float)*pvpdam
	local Skill_LV
	
	if 		caster:GetSkillInfo(210202) ~= nil then
			Skill_LV 	= caster:GetSkillInfo(210202):Level()
	else	Skill_LV	= 0
	end
	

	--灵气剑buff
	if caster:HasBuff(156) == true then
		damage		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
	end
	
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)

    --爆伤计算  
    local cri_dam_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_CRI_DAM)             
    local t_cri_damdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_CRI_DEF)
	
		if  (damage_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI) then
					damage_result:SetResult(damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
					damage_result:SetRealResult(damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
					damage_result:SetAttrAddition(element-1)
		else		damage_result:SetResult(damage)
					damage_result:SetRealResult(damage_real)
					damage_result:SetAttrAddition(element-1)
		end
	-- print("HP:"..args:GetCasterAttr(AttrType.ATTR_BASIC_HP).."/"..args:GetCasterAttr(AttrType.ATTR_BASIC_MAX_HP))
	-- print("SuppressRatio:"..SuppressRatio)
	-- print("yuansu:"..yuansu)
	-- print("size:"..size)
	-- print("element:"..element)
	-- print("damage:"..damage)
	-- print("def:"..def)
	-- print("def_suzhi:"..def_suzhi)
	
	

end

--物理技能计算
function DamageCalculator.PhysicalSkillCalc(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local Id = args.skill_:SkillEffectID()
	local car_weight 	=math.min(args:GetCasterAttr(AttrType.ATTR_BASIC_WIGHT_CART),8000+2000+2000,args:GetCasterAttr(AttrType.ATTR_SPECIAL_WIGHT_CART_MAX))--进阶二转可允许手推车负重上限增加2000,贝克战斧上限提升2000
	local damage
	local damage_real
	local Skill_LVB
	local Skill_LVC
	local Skill_LV


	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	--负重量上升技能等级
	if 		caster:GetSkillInfo(600014) ~= nil then
			Skill_LVB 	= caster:GetSkillInfo(600014):GetParam("DamUp")
	else 	Skill_LVB	= 0
	end

	if 		caster:GetSkillInfo(210202) ~= nil then
			Skill_LV 	= caster:GetSkillInfo(210202):Level()
	else	Skill_LV	= 0
	end
	local belluz7008 = caster:GetBuff(60054)	--贝鲁兹 手推车增幅 效果
	
	local bkzfBarrowDamUp = args:GetFightTempAttr("EQUIP_BKZF_BARROWDAMUP")/10000

	--魔刀吞神效果
	local TempAddDam = args:GetFightTempAttr("EQUIP_MDTS_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	

	--手推车攻击判定
	if Id == 60000501	then
		TempAddDam= TempAddDam + bkzfBarrowDamUp*car_weight
		if belluz7008 ~= nil then
			local belluz7008Level = belluz7008:GetBuffLevel()
			local belluz7008ID = 60054 * 100 + belluz7008Level
			local belluz7008ConfigData = utilTool:LuaGetBuffTableConfigData(belluz7008ID,false)
			local belluz7008Rate = belluz7008ConfigData:GetParam("rate")
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/8000+Skill_LVB/10000)*(1+car_weight/belluz7008Rate)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/8000+Skill_LVB/10000)*(1+car_weight/belluz7008Rate)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		else
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/8000+Skill_LVB/10000)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/8000+Skill_LVB/10000)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		end
	--剧毒短刀攻击判定
	elseif	Id > 51020900 and Id < 51020911 then
			if target:CheckBuffState(ROGameLibs.kBuffStateType_Poisoning) then
			Skill_LVC=10000
			damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*(1+Skill_LVC/10000)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float*(1+Skill_LVC/10000)+Additional_ATK)*pvpdam
			damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+Skill_LVC/10000)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float*(1+Skill_LVC/10000)+Additional_ATK)*pvpdam
			else
			Skill_LVC=0
			damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*(1+Skill_LVC/10000)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float*(1+Skill_LVC/10000)+Additional_ATK)*pvpdam
			damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+Skill_LVC/10000)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float*(1+Skill_LVC/10000)+Additional_ATK)*pvpdam
			end
	--手推车终结技判定
	elseif  Id > 61020500 and Id < 61020511 then
		TempAddDam= TempAddDam + bkzfBarrowDamUp*car_weight
		if belluz7008 ~= nil then
			local belluz7008Level = belluz7008:GetBuffLevel()
			local belluz7008ID = 60054 * 100 + belluz7008Level
			local belluz7008ConfigData = utilTool:LuaGetBuffTableConfigData(belluz7008ID,false)
			local belluz7008Rate = belluz7008ConfigData:GetParam("rate")
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/belluz7008Rate)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/belluz7008Rate)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		else
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		end
	--手推车冲撞判定
	elseif  Id > 60001900 and Id < 60001911 then
		TempAddDam= TempAddDam + bkzfBarrowDamUp*car_weight
		if belluz7008 ~= nil then
			local belluz7008Level = belluz7008:GetBuffLevel()
			local belluz7008ID = 60054 * 100 + belluz7008Level
			local belluz7008ConfigData = utilTool:LuaGetBuffTableConfigData(belluz7008ID,false)
			local belluz7008Rate = belluz7008ConfigData:GetParam("rate")
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/belluz7008Rate)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+car_weight/belluz7008Rate)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		else
			damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		end
	elseif	Id > 51010700 and Id < 51010711 and caster:HasBuff(65029) == true then    
	
		local belluz5001BuffLv = caster:GetBuff(65029):GetBuffLevel()	--贝鲁兹 影牙增幅 判断
		local belluz5001ID = 65029 * 100 + belluz5001BuffLv
		local belluz5001ConfigData = utilTool:LuaGetBuffTableConfigData(belluz5001ID,false)
		local TempIncDam = 1+ belluz5001ConfigData:GetParam("rate")/10000
		damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float*TempIncDam+skill_num*Dam_float*TempIncDam+Additional_ATK)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float*TempIncDam+skill_num*Dam_float*TempIncDam+Additional_ATK)*pvpdam
	--骑乘攻击判断
	elseif	Id > 21010800 and Id < 21010811 and args.result_index_ == 1 then
		local belluz2010 = caster:GetBuff(60036)
		if belluz2010 == nil then
			damage_result:IgnoreBeatBack()
			return
		else
			return
		end
	else
	
		damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
	end
    	
	--灵气剑buff
	if caster:HasBuff(156) == true then
		damage		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
	end
	
	
		-- print("SuppressRatio:"..SuppressRatio)
	-- print("atk:"..atk)
	-- print("wuqi_atk:"..wuqi_atk)
	-- print("Dam_float:"..Dam_float)
	-- print("wuqi_atk:"..wuqi_atk)
	-- print("size:"..size)
	-- print("element:"..element)
	-- print("damage:"..damage)
	-- print("def:"..def)
	-- print("def_suzhi:"..def_suzhi)

	
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
          damage_result:SetResult(damage)
          damage_result:SetRealResult(damage_real)
		  damage_result:SetAttrAddition(element-1)
end

--通用分摊伤害,skillper为点名玩家血量上限的倍率
function DamageCalculator.PhysicalShareReal(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local Hit_Num = args.aoe_opponents_count_
	-- local BaseLv  = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local PlayerMaxHp = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	-- local numlist = {6800,8000,9000,10000,12000}
--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	-- skill_num = numlist[BaseLv/5-10]
	-- if skill_num == nil then
		-- skill_num = 4000
	-- end
	
	local damage 		= (PlayerMaxHp*math.max(1,skill_per))/Hit_Num
    local damage_real 	= (PlayerMaxHp*math.max(1,skill_per))/Hit_Num
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--真实物理技能
function DamageCalculator.PhysicalSkillCalcReal(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)

	local pvpdam = 1
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	local damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--魔法技能计算
function DamageCalculator.MagicSkillCalc(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
	
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)

	
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	
	local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	local Id = args.skill_:SkillEffectID()
	--巫杖魂咬判定
		local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000

	if 40001600 < Id and Id < 40001611 then --怒雷强击
		TempAddDam = TempAddDam / 4
	elseif 41020800 < Id and Id < 41020811 then --巨石阵冲击
		TempAddDam = TempAddDam / 5
	end

	--圣锤十字星判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_SCSZX_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--魔锤安魂曲判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_MSAHQ_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)
	
	local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
	
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	  
        --判断是否拥有法师勋章1
	-- local oriBuffID = 8100180
	-- local pro = 0.3
	-- local randomNum = RORandom:randDouble(0,1)
	-- local checkBuffID = 8100181
	-- local deleteBuffID = 810018201
	-- local skillAttr = args.attack_type_
	-- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
		-- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		-- local buffID = checkBuffID * 100 + buffLvl
		-- if target:HasBuff(checkBuffID) == false then
			-- utilTool:LuaAddBuff(caster, target, buffID, true)
		-- elseif target:HasBuff(checkBuffID) == true then
			-- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
		-- end
    -- end
		
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)

end

--真实魔法技能
function DamageCalculator.MagicSkillCalcReal(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   

	local pvpdam = 1
	
	local damage 		= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	
	--判断是否拥有法师勋章1
	-- local oriBuffID = 8100180
	-- local pro = 0.3
	-- local randomNum = RORandom:randDouble(0,1)
	-- local checkBuffID = 8100181
	-- local deleteBuffID = 810018201
	-- local skillAttr = args.attack_type_
	-- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
		-- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		-- local buffID = checkBuffID * 100 + buffLvl
		-- if target:HasBuff(checkBuffID) == false then
			-- utilTool:LuaAddBuff(caster, target, buffID, true)
		-- elseif target:HasBuff(checkBuffID) == true then
			-- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
		-- end
    -- end  
        
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--元素固定伤害物理技能（目前仅施毒）
function DamageCalculator.StableElementDamage(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
	local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	--施毒伤害
	skill_num = skill_num * (math.min(45,BaseLv)-10)+50
	    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	local damage 		= (math.max((skill_num*element)-def_suzhi,0)*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((skill_num*element),0)*Dam_float+Additional_ATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		 	
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end 

--压团血技能，物理伤害必中
function DamageCalculator.PhysicalHit(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
	
	    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	local damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		 	
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--混合技能计算
function DamageCalculator.MixSkillCalc(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) 
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    
    local damage        = (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*(1+3*def)/4*(3+mdef)/4*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real   = (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--混合百分比血量技能计算（skillnum/10000 表示技能的血量伤害百分比）
function DamageCalculator.MixPerSkillCalc(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) 
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local HPMax = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	local HPDam = skill_num / 10000 * HPMax*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    
    local damage        = (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*(1+3*def)/4*(3+mdef)/4*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+HPDam*Dam_float+Additional_ATK)*pvpdam
    local damage_real   = (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+HPDam*Dam_float+Additional_ATK)*pvpdam

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--十字驱魔攻击
function DamageCalculator.ShiZiQuMo(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local def_type      = args.defense_type_  % 100
	local race_new      = args:GetTargetAttr(AttrType.ATTR_SPECIAL_RACE)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local TempIncDam = 1
	
	--检测贝鲁兹buff4016
	local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
    local belluz4016Buff = owner:GetBuff(65017)	--神圣钉刺效果
	--检测贝鲁兹buff4017
    local belluz4017Buff = owner:GetBuff(65019)	--驱魔增幅效果
	
	if  belluz4017Buff  ~= nil then 
	    local belluz4017BuffLv = belluz4017Buff:GetBuffLevel()
	    local finalBuff = 6504700 + belluz4017BuffLv
		local belluz4017ID = 65019 * 100 + belluz4017BuffLv
		local belluz4017ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4017ID,false)
		local belluz4017Rate = belluz4017ConfigData:GetParam("rate")/10000
		local belluz4017IncreaseBuff = target:GetBuff(65020)
		if belluz4017IncreaseBuff ~= nil then
			local belluz4017Count = belluz4017IncreaseBuff:GetAppendCount()
			TempIncDam = TempIncDam + belluz4017Rate * belluz4017Count
		end
		--utilTool:LuaLog("finalBuff"..tonumber(finalBuff))
	    utilTool:LuaAddBuff(owner, target, 6502000+belluz4017BuffLv, true)
		--utilTool:LuaLog("222")
	end
	--圣锤十字星判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_SCSZX_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--魔锤安魂曲判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_MSAHQ_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)
	
	
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    
	
	if  belluz4016Buff ~= nil then
	    local belluz4016BuffLv = belluz4016Buff:GetBuffLevel()
		local belluz4016ID = 65017 * 100 + belluz4016BuffLv
		local belluz4016ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4016ID,false)
		local belluz4016Pro = belluz4016ConfigData:GetParam("pro")
		local Random = RORandom:randDouble(0,1)
		if Random <= belluz4016Pro / 10000  then 
			utilTool:LuaAddBuff(owner, caster, 6504601, true)
		end
	end
	
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	
	if  race_new == 2 or def_type == 9 then
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage)
	else
		damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
    end	  

end

--猎人陷阱伤害
function DamageCalculator.Hunterhonk(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local int_base      = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	local dex_base      = args:GetCasterAttr(AttrType.ATTR_SPECIAL_DEX)
	
	local damage
	local damage_real
	local Id = args.skill_:SkillEffectID()
	local Skill_LVB = 0
	local Honk_UP		= args:GetCasterAttr(AttrType.ATTR_PERCENT_TRAP_DAM)
	local Honk_down		= args:GetTargetAttr(AttrType.ATTR_PERCENT_TRAP_RES)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	--爆散陷阱
	if 71011300<Id and Id<71011306 then
		if 		caster_snapshot:HasBuff(4054) == true then
			Skill_LVB 	= caster_snapshot:GetBuffAttr(4054, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)/100
		else 	
			Skill_LVB	= 0
		end
	else
	end
	--地雷陷阱
	if 71011500<Id and Id<71011506 then
		if 	caster_snapshot:HasBuff(4055) == true then
			local Skill_DamageUP_Array={[1]=0.32,[2]=0.34,[3]=0.36,[4]=0.38,[5]=0.4,[6]=0.42,[7]=0.44,[8]=0.46,[9]=0.48,[10]=0.5}
			local Skill_DamageUPPro_Array={[1]=1700,[2]=1900,[3]=2100,[4]=2300,[5]=2500,[6]=2700,[7]=2900,[8]=3100,[9]=3300,[10]=3500}
			local Skill_UP_LV 	= caster_snapshot:GetBuffAttr(4055, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
			Skill_UP_LV 	= caster_snapshot:GetBuffAttr(4055, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
			--保底buff,暂未实现
			--utilTool:LuaAddBuff(caster, caster_snapshot, 406101, true)
			--local buffCount = 1
				-- if caster_snapshot:HasBuff(406101) == true then
					-- buffCount = caster_snapshot:GetBuffAttr(406101, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_APPEND_COUNT)
					-- print("buffCount"..buffCount)
				-- else
					-- print("unfound406101")
				-- end
				--if buffCount >= math.ceil( 10000/Skill_DamageUPPro_Array[Skill_UP_LV]) then
					--Skill_LVB = Skill_DamageUP_Array[Skill_UP_LV]
					--utilTool:LuaAddBuff(caster, caster_snapshot, 406201, true)
				--else
					if RORandom:randDouble(0,10000)<Skill_DamageUPPro_Array[Skill_UP_LV] then
					Skill_LVB = Skill_DamageUP_Array[Skill_UP_LV]
					--utilTool:LuaAddBuff(caster, caster_snapshot, 406201, true)
					end
				--end
		else 	
			Skill_LVB	= 0
		end
	else
	end
	
	--local proList = {400,600,800,900,1000}
	local randomNum = RORandom:randDouble(0,1)
	local TempIncDam = 1
	if caster_snapshot:HasBuff(60048) == true then	--贝鲁兹陷阱增幅效果
        local belluz6010Level = caster_snapshot:GetBuffAttr(60048, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		local belluz6010ID = 60048 * 100 + belluz6010Level
		local belluz6010ConfigData = utilTool:LuaGetBuffTableConfigData(belluz6010ID,false)
		TempIncDam = TempIncDam + int_base / belluz6010ConfigData:GetParam("rate")/100
	end	
	
		damage			= (dex_base*(int_base/50+1)*(2+BaseLv/55)*Dam_float*TempIncDam*mdef*size*element*math.max((npc_dam+Honk_UP/10000-Honk_down/10000+matk_up/10000-matk_down/10000),0.1)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per*(1+Skill_LVB)+Additional_MATK)*pvpdam*SuppressRatio
		damage_real		= (dex_base*(int_base/50+1)*(2+BaseLv/55)*Dam_float*TempIncDam*size*element*math.max((npc_dam+Honk_UP/10000-Honk_down/10000+matk_up/10000-matk_down/10000),0.1)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per*(1+Skill_LVB)+Additional_MATK)*pvpdam*SuppressRatio
		--
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
	--弓手勋章效果（暂废弃）
    -- if caster_snapshot:HasBuff(8100260) == true then
        -- local buffLvl = caster_snapshot:GetBuffAttr(8100260, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		-- local pro = proList[buffLvl] / 10000
		-- if randomNum <= pro then
			-- damage = damage * 2
			-- damage_real = damage_real * 2
		-- end
	-- end

	damage_result:SetResult(damage)
    damage_result:SetRealResult(damage_real)
	damage_result:SetAttrAddition(element-1)
	
end

--返回0伤害
function DamageCalculator.DamageNil(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
		DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
        damage_result:SetResult(0)
        damage_result:SetRealResult(0)
end

--初心者治疗
function DamageCalculator.CureBase(utilTool, caster,caster_snapshot, target, damage_info, args, damage_result)
		DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
        damage_result:SetResult(-100)
        damage_result:SetRealResult(-100)
end

--初心者重击
function DamageCalculator.ZhongJi(utilTool, caster,caster_snapshot, target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
        damage_result:SetType(2)
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local racekezhi	= DamageCalculator.race_dam[args.calculate_data_.caster_race][args.calculate_data_.target_race]

	local pvpdam = 1
	
	local damage = skill_num*racekezhi*pvpdam
	local damage_real = 0
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage)
		
end

--圣灵召唤
function DamageCalculator.Holyzhaohuan(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local def_type      = args.defense_type_ % 100 
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage 		= 0
	local damage_real 	= 0
	   --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000/5
	
    if      def_type == 10  then 
       damage 			= (math.floor(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*skill_per*(1+skill_num/10000)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi)*Dam_float+40*Dam_float+Additional_MATK)*pvpdam
       damage_real 		= (math.floor(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element+TempAddDam)*skill_per*(1+skill_num/10000)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio)*Dam_float+40*Dam_float+Additional_MATK)*pvpdam
    else 
       damage 			= (math.floor(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi)*Dam_float+40*Dam_float+Additional_MATK)*pvpdam
       damage_real 		= (math.floor(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio)*Dam_float+40*Dam_float+Additional_MATK)*pvpdam
    end
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
            damage_result:SetResult(damage)
            damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
end

--水球术计算（魂咬需处理攻击段数）
function DamageCalculator.WaterBall(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	--释放的水球术等级
	local Skill_Lv = args.skill_:Level()
	local SkillResultCount = {3,9,9,9,15,15,15,15,15,15}
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000/SkillResultCount[Skill_Lv]
	
	local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
	
	local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
	
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		
		
		damage_result:SetResult(damage)
		damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)

end

--服侍治疗
function DamageCalculator.CureFushi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local def_type      = args.defense_type_  
	local Skill_LVB
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --local Skill_Lv    =   caster:GetSkillInfo(300008):Level() --取到治疗术等级   
    local damage 		= 0
    local weaponBuffA   =   0
	 --冥想等级
	if 		caster:GetSkillInfo(310205) ~= nil then
			Skill_LVB 	= caster:GetSkillInfo(310205):Level()
	else 	Skill_LVB	= 0
	end
    --装备：黛雅之杖
    if caster:HasBuff(4102050) == true then
        weaponBuffA = 1
    end

    local total_int     = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	--痊愈光辉减治疗效果
	local qyghdeheal = args:GetFightTempAttr("EQUIP_QYGH_DEHEAL")/10000
	local skillper_equip = 1
	skillper_equip=math.max(0.1,skillper_equip-qyghdeheal)
		  damage =((((math.max(BaseLv,1)+total_int)/5*3*skill_per)+((10+skill_per)/20+weaponBuffA)*(matk+wuqi_matk+jinglian_matk*(1+matk_per)))*skillper_equip*Dam_float+skill_num*Dam_float)*(1+Skill_LVB*0.02)*(1+Heal_up/10000)*(1+Beheal_up/10000)

    if  damage <= 1*damage_result:GetCount() then 
        damage = 1*damage_result:GetCount()
    end

    if 	args.result_index_ == 0 then
		if caster:GetUUID() == target:GetUUID() then
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
			return
		end
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			damage_result:SetResult(-damage)
			damage_result:SetRealResult(-damage)
		end
		
		local belluz4001 = 60062	--贝鲁兹 圣灵庇护 效果
		if caster:HasBuff(belluz4001) == true then
			local belluz4001targetHp = args:GetTargetAttr(AttrType.ATTR_BASIC_HP)
			local belluz4001targetMaxHp = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
			local belluz4001Buff = caster:GetBuff(belluz4001)
			local belluz4001BuffLvl = belluz4001Buff:GetBuffLevel()
			local belluz4001ID = 60062 * 100 + belluz4001BuffLvl
			local belluz4001ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4001ID,false)
			local belluz4001shieldRate = belluz4001ConfigData:GetParam("shieldRate")/10000
			local belluz4001maxRate = belluz4001ConfigData:GetParam("maxRate")/10000
			
			local belluz4001shieldHp = math.min(math.max(belluz4001targetHp+damage-belluz4001targetMaxHp,0) * belluz4001shieldRate,belluz4001targetMaxHp * belluz4001maxRate)
			if belluz4001shieldHp > 0 and caster:GetUUID() ~= target:GetUUID() then
				local belluz4001addbuffID = 6006300 + belluz4001BuffLvl
				utilTool:LuaAddShieldBuff(caster,target,belluz4001addbuffID,false,belluz4001shieldHp,false)
			end
		end
		
		--牧师勋章（暂废弃）
		-- local oriBuffID = 8100220
		-- if caster:HasBuff(oriBuffID) == true then
			-- local proList = {1000,1300,1500,1700,2000}
			-- local oriBuff = caster:GetBuff(oriBuffID)
			-- local oriBuffLvl = oriBuff:GetBuffLevel()
			-- local pro = proList[oriBuffLvl] / 10000
			-- local randomNum = RORandom:randDouble(0,1)
			-- local cdBuffID = 8100222 * 100 + oriBuffLvl
			-- local cdBuff = 8100222
			-- local buffID = 8100221 * 100 + oriBuffLvl
			-- if caster:HasBuff(cdBuff) == false and randomNum <= pro then
				-- utilTool:LuaAddBuff(caster, target, buffID, true)
				-- utilTool:LuaAddBuff(caster, caster, buffID, true)
				-- utilTool:LuaAddBuff(caster, caster, cdBuffID, true)
			-- end
		-- end
	
	elseif args.result_index_ == 1 then
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
		end
	elseif args.result_index_ == 2 then
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			
			local belluz4001 = 60062	--贝鲁兹 圣灵庇护 效果
			if caster:HasBuff(belluz4001) == true then
				local belluz4001targetHp = args:GetTargetAttr(AttrType.ATTR_BASIC_HP)
				local belluz4001targetMaxHp = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
				local belluz4001Buff = caster:GetBuff(belluz4001)
				local belluz4001BuffLvl = belluz4001Buff:GetBuffLevel()
				local belluz4001ID = 60062 * 100 + belluz4001BuffLvl
				local belluz4001ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4001ID,false)
				local belluz4001shieldRate = belluz4001ConfigData:GetParam("shieldRate")/10000
				local belluz4001maxRate = belluz4001ConfigData:GetParam("maxRate")/10000
			
				local belluz4001shieldHp = math.min(math.max(belluz4001targetHp+damage-belluz4001targetMaxHp,0) * belluz4001shieldRate,belluz4001targetMaxHp * belluz4001maxRate)
				if belluz4001shieldHp > 0 then
					local belluz4001addbuffID = 6006300 + belluz4001BuffLvl
					utilTool:LuaAddShieldBuff(caster,target,belluz4001addbuffID,false,belluz4001shieldHp,false)
				end
			end
			damage_result:SetResult(-damage)
			damage_result:SetRealResult(-damage)
		end
	end
end

--服侍治疗，buff专用
function DamageCalculator.CureFushiBuff(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	-- 初始化caster/target的属性信息，才能调用GetCasterAttr 和 GetTargetAttr 等方法
	args:CalculatorProcess(caster_snapshot, caster, target, damage_result, damage_info)
	-- DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local matk     = args:GetCasterAttr(AttrType.ATTR_BASIC_BASE_MATK_FINAL)
    local wuqi_matk     = args:GetCasterAttr(AttrType.ATTR_BASIC_WEAPON_MATK_FINAL)
    local jinglian_matk     = args:GetCasterAttr(AttrType.ATTR_TOTAL_MREFINE)
    local matk_per     = args:GetCasterAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE)/10000
    --local skill_per     = 100000/10000
    local damage 		= 0
    local total_int     = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	
	local Skill_LVB = 0
	if caster:GetSkillInfo(310205) ~= nil then
		Skill_LVB 	= caster:GetSkillInfo(310205):Level()
	else 	
		Skill_LVB	= 0
	end

	
	damage =(matk+wuqi_matk+jinglian_matk*(1+matk_per))*(1+Heal_up/10000)*(1+Beheal_up/10000)
	-- damage =1000
    if  damage <= 1*damage_result:GetCount() then 
        damage = 1*damage_result:GetCount()
    end
	local damType = damage_info:GetParam("damType")
	local rate = damage_info:GetParam("rate")/10000
	local atkType = damage_info:GetParam("atkType")
	
	damage = damage * rate
	
	damage_result:SetResult(-damage)
	damage_result:SetRealResult(-damage)
	
	damage_result:SetAttrAddition(atkType)
	-- 设置伤害类型 ！！！！默认为Unknown ，这种类型不会触发护盾
	damage_result:SetType(damType)
end

--灭世魔爆，buff专用
function DamageCalculator.MieShiMoBaoBuff(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	-- 初始化caster/target的属性信息，才能调用GetCasterAttr 和 GetTargetAttr 等方法
	args:CalculatorProcess(caster_snapshot, caster, target, damage_result, damage_info)
    local hp_max        = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	local rate = damage_info:GetParam("rate")/10000
    local damage 		= hp_max * rate
	local damType = damage_info:GetParam("damType")
	
	local atkType = damage_info:GetParam("atkType")
	
	damage_result:SetResult(damage)
	damage_result:SetRealResult(damage)
	
	damage_result:SetAttrAddition(atkType)
	-- 设置伤害类型 ！！！！默认为Unknown ，这种类型不会触发护盾
	damage_result:SetType(damType)
end

--模拟服事治愈术
function DamageCalculator.ZhiYuShuMoNi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local def_type      = args.defense_type_  
	local Skill_LVB
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --local Skill_Lv    =   caster:GetSkillInfo(300008):Level() --取到治疗术等级   
    local damage 		= 0
	local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	local weaponBuffA = 0
	 --冥想等级
	if owner ~= nil then
			--装备：黛雅之杖
		if owner:HasBuff(4102050) == true then
			weaponBuffA = 1
		end
		if owner:GetSkillInfo(310205) ~= nil then
			Skill_LVB 	= owner:GetSkillInfo(310205):Level()
		else
			Skill_LVB	= 0
		end
	else
		Skill_LVB	= 0
	end
    local total_int     = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
		  damage =(((((math.max(BaseLv,1)+total_int)/5*3*skill_per)+(skill_per/10)*(1+weaponBuffA)*(matk+wuqi_matk+jinglian_matk*(1+matk_per)))*Dam_float+skill_num*Dam_float)*(1+Skill_LVB*0.02))*(1+Heal_up/10000)*(1+Beheal_up/10000)
    if  damage <= 1*damage_result:GetCount() then 
        damage = 1*damage_result:GetCount()
    end

	if  def_type%100 == 9 then
		damage_result:SetResult(damage*0.5)
		damage_result:SetRealResult(damage*0.5)
	else
		damage_result:SetResult(-damage)
		damage_result:SetRealResult(-damage)
	end
		
end

--治疗怪物用
function DamageCalculator.CureMonster(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local hp_max        = args:GetCasterAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)	
    local damage 		= 0
	damage =skill_per*hp_max
	damage_result:SetResult(-damage)
	damage_result:SetRealResult(-damage)

	end

--光耀之堂治疗
function DamageCalculator.Holycure(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local race_new      = args:GetTargetAttr(AttrType.ATTR_SPECIAL_RACE)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local def_type      = args.defense_type_ % 100   
	
	if caster_snapshot:HasBuff(4100430) == true then	--装备效果光耀之堂增加
		Heal_up = 2000
	else
		Heal_up = Heal_up
	end


	
    local damage = skill_per*1000*(1+Heal_up/10000)*(1+Beheal_up/10000)
    if      damage <= 1*damage_result:GetCount() then 
            damage = 1*damage_result:GetCount()
    end
    
	local casterMarkBuff = caster:GetBuff(1027)
	local casterMarkBuffLvl = 1
	if casterMarkBuff ~= nil then
		casterMarkBuffLvl = casterMarkBuff:GetBuffLevel()
	end
	
	local targetMarkBuff = target:GetBuff(1028)
	local targetMarkBuffLvl = 1
	if targetMarkBuff ~= nil then
		targetMarkBuffLvl = targetMarkBuff:GetBuffLevel()
	end
	
    if  utilTool:LuaCheckFightGroup(caster_snapshot, target) == ROGameLibs.kFightGroupRelationFriendly then
        if target:HasBuff(1025) == false then
			if casterMarkBuffLvl >= targetMarkBuffLvl then
				damage_result:SetResult(-damage)
				damage_result:SetRealResult(-damage)
				damage_result:SetAttrAddition(element-1)
			else
				damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
				return
			end
		else
			damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
			return
		end
    else 
	
        if  race_new == 2 or def_type == 9 then
            if target:HasBuff(1025) == false then
				if casterMarkBuffLvl >= targetMarkBuffLvl then
					damage_result:SetResult(damage)
					damage_result:SetRealResult(damage)
				else
					damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
					return
				end
			else
				damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
				return
			end
        else
            -- damage_result:SetResult(0)
            -- damage_result:SetRealResult(0)
			damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
            damage_result:IgnoreBeatBack()
			return
        end
    end
end

--二刀连击
function DamageCalculator.ErDaoLianJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculateCommonSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
	return
	end
    local Skill_info    = 0
	local Skill_info2	= 0
    local damage        = 0
    local damage_real   = 0
    local damage2P      = 0
    local damage_real2P = 0
    local Weapon_type   = args:GetCasterAttr(AttrType.ATTR_SPECIAL_WEAPON_TYPE)
    --爆伤计算      
    local cri_dam_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_CRI_DAM)
    local t_cri_damdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_CRI_DEF)
    local edjtBuffID = 3037
    --PVP伤害系数
	local IsCommonSkill=true
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
    if caster:GetSkillInfo(500010)     ~= nil  then 
	    if caster:HasBuff(65030) == true then
			local belluz5002lv = caster:GetBuff(65030):GetBuffLevel()
			local belluz5002ID = 65030 * 100 + belluz5002lv
			local belluz5002ConfigData = utilTool:LuaGetBuffTableConfigData(belluz5002ID,false)
			local belluz5002Pro = belluz5002ConfigData:GetParam("pro")
			Skill_info = caster:GetSkillInfo(500010):GetParam("Pro") + belluz5002Pro
		else
			Skill_info = caster:GetSkillInfo(500010):GetParam("Pro")
		end
    else    Skill_info= 0
    end

    if      caster:GetSkillInfo(510203)     ~= nil  then 
            Skill_info2=caster:GetSkillInfo(510203):GetParam("secondHandPro")
    else    Skill_info2= 0
    end

    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
          damage        = (math.max((2*atk+(wuqi_atk/2+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
          damage_real   = (math.max((2*atk+(wuqi_atk/2+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
          damage_result:GetCount()
          damage2P      = (math.max((2*atk+(wuqi_atk/2+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
          damage_real2P = (math.max((2*atk+(wuqi_atk/2+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
          
   local temp           = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          damage2P      = math.max(damage2P,temp)
          damage_real2P = math.max(damage_real2P,temp)
    
    local erDao = caster:GetSkillInfo(500010)
    local erDaoLevel
    if erDao == nil then
        erDaoLevel = 0
    else
        erDaoLevel = erDao:Level()
    end
    
	local luaEnv = caster:GetLuaEnv()
	
    if Weapon_type == 16 then
        if args.result_index_ == 0 then
            if  (damage_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI) then
				luaEnv:SetInt("QuanrenCri",1)
                damage_result:SetResult(damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
                damage_result:SetRealResult(damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
                damage_result:SetAttrAddition(element-1)
                return
            else
                damage_result:SetResult(damage)
                damage_result:SetRealResult(damage_real)
                damage_result:SetAttrAddition(element-1)
                -- print("打第一次")
                return
            end
        elseif args.result_index_ == 1 then
			local mainHandCri = luaEnv:GetInt("QuanrenCri")
            if  mainHandCri == 1 then
                damage_result:SetResult(1+damage*0.02*erDaoLevel*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
                damage_result:SetRealResult(1+damage*0.02*erDaoLevel*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
                damage_result:SetAttrAddition(element-1)
				damage_result:SetType(ROGameLibs.DAMAGE_TYPE_CRI)
				luaEnv:SetInt("QuanrenCri",0)
                return
            else
                damage_result:SetResult(1+damage*0.02*erDaoLevel)
                damage_result:SetRealResult(1+damage*0.02*erDaoLevel)
                damage_result:SetAttrAddition(element-1)
				damage_result:SetType(ROGameLibs.DAMAGE_TYPE_COMMON)
                -- print("打第一次")
                return
            end
        end
    end
    if  (damage_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI) then
            damage_result:SetResult(damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
            damage_result:SetRealResult(damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
            damage_result:SetAttrAddition(element-1)
    elseif Weapon_type == 1 then
        if RORandom:randDouble(0,1) <= Skill_info/10000 then
            damage = damage*(damage_result:GetCount()+1)
            damage_real = damage_real*(damage_result:GetCount()+1)
            damage_result:SetCount(damage_result:GetCount()+1)
            damage_result:SetResult(damage)
            damage_result:SetRealResult(damage_real)
            damage_result:SetAttrAddition(element-1)
        else        
            damage_result:SetResult(damage)
            damage_result:SetRealResult(damage_real)
            damage_result:SetAttrAddition(element-1)
        end
    elseif Weapon_type == 101 then
        if  caster:HasBuff(edjtBuffID) == false then
            if  args.result_index_ == 0 then
                 if RORandom:randDouble(0,1) <= Skill_info/10000 then
                    damage2P = damage2P*(damage_result:GetCount()+1)
                    damage_real2P = damage_real2P*(damage_result:GetCount()+1)
                    damage_result:SetCount(damage_result:GetCount()+1)
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 else       
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 end
            elseif  args.result_index_ == 1 then                
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
            end
        elseif caster:HasBuff(edjtBuffID) == true then
            if  args.result_index_ == 0 then
                 if RORandom:randDouble(0,1) <= Skill_info/10000 then
                    damage2P = damage2P*(damage_result:GetCount()+1)
                    damage_real2P = damage_real2P*(damage_result:GetCount()+1)
                    damage_result:SetCount(damage_result:GetCount()+1)
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 else       
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 end
            elseif  args.result_index_ == 1 then                
                 if RORandom:randDouble(0,1) <= Skill_info2/10000 then
                    damage2P = damage2P*(damage_result:GetCount()+1)
                    damage_real2P = damage_real2P*(damage_result:GetCount()+1)
                    damage_result:SetCount(damage_result:GetCount()+1)
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 else       
                    damage_result:SetResult(damage2P)
                    damage_result:SetRealResult(damage_real2P)
                    damage_result:SetAttrAddition(element-1)
                 end
            end
        end
    elseif Weapon_type == 102 then
            damage_result:SetResult(damage2P)
            damage_result:SetRealResult(damage_real2P)
            damage_result:SetAttrAddition(element-1)
    elseif Weapon_type == 103 then
        if  args.result_index_ == 0 then
            if RORandom:randDouble(0,1) <= Skill_info/10000 then
                damage2P = damage2P*(damage_result:GetCount()+1)
                damage_real2P = damage_real2P*(damage_result:GetCount()+1)
                damage_result:SetCount(damage_result:GetCount()+1)
                damage_result:SetResult(damage2P)
                damage_result:SetRealResult(damage_real2P)
                damage_result:SetAttrAddition(element-1)
            else        
                damage_result:SetResult(damage2P)
                damage_result:SetRealResult(damage_real2P)
                damage_result:SetAttrAddition(element-1)
            end
        elseif  args.result_index_ == 1 then
            damage_result:SetResult(damage2P)
            damage_result:SetRealResult(damage_real2P)
            damage_result:SetAttrAddition(element-1)
        end
    elseif Weapon_type == 104 and caster:HasBuff(edjtBuffID) == true then
        if  args.result_index_ == 0 then
            damage_result:SetResult(damage2P)
            damage_result:SetRealResult(damage_real2P)
            damage_result:SetAttrAddition(element-1)
        elseif  args.result_index_ == 1 then
            if RORandom:randDouble(0,1) <= Skill_info2/10000 then
                damage2P = damage2P*(damage_result:GetCount()+1)
                damage_real2P = damage_real2P*(damage_result:GetCount()+1)
                damage_result:SetCount(damage_result:GetCount()+1)
                damage_result:SetResult(damage2P)
                damage_result:SetRealResult(damage_real2P)
                damage_result:SetAttrAddition(element-1)
            else        
                damage_result:SetResult(damage2P)
                damage_result:SetRealResult(damage_real2P)
                damage_result:SetAttrAddition(element-1)
            end
        end
    else
        damage_result:SetResult(damage2P)
        damage_result:SetRealResult(damage_real2P)
        damage_result:SetAttrAddition(element-1)
    end
    
end

--火球术技能伤害计算(根据不同的距离造成的伤害不一样)(20200612CBT2法师火球术改为使用普通魔法伤害公式)
function DamageCalculator.FireBall(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)   
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local skill_distance = args.distance_
    local damage        = 0
    local damage_real   = 0
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
    if skill_distance>=0 and skill_distance<=1.5 then
       damage       = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*mdef*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float+skill_num*Dam_float)*pvpdam
       damage_real  = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float)*pvpdam
    else
       damage       = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*mdef*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float*0.75+skill_num*Dam_float)*pvpdam
       damage_real  = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float*0.75+skill_num*Dam_float)*pvpdam
    end
    
    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--火焰之壁
function DamageCalculator.FireWall(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    
    if  target:HasBuff(2013) == true then 
        damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
        return
    end
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	
    local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	
	--判断是否拥有法师勋章1
	local oriBuffID = 8100180
	local pro = 0.3
	local randomNum = RORandom:randDouble(0,1)
	local checkBuffID = 8100181
	local deleteBuffID = 810018201
	local skillAttr = args.attack_type_
	if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
		local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		local buffID = checkBuffID * 100 + buffLvl
		if target:HasBuff(checkBuffID) == false then
			utilTool:LuaAddBuff(caster, target, buffID, true)
		elseif target:HasBuff(checkBuffID) == true then
			utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
		end
    end
		
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--陨石术
function DamageCalculator.YunShiShu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--释放的陨石术等级
	local Skill_Lv = args.skill_:Level()
	local SkillResultCount = {4,5,5,6,6,7,7,8,8,9}
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000/SkillResultCount[Skill_Lv]
	
	local TempIncDam = 1
	--贝鲁兹3023减伤
	local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	if owner:HasBuff(67633) == true then	--贝鲁兹 陨石链式反应 效果
		local belluz3023Lv = owner:GetBuff(67633):GetBuffLevel()
		local belluz3023ID = 67633 * 100 + belluz3023Lv
		local belluz3023ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3023ID,false)
		TempIncDam = TempIncDam-belluz3023ConfigData:GetParam("rate")/10000
    end
	
	--余烬烈焰法杖加成
	skill_per = skill_per * (1+owner:GetFightTempAttr("EQUIP_YJLY_YUNSHIDAM")/10000)
	
	
	local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float*TempIncDam+skill_num*Dam_float*TempIncDam+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float*TempIncDam+skill_num*Dam_float*TempIncDam+Additional_MATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        --判断是否拥有法师勋章1(暂废弃)
	-- local oriBuffID = 8100180
	-- local pro = 0.3
	-- local randomNum = RORandom:randDouble(0,1)
	-- local checkBuffID = 8100181
	-- local deleteBuffID = 810018201
	-- local skillAttr = args.attack_type_
	-- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
		-- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		-- local buffID = checkBuffID * 100 + buffLvl
		-- if target:HasBuff(checkBuffID) == false then
			-- utilTool:LuaAddBuff(caster, target, buffID, true)
		-- elseif target:HasBuff(checkBuffID) == true then
			-- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
		-- end
    -- end
	
		
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--火柱攻击
function DamageCalculator.HuoZhuGongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	local TempIncDam = 1
	--贝鲁兹3007增伤
    local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	if owner:HasBuff(67615) == true then	--贝鲁兹 火柱增幅协议 效果
		local belluz3007Lv = owner:GetBuff(67615):GetBuffLevel()
		local belluz3007ID = 67615 * 100 + belluz3007Lv
		local belluz3007ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3007ID,false)
		TempIncDam =TempIncDam + belluz3007ConfigData:GetParam("rate")/10000
    end 
	
    local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per)+skill_num)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per)+skill_num)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+Additional_MATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	
	--判断是否拥有法师勋章1(已废弃)
	-- local oriBuffID = 8100180
	-- local pro = 0.3
	-- local randomNum = RORandom:randDouble(0,1)
	-- local checkBuffID = 8100181
	-- local deleteBuffID = 810018201
	-- local skillAttr = args.attack_type_
	-- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
		-- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		-- local buffID = checkBuffID * 100 + buffLvl
		-- if target:HasBuff(checkBuffID) == false then
			-- utilTool:LuaAddBuff(caster, target, buffID, true)
		-- elseif target:HasBuff(checkBuffID) == true then
			-- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
		-- end
    -- end  
    


    damage_result:SetResult(damage)
    damage_result:SetRealResult(damage_real)
    damage_result:SetAttrAddition(element-1)
end

--暴风雪
function DamageCalculator.Blizzard(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	local TempIncDam = 1
	if caster:HasBuff(67616) == true then	--贝鲁兹 暴风增幅 效果
        local belluz3009Lv = caster:GetBuff(67616):GetBuffLevel()
		local belluz3009ID = 67616 * 100 + belluz3009Lv
		local belluz3009ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3009ID,false)
		TempIncDam =TempIncDam+ belluz3009ConfigData:GetParam("rate")/10000
	end
    local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
    
    if  target:CheckBuffState(301) == true then     --冰冻状态下不造成伤害
        damage_result:SetResult(0)
        damage_result:SetRealResult(0)
    else
        damage_result:SetResult(damage) 
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
    end
end

--造成目标百分比血量伤害
function DamageCalculator.MonsterBoom(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)   
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local HP_Max 		= args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damage 		= HP_Max*skill_per*element
    local damage_real 	= HP_Max*skill_per*element
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--病毒扩散计算(已废弃)
function DamageCalculator.DuXingBaoFa(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    if 	args.result_index_ == 0 or args.result_index_ == 2 then
        damage_result:SetResult(0)
        damage_result:SetRealResult(0)
        return
    end
    local Buff_Lv 	= 0
	local Skill_Lv 	= 0
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    
    
    if caster:HasBuff(3012) == true then
        Skill_Lv = args.skill_:Level()
        Buff_Lv = 300300 + Skill_Lv
        utilTool:LuaAddBuff(caster,target,Buff_Lv,true)
    end
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--大炮和守护者伤害公式
function DamageCalculator.ZhanChang(utilTool, caster,caster_snapshot, target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage 	= 0
	local HP_Max	= args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL) 
	local temp		= 1*damage_result:GetCount()
	local race_tar 	= args:GetTargetAttr(AttrType.ATTR_SPECIAL_RACE)
	if	skill_num == nil then
		skill_num = 0
	end
	
	if 		target:GetUnitTypeLevel() == -1 then
			damage 		=	HP_Max*skill_per
	elseif	race_tar 	==  9 then
			damage		= 1
	else	damage		=	skill_num
	end
		
		damage		= math.max(damage,temp)
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage)
end

--闪电冲击主动
function DamageCalculator.ShanDianChongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local Int	=args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	local Dex	=args:GetCasterAttr(AttrType.ATTR_SPECIAL_DEX)
	-- local Skill_LV
	--local Hit_Num = args.aoe_opponents_count_	
	local Skill_atk
	local Skill_LVA
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local Skill_LVB
	local Skill_LVC
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	if caster:InPvPScene() == true then--PVP中闪电冲击计算防御
	else
		def = 1
	end
   --钢制喙增加ATK读取
	-- if 		caster:GetSkillInfo(710107) ~= nil then
			-- Skill_LV 	= caster:GetSkillInfo(710107):Level()
	-- else 	Skill_LV	= 0
	-- end
	--钢制喙增加ATK读取
	if 		caster:GetSkillInfo(710107) ~= nil then
			Skill_atk = caster:GetSkillInfo(710107):GetParam("ATK")
			Skill_LVA = caster:GetSkillInfo(710107):Level()
	else 	Skill_atk	= 0
			Skill_LVA	= 0
	end
	
	 --闪电冲击等级
	if 		caster:GetSkillInfo(710125) ~= nil then
			Skill_LVB 	= caster:GetSkillInfo(710125):Level()
	else 	Skill_LVB	= 0
	end
	
	--闪电冲击暴击等级
	if 		caster:GetSkillInfo(710207) ~= nil then
			Skill_LVC 	= caster:GetSkillInfo(710207):Level()
	else 	Skill_LVC	= 0
	end

	 --爆伤计算  
    local cri_dam_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_CRI_DAM)             
    local t_cri_damdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_CRI_DEF)
	
	--鹰隼判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_YSZG_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk+jinglian_matk*(1+matk_per))
	
	if caster:GetBuff(60049) ~= nil then
		local belluz6011 = caster:GetBuff(60049)	--贝鲁兹 猎鹰亲和 效果
		local belluz6011Level = belluz6011:GetBuffLevel()
		local belluz6011ID = 60049 * 100 + belluz6011Level
		local belluz6011ConfigData = utilTool:LuaGetBuffTableConfigData(belluz6011ID,false)
		local rate = belluz6011ConfigData:GetParam("rate")/10000
		TempAddDam = TempAddDam +(2*atk+(wuqi_atk+jinglian_atk*(1+atk_per)))*rate
	end
	
	local damage 		=(((80+6*Skill_LVA+2*(Dex/3)+2*(Int/2)+Skill_atk)*math.max((1+atk_up/10000-atk_down/10000),0.1)*(Skill_LVB/2)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per+TempAddDam)*def*Dam_float+skill_per*Skill_LVB*10*Dam_float+Additional_MATK)*pvpdam
	local damage_real	=(((80+6*Skill_LVA+2*(Dex/3)+2*(Int/2)+Skill_atk)*math.max((1+atk_up/10000-atk_down/10000),0.1)*(Skill_LVB/2)*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per+TempAddDam)*Dam_float+skill_per*Skill_LVB*10*Dam_float+Additional_MATK)*pvpdam
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)

	--20200612CBT2爆裂喙的暴击概率只继承人物的1/4
	local cri		= args:GetCasterAttr(AttrType.ATTR_BASIC_CRI_FINAL)/4
	
	--乌勒尔的决意改变暴击倍率
	local WLEDJYMark	=	args:GetFightTempAttr("EQUIP_WLEDJY_JL15MARK")
	if WLEDJYMark == 1 then
		cri = cri*4
	end
	local cri_res	= args:GetTargetAttr(AttrType.ATTR_BASIC_CRI_RES_FINAL)
    local cri_final=(cri - cri_res)/100
    if	target:CheckBuffState(ROGameLibs.kBuffStateType_Sleep)       == true then
		cri_final = (cri - cri_res)/100*2	--目标处于睡眠状态则进行的第一次物理攻击，暴击值翻倍；
    end
		cri_final = math.max(math.min(cri_final,1),0)
	--暴击伤害计算
	if caster:HasBuff(4048) == true and RORandom:randDouble(0,1) <= cri_final then
		--乌勒尔的决意改变暴击伤害倍率
		if WLEDJYMark == 1 then
			cri_dam_per = cri_dam_per+3000
		end
		damage_result:SetType(2)
		damage = damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000+Skill_LVC*1/100)
		damage_real = damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000+Skill_LVC*1/100)
        damage_result:SetCount(1)
	end
	damage_result:SetResult(damage)
    damage_result:SetRealResult(damage_real)
	damage_result:SetAttrAddition(element-1)		
end

--音速投掷计算
function DamageCalculator.YinSuTouZhi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end 
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage_up = 0 
    if caster:GetSkillInfo(510104) ~= nil then
        damage_up = caster:GetSkillInfo(510104):GetParam("Damage")/10000
    end
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	
    local damage        = ((math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float)*(1+damage_up)+Additional_ATK)*pvpdam
    local damage_real   = ((math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float)*(1+damage_up)+Additional_ATK)*pvpdam
    
    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--深红切割计算
function DamageCalculator.ShenHongQieGe(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end 
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage_up = 0 
    if caster:GetSkillInfo(510116) ~= nil then
        damage_up = caster:GetSkillInfo(510116):GetParam("Damage")/10000
		if caster:HasBuff(3039) == true then
		size=1
		end
	end
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local damage        = ((math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float)*(1+damage_up)+Additional_ATK)*pvpdam
    local damage_real   = ((math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float)*(1+damage_up)+Additional_ATK)*pvpdam
	 
    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--转生术计算
function DamageCalculator.ZhuanShengShu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    
	local Enemy_Type = args:GetTargetAttr(AttrType.ATTR_SPECIAL_DEF_ATTR)
	local LUK = args:GetCasterAttr(AttrType.ATTR_SPECIAL_LUK)
	local INT = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	local BaseLv = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local Enemy_HP_Present = args:GetTargetAttr(AttrType.ATTR_BASIC_HP)
	local Enemy_HP_Max = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
	local damage = 0
	--保底buff
	local buff = caster:GetBuff(1060)
	local buffCount = 0
	
	if buff ~= nil then
		buffCount = buff:GetAppendCount()
	end
	-- utilTool:LuaLog("Enemy_Type:"..tonumber(Enemy_Type))
    if Enemy_Type%100 == 9 then
    -- if Enemy_Type == 9 then
		local SLv = args.skill_:Level()
		local Pro = ((20*SLv+LUK+INT+math.max(BaseLv,1)+(1-Enemy_HP_Present/Enemy_HP_Max)*200)/10)*100
        -- local SLv = 10
		--贝鲁兹4015 几率加成
		if caster:HasBuff(65016) == true then --贝鲁兹 神恩转生 效果
			local belluz4015Lv = caster:GetBuff(65016):GetBuffLevel()
			local belluz4015ID = 65016 * 100 + belluz4015Lv
			local belluz4015ConfigData = utilTool:LuaGetBuffTableConfigData(belluz4015ID,false)
			local belluz4015Rate = belluz4015ConfigData:GetParam("rate")
			Pro = Pro + belluz4015Rate
		end
			
		-- utilTool:LuaLog("Pro:"..tonumber(Pro))	
		local proNew = Pro + 100*buffCount*5
		damage = (math.max(BaseLv,1)+INT+10*SLv)*10000+Additional_MATK
		
		if Pro >= 7000 then
			proNew = 7000+100*buffCount*5
		elseif Pro <= 2*SLv*100 then
			proNew = 2*SLv*100
		end
		
		if RORandom:randDouble(0,10000) <= proNew then
			if target:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_Normal 
				or target:GetUnitTypeLevel() == ROGameLibs.kUnitTypeLevel_ELITE 
				or target:GetUnitTypeLevel() == 6 
				or target:GetUnitTypeLevel() == 7 
				or target:GetUnitTypeLevel() == 9 
				or target:GetUnitTypeLevel() == 10 then
				damage = Enemy_HP_Max
			else
				damage = math.max(BaseLv,1)+INT+10*SLv
			end
			utilTool:LuaAddBuff(caster, caster, 106101, true)
		else
			damage = math.max(BaseLv,1)+INT+10*SLv
			utilTool:LuaAddBuff(caster, caster, 106001, true)
		end
		-- utilTool:LuaLog("Pro:"..tonumber(Pro))	
    else
        damage = 0
    end
	-- utilTool:LuaLog("damage:"..tonumber(damage))
    damage_result:SetResult(damage)
    damage_result:SetRealResult(damage)
    damage_result:SetAttrAddition(element-1)
end

-- 连刺攻击
function DamageCalculator.LianCiGongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	--获取怪物体型
	local target_size   = args:GetTargetAttr(AttrType.ATTR_SPECIAL_SIZE)
	local Skill_LV
	
	if 		caster:GetSkillInfo(210202) ~= nil then
			Skill_LV 	= caster:GetSkillInfo(210202):Level()
	else	Skill_LV	= 0
	end
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	local count_num		= 0
    local damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    
	--灵气剑buff
	if caster:HasBuff(156) == true then
		damage		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
	end
	
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	
	local belluz2009 = caster:GetBuff(60035)
	--贝鲁兹2009
	if belluz2009 == nil then	--贝鲁兹 连刺增幅 效果
		if  target_size == 0 then
			count_num = 2
		elseif target_size == 1 then
			count_num = 3
		else
			count_num = 1
		end
	else
		local belluz2009Lev = belluz2009:GetBuffLevel()
		local belluz2009ID = 60035 * 100 + belluz2009Lev
		local belluz2009ConfigData = utilTool:LuaGetBuffTableConfigData(belluz2009ID,false)
		local belluz2009Pro = belluz2009ConfigData:GetParam("pro")/10000
		if RORandom:randDouble(0,1) <= belluz2009Pro then
			if  target_size == 0 then
				count_num = 3
			elseif target_size == 1 then
				count_num = 4
			else
				count_num = 2
			end
		else
			if  target_size == 0 then
				count_num = 2
			elseif target_size == 1 then
				count_num = 3
			else
				count_num = 1
			end
		end
	end
	-- print("count_num"..count_num)
	
	damage = damage * count_num
	damage_real = damage_real * count_num
	
	damage_result:SetResult(damage)
	damage_result:SetRealResult(damage_real)
	damage_result:SetAttrAddition(element-1)
	
	damage_result:SetCount(count_num)
	
	-- 计算各个体型对应的额外result
	-- if 	args.result_index_ == 0 then
		-- damage_result:SetResult(damage)
		-- damage_result:SetRealResult(damage_real)
		-- damage_result:SetAttrAddition(element-1)
		-- print("打第一次")
		-- return
    -- end
	-- if 	args.result_index_ == 1 then
        -- if count_num == 2 or count_num == 3 then
			-- damage_result:SetResult(damage)
			-- damage_result:SetRealResult(damage_real)
			-- damage_result:SetAttrAddition(element-1)
			-- print("打第二次")
			-- return
		-- else
			-- damage_result:SetResult(0)
			-- damage_result:SetRealResult(0)
			-- damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
			-- return
		-- end
    -- end
	-- if 	args.result_index_ == 2 then
        -- if count_num == 3 then
			-- damage_result:SetResult(damage)
			-- damage_result:SetRealResult(damage_real)
			-- damage_result:SetAttrAddition(element-1)
			-- print("打第三次")
			-- return
		-- else
			-- damage_result:SetResult(0)
			-- damage_result:SetRealResult(0)
			-- damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
			-- return
		-- end
    -- end
	-- print("count_num"..count_num)	  
    
end

--骑乘攻击技能计算
function DamageCalculator.QiChengGongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	--计算技能释放者于受击目标的距离
	local Skill_Distance = ROGameLibs.Vector3:HorizontaledDistance(caster:GetPosition(),target:GetPosition())
	local Skill_Lv = args.skill_:Level()
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	local QiChengGongJi_damage1 = {16000,18000,20000,22000,24000,26000,32000,35500,39000,46000}
	local QiChengGongJi_damage2 = {16000,18000,20000,22000,18000,20000,28000,31000,34000,40000}
	local QiChengGongJi_damage3 = {    0,    0,    0,16000,18000,20000,20000,22000,24000,33000}
	local QiChengGongJi_damage4 = {    0,    0,    0,    0,    0,    0,20000,22000,24000,26000}
	
	if args.result_index_ == 0 then
		--贝鲁兹2011
		local belluz2011 = caster:GetBuff(60037)
		if belluz2011 ~= nil then
			local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1))*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local temp = 1*damage_result:GetCount()
			damage = math.max(damage,temp)
			damage_real	= math.max(damage_real,temp)
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
			return
		end
		if Skill_Distance >= 0 and Skill_Distance <= 2 then
			if QiChengGongJi_damage1[Skill_Lv]==0 then--超出有效伤害距离，应该没有伤害
				skill_num=0
				Additional_ATK=0
			end
			local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1))*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local temp = 1*damage_result:GetCount()
			damage = math.max(damage,temp)
			damage_real	= math.max(damage_real,temp)
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
			return
		elseif Skill_Distance > 2 and Skill_Distance <= 3 then
			if QiChengGongJi_damage2[Skill_Lv]==0 then
				skill_num=0
				Additional_ATK=0
			end
			local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage2[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage2[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1))*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local temp = 1*damage_result:GetCount()
			damage = math.max(damage,temp)
			damage_real	= math.max(damage_real,temp)
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
			return
		elseif Skill_Distance > 3 and Skill_Distance <= 4 then
			if QiChengGongJi_damage3[Skill_Lv]==0 then
				skill_num=0
				Additional_ATK=0
			end
			local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage3[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage3[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1))*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage = math.max(damage,0)--在该距离是可能打出伤害为0的
			damage_real	= math.max(damage_real,0)
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			if damage~=0 then
				damage_result:SetAttrAddition(element-1)
			else
				damage_result:SetAttrAddition(0)
			end
			return
		elseif Skill_Distance > 4 then
			if QiChengGongJi_damage4[Skill_Lv]==0 then
				skill_num=0
				Additional_ATK=0
			end
			local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage4[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage4[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1))*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
			damage = math.max(damage,0)--在该距离是可能打出伤害为0的
			damage_real	= math.max(damage_real,0)
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			if damage~=0 then
				damage_result:SetAttrAddition(element-1)
			else
				damage_result:SetAttrAddition(0)
			end
			return
		end
	--贝鲁兹2010
	elseif args.result_index_ == 1 then	--贝鲁兹 骑乘引力模块 效果(已更换骑乘攻击伤害计算方式)
		local belluz2010 = caster:GetBuff(60036)
		if belluz2010 == nil then
			damage_result:IgnoreBeatBack()
			return
		else
			return
		end
	end	
end

--骑乘攻击技能计算
function DamageCalculator.MonsterQiChengGongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	--计算技能释放者于受击目标的距离
	local Skill_Distance = ROGameLibs.Vector3:HorizontaledDistance(caster:GetPosition(),target:GetPosition())
	local Skill_Lv = 1
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	--怪物等级压制系数
	local SuppressRatio = 1
	if args.damage_suppress_ratio_ ~= nil then
		SuppressRatio = 1 + args.damage_suppress_ratio_ / 10000
	end
	local QiChengGongJi_damage1 = {46000}
	local QiChengGongJi_damage2 = {40000}
	local QiChengGongJi_damage3 = {33000}
	local QiChengGongJi_damage4 = {26000}
	
	
	if Skill_Distance >= 0 and Skill_Distance <= 2 then
		local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage1[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local temp = 1*damage_result:GetCount()
		damage = math.max(damage,temp)
		damage_real	= math.max(damage_real,temp)
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		return
	elseif Skill_Distance > 2 and Skill_Distance <= 3 then
		local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage2[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage2[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local temp = 1*damage_result:GetCount()
		damage = math.max(damage,temp)
		damage_real	= math.max(damage_real,temp)
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		return
	elseif Skill_Distance > 3 and Skill_Distance <= 4 then
		local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage3[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage3[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local temp = 1*damage_result:GetCount()
		damage = math.max(damage,temp)
		damage_real	= math.max(damage_real,temp)
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		return
	elseif Skill_Distance > 4 then
		local damage 		= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*QiChengGongJi_damage4[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio-def_suzhi)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local damage_real 	= (math.floor((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*QiChengGongJi_damage4[Skill_Lv]/10000*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)*SuppressRatio)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		local temp = 1*damage_result:GetCount()
		damage = math.max(damage,temp)
		damage_real	= math.max(damage_real,temp)
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		return
	end
	
end

--基尔瑟炸弹
function DamageCalculator.JiErSeBoom(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local HP_Max 		= args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damage 		= HP_Max * skill_per
    local damage_real 	= HP_Max * skill_per
	if 	target:HasBuff(9020) == true then
	  	return
	else   
	   damage_result:SetResult(damage)
       damage_result:SetRealResult(damage_real)
    end
end

--毒性感染
function DamageCalculator.DuXingGanRan(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local Id = args.skill_:SkillEffectID()
	local damage
	local damage_real
	local rateList = {700,900,1100,1200,1300}
	local checkBuffID = 3003
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)

	damage 		=  (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
	damage_real =  (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	if caster_snapshot:HasBuff(8100170) == true and target:HasBuff(checkBuffID) == true then	  
        local buffLvl = caster_snapshot:GetBuffAttr(8100170, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
		local rate = rateList[buffLvl]/10000
		damage = damage * (1+rate)
		damage_real = damage_real * (1+rate)
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
	else
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
	end
end

--天地树叶子
function DamageCalculator.TianDiShuYeZi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local curHp = args:GetTargetAttr(AttrType.ATTR_BASIC_HP)
	local itemNum = caster:GetItemNum(2031004)
	if curHp <= 0 and itemNum >= utilTool:ToLong(1) then
		caster:CostItem(2031004,1)
		utilTool:LuaAddBuff(caster, target, 100801, true)
	else
		return
	end
	
	
end

--商人体验手推车攻击	
function DamageCalculator.ShouTuiCheGongJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    
		  
    if 	caster:HasBuff(33425) == true then
	    damage_result:SetResult(4*damage)
        damage_result:SetRealResult(4*damage_real)
		damage_result:SetAttrAddition(element-1)
	else
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
    end
end

--奶油伤害计算
function DamageCalculator.NaiYouDamage(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local damage 		= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*mdef*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
        if  target:HasBuff(9295) == false then
		     utilTool:LuaCountGuildSceneSkillHit(caster)
        end
end

--泰山压顶
function DamageCalculator.TaiShanYaDing(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local damage 		= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*mdef*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
    local damage_real 	= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)

		if damage > 0 then	
			utilTool:LuaAddBuff(caster, caster, 3326001, true)	
		end
		damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--螺旋刺击
function DamageCalculator.ClashingSpiral(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	
	local size_tar		= args:GetTargetAttr(AttrType.ATTR_SPECIAL_SIZE)
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local weightEquip	= ROLuaCommonFunction.GetEquipWeightByPos(caster,0) --取主武器的重量
	local damage
	local damage_real
	local Skill_LV
	local Skill_LVB
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	if weightEquip == nil then
		weightEquip = 0
	else
		weightEquip = weightEquip
	end
	
	if 		caster:GetSkillInfo(210205) ~= nil then
			Skill_LV 	= caster:GetSkillInfo(210205):Level()/10
	else	Skill_LV	= 0
	end
	
	if 		caster:GetSkillInfo(210202) ~= nil then
			Skill_LVB 	= caster:GetSkillInfo(210202):Level()
	else	Skill_LVB	= 0
	end
	
	--螺旋击刺特殊体型修正
	if size_tar == 2 then
	size = 1.25
	elseif size_tar == 0 then
	size = 1
	else
	size = 0.75
	end
	
		damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per)+weightEquip*Skill_LV)*element*size*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per)+weightEquip*Skill_LV)*element*size*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

	--灵气剑buff
	if caster:HasBuff(156) == true then
		damage		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per)+weightEquip*Skill_LV)*element*size*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LVB*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per)+weightEquip*Skill_LV)*element*size*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float
					+ math.max((Skill_LVB*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
	end
	
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--魔击术
function DamageCalculator.StaveCrasher(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local damage
	local damage_real
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
		--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	
		damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
		damage_real	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--心灵震波
function DamageCalculator.SoulDestroyer(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)	
	local damage
	local damage_real
	local INT = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
		
	local yueLingDuanJianIntUp = 1 + args:GetFightTempAttr("EQUIP_YLDJ_INTDAMUP")/10000+args:GetFightTempAttr("EQUIP_YLDJ_REFINEDAMUP")/10000
	local yueChenDuanJianRate = args:GetFightTempAttr("EQUIP_YCDJ_EXDAMRATE")/10000+args:GetFightTempAttr("EQUIP_YCDJ_REFINERATE")/10000
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
		damage 		= (math.max((wuqi_atk*skill_per*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+math.max((INT*skill_per*yueLingDuanJianIntUp*5+(matk+wuqi_matk+jinglian_matk*(1+matk_per))*yueChenDuanJianRate*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max((wuqi_atk*skill_per*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+math.max(INT*skill_per*yueLingDuanJianIntUp*5+(matk+wuqi_matk+jinglian_matk*(1+matk_per))*yueChenDuanJianRate*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+Additional_ATK)*pvpdam
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--锐利射击
function DamageCalculator.FocusedArrowStrike(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculateCommonSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	local damage
	local damage_real
		  damage 		= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*def*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		  damage_real	= (math.max((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
	  
		--爆伤计算  
    local cri_dam_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_CRI_DAM)             
    local t_cri_damdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_CRI_DEF)  
	
	if  (damage_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI) then
			damage_result:SetResult(damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
			damage_result:SetRealResult(damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
			damage_result:SetAttrAddition(element-1)
	else	damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
	end

end

--圣之裁决
function DamageCalculator.ShengZhiCaiJue(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculateCommonSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	local damage
	local damage_real
	
	--圣锤十字星判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_SCSZX_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--魔锤安魂曲判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_MSAHQ_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)
	
	
		  damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)
*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		  damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam

	  
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
          damage_result:SetResult(damage)
          damage_result:SetRealResult(damage_real)
		  damage_result:SetAttrAddition(element-1)
     
	--belluz圣之裁决暴击判定
	    if caster:HasBuff(65025) == true then		--贝鲁兹 裁决增幅 效果
	        local cri_dam_per       = args:GetCasterAttr(AttrType.ATTR_PERCENT_CRI_DAM)             
            local t_cri_damdef_per  = args:GetTargetAttr(AttrType.ATTR_PERCENT_CRI_DEF)
			
		    if  (damage_result:GetType() == ROGameLibs.DAMAGE_TYPE_CRI) then
					damage_result:SetResult(damage*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
					damage_result:SetRealResult(damage_real*1.5*(1+cri_dam_per/10000-t_cri_damdef_per/10000))
					damage_result:SetAttrAddition(element-1)
					---utilTool:LuaLog("222")
	     	else	damage_result:SetResult(damage)
					damage_result:SetRealResult(damage_real)
					damage_result:SetAttrAddition(element-1)
					---utilTool:LuaLog("333")
					---utilTool:LuaLog("type"..tonumber(damage_result:GetType()))
	        end   	
		end
	end	

--塔防：防御塔伤害计算
function DamageCalculator.TowerAttack(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then 
		return
	end
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local damage 		= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max(npc_dam,0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float)*pvpdam
    local damage_real 	= (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max(npc_dam,0.1),0)*Dam_float+skill_num*Dam_float)*pvpdam

    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--海浪冲击
function DamageCalculator.HaiLangBoom(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local HP_Max 		= args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damage 		= HP_Max * skill_per
    local damage_real 	= HP_Max * skill_per
	if 	target:HasBuff(33549) == true then
	   damage_result:SetResult(damage*0.2)
       damage_result:SetRealResult(damage_real*0.2)
	else   
	   damage_result:SetResult(damage)
       damage_result:SetRealResult(damage_real)
    end
end

--天地树：神圣之怒
function DamageCalculator.ShenShengZhiNu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local total_int     = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
    --local def_type      = args.defense_type_  
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local Hit_Num = args.aoe_opponents_count_

    local damage 		= 0
	
	if caster:GetBuff(51543) ~= nil then--取神圣之怒buff等级
		local buffLv = caster:GetBuff(51543):GetBuffLevel()
		skill_per = 1 * buffLv
	else
		skill_per = 1
	end
		 --冥想等级
	-- if 		caster:GetSkillInfo(310205) ~= nil then
	-- 		Skill_LVB 	= caster:GetSkillInfo(310205):Level()
	-- else 	Skill_LVB	= 0
	-- end

	damage =((((math.max(BaseLv,1)+total_int)/5*3*skill_per)*(1+Heal_up/10000)*(1+Beheal_up/10000)+4*skill_per/10*(matk+wuqi_matk+jinglian_matk*(1+matk_per)))*Dam_float)
    if Hit_Num <= 1 then
    --对单体造成4倍matk伤害，对群体最低造成1.33倍matk伤害
        damage = damage*damage_result:GetCount()
    else
        damage = damage*damage_result:GetCount()/math.min(3,Hit_Num)
    end
    if  damage <= 1*damage_result:GetCount() then 
        damage = 1*damage_result:GetCount()
    end
	damage_result:SetResult(damage)
	damage_result:SetRealResult(damage)
end

--需要背对的技能
function DamageCalculator.MagicSkillCalcFace(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local targetPos = target:GetPosition()
    local selfPos = caster:GetPosition()
    local targetFace = target:GetFace()
    local faceResult = utilTool:IsFrontOfTarget(targetPos, targetFace, selfPos)
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    if faceResult == true then
		if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
			return
		end
        local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
        local damage        = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*mdef*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
        local damage_real   = (math.max((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam

        local temp          = 1*damage_result:GetCount()
              damage        = math.max(damage,temp)
              damage_real   = math.max(damage_real,temp)

            damage_result:SetResult(damage)
            damage_result:SetRealResult(damage_real)
            damage_result:SetAttrAddition(element-1)
    else
        return
    end
end

--贝鲁兹：1006自爆
function DamageCalculator.BoomByHP(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)

	local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	local hp_max = owner:GetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
		
	local damage 		= hp_max * skill_per
    local damage_real 	= hp_max * skill_per
    
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end

--坚定打击
function DamageCalculator.JianDingDaJi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local Id = args.skill_:SkillEffectID()
	local damage
	local damage_real
	
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)

	--20200612CBT2坚定打击关联VIT伤害
	local totalVIT = args:GetCasterAttr(AttrType.ATTR_SPECIAL_VIT)

	--魔刀吞神效果
	local TempAddDam = args:GetFightTempAttr("EQUIP_MDTS_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--贝鲁兹2016
	if caster:GetBuff(60041) ~= nil then
		local belluz2016Lvl = caster:GetBuff(60041):GetBuffLevel()	--贝鲁兹 坚定打击增幅 效果
		local belluz2016ID = 60041 * 100 + belluz2016Lvl
		local belluz2016ConfigData = utilTool:LuaGetBuffTableConfigData(belluz2016ID,false)
		local belluz2016Rate = belluz2016ConfigData:GetParam("rate")
		local belluz2016Increace = totalVIT * belluz2016Rate
		TempAddDam = TempAddDam + belluz2016Increace
	end
	
	--灵气剑Buff
	local Skill_LV
	if 		caster:GetSkillInfo(210202) ~= nil then
			Skill_LV 	= caster:GetSkillInfo(210202):Level()
	else	Skill_LV	= 0
	end

		
		--灵气剑Buff
	if caster:HasBuff(156) == true then
		damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+totalVIT/300)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+totalVIT/300)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK+ math.max((Skill_LV*20*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float)*pvpdam
	else
		damage 		= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+totalVIT/300)+TempAddDam)*def*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1)-def_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
		damage_real	= (math.max(((2*atk+(wuqi_atk+jinglian_atk*(1+atk_per))*size*element*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1))*skill_per*(1+totalVIT/300)+TempAddDam)*math.max((npc_dam+atk_up/10000-atk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_ATK)*pvpdam
	end
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		

end

--雷鸣术伤害计算
function DamageCalculator.LeiMingShu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	
    local damage        = (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real   = (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    
    -- print("increase:"..tostring(caster:GetAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE)))
    -- print("buff1:"..tostring(caster:GetBuff(60017)))
    -- print("buff2:"..tostring(caster:GetBuff(60018)))

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        --判断是否拥有法师勋章1
    -- local oriBuffID = 8100180
    -- local pro = 0.3
    -- local randomNum = RORandom:randDouble(0,1)
    -- local checkBuffID = 8100181
    -- local deleteBuffID = 810018201
    -- local skillAttr = args.attack_type_
    -- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
        -- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
        -- local buffID = checkBuffID * 100 + buffLvl
        -- if target:HasBuff(checkBuffID) == false then
            -- utilTool:LuaAddBuff(caster, target, buffID, true)
        -- elseif target:HasBuff(checkBuffID) == true then
            -- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
        -- end
    -- end
    local belluz3025 = caster:GetBuff(67634)	--贝鲁兹 阳极雷鸣 效果
    if belluz3025 ~= nil then
        damage_result:IgnoreBeatBack()
    end
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)
end

--崩裂术计算
function DamageCalculator.BengLieShu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
	local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	--巫杖魂咬判定
	local TempAddDam = args:GetFightTempAttr("EQUIP_WZHY_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)/10000
	local TempIncDam =1 
    --belluz3027伤害判定
    if caster:HasBuff(67635) == true then
        local belluz3027Lv = caster:GetBuff(67635):GetBuffLevel()	--贝鲁兹 崩裂催化模块 效果
		local belluz3027ID = 67635 * 100 + belluz3027Lv
		local belluz3027ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3027ID,false)
		TempIncDam = TempIncDam - belluz3027ConfigData:GetParam("rate")/10000
    end 
    
    local damage        = (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float*TempIncDam+Additional_MATK)*pvpdam
    local damage_real   = (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per*TempIncDam+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float*TempIncDam+Additional_MATK)*pvpdam
    
    -- print("increase:"..tostring(caster:GetAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE)))
    -- print("buff1:"..tostring(caster:GetBuff(60017)))
    -- print("buff2:"..tostring(caster:GetBuff(60018)))

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
          
        --判断是否拥有法师勋章1
    -- local oriBuffID = 8100180
    -- local pro = 0.3
    -- local randomNum = RORandom:randDouble(0,1)
    -- local checkBuffID = 8100181
    -- local deleteBuffID = 810018201
    -- local skillAttr = args.attack_type_
    -- if caster_snapshot:HasBuff(oriBuffID) == true and skillAttr == 3 and randomNum <= pro then
        -- local buffLvl = caster_snapshot:GetBuffAttr(oriBuffID, ROGameLibs.BUFF_SNAPSHOT_ATTR_TYPE_LEVEL)
        -- local buffID = checkBuffID * 100 + buffLvl
        -- if target:HasBuff(checkBuffID) == false then
            -- utilTool:LuaAddBuff(caster, target, buffID, true)
        -- elseif target:HasBuff(checkBuffID) == true then
            -- utilTool:LuaAddBuff(caster, target, deleteBuffID, true)
        -- end
    -- end
        
    --print("damage"..damage)
    damage_result:SetResult(damage)
    --print("damage"..damage)
    damage_result:SetRealResult(damage_real)
    damage_result:SetAttrAddition(element-1)
end

--Belluz3026泥沼地伤害计算
function DamageCalculator.NiZaoDi(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local buffLv
    local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
    local damage 
    local damage_real 
    if owner:HasBuff(67645) == true then
        local belluz3026Lv = owner:GetBuff(67645):GetBuffLevel()
		local belluz3026ID = 67645 * 100 + belluz3026Lv
		local belluz3026ConfigData = utilTool:LuaGetBuffTableConfigData(belluz3026ID,false)
		local belluz3026Rate = belluz3026ConfigData:GetParam("rate")/10000
		local belluz3026RateLimit = belluz3026ConfigData:GetParam("ratelimit")/10000
		local targetMaxHp = args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
		damage       = targetMaxHp*belluz3026Rate*pvpdam*Dam_float
		damage_real  = targetMaxHp*belluz3026Rate*pvpdam*Dam_float

		if damage>((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*belluz3026RateLimit*mdef-mdef_suzhi)*pvpdam then
			damage=((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*belluz3026RateLimit*mdef-mdef_suzhi)*pvpdam*Dam_float
			damage_real=(matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*belluz3026RateLimit*pvpdam*Dam_float
		end

    end
    -- print("increase:"..tostring(caster:GetAttr(AttrType.ATTR_PERCENT_MDAM_INCREASE)))
    -- print("buff1:"..tostring(caster:GetBuff(60017)))
    -- print("buff2:"..tostring(caster:GetBuff(60018)))

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
        
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)

end

--Belluz3005陨石术火焰领域
function DamageCalculator.HuoYanLingYu(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
    local IsCommonSkill=false
    local IsMagicSkill=true
    local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
    local buffLv
    local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
    local damage 
    local damage_real 
    local belluz3005List = {0.2, 0.5, 1}
    if owner:HasBuff(67608) == true then
        local buffLv = owner:GetBuff(67608):GetBuffLevel()
        local rate = 0
        rate = belluz3005List[buffLv]
		damage       = ((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*mdef*rate-mdef_suzhi)*pvpdam
		damage_real  = (matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*rate*pvpdam
    end

    local temp          = 1*damage_result:GetCount()
          damage        = math.max(damage,temp)
          damage_real   = math.max(damage_real,temp)
    
        
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
        damage_result:SetAttrAddition(element-1)

end

--装备：黄金之锤
function DamageCalculator.HuangJinZhiChui(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false == DamageCalculator.CalculatePhysicalSkillDamageBase(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	local BaseLv = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
	local TempIncDam  = args:GetCasterAttr(AttrType.ATTR_PERCENT_HOLY_SKILL_ATK)/10000 - args:GetTargetAttr(AttrType.ATTR_PERCENT_HOLY_SKILL_DEF)/10000
	local damage
	local damage_real
	--PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=false
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)

	damage 		= (((BaseLv*skill_per)*element*def+skill_num)*Dam_float*(1+TempIncDam)-def_suzhi)*pvpdam
	damage_real	= (((BaseLv*skill_per)*element+skill_num)*Dam_float*(1+TempIncDam))*pvpdam
	local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
          damage_result:SetResult(damage)
          damage_result:SetRealResult(damage_real)
		  damage_result:SetAttrAddition(element-1)
end

--第二次封印：灭世魔爆
function DamageCalculator.MieShiMoBao(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)   
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    local HP_Max 		= args:GetTargetAttr(AttrType.ATTR_BASIC_MAX_HP_FINAL)
    local damage 		= HP_Max*skill_per*element
    local damage_real 	= HP_Max*skill_per*element
	
	if target ~= nil then
		if target:HasBuff(34321) == true then
			return
		end
	end
	
        damage_result:SetResult(damage)
        damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
end


--装备：痊愈光辉
function DamageCalculator.QuanYuGuangHui(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON)
    local BaseLv        = args:GetCasterAttr(AttrType.ATTR_SPECIAL_BASE_LV)
    local Heal_up       = args:GetCasterAttr(AttrType.ATTR_PERCENT_HEAL_ENC)
    local Beheal_up     = args:GetTargetAttr(AttrType.ATTR_PERCENT_BEHEAL_ENC)
    local def_type      = args.defense_type_  
	local Skill_LVB
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
    --local Skill_Lv    =   caster:GetSkillInfo(300008):Level() --取到治疗术等级   
    local owner = utilTool:LuaGetMobMonsterOwner(caster, true)
	
	if owner == nil then
		return
	end
	
	local damage 		= 0
	 --冥想等级
	if 		owner:GetSkillInfo(310205) ~= nil then
			Skill_LVB 	= owner:GetSkillInfo(310205):Level()
	else 	Skill_LVB	= 0
	end
    local qyghMobRate = owner:GetFightTempAttr("EQUIP_QYGH_MOBRATE")/10000
	local qyghdeheal = owner:GetFightTempAttr("EQUIP_QYGH_DEHEAL")/10000
	local skillper_equip = 1
	skill_per = skill_per *(1+qyghMobRate)
	skillper_equip = math.max(0.1,skillper_equip-qyghdeheal)
	local total_int     = args:GetCasterAttr(AttrType.ATTR_SPECIAL_INT)
		  damage =((((math.max(BaseLv,1)+total_int)/5*3*skill_per)+(10+skill_per/0.3/(1+qyghMobRate))/20*0.3*(1+qyghMobRate)*(matk+wuqi_matk+jinglian_matk*(1+matk_per)))*Dam_float+(1+qyghMobRate)*skill_num*Dam_float)*skillper_equip*(1+Skill_LVB*0.02)*(1+Heal_up/10000)*(1+Beheal_up/10000)

	if  damage <= 1*damage_result:GetCount() then 
        damage = 1*damage_result:GetCount()
    end

    if 	args.result_index_ == 0 then
		if caster:GetUUID() == target:GetUUID() then
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
			return
		end
		
		if owner:GetUUID() == target:GetUUID() then
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
			return
		end
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			damage_result:SetResult(-damage)
			damage_result:SetRealResult(-damage)
		end
	elseif args.result_index_ == 1 then
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
		end
	elseif args.result_index_ == 2 then
		if  def_type%100 == 9 then
			damage_result:SetResult(damage*0.5)
			damage_result:SetRealResult(damage*0.5)
		else
			
			damage_result:SetResult(-damage)
			damage_result:SetRealResult(-damage)
		end
	end
end


-- 审判
function DamageCalculator.ShenPan(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)
	if false==DamageCalculator.DamageCalcBase(caster, caster_snapshot, target, args,damage_result,ROGameLibs.DAMAGE_TYPE_COMMON) then
		return
	end
    local skill_num,skill_per,atk,matk,wuqi_atk,wuqi_matk,atk_per,matk_per,size,size_inc,yuansu,race,element,jinglian_atk,jinglian_matk,def,mdef,npc_dam,def_suzhi,mdef_suzhi,atk_up,matk_up,atk_down,matk_down,Dam_float,Additional_ATK,Additional_MATK = DamageCalculator.Calculatorprocess(utilTool, caster, caster_snapshot,target, damage_info, args, damage_result)   
    --PVP伤害系数
	local IsCommonSkill=false
	local IsMagicSkill=true
	local pvpdam = DamageCalculator.PvpDamProcess(caster ,IsCommonSkill ,IsMagicSkill)
	
	local szylRate = 0
	local LUK = args:GetCasterAttr(AttrType.ATTR_SPECIAL_LUK)
	local szylRandom = RORandom:randDouble(0,1)
	
	--审判圣杖英灵判定3
		local szylPerUpRate = args:GetFightTempAttr("EQUIP_SZJY_PERUPRATE")/10000
		skill_per = skill_per  + szylPerUpRate
	
	local TempAddDam = 0
	
	--圣锤十字星判定
	TempAddDam = args:GetFightTempAttr("EQUIP_SCSZX_EXDAMRATE_SNAPSHOT")*(2*atk+wuqi_atk)/10000
	
	--魔锤安魂曲判定
	TempAddDam = TempAddDam+ args:GetFightTempAttr("EQUIP_MSAHQ_EXDAMRATE_SNAPSHOT")*(matk+wuqi_matk)
	
	local damage 		= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*mdef*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1)-mdef_suzhi,0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
    local damage_real 	= (math.max(((matk+wuqi_matk+jinglian_matk*(1+matk_per))*math.max(1+race,0.1)*math.max(1+size_inc,0.1)*math.max(yuansu,0.1)*element*skill_per+TempAddDam)*math.max((npc_dam+matk_up/10000-matk_down/10000),0.1),0)*Dam_float+skill_num*Dam_float+Additional_MATK)*pvpdam
	
    local temp			= 1*damage_result:GetCount()
		  damage		= math.max(damage,temp)
		  damage_real	= math.max(damage_real,temp)
		  
	--审判圣杖英灵判定1&2
		local szylDamageUp = args:GetFightTempAttr("EQUIP_SZJY_DAMUPRATE")/10000
		local szylBasePer = args:GetFightTempAttr("EQUIP_SZJY_DAMUPBASEPRO")/10000
		local szylLuckRate = args:GetFightTempAttr("EQUIP_SZJY_DAMUPLUKRATE")/10000
		szylRate = szylBasePer + szylLuckRate * LUK / 100

    --圣杖英灵额外result
	if  args.result_index_ == 0 then
		if szylRandom <= szylRate then
			damage = damage *( 1 + szylDamageUp)
			damage_real = damage_real * ( 1 + szylDamageUp)
		end	
		damage_result:SetResult(damage)
		damage_result:SetRealResult(damage_real)
		damage_result:SetAttrAddition(element-1)
		return
	end
	if 	args.result_index_ == 1 then
		if caster:HasBuff(4102330) == true then
			damage_result:SetResult(damage)
			damage_result:SetRealResult(damage_real)
			damage_result:SetAttrAddition(element-1)
			return
		else
			damage_result:SetResult(0)
			damage_result:SetRealResult(0)
			damage_result:SetType(ROGameLibs.DAMAGE_TYPE_HAS_DAMAGED)
			return
		end
	end
end	