
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;

public class LogMsg {

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern int SetWindowText(IntPtr hwnd, string lpString);


    public static Action<string> logCB;


    public static void SetWindowInfo(string text) {
        SetWindowText(GetConsoleWindow(), text);
    }

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

public enum LogMsgType {
    None = 0,// None
    Warn = 1,//Yellow
    Error = 2,//Red
    Info = 3//Green
}
