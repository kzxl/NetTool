using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.PortScan
{
    public class PortEntry
    {
        public int Port { get; set; }
        public string Status { get; set; } = "";
        public string Service { get; set; } = "";
        public string Latency { get; set; } = "";
    }

    public class PortScanViewModel : ToolViewModelBase
    {
        private string _host = "google.com";
        private string _ports = "common";
        private int _timeoutMs = 2000;
        private int _workers = 50;
        private int _openCount;
        private int _closedCount;
        private int _scannedCount;

        public string Host { get => _host; set => SetProperty(ref _host, value); }
        public string Ports { get => _ports; set => SetProperty(ref _ports, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }
        public int Workers { get => _workers; set => SetProperty(ref _workers, value); }
        public int OpenCount { get => _openCount; set => SetProperty(ref _openCount, value); }
        public int ClosedCount { get => _closedCount; set => SetProperty(ref _closedCount, value); }
        public int ScannedCount { get => _scannedCount; set => SetProperty(ref _scannedCount, value); }

        public ObservableCollection<PortEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                StatusText = "Error: Host is required";
                return;
            }

            Results.Clear();
            OpenCount = 0;
            ClosedCount = 0;
            ScannedCount = 0;
            AddLog($"Scanning ports on {Host} (ports: {Ports})...");

            var config = new
            {
                host = Host.Trim(),
                ports = Ports.Trim(),
                timeout_ms = TimeoutMs,
                workers = Workers,
            };

            RunEngine("portscan", JsonSerializer.Serialize(config));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var port = root.GetProperty("port").GetInt32();
                var status = root.GetProperty("status").GetString() ?? "closed";
                var service = root.TryGetProperty("service", out var svcProp) ? svcProp.GetString() ?? "" : "";
                var latency = root.GetProperty("latency_ms").GetDouble();

                ScannedCount++;

                if (status == "open")
                {
                    OpenCount++;
                    Results.Add(new PortEntry
                    {
                        Port = port,
                        Status = "OPEN",
                        Service = service,
                        Latency = $"{latency:F1} ms"
                    });
                    AddLog($"  Port {port}: OPEN ({service}) — {latency:F1}ms");
                }
                else
                {
                    ClosedCount++;
                }

                StatusText = $"Scanned {ScannedCount} ports — {OpenCount} open";
            }
            catch
            {
                AddLog(line);
            }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- Scan completed: {ScannedCount} scanned, {OpenCount} open, {ClosedCount} closed ---");
        }
    }
}
