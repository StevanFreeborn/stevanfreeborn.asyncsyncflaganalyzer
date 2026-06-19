using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using StevanFreeborn.AsyncSyncFlagAnalyzer.Common;

namespace StevanFreeborn.AsyncSyncFlagAnalyzer.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SyncParameterCodeFixProvider)), Shared]
public class SyncParameterCodeFixProvider : CodeFixProvider
{
  public sealed override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticProperties.DiagnosticId];

  public sealed override FixAllProvider GetFixAllProvider()
  {
    return WellKnownFixAllProviders.BatchFixer;
  }

  public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document
      .GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);

    var diagnostic = context.Diagnostics.FirstOrDefault();

    if (diagnostic is null)
    {
      return;
    }

    var diagnosticSpan = diagnostic.Location.SourceSpan;

    var invocation = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
      .OfType<InvocationExpressionSyntax>()
      .FirstOrDefault();

    if (invocation is null)
    {
      return;
    }

    _ = diagnostic.Properties.TryGetValue(DiagnosticProperties.EnclosingSyncName, out var enclosingSyncName);

    if (string.IsNullOrEmpty(enclosingSyncName))
    {
      enclosingSyncName = DiagnosticProperties.DefaultSyncName;
    }

    _ = diagnostic.Properties.TryGetValue(DiagnosticProperties.TargetSyncName, out var targetSyncName);

    if (string.IsNullOrEmpty(targetSyncName))
    {
      targetSyncName = enclosingSyncName;
    }

    _ = diagnostic.Properties.TryGetValue(DiagnosticProperties.TargetSyncOrdinal, out var targetSyncOrdinalStr);

    if (!int.TryParse(targetSyncOrdinalStr, out var targetSyncOrdinal))
    {
      targetSyncOrdinal = -1;
    }

    var codeAction = CodeAction.Create(
      title: $"Pass '{enclosingSyncName}' parameter",
      createChangedDocument: c => AddSyncParameterAsync(
        context.Document,
        invocation,
        enclosingSyncName!,
        targetSyncName!,
        targetSyncOrdinal,
        c
      ),
      equivalenceKey: "PassSyncParameterFix"
    );

    context.RegisterCodeFix(codeAction, diagnostic);
  }

  private static async Task<Document> AddSyncParameterAsync(
    Document document,
    InvocationExpressionSyntax invocation,
    string enclosingSyncName,
    string targetSyncName,
    int targetSyncOrdinal,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

    if (root is null)
    {
      return document;
    }

    var arguments = invocation.ArgumentList.Arguments;
    var syncIdentifier = SyntaxFactory.IdentifierName(enclosingSyncName);

    var existingNamedArg = arguments.FirstOrDefault(
      a => a.NameColon?.Name.Identifier.Text == targetSyncName
    );

    if (existingNamedArg is not null)
    {
      var newNamedArg = existingNamedArg.WithExpression(syncIdentifier);
      var newArguments = arguments.Replace(existingNamedArg, newNamedArg);
      var newInvocation = invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments));
      var newRoot = root.ReplaceNode(invocation, newInvocation);
      return document.WithSyntaxRoot(newRoot);
    }

    var positionalCount = 0;

    foreach (var arg in arguments)
    {
      if (arg.NameColon is not null)
      {
        break;
      }

      positionalCount++;
    }

    var newArg = SyntaxFactory.Argument(syncIdentifier);
    var updatedArguments = targetSyncOrdinal >= positionalCount
      ? arguments.Insert(positionalCount, newArg)
      : arguments.Replace(arguments[targetSyncOrdinal], newArg);

    var resultInvocation = invocation.WithArgumentList(invocation.ArgumentList.WithArguments(updatedArguments));
    var resultRoot = root.ReplaceNode(invocation, resultInvocation);
    return document.WithSyntaxRoot(resultRoot);
  }
}
