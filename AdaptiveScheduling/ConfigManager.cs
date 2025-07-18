using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 配置管理器
    /// </summary>
    public class ConfigManager
    {
        private readonly string _configFilePath;
        private readonly string _registryPath = @"Software\OmenSuperHub\AdaptiveScheduling";
        private ScenarioConfig _config;

        public ScenarioConfig Config => _config;

        public ConfigManager()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adaptive_config.json");
            LoadConfig();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                Logger.Debug($"[ConfigManager] 开始加载配置，文件路径: {_configFilePath}");
                if (File.Exists(_configFilePath))
                {
                    Logger.Debug($"[ConfigManager] 配置文件存在，正在读取...");
                    string json = File.ReadAllText(_configFilePath);
                    _config = JsonConvert.DeserializeObject<ScenarioConfig>(json);
                    Logger.Info($"[ConfigManager] 配置加载成功，AppRules数量: {_config.AppRules?.Count ?? 0}");
                    Logger.Info($"[ConfigManager] 场景配置数量: {_config.Scenarios?.Count ?? 0}, 默认场景: {_config.DefaultScenario}");
                    
                    // 如果反序列化后AppRules为null，初始化为空列表（不添加默认规则）
                    if (_config.AppRules == null)
                    {
                        _config.AppRules = new List<AppRule>();
                        Logger.Debug($"[ConfigManager] AppRules为null，初始化为空列表");
                    }

                    // 如果反序列化后Scenarios为null，初始化默认场景
                    if (_config.Scenarios == null || _config.Scenarios.Count == 0)
                    {
                        Logger.Info($"[ConfigManager] Scenarios为空或数量为0，需要初始化默认场景");
                        _config.Scenarios = new Dictionary<AppScenario, PerformanceConfig>();
                        _config.InitializeDefaultScenariosOnly();
                        Logger.Info($"[ConfigManager] 默认场景初始化完成，场景数量: {_config.Scenarios.Count}");
                    }
                    else
                    {
                        Logger.Info($"[ConfigManager] 场景配置从文件加载成功，数量: {_config.Scenarios.Count}");
                    }
                }
                else
                {
                    Logger.Info($"[ConfigManager] 配置文件不存在，创建新配置");
                    _config = new ScenarioConfig();
                    // 初始化默认场景（新配置时才需要）
                    _config.InitializeDefaultScenariosOnly();
                    Logger.Info($"[ConfigManager] 新配置创建完成，场景数量: {_config.Scenarios.Count}, AppRules数量: {_config.AppRules.Count}");
                    SaveConfig();
                    Logger.Info($"[ConfigManager] 新配置已保存");
                }

                // 从注册表加载运行时设置
                LoadRuntimeSettings();
            }
            catch (Exception ex)
            {
                Logger.Error($"配置加载失败: {ex.Message}");
                _config = new ScenarioConfig();
                // 异常情况下也需要初始化默认场景
                _config.InitializeDefaultScenariosOnly();
                Logger.Info($"[ConfigManager] 异常恢复：创建默认配置，场景数量: {_config.Scenarios.Count}");
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                Logger.Debug($"[ConfigManager] 开始保存配置，AppRules数量: {_config.AppRules.Count}");
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
                Logger.Info($"[ConfigManager] 配置已保存到: {_configFilePath}");
                SaveRuntimeSettings();
            }
            catch (Exception ex)
            {
                Logger.Error($"配置保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从注册表加载运行时设置
        /// </summary>
        private void LoadRuntimeSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_registryPath))
                {
                    if (key != null)
                    {
                        _config.IsAutoSchedulingEnabled = Convert.ToBoolean(key.GetValue("IsAutoSchedulingEnabled", false));
                        _config.CurrentScenario = (AppScenario)Enum.Parse(typeof(AppScenario),
                            key.GetValue("CurrentScenario", AppScenario.Office.ToString()).ToString());
                        _config.ScanInterval = Convert.ToInt32(key.GetValue("ScanInterval", 30));
                        _config.CpuThreshold = Convert.ToSingle(key.GetValue("CpuThreshold", 80.0f));
                        _config.GpuThreshold = Convert.ToSingle(key.GetValue("GpuThreshold", 50.0f));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从注册表加载设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存运行时设置到注册表
        /// </summary>
        private void SaveRuntimeSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_registryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("IsAutoSchedulingEnabled", _config.IsAutoSchedulingEnabled);
                        key.SetValue("CurrentScenario", _config.CurrentScenario.ToString());
                        key.SetValue("ScanInterval", _config.ScanInterval);
                        key.SetValue("CpuThreshold", _config.CpuThreshold);
                        key.SetValue("GpuThreshold", _config.GpuThreshold);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置到注册表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取场景配置
        /// </summary>
        public PerformanceConfig GetScenarioConfig(AppScenario scenario)
        {
            return _config.Scenarios.ContainsKey(scenario)
                ? _config.Scenarios[scenario]
                : _config.Scenarios[AppScenario.Office];
        }

        /// <summary>
        /// 更新场景配置
        /// </summary>
        public void UpdateScenarioConfig(AppScenario scenario, PerformanceConfig config)
        {
            _config.Scenarios[scenario] = config;
            SaveConfig();
        }

        /// <summary>
        /// 添加应用规则
        /// </summary>
        public void AddAppRule(AppRule rule)
        {
            _config.AppRules.Add(rule);
            SaveConfig();
        }

        /// <summary>
        /// 移除应用规则
        /// </summary>
        public void RemoveAppRule(AppRule rule)
        {
            _config.AppRules.Remove(rule);
            SaveConfig();
        }

        /// <summary>
        /// 更新应用规则
        /// </summary>
        public void UpdateAppRule(int index, AppRule rule)
        {
            if (index >= 0 && index < _config.AppRules.Count)
            {
                _config.AppRules[index] = rule;
                SaveConfig();
            }
        }

        /// <summary>
        /// 设置自动调度状态
        /// </summary>
        public void SetAutoSchedulingEnabled(bool enabled)
        {
            _config.IsAutoSchedulingEnabled = enabled;
            SaveRuntimeSettings();
        }

        /// <summary>
        /// 设置当前场景
        /// </summary>
        public void SetCurrentScenario(AppScenario scenario)
        {
            _config.CurrentScenario = scenario;
            SaveRuntimeSettings();
        }

        /// <summary>
        /// 设置默认场景
        /// </summary>
        public void SetDefaultScenario(AppScenario scenario)
        {
            _config.DefaultScenario = scenario;
            SaveConfig();
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefault()
        {
            _config = new ScenarioConfig();
            // 重置时需要初始化默认场景和应用规则
            _config.InitializeDefaultScenariosOnly();
            _config.InitializeDefaultAppRules();
            Logger.Info($"[ConfigManager] 重置为默认配置，场景数量: {_config.Scenarios.Count}, AppRules数量: {_config.AppRules.Count}");
            SaveConfig();
        }
    }
}
