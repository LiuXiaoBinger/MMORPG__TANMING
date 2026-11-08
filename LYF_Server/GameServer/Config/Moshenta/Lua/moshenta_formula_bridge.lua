-- 原C++对象的本副本Lua适配边界；DamageCalculator正文保持原文件字节。
function declareGlobal(name, value) _G[name] = value end
-- 使用原字段名称作为适配索引，不更改原脚本中的AttrType引用。
AttrType = setmetatable({}, { __index = function(_, name) return name end })
ROGameLibs = {
    DAMAGE_TYPE_UNKNOW=0, DAMAGE_TYPE_COMMON=1, DAMAGE_TYPE_CRI=2,
    DAMAGE_TYPE_MISS=3, DAMAGE_TYPE_IMMUNE=4, DAMAGE_TYPE_BLOCK=9,
    kBuffStateType_Sleep=203, kBuffStateType_Paralysis=304, kBuffStateType_Stun=401,
    kUnitTypeLevel_BOSS=2, kUnitTypeLevel_Mini=4, kUnitTypeLevel_MVP=5
}
-- 本闭包没有伤害判定前的RO临时Buff叶子；真实Gene面板已在输入中合成。
ROGameLibs.DamageCalculatorUtil = {
    ApplyBuffEffectBeforeCalculate=function() end,
    ApplyBuffEffectBeforeJudge=function() end
}
-- 原skill_trigger_moudle仍执行原表SkillTriggerID==0的返回分支。
Table = { SkillEffectTable = { GetRowById=function(id) return {SkillTriggerID=0} end } }
package.loaded['Table/SkillEffectTable'] = Table.SkillEffectTable
-- 本副本触发函数不查询这两张表；仅满足原文件模块加载，不伪造查询结果。
package.loaded['Table/SkillTable'] = {}
package.loaded['Table/EntityTable'] = {}
-- 所有随机调用共用服务端逻辑线程的随机源，测试可替换为固定序列。
RORandom = { randDouble=function(_, low, high) return low + (high-low)*moshenta_random() end }

-- 将具名属性快照接到原ROLuaEntity所需的最小实际接口。
local function entity(input)
    local value = {input=input}
    function value:GetAttr(name) return self.input.attrs[name] or 0 end
    function value:GetSkillInfo(id) return nil end
    function value:GetBuff(id)
        local level = self.input.buffs[id]
        if level == nil then return nil end
        return {GetBuffLevel=function() return level end}
    end
    function value:HasBuff(id) return self.input.buffs[id] ~= nil end
    function value:CheckBuffState(state) return state==ROGameLibs.kBuffStateType_Stun and self.input.stunned end
    function value:GetAttackerCount() return self.input.attackers end
    function value:InPvPScene() return false end
    function value:GetFightTempAttr(name) return 0 end
    function value:GetUnitTypeLevel() return self.input.unitLevel or 0 end
    function value:GetPosition() return self.input.position end
    function value:GetFace() return self.input.face end
    return value
end

-- 每次真正结算调用原DamageCalculator中配置的函数名。
function MoshentaCalculate(input)
    local caster = entity(input.caster)
    local target = entity(input.target)
    local result = {damage=0,real=0,kind=0,count=input.count,attr=0}
    function result:GetType() return self.kind end
    function result:SetType(value) self.kind=value end
    function result:GetResult() return self.damage end
    function result:SetResult(value)
        if value>=0 then self.damage=math.floor(value) else self.damage=math.ceil(value) end
    end
    function result:SetRealResult(value)
        if value>=0 then self.real=math.floor(value) else self.real=math.ceil(value) end
    end
    function result:GetRealResult() return self.real end
    function result:GetCount() return math.max(1,self.count) end
    function result:SetAttrAddition(value) self.attr=value end
    function result:GetSkillDamageType() return input.damageType end
    local skill = {}
    function skill:SkillID() return input.skillId end
    function skill:SkillEffectID() return input.skillId end
    function skill:SkillTableUUID() return input.skillId end
    function skill:Level() return 1 end
    local args = { skill_=skill, result_index_=input.resultIndex, target_index_=input.targetIndex,
        distance_=input.distance, attack_type_=input.attackType, defense_type_=input.defenseType,
        damage_suppress_ratio_=input.suppress, aoe_opponents_count_=input.opponents,
        calculate_data_=input.calculated }
    function args:GetCasterAttr(name) return caster:GetAttr(name) end
    function args:GetTargetAttr(name) return target:GetAttr(name) end
    function args:GetFightTempAttr(name) return 0 end
    function args:CalculatorSkillCrit()
        self.skill_crit_attr_={caster_cri=caster:GetAttr('ATTR_BASIC_CRI_FINAL'),target_cri_res=target:GetAttr('ATTR_BASIC_CRI_RES_FINAL')}
    end
    function args:CalculatorSkillNotMissAttr()
        self.skill_not_miss_attr_={caster_hit_base=caster:GetAttr('ATTR_BASIC_HIT_FINAL'),target_flee_base=target:GetAttr('ATTR_BASIC_FLEE_FINAL')}
    end
    function args:CalculatorProcess()
        -- 原damage_def.cpp在完成命中/暴击判定后才抽本次伤害浮动。
        self.calculate_data_.dam_float=moshenta_float(RORandom:randDouble(0.97,1.03))
    end
    local util = {}
    function util:LuaGetMobMonsterOwner(value) return caster end
    function util:IsFrontOfTarget(position,face,source)
        local radians=face*math.pi/180
        return (source.x-position.x)*math.sin(radians)+(source.z-position.z)*math.cos(radians)>0
    end
    DamageCalculator[input.formula](util,caster,caster,target,{},args,result)
    return tostring(result.damage)..','..tostring(result.kind)..','..tostring(result.real)..','..tostring(result.count)
end

-- 原Buff添加概率和寿命脚本共用同一个实体适配，不另写近似公式。
function MoshentaBuffRule(input)
    local caster=entity(input.caster)
    local target=entity(input.target)
    local param={GetParam=function(_,name) return input.params[name] or 0 end}
    local duration=input.duration
    local buff={SetBuffTime=function(_,value) duration=value end}
    if input.condition~='' then
        if not BuffAddCondition[input.condition]({},caster,target,param) then return -2 end
    end
    if input.timeRule~='' then BuffTimeCalculator[input.timeRule]({},caster,target,0,buff,param) end
    return duration
end

-- 原光环只读条件，进入者使用当前实际Buff家族。
function MoshentaBuffCondition(input)
    local target=entity(input.target)
    local args={GetLuaEnterEntity=function() return target end}
    local param={GetParam=function(_,name) return input.params[name] or 0 end}
    if BuffTriggerCondition[input.name]({},nil,nil,param,args) then return 1 end
    return 0
end

-- 原Lua主动请求的自杀保留为动作，不依赖零HP机制的扣血结果。
function MoshentaBuffResult(input)
    local caster=entity(input.caster)
    local target=entity(input.target)
    local action=''
    function target:LuaIncAttr(name,amount,firer,force)
        if input.name=='Suicide' then action='suicide' end
    end
    local param={GetParam=function(_,name) return input.params[name] or 0 end}
    BuffTriggerResult[input.name]({},caster,target,param,{})
    return action
end
