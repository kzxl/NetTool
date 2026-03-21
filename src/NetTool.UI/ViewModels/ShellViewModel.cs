using System.Collections.ObjectModel;
using NetTool.UI.Core;
using NetTool.UI.Modules.DnsLookup;
using NetTool.UI.Modules.GeoIP;
using NetTool.UI.Modules.HttpHeaders;
using NetTool.UI.Modules.IPScan;
using NetTool.UI.Modules.LoadTest;
using NetTool.UI.Modules.Ping;
using NetTool.UI.Modules.PortScan;
using NetTool.UI.Modules.SSLChecker;
using NetTool.UI.Modules.Traceroute;
using NetTool.UI.Modules.WebSocket;
using NetTool.UI.Modules.Whois;

namespace NetTool.UI.ViewModels
{
    /// <summary>
    /// Shell ViewModel — orchestrate tools từ ToolRegistry.
    /// </summary>
    public class ShellViewModel : ViewModelBase
    {
        private ITool? _selectedTool;

        public ShellViewModel()
        {
            // ── Web ─────────────────────────────────
            ToolRegistry.Register(new LoadTestTool());
            ToolRegistry.Register(new HttpHeadersTool());
            ToolRegistry.Register(new SSLTool());
            ToolRegistry.Register(new WebSocketTool());

            // ── Network ─────────────────────────────
            ToolRegistry.Register(new PingTool());
            ToolRegistry.Register(new TracerouteTool());

            // ── Discovery ───────────────────────────
            ToolRegistry.Register(new DnsTool());
            ToolRegistry.Register(new PortScanTool());
            ToolRegistry.Register(new IPScanTool());
            ToolRegistry.Register(new GeoIPTool());
            ToolRegistry.Register(new WhoisTool());

            Tools = new ObservableCollection<ITool>(ToolRegistry.Tools);

            if (Tools.Count > 0)
                SelectedTool = Tools[0];
        }

        public ObservableCollection<ITool> Tools { get; }

        public ITool? SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public string StatusText => SelectedTool?.ViewModel.StatusText ?? "Ready";
    }
}
