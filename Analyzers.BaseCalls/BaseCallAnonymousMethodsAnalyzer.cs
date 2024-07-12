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

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnonymousMethodsAnalyzer : DiagnosticAnalyzer
{
  private const string DiagnosticId = "RMBCA0003";
  private const string Category = "Usage";

  private static readonly LocalizableString Title = "Base Call found in anonymous method";
  private static readonly LocalizableString MessageFormat = "Base Call is not allowed in anonymous methods";
  private static readonly LocalizableString Description = "Base Calls should not be used in anonymous methods.";

  public static readonly DiagnosticDescriptor DiagnosticDescriptionBaseCallFoundInAnonymousMethod = new(
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      true,
      Description);

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(DiagnosticDescriptionBaseCallFoundInAnonymousMethod);

  public override void Initialize (AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSyntaxNodeAction(AnalyzeAnonymousMethod, SyntaxKind.AnonymousMethodExpression);
    context.RegisterSyntaxNodeAction(AnalyzeAnonymousMethod, SyntaxKind.SimpleLambdaExpression);
    context.RegisterSyntaxNodeAction(AnalyzeAnonymousMethod, SyntaxKind.ParenthesizedLambdaExpression);
  }

  private static void AnalyzeAnonymousMethod (SyntaxNodeAnalysisContext context)
  {
    var anonymousMethod = context.Node as AnonymousFunctionExpressionSyntax;

    if (anonymousMethod == null)
      return;

    SyntaxNode body;

    if (anonymousMethod is SimpleLambdaExpressionSyntax simpleLambda)
    {
      body = simpleLambda.Body;
    }
    else
    {
      body = anonymousMethod.Body;
    }

    if (BaseCallAnalyzer.ContainsAnyBaseCall(context, body))
    {
      ReportBaseCallInAnonymousMethodDiagnostic(context, anonymousMethod);
    }
  }
  
  private static void ReportBaseCallInAnonymousMethodDiagnostic (SyntaxNodeAnalysisContext context, AnonymousFunctionExpressionSyntax anonymousMethod)
  {
    var squiggliesLocation = anonymousMethod.GetLocation();
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInAnonymousMethod, squiggliesLocation));
  }
}