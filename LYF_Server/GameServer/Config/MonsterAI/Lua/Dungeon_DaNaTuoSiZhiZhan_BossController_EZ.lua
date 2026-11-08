require("time.Perform_BasicAction")
declareGlobal("Dungeon_DaNaTuoSiZhiZhan_BossController_EZ",{})
local Self = Dungeon_DaNaTuoSiZhiZhan_BossController_EZ

--[[ 分布规则

	1. 前置枚举，说明时间轴方案,方便查询
	2. 统一入口:
		A. 初始化部分，只调用一次
		B. 返回时间轴所有时间点
		C. 统一调用入口
	3. 逻辑编写块 (通用参数 utilTool, entity)
]]


-----------------------------------------------------------------------
-- 前置枚举
-- 注意点: 前置时间 { 3000, "idle" } => 3秒过后，执行idle函数
-- 注意点: 时间精确至毫秒 即  1000 = 1秒
-----------------------------------------------------------------------
	Self.element_plan =
	{
		[1] = {
				[1] = {false, 1000000, "Wait"},
			  },
		[2] = {
				[1] = {false, 500, "SetIntSceneVar", "DaNaTuoSiState", 4},--癫狂循环
				[2] = {false, 15000, "Wait"},
			  },
		--憎恨
		[3] = {
				[1] = {false, 200, "SetIntSceneVar", "HaterState", 1},--憎恨出场
				[2] = {false, 2500, "SetIntSceneVar", "HaterState", 3},--憎恨激活
				[3] = {false, 3000, "SetIntSceneVar", "HaterSkillState", 2},--3连
				[4] = {false, 7000, "SetIntSceneVar", "HaterSkillState", 3},--挥击
				[5] = {false, 6000, "SetIntSceneVar", "HaterSkillState", 1},--凝视
				[6] = {false, 6000, "SetIntSceneVar", "HaterSkillState", 3},--挥击
				[7] = {false, 8000, "SetIntSceneVar", "HaterSkillState", 2},--3连
				[8] = {false, 7000, "SetIntSceneVar", "HaterSkillState", 3},--挥击
				[9] = {false, 8000, "SetIntSceneVar", "HaterSkillState", 1},--凝视
				[10] = {false, 8000, "SetIntSceneVar", "HaterSkillState", 3},--挥击
				[11] = {false, 8000, "SetIntSceneVar", "HaterSkillState", 2},--3连
				[12] = {false, 6000, "SetIntSceneVar", "HaterState", 2},--憎恨谢幕
				[13] = {false, 4000, "SetDaNaTuoSiState"},
				[14] = {false, 11000, "ChooseOption"},
			  },
		--绝望
		[4] = {
				[1] = {false, 200, "SetIntSceneVar", "DespairState", 1},--绝望出场
				[2] = {false, 2500, "SetIntSceneVar", "DespairState", 3},--绝望激活
				[3] = {false, 6000, "SetIntSceneVar", "DespairSkillState", 3},--炎火箭
				[4] = {false, 8000, "SetIntSceneVar", "DespairSkillState", 4},--熔岩爆发
				[5] = {false, 9000, "SetIntSceneVar", "DespairSkillState", 2},--熔岩吐息
				[6] = {false, 23000, "SetIntSceneVar", "DespairSkillState", 3},--炎火箭
				[7] = {false, 9000, "SetIntSceneVar", "DespairSkillState", 1},--熔岩爆发
				[8] = {false, 9000, "SetIntSceneVar", "DespairSkillState", 3},--炎火箭
				[9] = {false, 8000, "SetIntSceneVar", "DespairSkillState", 2},--熔岩吐息
				[10] = {false, 22000, "SetIntSceneVar", "DespairState", 2},--绝望谢幕
				[11] = {false, 4000, "SetDaNaTuoSiState"},
				[12] = {false, 11000, "ChooseOption"},
			  },  
		--悲哀	
		[5] = {
				[1] = {false, 200, "SetIntSceneVar", "SadState", 1},--悲哀出场
				[2] = {false, 2500, "SetIntSceneVar", "SadState", 3},--悲哀激活
				[3] = {false, 8000, "SetIntSceneVar", "SadSkillState", 3},--悲哀或欣喜
				[4] = {false, 10000, "SetIntSceneVar", "SadSkillState", 2},--诡诈把戏
				[5] = {false, 10000, "SetIntSceneVar", "SadSkillState", 1},--十字绞杀
				[6] = {false, 10000, "SetIntSceneVar", "SadSkillState", 2},--诡诈把戏
				[7] = {false, 10000, "SetIntSceneVar", "SadSkillState", 3},--悲哀或欣喜
				[8] = {false, 10000, "SetIntSceneVar", "SadSkillState", 1},--十字绞杀
				[9] = {false, 10000, "SetIntSceneVar", "SadSkillState", 2},--诡诈把戏
				[10] = {false, 8000, "SetIntSceneVar", "SadSkillState", 3},--悲哀或欣喜
				[11] = {false, 6000, "SetIntSceneVar", "SadState", 2},--悲哀谢幕
				[12] = {false, 2000, "SetDaNaTuoSiState"},
				[13] = {false, 11000, "ChooseOption"},
			  }, 
		--一起出场
		[6] = {	
				[1] = {false, 2000, "LuaSendTVNotice", 2318, 0},--达纳托斯撑不住了
				[2] = {false, 0, "SetIntSceneVar", "SadState", 3},--悲哀激活
				[3] = {false, 0, "SetIntSceneVar", "DespairState", 3},--绝望激活
				[4] = {false, 0, "SetIntSceneVar", "HaterState", 3},--憎恨激活
				[5] = {false, 5000, "SetIntSceneVar", "HaterSkillState", 2},--3连
				[6] = {false, 2000, "SetIntSceneVar", "DespairSkillState", 3},--炎火箭
				[7] = {false, 0, "SetIntSceneVar", "SadSkillState", 1},--十字绞杀
				[8] = {false, 10000, "SetIntSceneVar", "HaterSkillState", 3},--挥击
				[9] = {false, 3000, "SetIntSceneVar", "SadSkillState", 3},--悲哀或欣喜
				[10] = {false, 2000, "SetIntSceneVar", "DespairSkillState", 3},--炎火箭
				[11] = {false, 8000, "SetIntSceneVar", "DespairSkillState", 4},--熔岩爆发
				[12] = {false, 0, "SetIntSceneVar", "HaterSkillState", 2},--3连
				[13] = {false, 2000, "SetIntSceneVar", "SadSkillState", 2},--诡诈把戏
				[14] = {false, 5000, "SetIntSceneVar", "SadState", 2},--悲哀未激活
				[15] = {false, 0, "SetIntSceneVar", "DespairState", 2},--绝望未激活
				[16] = {false, 0, "SetIntSceneVar", "HaterState", 2},--憎恨未激活
				[17] = {false, 4000, "SetIntSceneVar", "DaNaTuoSiState", 4},--第四次癫狂
				[18] = {false, 15000, "SwitchAxisBySelf", 2},
			  }, 
		[7] = {
				[1] = {false, 200, "Wait"},
				[2] = {false, 0, "ChooseOption"},
			  },
	}

-----------------------------------------------------------------------
-- 统一入口: 统一初始化部分，只调用一次
-----------------------------------------------------------------------
function Self.InitVariables(utilTool, entity)
	local lua_env = entity:GetLuaEnv() --加载隔离变量组
	local isChoosen = false
	local key = ""
	local initAxisVar = 0
	local tempA = 0
	local tempB = ""
	isChoosen, key, initAxisVar, tempA, tempB = utilTool:LuaGetSceneVar(entity, "BossOpt", initAxisVar, tempA, tempB)
	if isChoosen == false then
		local randAxis = RORandom:randInt(2,8)
		utilTool:LuaSetSceneVar(entity, "BossOpt", randAxis, 0)
		isChoosen, key, initAxisVar, tempA, tempB = utilTool:LuaGetSceneVar(entity, "BossOpt", initAxisVar, tempA, tempB)
	end
	lua_env:SetInt("init_axis_var", initAxisVar)
	lua_env:SetInt("axis_seq", 1)
	return true
end

-----------------------------------------------------------------------
-- 统一入口: 返回时间轴所有时间点(不用修改)
-----------------------------------------------------------------------
function Self.FirstReviewOver(utilTool, review_data)
	for key, value in ipairs(Self.element_plan) do
		for v1, v2 in ipairs(value) do
			review_data:SetLineEvent(key, v2[3], v2[2], v2[1])
		end
	end
	return true
end

-----------------------------------------------------------------------
-- 统一入口: 统一调用入口
-----------------------------------------------------------------------
function Self.StepInterface(utilTool, entity, axis_data)
	local line = axis_data:GetTimeLine()
	local event = axis_data:GetTimeQueue()
	if line > #Self.element_plan then
		return false
	end

	if event > #Self.element_plan[line] then	
		return false
	end
	
	local lua_line = Self.element_plan[line]
    local lua_event = lua_line[event]

    if lua_event == nil then
        return false
    end

    local lua_param = {}
    local lua_param_size = #lua_event
		if lua_param_size >= 4 then
			local i = 1
			for var = 4, lua_param_size, 1 do
				lua_param[i] = lua_event[var]
				i = i+1
			end
		end

	if Perform_BasicAction[lua_event[3]] ~= nil then
		return Perform_BasicAction[lua_event[3]](utilTool, entity, lua_param)
	elseif Self[lua_event[3]] ~= nil then
		return Self[lua_event[3]](utilTool, entity, lua_param)
	end
end

-----------------------------------------------------------------------
-- 逻辑编写块 (通用参数 utilTool, entity)
-- 返回值 true / false
-----------------------------------------------------------------------

--选择方案
function Self.ChooseOption(utilTool, entity)
	--初始化时间轴顺序，每局调用一次
	local lua_env = entity:GetLuaEnv()
	local initAxisVar = lua_env:GetInt("init_axis_var")
	--根据顺序切轴
	local axisList = {}
	local seq = lua_env:GetInt("axis_seq")
	if initAxisVar == 2 then
		axisList = {3, 4, 5, 6}
	elseif initAxisVar == 3 then
		axisList = {3, 5, 4, 6}
	elseif initAxisVar == 4 then
		axisList = {4, 3, 5, 6}
	elseif initAxisVar == 5 then
		axisList = {4, 5, 3, 6}
	elseif initAxisVar == 6 then
		axisList = {5, 4, 3, 6}
	elseif initAxisVar == 7 then
		axisList = {5, 3, 4, 6}
	end
	seq = seq + 1
	lua_env:SetInt("axis_seq", seq)
	utilTool:LuaSwitchAxis(entity, axisList[seq-1])
	return true	
end

--达纳托斯切换状态
function Self.SetDaNaTuoSiState(utilTool, entity, param)
	local lua_env = entity:GetLuaEnv()
	local seq = lua_env:GetInt("axis_seq") - 1
	utilTool:LuaSetSceneVar(entity, "DaNaTuoSiState", seq, 0)
	return true
end
