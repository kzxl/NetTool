using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.HttpHeaders
{
    public class HeaderEntry
    {
        public string Category { get; set; } = "";
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class HttpHeadersViewModel : ToolViewModelBase
    {
        private string _url = "https://google.com";
        private string _method = "GET";
        private int _timeoutMs = 10000;

        public string Url { get => _url; set => SetProperty(ref _url, value); }
        public string Method { get => _method; set => SetProperty(ref _method, value); }
        public int TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }

        public ObservableCollection<HeaderEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusText = "Error: URL is required";
                return;
            }

            Results.Clear();
            AddLog($"Fetching headers from {Url}...");

            var config = new
            {
                url = Url.Trim(),
                method = Method.Trim(),
                timeout_ms = TimeoutMs,
            };

            RunEngine("httpheaders", JsonSerializer.Serialize(config));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString() ?? "header";
                var key = root.GetProperty("key").GetString() ?? "";
                var value = root.GetProperty("value").GetString() ?? "";

                var category = type switch
                {
                    "status" => "🔵 Status",
                    "tls" => "🔒 TLS",
                    "header" => "📋 Header",
                    "error" => "❌ Error",
                    _ => type
                };

                Results.Add(new HeaderEntry { Category = category, Key = key, Value = value });
                AddLog($"  {key}: {value}");
                StatusText = $"Received {Results.Count} entries from {Url}";
            }
            catch
            {
                AddLog(line);
            }
        }

        protected override void OnEngineExited(int exitCode)
        {
            AddLog($"--- HTTP headers completed: {Results.Count} entries ---");
        }
    }
}
