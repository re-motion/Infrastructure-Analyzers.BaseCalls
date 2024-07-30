// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseCallAnalyzer : DiagnosticAnalyzer
{
  //list of Rules
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
  [
      Rules.NoBaseCall, Rules.InLoop, Rules.MultipleBaseCalls, Rules.WrongBaseCall,
      Rules.InTryOrCatch, Rules.InNonOverridingMethod, Rules.InLocalFunction,
      Rules.InAnonymousMethod, Rules.InSwitch, Rules.Error
  ];

  public override void Initialize (AnalysisContext context)
  {
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

    context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);


    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
  }

  private static void AnalyzeMethod (SyntaxNodeAnalysisContext context)
  {
    try
    {
      if (AttributeChecks.HasIgnoreBaseCallCheckAttribute(context))
      {
        return;
      }

      var node = context.Node as MethodDeclarationSyntax
                 ?? throw new ArgumentException("expected MethodDeclarationSyntax");

      if (!DoesOverride(context, out var isMixin))
      {
        var baseCalls = BaseCallChecker.GetBaseCalls(context, node, isMixin).ToArray();
        if (node.Modifiers.Any(SyntaxKind.NewKeyword))
        {
          BaseCallReporter.ReportAllWrong(context, baseCalls);
        }
        else if (baseCalls.Any())
        {
          BaseCallReporter.ReportAll(context, baseCalls, Rules.InNonOverridingMethod);
        }

        return;
      }

      if (BaseCallMustBePresent(context, isMixin))
      {
        var diagnostic = BaseCallChecker.BaseCallCheckerInitializer(context, isMixin);
        BaseCallReporter.ReportDiagnostic(context, diagnostic);
      }
    }
    catch (Exception ex)
    {
      //for debugging, comment return and uncomment reportDiagnostic
      //return;
      context.ReportDiagnostic(Diagnostic.Create(Rules.Error, context.Node.GetLocation(), ex.ToString()));
    }
  }


  private static void AnalyzeLocalFunction (SyntaxNodeAnalysisContext context)
  {
    SyntaxNode? body = ((LocalFunctionStatementSyntax)context.Node).Body;

    if (body is null)
    {
      return;
    }

    BaseCallReporter.ReportAll(context, BaseCallChecker.GetBaseCalls(context, body, false).ToArray(), Rules.InLocalFunction);
  }

  private static bool BaseCallMustBePresent (SyntaxNodeAnalysisContext context, bool isMixin)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    var checkType = isMixin
        ? BaseCall.Default
        : AttributeChecks.CheckForBaseCallCheckAttribute(context);

    if (checkType is BaseCall.Default)
    {
      var isVoid = node.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax
                   && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);

      checkType = isVoid ? BaseCall.IsMandatory : BaseCall.IsOptional;
    }

    if (checkType is BaseCall.IsOptional)
    {
      if (!BaseCallChecker.ContainsBaseCall(context, node, isMixin, out _))
      {
        return false;
      }
    }

    return true;
  }

  private static bool DoesOverride (SyntaxNodeAnalysisContext context, out bool isMixin)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    if (node.Modifiers.Any(SyntaxKind.OverrideKeyword))
    {
      isMixin = false;
      return true;
    }

    // for mixins -> check if there is an [OverrideTarget] attribute
    var hasOverrideTargetAttribute = AttributeChecks.HasOverrideTargetAttribute(context);
    isMixin = hasOverrideTargetAttribute;
    return hasOverrideTargetAttribute;
  }

  public static void AnalyzeAnonymousMethod (SyntaxNodeAnalysisContext context, SyntaxNode? node)
  {
    SyntaxNode body;

    switch (node)
    {
      case SimpleLambdaExpressionSyntax simpleLambda:
        body = simpleLambda.Body;
        break;
      case AnonymousMethodExpressionSyntax anonymousMethod:
        body = anonymousMethod.Body;
        break;
      case ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax:
        body = parenthesizedLambdaExpressionSyntax.Body;
        break;
      default:
        return;
    }


    if (BaseCallChecker.ContainsBaseCall(context, body, false, out var baseCalls))
    {
      BaseCallReporter.ReportDiagnostic(context, Diagnostic.Create(Rules.InAnonymousMethod, baseCalls[0].Location));
    }
  }
}