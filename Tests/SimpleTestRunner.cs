using System;
using System.Collections.Generic;
using System.Linq;
using OmenSuperHub.AdaptiveScheduling;

namespace OmenSuperHub.Tests
{
    /// <summary>
    /// 简单的测试运行器，用于验证自适应调度功能
    /// </summary>
    public class SimpleTestRunner
    {
        private static int passedTests = 0;
        private static int failedTests = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("开始运行自适应调度功能测试...\n");
            
            // 启用测试模式以避免硬件权限问题
            PerformanceController.IsTestMode = true;

            // 运行所有测试
            TestScenarioConfigInitialization();
            TestScenarioConfigSettings();
            TestConfigManagerCreation();
            TestConfigManagerScenarioRetrieval();
            TestProcessMonitorCreation();
            TestEventDrivenMonitorCreation();
            TestAdaptiveSchedulerCreation();
            TestAdaptiveSchedulerStateManagement();
            TestAdaptiveSchedulerMonitorTypeSwitching();
            TestScenarioDisplayNames();
            TestPerformanceController();
            TestAppRuleProperties();
            
            // 运行场景菜单测试
            // Console.WriteLine("\n运行独立测试:");
            // 注意：独立测试类需要实现相应的Run方法才能调用

            // 输出测试结果
            Console.WriteLine($"\n测试完成！");
            Console.WriteLine($"通过: {passedTests}");
            Console.WriteLine($"失败: {failedTests}");
            Console.WriteLine($"总计: {passedTests + failedTests}");
            
            if (failedTests == 0)
            {
                Console.WriteLine("所有测试都通过了！✓");
            }
            else
            {
                Console.WriteLine($"有 {failedTests} 个测试失败！✗");
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"✓ {testName}");
                passedTests++;
            }
            else
            {
                Console.WriteLine($"✗ {testName}");
                failedTests++;
            }
        }

        private static void TestScenarioConfigInitialization()
        {
            try
            {
                var config = new ScenarioConfig();
                Assert(config.Scenarios != null, "ScenarioConfig应该初始化Scenarios字典");
                Assert(config.Scenarios.Count == 6, "应该有6个默认场景");
                Assert(config.Scenarios.ContainsKey(AppScenario.Gaming), "应该包含Gaming场景");
                Assert(config.Scenarios.ContainsKey(AppScenario.Office), "应该包含Office场景");
                Assert(config.AppRules != null, "应该初始化AppRules列表");
                Assert(config.AppRules.Count == 0, "初始化时AppRules应该为空，不自动添加默认规则");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ScenarioConfig初始化测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestScenarioConfigSettings()
        {
            try
            {
                var config = new ScenarioConfig();
                var gamingConfig = config.Scenarios[AppScenario.Gaming];
                Assert(gamingConfig.FanTable == "cool", "Gaming模式应该使用cool风扇配置");
                Assert(gamingConfig.FanMode == "performance", "Gaming模式应该使用performance模式");
                Assert(gamingConfig.CpuPower == "max", "Gaming模式应该使用最大CPU功率");
                
                var officeConfig = config.Scenarios[AppScenario.Office];
                Assert(officeConfig.FanTable == "silent", "Office模式应该使用silent风扇配置");
                Assert(officeConfig.FanMode == "default", "Office模式应该使用default模式");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ScenarioConfig设置测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestConfigManagerCreation()
        {
            try
            {
                var configManager = new ConfigManager();
                Assert(configManager.Config != null, "ConfigManager应该有有效的Config");
                Assert(configManager.Config.Scenarios.Count > 0, "Config应该有场景配置");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ConfigManager创建测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestConfigManagerScenarioRetrieval()
        {
            try
            {
                var configManager = new ConfigManager();
                var gamingConfig = configManager.GetScenarioConfig(AppScenario.Gaming);
                var officeConfig = configManager.GetScenarioConfig(AppScenario.Office);
                
                Assert(gamingConfig != null, "应该能获取Gaming配置");
                Assert(officeConfig != null, "应该能获取Office配置");
                Assert(gamingConfig.FanTable == "cool", "Gaming配置应该正确");
                Assert(officeConfig.FanTable == "silent", "Office配置应该正确");
                
                // 测试无效场景返回默认配置
                var invalidConfig = configManager.GetScenarioConfig((AppScenario)999);
                Assert(invalidConfig != null, "无效场景应该返回默认配置");
                Assert(invalidConfig.FanTable == "silent", "无效场景应该返回Office配置");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ConfigManager场景检索测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestProcessMonitorCreation()
        {
            try
            {
                var appRules = new List<AppRule>
                {
                    new AppRule { ProcessName = "test", Scenario = AppScenario.Gaming, Priority = 5 }
                };
                var monitor = new ProcessMonitor(appRules, 30);
                Assert(monitor != null, "ProcessMonitor应该能够创建");
                
                var scenario = monitor.DetectCurrentScenario();
                Assert(Enum.IsDefined(typeof(AppScenario), scenario), "检测到的场景应该是有效的");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ProcessMonitor创建测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestAdaptiveSchedulerCreation()
        {
            try
            {
                var scheduler = new AdaptiveScheduler();
                Assert(scheduler != null, "AdaptiveScheduler应该能够创建");
                Assert(!scheduler.IsEnabled, "默认应该是禁用状态");
                Assert(Enum.IsDefined(typeof(AppScenario), scheduler.CurrentScenario), "当前场景应该是有效的");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ AdaptiveScheduler创建测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestAdaptiveSchedulerStateManagement()
        {
            try
            {
                var scheduler = new AdaptiveScheduler();
                
                // 测试启用/禁用
                scheduler.Enable();
                Assert(scheduler.IsEnabled, "启用后应该是启用状态");
                
                scheduler.Disable();
                Assert(!scheduler.IsEnabled, "禁用后应该是禁用状态");
                
                // 测试场景设置
                bool scenarioChanged = false;
                AppScenario changedScenario = AppScenario.Office;
                
                scheduler.ScenarioChanged += (scenario, source) => {
                    scenarioChanged = true;
                    changedScenario = scenario;
                };
                
                scheduler.SetScenario(AppScenario.Gaming);
                Assert(scheduler.CurrentScenario == AppScenario.Gaming, "应该能设置当前场景");
                Assert(scenarioChanged, "应该触发场景变更事件");
                Assert(changedScenario == AppScenario.Gaming, "事件应该传递正确的场景");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ AdaptiveScheduler状态管理测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestScenarioDisplayNames()
        {
            try
            {
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Gaming) == "游戏模式", "Gaming应该显示为游戏模式");
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Content) == "创作模式", "Content应该显示为创作模式");
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Office) == "办公模式", "Office应该显示为办公模式");
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Media) == "娱乐模式", "Media应该显示为娱乐模式");
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Idle) == "节能模式", "Idle应该显示为节能模式");
                Assert(AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Custom) == "自定义模式", "Custom应该显示为自定义模式");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 场景显示名称测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestPerformanceController()
        {
            try
            {
                var controller = new PerformanceController();
                var config = new PerformanceConfig
                {
                    FanMode = "performance",
                    FanControl = "auto",
                    CpuPower = "max",
                    GpuPower = "max"
                };

                // 测试没有委托的情况
                PerformanceController.ApplyConfigDelegate = null;
                controller.ApplyConfig(config); // 应该不抛出异常
                Assert(true, "没有委托时应该能正常调用ApplyConfig");

                // 测试有委托的情况
                bool delegateCalled = false;
                PerformanceConfig receivedConfig = null;

                PerformanceController.ApplyConfigDelegate = (cfg) => {
                    delegateCalled = true;
                    receivedConfig = cfg;
                };

                controller.ApplyConfig(config);
                Assert(delegateCalled, "应该调用委托");
                Assert(receivedConfig == config, "应该传递正确的配置");

                // 清理
                PerformanceController.ApplyConfigDelegate = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PerformanceController测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestEventDrivenMonitorCreation()
        {
            try
            {
                var appRules = new List<AppRule>
                {
                    new AppRule { ProcessName = "test", Scenario = AppScenario.Gaming, Priority = 5 }
                };
                
                using (var monitor = new EventDrivenProcessMonitor(appRules))
                {
                    Assert(monitor != null, "EventDrivenProcessMonitor应该能够创建");
                    
                    // 测试更新应用规则
                    var newRules = new List<AppRule>
                    {
                        new AppRule { ProcessName = "newtest", Scenario = AppScenario.Content, Priority = 7 }
                    };
                    monitor.UpdateAppRules(newRules);
                    Assert(true, "应该能更新应用规则");
                    
                    // 测试更新默认场景
                    monitor.UpdateDefaultScenario(AppScenario.Media);
                    Assert(true, "应该能更新默认场景");
                    
                    // 测试启动和停止监控
                    monitor.StartMonitoring();
                    Assert(true, "应该能启动监控");
                    
                    monitor.StopMonitoring();
                    Assert(true, "应该能停止监控");
                    
                    // 测试手动触发检测
                    monitor.TriggerDetection();
                    Assert(true, "应该能手动触发检测");
                }
                Assert(true, "应该能正确释放资源");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ EventDrivenProcessMonitor创建测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestAdaptiveSchedulerMonitorTypeSwitching()
        {
            try
            {
                // 测试默认Timer模式
                var scheduler1 = new AdaptiveScheduler();
                Assert(scheduler1.GetMonitorType() == MonitorType.Timer, "默认应该使用Timer模式");
                
                // 测试指定EventDriven模式
                var scheduler2 = new AdaptiveScheduler(MonitorType.EventDriven);
                Assert(scheduler2.GetMonitorType() == MonitorType.EventDriven, "应该使用EventDriven模式");
                
                // 测试运行时切换监控模式
                scheduler1.SetMonitorType(MonitorType.EventDriven);
                Assert(scheduler1.GetMonitorType() == MonitorType.EventDriven, "应该能切换到EventDriven模式");
                
                scheduler1.SetMonitorType(MonitorType.Timer);
                Assert(scheduler1.GetMonitorType() == MonitorType.Timer, "应该能切换回Timer模式");
                
                // 测试启用状态下的模式切换
                scheduler1.Enable();
                scheduler1.SetMonitorType(MonitorType.EventDriven);
                Assert(scheduler1.GetMonitorType() == MonitorType.EventDriven, "启用状态下应该能切换模式");
                scheduler1.Disable();
                
                // 清理资源
                scheduler1.Dispose();
                scheduler2.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ AdaptiveScheduler监控模式切换测试失败: {ex.Message}");
                failedTests++;
            }
        }

        private static void TestAppRuleProperties()
        {
            try
            {
                var rule = new AppRule
                {
                    ProcessName = "game.exe",
                    WindowTitle = "Game Window",
                    Scenario = AppScenario.Gaming,
                    Priority = 9,
                    IsEnabled = true,
                    Description = "Gaming application"
                };

                Assert(rule.ProcessName == "game.exe", "ProcessName应该正确设置");
                Assert(rule.WindowTitle == "Game Window", "WindowTitle应该正确设置");
                Assert(rule.Scenario == AppScenario.Gaming, "Scenario应该正确设置");
                Assert(rule.Priority == 9, "Priority应该正确设置");
                Assert(rule.IsEnabled == true, "IsEnabled应该正确设置");
                Assert(rule.Description == "Gaming application", "Description应该正确设置");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ AppRule属性测试失败: {ex.Message}");
                failedTests++;
            }
        }
    }
}