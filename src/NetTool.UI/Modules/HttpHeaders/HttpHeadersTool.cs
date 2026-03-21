using System.Windows;
using NetTool.UI.Core;

namespace NetTool.UI.Modules.HttpHeaders
{
    public class HttpHeadersTool : ITool
    {
        private HttpHeadersViewModel? _viewModel;

        public string Name => "HTTP Headers";
        public string Icon => "📋";
        public string Description => "Inspect HTTP response headers & TLS info";
        public int Order => 50;

        public ToolViewModelBase ViewModel => _viewModel ??= new HttpHeadersViewModel();

        public FrameworkElement CreateView()
        {
            var view = new HttpHeadersPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
