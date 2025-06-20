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
        private DataGridView _appRulesGrid;
        private DataGridView _scenarioConfigGrid;
        private NumericUpDown _scanIntervalNumeric;
        private Button _saveButton;
        private Button _resetButton;
        private Button _addRuleButton;
        private Button _deleteRuleButton;

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
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

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
                Size = new Size(100, 25)
            };
            panel.Controls.Add(scanIntervalLabel);

            _scanIntervalNumeric = new NumericUpDown
            {
                Location = new Point(130, yPos),
                Size = new Size(80, 25),
                Minimum = 5,
                Maximum = 300,
                Value = 30
            };
            panel.Controls.Add(_scanIntervalNumeric);
            yPos += 40;

            // 说明文字
            var helpLabel = new Label
            {
                Text = "• 启用自适应调度后，系统会根据运行的应用自动切换性能模式\\n" +
                       "• 扫描间隔决定了检测应用的频率，越小响应越快但占用资源越多\\n" +
                       "• 手动设置场景会临时禁用自动调度，重新启用后会恢复自动检测",
                Location = new Point(20, yPos),
                Size = new Size(500, 80),
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

            // 数据表格
            _appRulesGrid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(750, 400),
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
                Width = 150
            });

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "WindowTitle",
                HeaderText = "窗口标题",
                Width = 150
            });

            var scenarioColumn = new DataGridViewComboBoxColumn
            {
                Name = "Scenario",
                HeaderText = "场景",
                Width = 100
            };
            scenarioColumn.Items.AddRange(new object[]
            {
                "Office", "Gaming", "Content", "Media", "Idle", "Custom"
            });
            _appRulesGrid.Columns.Add(scenarioColumn);

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Priority",
                HeaderText = "优先级",
                Width = 80
            });

            _appRulesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsEnabled",
                HeaderText = "启用",
                Width = 60
            });

            _appRulesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 200
            });

            panel.Controls.Add(_appRulesGrid);
            tabPage.Controls.Add(panel);
            _tabControl.TabPages.Add(tabPage);
        }

        private void CreateScenarioConfigTab()
        {
            var tabPage = new TabPage("场景配置");
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // 数据表格
            _scenarioConfigGrid = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(750, 450),
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
                Width = 100,
                ReadOnly = true
            });

            var fanTableColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanTable",
                HeaderText = "风扇配置",
                Width = 80
            };
            fanTableColumn.Items.AddRange(new object[] { "silent", "cool" });
            _scenarioConfigGrid.Columns.Add(fanTableColumn);

            var fanModeColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanMode",
                HeaderText = "性能模式",
                Width = 80
            };
            fanModeColumn.Items.AddRange(new object[] { "performance", "default" });
            _scenarioConfigGrid.Columns.Add(fanModeColumn);

            var fanControlColumn = new DataGridViewComboBoxColumn
            {
                Name = "FanControl",
                HeaderText = "风扇控制",
                Width = 100
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
                Width = 80
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
                Width = 100
            };
            gpuPowerColumn.Items.AddRange(new object[] { "max", "med", "min" });
            _scenarioConfigGrid.Columns.Add(gpuPowerColumn);

            _scenarioConfigGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 200
            });

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

            this.Controls.Add(buttonPanel);
        }

        private void LoadData()
        {
            // 加载基础设置
            _enableAutoScheduling.Checked = _configManager.Config.IsAutoSchedulingEnabled;
            _currentScenarioCombo.SelectedIndex = (int)_configManager.Config.CurrentScenario;
            _scanIntervalNumeric.Value = _configManager.Config.ScanInterval;

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
                    rule.Scenario.ToString(),
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
                    scenario.Value.Description
                );
            }
        }

        private void AddRuleButton_Click(object sender, EventArgs e)
        {
            var newRule = new AppRule
            {
                ProcessName = "新应用",
                Scenario = AppScenario.Office,
                Priority = 5,
                IsEnabled = true,
                Description = "新规则"
            };

            _configManager.AddAppRule(newRule);
            LoadAppRules();
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
                // 保存基础设置
                _configManager.SetAutoSchedulingEnabled(_enableAutoScheduling.Checked);
                _configManager.SetCurrentScenario((AppScenario)_currentScenarioCombo.SelectedIndex);
                _configManager.Config.ScanInterval = (int)_scanIntervalNumeric.Value;

                // 保存应用规则
                SaveAppRules();

                // 保存场景配置
                SaveScenarioConfig();

                // 保存到文件
                _configManager.SaveConfig();

                MessageBox.Show("配置已保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
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
                rule.Scenario = (AppScenario)Enum.Parse(typeof(AppScenario), row.Cells["Scenario"].Value?.ToString() ?? "Office");
                rule.Priority = Convert.ToInt32(row.Cells["Priority"].Value ?? 5);
                rule.IsEnabled = Convert.ToBoolean(row.Cells["IsEnabled"].Value ?? true);
                rule.Description = row.Cells["Description"].Value?.ToString() ?? "";
            }
        }

        private void SaveScenarioConfig()
        {
            var scenarios = _configManager.Config.Scenarios.Keys.ToArray();
            for (int i = 0; i < _scenarioConfigGrid.Rows.Count && i < scenarios.Length; i++)
            {
                var row = _scenarioConfigGrid.Rows[i];
                var scenario = scenarios[i];
                var config = _configManager.Config.Scenarios[scenario];

                config.FanTable = row.Cells["FanTable"].Value?.ToString() ?? "silent";
                config.FanMode = row.Cells["FanMode"].Value?.ToString() ?? "performance";
                config.FanControl = row.Cells["FanControl"].Value?.ToString() ?? "auto";
                config.CpuPower = row.Cells["CpuPower"].Value?.ToString() ?? "max";
                config.GpuPower = row.Cells["GpuPower"].Value?.ToString() ?? "max";
                config.Description = row.Cells["Description"].Value?.ToString() ?? "";
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
    }
}
