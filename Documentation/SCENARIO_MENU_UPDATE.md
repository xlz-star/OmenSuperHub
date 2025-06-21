# 场景菜单动态加载更新说明

## 问题描述
主菜单（系统托盘菜单）中的场景选择列表使用了硬编码方式，只包含了5个场景（Gaming、Content、Office、Media、Idle），缺少了第6个场景（Custom 自定义模式）。

## 解决方案
将主菜单的场景选择列表从硬编码改为动态加载，使其能够：
1. 从配置文件动态读取所有可用场景
2. 支持未来添加新场景而无需修改代码
3. 保持场景的显示顺序

## 修改内容

### 1. Program.cs - 添加静态变量
```csharp
static ToolStripMenuItem adaptiveSchedulingMenu;
```

### 2. Program.cs - 添加场景显示名称转换方法
```csharp
static string GetScenarioDisplayName(AppScenario scenario) {
    switch (scenario) {
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
```

### 3. Program.cs - 添加动态重建场景菜单方法
```csharp
static void RebuildScenarioMenu() {
    // 动态获取配置并重建场景菜单项
    // 按预定义顺序排列场景
    // 支持所有场景类型，包括自定义场景
}
```

### 4. Program.cs - 修改 InitTrayIcon 方法
- 初始化时添加占位符："场景加载中..."
- 在 adaptiveScheduler 初始化后调用 RebuildScenarioMenu()

### 5. Program.cs - 修改 UpdateAdaptiveMenuState 方法
- 使用 GetScenarioDisplayName 方法代替硬编码的 switch 语句

### 6. Program.cs - 在以下位置调用 RebuildScenarioMenu()
- InitializeAdaptiveScheduler() - 初始化后重建菜单
- ShowAdaptiveConfigForm() - 配置窗口关闭后重建菜单

## 优点
1. **灵活性**：支持动态添加/删除场景类型
2. **一致性**：菜单项与实际配置保持同步
3. **可维护性**：减少硬编码，提高代码质量
4. **完整性**：显示所有可用场景，包括自定义场景

## 验证结果
- ✓ 所有6个场景都能在菜单中显示
- ✓ 场景按预定义顺序排列
- ✓ 每个场景都有对应的中文显示名称
- ✓ 配置更新后菜单会自动刷新
- ✓ 当前选中的场景会正确标记

## 影响范围
此修改仅影响主菜单的场景选择部分，不会影响：
- 自适应调度的核心功能
- 场景配置的保存和加载
- 应用规则的匹配逻辑
- 性能控制的执行