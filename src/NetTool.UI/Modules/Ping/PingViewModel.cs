using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;
using SkiaSharp;

namespace NetTool.UI.Modules.Ping
{
    public class PingViewModel : ToolViewModelBase
    {
        private string _host = "google.com";
        private int _count = 20;
        private int _intervalMs = 1000;
        private int _timeoutMs = 3000;

        // Stats
        private int _sent;
        private int _received;
        private int _failed;
        private string _avgLatency = "-";
        private string _minLatency = "-";
        private string _maxLatency = "-";
        private string _resolvedIp = "-";

        private readonly List<double> _latencies = new();
        private readonly ObservableCollection<ObservableValue> _latencyValues = new();

        public PingViewModel()
        {
            InitChartSeries();
        }

        #region Properties

        public string Host { get => _host; set => SetProperty(ref _host, value); }
        public int Count { get => _count; set => SetProperty(ref _count, value); }
        public int IntervalMs { get => _intervalMs; set => SetProperty(ref _intervalMs, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }

        public int Sent { get => _sent; set => SetProperty(ref _sent, value); }
        public int Received { get => _received; set => SetProperty(ref _received, value); }
        public int Failed { get => _failed; set => SetProperty(ref _failed, value); }
        public string AvgLatency { get => _avgLatency; set => SetProperty(ref _avgLatency, value); }
        public string MinLatency { get => _minLatency; set => SetProperty(ref _minLatency, value); }
        public string MaxLatency { get => _maxLatency; set => SetProperty(ref _maxLatency, value); }
        public string ResolvedIp { get => _resolvedIp; set => SetProperty(ref _resolvedIp, value); }

        public ISeries[] LatencySeries { get; private set; } = Array.Empty<ISeries>();

        #endregion

        private void InitChartSeries()
        {
            LatencySeries = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = _latencyValues,
                    Name = "Latency",
                    GeometrySize = 4,
                    GeometryFill = new SolidColorPaint(SKColors.DodgerBlue),
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 1),
                    LineSmoothness = 0.3,
                    Stroke = new SolidColorPaint(new SKColor(0x56, 0xD4, 0xDD), 2),
                    Fill = new SolidColorPaint(new SKColor(0x56, 0xD4, 0xDD, 30)),
                }
            };
        }

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                StatusText = "Error: Host is required";
                return;
            }

            _latencyValues.Clear();
            _latencies.Clear();
            Sent = 0;
            Received = 0;
            Failed = 0;
            AvgLatency = "-";
            MinLatency = "-";
            MaxLatency = "-";
            ResolvedIp = "-";

            AddLog($"Pinging {Host} ({Count} times, interval {IntervalMs}ms)...");

            var config = new
            {
                host = Host.Trim(),
                count = Count,
                interval_ms = IntervalMs,
                timeout_ms = TimeoutMs,
            };

            var configJson = JsonSerializer.Serialize(config);
            RunEngine("ping", configJson);
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var seq = root.GetProperty("seq").GetInt32();
                var status = root.GetProperty("status").GetString() ?? "error";
                var latency = root.GetProperty("latency_ms").GetDouble();
                var ip = root.TryGetProperty("ip", out var ipProp) ? ipProp.GetString() ?? "-" : "-";

                Sent = seq;
                ResolvedIp = ip;

                if (status == "ok")
                {
                    Received++;
                    _latencies.Add(latency);
                    _latencyValues.Add(new ObservableValue(latency));
                    UpdateLatencyStats();
                    StatusText = $"Ping #{seq}: {latency:F1}ms ({ip})";
                    AddLog($"#{seq} Reply from {ip}: {latency:F1}ms");
                }
                else
                {
                    Failed++;
                    _latencyValues.Add(new ObservableValue(null));
                    var errMsg = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "timeout";
                    StatusText = $"Ping #{seq}: {status} — {errMsg}";
                    AddLog($"#{seq} {status}: {errMsg}");
                }
            }
            catch
            {
                AddLog(line);
            }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- Ping completed: {Sent} sent, {Received} received, {Failed} failed ---");
            if (_latencies.Count > 0)
            {
                AddLog($"    Avg: {AvgLatency}, Min: {MinLatency}, Max: {MaxLatency}");
            }
        }

        private void UpdateLatencyStats()
        {
            if (_latencies.Count == 0) return;
            AvgLatency = $"{_latencies.Average():F1} ms";
            MinLatency = $"{_latencies.Min():F1} ms";
            MaxLatency = $"{_latencies.Max():F1} ms";
        }
    }
}
