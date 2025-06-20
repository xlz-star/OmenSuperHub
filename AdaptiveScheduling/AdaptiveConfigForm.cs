using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using OmenSuperHub.AdaptiveScheduling;

namespace OmenSuperHub
{
    /// <summary>
    /// 自适应调度配置窗口
    /// </summary>
    public partial class AdaptiveConfigForm : Form
    {
        private readonly ConfigManager _configManager;
        private TabControl _tabControl;
        private CheckBox _enableAutoScheduling;
        private ComboBox _currentScenarioCombo;
        private ComboBox _monitorModeCombo;
        private DataGridView _appRulesGrid;
        private DataGridView _scenarioConfigGrid;
        private NumericUpDown _scanIntervalNumeric;
        private Button _saveButton;
        private Button _resetButton;
        private Button _closeButton;
        private Button _addRuleButton;
        private Button _deleteRuleButton;
        private Button _clearRulesButton;
        private Button _addScenarioButton;
        private Button _deleteScenarioButton;

        public AdaptiveConfigForm(ConfigManager configManager)
        {
            _configManager = configManager;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "自适应性能调度配置";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // 创建主标签页控件
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Padding = new Point(10, 5);

            // 创建基础设置标签页
            CreateBasicSettingsTab();

            // 创建应用规则标签页
            CreateAppRulesTab();

            // 创建场景配置标签页
            CreateScenarioConfigTab();

            // 创建底部按钮
            CreateBottomButtons();

            this.Controls.Add(_tabControl);
        }

        private void CreateBasicSettingsTab()
        {
            var tabPage = new TabPage("基础设置");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            int yPos = 20;

            // 启用自动调度
            _enableAutoScheduling = new CheckBox
            {
                Text = "启用自适应调度",
                Location = new Point(20, yPos),
                Size = new Size(200, 25)
            };
            panel.Controls.Add(_enableAutoScheduling);
            yPos += 40;

            // 当前场景
            var currentScenarioLabel = new Label
            {
                Text = "当前场景：",
                Location = new Point(20, yPos),
                Size = new Size(100, 25)
            };
            panel.Controls.Add(currentScenarioLabel);

            _currentScenarioCombo = new ComboBox
            {
                Location = new Point(130, yPos),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _currentScenarioCombo.Items.AddRange(new object[]
            {
                "办公模式", "游戏模式", "创作模式", "娱乐模式", "节能模式", "自定义模式"
            });
            panel.Controls.Add(_currentScenarioCombo);
            yPos += 40;

            // 扫描间隔
            var scanIntervalLabel = new Label
            {
                Text = "扫描间隔（秒）：",
                Location = new Point(20, yPos),
                Size = new Size(120, 25)
            };
            panel.Controls.Add(scanIntervalLabel);

            _scanIntervalNumeric = new NumericUpDown
            {
                Location = new Point(150, yPos),
                Size = new Size(80, 25),
                Minimum = 1,
                Maximum = 300,
                Value = 30
            };
            panel.Controls.Add(_scanIntervalNumeric);
            yPos += 40;

            // 监控模式
            var monitorModeLabel = new Label
            {
                Text = "监控模式：",
                Location = new Point(20, yPos),
                Size = new Size(100, 25)
            };
            panel.Controls.Add(monitorModeLabel);

            _monitorModeCombo = new ComboBox
            {
                Location = new Point(130, yPos),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _monitorModeCombo.Items.AddRange(new object[]
            {
                "定时器模式", "事件驱动模式"
            });
            _monitorModeCombo.SelectedIndex = 1; // 默认选择事件驱动模式
            _monitorModeCombo.SelectedIndexChanged += MonitorModeCombo_SelectedIndexChanged;
            panel.Controls.Add(_monitorModeCombo);
            yPos += 40;

            // 说明文字
            var helpLabel = new Label
            {
                Text = "• 启用自适应调度后，系统会根据运行的应用自动切换性能模式\\n" +
                       "• 扫描间隔决定了检测应用的频率（仅定时器模式），越小响应越快但占用资源越多\\n" +
                       "• 事件驱动模式响应更快（<100ms）且资源占用更低，推荐优先使用\\n" +
                       "• 手动设置场景会临时禁用自动调度，重新启用后会恢复自动检测",
                Location = new Point(20, yPos),
                Size = new Size(500, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.DarkBlue
            };
            panel.Controls.Add(helpLabel);

            tabPage.Controls.Add(panel);
            _tabControl.TabPages.Add(tabPage);
        }

        private void CreateAppRulesTab()
        {
            var tabPage = new TabPage("应用规则");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // 顶部按钮
            _addRuleButton = new Button
            {
                Text = "添加规则",
                Location = new Point(10, 10),
                Size = new Size(80, 30)
            };
            _addRuleButton.Click += AddRuleButton_Click;
            panel.Controls.Add(_addRuleButton);

            _deleteRuleButton = new Button
            {
                Text = "删除规则",
                Location = new Point(100, 10),
                Size = new Size(80, 30)
            };
            _deleteRuleButton.Click += DeleteRuleButton_Click;
            panel.Controls.Add(_deleteRuleButton);

            _clearRulesButton = new Button
            {
                Text = "清空规则",
                Location = new Point(190, 10),
                Size = new Size(80, 30),
                ForeColor = Color.Red
            };
            _clearRulesButton.Click += ClearRulesButton_Click;
            panel.Controls.Add(_clearRulesButton);

            // 数据表格
            _appRulesGrid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(750, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // 配置列
            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProcessName",
                HeaderText = "进程名称",
                Width = 120,
                MinimumWidth = 100
            });

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "WindowTitle",
                HeaderText = "窗口标题",
                Width = 180,
                MinimumWidth = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });

            var scenarioColumn = new DataGridViewComboBoxColumn
            {
                Name = "Scenario",
                HeaderText = "场景",
                Width = 100,
                MinimumWidth = 80
            };
            // 场景选项将在LoadData中动态设置
            _appRulesGrid.Columns.Add(scenarioColumn);

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Priority",
                HeaderText = "优先级",
                Width = 70,
                MinimumWidth = 60
            });

            _appRulesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsEnabled",
                HeaderText = "启用",
                Width = 60,
                MinimumWidth = 50
            });

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 200,
                MinimumWidth = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 60
            });

            panel.Controls.Add(_appRulesGrid);
            tabPage.Controls.Add(panel);
            _tabControl.TabPages.Add(tabPage);
        }

        private void CreateScenarioConfigTab()
        {
            var tabPage = new TabPage("场景配置");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // 顶部按钮
            _addScenarioButton = new Button
            {
                Text = "添加场景",
                Location = new Point(10, 10),
                Size = new Size(80, 30)
            };
            _addScenarioButton.Click += AddScenarioButton_Click;
            panel.Controls.Add(_addScenarioButton);

            _deleteScenarioButton = new Button
            {
                Text = "删除场景",
                Location = new Point(100, 10),
                Size = new Size(80, 30)
            };
            _deleteScenarioButton.Click += DeleteScenarioButton_Click;
            panel.Controls.Add(_deleteScenarioButton);

            // 数据表格
            _scenarioConfigGrid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(750, 410),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // 配置列
            _scenarioConfigGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Scenario",
                HeaderText = "场景",
                Width = 90,
                MinimumWidth = 80,
                ReadOnly = true
            });

            var fanTableColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanTable",
                HeaderText = "风扇配置",
                Width = 80,
                MinimumWidth = 70
            };
            fanTableColumn.Items.AddRange(new object[] { "silent", "cool" });
            _scenarioConfigGrid.Columns.Add(fanTableColumn);

            var fanModeColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanMode",
                HeaderText = "性能模式",
                Width = 80,
                MinimumWidth = 70
            };
            fanModeColumn.Items.AddRange(new object[] { "performance", "default" });
            _scenarioConfigGrid.Columns.Add(fanModeColumn);

            var fanControlColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanControl",
                HeaderText = "风扇控制",
                Width = 100,
                MinimumWidth = 90
            };
            fanControlColumn.Items.AddRange(new object[] 
            { 
                "auto", "max", "1600 RPM", "2000 RPM", "2400 RPM", "2800 RPM", 
                "3200 RPM", "3600 RPM", "4000 RPM", "4400 RPM", "4800 RPM", 
                "5200 RPM", "5600 RPM", "6000 RPM", "6400 RPM" 
            });
            _scenarioConfigGrid.Columns.Add(fanControlColumn);

            var cpuPowerColumn = new DataGridViewComboBoxColumn
            {
                Name = "CpuPower",
                HeaderText = "CPU功率",
                Width = 80,
                MinimumWidth = 70
            };
            cpuPowerColumn.Items.AddRange(new object[] 
            { 
                "max", "20 W", "30 W", "40 W", "50 W", "60 W", "70 W", "80 W", 
                "90 W", "100 W", "110 W", "120 W" 
            });
            _scenarioConfigGrid.Columns.Add(cpuPowerColumn);

            var gpuPowerColumn = new DataGridViewComboBoxColumn
            {
                Name = "GpuPower",
                HeaderText = "GPU功率",
                Width = 80,
                MinimumWidth = 70
            };
            gpuPowerColumn.Items.AddRange(new object[] { "max", "med", "min" });
            _scenarioConfigGrid.Columns.Add(gpuPowerColumn);

            _scenarioConfigGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsDefault",
                HeaderText = "默认场景",
                Width = 80,
                MinimumWidth = 70
            });

            _scenarioConfigGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 200,
                MinimumWidth = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // 添加单元格值改变事件处理
            _scenarioConfigGrid.CellValueChanged += ScenarioConfigGrid_CellValueChanged;

            panel.Controls.Add(_scenarioConfigGrid);
            tabPage.Controls.Add(panel);
            _tabControl.TabPages.Add(tabPage);
        }

        private void CreateBottomButtons()
        {
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };

            _saveButton = new Button
            {
                Text = "保存配置",
                Size = new Size(80, 30),
                Location = new Point(620, 10)
            };
            _saveButton.Click += SaveButton_Click;
            buttonPanel.Controls.Add(_saveButton);

            _resetButton = new Button
            {
                Text = "重置默认",
                Size = new Size(80, 30),
                Location = new Point(710, 10)
            };
            _resetButton.Click += ResetButton_Click;
            buttonPanel.Controls.Add(_resetButton);

            _closeButton = new Button
            {
                Text = "关闭",
                Size = new Size(60, 30),
                Location = new Point(800, 10),
                DialogResult = DialogResult.Cancel
            };
            _closeButton.Click += (s, e) => this.Close();
            buttonPanel.Controls.Add(_closeButton);

            this.Controls.Add(buttonPanel);
        }

        private void LoadData()
        {
            // 加载基础设置
            _enableAutoScheduling.Checked = _configManager.Config.IsAutoSchedulingEnabled;
            
            // 设置当前场景下拉框
            string currentScenarioDisplayName = AdaptiveScheduler.GetScenarioDisplayName(_configManager.Config.CurrentScenario);
            for (int i = 0; i < _currentScenarioCombo.Items.Count; i++)
            {
                if (_currentScenarioCombo.Items[i].ToString() == currentScenarioDisplayName)
                {
                    _currentScenarioCombo.SelectedIndex = i;
                    break;
                }
            }
            
            _scanIntervalNumeric.Value = _configManager.Config.ScanInterval;
            
            // 设置监控模式（从配置加载）
            if (_configManager.Config.MonitorMode == "Timer")
            {
                _monitorModeCombo.SelectedIndex = 0;
            }
            else
            {
                _monitorModeCombo.SelectedIndex = 1; // 默认事件驱动模式
            }
            
            // 更新扫描间隔显示状态
            UpdateScanIntervalVisibility();

            // 更新应用规则的场景选项
            UpdateAppRuleScenarioOptions();

            // 加载应用规则
            LoadAppRules();

            // 加载场景配置
            LoadScenarioConfig();
        }

        private void LoadAppRules()
        {
            _appRulesGrid.Rows.Clear();
            foreach (var rule in _configManager.Config.AppRules)
            {
                _appRulesGrid.Rows.Add(
                    rule.ProcessName,
                    rule.WindowTitle,
                    AdaptiveScheduler.GetScenarioDisplayName(rule.Scenario),
                    rule.Priority,
                    rule.IsEnabled,
                    rule.Description
                );
            }
        }

        private void LoadScenarioConfig()
        {
            _scenarioConfigGrid.Rows.Clear();
            foreach (var scenario in _configManager.Config.Scenarios)
            {
                _scenarioConfigGrid.Rows.Add(
                    AdaptiveScheduler.GetScenarioDisplayName(scenario.Key),
                    scenario.Value.FanTable,
                    scenario.Value.FanMode,
                    scenario.Value.FanControl,
                    scenario.Value.CpuPower,
                    scenario.Value.GpuPower,
                    scenario.Key == _configManager.Config.DefaultScenario,
                    scenario.Value.Description
                );
            }
        }

        /// <summary>
        /// 更新应用规则网格中的场景选项
        /// </summary>
        private void UpdateAppRuleScenarioOptions()
        {
            if (_appRulesGrid.Columns["Scenario"] is DataGridViewComboBoxColumn scenarioColumn)
            {
                // 清空现有选项
                scenarioColumn.Items.Clear();
                
                // 添加当前可用的场景
                foreach (var scenario in _configManager.Config.Scenarios.Keys)
                {
                    string displayName = AdaptiveScheduler.GetScenarioDisplayName(scenario);
                    scenarioColumn.Items.Add(displayName);
                }
                
                Logger.Info($"[AdaptiveConfigForm] 已更新应用规则场景选项，数量: {scenarioColumn.Items.Count}");
            }
        }

        private void AddRuleButton_Click(object sender, EventArgs e)
        {
            // 显示进程选择器
            using (var processSelector = new ProcessSelectorForm())
            {
                if (processSelector.ShowDialog() == DialogResult.OK)
                {
                    var newRule = new AppRule
                    {
                        ProcessName = processSelector.SelectedProcessName,
                        WindowTitle = processSelector.SelectedWindowTitle,
                        Scenario = AppScenario.Office,
                        Priority = 5,
                        IsEnabled = true,
                        Description = $"自动添加的规则: {processSelector.SelectedProcessName}"
                    };

                    _configManager.AddAppRule(newRule);
                    LoadAppRules();
                }
            }
        }

        private void DeleteRuleButton_Click(object sender, EventArgs e)
        {
            if (_appRulesGrid.SelectedRows.Count > 0)
            {
                int index = _appRulesGrid.SelectedRows[0].Index;
                if (index < _configManager.Config.AppRules.Count)
                {
                    _configManager.Config.AppRules.RemoveAt(index);
                    LoadAppRules();
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Info($"[AdaptiveConfigForm] 开始保存配置，当前AppRules数量: {_configManager.Config.AppRules.Count}");
                
                // 保存基础设置
                _configManager.SetAutoSchedulingEnabled(_enableAutoScheduling.Checked);
                
                // 转换显示名称到场景枚举
                string selectedScenarioName = _currentScenarioCombo.SelectedItem?.ToString() ?? "办公模式";
                AppScenario selectedScenario = AdaptiveScheduler.GetScenarioFromDisplayName(selectedScenarioName);
                _configManager.SetCurrentScenario(selectedScenario);
                
                // 保存扫描间隔
                _configManager.Config.ScanInterval = (int)_scanIntervalNumeric.Value;
                
                // 保存监控模式
                _configManager.Config.MonitorMode = _monitorModeCombo.SelectedIndex == 0 ? "Timer" : "EventDriven";
                Logger.Info($"[AdaptiveConfigForm] 保存监控模式: {_configManager.Config.MonitorMode}");

                // 保存应用规则
                SaveAppRules();

                // 保存场景配置
                SaveScenarioConfig();

                Logger.Info($"[AdaptiveConfigForm] 准备保存到文件，最终AppRules数量: {_configManager.Config.AppRules.Count}");
                
                // 保存到文件
                _configManager.SaveConfig();

                Logger.Info($"[AdaptiveConfigForm] 配置保存完成");
                MessageBox.Show("配置已保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 保存后不关闭窗口，让用户继续编辑
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAppRules()
        {
            for (int i = 0; i < _appRulesGrid.Rows.Count && i < _configManager.Config.AppRules.Count; i++)
            {
                var row = _appRulesGrid.Rows[i];
                var rule = _configManager.Config.AppRules[i];

                rule.ProcessName = row.Cells["ProcessName"].Value?.ToString() ?? "";
                rule.WindowTitle = row.Cells["WindowTitle"].Value?.ToString() ?? "";
                string scenarioDisplayName = row.Cells["Scenario"].Value?.ToString() ?? "办公模式";
                rule.Scenario = AdaptiveScheduler.GetScenarioFromDisplayName(scenarioDisplayName);
                rule.Priority = Convert.ToInt32(row.Cells["Priority"].Value ?? 5);
                rule.IsEnabled = Convert.ToBoolean(row.Cells["IsEnabled"].Value ?? true);
                rule.Description = row.Cells["Description"].Value?.ToString() ?? "";
            }
        }

        private void SaveScenarioConfig()
        {
            var scenarios = _configManager.Config.Scenarios.Keys.ToArray();
            Logger.Debug($"[AdaptiveConfigForm] 开始保存场景配置，场景数量: {scenarios.Length}");
            
            for (int i = 0; i < _scenarioConfigGrid.Rows.Count && i < scenarios.Length; i++)
            {
                var row = _scenarioConfigGrid.Rows[i];
                var scenario = scenarios[i];
                var config = _configManager.Config.Scenarios[scenario];

                string oldCpuPower = config.CpuPower;
                config.FanTable = row.Cells["FanTable"].Value?.ToString() ?? "silent";
                config.FanMode = row.Cells["FanMode"].Value?.ToString() ?? "performance";
                config.FanControl = row.Cells["FanControl"].Value?.ToString() ?? "auto";
                config.CpuPower = row.Cells["CpuPower"].Value?.ToString() ?? "max";
                config.GpuPower = row.Cells["GpuPower"].Value?.ToString() ?? "max";
                config.Description = row.Cells["Description"].Value?.ToString() ?? "";

                Logger.Debug($"[AdaptiveConfigForm] 场景 {scenario}: CPU功率 {oldCpuPower} -> {config.CpuPower}");

                // 处理默认场景设置
                bool isDefault = Convert.ToBoolean(row.Cells["IsDefault"].Value ?? false);
                if (isDefault)
                {
                    _configManager.Config.DefaultScenario = scenario;
                    Logger.Info($"[AdaptiveConfigForm] 设置默认场景为: {scenario}");
                }
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要重置为默认配置吗？这将清除所有自定义设置。", "确认", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _configManager.ResetToDefault();
                LoadData();
            }
        }

        private void AddScenarioButton_Click(object sender, EventArgs e)
        {
            // 创建新场景对话框
            string scenarioName = "";
            if (ShowInputDialog("新增场景", "请输入新场景的名称:", ref scenarioName) != DialogResult.OK)
                return;
                
            if (!string.IsNullOrWhiteSpace(scenarioName))
            {
                // 检查是否已存在
                var existingScenario = _configManager.Config.Scenarios.Values.FirstOrDefault(s => s.Description == scenarioName);
                if (existingScenario != null)
                {
                    MessageBox.Show("该场景名称已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 创建新的自定义场景配置
                var newConfig = new PerformanceConfig
                {
                    FanTable = "silent",
                    FanMode = "default",
                    FanControl = "auto",
                    CpuPower = "max",
                    GpuPower = "max",
                    TempSensitivity = "medium",
                    GpuClock = 0,
                    DBVersion = 2,
                    Description = scenarioName
                };

                // 添加到配置中（使用Custom类型）
                _configManager.Config.Scenarios[AppScenario.Custom] = newConfig;
                
                // 更新应用规则的场景选项
                UpdateAppRuleScenarioOptions();
                
                LoadScenarioConfig();
                MessageBox.Show($"已添加场景: {scenarioName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteScenarioButton_Click(object sender, EventArgs e)
        {
            if (_scenarioConfigGrid.SelectedRows.Count > 0)
            {
                var row = _scenarioConfigGrid.SelectedRows[0];
                var scenarioName = row.Cells["Scenario"].Value?.ToString();
                
                // 检查是否至少保留一个场景
                if (_configManager.Config.Scenarios.Count <= 1)
                {
                    MessageBox.Show("至少需要保留一个场景配置！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"确定要删除场景 '{scenarioName}' 吗？\n\n注意：使用此场景的应用规则将自动切换到默认场景（办公模式）。", "确认删除", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // 找到对应的场景并删除
                    var scenarioToRemove = _configManager.Config.Scenarios
                        .FirstOrDefault(s => AdaptiveScheduler.GetScenarioDisplayName(s.Key) == scenarioName);
                    
                    if (scenarioToRemove.Key != default(AppScenario))
                    {
                        // 获取默认场景（选择一个仍然存在的场景作为默认）
                        var defaultScenario = _configManager.Config.Scenarios.Keys
                            .Where(k => k != scenarioToRemove.Key)
                            .FirstOrDefault();
                        
                        // 如果没有找到其他场景，使用Office作为默认
                        if (defaultScenario == default(AppScenario))
                        {
                            defaultScenario = AppScenario.Office;
                        }

                        // 更新所有使用被删除场景的应用规则
                        foreach (var rule in _configManager.Config.AppRules.Where(r => r.Scenario == scenarioToRemove.Key))
                        {
                            rule.Scenario = defaultScenario;
                        }

                        // 删除场景配置
                        _configManager.Config.Scenarios.Remove(scenarioToRemove.Key);
                        
                        // 更新应用规则的场景选项
                        UpdateAppRuleScenarioOptions();
                        
                        // 刷新界面
                        LoadScenarioConfig();
                        LoadAppRules();
                        
                        MessageBox.Show($"已删除场景: {scenarioName}\n相关应用规则已更新为: {AdaptiveScheduler.GetScenarioDisplayName(defaultScenario)}", 
                            "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择要删除的场景。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearRulesButton_Click(object sender, EventArgs e)
        {
            if (_configManager.Config.AppRules.Count == 0)
            {
                MessageBox.Show("应用规则列表已经是空的。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要清空所有应用规则吗？\n\n当前共有 {_configManager.Config.AppRules.Count} 条规则，此操作不可撤销。", 
                "确认清空", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                int ruleCount = _configManager.Config.AppRules.Count;
                _configManager.Config.AppRules.Clear();
                LoadAppRules();
                
                MessageBox.Show($"已清空 {ruleCount} 条应用规则。", "清空完成", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static DialogResult ShowInputDialog(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "确定";
            buttonCancel.Text = "取消";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void ScenarioConfigGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // 如果修改的是默认场景列
            if (e.ColumnIndex >= 0 && _scenarioConfigGrid.Columns[e.ColumnIndex].Name == "IsDefault")
            {
                // 获取当前行的值
                bool isChecked = Convert.ToBoolean(_scenarioConfigGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? false);
                
                if (isChecked)
                {
                    // 如果当前行被选中为默认，则取消其他行的默认状态
                    for (int i = 0; i < _scenarioConfigGrid.Rows.Count; i++)
                    {
                        if (i != e.RowIndex)
                        {
                            _scenarioConfigGrid.Rows[i].Cells["IsDefault"].Value = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 监控模式切换事件处理
        /// </summary>
        private void MonitorModeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateScanIntervalVisibility();
        }

        /// <summary>
        /// 更新扫描间隔显示状态
        /// </summary>
        private void UpdateScanIntervalVisibility()
        {
            // 事件驱动模式隐藏扫描间隔，定时器模式显示扫描间隔
            bool isTimerMode = _monitorModeCombo.SelectedIndex == 0;
            
            // 查找扫描间隔相关控件
            foreach (Control control in this.Controls)
            {
                if (control is TabControl tabControl)
                {
                    var basicTab = tabControl.TabPages[0];
                    foreach (Control tabControl1 in basicTab.Controls)
                    {
                        if (tabControl1 is Panel panel)
                        {
                            foreach (Control panelControl in panel.Controls)
                            {
                                if (panelControl == _scanIntervalNumeric || 
                                    (panelControl is Label label && label.Text.Contains("扫描间隔")))
                                {
                                    panelControl.Visible = isTimerMode;
                                }
                            }
                        }
                    }
                }
            }
            
            Logger.Info($"[AdaptiveConfigForm] 扫描间隔控件可见性: {isTimerMode} (模式: {_monitorModeCombo.SelectedItem})");
        }
    }
}
