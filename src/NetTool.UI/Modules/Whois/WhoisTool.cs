using System.Windows;
using NetTool.UI.Core;
namespace NetTool.UI.Modules.Whois
{
    public class WhoisTool : ITool
    {
        private WhoisViewModel? _vm;
        public string Name => "Whois";
        public string Icon => "📝";
        public string Description => "Domain WHOIS/RDAP lookup";
        public string Group => "Discovery";
        public int Order => 44;
        public ToolViewModelBase ViewModel => _vm ??= new WhoisViewModel();
        public FrameworkElement CreateView() { var v = new WhoisPanel(); v.DataContext = ViewModel; return v; }
    }
}
