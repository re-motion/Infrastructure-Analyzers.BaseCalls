// SPDX-FileCopyrightText: (c) RUBICON IT GmbH, www.rubicon.eu
// SPDX-License-Identifier: MIT
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Remotion.Infrastructure.Analyzers.BaseCalls;

public static class AttributeChecks
{
  private const string c_ignoreBaseCallCheckAttribute =
      "Remotion.Infrastructure.Analyzers.BaseCalls.IgnoreBaseCallCheckAttribute";

  private const string c_emptyTemplateMethodAttribute =
      "Remotion.Infrastructure.Analyzers.BaseCalls.EmptyTemplateMethodAttribute";

  private const string c_overrideTargetAttribute =
      "Remotion.Mixins.OverrideTargetAttribute";

  private const string c_baseCallCheckAttributeMandatory =
      "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(Remotion.Infrastructure.Analyzers.BaseCalls.BaseCall.IsMandatory)";

  private const string c_baseCallCheckAttributeOptional =
      "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(Remotion.Infrastructure.Analyzers.BaseCalls.BaseCall.IsOptional)";

  private const string c_baseCallCheckAttributeDefault =
      "Remotion.Infrastructure.Analyzers.BaseCalls.BaseCallCheckAttribute(Remotion.Infrastructure.Analyzers.BaseCalls.BaseCall.Default)";

  public static bool HasAttribute (IMethodSymbol? methodSymbol, string searchedFullNameOfNamespace)
  {
    if (methodSymbol is null)
    {
      return false;
    }

    var attributes = methodSymbol.GetAttributes().Select(attr => attr.ToString()).ToList();
    return attributes.Any(attribute => attribute == searchedFullNameOfNamespace);
  }

  public static bool HasIgnoreBaseCallCheckAttribute (SyntaxNodeAnalysisContext context)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node);

    return HasAttribute(methodSymbol, c_ignoreBaseCallCheckAttribute);
  }

  public static bool HasOverrideTargetAttribute (SyntaxNodeAnalysisContext context)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node);

    return HasAttribute(methodSymbol, c_overrideTargetAttribute);
  }


  public static BaseCall CheckForBaseCallCheckAttribute (SyntaxNodeAnalysisContext context)
  {
    var node = context.Node as MethodDeclarationSyntax
               ?? throw new ArgumentException("expected MethodDeclarationSyntax");

    var currentMethod = context.SemanticModel.GetDeclaredSymbol(node);

    if (currentMethod is null)
    {
      throw new InvalidOperationException("could not get Semantic Model of node");
    }

    if (currentMethod.OverriddenMethod is null)
    {
      throw new InvalidOperationException("overriding method does not have an overriden method (there will be an other error: no suitable Method for override)");
    }

    if (currentMethod.OverriddenMethod.IsAbstract || HasAttribute(currentMethod.OverriddenMethod, c_emptyTemplateMethodAttribute))
    {
      return BaseCall.IsOptional;
    }

    do
    {
      if (HasAttribute(currentMethod, c_baseCallCheckAttributeOptional))
      {
        return BaseCall.IsOptional;
      }
      else if (HasAttribute(currentMethod, c_baseCallCheckAttributeMandatory))
      {
        return BaseCall.IsMandatory;
      }
      else if (HasAttribute(currentMethod, c_baseCallCheckAttributeDefault))
      {
        return BaseCall.Default;
      }
      else
      {
        currentMethod = currentMethod.OverriddenMethod;
      }
    } while (currentMethod != null);

    return BaseCall.Default;
  }
}