using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using StevanFreeborn.AsyncSyncFlagAnalyzer.Common;

namespace StevanFreeborn.AsyncSyncFlagAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SyncParameterAnalyzer : DiagnosticAnalyzer
{
  private const string Title = "Missing sync parameter in optionally async call";
  private const string MessageFormat = "The method '{0}' must pass the '{1}' parameter to '{2}'";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticProperties.DiagnosticId,
    Title,
    MessageFormat,
    "Architecture",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    if (context is null)
    {
      return;
    }

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
  }

  private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
  {
    var awaitExpr = (AwaitExpressionSyntax)context.Node;

    var enclosingMethod = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();

    if (enclosingMethod is null)
    {
      return;
    }

    var validSyncNames = GetValidSyncNames(context);

    var enclosingSyncParam = enclosingMethod.ParameterList.Parameters
      .FirstOrDefault(p => validSyncNames.Contains(p.Identifier.Text));

    if (enclosingSyncParam is null)
    {
      return;
    }

    var expectedSyncName = enclosingSyncParam.Identifier.Text;

    if (awaitExpr.Expression is not InvocationExpressionSyntax invocation)
    {
      return;
    }


    if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
    {
      return;
    }

    var targetSyncParam = methodSymbol.Parameters.FirstOrDefault(p => validSyncNames.Contains(p.Name));

    if (targetSyncParam is null)
    {
      return;
    }

    var passedSync = false;

    foreach (var argument in invocation.ArgumentList.Arguments)
    {
      if (argument.Expression is IdentifierNameSyntax id && id.Identifier.Text == expectedSyncName)
      {
        passedSync = true;
        break;
      }
    }

    if (!passedSync)
    {
      var properties = ImmutableDictionary<string, string?>.Empty
        .Add(DiagnosticProperties.EnclosingSyncName, enclosingSyncParam.Identifier.Text)
        .Add(DiagnosticProperties.TargetSyncName, targetSyncParam.Name)
        .Add(DiagnosticProperties.TargetSyncOrdinal, targetSyncParam.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));

      var diagnostic = Diagnostic.Create(
        Rule,
        invocation.GetLocation(),
        properties,
        enclosingMethod.Identifier.Text,
        expectedSyncName,
        methodSymbol.Name
      );

      context.ReportDiagnostic(diagnostic);
    }
  }

  private static HashSet<string> GetValidSyncNames(SyntaxNodeAnalysisContext context)
  {
    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { DiagnosticProperties.DefaultSyncName };
    var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);

    if (options.TryGetValue(DiagnosticProperties.EditorConfigKey, out var customNames) && !string.IsNullOrWhiteSpace(customNames))
    {
      var splitNames = customNames.Split([','], StringSplitOptions.RemoveEmptyEntries);

      foreach (var name in splitNames)
      {
        _ = names.Add(name.Trim());
      }
    }

    return names;
  }
}