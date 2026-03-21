using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.SSLChecker
{
    public class SSLEntry { public string Category { get; set; } = ""; public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

    public class SSLViewModel : ToolViewModelBase
    {
        private string _host = "google.com";
        private int _port = 443;
        public string Host { get => _host; set => SetProperty(ref _host, value); }
        public int Port { get => _port; set => SetProperty(ref _port, value); }
        public ObservableCollection<SSLEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Host)) { StatusText = "Error: Host required"; return; }
            Results.Clear();
            AddLog($"Checking SSL certificate for {Host}:{Port}...");
            RunEngine("ssl", JsonSerializer.Serialize(new { host = Host.Trim(), port = Port, timeout_ms = 10000 }));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                var type = r.GetProperty("type").GetString() ?? "";
                var key = r.GetProperty("key").GetString() ?? "";
                var value = r.GetProperty("value").GetString() ?? "";
                var cat = type switch { "status" => "✅ Status", "tls" => "🔒 TLS", "leaf" => "📜 Certificate", "chain" => "🔗 Chain", "warning" => "⚠️ Warning", "error" => "❌ Error", _ => type };
                Results.Add(new SSLEntry { Category = cat, Key = key, Value = value });
                AddLog($"  {key}: {value}");
                StatusText = $"SSL: {Results.Count} entries for {Host}";
            }
            catch { AddLog(line); }
        }

        protected override void OnEngineExited(int exitCode) { AddLog($"--- SSL check completed ---"); }
    }
}
