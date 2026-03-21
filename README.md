# 🌌 NetTool — Network Testing Suite

<p align="center">
  <strong>All-in-one network diagnostic & testing toolkit</strong><br/>
  WPF Dark UI + Go Engine • 11 Tools • Universe Architecture
</p>

---

## 📖 Overview

**NetTool** is a modular network toolkit with 11 built-in tools, powered by a Go engine backend and a premium WPF dark-themed UI.

| Component | Tech Stack | Role |
|-----------|-----------|------|
| **UI** | C# WPF (.NET 8) + LiveCharts2 | Dark theme UI, realtime charts, grouped tabs |
| **Engine** | Go (stdlib) | Network operations, JSON stdout streaming |

### Architecture — Universe Plugin System

```
┌─────────────────┐     config.json     ┌──────────────────┐
│   WPF UI        │ ──────────────────► │   Go Engine      │
│   (C# .NET 8)   │                     │   (CLI binary)   │
│                 │ ◄────────────────── │                  │
│  ToolRegistry   │   stdout JSON line  │  11 commands     │
│  ITool modules  │                     │  Worker pools    │
│  Nested tabs    │                     │  Concurrent ops  │
└─────────────────┘                     └──────────────────┘
```

Adding a new tool = 1 folder + 1 line register. Zero changes to existing code.

---

## 🛠️ Tools (11)

### 🌐 Web
| Tool | Description |
|------|-------------|
| 🚀 **API Load Test** | HTTP stress testing, RPS/latency charts, ramp-up, export CSV/JSON |
| 📋 **HTTP Headers** | Response headers, TLS info, response time |
| 🔐 **SSL Checker** | Certificate chain, expiry status, cipher suite, DNS names |
| ⚡ **WebSocket** | WebSocket upgrade handshake test |

### 🔧 Network
| Tool | Description |
|------|-------------|
| 📡 **Ping** | TCP ping with realtime latency chart & statistics |
| 🔄 **Traceroute** | Route tracing, hop-by-hop latency table |

### 🔍 Discovery
| Tool | Description |
|------|-------------|
| 🔍 **DNS Lookup** | A, AAAA, CNAME, MX, NS, TXT records |
| 🔓 **Port Scanner** | Concurrent TCP scan, service detection, port ranges |
| 🌐 **IP Scanner** | Subnet CIDR scan, live host discovery, reverse DNS |
| 🌍 **GeoIP** | IP geolocation — country, city, ISP, coordinates |
| 📝 **Whois** | Domain RDAP lookup — registrar, expiry, nameservers |

---

## ⚡ Quick Start

### Requirements
- **Windows 10/11** (64-bit)
- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Go 1.21+** — [Download](https://go.dev/dl/)

### Build & Run

```bash
# Clone
git clone https://github.com/kzxl/NetTool.git
cd NetTool

# Build Go Engine
cd engine && go build -o ../engine.exe . && cd ..

# Run UI
dotnet run --project src/NetTool.UI
```

### Using Pre-built Release

Download from [Releases](https://github.com/kzxl/NetTool/releases):
- **Full** — Self-contained, no runtime needed (~70MB)
- **Lite** — Requires .NET 8 Desktop Runtime (~10MB)

---

## 📁 Project Structure

```
NetTool/
├── engine/                              # Go Engine (11 commands)
│   ├── main.go                          # Entry + dispatcher
│   ├── cmd_loadtest.go                  # API load testing
│   ├── cmd_ping.go                      # TCP ping
│   ├── cmd_traceroute.go                # Route tracing
│   ├── cmd_dns.go                       # DNS lookup
│   ├── cmd_portscan.go                  # Port scanning
│   ├── cmd_ipscan.go                    # IP/subnet scanning
│   ├── cmd_httpheaders.go               # HTTP headers
│   ├── cmd_ssl.go                       # SSL certificate check
│   ├── cmd_geoip.go                     # IP geolocation
│   ├── cmd_whois.go                     # Domain WHOIS/RDAP
│   └── cmd_websocket.go                 # WebSocket test
│
├── src/NetTool.UI/                      # WPF Application
│   ├── Core/                            # ITool, ToolRegistry, ToolViewModelBase
│   ├── Modules/                         # 11 tool modules (self-contained)
│   │   ├── LoadTest/                    # Tool + ViewModel + Panel
│   │   ├── Ping/
│   │   ├── Traceroute/
│   │   ├── DnsLookup/
│   │   ├── PortScan/
│   │   ├── IPScan/
│   │   ├── GeoIP/
│   │   ├── Whois/
│   │   ├── HttpHeaders/
│   │   ├── SSLChecker/
│   │   └── WebSocket/
│   ├── ViewModels/ShellViewModel.cs     # Module registration (1 line each)
│   ├── Themes/Styles.xaml               # GitHub-inspired dark theme
│   └── MainWindow.xaml                  # Nested tabs (group → tools)
│
└── README.md
```

---

## 🏗️ Adding a New Tool

1. Create `engine/cmd_yourcommand.go`
2. Register in `engine/main.go` switch
3. Create `Modules/YourTool/` (Tool + ViewModel + Panel)
4. Add `ToolRegistry.Register(new YourTool())` in `ShellViewModel.cs`

**That's it.** The tab appears automatically in the correct group.

---

## ⚠️ Disclaimer

> This tool is for **legitimate testing purposes only.**
> Do not use it to attack external systems (DDoS).

---

## 📄 License

MIT License
