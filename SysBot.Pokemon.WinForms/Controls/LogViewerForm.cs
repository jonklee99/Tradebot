using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysBot.Base;

namespace SysBot.Pokemon.WinForms.Controls
{
    public partial class LogViewerForm : Form
    {
        #region Win32 API for smooth animations
        [DllImport("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hwnd, int time, AnimateWindowFlags flags);

        [Flags]
        private enum AnimateWindowFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }
        #endregion

        #region Color Theme
        private readonly Color CuztomBackground = Color.FromArgb(27, 40, 56);
        private readonly Color CuztomDarkBackground = Color.FromArgb(22, 32, 45);
        private readonly Color CuztomDarkerBackground = Color.FromArgb(16, 24, 34);
        private readonly Color CuztomAccent = Color.FromArgb(102, 192, 244);
        private readonly Color CuztomText = Color.FromArgb(239, 239, 239);
        private readonly Color CuztomSubText = Color.FromArgb(139, 179, 217);
        private readonly Color CuztomGreen = Color.FromArgb(90, 186, 71);
        private readonly Color CuztomRed = Color.FromArgb(236, 98, 95);
        private readonly Color CuztomYellow = Color.FromArgb(245, 197, 92);
        private readonly Color CuztomOrange = Color.FromArgb(251, 176, 64);
        private readonly Color CuztomPurple = Color.FromArgb(162, 155, 254);
        #endregion

        #region Fields
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly List<LogEntry> _allLogs = new();
        private readonly List<LogEntry> _filteredLogs = new();
        private readonly System.Windows.Forms.Timer _updateTimer;
        private readonly SynchronizationContext _syncContext;
        
        private LogLevel _currentFilterLevel = LogLevel.All;
        private string _searchText = "";
        private bool _autoScroll = true;
        private bool _isPaused = false;
        private readonly object _logLock = new();
        
        // UI Controls
        private Panel _headerPanel = null!;
        private Panel _toolbarPanel = null!;
        private LogListBox _logListBox = null!;
        private TextBox _searchBox = null!;
        private ComboBox _filterCombo = null!;
        private Button _clearButton = null!;
        private Button _pauseButton = null!;
        private Button _autoScrollButton = null!;
        private Label _statusLabel = null!;
        private Button _closeButton = null!;
        private Label _titleLabel = null!;
        
        // Animation
        private float _fadeProgress = 0f;
        private System.Windows.Forms.Timer? _animationTimer;
        
        // Performance
        private const int MAX_LOGS = 10000;
        private const int BATCH_SIZE = 50;
        private DateTime _lastUpdate = DateTime.MinValue;
        private const int UPDATE_THROTTLE_MS = 50;
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BotIdentity { get; set; } = "All Bots";
        #endregion

        public LogViewerForm()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            
            InitializeComponent();
            ConfigureForm();
            CreateControls();
            
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = UPDATE_THROTTLE_MS,
                Enabled = true
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // Start fade-in animation
            StartFadeAnimation(true);
        }

        #region Form Configuration
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 600);
            this.Name = "LogViewerForm";
            this.Text = "Log Viewer";
            this.StartPosition = FormStartPosition.CenterParent;
            
            this.ResumeLayout(false);
        }

        private void ConfigureForm()
        {
            // Form appearance
            this.BackColor = CuztomBackground;
            this.ForeColor = CuztomText;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            
            // Enable double buffering for smooth rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.OptimizedDoubleBuffer, true);
            
            // Shadow effect
            this.Load += (s, e) => ApplyShadow();
        }

        private void ApplyShadow()
        {
            var cs = CreateParams;
            cs.ClassStyle |= 0x00020000; // CS_DROPSHADOW
        }
        #endregion

        #region Control Creation
        private void CreateControls()
        {
            this.SuspendLayout();
            
            // Header Panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = CuztomDarkBackground,
                Padding = new Padding(10, 0, 10, 0)
            };
            
            // Title Label
            _titleLabel = new Label
            {
                Text = $"Log Viewer - {BotIdentity}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = CuztomText,
                AutoSize = true,
                Location = new Point(10, 10)
            };
            _headerPanel.Controls.Add(_titleLabel);
            
            // Close Button
            _closeButton = CreateIconButton("✕", new Point(_headerPanel.Width - 40, 5), 
                () => StartFadeAnimation(false));
            _closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _headerPanel.Controls.Add(_closeButton);
            
            // Toolbar Panel
            _toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = CuztomBackground,
                Padding = new Padding(10, 10, 10, 10)
            };
            
            // Search Box
            _searchBox = new TextBox
            {
                Width = 200,
                Height = 30,
                Font = new Font("Segoe UI", 9F),
                BackColor = CuztomDarkBackground,
                ForeColor = CuztomText,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(10, 10)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            SetPlaceholder(_searchBox, "Search logs...");
            _toolbarPanel.Controls.Add(_searchBox);
            
            // Filter ComboBox
            _filterCombo = new ComboBox
            {
                Width = 120,
                Height = 30,
                Font = new Font("Segoe UI", 9F),
                BackColor = CuztomDarkBackground,
                ForeColor = CuztomText,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(220, 10)
            };
            _filterCombo.Items.AddRange(new object[] { "All", "Info", "Warning", "Error", "Debug" });
            _filterCombo.SelectedIndex = 0;
            _filterCombo.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;
            _toolbarPanel.Controls.Add(_filterCombo);
            
            // Toolbar Buttons
            int buttonX = 350;
            _autoScrollButton = CreateToolbarButton("Auto Scroll", ref buttonX, 
                () => { _autoScroll = !_autoScroll; UpdateButtonStates(); });
            _pauseButton = CreateToolbarButton(_isPaused ? "Resume" : "Pause", ref buttonX,
                () => { _isPaused = !_isPaused; UpdateButtonStates(); });
            _clearButton = CreateToolbarButton("Clear", ref buttonX, ClearLogs);
            
            _toolbarPanel.Controls.AddRange(new Control[] { 
                _autoScrollButton, _pauseButton, _clearButton 
            });
            
            // Status Label
            _statusLabel = new Label
            {
                Text = "0 logs",
                Font = new Font("Segoe UI", 9F),
                ForeColor = CuztomSubText,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _statusLabel.Location = new Point(_toolbarPanel.Width - _statusLabel.Width - 10, 15);
            _toolbarPanel.Controls.Add(_statusLabel);
            
            // Log ListBox
            _logListBox = new LogListBox
            {
                Dock = DockStyle.Fill,
                BackColor = CuztomDarkerBackground,
                ForeColor = CuztomText,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9F)
            };
            
            // Add controls to form
            this.Controls.Add(_logListBox);
            this.Controls.Add(_toolbarPanel);
            this.Controls.Add(_headerPanel);
            
            this.ResumeLayout(true);
            
            UpdateButtonStates();
        }

        private Button CreateIconButton(string text, Point location, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(30, 30),
                Location = location,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = CuztomText,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = CuztomAccent.WithAlpha(50);
            button.Click += (s, e) => onClick();
            return button;
        }

        private Button CreateToolbarButton(string text, ref int x, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(80, 30),
                Location = new Point(x, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = CuztomDarkBackground,
                ForeColor = CuztomText,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = CuztomAccent.WithAlpha(100);
            button.FlatAppearance.MouseOverBackColor = CuztomAccent.WithAlpha(50);
            button.Click += (s, e) => onClick();
            
            x += button.Width + 5;
            return button;
        }

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Enter += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = CuztomText;
                }
            };
            
            textBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = CuztomSubText;
                }
            };
            
            textBox.Text = placeholder;
            textBox.ForeColor = CuztomSubText;
        }
        #endregion

        #region Log Management
        public void AddLog(string message, string identity, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Identity = identity,
                Message = message,
                Level = level
            };
            
            _logQueue.Enqueue(entry);
        }

        private void ProcessLogQueue()
        {
            if (_isPaused) return;
            
            var batch = new List<LogEntry>(BATCH_SIZE);
            
            while (batch.Count < BATCH_SIZE && _logQueue.TryDequeue(out var entry))
            {
                batch.Add(entry);
            }
            
            if (batch.Count == 0) return;
            
            lock (_logLock)
            {
                _allLogs.AddRange(batch);
                
                // Trim logs if exceeding max
                if (_allLogs.Count > MAX_LOGS)
                {
                    _allLogs.RemoveRange(0, _allLogs.Count - MAX_LOGS);
                }
                
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            lock (_logLock)
            {
                _filteredLogs.Clear();
                
                var filtered = _allLogs.AsEnumerable();
                
                // Apply level filter
                if (_currentFilterLevel != LogLevel.All)
                {
                    filtered = filtered.Where(log => log.Level == _currentFilterLevel);
                }
                
                // Apply search filter
                if (!string.IsNullOrWhiteSpace(_searchText) && _searchText != "Search logs...")
                {
                    var searchLower = _searchText.ToLower();
                    filtered = filtered.Where(log => 
                        log.Message.ToLower().Contains(searchLower) ||
                        log.Identity.ToLower().Contains(searchLower));
                }
                
                _filteredLogs.AddRange(filtered);
            }
        }

        private void ClearLogs()
        {
            lock (_logLock)
            {
                _allLogs.Clear();
                _filteredLogs.Clear();
                _logListBox.ClearLogs();
                UpdateStatus();
            }
        }
        #endregion

        #region UI Updates
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.Now - _lastUpdate < TimeSpan.FromMilliseconds(UPDATE_THROTTLE_MS))
                return;
            
            ProcessLogQueue();
            UpdateUI();
            _lastUpdate = DateTime.Now;
        }

        private void UpdateUI()
        {
            _syncContext.Post(_ =>
            {
                lock (_logLock)
                {
                    _logListBox.UpdateLogs(_filteredLogs, _autoScroll);
                    UpdateStatus();
                }
            }, null);
        }

        private void UpdateStatus()
        {
            _statusLabel.Text = $"{_filteredLogs.Count} logs" + 
                               (_filteredLogs.Count < _allLogs.Count ? $" (filtered from {_allLogs.Count})" : "");
        }

        private void UpdateButtonStates()
        {
            _autoScrollButton.BackColor = _autoScroll ? CuztomAccent.WithAlpha(100) : CuztomDarkBackground;
            _pauseButton.Text = _isPaused ? "Resume" : "Pause";
            _pauseButton.BackColor = _isPaused ? CuztomOrange.WithAlpha(100) : CuztomDarkBackground;
        }
        #endregion

        #region Event Handlers
        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            if (_searchBox.Text != "Search logs..." && _searchBox.ForeColor == CuztomText)
            {
                _searchText = _searchBox.Text;
                ApplyFilters();
                UpdateUI();
            }
        }

        private void FilterCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _currentFilterLevel = _filterCombo.SelectedIndex switch
            {
                0 => LogLevel.All,
                1 => LogLevel.Info,
                2 => LogLevel.Warning,
                3 => LogLevel.Error,
                4 => LogLevel.Debug,
                _ => LogLevel.All
            };
            
            ApplyFilters();
            UpdateUI();
        }
        #endregion

        #region Animation
        private void StartFadeAnimation(bool fadeIn)
        {
            _animationTimer?.Stop();
            _animationTimer = new System.Windows.Forms.Timer { Interval = 10 };
            
            _animationTimer.Tick += (s, e) =>
            {
                if (fadeIn)
                {
                    _fadeProgress += 0.1f;
                    if (_fadeProgress >= 1f)
                    {
                        _fadeProgress = 1f;
                        _animationTimer.Stop();
                    }
                }
                else
                {
                    _fadeProgress -= 0.1f;
                    if (_fadeProgress <= 0f)
                    {
                        _fadeProgress = 0f;
                        _animationTimer.Stop();
                        this.Close();
                    }
                }
                
                this.Opacity = _fadeProgress;
            };
            
            _animationTimer.Start();
        }
        #endregion

        #region Form Overrides
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED for double buffering
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border
            using var pen = new Pen(CuztomAccent.WithAlpha(100), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _animationTimer?.Stop();
            base.OnFormClosing(e);
        }
        #endregion

        #region Nested Classes
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Identity { get; set; } = "";
            public string Message { get; set; } = "";
            public LogLevel Level { get; set; }
        }

        public enum LogLevel
        {
            All,
            Info,
            Warning,
            Error,
            Debug
        }

        private class LogListBox : UserControl
        {
            private readonly List<LogEntry> _logs = new();
            private readonly VScrollBar _scrollBar;
            private int _scrollOffset = 0;
            private int _itemHeight = 20;
            private readonly Dictionary<LogLevel, Color> _levelColors;
            
            public LogListBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.DoubleBuffer |
                        ControlStyles.ResizeRedraw |
                        ControlStyles.OptimizedDoubleBuffer, true);
                
                _levelColors = new Dictionary<LogLevel, Color>
                {
                    { LogLevel.Info, Color.FromArgb(102, 192, 244) },
                    { LogLevel.Warning, Color.FromArgb(245, 197, 92) },
                    { LogLevel.Error, Color.FromArgb(236, 98, 95) },
                    { LogLevel.Debug, Color.FromArgb(139, 179, 217) }
                };
                
                _scrollBar = new VScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 15
                };
                _scrollBar.Scroll += (s, e) =>
                {
                    _scrollOffset = _scrollBar.Value;
                    Invalidate();
                };
                
                Controls.Add(_scrollBar);
            }
            
            public void UpdateLogs(List<LogEntry> logs, bool autoScroll)
            {
                _logs.Clear();
                _logs.AddRange(logs);
                
                UpdateScrollBar();
                
                if (autoScroll && _logs.Count > 0)
                {
                    _scrollBar.Value = Math.Max(0, _scrollBar.Maximum - _scrollBar.LargeChange + 1);
                    _scrollOffset = _scrollBar.Value;
                }
                
                Invalidate();
            }
            
            public void ClearLogs()
            {
                _logs.Clear();
                _scrollOffset = 0;
                _scrollBar.Value = 0;
                Invalidate();
            }
            
            private void UpdateScrollBar()
            {
                var visibleItems = Height / _itemHeight;
                _scrollBar.Maximum = Math.Max(0, _logs.Count - 1);
                _scrollBar.LargeChange = Math.Max(1, visibleItems);
                _scrollBar.SmallChange = 1;
                _scrollBar.Visible = _logs.Count > visibleItems;
            }
            
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                
                var visibleItems = Height / _itemHeight;
                var startIndex = _scrollOffset;
                var endIndex = Math.Min(_logs.Count, startIndex + visibleItems + 1);
                
                var y = 0;
                for (int i = startIndex; i < endIndex; i++)
                {
                    var log = _logs[i];
                    DrawLogEntry(g, log, y);
                    y += _itemHeight;
                }
            }
            
            private void DrawLogEntry(Graphics g, LogEntry entry, int y)
            {
                var timeStr = entry.Timestamp.ToString("HH:mm:ss.fff");
                var levelStr = $"[{entry.Level}]";
                var identityStr = $"[{entry.Identity}]";
                
                // Background for alternating rows
                if (y / _itemHeight % 2 == 1)
                {
                    using var bgBrush = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                    g.FillRectangle(bgBrush, 0, y, Width, _itemHeight);
                }
                
                var x = 5;
                
                // Timestamp
                using (var timeBrush = new SolidBrush(Color.FromArgb(139, 179, 217)))
                {
                    g.DrawString(timeStr, Font, timeBrush, x, y + 2);
                }
                x += 90;
                
                // Level indicator
                var levelColor = _levelColors.GetValueOrDefault(entry.Level, Color.White);
                using (var levelBrush = new SolidBrush(levelColor))
                {
                    g.DrawString(levelStr, Font, levelBrush, x, y + 2);
                }
                x += 70;
                
                // Identity
                using (var identityBrush = new SolidBrush(Color.FromArgb(162, 155, 254)))
                {
                    g.DrawString(identityStr, Font, identityBrush, x, y + 2);
                }
                x += 120;
                
                // Message
                using (var msgBrush = new SolidBrush(Color.FromArgb(239, 239, 239)))
                {
                    var msgRect = new Rectangle(x, y + 2, Width - x - 20, _itemHeight);
                    g.DrawString(entry.Message, Font, msgBrush, msgRect, 
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                }
            }
            
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateScrollBar();
            }
        }
        #endregion
    }

    #region Extension Methods
    internal static class ColorExtensions
    {
        public static Color WithAlpha(this Color color, int alpha)
        {
            return Color.FromArgb(alpha, color);
        }
    }
    #endregion
}