using System.Collections.ObjectModel;
using NetTool.UI.Core;
using NetTool.UI.Modules.LoadTest;
using NetTool.UI.ViewModels;

namespace NetTool.UI.ViewModels
{
    /// <summary>
    /// Shell ViewModel — orchestrate tools từ ToolRegistry.
    /// MainWindow bind tới ViewModel này.
    /// </summary>
    public class ShellViewModel : ViewModelBase
    {
        private ITool? _selectedTool;

        public ShellViewModel()
        {
            // Register modules — thêm module mới chỉ cần 1 dòng ở đây
            ToolRegistry.Register(new LoadTestTool());

            Tools = new ObservableCollection<ITool>(ToolRegistry.Tools);

            if (Tools.Count > 0)
                SelectedTool = Tools[0];
        }

        /// <summary>Danh sách tools hiển thị</summary>
        public ObservableCollection<ITool> Tools { get; }

        /// <summary>Tool đang active</summary>
        public ITool? SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        /// <summary>Status text từ active tool</summary>
        public string StatusText => SelectedTool?.ViewModel.StatusText ?? "Ready";
    }
}
