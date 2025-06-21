using System;
using System.Threading.Tasks;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 监控器类型枚举
    /// </summary>
    public enum MonitorType
    {
        Timer,      // 传统的定时器轮询模式
        EventDriven // 事件驱动模式
    }

    /// <summary>
    /// 自适应调度引擎
    /// </summary>
    public class AdaptiveScheduler : IDisposable
    {
        private readonly ConfigManager _configManager;
        private readonly ProcessMonitor _processMonitor;
        private readonly EventDrivenProcessMonitor _eventDrivenMonitor;
        private readonly PerformanceController _performanceController;
        private bool _isEnabled = false;
        private AppScenario _currentScenario = AppScenario.Office;
        private bool _isManualOverride = false;
        private MonitorType _currentMonitorType = MonitorType.EventDriven;

        public event Action<AppScenario, string> ScenarioChanged;

        public bool IsEnabled => _isEnabled;
        public AppScenario CurrentScenario => _currentScenario;

        public AdaptiveScheduler(MonitorType monitorType = MonitorType.EventDriven)
        {
            Logger.ClearLog(); // 每次启动时清空日志
            Logger.Info($"[AdaptiveScheduler] 开始初始化自适应调度器，监控模式: {monitorType}");
            _configManager = new ConfigManager();
            Logger.Info($"[AdaptiveScheduler] ConfigManager创建完成，AppRules数量: {_configManager.Config.AppRules.Count}");
            
            // 初始化两种监控器
            _processMonitor = new ProcessMonitor(_configManager.Config.AppRules, _configManager.Config.ScanInterval);
            _processMonitor.UpdateDefaultScenario(_configManager.Config.DefaultScenario);
            _processMonitor.ScenarioDetected += OnScenarioDetected;

            _eventDrivenMonitor = new EventDrivenProcessMonitor(_configManager.Config.AppRules);
            _eventDrivenMonitor.UpdateDefaultScenario(_configManager.Config.DefaultScenario);
            _eventDrivenMonitor.UpdateDebounceDelay(_configManager.Config.DebounceDelay);
            _eventDrivenMonitor.ScenarioDetected += OnScenarioDetected;

            _performanceController = new PerformanceController();
            
            // 从配置加载监控模式，如果配置中没有则使用参数传入的默认值
            if (_configManager.Config.MonitorMode == "Timer")
            {
                _currentMonitorType = MonitorType.Timer;
            }
            else if (_configManager.Config.MonitorMode == "EventDriven")
            {
                _currentMonitorType = MonitorType.EventDriven;
            }
            else
            {
                _currentMonitorType = monitorType;
            }

            // 加载保存的状态
            _isEnabled = _configManager.Config.IsAutoSchedulingEnabled;
            _currentScenario = _configManager.Config.CurrentScenario;
            Logger.Info($"[AdaptiveScheduler] 初始化完成，当前场景: {_currentScenario}, 自动调度: {_isEnabled}, 监控模式: {_currentMonitorType}");
            
            // 如果配置中启用了自动调度，则启动监控
            if (_isEnabled)
            {
                Logger.Info($"[AdaptiveScheduler] 配置中已启用自动调度，正在启动监控器...");
                Enable();
            }
        }

        /// <summary>
        /// 启用自适应调度
        /// </summary>
        public void Enable()
        {
            if (_isEnabled) 
            {
                Logger.Info($"[AdaptiveScheduler] 自适应调度已经启用，跳过重复启用");
                return;
            }

            _isEnabled = true;
            _isManualOverride = false;
            _configManager.SetAutoSchedulingEnabled(true);
            
            Logger.Info($"[AdaptiveScheduler] 启用自适应调度，当前监控模式: {_currentMonitorType}");
            
            // 根据当前监控模式启动相应的监控器
            if (_currentMonitorType == MonitorType.EventDriven)
            {
                _eventDrivenMonitor.StartMonitoring();
                Logger.Info($"[AdaptiveScheduler] 启用事件驱动监控器");
            }
            else
            {
                _processMonitor.StartMonitoring();
                Logger.Info($"[AdaptiveScheduler] 启用定时器监控器");
            }

            // 立即检测当前场景
            Task.Run(() => TriggerDetection());
        }

        /// <summary>
        /// 禁用自适应调度
        /// </summary>
        public void Disable()
        {
            if (!_isEnabled) return;

            _isEnabled = false;
            _configManager.SetAutoSchedulingEnabled(false);
            
            // 停止所有监控器
            _processMonitor.StopMonitoring();
            _eventDrivenMonitor.StopMonitoring();
            Logger.Info($"[AdaptiveScheduler] 禁用自适应调度，所有监控器已停止");
        }

        /// <summary>
        /// 手动设置场景（会临时禁用自动调度）
        /// </summary>
        public void SetScenario(AppScenario scenario, bool isManualOverride = true)
        {
            _isManualOverride = isManualOverride;
            ApplyScenario(scenario, "手动设置");
        }

        /// <summary>
        /// 获取当前配置管理器
        /// </summary>
        public ConfigManager GetConfigManager()
        {
            return _configManager;
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            Logger.Debug($"[AdaptiveScheduler] 开始重新加载配置");
            _configManager.LoadConfig();

            // 停止所有监控器
            _processMonitor.StopMonitoring();
            _eventDrivenMonitor.StopMonitoring();

            // 更新两个监控器的应用规则和默认场景
            _processMonitor.UpdateAppRules(_configManager.Config.AppRules);
            _processMonitor.UpdateDefaultScenario(_configManager.Config.DefaultScenario);
            _eventDrivenMonitor.UpdateAppRules(_configManager.Config.AppRules);
            _eventDrivenMonitor.UpdateDefaultScenario(_configManager.Config.DefaultScenario);
            _eventDrivenMonitor.UpdateDebounceDelay(_configManager.Config.DebounceDelay);
            Logger.Info($"[AdaptiveScheduler] 两个监控器已更新AppRules，数量: {_configManager.Config.AppRules.Count}");

            if (_isEnabled)
            {
                if (_currentMonitorType == MonitorType.EventDriven)
                {
                    _eventDrivenMonitor.StartMonitoring();
                    Logger.Debug($"[AdaptiveScheduler] 重新启动事件驱动监控");
                }
                else
                {
                    _processMonitor.StartMonitoring();
                    Logger.Debug($"[AdaptiveScheduler] 重新启动定时器监控");
                }
            }
        }

        /// <summary>
        /// 场景检测事件处理
        /// </summary>
        private void OnScenarioDetected(AppScenario scenario, string processName)
        {
            // 如果手动覆盖了，则忽略自动检测
            if (_isManualOverride) return;

            // 如果没有启用自动调度，则忽略
            if (!_isEnabled) return;

            ApplyScenario(scenario, processName);
        }

        /// <summary>
        /// 应用场景配置
        /// </summary>
        private void ApplyScenario(AppScenario scenario, string triggerSource)
        {
            if (_currentScenario == scenario) return;

            try
            {
                var config = _configManager.GetScenarioConfig(scenario);
                _performanceController.ApplyConfig(config);

                _currentScenario = scenario;
                _configManager.SetCurrentScenario(scenario);

                ScenarioChanged?.Invoke(scenario, triggerSource);

                Console.WriteLine($"切换到场景: {GetScenarioDisplayName(scenario)} (触发源: {triggerSource})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用场景配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除手动覆盖状态
        /// </summary>
        public void ClearManualOverride()
        {
            _isManualOverride = false;
            if (_isEnabled)
            {
                // 重新检测当前场景
                Task.Run(() => TriggerDetection());
            }
        }

        /// <summary>
        /// 切换监控模式
        /// </summary>
        public void SetMonitorType(MonitorType monitorType)
        {
            if (_currentMonitorType == monitorType) return;

            bool wasEnabled = _isEnabled;
            
            // 如果当前启用，先停止所有监控器
            if (_isEnabled)
            {
                _processMonitor.StopMonitoring();
                _eventDrivenMonitor.StopMonitoring();
            }

            _currentMonitorType = monitorType;
            Logger.Info($"[AdaptiveScheduler] 切换监控模式为: {monitorType}");

            // 如果之前启用，重新启动新的监控器
            if (wasEnabled)
            {
                if (_currentMonitorType == MonitorType.EventDriven)
                {
                    _eventDrivenMonitor.StartMonitoring();
                }
                else
                {
                    _processMonitor.StartMonitoring();
                }
                
                // 立即检测当前场景
                Task.Run(() => TriggerDetection());
            }
        }

        /// <summary>
        /// 获取当前监控模式
        /// </summary>
        public MonitorType GetMonitorType()
        {
            return _currentMonitorType;
        }

        /// <summary>
        /// 触发检测
        /// </summary>
        public void TriggerDetection()
        {
            if (_currentMonitorType == MonitorType.EventDriven)
            {
                _eventDrivenMonitor.TriggerDetection();
            }
            else
            {
                _processMonitor.TriggerDetection();
            }
        }

        /// <summary>
        /// 获取场景显示名称
        /// </summary>
        public static string GetScenarioDisplayName(AppScenario scenario)
        {
            switch (scenario)
            {
                case AppScenario.Gaming:
                    return "游戏模式";
                case AppScenario.Content:
                    return "创作模式";
                case AppScenario.Office:
                    return "办公模式";
                case AppScenario.Media:
                    return "娱乐模式";
                case AppScenario.Idle:
                    return "节能模式";
                case AppScenario.Custom:
                    return "自定义模式";
                default:
                    return "未知模式";
            }
        }

        /// <summary>
        /// 从显示名称获取场景枚举
        /// </summary>
        public static AppScenario GetScenarioFromDisplayName(string displayName)
        {
            switch (displayName)
            {
                case "游戏模式":
                    return AppScenario.Gaming;
                case "创作模式":
                    return AppScenario.Content;
                case "办公模式":
                    return AppScenario.Office;
                case "娱乐模式":
                    return AppScenario.Media;
                case "节能模式":
                    return AppScenario.Idle;
                case "自定义模式":
                    return AppScenario.Custom;
                default:
                    return AppScenario.Office;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _processMonitor?.StopMonitoring();
            _eventDrivenMonitor?.StopMonitoring();
            _eventDrivenMonitor?.Dispose();
            Logger.Info($"[AdaptiveScheduler] 已释放所有监控器资源");
        }
    }

    /// <summary>
    /// 性能控制器（与现有的OmenSuperHub集成）
    /// </summary>
    public class PerformanceController
    {
        // 委托用于调用Program类的方法
        public static Action<PerformanceConfig> ApplyConfigDelegate;
        
        // 测试模式标志，用于跳过硬件调用
        public static bool IsTestMode = false;

        /// <summary>
        /// 应用性能配置
        /// </summary>
        public void ApplyConfig(PerformanceConfig config)
        {
            try
            {
                // 如果设置了委托，则使用委托调用
                if (ApplyConfigDelegate != null)
                {
                    ApplyConfigDelegate(config);
                }
                else
                {
                    // 否则直接调用硬件接口（仅用于测试）
                    ApplyConfigDirect(config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用性能配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 直接应用配置（用于测试或独立运行）
        /// </summary>
        private void ApplyConfigDirect(PerformanceConfig config)
        {
            // 如果是测试模式，跳过硬件调用
            if (IsTestMode)
            {
                Console.WriteLine($"测试模式：模拟应用配置 - FanMode: {config.FanMode}, FanControl: {config.FanControl}, CpuPower: {config.CpuPower}, GpuPower: {config.GpuPower}");
                return;
            }

            try
            {
                // 应用性能模式
                if (config.FanMode == "performance")
                {
                    OmenHardware.SetFanMode(0x31);
                }
                else if (config.FanMode == "default")
                {
                    OmenHardware.SetFanMode(0x30);
                }

                // 应用风扇控制
                if (config.FanControl == "auto")
                {
                    OmenHardware.SetMaxFanSpeedOff();
                }
                else if (config.FanControl == "max")
                {
                    OmenHardware.SetMaxFanSpeedOn();
                }
                else if (config.FanControl.Contains(" RPM"))
                {
                    OmenHardware.SetMaxFanSpeedOff();
                    int rpmValue = int.Parse(config.FanControl.Replace(" RPM", "").Trim());
                    OmenHardware.SetFanLevel(rpmValue / 100, rpmValue / 100);
                }

                // 应用CPU功率
                if (config.CpuPower == "max")
                {
                    OmenHardware.SetCpuPowerLimit(254);
                }
                else if (config.CpuPower.Contains(" W"))
                {
                    int value = int.Parse(config.CpuPower.Replace(" W", "").Trim());
                    if (value > 10 && value <= 254)
                    {
                        OmenHardware.SetCpuPowerLimit((byte)value);
                    }
                }

                // 应用GPU功率
                switch (config.GpuPower)
                {
                    case "max":
                        OmenHardware.SetMaxGpuPower();
                        break;
                    case "med":
                        OmenHardware.SetMedGpuPower();
                        break;
                    case "min":
                        OmenHardware.SetMinGpuPower();
                        break;
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException("硬件控制需要管理员权限。请以管理员身份运行程序，或在测试模式下运行。");
            }
        }
    }
}
