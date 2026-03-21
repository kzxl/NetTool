using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetTool.UI.Core;
using NetTool.UI.ViewModels;

namespace NetTool.UI
{
    public partial class MainWindow : Window
    {
        private static readonly Dictionary<string, string> GroupIcons = new()
        {
            { "Web", "🌐" },
            { "Network", "🔧" },
            { "Discovery", "🔍" },
        };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ShellViewModel shell) return;

            // Group tools by Group property
            var groups = shell.Tools
                .GroupBy(t => t.Group)
                .OrderBy(g => g.Min(t => t.Order));

            foreach (var group in groups)
            {
                var groupIcon = GroupIcons.GetValueOrDefault(group.Key, "📦");

                // Group separator tab (disabled, acts as label)
                var separatorHeader = new TextBlock
                {
                    Text = $"{groupIcon} {group.Key.ToUpper()}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B949E")),
                    Margin = new Thickness(4, 2, 4, 2),
                };
                var separatorTab = new TabItem
                {
                    Header = separatorHeader,
                    IsEnabled = false,
                    Padding = new Thickness(8, 6, 8, 6),
                    Visibility = Visibility.Visible,
                };
                ToolTabs.Items.Add(separatorTab);

                // Tool tabs in this group
                foreach (var tool in group.OrderBy(t => t.Order))
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
            }

            // Select first real tool tab (skip separator)
            if (ToolTabs.Items.Count > 1)
                ToolTabs.SelectedIndex = 1;
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