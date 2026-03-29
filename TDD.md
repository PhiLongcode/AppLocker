# AppLocker - Technical Design Document (TDD)

## 1. Overview

### 1.1 Mục tiêu

Xây dựng ứng dụng desktop (Windows) cho phép:

* Theo dõi các ứng dụng đang chạy trong hệ thống
* Chặn hoặc đóng ứng dụng theo rule cấu hình
* Giới hạn thời gian sử dụng ứng dụng
* Cho phép mở khóa bằng mật khẩu

---

### 1.2 Scope

#### MVP

* Block app theo danh sách toàn bộ ứng dụng trên máy tính
* Kill process
* Cấu hình bằng JSON

#### Nâng cao

* UI cấu hình (WPF)
* Time tracking
* Password unlock
* Auto start cùng Windows

---

## 2. Architecture

Áp dụng Clean Architecture:

```
Presentation (WPF)
    ↓
Application (Use Cases)
    ↓
Domain (Core)
    ↓
Infrastructure (External)
```

---

## 3. Solution Structure

```
AppLocker.sln
│
├── AppLocker.Domain
├── AppLocker.Application
├── AppLocker.Infrastructure
├── AppLocker.Presentation
└── AppLocker.Shared
```

---

## 4. Layer Design

### 4.1 Domain Layer

#### Entities

```csharp
public class AppRule
{
    public string ProcessName { get; set; }
    public RuleType Type { get; set; }
    public int? TimeLimitMinutes { get; set; }
}

public enum RuleType
{
    Block,
    LimitTime
}
```

#### Interfaces

```csharp
public interface IRuleEvaluator
{
    RuleResult Evaluate(ProcessInfo process);
}
```

---

### 4.2 Application Layer

#### Services

* MonitorService
* RuleEngineService
* EnforcementService
* UsageTrackingService

#### Flow

```
MonitorService
    ↓
RuleEngineService
    ↓
EnforcementService
```

#### Example

```csharp
public class MonitorService
{
    private readonly IProcessService _processService;
    private readonly IRuleEvaluator _ruleEvaluator;
    private readonly IEnforcementService _enforcement;

    public void Check()
    {
        var processes = _processService.GetRunningProcesses();

        foreach (var process in processes)
        {
            var result = _ruleEvaluator.Evaluate(process);

            if (result.ShouldBlock)
            {
                _enforcement.Kill(process.Name);
            }
        }
    }
}
```

---

### 4.3 Infrastructure Layer

#### Services

* ProcessService
* EnforcementService
* StorageService
* StartupService

#### ProcessService

```csharp
using System.Diagnostics;

public class ProcessService : IProcessService
{
    public IEnumerable<Process> GetRunningProcesses()
    {
        return Process.GetProcesses();
    }
}
```

#### EnforcementService

```csharp
public class EnforcementService : IEnforcementService
{
    public void Kill(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        foreach (var p in processes)
        {
            p.Kill();
        }
    }
}
```

---

### 4.4 Presentation Layer (WPF)

#### Components

* MainWindow
* SettingsView

#### ViewModels

* MainViewModel
* SettingsViewModel

#### Features

* Danh sách app bị chặn
* Toggle bật/tắt
* Set time limit
* Nhập password

---

## 5. Runtime Flow

```
App start
    ↓
Load config
    ↓
Start MonitorService
    ↓
Loop:
    Scan processes
    ↓
    Evaluate rule
    ↓
    Kill nếu vi phạm
    ↓
    Update usage
```

---

## 6. Background Worker

```csharp
public async Task StartAsync()
{
    while (true)
    {
        Check();
        await Task.Delay(2000);
    }
}
```

---

## 7. Storage Design

### JSON (MVP)

```json
{
  "rules": [
    {
      "processName": "chrome",
      "type": "LimitTime",
      "timeLimitMinutes": 120
    }
  ]
}
```

### Future

* SQLite để lưu usage history

---

## 8. Security & Anti-bypass

### Vấn đề

* User kill app
* Rename exe
* Dùng portable app

### Giải pháp

#### 1. Run as Administrator

#### 2. Watchdog Process

```
AppLocker.exe
   ↔ Watchdog.exe
```

#### 3. Check Full Path

```csharp
process.MainModule.FileName
```

#### 4. Hash Validation

* SHA256 file exe

---

## 9. Advanced Features

### 9.1 Real-time Detection

* Sử dụng WMI:

```csharp
ManagementEventWatcher
```

### 9.2 Overlay Lock Screen

* Fullscreen Window
* Disable interaction

### 9.3 Auto Start

Registry:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

### 9.4 Windows Service Mode

* Service chạy nền
* UI là client

---

## 10. Non-functional Requirements

| Yếu tố      | Mục tiêu   |
| ----------- | ---------- |
| Performance | < 5% CPU   |
| Memory      | < 100MB    |
| Stability   | 24/7       |
| Security    | Khó bypass |
| UX          | Đơn giản   |

---

## 11. Roadmap

### Phase 1

* Scan process
* Kill app
* JSON config

### Phase 2

* UI WPF
* Config rules

### Phase 3

* Time tracking
* Password

### Phase 4

* WMI real-time
* Watchdog

---

## 12. Future Ideas

* Parental Control App
* Focus Mode
* Cloud sync
* AI detect distraction apps

---

## 13. Notes

* Nên chạy với quyền Admin
* Test trên nhiều app khác nhau
* Tránh bị antivirus flag
