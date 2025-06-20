using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 事件驱动的进程监控器 - 高效替代循环扫描
    /// </summary>
    public class EventDrivenProcessMonitor : IDisposable
    {
        #region Windows API 声明
        
        // Windows Hook API
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, 
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // 事件常量
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        // Hook委托
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        #endregion

        #region 字段和属性

        private readonly List<AppRule> _appRules;
        private readonly Dictionary<IntPtr, ProcessInfo> _windowCache;
        private readonly Timer _backgroundTimer;
        private AppScenario _defaultScenario = AppScenario.Office;
        
        private IntPtr _foregroundHook;
        private IntPtr _minimizeStartHook;
        private IntPtr _minimizeEndHook;
        private WinEventDelegate _hookDelegate;
        
        private ProcessInfo _lastActiveProcess;
        private AppScenario _lastDetectedScenario = AppScenario.Office;
        private DateTime _lastDetectionTime = DateTime.MinValue;
        
        private bool _isMonitoring = false;
        private bool _disposed = false;

        public event Action<AppScenario, string> ScenarioDetected;

        #endregion

        public EventDrivenProcessMonitor(List<AppRule> appRules)
        {
            _appRules = appRules ?? new List<AppRule>();
            _windowCache = new Dictionary<IntPtr, ProcessInfo>();
            
            // 创建Hook委托（必须保持引用避免被GC）
            _hookDelegate = new WinEventDelegate(OnWindowEvent);
            
            // 创建后台Timer作为备份机制（60秒扫描一次，检测可能遗漏的进程）
            _backgroundTimer = new Timer(BackgroundScanCallback, null, Timeout.Infinite, 60000);
            
            Logger.Info("[EventDrivenProcessMonitor] 事件驱动监控器初始化完成");
        }

        #region 公共方法

        /// <summary>
        /// 开始监控
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring || _disposed) return;

            try
            {
                // 安装Windows事件钩子
                _foregroundHook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, _hookDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

                _minimizeStartHook = SetWinEventHook(
                    EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZESTART,
                    IntPtr.Zero, _hookDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

                _minimizeEndHook = SetWinEventHook(
                    EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND,
                    IntPtr.Zero, _hookDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

                if (_foregroundHook == IntPtr.Zero)
                {
                    Logger.Error("[EventDrivenProcessMonitor] 无法安装前台窗口Hook，回退到Timer模式");
                    // 如果Hook安装失败，增加Timer频率作为备份
                    _backgroundTimer.Change(1000, 10000); // 10秒间隔
                }
                else
                {
                    Logger.Info("[EventDrivenProcessMonitor] Windows事件Hook安装成功");
                    // Hook成功，启动低频后台扫描
                    _backgroundTimer.Change(10000, 60000); // 60秒间隔
                }

                _isMonitoring = true;
                
                // 立即检测当前前台窗口
                CheckCurrentForegroundWindow();
                
                Logger.Info("[EventDrivenProcessMonitor] 监控已启动");
            }
            catch (Exception ex)
            {
                Logger.Error($"[EventDrivenProcessMonitor] 启动监控失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            try
            {
                // 卸载Windows事件钩子
                if (_foregroundHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_foregroundHook);
                    _foregroundHook = IntPtr.Zero;
                }

                if (_minimizeStartHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_minimizeStartHook);
                    _minimizeStartHook = IntPtr.Zero;
                }

                if (_minimizeEndHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_minimizeEndHook);
                    _minimizeEndHook = IntPtr.Zero;
                }

                // 停止后台扫描
                _backgroundTimer.Change(Timeout.Infinite, Timeout.Infinite);

                _isMonitoring = false;
                Logger.Info("[EventDrivenProcessMonitor] 监控已停止");
            }
            catch (Exception ex)
            {
                Logger.Error($"[EventDrivenProcessMonitor] 停止监控失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新应用规则列表
        /// </summary>
        public void UpdateAppRules(List<AppRule> appRules)
        {
            Logger.Debug($"[EventDrivenProcessMonitor] 开始更新AppRules，原数量: {_appRules.Count}");
            _appRules.Clear();
            if (appRules != null)
            {
                _appRules.AddRange(appRules);
                Logger.Info($"[EventDrivenProcessMonitor] AppRules更新完成，新数量: {_appRules.Count}");
            }
            else
            {
                Logger.Debug($"[EventDrivenProcessMonitor] 新AppRules为null，保持空列表");
            }

            // 清空窗口缓存，强制重新检测
            _windowCache.Clear();
            Logger.Debug($"[EventDrivenProcessMonitor] 窗口缓存已清空");
        }

        /// <summary>
        /// 更新默认场景
        /// </summary>
        public void UpdateDefaultScenario(AppScenario defaultScenario)
        {
            _defaultScenario = defaultScenario;
            Logger.Debug($"[EventDrivenProcessMonitor] 默认场景已更新为: {defaultScenario}");
        }

        /// <summary>
        /// 手动触发检测（用于立即检测当前状态）
        /// </summary>
        public void TriggerDetection()
        {
            CheckCurrentForegroundWindow();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// Windows事件回调
        /// </summary>
        private void OnWindowEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                // 只处理窗口事件（忽略子对象事件）
                if (idObject != 0 || idChild != 0) return;

                switch (eventType)
                {
                    case EVENT_SYSTEM_FOREGROUND:
                        Logger.Debug($"[EventDrivenProcessMonitor] 前台窗口变化事件: {hwnd}");
                        OnForegroundWindowChanged(hwnd);
                        break;

                    case EVENT_SYSTEM_MINIMIZESTART:
                        Logger.Debug($"[EventDrivenProcessMonitor] 窗口最小化: {hwnd}");
                        OnWindowMinimized(hwnd);
                        break;

                    case EVENT_SYSTEM_MINIMIZEEND:
                        Logger.Debug($"[EventDrivenProcessMonitor] 窗口恢复: {hwnd}");
                        OnWindowRestored(hwnd);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[EventDrivenProcessMonitor] 事件处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 前台窗口变化处理
        /// </summary>
        private void OnForegroundWindowChanged(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            // 防止重复检测同一个窗口
            if (_lastActiveProcess != null && _lastActiveProcess.WindowHandle == hwnd)
            {
                return;
            }

            // 节流：避免过于频繁的检测（最小间隔500ms）
            var now = DateTime.Now;
            if ((now - _lastDetectionTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastDetectionTime = now;

            // 从缓存获取或检测新窗口
            ProcessInfo processInfo = GetOrCacheProcessInfo(hwnd);
            if (processInfo != null)
            {
                _lastActiveProcess = processInfo;
                var detectedScenario = DetectScenarioForProcess(processInfo);
                
                if (detectedScenario != _lastDetectedScenario)
                {
                    _lastDetectedScenario = detectedScenario;
                    Logger.Info($"[EventDrivenProcessMonitor] 场景变化: {detectedScenario} (触发: {processInfo.ProcessName})");
                    ScenarioDetected?.Invoke(detectedScenario, processInfo.ProcessName);
                }
            }
        }

        /// <summary>
        /// 窗口最小化处理
        /// </summary>
        private void OnWindowMinimized(IntPtr hwnd)
        {
            // 如果最小化的是当前活动窗口，重新检测前台
            if (_lastActiveProcess?.WindowHandle == hwnd)
            {
                Logger.Debug($"[EventDrivenProcessMonitor] 活动窗口被最小化，重新检测前台");
                CheckCurrentForegroundWindow();
            }
        }

        /// <summary>
        /// 窗口恢复处理
        /// </summary>
        private void OnWindowRestored(IntPtr hwnd)
        {
            // 窗口恢复时立即检测
            OnForegroundWindowChanged(hwnd);
        }

        #endregion

        #region 核心检测逻辑

        /// <summary>
        /// 检测当前前台窗口
        /// </summary>
        private void CheckCurrentForegroundWindow()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    OnForegroundWindowChanged(hwnd);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[EventDrivenProcessMonitor] 检测前台窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取或缓存进程信息
        /// </summary>
        private ProcessInfo GetOrCacheProcessInfo(IntPtr hwnd)
        {
            // 从缓存获取
            if (_windowCache.TryGetValue(hwnd, out ProcessInfo cachedInfo))
            {
                return cachedInfo;
            }

            // 获取新的进程信息
            ProcessInfo processInfo = GetProcessInfoFromWindow(hwnd);
            if (processInfo != null)
            {
                // 添加到缓存（限制缓存大小，避免内存泄漏）
                if (_windowCache.Count > 1000)
                {
                    _windowCache.Clear();
                    Logger.Debug("[EventDrivenProcessMonitor] 窗口缓存已清理（超过限制）");
                }
                
                _windowCache[hwnd] = processInfo;
                Logger.Debug($"[EventDrivenProcessMonitor] 新窗口缓存: {processInfo.ProcessName} ({hwnd})");
            }

            return processInfo;
        }

        /// <summary>
        /// 从窗口句柄获取进程信息
        /// </summary>
        private ProcessInfo GetProcessInfoFromWindow(IntPtr hwnd)
        {
            try
            {
                // 获取进程ID
                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId == 0) return null;

                // 获取进程对象
                using (var process = Process.GetProcessById((int)processId))
                {
                    if (process.HasExited) return null;

                    // 获取窗口标题
                    int length = GetWindowTextLength(hwnd);
                    StringBuilder windowTitle = new StringBuilder(length + 1);
                    GetWindowText(hwnd, windowTitle, windowTitle.Capacity);

                    return new ProcessInfo
                    {
                        ProcessName = process.ProcessName.ToLower(),
                        ProcessId = (int)processId,
                        WindowTitle = windowTitle.ToString(),
                        WindowHandle = hwnd
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"[EventDrivenProcessMonitor] 获取进程信息失败 {hwnd}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 为进程检测场景
        /// </summary>
        private AppScenario DetectScenarioForProcess(ProcessInfo processInfo)
        {
            if (processInfo == null || string.IsNullOrEmpty(processInfo.ProcessName))
                return _defaultScenario;

            // 精确匹配进程名
            var exactMatch = _appRules
                .Where(rule => rule.IsEnabled && !string.IsNullOrEmpty(rule.ProcessName))
                .FirstOrDefault(rule => processInfo.ProcessName.Contains(rule.ProcessName.ToLower()));

            if (exactMatch != null)
            {
                Logger.Debug($"[EventDrivenProcessMonitor] 进程名匹配: {processInfo.ProcessName} -> {exactMatch.Scenario}");
                return exactMatch.Scenario;
            }

            // 窗口标题匹配
            if (!string.IsNullOrEmpty(processInfo.WindowTitle))
            {
                var titleMatch = _appRules
                    .Where(rule => rule.IsEnabled && !string.IsNullOrEmpty(rule.WindowTitle))
                    .FirstOrDefault(rule => processInfo.WindowTitle.ToLower().Contains(rule.WindowTitle.ToLower()));

                if (titleMatch != null)
                {
                    Logger.Debug($"[EventDrivenProcessMonitor] 窗口标题匹配: {processInfo.WindowTitle} -> {titleMatch.Scenario}");
                    return titleMatch.Scenario;
                }
            }

            // 没有匹配的规则，返回默认场景
            return _defaultScenario;
        }

        /// <summary>
        /// 后台扫描回调（备份机制）
        /// </summary>
        private void BackgroundScanCallback(object state)
        {
            try
            {
                Logger.Debug("[EventDrivenProcessMonitor] 执行后台扫描");
                CheckCurrentForegroundWindow();
                
                // 清理过期的窗口缓存
                CleanupWindowCache();
            }
            catch (Exception ex)
            {
                Logger.Error($"[EventDrivenProcessMonitor] 后台扫描异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理窗口缓存中的无效窗口
        /// </summary>
        private void CleanupWindowCache()
        {
            try
            {
                var invalidHandles = new List<IntPtr>();
                
                foreach (var kvp in _windowCache)
                {
                    // 检查窗口是否仍然有效
                    if (!IsWindow(kvp.Key))
                    {
                        invalidHandles.Add(kvp.Key);
                    }
                }

                foreach (var handle in invalidHandles)
                {
                    _windowCache.Remove(handle);
                }

                if (invalidHandles.Count > 0)
                {
                    Logger.Debug($"[EventDrivenProcessMonitor] 清理了 {invalidHandles.Count} 个无效窗口缓存");
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"[EventDrivenProcessMonitor] 清理窗口缓存失败: {ex.Message}");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopMonitoring();
                    _backgroundTimer?.Dispose();
                    _windowCache?.Clear();
                }

                _disposed = true;
                Logger.Info("[EventDrivenProcessMonitor] 资源已释放");
            }
        }

        ~EventDrivenProcessMonitor()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// 进程信息扩展（添加窗口句柄）
    /// </summary>
    public class ProcessInfo
    {
        public string ProcessName { get; set; } = "";
        public int ProcessId { get; set; }
        public string WindowTitle { get; set; } = "";
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
    }
}