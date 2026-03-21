namespace NetTool.UI.Core
{
    /// <summary>
    /// Registry quản lý danh sách tools.
    /// Modules tự đăng ký vào đây — Core không biết chi tiết module.
    /// </summary>
    public static class ToolRegistry
    {
        private static readonly List<ITool> _tools = new();

        /// <summary>Đăng ký 1 tool module</summary>
        public static void Register(ITool tool) => _tools.Add(tool);

        /// <summary>Danh sách tools đã sắp xếp theo Order</summary>
        public static IReadOnlyList<ITool> Tools => _tools.OrderBy(t => t.Order).ToList();
    }
}
