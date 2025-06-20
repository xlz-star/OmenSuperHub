using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 应用场景枚举
    /// </summary>
    public enum AppScenario
    {
        Gaming,      // 游戏场景 - 最高性能
        Content,     // 内容创作 - 高性能+稳定
        Office,      // 办公场景 - 平衡模式
        Media,       // 影音娱乐 - 低功耗+静音
        Idle,        // 空闲状态 - 节能模式
        Custom       // 自定义场景
    }

    /// <summary>
    /// 性能配置类
    /// </summary>
    public class PerformanceConfig
    {
        public string FanTable { get; set; } = "silent";
        public string FanMode { get; set; } = "performance";
        public string FanControl { get; set; } = "auto";
        public string TempSensitivity { get; set; } = "high";
        public string CpuPower { get; set; } = "max";
        public string GpuPower { get; set; } = "max";
        public int GpuClock { get; set; } = 0;
        public int DBVersion { get; set; } = 2;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 应用识别规则
    /// </summary>
    public class AppRule
    {
        public string ProcessName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public AppScenario Scenario { get; set; } = AppScenario.Office;
        public int Priority { get; set; } = 1; // 优先级 1-10，数字越大优先级越高
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 场景配置管理类
    /// </summary>
    public class ScenarioConfig
    {
        public Dictionary<AppScenario, PerformanceConfig> Scenarios { get; set; } = new Dictionary<AppScenario, PerformanceConfig>();
        public List<AppRule> AppRules { get; set; } = new List<AppRule>();
        public bool IsAutoSchedulingEnabled { get; set; } = false;
        public AppScenario CurrentScenario { get; set; } = AppScenario.Office;
        public AppScenario DefaultScenario { get; set; } = AppScenario.Office; // 默认场景
        public int ScanInterval { get; set; } = 30; // 扫描间隔（秒）
        public float CpuThreshold { get; set; } = 80.0f; // CPU占用阈值
        public float GpuThreshold { get; set; } = 50.0f; // GPU占用阈值

        public ScenarioConfig()
        {
            InitializeDefaultScenarios();
            // 不再自动初始化默认应用规则，让用户手动选择
        }

        /// <summary>
        /// 只初始化默认场景配置（不初始化应用规则）
        /// </summary>
        public void InitializeDefaultScenariosOnly()
        {
            InitializeDefaultScenarios();
        }

        /// <summary>
        /// 初始化默认场景配置
        /// </summary>
        private void InitializeDefaultScenarios()
        {
            // 游戏模式
            Scenarios[AppScenario.Gaming] = new PerformanceConfig
            {
                FanTable = "cool",
                FanMode = "performance",
                FanControl = "auto",
                TempSensitivity = "high",
                CpuPower = "max",
                GpuPower = "max",
                GpuClock = 0,
                DBVersion = 1,
                Description = "游戏模式 - 最高性能"
            };

            // 内容创作模式
            Scenarios[AppScenario.Content] = new PerformanceConfig
            {
                FanTable = "cool",
                FanMode = "performance",
                FanControl = "auto",
                TempSensitivity = "medium",
                CpuPower = "90 W",
                GpuPower = "med",
                GpuClock = 0,
                DBVersion = 2,
                Description = "内容创作模式 - 高性能稳定"
            };

            // 办公模式
            Scenarios[AppScenario.Office] = new PerformanceConfig
            {
                FanTable = "silent",
                FanMode = "default",
                FanControl = "auto",
                TempSensitivity = "low",
                CpuPower = "50 W",
                GpuPower = "min",
                GpuClock = 0,
                DBVersion = 2,
                Description = "办公模式 - 平衡性能"
            };

            // 娱乐模式
            Scenarios[AppScenario.Media] = new PerformanceConfig
            {
                FanTable = "silent",
                FanMode = "default",
                FanControl = "2000 RPM",
                TempSensitivity = "low",
                CpuPower = "40 W",
                GpuPower = "min",
                GpuClock = 0,
                DBVersion = 2,
                Description = "娱乐模式 - 低功耗静音"
            };

            // 节能模式
            Scenarios[AppScenario.Idle] = new PerformanceConfig
            {
                FanTable = "silent",
                FanMode = "default",
                FanControl = "1600 RPM",
                TempSensitivity = "low",
                CpuPower = "30 W",
                GpuPower = "min",
                GpuClock = 0,
                DBVersion = 2,
                Description = "节能模式 - 最低功耗"
            };

            // 自定义模式
            Scenarios[AppScenario.Custom] = new PerformanceConfig
            {
                FanTable = "silent",
                FanMode = "performance",
                FanControl = "auto",
                TempSensitivity = "high",
                CpuPower = "max",
                GpuPower = "max",
                GpuClock = 0,
                DBVersion = 2,
                Description = "自定义模式 - 用户自定义"
            };
        }

        /// <summary>
        /// 初始化默认应用规则
        /// </summary>
        public void InitializeDefaultAppRules()
        {
            // 游戏应用
            var gamingApps = new[]
            {
                "steam", "steamwebhelper", "epicgameslauncher", "origin", "originwebhelperservice",
                "uplay", "ubisoft connect", "battle.net", "battlenet", "gog galaxy",
                "csgo", "cs2", "dota2", "pubg", "tslgame", "valorant", "valorant-win64-shipping",
                "league of legends", "leagueclient", "minecraft", "javaw",
                "wow", "worldofwarcraft", "overwatch", "cod", "gta5", "gtav",
                "destiny2", "apex", "r5apex", "fortnite", "fortniteclient-win64-shipping",
                "cyberpunk2077", "witcher3", "rdr2", "sekiro", "eldenring"
            };

            foreach (var app in gamingApps)
            {
                AppRules.Add(new AppRule
                {
                    ProcessName = app.ToLower(),
                    Scenario = AppScenario.Gaming,
                    Priority = 9,
                    Description = "游戏应用"
                });
            }

            // 内容创作应用
            var contentApps = new[]
            {
                "photoshop", "illustrator", "premierepro", "aftereffects", "audition",
                "blender", "3dsmax", "maya", "cinema4d", "substance painter",
                "davinci resolve", "obs64", "obs32", "streamlabs obs", "xsplit",
                "unity", "unityeditor", "unrealed", "ue4editor", "ue5editor"
            };

            foreach (var app in contentApps)
            {
                AppRules.Add(new AppRule
                {
                    ProcessName = app.ToLower(),
                    Scenario = AppScenario.Content,
                    Priority = 8,
                    Description = "内容创作应用"
                });
            }

            // 办公应用
            var officeApps = new[]
            {
                "winword", "excel", "powerpnt", "outlook", "onenote",
                "chrome", "firefox", "edge", "opera", "brave",
                "notepad++", "vscode", "code", "sublime_text", "atom",
                "idea64", "intellij", "eclipse", "android studio"
            };

            foreach (var app in officeApps)
            {
                AppRules.Add(new AppRule
                {
                    ProcessName = app.ToLower(),
                    Scenario = AppScenario.Office,
                    Priority = 5,
                    Description = "办公应用"
                });
            }

            // 娱乐应用
            var mediaApps = new[]
            {
                "vlc", "potplayer", "kmplayer", "windows media player", "wmplayer",
                "spotify", "itunes", "foobar2000", "aimp", "musicbee",
                "netflix", "youtube", "bilibili", "iqiyi", "tencent video"
            };

            foreach (var app in mediaApps)
            {
                AppRules.Add(new AppRule
                {
                    ProcessName = app.ToLower(),
                    Scenario = AppScenario.Media,
                    Priority = 3,
                    Description = "娱乐应用"
                });
            }
        }
    }
}
