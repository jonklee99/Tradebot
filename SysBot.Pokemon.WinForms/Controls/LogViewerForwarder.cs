using SysBot.Base;
using System;
using System.Collections.Generic;

namespace SysBot.Pokemon.WinForms.Controls
{
    /// <summary>
    /// Forwards logs to LogViewerForm instances for real-time display
    /// </summary>
    public class LogViewerForwarder : ILogForwarder
    {
        private readonly List<LogViewerForm> _viewers = new();
        private readonly object _lock = new();

        public void RegisterViewer(LogViewerForm viewer)
        {
            lock (_lock)
            {
                if (!_viewers.Contains(viewer))
                {
                    _viewers.Add(viewer);
                    viewer.FormClosed += (s, e) => UnregisterViewer(viewer);
                }
            }
        }

        public void UnregisterViewer(LogViewerForm viewer)
        {
            lock (_lock)
            {
                _viewers.Remove(viewer);
            }
        }

        public void Forward(string message, string identity)
        {
            var level = DetermineLogLevel(message);
            
            lock (_lock)
            {
                foreach (var viewer in _viewers.ToArray()) // ToArray to avoid collection modified exception
                {
                    try
                    {
                        if (viewer.IsHandleCreated && !viewer.IsDisposed)
                        {
                            viewer.AddLog(message, identity, level);
                        }
                    }
                    catch
                    {
                        // Viewer might be closing, ignore
                    }
                }
            }
        }

        private LogViewerForm.LogLevel DetermineLogLevel(string message)
        {
            var lower = message.ToLower();
            
            if (lower.Contains("error") || lower.Contains("exception") || lower.Contains("fail"))
                return LogViewerForm.LogLevel.Error;
            
            if (lower.Contains("warn") || lower.Contains("warning"))
                return LogViewerForm.LogLevel.Warning;
            
            if (lower.Contains("debug") || lower.Contains("trace"))
                return LogViewerForm.LogLevel.Debug;
            
            return LogViewerForm.LogLevel.Info;
        }
    }
}