using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.Ping
{
    public class PingTool : ITool
    {
        private PingViewModel? _viewModel;

        public string Name => "Ping";
        public string Icon => "📡";
        public string Description => "TCP ping with latency tracking";
        public int Order => 10;

        public ToolViewModelBase ViewModel => _viewModel ??= new PingViewModel();

        public FrameworkElement CreateView()
        {
            var view = new PingPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
