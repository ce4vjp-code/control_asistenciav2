using System;
using System.IO;

public static class Logger {
    public static void Log(string msg) {
        try {
            File.AppendAllText("debug_log.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + msg + Environment.NewLine);
        } catch {}
    }
}
