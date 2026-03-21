using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.IPScan
{
    public class IPScanTool : ITool
    {
        private IPScanViewModel? _viewModel;

        public string Name => "IP Scanner";
        public string Icon => "🌐";
        public string Description => "Scan subnet for live hosts";
        public int Order => 40;

        public ToolViewModelBase ViewModel => _viewModel ??= new IPScanViewModel();

        public FrameworkElement CreateView()
        {
            var view = new IPScanPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
