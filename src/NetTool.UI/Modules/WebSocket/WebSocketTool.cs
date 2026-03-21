using System.Windows;
using NetTool.UI.Core;
namespace NetTool.UI.Modules.WebSocket
{
    public class WebSocketTool : ITool
    {
        private WebSocketViewModel? _vm;
        public string Name => "WebSocket";
        public string Icon => "⚡";
        public string Description => "WebSocket connection tester";
        public string Group => "Web";
        public int Order => 54;
        public ToolViewModelBase ViewModel => _vm ??= new WebSocketViewModel();
        public FrameworkElement CreateView() { var v = new WebSocketPanel(); v.DataContext = ViewModel; return v; }
    }
}
