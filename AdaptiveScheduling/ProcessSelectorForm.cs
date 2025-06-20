using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OmenSuperHub.AdaptiveScheduling
{
    /// <summary>
    /// 进程选择窗口
    /// </summary>
    public partial class ProcessSelectorForm : Form
    {
        private DataGridView _processGrid;
        private Button _okButton;
        private Button _cancelButton;
        private Button _refreshButton;
        private TextBox _searchBox;
        private Timer _refreshTimer;

        public string SelectedProcessName { get; private set; }
        public string SelectedWindowTitle { get; private set; }

        public ProcessSelectorForm()
        {
            InitializeComponent();
            LoadProcesses();
            
            // 启动自动刷新计时器
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 5000; // 5秒刷新一次
            _refreshTimer.Tick += (s, e) => LoadProcesses();
            _refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "选择进程";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);

            // 搜索框
            var searchLabel = new Label
            {
                Text = "搜索:",
                Location = new Point(10, 15),
                Size = new Size(50, 20)
            };
            this.Controls.Add(searchLabel);

            _searchBox = new TextBox
            {
                Location = new Point(65, 12),
                Size = new Size(200, 25)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            this.Controls.Add(_searchBox);

            // 刷新按钮
            _refreshButton = new Button
            {
                Text = "刷新",
                Location = new Point(275, 10),
                Size = new Size(60, 30)
            };
            _refreshButton.Click += RefreshButton_Click;
            this.Controls.Add(_refreshButton);

            // 进程列表
            _processGrid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(660, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true
            };

            // 配置列
            _processGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProcessName",
                HeaderText = "进程名称",
                Width = 150
            });

            _processGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "WindowTitle",
                HeaderText = "窗口标题",
                Width = 250,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _processGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PID",
                HeaderText = "PID",
                Width = 80
            });

            _processGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MemoryMB",
                HeaderText = "内存(MB)",
                Width = 100
            });

            _processGrid.DoubleClick += ProcessGrid_DoubleClick;
            this.Controls.Add(_processGrid);

            // 底部按钮
            _okButton = new Button
            {
                Text = "确定",
                Location = new Point(515, 415),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OkButton_Click;
            this.Controls.Add(_okButton);

            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(595, 415),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_cancelButton);

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                    .Select(p => new
                    {
                        ProcessName = p.ProcessName,
                        WindowTitle = GetWindowTitle(p),
                        PID = p.Id,
                        MemoryMB = GetMemoryUsage(p)
                    })
                    .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                    .OrderBy(p => p.ProcessName)
                    .ToList();

                var currentSearch = _searchBox?.Text?.ToLower() ?? "";
                if (!string.IsNullOrEmpty(currentSearch))
                {
                    processes = processes.Where(p => 
                        p.ProcessName.ToLower().Contains(currentSearch) || 
                        (p.WindowTitle ?? "").ToLower().Contains(currentSearch))
                        .ToList();
                }

                _processGrid.Rows.Clear();
                foreach (var process in processes)
                {
                    _processGrid.Rows.Add(
                        process.ProcessName,
                        process.WindowTitle,
                        process.PID,
                        process.MemoryMB
                    );
                }
            }
            catch (Exception ex)
            {
                // 忽略访问被拒绝的进程
                Console.WriteLine($"加载进程列表时出错: {ex.Message}");
            }
        }

        private string GetWindowTitle(Process process)
        {
            try
            {
                return string.IsNullOrEmpty(process.MainWindowTitle) ? "" : process.MainWindowTitle;
            }
            catch
            {
                return "";
            }
        }

        private string GetMemoryUsage(Process process)
        {
            try
            {
                return (process.WorkingSet64 / 1024 / 1024).ToString();
            }
            catch
            {
                return "0";
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            LoadProcesses();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadProcesses();
        }

        private void ProcessGrid_DoubleClick(object sender, EventArgs e)
        {
            if (_processGrid.SelectedRows.Count > 0)
            {
                SelectCurrentProcess();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (_processGrid.SelectedRows.Count > 0)
            {
                SelectCurrentProcess();
            }
            else
            {
                MessageBox.Show("请选择一个进程。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        private void SelectCurrentProcess()
        {
            if (_processGrid.SelectedRows.Count > 0)
            {
                var row = _processGrid.SelectedRows[0];
                SelectedProcessName = row.Cells["ProcessName"].Value?.ToString() ?? "";
                SelectedWindowTitle = row.Cells["WindowTitle"].Value?.ToString() ?? "";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}