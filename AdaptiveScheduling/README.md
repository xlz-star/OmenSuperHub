# OmenSuperHub 自适应性能调度功能

## 项目结构

```
OmenSuperHub/
├── AdaptiveScheduling/           # 自适应调度功能模块
│   ├── AdaptiveSchedulingModels.cs   # 数据模型定义
│   ├── ProcessMonitor.cs            # 进程监控和应用识别
│   ├── ConfigManager.cs             # 配置管理
│   ├── AdaptiveScheduler.cs         # 调度引擎核心
│   └── AdaptiveConfigForm.cs        # 用户配置界面
├── Tests/                        # 单元测试
│   └── AdaptiveSchedulingTests.cs   # 自适应调度功能测试
├── Documentation/                # 文档
│   └── ADAPTIVE_SCHEDULING_GUIDE.md # 使用说明文档
├── Collections/                  # 集合类库（原有）
├── Hardware/                     # 硬件监控库（原有）
├── Properties/                   # 程序属性（原有）
├── Resources/                    # 资源文件（原有）
├── backup/                       # 备份文件（原有）
├── packages/                     # NuGet包（原有）
├── publish/                      # 发布文件（原有）
├── Program.cs                    # 主程序入口（已修改）
├── OmenHardware.cs              # 硬件控制接口（原有）
├── MainForm.cs                   # 主窗口（原有）
├── HelpForm.cs                   # 帮助窗口（原有）
├── FloatingForm.cs               # 浮窗（原有）
└── ...                          # 其他原有文件
```

## 新增功能

### 核心模块

1. **AdaptiveSchedulingModels.cs**
   - `AppScenario` 枚举：定义6种使用场景
   - `PerformanceConfig` 类：性能配置参数
   - `AppRule` 类：应用识别规则
   - `ScenarioConfig` 类：场景配置管理

2. **ProcessMonitor.cs**
   - 实时进程监控
   - 前台应用检测
   - 基于优先级的智能识别
   - 应用规则匹配

3. **ConfigManager.cs**
   - JSON配置文件管理
   - 注册表运行时状态保存
   - 配置加载和保存
   - 默认配置初始化

4. **AdaptiveScheduler.cs**
   - 自适应调度引擎
   - 场景切换逻辑
   - 事件通知系统
   - 手动覆盖机制

5. **AdaptiveConfigForm.cs**
   - 用户配置界面
   - 三标签页设计
   - 实时配置编辑
   - 数据验证和保存

### 集成修改

- **Program.cs**: 集成自适应调度功能到主程序
  - 添加托盘菜单选项
  - 实现委托调用机制
  - 添加事件处理
  - 状态更新和通知

### 测试和文档

- **Tests/AdaptiveSchedulingTests.cs**: 全面的单元测试
- **Documentation/ADAPTIVE_SCHEDULING_GUIDE.md**: 详细使用说明

## 技术特点

1. **模块化设计**: 功能独立，易于维护和扩展
2. **向下兼容**: 不影响原有功能
3. **事件驱动**: 基于事件的松耦合架构
4. **配置灵活**: 支持JSON和注册表双重配置
5. **用户友好**: 直观的图形化配置界面

## 安装和使用

1. 编译项目（需要.NET Framework 4.8）
2. 运行OmenSuperHub.exe（需要管理员权限）
3. 右键托盘图标选择"自适应调度"
4. 根据需要配置应用规则和场景参数

详细使用说明请参考 `Documentation/ADAPTIVE_SCHEDULING_GUIDE.md`

## 注意事项

- 需要管理员权限运行
- 建议扫描间隔不少于10秒
- 配置文件保存在程序目录下的adaptive_config.json
- 运行时状态保存在注册表 HKCU\Software\OmenSuperHub\AdaptiveScheduling

## 开发者说明

本功能完全集成到现有代码中，使用委托模式避免直接修改核心功能，确保兼容性和可维护性。

