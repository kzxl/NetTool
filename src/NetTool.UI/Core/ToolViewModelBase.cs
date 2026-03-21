using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using NetTool.UI.Services;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Core
{
    /// <summary>
    /// Base class cho mọi tool ViewModel.
    /// Cung cấp shared infrastructure: IsRunning, StatusText, LogLines, Engine integration.
    /// </summary>
    public abstract class ToolViewModelBase : ViewModelBase, IDisposable
    {
        protected readonly EngineRunner Engine = new();
        protected readonly Dispatcher Dispatcher;
        private readonly ObservableCollection<string> _logLines = new();

        private bool _isRunning;
        private string _statusText = "Ready";

        protected ToolViewModelBase()
        {
            Dispatcher = Application.Current.Dispatcher;

            StartCommand = new RelayCommand(DoStart, () => !IsRunning);
            StopCommand = new RelayCommand(DoStop, () => IsRunning);

            Engine.OnStdOutReceived += line => Dispatcher.BeginInvoke(() => OnEngineOutput(line));
            Engine.OnStdErrReceived += line => Dispatcher.BeginInvoke(() => AddLog($"[ENGINE] {line}"));
            Engine.OnExited += code => Dispatcher.BeginInvoke(() =>
            {
                IsRunning = false;
                StatusText = code == 0 ? "Completed" : $"Exited with code {code}";
                AddLog($"Engine exited (code: {code})");
                OnEngineExited(code);
                RaiseCommandStates();
            });
        }

        #region Shared Properties

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    RaiseCommandStates();
                    OnPropertyChanged(nameof(IsNotRunning));
                }
            }
        }

        public bool IsNotRunning => !IsRunning;

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ObservableCollection<string> LogLines => _logLines;

        #endregion

        #region Commands

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        #endregion

        #region Engine Actions

        private void DoStart()
        {
            if (IsRunning) return;

            try
            {
                OnStart();
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                AddLog($"ERROR: {ex.Message}");
            }
        }

        private void DoStop()
        {
            if (!IsRunning) return;

            try
            {
                Engine.Stop();
                StatusText = "Stopped";
                AddLog("Stopped by user.");
            }
            catch (Exception ex)
            {
                StatusText = $"Error stopping: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
                OnStop();
                RaiseCommandStates();
            }
        }

        /// <summary>
        /// Chạy Go engine với command và config JSON.
        /// </summary>
        protected void RunEngine(string command, string configJson)
        {
            var configPath = Path.Combine(Path.GetTempPath(), $"nettool_{command}_{Guid.NewGuid():N}.json");
            File.WriteAllText(configPath, configJson);

            var enginePath = FindEnginePath();
            if (enginePath == null)
            {
                StatusText = "Error: engine.exe not found. Build the Go engine first.";
                return;
            }

            Engine.Start(enginePath, command, configPath);
            IsRunning = true;
            StatusText = "Running...";
        }

        #endregion

        #region Template Methods — Module Override

        /// <summary>Module khởi tạo test, gọi RunEngine()</summary>
        protected abstract void OnStart();

        /// <summary>Module cleanup sau khi stop</summary>
        protected virtual void OnStop() { }

        /// <summary>Module xử lý stdout line từ engine</summary>
        protected abstract void OnEngineOutput(string line);

        /// <summary>Module xử lý khi engine exit</summary>
        protected virtual void OnEngineExited(int exitCode) { }

        #endregion

        #region Shared Helpers

        protected void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logLines.Add($"[{timestamp}] {message}");

            while (_logLines.Count > 500)
                _logLines.RemoveAt(0);
        }

        protected virtual void RaiseCommandStates()
        {
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }

        protected static string? FindEnginePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine", "engine.exe"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine.exe")),
            };

            foreach (var path in candidates)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }

        protected static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        public void Dispose()
        {
            Engine.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
