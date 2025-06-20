using System;
using System.IO;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 简单的日志记录器，用于调试自适应调度
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adaptive_scheduling_debug.log");
        private static readonly object LogLock = new object();

        /// <summary>
        /// 记录调试信息到文件
        /// </summary>
        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        /// <summary>
        /// 记录信息到文件
        /// </summary>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// 记录错误信息到文件
        /// </summary>
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (LogLock)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] [{level}] {message}";
                    
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
                    
                    // 同时输出到控制台（如果有的话）
                    Console.WriteLine(logLine);
                }
            }
            catch
            {
                // 忽略日志写入错误，避免影响主程序
            }
        }

        /// <summary>
        /// 清空日志文件
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                lock (LogLock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                    WriteLog("INFO", "=== 自适应调度调试日志开始 ===");
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }
    }
}