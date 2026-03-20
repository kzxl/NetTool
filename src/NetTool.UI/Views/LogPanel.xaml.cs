using System.Collections.Specialized;
using System.Windows.Controls;

namespace NetTool.UI.Views
{
    public partial class LogPanel : UserControl
    {
        public LogPanel()
        {
            InitializeComponent();

            // Auto-scroll to bottom when new items are added
            if (LogListBox.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += (_, _) => ScrollToBottom();
            }

            Loaded += (_, _) =>
            {
                if (LogListBox.ItemsSource is INotifyCollectionChanged col)
                {
                    col.CollectionChanged += (_, _) => ScrollToBottom();
                }
            };
        }

        private void ScrollToBottom()
        {
            if (LogListBox.Items.Count > 0)
            {
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            }
        }
    }
}
