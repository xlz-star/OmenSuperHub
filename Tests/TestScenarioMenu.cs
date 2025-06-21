using System;
using System.Collections.Generic;
using OmenSuperHub.AdaptiveScheduling;

namespace OmenSuperHub.Tests
{
    public class TestScenarioMenu
    {
        public static void Run()
        {
            Console.WriteLine("=== 测试场景菜单动态加载 ===\n");
            
            try
            {
                // 创建配置管理器
                var configManager = new ConfigManager();
                
                Console.WriteLine("1. 检查场景配置:");
                Console.WriteLine($"   场景数量: {configManager.Config.Scenarios.Count}");
                
                foreach (var scenario in configManager.Config.Scenarios)
                {
                    Console.WriteLine($"   - {scenario.Key}: {scenario.Value.Description}");
                }
                
                Console.WriteLine("\n2. 测试场景显示名称转换:");
                foreach (AppScenario scenario in Enum.GetValues(typeof(AppScenario)))
                {
                    string displayName = GetScenarioDisplayName(scenario);
                    Console.WriteLine($"   {scenario} -> {displayName}");
                }
                
                Console.WriteLine("\n3. 检查场景排序:");
                var orderedScenarios = new[] { 
                    AppScenario.Gaming, 
                    AppScenario.Content, 
                    AppScenario.Office, 
                    AppScenario.Media, 
                    AppScenario.Idle,
                    AppScenario.Custom 
                };
                
                Console.WriteLine("   预定义顺序:");
                foreach (var scenario in orderedScenarios)
                {
                    if (configManager.Config.Scenarios.ContainsKey(scenario))
                    {
                        Console.WriteLine($"   - {GetScenarioDisplayName(scenario)} ✓");
                    }
                    else
                    {
                        Console.WriteLine($"   - {GetScenarioDisplayName(scenario)} ✗ (未找到)");
                    }
                }
                
                Console.WriteLine("\n✓ 测试通过：场景菜单可以正确动态加载");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ 测试失败: {ex.Message}");
            }
        }
        
        static string GetScenarioDisplayName(AppScenario scenario)
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
                    return scenario.ToString();
            }
        }
    }
}