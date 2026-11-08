require("Table/SkillEffectTable")
declareGlobal("SkillEffectIDMoudle", {})

SkillEffectIDMoudle.Cache = {}

--此文件专用接口
function SkillEffectIDMoudle.GetMoudle( scriptname )
	scriptname = "skill." .. scriptname
	if SkillEffectIDMoudle.Cache[scriptname] == nil then
		SkillEffectIDMoudle.Cache[scriptname] = require(scriptname)
	end
    return SkillEffectIDMoudle.Cache[scriptname]
end

function SkillEffectIDMoudle.SetUtilTool( utiltool )
	SkillEffectIDMoudle.mUtilTool = utiltool
end


--接口统一参数
--caster 技能释放者
--caster_snapshot 技能释放者属性快照
--target 技能目标
--skill_id 技能id
--skill_uuid 技能uuid
--skill_effect_id 技能特效id
--damage_result DamageResult
--args DamageLuaArgs

--击中目标，伤害计算后 kBuffTTT_Attack = 4
function SkillEffectIDMoudle.OnSkillAttack( caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	local skillEffectTableInfo = Table.SkillEffectTable.GetRowById(skill_effect_id,true)
	if skillEffectTableInfo == nil then
		return false
	end
	if skillEffectTableInfo.SkillTriggerID == 0 then
		return true
	end
	local SkillTriggerIDModule = SkillEffectIDMoudle.GetMoudle(skillEffectTableInfo.SkillTriggerID)
	if SkillTriggerIDModule ~= nil and SkillTriggerIDModule.OnSkillAttack ~= nil then
		return SkillTriggerIDModule.OnSkillAttack(SkillEffectIDMoudle.mUtilTool, caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	end
	return false
end

--攻击时，伤害计算前 kBuffTTT_AttackBeforeCalculate = 11
function SkillEffectIDMoudle.OnSkillAttackBeforeCalculate( caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	local skillEffectTableInfo = Table.SkillEffectTable.GetRowById(skill_effect_id,true)
	if skillEffectTableInfo == nil then
		return false
	end
	if skillEffectTableInfo.SkillTriggerID == 0 then
		return true
	end
	local SkillTriggerIDModule = SkillEffectIDMoudle.GetMoudle(skillEffectTableInfo.SkillTriggerID)
	if SkillTriggerIDModule ~= nil and SkillTriggerIDModule.OnSkillAttack ~= nil then
		return SkillTriggerIDModule.OnSkillAttackBeforeCalculate( SkillEffectIDMoudle.mUtilTool, caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	end
	return false
end

--释放技能后 kBuffTTT_SkillEnd = 15
function SkillEffectIDMoudle.OnSkillEnd( caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id)
	local skillEffectTableInfo = Table.SkillEffectTable.GetRowById(skill_effect_id,true)
	if skillEffectTableInfo == nil then
		return false
	end
	if skillEffectTableInfo.SkillTriggerID == 0 then
		return true
	end
	local SkillTriggerIDModule = SkillEffectIDMoudle.GetMoudle(skillEffectTableInfo.SkillTriggerID)
	if SkillTriggerIDModule ~= nil and SkillTriggerIDModule.OnSkillAttack ~= nil then
		return SkillTriggerIDModule.OnSkillEnd( SkillEffectIDMoudle.mUtilTool, caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	end
	return false
end

--攻击时，暴击&命中判定前 kBuffTTT_AttackBeforeJudge = 24
function SkillEffectIDMoudle.OnSkillAttackBeforeJudge( caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	local skillEffectTableInfo = Table.SkillEffectTable.GetRowById(skill_effect_id,true)
	if skillEffectTableInfo == nil then
		return false
	end
	if skillEffectTableInfo.SkillTriggerID == 0 then
		return true
	end
	local SkillTriggerIDModule = SkillEffectIDMoudle.GetMoudle(skillEffectTableInfo.SkillTriggerID)
	if SkillTriggerIDModule ~= nil and SkillTriggerIDModule.OnSkillAttack ~= nil then
		return SkillTriggerIDModule.OnSkillAttackBeforeJudge( SkillEffectIDMoudle.mUtilTool, caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	end
	return false
end

--释放技能后立刻 kBuffTTT_SkillStart = 31
function SkillEffectIDMoudle.OnSkillStart( caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id)
	local skillEffectTableInfo = Table.SkillEffectTable.GetRowById(skill_effect_id,true)
	if skillEffectTableInfo == nil then
		return false
	end
	if skillEffectTableInfo.SkillTriggerID == 0 then
		return true
	end
	local SkillTriggerIDModule = SkillEffectIDMoudle.GetMoudle(skillEffectTableInfo.SkillTriggerID)
	if SkillTriggerIDModule ~= nil and SkillTriggerIDModule.OnSkillAttack ~= nil then
		return SkillTriggerIDModule.OnSkillStart( SkillEffectIDMoudle.mUtilTool, caster, caster_snapshot, target, skill_id, skill_uuid, skill_effect_id, damage_result, args )
	end
	return false
end
