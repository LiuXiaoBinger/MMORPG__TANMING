using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>在游戏逻辑线程拥有一个真实Lua状态，提供时间轴和公式共用的窄栈操作。</summary>
internal sealed class Lua51State : IDisposable
{
    /// <summary>原生状态地址，仅在本实例存活期间有效。</summary>
    internal IntPtr Handle { get; private set; }
    /// <summary>注册后的委托必须保留，避免原生Lua持有已回收回调地址。</summary>
    private readonly List<Lua51Callback> callbacks = new List<Lua51Callback>();
    /// <summary>初始化当前位数Lua并打开原标准库。</summary>
    internal Lua51State()
    {
        Lua51Native.EnsureLoaded();
        Handle = Lua51Native.luaL_newstate();
        Lua51Native.luaL_openlibs(Handle);
    }
    /// <summary>读取一个Lua字符串，供原错误和函数参数使用。</summary><param name="state">所属状态。</param><param name="index">栈索引。</param>
    internal static string Text(IntPtr state, int index)
    {
        // Lua返回的长度允许UTF8多字节内容，不使用平台ANSI转换。
        UIntPtr length;
        // 字节内存只在本调用复制期间读取。
        IntPtr pointer = Lua51Native.lua_tolstring(state, index, out length);
        if (pointer == IntPtr.Zero) return "";
        // 拷贝Lua持有的UTF8序列，返回独立托管字符串。
        byte[] bytes = new byte[(int)length.ToUInt64()];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(bytes);
    }
    /// <summary>压入UTF8字符串。</summary><param name="state">所属状态。</param><param name="value">业务字符串。</param>
    internal static void PushText(IntPtr state, string value)
    {
        // P/Invoke序列化边界，只复制本次UTF8文本。
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        Lua51Native.lua_pushlstring(state, bytes, (UIntPtr)bytes.Length);
    }
    /// <summary>注册全局C回调并固定其生命周期。</summary><param name="name">原脚本可见函数名。</param><param name="callback">具体强类型业务桥。</param>
    internal void Register(string name, Lua51Callback callback)
    {
        callbacks.Add(callback);
        Lua51Native.lua_pushcclosure(Handle, callback, 0);
        Lua51Native.lua_setfield(Handle, Lua51Native.Globals, name);
    }
    /// <summary>编译并执行完整原脚本，错误保留原文并恢复调用前栈。</summary><param name="source">原UTF8源码内容。</param><param name="name">用于错误位置的资源名。</param>
    internal void Execute(string source, string name)
    {
        // 执行完成后不得把脚本返回值泄漏到下一次调用。
        int top = Lua51Native.lua_gettop(Handle);
        // UTF8源码不经过宿主重写或配置化解析。
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        // Lua语法错误按原状态码暴露，不能当成执行成功。
        int status = Lua51Native.luaL_loadbuffer(Handle, bytes, (UIntPtr)bytes.Length, name);
        if (status == 0) status = Lua51Native.lua_pcall(Handle, 0, 0, 0);
        // 错误栈内容先复制再清理，保留真实Lua诊断。
        string error = "";
        if (status != 0) error = Text(Handle, -1);
        Lua51Native.lua_settop(Handle, top);
        if (status != 0) throw new InvalidOperationException(name + ": " + error);
    }
    /// <summary>执行调用者提供的完整Lua桥脚本。</summary><param name="source">实际Lua源码，不解释为配置。</param>
    internal void Execute(string source) { Execute(source, "MoshentaLua"); }
    /// <summary>执行一个纯数值表达式并读取唯一number结果。</summary><param name="expression">已构造的Lua表达式。</param>
    internal double EvaluateNumber(string expression)
    {
        // 临时函数返回值只存在于本次调用栈。
        int top = Evaluate(expression);
        // 原Lua number与公式计算使用相同double类型。
        double result = Lua51Native.lua_tonumber(Handle, -1);
        Lua51Native.lua_settop(Handle, top);
        return result;
    }
    /// <summary>执行一个字符串表达式并复制唯一字符串结果。</summary><param name="expression">已构造的Lua表达式。</param>
    internal string EvaluateString(string expression)
    {
        // 本次表达式前的栈深度。
        int top = Evaluate(expression);
        // 文本离开Lua栈后仍属于托管调用者。
        string result = Text(Handle, -1);
        Lua51Native.lua_settop(Handle, top);
        return result;
    }
    /// <summary>编译并执行单表达式，返回调用前栈顶用于结果消费后还原。</summary><param name="expression">合法Lua表达式。</param>
    private int Evaluate(string expression)
    {
        // 所有表达式仍由真实Lua编译器执行。
        int top = Lua51Native.lua_gettop(Handle);
        // 加return只声明结果返回，不改表达式语义。
        byte[] bytes = Encoding.UTF8.GetBytes("return " + expression);
        if (Lua51Native.luaL_loadbuffer(Handle, bytes, (UIntPtr)bytes.Length, "MoshentaExpression") != 0)
            throw new InvalidOperationException(Text(Handle, -1));
        Call(0, 1);
        return top;
    }
    /// <summary>保护调用已压入栈的函数及参数。</summary><param name="arguments">参数数量。</param><param name="results">预期结果数量。</param>
    internal void Call(int arguments, int results)
    {
        // 错误只在Lua边界转为异常，绝不吞掉业务失败。
        int status = Lua51Native.lua_pcall(Handle, arguments, results, 0);
        if (status != 0) throw new InvalidOperationException(Text(Handle, -1));
    }
    /// <summary>释放实例Lua环境。IDisposable是.NET资源释放框架强制约定。</summary>
    public void Dispose()
    {
        if (Handle == IntPtr.Zero) return;
        Lua51Native.lua_close(Handle);
        Handle = IntPtr.Zero;
        callbacks.Clear();
    }
}
