using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.DnsLookup
{
    public class DnsTool : ITool
    {
        private DnsViewModel? _viewModel;

        public string Name => "DNS Lookup";
        public string Icon => "🔍";
        public string Description => "DNS record lookup (A, AAAA, MX, NS, TXT, CNAME)";
        public int Order => 20;

        public ToolViewModelBase ViewModel => _viewModel ??= new DnsViewModel();

        public FrameworkElement CreateView()
        {
            var view = new DnsPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
