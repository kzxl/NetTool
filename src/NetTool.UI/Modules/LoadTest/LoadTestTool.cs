using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.LoadTest
{
    public class LoadTestTool : ITool
    {
        private LoadTestViewModel? _viewModel;

        public string Name => "API Load Test";
        public string Icon => "🚀";
        public string Description => "HTTP API performance & stress testing";
        public int Order => 0;

        public ToolViewModelBase ViewModel => _viewModel ??= new LoadTestViewModel();

        public FrameworkElement CreateView()
        {
            var view = new LoadTestPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
