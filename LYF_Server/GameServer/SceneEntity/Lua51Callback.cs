using System;
using System.Runtime.InteropServices;

/// <summary>Lua 5.1 的 C 函数回调边界；参数是原生栈，返回本次压入的返回值数量。</summary>
/// <param name="state">只在当前逻辑线程调用期间有效的 lua_State 指针。</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int Lua51Callback(IntPtr state);
