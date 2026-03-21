using System.Windows;
using NetTool.UI.Core;
namespace NetTool.UI.Modules.GeoIP
{
    public class GeoIPTool : ITool
    {
        private GeoIPViewModel? _vm;
        public string Name => "GeoIP";
        public string Icon => "🌍";
        public string Description => "IP geolocation lookup";
        public string Group => "Discovery";
        public int Order => 42;
        public ToolViewModelBase ViewModel => _vm ??= new GeoIPViewModel();
        public FrameworkElement CreateView() { var v = new GeoIPPanel(); v.DataContext = ViewModel; return v; }
    }
}
