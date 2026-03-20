# 🚀 NetTool - API Performance Tester

<p align="center">
  <strong>High-performance API load testing tool</strong><br/>
  WPF Dark UI + Go Engine • Realtime Charts • Detailed Metrics
</p>

---

## 📖 Giới thiệu

**NetTool** là công cụ test hiệu suất API gồm 2 components:

| Component | Tech Stack | Vai trò |
|-----------|-----------|---------|
| **UI** | C# WPF (.NET 8) + LiveCharts2 | Giao diện cấu hình, realtime chart, log |
| **Engine** | Go (stdlib only) | Gửi HTTP requests, thu thập metrics, stream qua stdout |

### Kiến trúc

```
┌─────────────────┐     config.json     ┌──────────────────┐
│   WPF UI        │ ──────────────────► │   Go Engine      │
│   (C# .NET 8)   │                     │   (CLI binary)   │
│                 │ ◄────────────────── │                  │
│  • Config Panel │   stdout JSON line  │  • Worker Pool   │
│  • Chart Panel  │                     │  • HTTP Client   │
│  • Summary Panel│                     │  • Metrics Agg   │
│  • Log Panel    │                     │  • Resp Validate │
└─────────────────┘                     └──────────────────┘
```

---

## 🛠️ Yêu cầu hệ thống

- **Windows 10/11** (64-bit)
- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Go 1.21+** — [Download](https://go.dev/dl/)

---

## ⚡ Quick Start

### 1. Clone repo

```bash
git clone https://github.com/kzxl/NetTool.git
cd NetTool
```

### 2. Build Go Engine

```bash
cd engine
go build -o ../engine.exe .
cd ..
```

### 3. Chạy UI

```bash
dotnet run --project src/NetTool.UI
```

### 4. Sử dụng

1. Nhập **URL**, chọn **Method**, cấu hình **Headers/Body** nếu cần
2. Đặt **Concurrency**, **Duration**, **Ramp-up**, **Timeout**
3. (Tuỳ chọn) Đặt **Expected Status** để validate response
4. (Tuỳ chọn) Đặt **Alert Threshold** p95/p99
5. Bấm **▶ START** → theo dõi realtime chart và log
6. Khi test xong → **Summary Panel** hiện tổng kết
7. Bấm **Export CSV/JSON** để xuất kết quả

### Test Engine độc lập (không cần UI)

```bash
./engine.exe sample_config.json
```

---

## ✨ Tính năng

### Core
- 🎯 **Realtime Charts** — RPS + Latency (Avg/p95/p99) LiveCharts2
- 📈 **Detailed Metrics** — Min/Max/StdDev/p50/p95/p99, Status Code distribution, Bytes, Active Connections
- 🔄 **Ramp-up** — Tăng dần concurrency trong thời gian cấu hình
- 🛑 **Graceful Shutdown** — Stop giữa chừng không mất data

### Phase 2 — Enhanced UI
- 📊 **Export CSV/JSON** — Xuất toàn bộ metrics history ra file
- 📂 **Request Templates** — Save/Load cấu hình test thường dùng (`%AppData%/NetTool/templates/`)
- 🔔 **Alert Thresholds** — Cảnh báo visual khi p95/p99 vượt ngưỡng (ms)
- 📋 **Summary Panel** — Tổng kết overall metrics (3×3 grid) khi test kết thúc
- ✅ **Response Validation** — Kiểm tra expected HTTP status code

---

## 📊 Metrics chi tiết

Mỗi giây, engine xuất 1 JSON line chứa:

| Metric | Key | Mô tả |
|--------|-----|--------|
| **RPS** | `rps` | Số request hoàn thành trong 1 giây |
| **Avg Latency** | `avg` | Trung bình response time (ms) |
| **Min Latency** | `min` | Response time nhanh nhất (ms) |
| **Max Latency** | `max` | Response time chậm nhất (ms) |
| **Std Dev** | `stddev` | Độ lệch chuẩn response time |
| **p50** | `p50` | Median (50th percentile) |
| **p95** | `p95` | 95% request nhanh hơn giá trị này |
| **p99** | `p99` | 99th percentile |
| **Error Rate** | `err` | Tỷ lệ lỗi (0.0 – 1.0) |
| **Status Codes** | `codes` | Phân bổ 2xx / 3xx / 4xx / 5xx |
| **Bytes Received** | `bytes` | Tổng bytes nhận trong 1s |
| **Active Conns** | `conns` | Số connection đang active |

### Ví dụ output

```json
{
  "t": 5, "rps": 120,
  "avg": 245.3, "min": 98, "max": 1205, "stddev": 156.2,
  "p50": 210, "p95": 450, "p99": 890,
  "err": 0.008, "total": 120, "success": 119, "fail": 1,
  "codes": { "2xx": 119, "3xx": 0, "4xx": 1, "5xx": 0 },
  "bytes": 245760, "conns": 10
}
```

---

## ⚙️ Config Schema

```json
{
  "request": {
    "url": "https://api.example.com/endpoint",
    "method": "POST",
    "headers": {
      "Authorization": "Bearer your-token",
      "Content-Type": "application/json"
    },
    "body": "{\"key\": \"value\"}",
    "expectedStatus": 200
  },
  "load": {
    "concurrency": 100,
    "durationSec": 60,
    "rampUpSec": 10
  },
  "timeoutMs": 5000
}
```

| Field | Mô tả | Default |
|-------|--------|---------|
| `request.url` | URL target (bắt buộc) | — |
| `request.method` | HTTP method | `GET` |
| `request.headers` | Custom headers | `{}` |
| `request.body` | Request body | `""` |
| `request.expectedStatus` | Expected status code (0 = any 2xx-3xx) | `0` |
| `load.concurrency` | Số worker đồng thời | `10` |
| `load.durationSec` | Thời gian test (giây) | `30` |
| `load.rampUpSec` | Thời gian ramp-up | `5` |
| `timeoutMs` | Timeout mỗi request (ms) | `5000` |

---

## 📁 Cấu trúc dự án

```
NetTool/
├── engine/                         # Go Engine
│   ├── main.go                     # Entry point
│   ├── config/config.go            # Config loader + ExpectedStatus
│   ├── worker/pool.go              # Worker pool + response validation
│   ├── metrics/collector.go        # Metrics aggregation
│   └── reporter/reporter.go        # JSON stdout reporter
│
├── src/NetTool.UI/                 # WPF Application
│   ├── Converters/                 # BoolToVisibilityConverter
│   ├── Models/                     # TestConfig, MetricsData, StatusCodeDist
│   ├── ViewModels/                 # MainViewModel, RelayCommand, ViewModelBase
│   ├── Services/                   # EngineRunner, MetricsParser, ExportService, TemplateService
│   ├── Views/                      # ConfigPanel, ChartPanel, SummaryPanel, LogPanel
│   └── Themes/Styles.xaml          # Dark theme + custom ComboBox
│
├── sample_config.json
└── README.md
```

---

## 🧪 Test Scenarios

| Case | Concurrency | Duration | Mục đích |
|------|------------|----------|----------|
| Smoke | 5 | 10s | Verify tool hoạt động |
| Normal | 50 | 30s | Test performance baseline |
| Stress | 100 | 60s | Kiểm tra stability |
| Extreme | 500+ | 60s | Tìm breaking point |
| Validation | 10 | 10s | Set expected status, verify error rate |

---

## 🔧 Thiết kế kỹ thuật

### Go Engine
- **Worker Pool**: Semaphore (`chan struct{}`) giới hạn goroutine
- **HTTP Client**: 1 client, connection pooling, keep-alive
- **Ramp-up**: Tăng dần concurrency trong `rampUpSec`
- **Response Validation**: So khớp `expectedStatus` nếu > 0
- **Memory**: Reset metrics slice mỗi 1s, không append vô hạn
- **Graceful Shutdown**: Context cancellation + SIGINT/SIGTERM

### WPF UI
- **MVVM**: ViewModelBase + RelayCommand
- **Thread Safety**: `Dispatcher.BeginInvoke` cho UI updates
- **Charting**: LiveCharts2 (SkiaSharp) — RPS + Latency
- **Summary**: Compute overall metrics từ `_metricsHistory`
- **Export**: CSV (full table) / JSON (array)
- **Templates**: Save/Load tại `%AppData%/NetTool/templates/`
- **Dark Theme**: Custom styled controls + ComboBox template

---

## 🗺️ Roadmap

### ✅ Phase 1 — Core (Done)
- [x] Go Engine (worker pool, metrics, reporter)
- [x] WPF UI (config, chart, log, dark theme)
- [x] Realtime metrics streaming

### ✅ Phase 2 — Enhanced UI (Done)
- [x] Export CSV/JSON
- [x] Request Templates (save/load)
- [x] Alert Thresholds (p95/p99)
- [x] Summary Panel
- [x] Response Validation

### 🔮 Phase 3 — Advanced Engine
- [ ] HTTP/2 Support
- [ ] WebSocket Testing
- [ ] Custom Scripts (giống k6)
- [ ] Variable Injection (`{{random_id}}`, `{{timestamp}}`)
- [ ] Multi-endpoint scenario testing
- [ ] Rate Limiting (giới hạn RPS)
- [ ] Certificate Pinning (mTLS)

### 🚀 Phase 4 — Distributed & Integration
- [ ] gRPC Control API
- [ ] WebSocket realtime streaming
- [ ] Distributed Mode (multi-node)
- [ ] CI/CD Integration (CLI with exit code)
- [ ] Docker Support
- [ ] Grafana Dashboard (Prometheus export)

### 💡 Ý tưởng mở rộng
- [ ] DNS / TLS Handshake metrics
- [ ] Connection Reuse Rate
- [ ] Bandwidth Throttle (simulate slow network)
- [ ] AI-powered bottleneck analysis

---

## ⚠️ Lưu ý

> **Tool này chỉ dùng cho mục đích test hợp pháp.**
> Không dùng để tấn công (DDoS) hệ thống bên ngoài.

---

## 📄 License

MIT License
