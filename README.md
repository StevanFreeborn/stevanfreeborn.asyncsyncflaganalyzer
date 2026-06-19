# StevanFreeborn.AsyncSyncFlagAnalyzer

[![NuGet](https://img.shields.io/nuget/v/StevanFreeborn.AsyncSyncFlagAnalyzer.svg)](https://www.nuget.org/packages/StevanFreeborn.AsyncSyncFlagAnalyzer)
[![NuGet](https://img.shields.io/nuget/dt/StevanFreeborn.AsyncSyncFlagAnalyzer.svg)](https://www.nuget.org/packages/StevanFreeborn.AsyncSyncFlagAnalyzer)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A Roslyn analyzer and code fix that enforces the **Flag Argument Hack** pattern — forwarding a `sync` boolean parameter through optionally-asynchronous call chains instead of hardcoding `false` or omitting the argument.

## The Problem

Consider a service interface with an optional async path:

```csharp
interface IDataService
{
  Task<string> GetDataAsync(bool sync = false);
}
```

Callers in the same async infrastructure should forward their own `sync` flag:

```csharp
// ✅ Good: signal is propagated
Task<string> Good_ForwardSync(bool sync) => _dataService.GetDataAsync(sync);

// ❌ Bad: sync signal is lost
Task<string> Bad_HardcodedValue(bool sync) => _dataService.GetDataAsync(false);

// ❌ Bad: relies on the default (false), sync signal is lost
Task<string> Bad_OmittedFlag(bool sync) => _dataService.GetDataAsync();
```

This analyzer flags every case where the sync parameter is dropped, and provides a code fix to forward it automatically.

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
  Version="0.0.0"
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
