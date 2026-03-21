using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using NetTool.UI.Core;
using NetTool.UI.Models;
using NetTool.UI.Services;
using NetTool.UI.ViewModels;
using SkiaSharp;

namespace NetTool.UI.Modules.LoadTest
{
    public class LoadTestViewModel : ToolViewModelBase
    {
        private readonly List<MetricsData> _metricsHistory = new();

        // Config properties
        private string _url = "https://httpbin.org/get";
        private string _method = "GET";
        private string _headers = "{}";
        private string _body = "";
        private int _concurrency = 10;
        private int _durationSec = 30;
        private int _rampUpSec = 5;
        private int _timeoutMs = 5000;
        private int _expectedStatus;

        // Alert thresholds
        private int _p95Threshold;
        private int _p99Threshold;
        private bool _isAlerted;
        private string _alertMessage = "";

        // Counters
        private int _totalRequests;
        private int _totalErrors;
        private long _totalBytes;

        // Summary
        private bool _showSummary;
        private string _summaryOverallAvg = "-";
        private string _summaryOverallMin = "-";
        private string _summaryOverallMax = "-";
        private string _summaryOverallP95 = "-";
        private string _summaryOverallP99 = "-";
        private string _summaryAvgRps = "-";
        private string _summaryErrRate = "-";
        private string _summaryThroughput = "-";
        private string _summaryDuration = "-";

        // Chart data
        private readonly ObservableCollection<ObservableValue> _rpsValues = new();
        private readonly ObservableCollection<ObservableValue> _avgValues = new();
        private readonly ObservableCollection<ObservableValue> _p95Values = new();
        private readonly ObservableCollection<ObservableValue> _p99Values = new();

        public LoadTestViewModel()
        {
            ExportCsvCommand = new RelayCommand(ExportCsv, () => _metricsHistory.Count > 0);
            ExportJsonCommand = new RelayCommand(ExportJson, () => _metricsHistory.Count > 0);
            SaveTemplateCommand = new RelayCommand(SaveTemplate);
            LoadTemplateCommand = new RelayCommand(LoadTemplate);

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
        public int ExpectedStatus { get => _expectedStatus; set => SetProperty(ref _expectedStatus, value); }
        public int P95Threshold { get => _p95Threshold; set => SetProperty(ref _p95Threshold, value); }
        public int P99Threshold { get => _p99Threshold; set => SetProperty(ref _p99Threshold, value); }

        public string[] HttpMethods { get; } = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

        #endregion

        #region State Properties

        public int TotalRequests { get => _totalRequests; set => SetProperty(ref _totalRequests, value); }
        public int TotalErrors { get => _totalErrors; set => SetProperty(ref _totalErrors, value); }
        public long TotalBytes { get => _totalBytes; set => SetProperty(ref _totalBytes, value); }

        public bool IsAlerted { get => _isAlerted; set => SetProperty(ref _isAlerted, value); }
        public string AlertMessage { get => _alertMessage; set => SetProperty(ref _alertMessage, value); }

        // Summary
        public bool ShowSummary { get => _showSummary; set => SetProperty(ref _showSummary, value); }
        public string SummaryOverallAvg { get => _summaryOverallAvg; set => SetProperty(ref _summaryOverallAvg, value); }
        public string SummaryOverallMin { get => _summaryOverallMin; set => SetProperty(ref _summaryOverallMin, value); }
        public string SummaryOverallMax { get => _summaryOverallMax; set => SetProperty(ref _summaryOverallMax, value); }
        public string SummaryOverallP95 { get => _summaryOverallP95; set => SetProperty(ref _summaryOverallP95, value); }
        public string SummaryOverallP99 { get => _summaryOverallP99; set => SetProperty(ref _summaryOverallP99, value); }
        public string SummaryAvgRps { get => _summaryAvgRps; set => SetProperty(ref _summaryAvgRps, value); }
        public string SummaryErrRate { get => _summaryErrRate; set => SetProperty(ref _summaryErrRate, value); }
        public string SummaryThroughput { get => _summaryThroughput; set => SetProperty(ref _summaryThroughput, value); }
        public string SummaryDuration { get => _summaryDuration; set => SetProperty(ref _summaryDuration, value); }

        #endregion

        #region Commands

        public RelayCommand ExportCsvCommand { get; }
        public RelayCommand ExportJsonCommand { get; }
        public RelayCommand SaveTemplateCommand { get; }
        public RelayCommand LoadTemplateCommand { get; }

        #endregion

        #region Chart

        public ISeries[] RpsSeries { get; private set; } = Array.Empty<ISeries>();
        public ISeries[] LatencySeries { get; private set; } = Array.Empty<ISeries>();

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

        #region Template Methods

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusText = "Error: URL is required";
                return;
            }

            var config = BuildCurrentConfig();

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
            _metricsHistory.Clear();
            TotalRequests = 0;
            TotalErrors = 0;
            TotalBytes = 0;
            ShowSummary = false;
            IsAlerted = false;
            AlertMessage = "";

            AddLog($"Starting test: {Method} {Url}");
            AddLog($"Concurrency: {Concurrency}, Duration: {DurationSec}s, Ramp-up: {RampUpSec}s");
            if (ExpectedStatus > 0) AddLog($"Expected status: {ExpectedStatus}");
            if (P95Threshold > 0) AddLog($"Alert threshold — p95: {P95Threshold}ms");
            if (P99Threshold > 0) AddLog($"Alert threshold — p99: {P99Threshold}ms");

            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var configJson = JsonSerializer.Serialize(config, jsonOptions);
            RunEngine("loadtest", configJson);
        }

        protected override void OnStop()
        {
            ComputeSummary();
            RaiseExportCanExecute();
        }

        protected override void OnEngineOutput(string line)
        {
            var metrics = MetricsParser.Parse(line);
            if (metrics != null)
            {
                _metricsHistory.Add(metrics);

                _rpsValues.Add(new ObservableValue(metrics.Rps));
                _avgValues.Add(new ObservableValue(metrics.Avg));
                _p95Values.Add(new ObservableValue(metrics.P95));
                _p99Values.Add(new ObservableValue(metrics.P99));

                TotalRequests += metrics.Total;
                TotalErrors += metrics.Fail;
                TotalBytes += metrics.BytesReceived;

                CheckAlerts(metrics);

                var bytesStr = FormatBytes(TotalBytes);
                StatusText = $"Running... | RPS: {metrics.Rps} | Avg: {metrics.Avg:F0}ms | p95: {metrics.P95:F0}ms | Conns: {metrics.ActiveConnections} | {bytesStr}";
            }

            AddLog(line);
        }

        protected override void OnEngineExited(int exitCode)
        {
            ComputeSummary();
            RaiseExportCanExecute();
        }

        #endregion

        #region Private Helpers

        private TestConfig BuildCurrentConfig()
        {
            return new TestConfig
            {
                Request = new RequestConfig
                {
                    Url = Url.Trim(),
                    Method = Method,
                    Body = Body,
                    ExpectedStatus = ExpectedStatus,
                },
                Load = new LoadConfig
                {
                    Concurrency = Concurrency,
                    DurationSec = DurationSec,
                    RampUpSec = RampUpSec,
                },
                TimeoutMs = TimeoutMs,
            };
        }

        private void CheckAlerts(MetricsData metrics)
        {
            var alerts = new List<string>();

            if (P95Threshold > 0 && metrics.P95 > P95Threshold)
                alerts.Add($"p95 ({metrics.P95:F0}ms) > {P95Threshold}ms");

            if (P99Threshold > 0 && metrics.P99 > P99Threshold)
                alerts.Add($"p99 ({metrics.P99:F0}ms) > {P99Threshold}ms");

            if (alerts.Count > 0)
            {
                IsAlerted = true;
                AlertMessage = "⚠ " + string.Join(" | ", alerts);
            }
            else
            {
                IsAlerted = false;
                AlertMessage = "";
            }
        }

        private void ComputeSummary()
        {
            if (_metricsHistory.Count == 0)
            {
                ShowSummary = false;
                return;
            }

            var totalReqs = _metricsHistory.Sum(m => m.Total);
            var totalFails = _metricsHistory.Sum(m => m.Fail);
            var duration = _metricsHistory.Count;

            var weightedSum = _metricsHistory.Sum(m => m.Avg * m.Total);
            var overallAvg = totalReqs > 0 ? weightedSum / totalReqs : 0;

            SummaryOverallAvg = $"{overallAvg:F1} ms";
            SummaryOverallMin = $"{_metricsHistory.Min(m => m.Min):F1} ms";
            SummaryOverallMax = $"{_metricsHistory.Max(m => m.Max):F1} ms";
            SummaryOverallP95 = $"{_metricsHistory.Max(m => m.P95):F1} ms";
            SummaryOverallP99 = $"{_metricsHistory.Max(m => m.P99):F1} ms";
            SummaryAvgRps = $"{totalReqs / (double)Math.Max(duration, 1):F1}";
            SummaryErrRate = $"{(totalReqs > 0 ? (double)totalFails / totalReqs : 0):P2}";
            SummaryThroughput = FormatBytes(TotalBytes);
            SummaryDuration = $"{duration}s";

            ShowSummary = true;
        }

        private void RaiseExportCanExecute()
        {
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportJsonCommand.RaiseCanExecuteChanged();
        }

        private void ExportCsv()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"nettool_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ExportService.ExportCsv(_metricsHistory, dlg.FileName);
                    AddLog($"Exported CSV: {dlg.FileName}");
                    StatusText = $"Exported to {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex) { AddLog($"Export error: {ex.Message}"); }
            }
        }

        private void ExportJson()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export JSON",
                Filter = "JSON files (*.json)|*.json",
                FileName = $"nettool_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ExportService.ExportJson(_metricsHistory, dlg.FileName);
                    AddLog($"Exported JSON: {dlg.FileName}");
                    StatusText = $"Exported to {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex) { AddLog($"Export error: {ex.Message}"); }
            }
        }

        private void SaveTemplate()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Template",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = TemplateService.GetTemplateDirectory(),
                FileName = "my_template.json",
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    TemplateService.Save(BuildCurrentConfig(), dlg.FileName);
                    AddLog($"Template saved: {Path.GetFileName(dlg.FileName)}");
                    StatusText = "Template saved";
                }
                catch (Exception ex) { AddLog($"Save template error: {ex.Message}"); }
            }
        }

        private void LoadTemplate()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Template",
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = TemplateService.GetTemplateDirectory(),
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var config = TemplateService.Load(dlg.FileName);
                    if (config == null) { StatusText = "Error: Could not load template"; return; }

                    Url = config.Request.Url;
                    Method = config.Request.Method;
                    Body = config.Request.Body;
                    ExpectedStatus = config.Request.ExpectedStatus;
                    Concurrency = config.Load.Concurrency;
                    DurationSec = config.Load.DurationSec;
                    RampUpSec = config.Load.RampUpSec;
                    TimeoutMs = config.TimeoutMs;
                    Headers = config.Request.Headers.Count > 0 ? JsonSerializer.Serialize(config.Request.Headers) : "{}";

                    AddLog($"Template loaded: {Path.GetFileName(dlg.FileName)}");
                    StatusText = $"Template loaded: {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex) { AddLog($"Load template error: {ex.Message}"); }
            }
        }

        #endregion
    }
}
