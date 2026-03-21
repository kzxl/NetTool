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

            var groups = shell.Tools
                .GroupBy(t => t.Group)
                .OrderBy(g => g.Min(t => t.Order));

            bool first = true;
            foreach (var group in groups)
            {
                var groupIcon = GroupIcons.GetValueOrDefault(group.Key, "📦");

                // === Outer tab header (group) ===
                var outerHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 8, 4, 8) };
                outerHeader.Children.Add(new TextBlock
                {
                    Text = groupIcon,
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                });
                outerHeader.Children.Add(new TextBlock
                {
                    Text = group.Key.ToUpper(),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9D1D9")),
                    VerticalAlignment = VerticalAlignment.Center,
                });

                // === Inner TabControl (tools in this group) ===
                var innerTabs = new TabControl
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                };

                foreach (var tool in group.OrderBy(t => t.Order))
                {
                    var toolHeader = new TextBlock
                    {
                        Text = $"{tool.Icon}  {tool.Name}",
                        Margin = new Thickness(4, 2, 4, 2),
                    };

                    var tabItem = new TabItem
                    {
                        Header = toolHeader,
                        Content = tool.CreateView(),
                        Padding = new Thickness(12, 8, 12, 8),
                        Tag = tool,
                    };

                    innerTabs.Items.Add(tabItem);
                }

                if (innerTabs.Items.Count > 0)
                    innerTabs.SelectedIndex = 0;

                // === Outer tab item ===
                var outerTab = new TabItem
                {
                    Header = outerHeader,
                    Content = innerTabs,
                    Padding = new Thickness(8, 4, 8, 4),
                };

                GroupTabs.Items.Add(outerTab);

                if (first)
                {
                    GroupTabs.SelectedItem = outerTab;
                    first = false;
                }
            }
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