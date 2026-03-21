---
description: Publish NetTool thành portable package (2 options)
---

## Publish NetTool

Có 2 chế độ publish:

### Option 1: 🟢 Full (Self-Contained) — Copy & Run

> Bản đầy đủ, không cần cài .NET Runtime. File lớn (~60-80MB) nhưng chạy ngay.

#### 1a. Build Go Engine (release, stripped)
// turbo
```bash
cd e:\15. Other\NetTool\engine && go build -ldflags="-s -w" -o ../publish/full/engine.exe .
```

#### 1b. Publish WPF UI (self-contained, single file)
// turbo
```bash
dotnet publish e:\15. Other\NetTool\src\NetTool.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o e:\15. Other\NetTool\publish\full
```

#### 1c. Verify
// turbo
```bash
ls e:\15. Other\NetTool\publish\full
```

Kết quả: thư mục `publish/full/` chứa `NetTool.UI.exe` + `engine.exe`, copy cả folder là chạy.

---

### Option 2: 🔵 Lightweight (Framework-Dependent) — Yêu cầu .NET Runtime

> Bản nhẹ (~5-10MB), yêu cầu người dùng cài [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

#### 2a. Build Go Engine (release, stripped)
// turbo
```bash
cd e:\15. Other\NetTool\engine && go build -ldflags="-s -w" -o ../publish/lite/engine.exe .
```

#### 2b. Publish WPF UI (framework-dependent, single file)
// turbo
```bash
dotnet publish e:\15. Other\NetTool\src\NetTool.UI -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o e:\15. Other\NetTool\publish\lite
```

#### 2c. Verify
// turbo
```bash
ls e:\15. Other\NetTool\publish\lite
```

Kết quả: thư mục `publish/lite/` với file nhỏ hơn nhiều. Chạy cần .NET 8 Desktop Runtime.

---

### So sánh 2 options

| | Full (Self-Contained) | Lite (Framework-Dependent) |
|---|---|---|
| **Kích thước** | ~60-80 MB | ~5-10 MB |
| **Yêu cầu runtime** | Không | .NET 8 Desktop Runtime |
| **Dùng khi** | Gửi cho người khác, USB, portable | Team đã có .NET, CI/CD |
| **Ưu điểm** | Copy & chạy ngay | File nhỏ, tải nhanh |
| **Nhược điểm** | File lớn | Phải cài runtime riêng |
