using System;
using System.Collections.Generic;
using OmenSuperHub.AdaptiveScheduling;

namespace OmenSuperHub.Tests
{
    /// <summary>
    /// 事件驱动监控器测试类
    /// </summary>
    public class EventDrivenMonitorTest
    {
        /// <summary>
        /// 测试事件驱动监控器基本功能
        /// </summary>
        public static void TestBasicFunctionality()
        {
            try
            {
                Console.WriteLine("开始测试事件驱动监控器...");
                
                // 创建测试应用规则
                var testRules = new List<AppRule>
                {
                    new AppRule
                    {
                        ProcessName = "notepad",
                        Scenario = AppScenario.Office,
                        Priority = 5,
                        IsEnabled = true,
                        Description = "记事本测试"
                    },
                    new AppRule
                    {
                        ProcessName = "chrome",
                        Scenario = AppScenario.Media,
                        Priority = 3,
                        IsEnabled = true,
                        Description = "浏览器测试"
                    }
                };

                // 创建事件驱动监控器
                using (var monitor = new EventDrivenProcessMonitor(testRules))
                {
                    // 设置事件处理
                    monitor.ScenarioDetected += (scenario, processName) =>
                    {
                        Console.WriteLine($"检测到场景变化: {scenario} (触发进程: {processName})");
                    };

                    // 测试更新应用规则
                    Console.WriteLine("测试更新应用规则...");
                    var newRules = new List<AppRule>
                    {
                        new AppRule
                        {
                            ProcessName = "firefox",
                            Scenario = AppScenario.Gaming,
                            Priority = 8,
                            IsEnabled = true,
                            Description = "Firefox测试"
                        }
                    };
                    monitor.UpdateAppRules(newRules);

                    // 测试更新默认场景
                    Console.WriteLine("测试更新默认场景...");
                    monitor.UpdateDefaultScenario(AppScenario.Media);

                    // 测试启动监控
                    Console.WriteLine("测试启动监控...");
                    monitor.StartMonitoring();
                    
                    // 模拟运行5秒
                    System.Threading.Thread.Sleep(5000);
                    
                    // 测试手动触发检测
                    Console.WriteLine("测试手动触发检测...");
                    monitor.TriggerDetection();
                    
                    // 再等待2秒
                    System.Threading.Thread.Sleep(2000);
                    
                    // 测试停止监控
                    Console.WriteLine("测试停止监控...");
                    monitor.StopMonitoring();
                }

                Console.WriteLine("事件驱动监控器测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex}");
            }
        }

        /// <summary>
        /// 测试AdaptiveScheduler集成
        /// </summary>
        public static void TestAdaptiveSchedulerIntegration()
        {
            try
            {
                Console.WriteLine("开始测试AdaptiveScheduler集成...");
                
                // 测试使用事件驱动模式创建调度器
                using (var scheduler = new AdaptiveScheduler(MonitorType.EventDriven))
                {
                    Console.WriteLine($"当前监控模式: {scheduler.GetMonitorType()}");
                    
                    // 测试切换监控模式
                    Console.WriteLine("切换到定时器模式...");
                    scheduler.SetMonitorType(MonitorType.Timer);
                    Console.WriteLine($"切换后监控模式: {scheduler.GetMonitorType()}");
                    
                    // 切换回事件驱动模式
                    Console.WriteLine("切换回事件驱动模式...");
                    scheduler.SetMonitorType(MonitorType.EventDriven);
                    Console.WriteLine($"最终监控模式: {scheduler.GetMonitorType()}");
                    
                    // 测试启用自适应调度
                    Console.WriteLine("启用自适应调度...");
                    scheduler.Enable();
                    
                    // 运行3秒
                    System.Threading.Thread.Sleep(3000);
                    
                    // 测试禁用
                    Console.WriteLine("禁用自适应调度...");
                    scheduler.Disable();
                }

                Console.WriteLine("AdaptiveScheduler集成测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"集成测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex}");
            }
        }
    }
}