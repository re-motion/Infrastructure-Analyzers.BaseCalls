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

    if (ContainsBaseCall(body))
    {
      ReportBaseCallInAnonymousMethodDiagnostic(context, anonymousMethod);
    }
  }

  private static bool ContainsBaseCall (SyntaxNode node)
  {
    if (node is ExpressionSyntax expression)
    {
      return IsBaseCall(expression);
    }

    foreach (var childNode in node.DescendantNodes())
    {
      if (IsBaseCall(childNode))
        return true;
      if (!BaseCallInLoopRecursive(childNode))
        continue;
      if (BaseCallInAllIfsRecursive(childNode))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsBaseCall (SyntaxNode childNode)
  {
    if (childNode is InvocationExpressionSyntax invocationExpressionNode1)
    {
      if (invocationExpressionNode1.Expression is MemberAccessExpressionSyntax simpleMemberAccessExpressionNode)
      {
        return simpleMemberAccessExpressionNode.Expression is BaseExpressionSyntax;
      }
    }
    else if (childNode is ExpressionStatementSyntax expressionStatementNode)
    {
      if (expressionStatementNode.Expression is InvocationExpressionSyntax invocationExpressionNode2)
      {
        if (invocationExpressionNode2.Expression is MemberAccessExpressionSyntax simpleMemberAccessExpressionNode)
        {
          return simpleMemberAccessExpressionNode.Expression is BaseExpressionSyntax;
        }
      }
    }

    return false;
  }

  private static bool BaseCallInLoopRecursive (SyntaxNode node)
  {
    if (IsBaseCall(node)) return true;
    if (!BaseCallAnalyzer.IsLoop(node)) return false;

    var loopStatement = (node as ForStatementSyntax)?.Statement ??
                        (node as WhileStatementSyntax)?.Statement ??
                        (node as ForEachStatementSyntax)?.Statement ??
                        (node as DoStatementSyntax)?.Statement;


    return loopStatement!.ChildNodes().Any(BaseCallInLoopRecursive);
  }

  private static bool BaseCallInAllIfsRecursive (SyntaxNode node)
  {
    if (IsBaseCall(node)) return true;
    if (!BaseCallAnalyzer.IsBranch(node)) return false;

    var ifStatement = (node as IfStatementSyntax)?.Statement ??
                      (node as ElseClauseSyntax)?.Statement;

    var baseCallFound = true;
    var baseCallFoundHere = false;
    foreach (var childNode in ifStatement!.ChildNodes())
    {
      if (childNode is BlockSyntax blockSyntax)
      {
        //for if blocks with {}
        foreach (var blockChildNode in blockSyntax.ChildNodes())
        {
          if (BaseCallInAllIfsRecursive(blockChildNode))
            baseCallFoundHere = true;
          if (!BaseCallAnalyzer.IsLoop(blockChildNode))
            continue;
          if (!BaseCallInLoopRecursive(blockChildNode))
            continue;
          baseCallFoundHere = false;
          break;
        }
      }

      //for if block without {}
      else if (BaseCallAnalyzer.IsBranch(childNode))
      {
        baseCallFound = baseCallFound && BaseCallInAllIfsRecursive(childNode);
      }
      else if (BaseCallAnalyzer.IsLoop(childNode))
      {
        if (!BaseCallInLoopRecursive(childNode))
          continue;
        baseCallFoundHere = false;
        break;
      }
      else if (IsBaseCall(childNode))
      {
        baseCallFoundHere = true;
      }
    }

    return baseCallFound && baseCallFoundHere;
  }

  private static void ReportBaseCallInAnonymousMethodDiagnostic (SyntaxNodeAnalysisContext context, AnonymousFunctionExpressionSyntax anonymousMethod)
  {
    var squiggliesLocation = anonymousMethod.GetLocation();
    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptionBaseCallFoundInAnonymousMethod, squiggliesLocation));
  }
}