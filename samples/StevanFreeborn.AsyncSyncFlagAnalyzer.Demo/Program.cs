internal static class Program
{
  public static async Task Main()
  {
    await Task.CompletedTask;
  }
}

internal interface IDataService
{
  Task<string> GetDataAsync(string name, bool sync = false);
  Task<string> GetMoreDataAsync(bool runSynchronously, string name);
  Task<string> FetchAsync(int id, bool sync = false, bool cache = true);
}

internal class BusinessLogic(IDataService dataService)
{
  private readonly IDataService _dataService = dataService;

  // ── Good patterns (no SYNC001) ──────────────────────────

  /// <summary>Passes the default 'sync' variable positionally.</summary>
  public async Task<string> Good_ForwardSync(bool sync)
  {
    return await _dataService.GetDataAsync("Stevan", sync);
  }

  /// <summary>Passes a custom 'runSynchronously' variable positionally.</summary>
  public async Task<string> Good_ForwardCustomFlag(bool runSynchronously)
  {
    return await _dataService.GetMoreDataAsync(runSynchronously, "Freeborn");
  }

  // ── Bad patterns (SYNC001, demonstrating code fix) ──────

  /// <summary>Omits the sync argument entirely.
  /// Code fix: inserts `sync` at ordinal 1.</summary>
  public async Task<string> Bad_OmittedSync(bool sync)
  {
    return await _dataService.GetDataAsync("Stevan");
  }

  /// <summary>Hardcodes `false` as a positional argument.
  /// Code fix: replaces arg at ordinal 1 with `sync`.</summary>
  public async Task<string> Bad_HardcodedValue(bool sync)
  {
    return await _dataService.GetDataAsync("Stevan", false);
  }

  /// <summary>Hardcodes a value using a named argument.
  /// Code fix: replaces expression in the named arg, keeping `sync:`.</summary>
  public async Task<string> Bad_NamedHardcoded(bool sync)
  {
    return await _dataService.GetDataAsync("Stevan", sync: false);
  }

  /// <summary>Omits sync when it's not the last parameter.
  /// Code fix: inserts `sync` at ordinal 1 (before `cache`).</summary>
  public async Task<string> Bad_OmittedSyncNotLast(bool sync)
  {
    return await _dataService.FetchAsync(17);
  }

  /// <summary>Hardcodes `false` at ordinal 0 where the target expects `runSynchronously`.
  /// Code fix: replaces arg at ordinal 0 with `sync` (value = enclosing name).</summary>
  public async Task<string> Bad_HardcodedAtOrdinalZero(bool sync)
  {
    return await _dataService.GetMoreDataAsync(false, "Freeborn");
  }
}