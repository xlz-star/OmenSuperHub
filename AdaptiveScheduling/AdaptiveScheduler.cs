using System;
using System.Threading.Tasks;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 自适应调度引擎
    /// </summary>
    public class AdaptiveScheduler
    {
        private readonly ConfigManager _configManager;
        private readonly ProcessMonitor _processMonitor;
        private readonly PerformanceController _performanceController;
        private bool _isEnabled = false;
        private AppScenario _currentScenario = AppScenario.Office;
        private bool _isManualOverride = false;

        public event Action<AppScenario, string> ScenarioChanged;

        public bool IsEnabled => _isEnabled;
        public AppScenario CurrentScenario => _currentScenario;

        public AdaptiveScheduler()
        {
            _configManager = new ConfigManager();
            _processMonitor = new ProcessMonitor(_configManager.Config.AppRules, _configManager.Config.ScanInterval);
            _performanceController = new PerformanceController();

            _processMonitor.ScenarioDetected += OnScenarioDetected;

            // 加载保存的状态
            _isEnabled = _configManager.Config.IsAutoSchedulingEnabled;
            _currentScenario = _configManager.Config.CurrentScenario;
        }

        /// <summary>
        /// 启用自适应调度
        /// </summary>
        public void Enable()
        {
            if (_isEnabled) return;

            _isEnabled = true;
            _isManualOverride = false;
            _configManager.SetAutoSchedulingEnabled(true);
            _processMonitor.StartMonitoring();

            // 立即检测当前场景
            Task.Run(() => _processMonitor.TriggerDetection());
        }

        /// <summary>
        /// 禁用自适应调度
        /// </summary>
        public void Disable()
        {
            if (!_isEnabled) return;

            _isEnabled = false;
            _configManager.SetAutoSchedulingEnabled(false);
            _processMonitor.StopMonitoring();
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
            _configManager.LoadConfig();

            // 更新进程监控器的应用规则
            _processMonitor.StopMonitoring();

            if (_isEnabled)
            {
                _processMonitor.StartMonitoring();
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
                Task.Run(() => _processMonitor.TriggerDetection());
            }
        }

        /// <summary>
        /// 获取场景显示名称
        /// </summary>
        public static string GetScenarioDisplayName(AppScenario scenario)
        {
            return scenario switch
            {
                AppScenario.Gaming => "游戏模式",
                AppScenario.Content => "创作模式",
                AppScenario.Office => "办公模式",
                AppScenario.Media => "娱乐模式",
                AppScenario.Idle => "节能模式",
                AppScenario.Custom => "自定义模式",
                _ => "未知模式"
            };
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _processMonitor?.StopMonitoring();
        }
    }

    /// <summary>
    /// 性能控制器（与现有的OmenSuperHub集成）
    /// </summary>
    public class PerformanceController
    {
        // 委托用于调用Program类的方法
        public static Action<PerformanceConfig> ApplyConfigDelegate;

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
    }
}
