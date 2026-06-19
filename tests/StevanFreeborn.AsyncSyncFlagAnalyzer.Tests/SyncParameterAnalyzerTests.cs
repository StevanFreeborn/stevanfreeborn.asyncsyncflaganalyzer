using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using StevanFreeborn.AsyncSyncFlagAnalyzer.CodeFixes;

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
  StevanFreeborn.AsyncSyncFlagAnalyzer.SyncParameterAnalyzer,
  StevanFreeborn.AsyncSyncFlagAnalyzer.CodeFixes.SyncParameterCodeFixProvider,
  Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace StevanFreeborn.AsyncSyncFlagAnalyzer.Tests;

public class SyncParameterAnalyzerTests
{
  [Fact]
  public async Task WhenSyncIsPassedCorrectly_ItShouldNotReportDiagnostic()
  {
    var testData = await LoadTestDataAsync("WhenSyncIsPassedCorrectly");
    await VerifyCS.VerifyAnalyzerAsync(testData);
  }

  [Fact]
  public async Task WhenSyncIsPassedPositionally_ItShouldNotReportDiagnostic()
  {
    var testData = await LoadTestDataAsync("WhenSyncIsPassedPositionally");
    await VerifyCS.VerifyAnalyzerAsync(testData);
  }

  [Fact]
  public async Task WhenSyncIsPassedAsNamed_ItShouldNotReportDiagnostic()
  {
    var testData = await LoadTestDataAsync("WhenSyncIsPassedAsNamed");
    await VerifyCS.VerifyAnalyzerAsync(testData);
  }

  [Fact]
  public async Task WhenSyncIsOmitted_ItShouldReportDiagnosticAndApplyCodeFix()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetCoreAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsOmitted");
    var fixedData = await LoadTestDataAsync("WhenSyncIsOmitted_Fixed");

    await VerifyCS.VerifyCodeFixAsync(testData, expectedDiagnostic, fixedData);
  }

  [Fact]
  public async Task WhenSyncIsHardcodedFalse_ItShouldReportDiagnostic()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetCoreAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsHardcodedFalse");

    await VerifyCS.VerifyAnalyzerAsync(testData, expectedDiagnostic);
  }

  [Fact]
  public async Task WhenSyncIsHardcodedFalse_ItShouldApplyCodeFix()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetCoreAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsHardcodedFalse");
    var fixedData = await LoadTestDataAsync("WhenSyncIsHardcodedFalse_Fixed");

    await VerifyCS.VerifyCodeFixAsync(testData, expectedDiagnostic, fixedData);
  }

  [Fact]
  public async Task WhenSyncIsPassedAsNamedHardcoded_ItShouldReplaceNamedArgument()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetCoreAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsPassedAsNamedHardcoded");
    var fixedData = await LoadTestDataAsync("WhenSyncIsPassedAsNamedHardcoded_Fixed");

    await VerifyCS.VerifyCodeFixAsync(testData, expectedDiagnostic, fixedData);
  }

  [Fact]
  public async Task WhenOuterMethodHasNoSyncParam_ItShouldNotReportDiagnostic()
  {
    var testData = await LoadTestDataAsync("WhenOuterMethodHasNoSyncParam");
    await VerifyCS.VerifyAnalyzerAsync(testData);
  }

  [Fact]
  public async Task WhenTargetHasNoSyncParam_ItShouldNotReportDiagnostic()
  {
    var testData = await LoadTestDataAsync("WhenTargetHasNoSyncParam");
    await VerifyCS.VerifyAnalyzerAsync(testData);
  }

  [Fact]
  public async Task WhenSyncIsOmitted_CustomName_ItShouldReportDiagnosticAndApplyCodeFix()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "runSynchronously", "GetMoreDataAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsOmitted_CustomName");
    var fixedData = await LoadTestDataAsync("WhenSyncIsOmitted_CustomName_Fixed");

    var test = new CSharpCodeFixTest<SyncParameterAnalyzer, SyncParameterCodeFixProvider, DefaultVerifier>
    {
      TestCode = testData,
      FixedCode = fixedData
    };
    test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", """
root = true

[*.cs]
dotnet_diagnostic.SYNC001.additional_sync_names = runSynchronously
"""));
    test.ExpectedDiagnostics.Add(expectedDiagnostic);

    await test.RunAsync();
  }

  [Fact]
  public async Task WhenSyncIsOmitted_MultipleAwaits_OnlyOneShouldReportDiagnostic()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetMoreDataAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsOmitted_MultipleAwaits");
    var fixedData = await LoadTestDataAsync("WhenSyncIsOmitted_MultipleAwaits_Fixed");

    await VerifyCS.VerifyCodeFixAsync(testData, expectedDiagnostic, fixedData);
  }

  [Fact]
  public async Task WhenSyncIsNotLastTargetParameter_ItShouldReportDiagnosticAndApplyCodeFix()
  {
    var expectedDiagnostic = VerifyCS.Diagnostic("SYNC001")
      .WithLocation(0)
      .WithArguments("GetFrobCoreAsync", "sync", "GetDataAsync");

    var testData = await LoadTestDataAsync("WhenSyncIsNotLastTargetParameter");
    var fixedData = await LoadTestDataAsync("WhenSyncIsNotLastTargetParameter_Fixed");

    await VerifyCS.VerifyCodeFixAsync(testData, expectedDiagnostic, fixedData);
  }

  private static async Task<string> LoadTestDataAsync(string name)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "TestData", $"{name}.cs");
    var text = await File.ReadAllTextAsync(path);
    return text;
  }
}