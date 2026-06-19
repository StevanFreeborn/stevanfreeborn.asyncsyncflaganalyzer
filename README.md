# StevanFreeborn.AsyncSyncFlagAnalyzer

[![NuGet](https://img.shields.io/nuget/v/StevanFreeborn.AsyncSyncFlagAnalyzer.svg)](https://www.nuget.org/packages/StevanFreeborn.AsyncSyncFlagAnalyzer)
[![NuGet](https://img.shields.io/nuget/dt/StevanFreeborn.AsyncSyncFlagAnalyzer.svg)](https://www.nuget.org/packages/StevanFreeborn.AsyncSyncFlagAnalyzer)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Roslyn analyzer and code fix that enforces the **Flag Argument Hack** pattern — forwarding a `sync` boolean parameter through optionally-asynchronous call chains instead of hardcoding `false` or omitting the argument.

## The Flag Argument Hack

You are working in a legacy codebase that has existing synchronous APIs and you need to start introducing asynchronous APIs. You want to avoid code duplication and keep the logic in one place, but you also need to maintain synchronous wrappers for backward compatibility. A pattern that can be introduced to solve this problem is the **Flag Argument Hack**. It's an effective way to implement both sync and async versions of a method without duplicating code. It is described in detail in [this article](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development#the-flag-argument-hack) by Stephen Cleary who credits the idea to Stephen Toub.

The core idea: a private `CoreAsync` method takes a `bool sync` flag. When `sync` is `true`, the method runs synchronously and returns an already-completed task. When `sync` is `false`, it runs asynchronously. Two public wrappers — one synchronous, one asynchronous — call `CoreAsync` with `true` or `false`:

```csharp
public interface IDataService
{
  string Get(int id);
  Task<string> GetAsync(int id);
}

public sealed class WebDataService : IDataService
{
  private async Task<string> GetCoreAsync(int id, bool sync)
  {
    using var client = new WebClient();
    
    return sync
      ? client.DownloadString("https://example.com/api/values/" + id)
      : await client.DownloadStringTaskAsync("https://example.com/api/values/" + id);
  }

  public string Get(int id) => GetCoreAsync(id, sync: true).GetAwaiter().GetResult();

  public Task<string> GetAsync(int id) => GetCoreAsync(id, sync: false);
}
```

Because the task is already completed when `sync` is `true`, calling `.GetAwaiter().GetResult()` on it cannot deadlock — it simply retrieves the already-computed value.

Business logic follows the same pattern:

```csharp
public sealed class BusinessLogic
{
  private readonly IDataService _dataService;

  public BusinessLogic(IDataService dataService) => _dataService = dataService;

  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    var result = sync
      ? _dataService.Get(17)
      : await _dataService.GetAsync(17);

    if (result != string.Empty)
      return result;

    return sync
      ? _dataService.Get(13)
      : await _dataService.GetAsync(13);
  }

  public string GetFrob() => GetFrobCoreAsync(sync: true).GetAwaiter().GetResult();

  public Task<string> GetFrobAsync() => GetFrobCoreAsync(sync: false);
}
```

The logic stays essentially the same — it just calls different APIs based on the flag. This works well when there's a one-to-one correspondence between synchronous and asynchronous APIs, which is usually the case.

## The Forwarding Problem

When the `CoreAsync` method is exposed from the service layer, business logic can call it directly via `await`. Every call in the chain must forward its own `sync` flag to the next:

```csharp
public interface IDataService
{
  Task<string> GetCoreAsync(int id, bool sync);
}

public sealed class BusinessLogic
{
  private readonly IDataService _dataService;

  public BusinessLogic(IDataService dataService) => _dataService = dataService;

  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    return await _dataService.GetCoreAsync(17, sync);
  }

  public string GetFrob() => GetFrobCoreAsync(sync: true).GetAwaiter().GetResult();

  public Task<string> GetFrobAsync() => GetFrobCoreAsync(sync: false);
}
```

The bug this analyzer catches is simple but subtle: forgetting to forward the flag.

```csharp
// ❌ Bad: hardcodes false, sync signal is lost
Task<string> Bad_HardcodedValue(bool sync) => _dataService.GetCoreAsync(17, false);
```

When a developer accidentally drops the `sync` argument, the `GetCoreAsync` method silently defaults to asynchronous behavior — even when the caller asked for synchronous execution. This can cause deadlocks in synchronous wrappers or simply produce incorrect behavior depending on the implementation.

The analyzer flags every invocation where the sync parameter is dropped and provides a code fix to forward it automatically.

## How It Works

The analyzer registers a `SyntaxNodeAction` on `AwaitExpression` nodes:

1. Finds the enclosing method and checks if it has a parameter matching a known sync name
2. If yes, resolves the invoked method's symbol and checks whether it also has a sync parameter
3. Walks the invocation's arguments — if none of them reference the enclosing method's sync parameter by name, reports SYNC001
4. Passes the enclosing parameter name, target parameter name, and target ordinal via `Diagnostic.Properties` so the code fix knows what variable to pass and where to place it

The code fix reads `EnclosingSyncName`, `TargetSyncName`, and `TargetSyncOrdinal` from the diagnostic properties. It first checks whether a named argument already exists for the target parameter — if so, it replaces only the expression while preserving the name colon. Otherwise, it counts existing positional arguments and inserts the sync flag at the correct ordinal position, replacing any positional argument at that slot.

## Diagnostic Rules

| ID          | Severity | Description                                     |
|-------------|----------|-------------------------------------------------|
| **SYNC001** | Error    | Missing sync parameter in optionally async call |

> The severity is configurable via `.editorconfig` if error-level is too strict:
>
> `dotnet_diagnostic.SYNC001.severity = warning`

## Installation

Add the NuGet package to projects that need the analyzer:

```xml
<PackageReference 
  Include="StevanFreeborn.AsyncSyncFlagAnalyzer"
  Version="x.x.x"
  ReferenceOutputAssembly="false"
  OutputItemType="Analyzer" 
/>
```

The analyzer and code fix are bundled together in the same package.

## Configuration

By default, the analyzer recognizes `sync` as the parameter name. To define additional names, add an entry to your `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.SYNC001.additional_sync_names = runSynchronously, isSync
```

All names are matched case-insensitively. The default name `sync` is always included and does not need to be listed.

## Code Fix

The analyzer ships with a code fix provider (registered as SYNC001). When the lightbulb appears, it offers **"Pass 'sync' parameter"** which inserts the sync flag at the correct position in the argument list.

The fix uses the enclosing method's parameter name as the argument value (the variable in scope) and the target method's parameter name for named argument matching. It handles three scenarios:

- **Sync fully omitted:** the argument is inserted as a positional argument at the correct ordinal position
  - `GetDataAsync(17)` → `GetDataAsync(17, sync)`
- **Sync passed positionally with a hardcoded value:** the existing argument is replaced in-place
  - `GetDataAsync(17, false)` → `GetDataAsync(17, sync)`
- **Sync passed as a named argument with a hardcoded value:** the expression is replaced while preserving the name colon
  - `GetDataAsync(17, sync: false)` → `GetDataAsync(17, sync: sync)`

## Demo

The demo project at `samples/StevanFreeborn.AsyncSyncFlagAnalyzer.Demo` exercises the analyzer with both good and bad patterns. It also configures custom sync names (`runSynchronously`, `isSync`) via its `.editorconfig` to demonstrate the override feature.

To see the analyzer in action within an IDE, open and build the demo project — SYNC001 violations will appear on the `Bad_` methods.
