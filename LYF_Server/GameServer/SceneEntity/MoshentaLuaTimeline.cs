using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>真实Lua5.1执行两份原时间轴，窄桥只连接其实际调用的原宿主API。</summary>
internal sealed class MoshentaLuaTimeline : MoshentaTimeline
{
    /// <summary>本机制实体独有解释器，原lua_env不会跨实体/副本串用。</summary>
    private readonly Lua51State lua = new Lua51State();
    /// <summary>本Lua环境临时零基ROLuaEntity向量，只保存同实例玩家ID。</summary>
    private List<ulong> players = new List<ulong>();
    /// <summary>原脚本声明的全局表名。</summary>
    private readonly string scriptName;
    /// <summary>只提供原API的Lua方法语法包装，不包含任何魔神塔行为决策。</summary>
    private const string Bootstrap = @"
function declareGlobal(name,value) rawset(_G,name,value or false) end
function getGlobal(name) return rawget(_G,name) end
local env={}
function env:GetInt(key) return self[key] or 0 end
function env:SetInt(key,value) self[key]=value end
entity={GetLuaEnv=function() return env end}
utilTool={}
function utilTool:LuaGetSceneVar(entity,key,a,b,c) return __sceneGet(key) end
function utilTool:LuaSetSceneVar(entity,key,value,kind) return __sceneSet(key,tostring(value),kind) end
function utilTool:LuaSwitchAxis(entity,axis) return __switch(axis) end
function utilTool:LuaSendTVNotice(who,entity,target,id,args,isSelf) return __notice(id) end
function utilTool:LuaForeachPlayers(entity,near,far,vector) return __players(near,far) end
function utilTool:LuaAddBuff(entity,target,id,force) return __buff(target,id) end
review_data={SetLineEvent=function(self,line,action,delay,cache) return __plan(line,action,delay,cache) end}
axis_data={line=1,event=1,GetTimeLine=function(self) return self.line end,GetTimeQueue=function(self) return self.event end}
RORandom={randInt=function(self,first,last) return __random(first,last) end}
__vectorMeta={__index=__vectorIndex,__newindex=__vectorWrite}
std={vector_ROGameLibs__ROLuaEntity__={new_local=function() return __vectorNew() end}}
ROLuaCommonFunction={}
function ROLuaCommonFunction.RandomSort(t)
 if not t then return end
 local cnt=0
 if type(t)=='userdata' then
  cnt=t:size()
  for i=0,cnt-1 do local j=RORandom:randInt(i,cnt); t[i],t[j]=t[j],t[i] end
 else
  cnt=#t
  for i=1,cnt do local j=RORandom:randInt(i,cnt+1); t[i],t[j]=t[j],t[i] end
 end
end
";
    /// <summary>注册真实C API桥后原样加载所选配置的helper和实体脚本。</summary><param name="actor">本时间轴真实宿主。</param><param name="binding">当前实体明确登记的脚本名称。</param>
    internal MoshentaLuaTimeline(MoshentaActor actor, DungeonTimelineBinding binding) : base(actor)
    {
        scriptName = binding.Script;
        lua.Register("__sceneGet", SceneGet); lua.Register("__sceneSet", SceneSet); lua.Register("__switch", SwitchAxis);
        lua.Register("__notice", Notice); lua.Register("__players", Players); lua.Register("__buff", AddBuff);
        lua.Register("__plan", Plan); lua.Register("__random", Random); lua.Register("__vectorNew", VectorNew);
        lua.Register("__vectorIndex", VectorIndex); lua.Register("__vectorWrite", VectorWrite); lua.Register("__vectorSize", VectorSize);
        lua.Execute(Bootstrap, "RO窄API桥");
        // 源码按文件原样执行，require缓存仍使用原time模块名。
        string directory = actor.Owner.Battle.LuaDirectory;
        lua.Execute(File.ReadAllText(Path.Combine(directory, "Perform_BasicAction.lua")), "Perform_BasicAction.lua");
        lua.Execute("package.loaded['time.Perform_BasicAction']=true", "原模块缓存");
        lua.Execute(File.ReadAllText(Path.Combine(directory, "lua_timeline_utiltool.lua")), "lua_timeline_utiltool.lua");
        lua.Execute(File.ReadAllText(Path.Combine(directory, scriptName + ".lua")), scriptName + ".lua");
        lua.Execute("package.loaded['time." + scriptName + "']=true", "原时间轴模块缓存");
        Call("InitVariables", false);
        Call("FirstReviewOver", true);
    }
    /// <summary>调用原Self函数，保持不同入口的参数顺序。</summary><param name="name">原函数名。</param><param name="review">是否传review_data代替entity。</param>
    private bool Call(string name, bool review)
    {
        // 本次调用前保存原栈顶，确保每tick不增长栈。
        IntPtr state = lua.Handle;
        int top = Lua51Native.lua_gettop(state);
        Lua51Native.lua_getfield(state, Lua51Native.Globals, "LuaTimeLineTool");
        Lua51Native.lua_getfield(state, -1, "CallTimeLine");
        Lua51State.PushText(state, "time." + scriptName);
        Lua51State.PushText(state, scriptName);
        Lua51State.PushText(state, name);
        Lua51Native.lua_getfield(state, Lua51Native.Globals, "utilTool");
        if (review) Lua51Native.lua_getfield(state, Lua51Native.Globals, "review_data");
        else Lua51Native.lua_getfield(state, Lua51Native.Globals, "entity");
        // StepInterface额外读取轴号与事件号，其余入口仅两个参数。
        int count = 6;
        Lua51Native.lua_getfield(state, Lua51Native.Globals, "axis_data");
        lua.Call(count, 1);
        bool result = Lua51Native.lua_toboolean(state, -1) != 0;
        Lua51Native.lua_settop(state, top); return result;
    }
    /// <summary>原StepInterface在Lua中选择并执行具体函数。</summary><param name="axis">原轴号。</param><param name="number">事件号。</param>
    protected override bool Invoke(int axis, int number)
    {
        // 对应LuaScriptTimeAxisData两个只读访问值。
        IntPtr state = lua.Handle;
        Lua51Native.lua_getfield(state, Lua51Native.Globals, "axis_data");
        Lua51Native.lua_pushnumber(state, axis); Lua51Native.lua_setfield(state, -2, "line");
        Lua51Native.lua_pushnumber(state, number); Lua51Native.lua_setfield(state, -2, "event");
        Lua51Native.lua_settop(state, -2);
        return Call("StepInterface", false);
    }
    /// <summary>返回原LuaGetSceneVar的五个值。</summary><param name="state">原生回调栈。</param>
    private int SceneGet(IntPtr state)
    {
        // 原API返回成功标记、key以及三种类型槽。
        string key = Lua51State.Text(state, 1);
        MoshentaSceneVariable value;
        bool found = Actor.Owner.TrySceneVariable(key, out value);
        Lua51Native.lua_pushboolean(state, Convert.ToInt32(found)); Lua51State.PushText(state, key);
        int integer = 0; float number = 0; string text = "";
        if (found) { integer = value.IntValue; number = value.FloatValue; text = value.StringValue ?? ""; }
        Lua51Native.lua_pushnumber(state, integer); Lua51Native.lua_pushnumber(state, number); Lua51State.PushText(state, text); return 5;
    }
    /// <summary>按原类型枚举写场景变量。</summary><param name="state">原生回调栈。</param>
    private int SceneSet(IntPtr state)
    {
        // 原0/1/2依次代表int/float/string。
        int kind = (int)Lua51Native.lua_tonumber(state, 3);
        string type = "kTypeInt";
        if (kind == 1) type = "kTypeFloat";
        else if (kind == 2) type = "kTypeString";
        Actor.Owner.SetSceneVariable(Lua51State.Text(state, 1), Lua51State.Text(state, 2), type);
        Lua51Native.lua_pushboolean(state, 1); return 1;
    }
    /// <summary>切换原时间轴，返回实际切换结果。</summary><param name="state">原生回调栈。</param>
    private int SwitchAxis(IntPtr state) { Lua51Native.lua_pushboolean(state, Convert.ToInt32(Switch((int)Lua51Native.lua_tonumber(state, 1)))); return 1; }
    /// <summary>转发原TV公告。</summary><param name="state">原生回调栈。</param>
    private int Notice(IntPtr state) { Actor.Owner.PublishAiNotice(1, (int)Lua51Native.lua_tonumber(state, 1), Actor, 0); Lua51Native.lua_pushboolean(state, 1); return 1; }
    /// <summary>原LuaForeachPlayers按同实例水平范围填零基向量。</summary><param name="state">原生回调栈。</param>
    private int Players(IntPtr state) { players = Actor.Owner.PlayersInRange(Actor.Position, (float)Lua51Native.lua_tonumber(state, 1), (float)Lua51Native.lua_tonumber(state, 2)); Lua51Native.lua_pushboolean(state, Convert.ToInt32(players.Count > 0)); return 1; }
    /// <summary>对原向量中的玩家执行真实Buff结果。</summary><param name="state">原生回调栈。</param>
    private int AddBuff(IntPtr state) { Actor.Owner.AddBuff(Actor, (ulong)Lua51Native.lua_tonumber(state, 1), (int)Lua51Native.lua_tonumber(state, 2)); Lua51Native.lua_pushboolean(state, 1); return 1; }
    /// <summary>从原FirstReviewOver收集计划，不另解析Lua文本。</summary><param name="state">原生回调栈。</param>
    private int Plan(IntPtr state)
    {
        // 每条回调由原ipairs顺序产生。
        int axis = (int)Lua51Native.lua_tonumber(state, 1);
        if (!Plans.ContainsKey(axis)) Plans[axis] = new List<MoshentaTimePoint>();
        if (Lua51Native.lua_toboolean(state, 4) != 0) throw new InvalidDataException("本次原时间轴出现未核验cache事件");
        Plans[axis].Add(new MoshentaTimePoint { Action = Lua51State.Text(state, 2), Delay = (int)Lua51Native.lua_tonumber(state, 3), Parameters = new string[0] }); return 0;
    }
    /// <summary>同一个实例随机源，保持原randInt左闭右开。</summary><param name="state">原生回调栈。</param>
    private int Random(IntPtr state) { Lua51Native.lua_pushnumber(state, Actor.Owner.Random.Next((int)Lua51Native.lua_tonumber(state, 1), (int)Lua51Native.lua_tonumber(state, 2))); return 1; }
    /// <summary>创建真实userdata，使原RandomSort进入零基向量分支。</summary><param name="state">原生回调栈。</param>
    private int VectorNew(IntPtr state) { IntPtr pointer = Lua51Native.lua_newuserdata(state, (UIntPtr)4); Marshal.WriteInt32(pointer, 1); Lua51Native.lua_getfield(state, Lua51Native.Globals, "__vectorMeta"); Lua51Native.lua_setmetatable(state, -2); return 1; }
    /// <summary>向量方法size或零基整数读取。</summary><param name="state">原生回调栈。</param>
    private int VectorIndex(IntPtr state)
    {
        if (Lua51Native.lua_type(state, 2) == 4) { Lua51Native.lua_getfield(state, Lua51Native.Globals, "__vectorSize"); return 1; }
        // 原向量内玩家ID来自数据库int，Lua number可精确表示。
        int index = (int)Lua51Native.lua_tonumber(state, 2);
        Lua51Native.lua_pushnumber(state, players[index]); return 1;
    }
    /// <summary>原RandomSort的零基向量交换写入。</summary><param name="state">原生回调栈。</param>
    private int VectorWrite(IntPtr state) { players[(int)Lua51Native.lua_tonumber(state, 2)] = (ulong)Lua51Native.lua_tonumber(state, 3); return 0; }
    /// <summary>原std::vector.size。</summary><param name="state">原生回调栈。</param>
    private int VectorSize(IntPtr state) { Lua51Native.lua_pushnumber(state, players.Count); return 1; }
    /// <summary>随实体/实例退出释放Lua状态。</summary>
    public override void Dispose() { lua.Dispose(); }
}
