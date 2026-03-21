using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.DnsLookup
{
    public class DnsRecord
    {
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class DnsViewModel : ToolViewModelBase
    {
        private string _host = "google.com";

        public DnsViewModel() { }

        public string Host { get => _host; set => SetProperty(ref _host, value); }

        public ObservableCollection<DnsRecord> Records { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                StatusText = "Error: Host is required";
                return;
            }

            Records.Clear();
            AddLog($"Looking up DNS records for {Host}...");

            var config = new { host = Host.Trim() };
            var configJson = JsonSerializer.Serialize(config);
            RunEngine("dns", configJson);
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString() ?? "";
                var records = root.GetProperty("records");
                var error = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;

                if (!string.IsNullOrEmpty(error) && records.GetArrayLength() == 0)
                {
                    AddLog($"  {type}: (no records)");
                    return;
                }

                foreach (var rec in records.EnumerateArray())
                {
                    var value = rec.GetString() ?? "";
                    Records.Add(new DnsRecord { Type = type, Value = value });
                    AddLog($"  {type}: {value}");
                }

                StatusText = $"Found {Records.Count} records for {Host}";
            }
            catch
            {
                AddLog(line);
            }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- DNS lookup completed: {Records.Count} records found ---");
        }
    }
}
