using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NetTool.UI.Models;
using NetTool.UI.Services;
using SkiaSharp;

namespace NetTool.UI.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly EngineRunner _engine = new();
        private readonly Dispatcher _dispatcher;

        // Config properties
        private string _url = "https://httpbin.org/get";
        private string _method = "GET";
        private string _headers = "{}";
        private string _body = "";
        private int _concurrency = 10;
        private int _durationSec = 30;
        private int _rampUpSec = 5;
        private int _timeoutMs = 5000;

        // State
        private bool _isRunning;
        private string _statusText = "Ready";
        private int _totalRequests;
        private int _totalErrors;
        private long _totalBytes;

        // Chart data
        private readonly ObservableCollection<ObservableValue> _rpsValues = new();
        private readonly ObservableCollection<ObservableValue> _avgValues = new();
        private readonly ObservableCollection<ObservableValue> _p95Values = new();
        private readonly ObservableCollection<ObservableValue> _p99Values = new();

        // Log
        private readonly ObservableCollection<string> _logLines = new();

        public MainViewModel()
        {
            _dispatcher = Application.Current.Dispatcher;

            StartCommand = new RelayCommand(Start, () => !IsRunning);
            StopCommand = new RelayCommand(Stop, () => IsRunning);

            _engine.OnStdOutReceived += OnEngineStdOut;
            _engine.OnStdErrReceived += OnEngineStdErr;
            _engine.OnExited += OnEngineExited;

            InitChartSeries();
        }

        #region Config Properties

        public string Url { get => _url; set => SetProperty(ref _url, value); }
        public string Method { get => _method; set => SetProperty(ref _method, value); }
        public string Headers { get => _headers; set => SetProperty(ref _headers, value); }
        public string Body { get => _body; set => SetProperty(ref _body, value); }
        public int Concurrency { get => _concurrency; set => SetProperty(ref _concurrency, value); }
        public int DurationSec { get => _durationSec; set => SetProperty(ref _durationSec, value); }
        public int RampUpSec { get => _rampUpSec; set => SetProperty(ref _rampUpSec, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }

        #endregion

        #region State Properties

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsNotRunning));
                }
            }
        }

        public bool IsNotRunning => !IsRunning;

        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public int TotalRequests { get => _totalRequests; set => SetProperty(ref _totalRequests, value); }
        public int TotalErrors { get => _totalErrors; set => SetProperty(ref _totalErrors, value); }
        public long TotalBytes { get => _totalBytes; set => SetProperty(ref _totalBytes, value); }

        #endregion

        #region Commands

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        #endregion

        #region Chart

        public ISeries[] RpsSeries { get; private set; } = Array.Empty<ISeries>();
        public ISeries[] LatencySeries { get; private set; } = Array.Empty<ISeries>();
        public ObservableCollection<string> LogLines => _logLines;

        public string[] HttpMethods { get; } = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

        private void InitChartSeries()
        {
            RpsSeries = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = _rpsValues,
                    Name = "RPS",
                    GeometrySize = 0,
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(40)),
                }
            };

            LatencySeries = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = _avgValues,
                    Name = "Avg",
                    GeometrySize = 0,
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(SKColors.LimeGreen, 2),
                    Fill = null,
                },
                new LineSeries<ObservableValue>
                {
                    Values = _p95Values,
                    Name = "p95",
                    GeometrySize = 0,
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(SKColors.Orange, 2),
                    Fill = null,
                },
                new LineSeries<ObservableValue>
                {
                    Values = _p99Values,
                    Name = "p99",
                    GeometrySize = 0,
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    Fill = null,
                },
            };
        }

        #endregion

        #region Actions

        private void Start()
        {
            if (IsRunning) return;

            try
            {
                // Validate URL
                if (string.IsNullOrWhiteSpace(Url))
                {
                    StatusText = "Error: URL is required";
                    return;
                }

                // Build config
                var config = new TestConfig
                {
                    Request = new RequestConfig
                    {
                        Url = Url.Trim(),
                        Method = Method,
                        Body = Body,
                    },
                    Load = new LoadConfig
                    {
                        Concurrency = Concurrency,
                        DurationSec = DurationSec,
                        RampUpSec = RampUpSec,
                    },
                    TimeoutMs = TimeoutMs,
                };

                // Parse headers
                if (!string.IsNullOrWhiteSpace(Headers) && Headers.Trim() != "{}")
                {
                    try
                    {
                        config.Request.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(Headers) ?? new();
                    }
                    catch
                    {
                        StatusText = "Error: Invalid JSON headers";
                        return;
                    }
                }

                // Clear previous data
                _rpsValues.Clear();
                _avgValues.Clear();
                _p95Values.Clear();
                _p99Values.Clear();
                _logLines.Clear();
                TotalRequests = 0;
                TotalErrors = 0;
                TotalBytes = 0;

                // Write config to temp file
                var configPath = Path.Combine(Path.GetTempPath(), $"nettool_config_{Guid.NewGuid():N}.json");
                var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                File.WriteAllText(configPath, JsonSerializer.Serialize(config, jsonOptions));

                // Find engine.exe
                var enginePath = FindEnginePath();
                if (enginePath == null)
                {
                    StatusText = "Error: engine.exe not found. Build the Go engine first.";
                    return;
                }

                AddLog($"Starting test: {Method} {Url}");
                AddLog($"Concurrency: {Concurrency}, Duration: {DurationSec}s, Ramp-up: {RampUpSec}s");

                _engine.Start(enginePath, configPath);
                IsRunning = true;
                StatusText = "Running...";
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                AddLog($"ERROR: {ex.Message}");
            }
        }

        private void Stop()
        {
            if (!IsRunning) return;

            try
            {
                _engine.Stop();
                StatusText = "Stopped";
                AddLog("Test stopped by user.");
            }
            catch (Exception ex)
            {
                StatusText = $"Error stopping: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        #endregion

        #region Engine Events

        private void OnEngineStdOut(string line)
        {
            _dispatcher.BeginInvoke(() =>
            {
                var metrics = MetricsParser.Parse(line);
                if (metrics != null)
                {
                    _rpsValues.Add(new ObservableValue(metrics.Rps));
                    _avgValues.Add(new ObservableValue(metrics.Avg));
                    _p95Values.Add(new ObservableValue(metrics.P95));
                    _p99Values.Add(new ObservableValue(metrics.P99));

                    TotalRequests += metrics.Total;
                    TotalErrors += metrics.Fail;
                    TotalBytes += metrics.BytesReceived;

                    var bytesStr = FormatBytes(TotalBytes);
                    StatusText = $"Running... | RPS: {metrics.Rps} | Avg: {metrics.Avg:F0}ms | Min: {metrics.Min:F0}ms | Max: {metrics.Max:F0}ms | p95: {metrics.P95:F0}ms | Conns: {metrics.ActiveConnections} | {bytesStr}";
                }

                AddLog(line);
            });
        }

        private void OnEngineStdErr(string line)
        {
            _dispatcher.BeginInvoke(() =>
            {
                AddLog($"[ENGINE] {line}");
            });
        }

        private void OnEngineExited(int exitCode)
        {
            _dispatcher.BeginInvoke(() =>
            {
                IsRunning = false;
                StatusText = exitCode == 0 ? "Completed" : $"Exited with code {exitCode}";
                AddLog($"Engine exited (code: {exitCode})");
            });
        }

        #endregion

        #region Helpers

        private string? FindEnginePath()
        {
            // Look for engine.exe relative to the app
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine", "engine.exe"),
                // Also check the solution root
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

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logLines.Add($"[{timestamp}] {message}");

            // Keep max 500 lines to avoid memory issues
            while (_logLines.Count > 500)
                _logLines.RemoveAt(0);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        public void Dispose()
        {
            _engine.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
