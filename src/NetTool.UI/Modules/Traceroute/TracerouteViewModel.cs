using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.Traceroute
{
    public class HopEntry
    {
        public int Hop { get; set; }
        public string IP { get; set; } = "";
        public string Hostname { get; set; } = "";
        public string Latency { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class TracerouteViewModel : ToolViewModelBase
    {
        private string _host = "google.com";
        private int _maxHops = 30;
        private int _timeoutMs = 2000;

        public string Host { get => _host; set => SetProperty(ref _host, value); }
        public int MaxHops { get => _maxHops; set => SetProperty(ref _maxHops, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }

        public ObservableCollection<HopEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Host)) { StatusText = "Error: Host required"; return; }
            Results.Clear();
            AddLog($"Tracing route to {Host} (max {MaxHops} hops)...");
            var config = new { host = Host.Trim(), max_hops = MaxHops, timeout_ms = TimeoutMs };
            RunEngine("traceroute", JsonSerializer.Serialize(config));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                var hop = r.GetProperty("hop").GetInt32();
                var ip = r.GetProperty("ip").GetString() ?? "*";
                var host = r.TryGetProperty("hostname", out var h) ? h.GetString() ?? "" : "";
                var latency = r.GetProperty("latency_ms").GetDouble();
                var status = r.GetProperty("status").GetString() ?? "";

                Results.Add(new HopEntry { Hop = hop, IP = ip, Hostname = host, Latency = $"{latency:F1} ms", Status = status.ToUpper() });
                AddLog($"  {hop}. {ip} ({host}) — {latency:F1}ms [{status}]");
                StatusText = $"Hop {hop}: {ip} — {latency:F1}ms";
            }
            catch { AddLog(line); }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- Traceroute completed: {Results.Count} hops ---");
        }
    }
}
