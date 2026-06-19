namespace StevanFreeborn.AsyncSyncFlagAnalyzer.Common;

public static class DiagnosticProperties
{
  public const string DiagnosticId = "SYNC001";
  public const string DefaultSyncName = "sync";
  public const string EditorConfigKey = "dotnet_diagnostic.SYNC001.additional_sync_names";
  public const string EnclosingSyncName = "EnclosingSyncName";
  public const string TargetSyncName = "TargetSyncName";
  public const string TargetSyncOrdinal = "TargetSyncOrdinal";
}