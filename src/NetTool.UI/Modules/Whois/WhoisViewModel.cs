using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.Whois
{
    public class WhoisEntry { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

    public class WhoisViewModel : ToolViewModelBase
    {
        private string _domain = "google.com";
        public string Domain { get => _domain; set => SetProperty(ref _domain, value); }
        public ObservableCollection<WhoisEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Domain)) { StatusText = "Error: Domain required"; return; }
            Results.Clear();
            AddLog($"Looking up WHOIS for {Domain}...");
            RunEngine("whois", JsonSerializer.Serialize(new { domain = Domain.Trim() }));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                var key = r.GetProperty("key").GetString() ?? "";
                var value = r.GetProperty("value").GetString() ?? "";
                Results.Add(new WhoisEntry { Key = key, Value = value });
                AddLog($"  {key}: {value}");
                StatusText = $"Whois: {Results.Count} fields for {Domain}";
            }
            catch { AddLog(line); }
        }

        protected override void OnEngineExited(int exitCode) { AddLog($"--- Whois lookup completed ---"); }
    }
}
