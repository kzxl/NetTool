using System.Windows;
using NetTool.UI.Core;
namespace NetTool.UI.Modules.Traceroute
{
    public class TracerouteTool : ITool
    {
        private TracerouteViewModel? _vm;
        public string Name => "Traceroute";
        public string Icon => "🔄";
        public string Description => "Trace route to host";
        public string Group => "Network";
        public int Order => 12;
        public ToolViewModelBase ViewModel => _vm ??= new TracerouteViewModel();
        public FrameworkElement CreateView() { var v = new TraceroutePanel(); v.DataContext = ViewModel; return v; }
    }
}
