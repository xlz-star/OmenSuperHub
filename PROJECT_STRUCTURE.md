# OmenSuperHub 项目结构总览

## 📁 目录结构

```
OmenSuperHub/
├── 📁 AdaptiveScheduling/           # ✨ 自适应调度功能模块
│   ├── 📄 AdaptiveSchedulingModels.cs   # 数据模型和枚举定义
│   ├── 📄 ProcessMonitor.cs            # 进程监控和应用识别
│   ├── 📄 ConfigManager.cs             # 配置文件管理
│   ├── 📄 AdaptiveScheduler.cs         # 调度引擎核心逻辑
│   ├── 📄 AdaptiveConfigForm.cs        # 用户配置界面
│   └── 📄 README.md                    # 模块说明文档
├── 📁 Tests/                        # ✨ 单元测试
│   └── 📄 AdaptiveSchedulingTests.cs   # 自适应调度功能测试
├── 📁 Documentation/                # ✨ 用户文档
│   └── 📄 ADAPTIVE_SCHEDULING_GUIDE.md # 详细使用说明
├── 📁 Collections/                  # 原有：集合类库
│   ├── 📄 IReadOnlyArray.cs
│   ├── 📄 ListSet.cs
│   ├── 📄 Pair.cs
│   ├── 📄 ReadOnlyArray.cs
│   └── 📄 RingCollection.cs
├── 📁 Hardware/                     # 原有：硬件监控库
│   ├── 📁 ATI/                      # ATI显卡支持
│   ├── 📁 CPU/                      # CPU监控
│   ├── 📁 HDD/                      # 硬盘监控
│   ├── 📁 LPC/                      # LPC接口
│   ├── 📁 Mainboard/                # 主板监控
│   ├── 📁 Nvidia/                   # NVIDIA显卡支持
│   ├── 📁 RAM/                      # 内存监控
│   ├── 📁 TBalancer/                # T-Balancer支持
│   └── 📄 ... (其他硬件相关文件)
├── 📁 Properties/                   # 原有：程序属性
│   ├── 📄 AssemblyInfo.cs
│   ├── 📄 AssemblyLibInfo.cs
│   ├── 📄 AssemblyVersion.cs
│   ├── 📄 Resources.Designer.cs
│   └── 📄 Resources.resx
├── 📁 Resources/                    # 原有：资源文件
│   ├── 🖼️ fan.ico
│   ├── 🖼️ smallfan.ico
│   ├── 📄 nvpcf_cat.CAT
│   ├── 📄 nvpcf_inf.inf
│   └── 📄 nvpcf_sys.sys
├── 📁 backup/                       # 原有：备份文件
├── 📁 packages/                     # 原有：NuGet包
├── 📁 publish/                      # 原有：发布文件
├── 📄 Program.cs                    # 🔧 主程序入口（已修改集成新功能）
├── 📄 OmenHardware.cs              # 原有：硬件控制接口
├── 📄 MainForm.cs                   # 原有：主窗口
├── 📄 HelpForm.cs                   # 原有：帮助窗口
├── 📄 FloatingForm.cs               # 原有：浮窗显示
├── 📄 OmenSuperHub.csproj          # 🔧 项目文件（已更新）
├── 📄 OmenSuperHub.sln             # 原有：解决方案文件
├── 📄 packages.config               # 🔧 包配置（已添加Newtonsoft.Json）
├── 📄 README.md                     # 🔧 项目说明（已更新）
└── 📄 ... (其他配置文件)

## ✨ 新增文件说明

### 核心功能模块
- **AdaptiveScheduling/**: 完整的自适应调度功能实现
  - 模块化设计，独立维护
  - 包含数据模型、逻辑处理、界面和配置
  - 通过委托模式与主程序集成

### 测试和文档
- **Tests/**: 单元测试确保代码质量
- **Documentation/**: 用户使用指南

### 修改的原有文件
- **Program.cs**: 集成自适应调度到主程序
- **OmenSuperHub.csproj**: 添加新文件到项目
- **packages.config**: 添加JSON序列化依赖
- **README.md**: 更新项目说明

## 🔧 技术架构

1. **模块化设计**: 新功能独立于原有代码
2. **委托集成**: 通过委托模式调用原有功能
3. **事件驱动**: 基于事件的松耦合架构
4. **配置分离**: JSON配置文件 + 注册表状态
5. **向下兼容**: 不影响原有功能的正常使用

## 📝 开发说明

- 所有新功能代码位于`AdaptiveScheduling/`目录
- 测试代码位于`Tests/`目录  
- 文档位于`Documentation/`目录
- 主程序集成代码在`Program.cs`中明确标注
- 项目文件已正确配置所有新文件的引用

此结构确保新功能与原有代码清晰分离，便于维护和后续开发。

