declareGlobal("LuaTimeLineTool", {})


function LuaTimeLineTool.CallTimeLine(path, scriptname, funcname, utilTool, entity, axis_data)
    if package.loaded[path] == nil then
        require(path)
    end

    local tablelib = getGlobal(scriptname) 
    if  tablelib ~= nil then
        return tablelib[funcname](utilTool, entity,axis_data)
    end
    return false
end
