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

    if (localFunction == null)
      return;

    SyntaxNode body = localFunction.Body!;


    if (ContainsBaseCall(context, body))
    {
      ReportBaseCallInLocalFunctionDiagnostic(context);
    }
  }

  private static bool ContainsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (node is ExpressionSyntax expression)
    {
      return IsBaseCall(context, expression);
    }

    foreach (var childNode in node.DescendantNodes())
    {
      if (IsBaseCall(context, childNode))
        return true;
      if (!BaseCallInLoopRecursive(context, childNode))
        continue;
      if (BaseCallInAllIfsRecursive(context, childNode))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsBaseCall (SyntaxNodeAnalysisContext context, SyntaxNode childNode)
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

  private static bool BaseCallInLoopRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (IsBaseCall(context, node)) return true;
    if (!BaseCallAnalyzer.IsLoop(node)) return false;

    var loopStatement = (node as ForStatementSyntax)?.Statement ??
                        (node as WhileStatementSyntax)?.Statement ??
                        (node as ForEachStatementSyntax)?.Statement ??
                        (node as DoStatementSyntax)?.Statement;


    return loopStatement!.ChildNodes().Any(cn => BaseCallInLoopRecursive(context, cn));
  }

  private static bool BaseCallInAllIfsRecursive (SyntaxNodeAnalysisContext context, SyntaxNode node)
  {
    if (IsBaseCall(context, node)) return true;
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
          if (BaseCallInAllIfsRecursive(context, blockChildNode))
            baseCallFoundHere = true;
          if (!BaseCallAnalyzer.IsLoop(blockChildNode))
            continue;
          if (!BaseCallInLoopRecursive(context, blockChildNode))
            continue;
          baseCallFoundHere = false;
          break;
        }
      }

      //for if block without {}
      else if (BaseCallAnalyzer.IsBranch(childNode))
      {
        baseCallFound = baseCallFound && BaseCallInAllIfsRecursive(context, childNode);
      }
      else if (BaseCallAnalyzer.IsLoop(childNode))
      {
        if (!BaseCallInLoopRecursive(context, childNode))
          continue;
        baseCallFoundHere = false;
        break;
      }
      else if (IsBaseCall(context, childNode))
      {
        baseCallFoundHere = true;
      }
    }

    return baseCallFound && baseCallFoundHere;
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