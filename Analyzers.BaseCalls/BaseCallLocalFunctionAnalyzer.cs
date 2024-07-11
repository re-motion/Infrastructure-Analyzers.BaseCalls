// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallLocalFunctionAnalyzer : DiagnosticAnalyzer
{
  private const string DiagnosticId = "RMBCA0004";
  private const string Category = "Usage";

  private static readonly LocalizableString Title = "Base Call found in local function";
  private static readonly LocalizableString MessageFormat = "Base Call is not allowed in local function";
  private static readonly LocalizableString Description = "Base Calls should not be used in local function.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionBaseCallFoundInLocalFunction = new(
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      true,
      Description);

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [DiagnosticDescriptionBaseCallFoundInLocalFunction];

  public override void Initialize (AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
  }

  private static void AnalyzeLocalFunction (SyntaxNodeAnalysisContext context)
  {
    var localFunction = context.Node as LocalFunctionStatementSyntax;

    if (BaseCallAnalyzer.HasIgnoreBaseCallCheckAttribute(context))
      return;


    if (localFunction == null)
      return;

    SyntaxNode body = localFunction.Body!;


    if (BaseCallAnalyzer.ContainsBaseCall(context, body))
    {
      ReportBaseCallInLocalFunctionDiagnostic(context);
    }
  }

  private static void ReportBaseCallInLocalFunctionDiagnostic (SyntaxNodeAnalysisContext context)
  {
    var node = (LocalFunctionStatementSyntax)context.Node;
    //location of the squigglies (whole line of the method declaration)
    var squiggliesLocation = Location.Create(
        node.SyntaxTree,
        TextSpan.FromBounds(
            node.GetLeadingTrivia().Span.End,
            node.ParameterList.Span.End
        )
    );
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInLocalFunction, squiggliesLocation));
  }
}