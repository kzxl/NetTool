using System.Windows;
using System.Windows.Controls;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<ITool, FrameworkElement> _viewCache = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        // When WPF changes the selected tab, we load the tool's view
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Find the TabControl and hook SelectionChanged
            var tabControl = FindTabControl(this);
            if (tabControl != null)
            {
                tabControl.SelectionChanged += TabControl_SelectionChanged;
                // Load initial tab
                LoadToolContent(tabControl);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tab)
                LoadToolContent(tab);
        }

        private void LoadToolContent(TabControl tabControl)
        {
            if (tabControl.SelectedItem is not ITool tool) return;

            // Get or create the view for this tool
            if (!_viewCache.TryGetValue(tool, out var view))
            {
                view = tool.CreateView();
                _viewCache[tool] = view;
            }

            // Find the ContentPresenter for the selected tab and set its content
            var tabItem = tabControl.ItemContainerGenerator.ContainerFromItem(tool) as TabItem;
            if (tabItem != null)
            {
                tabItem.Content = view;
            }
        }

        private static TabControl? FindTabControl(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is TabControl tab) return tab;
                var result = FindTabControl(child);
                if (result != null) return result;
            }
            return null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Dispose all tool ViewModels
            if (DataContext is ShellViewModel shell)
            {
                foreach (var tool in shell.Tools)
                {
                    tool.ViewModel.Dispose();
                }
            }
        }
    }
}