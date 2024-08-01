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

      if (!IsOverride(context, out var isMixin))
      {
        var node = GetNode(context);

        var baseCalls = BaseCallChecker.GetBaseCalls(context, node, isMixin).ToArray();
        if (node.Modifiers.Any(SyntaxKind.NewKeyword))
        {
          BaseCallReporter.ReportAllWrong(context, baseCalls);
        }
        else if (baseCalls.Any())
        {
          BaseCallReporter.ReportAll(context, baseCalls, Rules.InNonOverridingMethod);
        }
      }
      else if (BaseCallMustBePresent(context, isMixin))
      {
        var diagnostic = BaseCallChecker.Check(context, isMixin);
        BaseCallReporter.ReportDiagnostic(context, diagnostic);
      }
    }
    catch (Exception ex)
    {
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
    var node = GetNode(context);

    var expectedBaseCall = isMixin
        ? BaseCall.Default
        : AttributeChecks.CheckForBaseCallCheckAttribute(context);

    if (expectedBaseCall is BaseCall.Default)
    {
      var isVoid = node.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax
                   && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);

      expectedBaseCall = isVoid ? BaseCall.IsMandatory : BaseCall.IsOptional;
    }

    if (expectedBaseCall is BaseCall.IsOptional)
    {
      var baseCalls = BaseCallChecker.GetBaseCalls(context, node, isMixin).ToArray();
      if (baseCalls.Length == 0)
      {
        return false;
      }
      else if (baseCalls.Any(bc => !bc.CallsBaseMethod))
      {
        BaseCallReporter.ReportAllWrong(context, baseCalls);
        return false;
      }
    }

    return true;
  }

  public static MethodDeclarationSyntax GetNode (SyntaxNodeAnalysisContext context)
  {
    return context.Node as MethodDeclarationSyntax
           ?? throw new ArgumentException($"Context should have node of type '{nameof(MethodDeclarationSyntax)}' but was '{context.Node.GetType()}'.", nameof(context));
  }

  private static bool IsOverride (SyntaxNodeAnalysisContext context, out bool isMixin)
  {
    var node = GetNode(context);

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
}