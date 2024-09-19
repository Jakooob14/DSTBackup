using System.Diagnostics;
using System.IO;

namespace DSTBackup;

public static class Debugging
{
    private static bool _initialized = false;
    private static string _file = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".log";

    public enum LoggingType
    {
        Info,
        Warning,
        Error
    }
    
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        File.Create(_file);
        Log("Started logging");
    }
    
    public static void Log(string message, LoggingType loggingType = LoggingType.Info)
    {
        string prefix = $"[{DateTime.Now}] [{loggingType.ToString().ToUpper()}] ";
        
        Debug.WriteLine(prefix + message);
        File.AppendAllText(_file, prefix + message + "\n");
    }
}
