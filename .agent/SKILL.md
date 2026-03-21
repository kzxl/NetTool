---
name: NetTool Development Standards
description: Coding standards, conventions, and architecture rules for NetTool project
---

# NetTool Development Standards

## 🏗️ Architecture Overview

NetTool sử dụng kiến trúc **Core + Plugin** (PoE-inspired):
- **Core**: Shared infrastructure (theme, engine runner, logging, export, base classes)
- **Modules**: Mỗi tool là 1 module tự chứa, lắp vào core qua `ITool` interface
- **Engine**: Go CLI multi-command, mỗi module = 1 command

```
engine.exe <command> <config.json>
UI → EngineRunner.Start(enginePath, command, configPath) → parse stdout JSON lines
```

## 📐 Quy Tắc Kiến Trúc

### Module Independence (Không Xung Đột)
1. **Mỗi module = 1 folder riêng** trong `Modules/` (UI) và `cmd/` (Engine)
2. **Không import chéo** giữa modules — Module A không bao giờ reference Module B
3. **Shared code** chỉ ở `Core/` (UI) hoặc `shared/` (Engine)
4. **Đăng ký module** qua `ToolRegistry` — thêm module mới chỉ cần 1 dòng

### Layer Rules
| Layer | Allowed Dependencies |
|-------|---------------------|
| `Core/` | .NET framework, LiveCharts2, ViewModels/ViewModelBase |
| `Modules/X/` | `Core/`, `Services/`, `Themes/`, `Converters/` — KHÔNG module khác |
| `Services/` | .NET framework — KHÔNG reference ViewModels hoặc Views |
| `engine/cmd/X` | `engine/shared/` — KHÔNG reference cmd khác |
| `engine/shared/` | Go stdlib only |

## 📝 Naming Conventions

### C# (.NET)
| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Namespace | PascalCase, theo folder | `NetTool.UI.Modules.Ping` |
| Class | PascalCase + suffix | `PingViewModel`, `PingPanel`, `PingConfig` |
| Interface | I + PascalCase | `ITool`, `IExportable` |
| Public method | PascalCase | `StartPing()`, `ParseResult()` |
| Private field | _camelCase | `_isRunning`, `_metricsHistory` |
| Property | PascalCase | `IsRunning`, `StatusText` |
| Command | PascalCase + "Command" | `StartCommand`, `ExportCsvCommand` |
| Event | On + PascalCase | `OnStdOutReceived`, `OnExited` |
| XAML controls | PascalCase, descriptive | `TargetHostTextBox`, `ResultsDataGrid` |

### Go (Engine)
| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Package | lowercase, single word | `ping`, `dns`, `shared` |
| Exported func | PascalCase | `RunPing()`, `LoadConfig()` |
| Unexported func | camelCase | `doRequest()`, `parseResult()` |
| Struct | PascalCase | `PingResult`, `DnsConfig` |
| JSON field tag | camelCase | `json:"targetHost"` |
| Constants | PascalCase (exported) | `DefaultTimeout` |

### JSON Config
- Field names: **camelCase**
- Mode/command names: **lowercase** (`loadtest`, `ping`, `dns`, `portscan`)

## 🎨 UI Standards

### Dark Theme
- **PHẢI** sử dụng `StaticResource` từ `Themes/Styles.xaml`
- Backgrounds: `BgDarkBrush`, `BgMediumBrush`, `BgLightBrush`
- Text: `TextPrimaryBrush`, `TextSecondaryBrush`
- Accents: `AccentBlueBrush`, `AccentGreenBrush`
- Borders: `BorderBrush`

### Panel Layout Pattern
Mỗi tool panel nên follow layout:
```xml
<UserControl>
    <DockPanel>
        <!-- Top: Config area -->
        <ScrollViewer DockPanel.Dock="Left" Width="340">
            <!-- Input fields, buttons -->
        </ScrollViewer>
        
        <!-- Right: Results area -->
        <Grid>
            <!-- Charts / DataGrid / Results -->
            <!-- Log area (bottom) -->
        </Grid>
    </DockPanel>
</UserControl>
```

### MVVM Strict Rules
1. **ViewModel không biết View** — chỉ expose properties + commands
2. **View không có logic** — code-behind chỉ để khởi tạo hoặc UI-only tasks
3. **Data binding ONLY** — không trực tiếp set UI properties từ code-behind
4. **RelayCommand** cho mọi user action

## 🔧 Engine Output Protocol

Mọi Go engine command **PHẢI** output qua:
- **stdout**: JSON lines (1 JSON object per line) — data cho UI parse
- **stderr**: Human-readable log messages — hiển thị trong log panel

### JSON Output Contract
```json
{
    "type": "result|progress|error|complete",
    "data": { ... }
}
```

Mỗi module define schema `data` riêng, nhưng `type` field là bắt buộc.

## ✅ Pre-Commit Checklist

Trước khi commit bất kỳ thay đổi nào:
1. `cd engine && go build .` → PASS
2. `dotnet build src/NetTool.UI` → PASS
3. UI chạy, tab mới hiển thị đúng dark theme
4. Engine command chạy standalone
5. Không break API/interface cũ
6. README.md updated nếu thêm feature mới

## 📁 File Organization

```
Khi thêm module mới, tạo đúng 3-4 files:
  
UI:
  src/NetTool.UI/Modules/<Name>/
  ├── <Name>ViewModel.cs    # Kế thừa ToolViewModelBase
  ├── <Name>Panel.xaml       # UserControl view
  ├── <Name>Panel.xaml.cs    # Minimal code-behind  
  └── <Name>Models.cs        # Config + result models

Engine:
  engine/cmd/<name>.go       # Command handler
  engine/<name>/             # Package nếu logic phức tạp
  └── <name>.go
```
