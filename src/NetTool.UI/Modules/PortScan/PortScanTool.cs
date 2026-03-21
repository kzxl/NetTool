using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.PortScan
{
    public class PortScanTool : ITool
    {
        private PortScanViewModel? _viewModel;

        public string Name => "Port Scanner";
        public string Icon => "🔓";
        public string Description => "TCP port scanner with service detection";
        public string Group => "Discovery";
        public int Order => 30;

        public ToolViewModelBase ViewModel => _viewModel ??= new PortScanViewModel();

        public FrameworkElement CreateView()
        {
            var view = new PortScanPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
