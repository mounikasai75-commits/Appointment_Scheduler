# Requirements

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (verified with 8.0.426)
- macOS, Linux or Windows
- Git

No database or external services are needed. Data is held in memory.

## NuGet packages

Restored automatically by `dotnet restore` (run implicitly by `dotnet build`).

### API (`src/AppointmentScheduler.Api`)

| Package | Version |
|---------|---------|
| Microsoft.AspNetCore.OpenApi | 8.0.32 |
| Swashbuckle.AspNetCore | 6.6.2 |

### Tests (`tests/AppointmentScheduler.Api.Tests`)

| Package | Version |
|---------|---------|
| Microsoft.NET.Test.Sdk | 17.8.0 |
| xunit | 2.5.3 |
| xunit.runner.visualstudio | 2.5.3 |
| coverlet.collector | 6.0.0 |

## Install and verify

```sh
dotnet --version
dotnet restore AppointmentScheduler.sln
dotnet build AppointmentScheduler.sln
dotnet test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj
```
