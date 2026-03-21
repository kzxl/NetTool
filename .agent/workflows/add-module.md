---
description: Quy trình thêm một phân hệ (module) mới cho NetTool
---

## Thêm Module Mới

Khi thêm 1 phân hệ mới (ví dụ: Ping, DNS, Port Scan...), follow đúng quy trình sau:

### 1. Go Engine — Thêm package mới

1. Tạo folder `engine/<module_name>/` (ví dụ: `engine/ping/`)
2. Implement main logic trong file `engine/<module_name>/<module_name>.go`
3. Struct output **PHẢI** serialize JSON tương thích stdout streaming
4. Nếu cần config riêng, thêm vào `engine/config/config.go`
5. Kết nối vào `engine/main.go` qua sub-command hoặc mode parameter

### 2. WPF UI — Thêm ViewModel + View

1. **Model**: Tạo `Models/<Module>Config.cs` và `Models/<Module>Result.cs`
2. **ViewModel**: Tạo `ViewModels/<Module>ViewModel.cs`
   - Kế thừa `ToolViewModelBase` (hoặc `ViewModelBase` nếu chưa có base)
   - Implement `Start()`, `Stop()`, `OnEngineOutput()` methods
   - Expose `RelayCommand` cho Start/Stop/Export
3. **View**: Tạo `Views/<Module>Panel.xaml` + `.xaml.cs`
   - Follow dark theme (sử dụng StaticResource từ `Themes/Styles.xaml`)
   - Có Log output area
4. **MainWindow**: Thêm tab mới trong TabControl
5. **EngineRunner**: Nếu cần mode khác, truyền argument tương ứng

### 3. Quy tắc đặt tên

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Go package | lowercase | `engine/ping` |
| Go struct | PascalCase | `PingResult` |
| Go func | PascalCase | `RunPing()` |
| C# class | PascalCase | `PingViewModel` |
| C# method | PascalCase | `StartPing()` |
| XAML name | PascalCase | `PingPanel` |
| JSON field | camelCase | `targetHost` |

### 4. Checklist trước PR

- [ ] Go build pass: `cd engine && go build .`
- [ ] Dotnet build pass: `dotnet build src/NetTool.UI`
- [ ] UI hiển thị tab mới đúng dark theme
- [ ] Engine command chạy standalone với sample config
- [ ] Kết quả stream đúng JSON format qua stdout
- [ ] Export CSV/JSON hoạt động (nếu applicable)
- [ ] README.md updated
