# AppLocker

Ứng dụng desktop Windows giúp giám sát và giới hạn ứng dụng theo rule, hỗ trợ chạy nền với tray icon, khóa/mở bằng mật khẩu, và lưu dữ liệu bằng SQLite.

## Mục tiêu ứng dụng

- Giám sát các process đang chạy theo chu kỳ.
- Chặn app theo `processName` hoặc giới hạn thời gian sử dụng.
- Yêu cầu mật khẩu để mở khóa app bị chặn.
- Chạy nền (`--background`) và điều khiển từ system tray.
- Tự động khởi động cùng Windows (qua Registry HKCU Run).

## Công nghệ sử dụng

- Nền tảng: `.NET 8` (`net8.0-windows`)
- UI: `WPF` + `Windows Forms NotifyIcon` cho tray
- Kiến trúc: `Clean Architecture`
- Lưu trữ: `SQLite` (`Microsoft.Data.Sqlite`)
- Tích hợp hệ thống:
  - `System.Diagnostics` (đọc process)
  - `Microsoft.Win32` (startup Registry)
  - `System.Management` (mở rộng cho trường hợp cần quản lý hệ thống)
- Test: `xUnit` trong project `tests/AppLocker.Tests`

## Cấu trúc solution

```text
AppLocker.sln
|-- src/AppLocker.Presentation   # WPF UI, startup app, tray, ViewModels
|-- src/AppLocker.Application    # Use cases/services (monitor, password, rule engine)
|-- src/AppLocker.Domain         # Entities, models, interfaces cốt lõi
|-- src/AppLocker.Infrastructure # SQLite, process/enforcement, startup, IPC
|-- src/AppLocker.Shared         # Thành phần dùng chung
|-- src/AppLocker.Service        # Worker service (mở rộng chạy nền dạng service)
|-- src/AppLocker.Watchdog       # Watchdog process (mở rộng anti-bypass)
`-- tests/AppLocker.Tests        # Unit tests
```

## Kiến trúc (Clean Architecture)

```text
Presentation (WPF)
    -> Application (Use Cases / Business Flow)
        -> Domain (Core Rules, Entities, Interfaces)
            <- Infrastructure (SQLite, Process, Registry, IPC)
```

Nguyên tắc:

- `Domain` không phụ thuộc layer bên ngoài.
- `Application` điều phối logic nghiệp vụ thông qua abstractions.
- `Infrastructure` cung cấp implement cụ thể cho I/O, process, storage.
- `Presentation` chỉ xử lý UI và thao tác người dùng.

## Luồng hoạt động chính

1. App khởi động, tải `SQLite` DB và trạng thái mật khẩu.
2. Người dùng bật giám sát hoặc khởi động với `--background`.
3. `MonitorService` quét process theo interval.
4. `RuleEvaluator` đánh giá process theo rule.
5. Nếu vi phạm, `EnforcementService` kill process; trường hợp cần mở khóa thì hiện form nhập mật khẩu.
6. Nếu mở khóa thành công, app được cho phép mở lại tạm thời.

## Dữ liệu runtime (AppData)

Dữ liệu không còn lưu cạnh file `.exe` khi deploy. Hiện tại được lưu tại:

- `%LOCALAPPDATA%\AppLocker\applocker.db`

Ví dụ trên máy user:

- `C:\Users\<UserName>\AppData\Local\AppLocker\applocker.db`

## Hướng dẫn chạy local

Yêu cầu:

- Windows 10/11
- .NET SDK 8

Lệnh cơ bản:

```powershell
dotnet restore
dotnet build AppLocker.sln -c Debug
dotnet run --project src/AppLocker.Presentation/AppLocker.Presentation.csproj
```

Chạy nền:

```powershell
dotnet run --project src/AppLocker.Presentation/AppLocker.Presentation.csproj -- --background
```

## Build và publish exe

Build Release:

```powershell
dotnet build src/AppLocker.Presentation/AppLocker.Presentation.csproj -c Release
```

Publish nhanh ra Desktop (có script sẵn):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-ToDesktop.ps1
```

Script sẽ:

- Publish app vào `Desktop\App Locker`
- Tạo 2 shortcut:
  - `AppLocker.lnk` (UI đầy đủ)
  - `AppLocker (background).lnk` (chạy nền + giám sát)
- In ra đường dẫn dữ liệu AppData.

## Test

Chạy toàn bộ test:

```powershell
dotnet test AppLocker.sln
```

Một số nhóm test đã có:

- Rule evaluator logic
- Password service
- Usage tracking
- IPC (Named Pipe)

## Định hướng mở rộng

- Đóng gói `AppLocker.Service` thành Windows Service thực tế.
- Nâng cấp watchdog để tăng khả năng anti-bypass.
- Bổ sung telemetry, logging, và hardening bảo mật.
