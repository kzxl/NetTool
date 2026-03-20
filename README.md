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
│  • Log Panel    │                     │  • Metrics Agg   │
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
3. Bấm **▶ START** → theo dõi realtime chart và log
4. Bấm **■ STOP** để dừng giữa chừng

### Test Engine độc lập (không cần UI)

```bash
./engine.exe sample_config.json
```

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
| **p99** | `p99` | 99th percentile (worst case gần max) |
| **Error Rate** | `err` | Tỷ lệ lỗi (0.0 = 0%, 1.0 = 100%) |
| **Status Codes** | `codes` | Phân bổ 2xx / 3xx / 4xx / 5xx |
| **Bytes Received** | `bytes` | Tổng bytes nhận được trong 1s |
| **Active Conns** | `conns` | Số connection đang active |

### Ví dụ output

```json
{
  "t": 5,
  "rps": 120,
  "avg": 245.3,
  "min": 98,
  "max": 1205,
  "stddev": 156.2,
  "p50": 210,
  "p95": 450,
  "p99": 890,
  "err": 0.008,
  "total": 120,
  "success": 119,
  "fail": 1,
  "codes": { "2xx": 119, "3xx": 0, "4xx": 1, "5xx": 0 },
  "bytes": 245760,
  "conns": 10
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
    "body": "{\"key\": \"value\"}"
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
| `load.concurrency` | Số worker đồng thời | `10` |
| `load.durationSec` | Thời gian test (giây) | `30` |
| `load.rampUpSec` | Thời gian ramp-up | `5` |
| `timeoutMs` | Timeout mỗi request (ms) | `5000` |

---

## 📁 Cấu trúc dự án

```
NetTool/
├── engine/                     # Go Engine
│   ├── main.go                 # Entry point, orchestration
│   ├── config/config.go        # Config loader & validation
│   ├── worker/pool.go          # Semaphore-based worker pool + ramp-up
│   ├── metrics/collector.go    # Metrics aggregation (1s window)
│   └── reporter/reporter.go    # JSON stdout reporter
│
├── src/NetTool.UI/             # WPF Application
│   ├── Models/                 # TestConfig, MetricsData
│   ├── ViewModels/             # MVVM (MainViewModel, RelayCommand)
│   ├── Services/               # EngineRunner, MetricsParser
│   ├── Views/                  # ConfigPanel, ChartPanel, LogPanel
│   └── Themes/Styles.xaml      # Dark theme
│
├── engine.exe                  # Built engine binary (gitignored)
├── sample_config.json          # Sample config cho testing
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

---

## 🔧 Thiết kế kỹ thuật

### Go Engine

- **Worker Pool**: Semaphore pattern (`chan struct{}`) giới hạn goroutine, tránh spawn vô hạn
- **HTTP Client**: 1 client duy nhất cho tất cả workers, connection pooling, keep-alive
- **Ramp-up**: Tăng dần concurrency trong `rampUpSec`, tránh spike đột ngột
- **Memory**: Reset metrics slice mỗi 1s flush, không append vô hạn
- **Graceful Shutdown**: Context cancellation + SIGINT/SIGTERM handling

### WPF UI

- **MVVM**: ViewModelBase + RelayCommand, data binding 2 chiều
- **Thread Safety**: `Dispatcher.BeginInvoke` cho tất cả UI updates từ engine events
- **Charting**: LiveCharts2 (SkiaSharp) — RPS chart + Latency chart (Avg/p95/p99)
- **Log**: Virtualized ListBox, capped 500 lines, auto-scroll
- **Dark Theme**: Custom styled controls (TextBox, ComboBox, Button, ListBox)

---

## 🗺️ Roadmap — Tính năng phát triển

### 🔜 Phase 2 — Enhanced UI
- [ ] **Export CSV/JSON** — xuất kết quả test ra file để phân tích
- [ ] **Test History** — lưu lại các lần test, so sánh kết quả
- [ ] **Compare Mode** — overlay chart 2 lần test để thấy regression
- [ ] **Alert Threshold** — cảnh báo khi p95/p99 vượt ngưỡng đặt trước
- [ ] **Request Templates** — lưu/load preset config thường dùng
- [ ] **Response Validation** — check body/status code response có đúng không

### 🔮 Phase 3 — Advanced Engine
- [ ] **HTTP/2 Support** — test API hỗ trợ HTTP/2
- [ ] **WebSocket Testing** — test WebSocket connections
- [ ] **Custom Scripts** — viết script test phức tạp (giống k6)
- [ ] **Variable Injection** — `{{random_id}}`, `{{timestamp}}` trong URL/body
- [ ] **Multi-endpoint** — test nhiều endpoint cùng lúc theo scenario
- [ ] **Rate Limiting** — giới hạn RPS thay vì chỉ concurrency
- [ ] **Certificate Pinning** — hỗ trợ mTLS / custom certificates

### 🚀 Phase 4 — Distributed & Integration
- [ ] **gRPC Control API** — điều khiển engine qua API thay vì process
- [ ] **WebSocket Realtime** — stream metrics qua WebSocket
- [ ] **Distributed Mode** — nhiều engine node chạy song song
- [ ] **CI/CD Integration** — CLI mode với exit code dựa trên threshold
- [ ] **Docker Support** — containerize engine để chạy trên cloud
- [ ] **Grafana Dashboard** — export metrics sang Prometheus/Grafana

### 💡 Ý tưởng mở rộng
- [ ] **DNS Metrics** — tách riêng DNS lookup time
- [ ] **TLS Handshake** — measure TLS negotiation time  
- [ ] **Connection Reuse Rate** — % connection được reuse
- [ ] **Bandwidth Throttle** — simulate slow network
- [ ] **Geographic Distribution** — test từ nhiều region
- [ ] **AI Analysis** — tự động phân tích bottleneck từ metrics

---

## ⚠️ Lưu ý quan trọng

> **⚠️ Tool này chỉ được sử dụng cho mục đích test hợp pháp:**
> - Test API của chính bạn hoặc có quyền test
> - Test trong môi trường staging/development
> - **KHÔNG** dùng để tấn công (DDoS) hệ thống bên ngoài

---

## 📄 License

MIT License — Xem [LICENSE](LICENSE) file.
