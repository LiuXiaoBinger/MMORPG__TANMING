
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// 服务端控制台日志工具：统一输出带时间、托管线程 ID、调用方类/文件/行号前缀的日志。
/// </summary>
public class LogMsg {

    /// <summary>获取控制台窗口句柄（P/Invoke）。</summary>
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    /// <summary>设置控制台窗口标题（P/Invoke）。</summary>
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern int SetWindowText(IntPtr hwnd, string lpString);


    /// <summary>日志输出回调，服务宿主可订阅以把日志转发到文件或界面。</summary>
    public static Action<string> logCB;


    /// <summary>设置控制台窗口标题文字。</summary>
    public static void SetWindowInfo(string text) {
        SetWindowText(GetConsoleWindow(), text);
    }

    /// <summary>
    /// 输出一条日志；自动附加时间、托管线程 ID、调用方位置前缀，并按日志级别加横幅。
    /// </summary>
    /// <param name="msg">日志内容。</param>
    /// <param name="lv">日志级别，None 表示不加级别横幅。</param>
    /// <param name="callerMember">编译器自动填充的调用方方法名。</param>
    /// <param name="callerFile">编译器自动填充的调用源文件路径。</param>
    /// <param name="callerLine">编译器自动填充的调用行号。</param>
    public static void Info(
        string msg,
        LogMsgType lv = LogMsgType.None,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0) {

        // Include the managed thread ID in every log entry so asynchronous callbacks
        // can be traced back to the thread that handled them.
        Type callerType = new StackTrace(1, false).GetFrame(0)?.GetMethod()?.DeclaringType;
        string callerClass = callerType == null ? "UnknownClass" : callerType.Name;
        string callerFileName = string.IsNullOrEmpty(callerFile)
            ? "UnknownFile"
            : Path.GetFileName(callerFile);
        msg = DateTime.Now.ToLongTimeString()
            + " [Thread:" + Thread.CurrentThread.ManagedThreadId + "]"
            + " [" + callerClass + "." + callerMember
            + " @ " + callerFileName + ":" + callerLine + "] >> " + msg;
        logCB?.Invoke(msg);

        if (lv == LogMsgType.None) {
            Console.WriteLine(msg);
        }
        else if (lv == LogMsgType.Warn) {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("//--------------------Warn--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else if (lv == LogMsgType.Error) {
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("//--------------------ErrorCode--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else if (lv == LogMsgType.Info) {
            //Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("//--------------------Info--------------------//");
            Console.WriteLine(msg);
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
        else {
            //Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("//--------------------ErrorCode--------------------//");
            Console.WriteLine(msg + " >> Unknow LogMsg Type\n");
            //Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

}

/// <summary>日志级别。</summary>
public enum LogMsgType {
    None = 0,// None
    Warn = 1,//Yellow
    Error = 2,//Red
    Info = 3//Green
}
