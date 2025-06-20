using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmenSuperHub.AdaptiveScheduling;

namespace OmenSuperHub.Tests
{
    [TestClass]
    public class AdaptiveSchedulingTests
    {
        [TestMethod]
        public void ScenarioConfig_Initialization_ShouldCreateDefaultScenarios()
        {
            // Arrange & Act
            var config = new ScenarioConfig();

            // Assert
            Assert.IsNotNull(config.Scenarios);
            Assert.AreEqual(6, config.Scenarios.Count); // 6个场景
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Gaming));
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Content));
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Office));
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Media));
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Idle));
            Assert.IsTrue(config.Scenarios.ContainsKey(AppScenario.Custom));
        }

        [TestMethod]
        public void ScenarioConfig_Gaming_ShouldHaveCorrectSettings()
        {
            // Arrange
            var config = new ScenarioConfig();

            // Act
            var gamingConfig = config.Scenarios[AppScenario.Gaming];

            // Assert
            Assert.AreEqual("cool", gamingConfig.FanTable);
            Assert.AreEqual("performance", gamingConfig.FanMode);
            Assert.AreEqual("max", gamingConfig.CpuPower);
            Assert.AreEqual("max", gamingConfig.GpuPower);
            Assert.AreEqual(1, gamingConfig.DBVersion);
        }

        [TestMethod]
        public void ScenarioConfig_Office_ShouldHaveCorrectSettings()
        {
            // Arrange
            var config = new ScenarioConfig();

            // Act
            var officeConfig = config.Scenarios[AppScenario.Office];

            // Assert
            Assert.AreEqual("silent", officeConfig.FanTable);
            Assert.AreEqual("default", officeConfig.FanMode);
            Assert.AreEqual("50 W", officeConfig.CpuPower);
            Assert.AreEqual("min", officeConfig.GpuPower);
            Assert.AreEqual(2, officeConfig.DBVersion);
        }

        [TestMethod]
        public void ScenarioConfig_InitializeDefaultAppRules_ShouldCreateRules()
        {
            // Arrange & Act
            var config = new ScenarioConfig();

            // Assert
            Assert.IsNotNull(config.AppRules);
            Assert.IsTrue(config.AppRules.Count > 0);
            
            // 检查是否包含常见的游戏应用
            Assert.IsTrue(config.AppRules.Any(r => r.ProcessName.Contains("steam")));
            Assert.IsTrue(config.AppRules.Any(r => r.ProcessName.Contains("chrome")));
        }

        [TestMethod]
        public void AppRule_GamingApp_ShouldHaveHighPriority()
        {
            // Arrange
            var config = new ScenarioConfig();

            // Act
            var steamRule = config.AppRules.FirstOrDefault(r => r.ProcessName.Contains("steam"));

            // Assert
            Assert.IsNotNull(steamRule);
            Assert.AreEqual(AppScenario.Gaming, steamRule.Scenario);
            Assert.AreEqual(9, steamRule.Priority);
            Assert.IsTrue(steamRule.IsEnabled);
        }

        [TestMethod]
        public void ConfigManager_LoadConfig_ShouldNotThrowException()
        {
            // Arrange & Act & Assert
            DoesNotThrow(() => {
                var configManager = new ConfigManager();
                Assert.IsNotNull(configManager.Config);
            });
        }

        [TestMethod]
        public void ConfigManager_GetScenarioConfig_ShouldReturnCorrectConfig()
        {
            // Arrange
            var configManager = new ConfigManager();

            // Act
            var gamingConfig = configManager.GetScenarioConfig(AppScenario.Gaming);
            var officeConfig = configManager.GetScenarioConfig(AppScenario.Office);

            // Assert
            Assert.IsNotNull(gamingConfig);
            Assert.IsNotNull(officeConfig);
            Assert.AreEqual("cool", gamingConfig.FanTable);
            Assert.AreEqual("silent", officeConfig.FanTable);
        }

        [TestMethod]
        public void ConfigManager_GetScenarioConfig_InvalidScenario_ShouldReturnOfficeConfig()
        {
            // Arrange
            var configManager = new ConfigManager();

            // Act
            var config = configManager.GetScenarioConfig((AppScenario)999);

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual("silent", config.FanTable); // 应该返回Office配置
        }

        [TestMethod]
        public void ProcessMonitor_Constructor_ShouldNotThrowException()
        {
            // Arrange
            var appRules = new List<AppRule>
            {
                new AppRule { ProcessName = "test", Scenario = AppScenario.Gaming, Priority = 5 }
            };

            // Act & Assert
            DoesNotThrow(() => {
                var monitor = new ProcessMonitor(appRules, 30);
                Assert.IsNotNull(monitor);
            });
        }

        [TestMethod]
        public void ProcessMonitor_DetectCurrentScenario_ShouldReturnValidScenario()
        {
            // Arrange
            var appRules = new List<AppRule>
            {
                new AppRule { ProcessName = "notepad", Scenario = AppScenario.Office, Priority = 5, IsEnabled = true },
                new AppRule { ProcessName = "steam", Scenario = AppScenario.Gaming, Priority = 9, IsEnabled = true }
            };
            var monitor = new ProcessMonitor(appRules, 30);

            // Act
            var scenario = monitor.DetectCurrentScenario();

            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(AppScenario), scenario));
        }

        [TestMethod]
        public void AdaptiveScheduler_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act & Assert
            DoesNotThrow(() => {
                var scheduler = new AdaptiveScheduler();
                Assert.IsNotNull(scheduler);
                Assert.IsFalse(scheduler.IsEnabled); // 默认应该是禁用状态
                Assert.IsTrue(Enum.IsDefined(typeof(AppScenario), scheduler.CurrentScenario));
            });
        }

        [TestMethod]
        public void AdaptiveScheduler_Enable_ShouldSetEnabledState()
        {
            // Arrange
            var scheduler = new AdaptiveScheduler();

            // Act
            scheduler.Enable();

            // Assert
            Assert.IsTrue(scheduler.IsEnabled);
        }

        [TestMethod]
        public void AdaptiveScheduler_Disable_ShouldSetDisabledState()
        {
            // Arrange
            var scheduler = new AdaptiveScheduler();
            scheduler.Enable();

            // Act
            scheduler.Disable();

            // Assert
            Assert.IsFalse(scheduler.IsEnabled);
        }

        [TestMethod]
        public void AdaptiveScheduler_SetScenario_ShouldChangeCurrentScenario()
        {
            // Arrange
            var scheduler = new AdaptiveScheduler();
            bool scenarioChanged = false;
            AppScenario changedScenario = AppScenario.Office;

            scheduler.ScenarioChanged += (scenario, source) => {
                scenarioChanged = true;
                changedScenario = scenario;
            };

            // Act
            scheduler.SetScenario(AppScenario.Gaming);

            // Assert
            Assert.AreEqual(AppScenario.Gaming, scheduler.CurrentScenario);
            Assert.IsTrue(scenarioChanged);
            Assert.AreEqual(AppScenario.Gaming, changedScenario);
        }

        [TestMethod]
        public void AdaptiveScheduler_GetScenarioDisplayName_ShouldReturnCorrectNames()
        {
            // Act & Assert
            Assert.AreEqual("游戏模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Gaming));
            Assert.AreEqual("创作模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Content));
            Assert.AreEqual("办公模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Office));
            Assert.AreEqual("娱乐模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Media));
            Assert.AreEqual("节能模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Idle));
            Assert.AreEqual("自定义模式", AdaptiveScheduler.GetScenarioDisplayName(AppScenario.Custom));
        }

        [TestMethod]
        public void PerformanceController_ApplyConfig_WithoutDelegate_ShouldNotThrowException()
        {
            // Arrange
            var controller = new PerformanceController();
            var config = new PerformanceConfig
            {
                FanMode = "performance",
                FanControl = "auto",
                CpuPower = "max",
                GpuPower = "max"
            };

            // 确保委托为空
            PerformanceController.ApplyConfigDelegate = null;

            // Act & Assert
            DoesNotThrow(() => controller.ApplyConfig(config));
        }

        [TestMethod]
        public void PerformanceController_ApplyConfig_WithDelegate_ShouldCallDelegate()
        {
            // Arrange
            var controller = new PerformanceController();
            var config = new PerformanceConfig
            {
                FanMode = "performance",
                CpuPower = "max"
            };

            bool delegateCalled = false;
            PerformanceConfig receivedConfig = null;

            PerformanceController.ApplyConfigDelegate = (cfg) => {
                delegateCalled = true;
                receivedConfig = cfg;
            };

            // Act
            controller.ApplyConfig(config);

            // Assert
            Assert.IsTrue(delegateCalled);
            Assert.AreEqual(config, receivedConfig);

            // Cleanup
            PerformanceController.ApplyConfigDelegate = null;
        }

        [TestMethod]
        public void ProcessInfo_Properties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var processInfo = new ProcessInfo
            {
                ProcessName = "test.exe",
                ProcessId = 1234,
                WindowTitle = "Test Window"
            };

            // Assert
            Assert.AreEqual("test.exe", processInfo.ProcessName);
            Assert.AreEqual(1234, processInfo.ProcessId);
            Assert.AreEqual("Test Window", processInfo.WindowTitle);
        }

        [TestMethod]
        public void AppRule_Properties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var rule = new AppRule
            {
                ProcessName = "game.exe",
                WindowTitle = "Game Window",
                Scenario = AppScenario.Gaming,
                Priority = 9,
                IsEnabled = true,
                Description = "Gaming application"
            };

            // Assert
            Assert.AreEqual("game.exe", rule.ProcessName);
            Assert.AreEqual("Game Window", rule.WindowTitle);
            Assert.AreEqual(AppScenario.Gaming, rule.Scenario);
            Assert.AreEqual(9, rule.Priority);
            Assert.IsTrue(rule.IsEnabled);
            Assert.AreEqual("Gaming application", rule.Description);
        }

        // 测试辅助方法
        private static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
