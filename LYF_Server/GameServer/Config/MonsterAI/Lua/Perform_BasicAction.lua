declareGlobal("Perform_BasicAction",{})

--等待
function Perform_BasicAction.Wait(utilTool, entity)
	return true
end

--自己切换时间轴
function Perform_BasicAction.SwitchAxisBySelf(utilTool, entity, param)
    local axis = param[1]
    utilTool:LuaSwitchAxis(entity, axis)
    return true
end

--他人切换时间轴
function Perform_BasicAction.SwitchAxisByEntity(utilTool, entity, param)
    local targetID = param[1]
	local axis = param[2]
	utilTool:LuaPerformSwitchAxis(entity, targetID, nil, axis)
    return true
end

--移动到位置
function Perform_BasicAction.MoveToDest(utilTool, entity, param)
    local destX = param[1]
	local destY = param[2]
	local destZ = param[3]
    utilTool:LuaEntityMoveAction(entity, destX, destY, destZ)
    return true
end

--顺时针旋转角度
function Perform_BasicAction.RotateAngle(utilTool, entity, param)
    local angle = param[1]
    utilTool:LuaEntityRotate(entity, angle)
    return true
end

--说话
function Perform_BasicAction.ShowBubble(utilTool, entity, param)
	local content= param[1]
	local talkTime = param[2]
	utilTool:LuaShowBubble(entity, nil, nil, true, 2, content, false, talkTime)
    return true
end

--冒表情
function Perform_BasicAction.ShowEmotion(utilTool, entity, param)
	local emotion = param[1]
	utilTool:LuaEntityEmotion(entity, true, nil, 2, nil, emotion, 2)
    return true
end

--做动作
function Perform_BasicAction.PlayAction(utilTool, entity, param)
	local action = param[1]
	local result = utilTool:LuaEntityPlayAction(entity, action)
	if result == false then
		utilTool:LuaLibLogError(tostring(debug.traceback()))
		return false
	end
    return true
end

function Perform_BasicAction.CastSkillToEntity(utilTool, entity, param)
	local targetID = param[1]
--向某个entity释放技能
	local skillID = param[2]
	local targetList = std.vector_ROGameLibs__ROLuaEntity__:new_local()
	local targetReturn = utilTool:LuaForeachEntity(entity, 1, targetID, 0, 20, targetList)
	if targetReturn == true then
		local target = targetList[0]
		local targetPos = target:GetPosition()
		utilTool:LuaDoCastSkill(entity, target, targetPos, skillID)
	end
	return true
end

--向某个人释放技能
function Perform_BasicAction.CastSkillToPlayer(utilTool, entity, param)
	local skillID = param[1]
	local playerList = std.vector_ROGameLibs__ROLuaEntity__:new_local()
	local playerReturn = utilTool:LuaForeachPlayers(entity, 0, 20, playerList)
	if playerReturn == true then
		local player = playerList[0]
		local playerPos = player:GetPosition()
		utilTool:LuaDoCastSkill(entity, player, playerPos, skillID)
	end
	return true
end

--自己释放技能
function Perform_BasicAction.CastSkillBySelf(utilTool, entity, param)
	local skillID = param[1]
	local entityPos = entity:GetPosition()
	utilTool:LuaDoCastSkill(entity, entity, entityPos, skillID)
	return true
end

--改变阵营
function Perform_BasicAction.ChangeFightGroup(utilTool, entity, param)
	local fightGroup = param[1]
	utilTool:LuaTryChangeFightGroup(entity, entity, fightGroup)
	return true
end

--改变场景Int变量
function Perform_BasicAction.SetIntSceneVar(utilTool, entity, param)
	local var = param[1]
	local value = param[2]
	utilTool:LuaSetSceneVar(entity, var, value, 0)
	return true
end

--做脸部表情
function Perform_BasicAction.ShowEmotionFace(utilTool, entity, param)
	local emotion = param[1]
	utilTool:LuaEntityEmotion(entity, true, nil, 2, nil, emotion, 3)
    return true
end

--给场景发送AI事件
function Perform_BasicAction.SendAIEvent(utilTool, entity, param)
	local event = param[1]
	local delayTime = param[2]
	local intValue = param[3]
	local floatValue = param[4]
	utilTool:LuaEntitySendAIEventToScene(entity, event, delayTime, intValue, floatValue)
    return true
end

--发送Tv公告
function Perform_BasicAction.LuaSendTVNotice(utilTool, entity, param)
	local messageID = param[1]
	local isSelf = param[2]
	utilTool:LuaSendTVNotice(2, entity, nil, messageID, nil, isSelf)
    return true
end

--旋转到指定角度（跟在Unity中调整角度的参考系一致）
function Perform_BasicAction.RotateAngleLikeUnity( utilTool, entity, param )
	-- body
	local angle = param[1]
	utilTool:LuaRotateLikeUnity(entity, angle)
	return true
end

--打断特殊状态
function Perform_BasicAction.EndSpeicalAction( utilTool, entity )
	-- body
	utilTool:LuaEndSpeicalAction(entity)
	return true
end
