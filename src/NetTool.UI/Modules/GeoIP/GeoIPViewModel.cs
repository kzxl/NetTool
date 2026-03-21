using System.Collections.ObjectModel;
using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.GeoIP
{
    public class GeoEntry { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

    public class GeoIPViewModel : ToolViewModelBase
    {
        private string _ip = "8.8.8.8";
        public string IP { get => _ip; set => SetProperty(ref _ip, value); }
        public ObservableCollection<GeoEntry> Results { get; } = new();

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(IP)) { StatusText = "Error: IP required"; return; }
            Results.Clear();
            AddLog($"Looking up geolocation for {IP}...");
            RunEngine("geoip", JsonSerializer.Serialize(new { ip = IP.Trim() }));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                var key = r.GetProperty("key").GetString() ?? "";
                var value = r.GetProperty("value").GetString() ?? "";
                Results.Add(new GeoEntry { Key = key, Value = value });
                AddLog($"  {key}: {value}");
                StatusText = $"GeoIP: {Results.Count} fields";
            }
            catch { AddLog(line); }
        }

        protected override void OnEngineExited(int exitCode) { AddLog($"--- GeoIP lookup completed ---"); }
    }
}
