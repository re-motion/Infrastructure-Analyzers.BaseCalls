﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

/// <summary>
///   A sample analyzer that reports superstitious names being used in class declarations.
///   Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSyntaxAnalyzer : DiagnosticAnalyzer
{
  // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
  public const string DiagnosticId = "BCA0001";

  // The category of the diagnostic (Design, Naming etc.).
  private const string Category = "Naming";

  // Feel free to use raw strings if you don't need localization.
  private static readonly LocalizableString Title = "Classname is superstitious";

  // The message that will be displayed to the user.
  private static readonly LocalizableString MessageFormat = "The classname '{0}' is superstitious";

  private static readonly LocalizableString Description = "A classname should not be superstitious.";

  private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
    DiagnosticSeverity.Warning, true, Description);

  // Keep in mind: you have to list your rules here.
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    // You must call this method to avoid analyzing generated code.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    // You must call this method to enable the Concurrent Execution.
    context.EnableConcurrentExecution();

    // Subscribe to the Syntax Node with the appropriate 'SyntaxKind' (ClassDeclaration) action.
    // To figure out which Syntax Nodes you should choose, consider installing the Roslyn syntax tree viewer plugin Rossynt: https://plugins.jetbrains.com/plugin/16902-rossynt/
    // Alternatively, you can also use https://sharplab.io/ and set results to Syntax Tree
    context.RegisterSyntaxNodeAction(AnalyzeClassSyntax, SyntaxKind.ClassDeclaration);
  }

  /// <summary>
  ///   Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration', as called above.
  /// </summary>
  /// <param name="context">Operation context.</param>
  private void AnalyzeClassSyntax(SyntaxNodeAnalysisContext context)
  {
    // The Roslyn architecture is based on inheritance.
    // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
    if (context.Node is not ClassDeclarationSyntax classDeclarationNode)
      return;

    // 'Identifier' means the token of the node. In this case, the identifier of the 'ClassDeclarationNode' is the class name.
    var classDeclarationIdentifier = classDeclarationNode.Identifier;

    // Find class symbols whose name contains the company name.
    if (classDeclarationIdentifier.Text.Contains("Superstitious"))
    {
      var diagnostic = Diagnostic.Create(Rule,
        // The highlighted area in the analyzed source code. Keep it as specific as possible.
        classDeclarationIdentifier.GetLocation(),
        // The value is passed to 'MessageFormat' argument of your 'Rule'.
        classDeclarationIdentifier.Text);

      // Reporting a diagnostic is the primary outcome of the analyzer.
      context.ReportDiagnostic(diagnostic);
    }
  }
}