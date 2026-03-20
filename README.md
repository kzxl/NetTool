# NetTool - API Performance Tester

Tool test hiệu suất API gồm **WPF UI** (C# .NET 8) và **Go Engine** (CLI).

## 🏗️ Kiến trúc

```
[WPF UI] → (config.json) → [Go Engine CLI] → (stdout JSON) → [WPF đọc + chart realtime]
```

## 📦 Cấu trúc

```
NetTool/
├── engine/          # Go engine source
├── engine.exe       # Built binary
├── src/NetTool.UI/  # WPF application
└── sample_config.json
```

## 🚀 Build & Run

### Build Go Engine
```bash
cd engine
go build -o ../engine.exe .
```

### Build & Run UI
```bash
dotnet run --project src/NetTool.UI
```

### Test Engine Standalone
```bash
./engine.exe sample_config.json
```

## 📊 Metrics

| Metric | Mô tả |
|--------|--------|
| RPS | Số request/giây |
| Avg | Trung bình response time (ms) |
| p50 | Median |
| p95 | 95th percentile |
| p99 | 99th percentile |
| ErrorRate | % lỗi |

## ⚙️ Config Schema

```json
{
  "request": {
    "url": "https://example.com/api",
    "method": "GET",
    "headers": { "Authorization": "Bearer token" },
    "body": ""
  },
  "load": {
    "concurrency": 100,
    "durationSec": 60,
    "rampUpSec": 10
  },
  "timeoutMs": 5000
}
```

## ⚠️ Lưu ý

- Chỉ dùng cho môi trường test / API có quyền
- Không dùng vào mục đích tấn công hệ thống
