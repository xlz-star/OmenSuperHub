using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 进程监控和应用识别模块
    /// </summary>
    public class ProcessMonitor
    {
        private Timer _monitorTimer;
        private readonly List<AppRule> _appRules;
        private readonly int _scanInterval;
        private AppScenario _lastDetectedScenario = AppScenario.Office;
        private string _lastActiveProcess = "";
        private AppScenario _defaultScenario = AppScenario.Office;

        public event Action<AppScenario, string> ScenarioDetected;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public ProcessMonitor(List<AppRule> appRules, int scanInterval = 30)
        {
            _appRules = appRules ?? new List<AppRule>();
            _scanInterval = scanInterval;
        }

        /// <summary>
        /// 更新应用规则列表
        /// </summary>
        public void UpdateAppRules(List<AppRule> appRules)
        {
            Logger.Debug($"[ProcessMonitor] 开始更新AppRules，原数量: {_appRules.Count}");
            _appRules.Clear();
            if (appRules != null)
            {
                _appRules.AddRange(appRules);
                Logger.Info($"[ProcessMonitor] AppRules更新完成，新数量: {_appRules.Count}");
            }
            else
            {
                Logger.Debug($"[ProcessMonitor] 新AppRules为null，保持空列表");
            }
        }

        /// <summary>
        /// 更新默认场景
        /// </summary>
        public void UpdateDefaultScenario(AppScenario defaultScenario)
        {
            _defaultScenario = defaultScenario;
            Logger.Debug($"[ProcessMonitor] 默认场景已更新为: {defaultScenario}");
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        public void StartMonitoring()
        {
            StopMonitoring();
            _monitorTimer = new Timer(MonitorCallback, null, 1000, _scanInterval * 1000);
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        /// <summary>
        /// 监控回调函数
        /// </summary>
        private void MonitorCallback(object state)
        {
            try
            {
                var detectedScenario = DetectCurrentScenario();
                var activeProcess = GetActiveProcessName();

                // 如果场景发生变化，触发事件
                if (detectedScenario != _lastDetectedScenario || activeProcess != _lastActiveProcess)
                {
                    _lastDetectedScenario = detectedScenario;
                    _lastActiveProcess = activeProcess;
                    ScenarioDetected?.Invoke(detectedScenario, activeProcess);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessMonitor error: {ex.Message}");
            }
        }

        /// <summary>
        /// 检测当前场景
        /// </summary>
        public AppScenario DetectCurrentScenario()
        {
            var runningProcesses = GetRunningProcesses();
            var activeProcess = GetActiveProcessInfo();

            // 首先检查前台应用
            if (activeProcess != null)
            {
                var activeRule = MatchProcessToRule(activeProcess.ProcessName, activeProcess.WindowTitle);
                if (activeRule != null && activeRule.IsEnabled)
                {
                    return activeRule.Scenario;
                }
            }

            // 检查所有运行的进程，按优先级排序
            var matchedRules = new List<(AppRule rule, string processName)>();

            foreach (var process in runningProcesses)
            {
                var rule = MatchProcessToRule(process.ProcessName, "");
                if (rule != null && rule.IsEnabled)
                {
                    matchedRules.Add((rule, process.ProcessName));
                }
            }

            // 按优先级降序排序，返回最高优先级的场景
            var highestPriorityRule = matchedRules
                .OrderByDescending(x => x.rule.Priority)
                .FirstOrDefault();

            if (highestPriorityRule.rule != null)
            {
                return highestPriorityRule.rule.Scenario;
            }

            // 基于资源占用判断
            var resourceBasedScenario = DetectScenarioByResourceUsage();
            if (resourceBasedScenario != AppScenario.Office)
            {
                return resourceBasedScenario;
            }

            // 如果没有匹配的规则且资源占用正常，返回默认场景
            return _defaultScenario;
        }

        /// <summary>
        /// 获取运行中的进程信息
        /// </summary>
        private List<ProcessInfo> GetRunningProcesses()
        {
            var processes = new List<ProcessInfo>();

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (!process.HasExited && !string.IsNullOrEmpty(process.ProcessName))
                        {
                            processes.Add(new ProcessInfo
                            {
                                ProcessName = process.ProcessName.ToLower(),
                                ProcessId = process.Id,
                                WindowTitle = ""
                            });
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的进程
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRunningProcesses error: {ex.Message}");
            }

            return processes;
        }

        /// <summary>
        /// 获取当前活动进程信息
        /// </summary>
        private ProcessInfo GetActiveProcessInfo()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId == 0) return null;

                var process = Process.GetProcessById((int)processId);
                if (process == null) return null;

                int length = GetWindowTextLength(hwnd);
                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, sb.Capacity);

                return new ProcessInfo
                {
                    ProcessName = process.ProcessName.ToLower(),
                    ProcessId = process.Id,
                    WindowTitle = sb.ToString()
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取当前活动进程名称
        /// </summary>
        private string GetActiveProcessName()
        {
            var activeProcess = GetActiveProcessInfo();
            return activeProcess?.ProcessName ?? "";
        }

        /// <summary>
        /// 将进程匹配到规则
        /// </summary>
        private AppRule MatchProcessToRule(string processName, string windowTitle)
        {
            if (string.IsNullOrEmpty(processName)) return null;

            // 精确匹配进程名
            var exactMatch = _appRules.FirstOrDefault(rule =>
                rule.IsEnabled &&
                !string.IsNullOrEmpty(rule.ProcessName) &&
                processName.Contains(rule.ProcessName.ToLower()));

            if (exactMatch != null) return exactMatch;

            // 窗口标题匹配
            if (!string.IsNullOrEmpty(windowTitle))
            {
                var titleMatch = _appRules.FirstOrDefault(rule =>
                    rule.IsEnabled &&
                    !string.IsNullOrEmpty(rule.WindowTitle) &&
                    windowTitle.ToLower().Contains(rule.WindowTitle.ToLower()));

                if (titleMatch != null) return titleMatch;
            }

            return null;
        }

        /// <summary>
        /// 基于资源占用检测场景
        /// </summary>
        private AppScenario DetectScenarioByResourceUsage()
        {
            try
            {
                // 这里可以根据CPU和GPU占用率来判断
                // 暂时返回办公模式作为默认值
                return AppScenario.Office;
            }
            catch
            {
                return AppScenario.Office;
            }
        }

        /// <summary>
        /// 手动触发场景检测
        /// </summary>
        public void TriggerDetection()
        {
            Task.Run(() => MonitorCallback(null));
        }
    }

    /// <summary>
    /// 进程信息类
    /// </summary>
    public class ProcessInfo
    {
        public string ProcessName { get; set; } = "";
        public int ProcessId { get; set; }
        public string WindowTitle { get; set; } = "";
    }
}
