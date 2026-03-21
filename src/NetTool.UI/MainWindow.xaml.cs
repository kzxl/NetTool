using System.Windows;
using System.Windows.Controls;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ShellViewModel shell) return;

            foreach (var tool in shell.Tools)
            {
                var header = new TextBlock
                {
                    Text = $"{tool.Icon}  {tool.Name}",
                    Margin = new Thickness(4, 2, 4, 2),
                };

                var tabItem = new TabItem
                {
                    Header = header,
                    Content = tool.CreateView(),
                    Padding = new Thickness(12, 8, 12, 8),
                    Tag = tool,
                };

                ToolTabs.Items.Add(tabItem);
            }

            if (ToolTabs.Items.Count > 0)
                ToolTabs.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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