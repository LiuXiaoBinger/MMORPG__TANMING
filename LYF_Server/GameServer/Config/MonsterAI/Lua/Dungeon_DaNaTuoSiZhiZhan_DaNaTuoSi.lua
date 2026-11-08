require("time.Perform_BasicAction")
declareGlobal("Dungeon_DaNaTuoSiZhiZhan_DaNaTuoSi",{})
local Self = Dungeon_DaNaTuoSiZhiZhan_DaNaTuoSi

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
				[1] = {false, 100000, "Wait"},
			  },
		[2] = {
				[1] = {false, 0, "DianKuangBuff"},
				[2] = {false, 5000, "SwitchAxisBySelf", 1},
			  },
	}

-----------------------------------------------------------------------
-- 统一入口: 统一初始化部分，只调用一次
-----------------------------------------------------------------------
function Self.InitVariables(utilTool, entity)
	local lua_env = entity:GetLuaEnv() --加载隔离变量组
	lua_env:SetInt("mark_count", 1)
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
--癫狂随机buff
function Self.DianKuangBuff(utilTool, entity)
	local lua_env = entity:GetLuaEnv()
	local playerList = std.vector_ROGameLibs__ROLuaEntity__:new_local()
	local playerReturn = utilTool:LuaForeachPlayers(entity, 0, 25, playerList)

	-- GC 会进行删除这里不需要手动删了 
	if playerReturn == false then
		--playerList:delete()
		return false
	end  

	local playerNum = playerList:size()
	local buffContainer = {3605401, 3605501, 3605601, 3605701, 3605801}
	local buffList = {}
	--根据技能释放次数加buff
	local markCount = lua_env:GetInt("mark_count")

	if markCount < 5 then
		markCount = markCount + 1
		lua_env:SetInt("mark_count", markCount)
		for i = 1, markCount do
			buffList[i] = buffContainer[i]
		end

		--如果人数 <= buff数量
		if playerNum <= #buffList then
			ROLuaCommonFunction.RandomSort(buffList)
			for i = 1, playerNum do 
				utilTool:LuaAddBuff(entity, playerList[i-1], buffList[i], true)
			end
		--如果人数 > buff数量
		elseif playerNum > #buffList then
			ROLuaCommonFunction.RandomSort(playerList)
			for i = 1, #buffList do 
				utilTool:LuaAddBuff(entity, playerList[i-1], buffList[i], true)
			end
			for i = #buffList + 1, playerNum do
		        local rand = RORandom:randInt(1, #buffList+1)
		        utilTool:LuaAddBuff(entity, playerList[i-1], buffList[rand], true)
	   		end
		end
	elseif markCount == 5 then
		for i = 1, playerNum do
			utilTool:LuaAddBuff(entity, playerList[i-1], buffContainer[#buffContainer], true)
		end
	end
	-- playerList:delete()
	return true
end
