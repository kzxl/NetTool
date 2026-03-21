using System.Windows;

namespace NetTool.UI.Core
{
    /// <summary>
    /// Interface cho mọi tool module trong NetTool.
    /// Mỗi tool implement interface này và đăng ký vào ToolRegistry.
    /// </summary>
    public interface ITool
    {
        /// <summary>Tên hiển thị (vd: "API Load Test")</summary>
        string Name { get; }

        /// <summary>Icon emoji (vd: "🚀")</summary>
        string Icon { get; }

        /// <summary>Mô tả ngắn</summary>
        string Description { get; }

        /// <summary>Nhóm (vd: "Network", "Discovery", "Web")</summary>
        string Group { get; }

        /// <summary>Thứ tự hiển thị trong TabControl (thấp = trước)</summary>
        int Order { get; }

        /// <summary>ViewModel cho tool này</summary>
        ToolViewModelBase ViewModel { get; }

        /// <summary>Factory tạo View (UserControl)</summary>
        FrameworkElement CreateView();
    }
}
