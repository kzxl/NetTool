using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.IPScan
{
    public class IPEntry
    {
        public string IP { get; set; } = "";
        public string Status { get; set; } = "";
        public string Latency { get; set; } = "";
        public string Hostname { get; set; } = "";
    }

    public class IPScanViewModel : ToolViewModelBase
    {
        private string _subnet = "192.168.1.0/24";
        private int _timeoutMs = 1000;
        private int _workers = 50;
        private int _upCount;
        private int _downCount;
        private int _scannedCount;

        public string Subnet { get => _subnet; set => SetProperty(ref _subnet, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
        public int Workers { get => _workers; set => SetProperty(ref _workers, value); }
        public int UpCount { get => _upCount; set => SetProperty(ref _upCount, value); }
        public int DownCount { get => _downCount; set => SetProperty(ref _downCount, value); }
        public int ScannedCount { get => _scannedCount; set => SetProperty(ref _scannedCount, value); }

        public ObservableCollection<IPEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Subnet))
            {
                StatusText = "Error: Subnet is required";
                return;
            }

            Results.Clear();
            UpCount = 0;
            DownCount = 0;
            ScannedCount = 0;
            AddLog($"Scanning subnet {Subnet}...");

            var config = new
            {
                subnet = Subnet.Trim(),
                timeout_ms = TimeoutMs,
                workers = Workers,
            };

            RunEngine("ipscan", JsonSerializer.Serialize(config));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var ip = root.GetProperty("ip").GetString() ?? "";
                var status = root.GetProperty("status").GetString() ?? "down";
                var latency = root.TryGetProperty("latency_ms", out var latProp) ? latProp.GetDouble() : 0;
                var hostname = root.TryGetProperty("hostname", out var hostProp) ? hostProp.GetString() ?? "" : "";

                ScannedCount++;

                if (status == "up")
                {
                    UpCount++;
                    Results.Add(new IPEntry
                    {
                        IP = ip,
                        Status = "UP",
                        Latency = $"{latency:F1} ms",
                        Hostname = hostname,
                    });
                    var hostStr = string.IsNullOrEmpty(hostname) ? "" : $" ({hostname})";
                    AddLog($"  {ip}: UP — {latency:F1}ms{hostStr}");
                }
                else
                {
                    DownCount++;
                }

                StatusText = $"Scanned {ScannedCount} — {UpCount} hosts up";
            }
            catch
            {
                AddLog(line);
            }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- Scan completed: {ScannedCount} scanned, {UpCount} up, {DownCount} down ---");
        }
    }
}
