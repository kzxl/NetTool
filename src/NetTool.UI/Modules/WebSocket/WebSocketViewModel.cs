using System.Text.Json;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI.Modules.WebSocket
{
    public class WebSocketViewModel : ToolViewModelBase
    {
        private string _url = "wss://echo.websocket.org";
        private string _message = "Hello from NetTool!";
        public string Url { get => _url; set => SetProperty(ref _url, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }

        protected override void OnStart()
        {
            if (string.IsNullOrWhiteSpace(Url)) { StatusText = "Error: URL required"; return; }
            AddLog($"Testing WebSocket: {Url}...");
            RunEngine("websocket", JsonSerializer.Serialize(new { url = Url.Trim(), message = Message, count = 1 }));
        }

        protected override void OnEngineOutput(string line)
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                var type = r.GetProperty("type").GetString() ?? "";
                var msg = r.GetProperty("message").GetString() ?? "";
                var icon = type switch { "connected" => "✅", "error" => "❌", "header" => "📋", "info" => "ℹ️", _ => "📨" };
                AddLog($"  {icon} [{type}] {msg}");
                StatusText = $"WS: {msg}";
            }
            catch { AddLog(line); }
        }

        protected override void OnEngineExited(int exitCode) { AddLog($"--- WebSocket test completed ---"); }
    }
}
