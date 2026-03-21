---
description: Build Go Engine và chạy WPF UI
---

## Build & Run NetTool

### 1. Build Go Engine
// turbo
```bash
cd e:\15. Other\NetTool\engine && go build -o ../engine.exe .
```

### 2. Chạy WPF UI
// turbo
```bash
dotnet run --project e:\15. Other\NetTool\src\NetTool.UI
```

### 3. Build cả hai (nếu Go modified)
// turbo
```bash
cd e:\15. Other\NetTool\engine && go build -o ../engine.exe . && cd .. && dotnet run --project src/NetTool.UI
```
