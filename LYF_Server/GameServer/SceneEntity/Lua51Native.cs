using System;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>原版 Lua 5.1.4 的窄 C ABI；这里只处理解释器栈，不承载游戏业务数据。</summary>
internal static class Lua51Native
{
    /// <summary>Lua 5.1 全局表的伪索引，来自原 lua.h。</summary>
    internal const int Globals = -10002;
    /// <summary>Lua 5.1 注册表的伪索引，用于保存回调对象。</summary>
    internal const int Registry = -10000;
    /// <summary>加载当前进程位数对应库后保留模块句柄，直到进程退出。</summary>
    private static readonly IntPtr module = LoadRuntime();

    /// <summary>按当前进程指针宽度选择随服务器部署的真实 Lua 库。</summary>
    private static IntPtr LoadRuntime()
    {
        // AnyCPU 进程实际位数决定原生 ABI，不改变服务器项目平台。
        string architecture = "x86";
        if (IntPtr.Size == 8) architecture = "x64";
        // 文件随 GameServer 发布，不依赖参考工程或开发机路径。
        string path = Path.Combine(Path.GetDirectoryName(typeof(Lua51Native).Assembly.Location), "ThirdParty", "Lua51", architecture, "lua51.dll");
        // Windows 先注册相同文件名模块，下面 Cdecl 导入使用该模块。
        IntPtr handle = LoadLibrary(path);
        if (handle == IntPtr.Zero) throw new DllNotFoundException("加载 Lua 5.1 失败：" + path + "，Win32=" + Marshal.GetLastWin32Error());
        return handle;
    }

    /// <summary>在第一次调用 C API 前触发显式静态初始化。</summary>
    internal static void EnsureLoaded() { if (module == IntPtr.Zero) throw new DllNotFoundException("Lua 5.1 模块未加载"); }
    /// <summary>Windows 原生模块加载入口。</summary>
    /// <param name="path">当前位数 DLL 的绝对路径。</param>
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibrary(string path);
    /// <summary>创建独立 Lua 状态；由对应宿主释放。</summary>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr luaL_newstate();
    /// <summary>释放状态及其 Lua 内存。</summary><param name="state">待释放状态。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_close(IntPtr state);
    /// <summary>加载原 Lua 标准库，供 require/ipairs/数学等原脚本使用。</summary><param name="state">所属状态。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void luaL_openlibs(IntPtr state);
    /// <summary>编译 UTF8 脚本到栈顶，返回 Lua 状态码。</summary><param name="state">所属状态。</param><param name="buffer">完整UTF8字节。</param><param name="size">字节长度。</param><param name="name">错误定位名。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int luaL_loadbuffer(IntPtr state, byte[] buffer, UIntPtr size, string name);
    /// <summary>保护调用栈顶函数，保留原 Lua 错误返回码。</summary><param name="state">所属状态。</param><param name="arguments">参数数量。</param><param name="results">结果数量。</param><param name="errorFunction">错误函数栈索引，0表示无。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int lua_pcall(IntPtr state, int arguments, int results, int errorFunction);
    /// <summary>读取栈顶绝对索引。</summary><param name="state">所属状态。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int lua_gettop(IntPtr state);
    /// <summary>恢复栈大小，弹出本次临时值。</summary><param name="state">所属状态。</param><param name="index">需要保留的栈顶。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_settop(IntPtr state, int index);
    /// <summary>读取 Lua 值类型编号。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int lua_type(IntPtr state, int index);
    /// <summary>读取数值，原 Lua number 为 double。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern double lua_tonumber(IntPtr state, int index);
    /// <summary>读取布尔语义，0为false。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int lua_toboolean(IntPtr state, int index);
    /// <summary>读取字符串地址和UTF8字节数，地址属于当前Lua状态。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param><param name="length">返回字节数。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr lua_tolstring(IntPtr state, int index, out UIntPtr length);
    /// <summary>压入number。</summary><param name="state">所属状态。</param><param name="value">数值。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushnumber(IntPtr state, double value);
    /// <summary>压入boolean。</summary><param name="state">所属状态。</param><param name="value">0或1。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushboolean(IntPtr state, int value);
    /// <summary>压入nil。</summary><param name="state">所属状态。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushnil(IntPtr state);
    /// <summary>压入UTF8字节串并由Lua复制。</summary><param name="state">所属状态。</param><param name="value">UTF8字节。</param><param name="length">有效字节数。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushlstring(IntPtr state, byte[] value, UIntPtr length);
    /// <summary>复制指定栈值。</summary><param name="state">所属状态。</param><param name="index">原值栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushvalue(IntPtr state, int index);
    /// <summary>将稳定委托压为Lua函数，upvalue数量为0。</summary><param name="state">所属状态。</param><param name="callback">保留到状态释放的回调。</param><param name="upvalues">上值数量。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_pushcclosure(IntPtr state, Lua51Callback callback, int upvalues);
    /// <summary>创建普通Lua表。</summary><param name="state">所属状态。</param><param name="array">数组预留。</param><param name="hash">键值预留。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_createtable(IntPtr state, int array, int hash);
    /// <summary>读取表的字符串键并压栈。</summary><param name="state">所属状态。</param><param name="index">表索引。</param><param name="key">字段名。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_getfield(IntPtr state, int index, string key);
    /// <summary>取栈顶值写表字符串键并弹栈。</summary><param name="state">所属状态。</param><param name="index">表索引。</param><param name="key">字段名。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_setfield(IntPtr state, int index, string key);
    /// <summary>按整数键读取表项。</summary><param name="state">所属状态。</param><param name="index">表索引。</param><param name="key">整数键。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_rawgeti(IntPtr state, int index, int key);
    /// <summary>将栈顶值按整数键写表。</summary><param name="state">所属状态。</param><param name="index">表索引。</param><param name="key">整数键。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern void lua_rawseti(IntPtr state, int index, int key);
    /// <summary>创建托管桥句柄的原生userdata，使原脚本type判断保持userdata。</summary><param name="state">所属状态。</param><param name="size">句柄所需字节数。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr lua_newuserdata(IntPtr state, UIntPtr size);
    /// <summary>取userdata储存地址。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr lua_touserdata(IntPtr state, int index);
    /// <summary>设置userdata元表，支持零基向量索引。</summary><param name="state">所属状态。</param><param name="index">userdata栈索引。</param>
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)] internal static extern int lua_setmetatable(IntPtr state, int index);
}
